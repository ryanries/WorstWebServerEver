// WWSEService.cs
// Worst Web Server Ever: Installer
// Ryan Ries, 2014
//
// When writing events to the event log, the event descritpion should begin with "MethodName:",
// depending on the method in which the event log entry takes place.
// The event IDs are 5 digits. The first two digits belong to a certain method.
// If the third digit begins with a 3, it is Informational.
// If the third digit begins with a 2, it is a Warning.
// If the third digit begins with a 1, it is an Error.
// An error event should be something fatal; that will cause the death of the service.
// A warning event should be something that needs attention, but the service can still proceed.
// The last two digits should start at 00 and increment upwards throughout the method.
// Events that describe things that are about to happen or are in the process of happening should end with ellipses. (...)
// Events that describe things that have happened should end with periods. (.)
// Error events (such as those caused by exceptions) should end with exclamation marks!

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Configuration;
using System.ServiceProcess;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace WorstWebServerEver
{
    public partial class WWSEService : ServiceBase
    {
        public static readonly string serverVersion = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();
        public static string sitesRoot = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\sites";
        public static Dictionary<string, string> mimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public WWSEService()
        {
            InitializeComponent();
            this.ServiceName = "WWSE";
            this.CanHandlePowerEvent = false;
            this.CanHandleSessionChangeEvent = false;
            this.CanPauseAndContinue = false;
            this.CanShutdown = true;
            this.CanStop = true;
            this.AutoLog = false;
        }

        /// <summary>
        /// Event Log Code 57
        /// This method happens when the service first starts.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            EventLog.WriteEntry("OnStart: " + this.ServiceName + " has started with Process ID " + Process.GetCurrentProcess().Id + ".", EventLogEntryType.Information, 57300);
            try
            {
                EventLog.WriteEntry("OnStart: Creating SiteManager thread...", EventLogEntryType.Information, 57301);
                Thread siteManagerThread = new Thread(new ThreadStart(SiteManager));
                siteManagerThread.IsBackground = true;
                siteManagerThread.Start();
                EventLog.WriteEntry("OnStart: Site Manager thread started with Thread ID " + siteManagerThread.ManagedThreadId + ".", EventLogEntryType.Information, 57302);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("OnStart: Failed to start Site Manager!\n" + ex.Message, EventLogEntryType.Error, 57100);
                this.Stop();
            }
        }

        protected override void OnStop()
        {
            EventLog.WriteEntry("OnStop: " + this.ServiceName + " is stopping...", EventLogEntryType.Information, 58300);
        }
        
        protected override void OnShutdown()
        {
            this.Stop();
        }

        /// <summary>
        /// Event Log Code 22
        /// The Site Manager runs as a separate thread, spawned from OnStart().
        /// Its purpose is to load the configuration data for all of the websites defined
        /// in the app.config, and start each of the websites on its own thread.
        /// It is possible that some websites do not get loaded because of bad config settings.
        /// </summary>
        private void SiteManager()
        {
            Thread.Yield(); // Let OnStart(), who just spawned us, finish first.            
            List<WebSiteConfiguration> websiteConfigurations = new List<WebSiteConfiguration>();

            if (!Directory.Exists(sitesRoot))
            {
                EventLog.WriteEntry("SiteManager: The " + sitesRoot + " directory is missing! This should have been created during installation!", EventLogEntryType.Error, 22100);
                this.Stop();
                Thread.Sleep(Timeout.Infinite);
            }

            try
            {
                foreach (string mt in File.ReadAllLines(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\mimeTypes.txt"))
                {
                    if (!mt.Trim().StartsWith("."))
                        continue;

                    if (mt.Trim().Length < 2 || mt.Trim().Split(',').Length != 2)
                        continue;

                    mimeTypes.Add(mt.ToLower().Split(',')[0].Trim(), mt.ToLower().Split(',')[1].Trim());
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("SiteManager: Error reading MimeTypes.txt!\n" + ex.Message, EventLogEntryType.Error, 22112);
                this.Stop();
                Thread.Sleep(Timeout.Infinite);
            }

            EventLog.WriteEntry("SiteManger: MIME types loaded: " + mimeTypes.Count, EventLogEntryType.Information, 22344);

            try
            {
                NameObjectCollectionBase.KeysCollection keys = ConfigurationManager.AppSettings.Keys;
                foreach (var key in keys)
                {
                    WebSiteConfiguration websiteConfig = new WebSiteConfiguration();
                    string[] appConfigSettings = (ConfigurationManager.AppSettings.Get(key.ToString().Trim().ToLower())).Split(';');
                    if ((appConfigSettings.Length != 3 & appConfigSettings.Length != 4) ||
                        (!appConfigSettings[0].StartsWith("http")) ||
                        (appConfigSettings[0].StartsWith("https") & appConfigSettings.Length != 4) ||
                        (!appConfigSettings[0].EndsWith("/")) ||
                        (appConfigSettings[0].Split(':').Length != 3) ||
                        (appConfigSettings[0].Trim().Length < 8) || appConfigSettings[1].Trim().Length < 1 || appConfigSettings[2].Length < 1)
                    {
                        EventLog.WriteEntry("SiteManager: The website " + key.ToString() + " has an invalid configuration and will not be loaded.", EventLogEntryType.Warning, 22200);
                        continue;
                    }
                    websiteConfig.Name = key.ToString().Trim();
                    websiteConfig.UrlPrefix = appConfigSettings[0].Trim().ToLower();
                    websiteConfig.RootDirectory = appConfigSettings[1].Trim().ToLower().Trim(new char[] { '/' });
                    websiteConfig.DefaultDocument = appConfigSettings[2].Trim().ToLower();
                    if (!int.TryParse(websiteConfig.UrlPrefix.Split(':')[2].Split('/')[0], out websiteConfig.Port))
                    {
                        EventLog.WriteEntry("SiteManager: The website " + key.ToString() + " has an invalid configuration and will not be loaded. Port number did not make sense.", EventLogEntryType.Warning, 22201);
                        continue;
                    }

                    if (appConfigSettings.Length == 4 & appConfigSettings[0].StartsWith("https"))
                        websiteConfig.CertHash = appConfigSettings[3].Trim().ToLower();

                    if (!Directory.Exists(sitesRoot + @"\" + websiteConfig.RootDirectory))
                    {
                        EventLog.WriteEntry("SiteManager: Cannot locate the directory " + sitesRoot + @"\" + websiteConfig.RootDirectory + ". The website " + key.ToString() + " has an invalid configuration and will not be loaded.", EventLogEntryType.Warning, 22202);
                        continue;
                    }

                    if (!File.Exists(sitesRoot + @"\" + websiteConfig.RootDirectory + @"\" + websiteConfig.DefaultDocument))
                    {
                        EventLog.WriteEntry("SiteManager: Cannot locate the file " + sitesRoot + @"\" + websiteConfig.RootDirectory + @"\" + websiteConfig.DefaultDocument + ". The website " + key.ToString() + " has an invalid configuration and will not be loaded.", EventLogEntryType.Warning, 22203);
                        continue;
                    }

                    if (websiteConfig.UrlPrefix.StartsWith("https"))
                    {
                        if (!ValidateCertificate(websiteConfig.CertHash))
                        {
                            EventLog.WriteEntry("SiteManager: Cannot validate the certificate with hash " + websiteConfig.CertHash + ". The website " + key.ToString() + " has an invalid configuration and will not be loaded.", EventLogEntryType.Warning, 22204);
                            continue;
                        }
                        if (!BindCertificate(websiteConfig.CertHash, websiteConfig.Port))
                        {
                            EventLog.WriteEntry("SiteManager: Cannot bind certificate " + websiteConfig.CertHash + " to port " + websiteConfig.Port + ". The website " + key.ToString() + " has an invalid configuration and will not be loaded.", EventLogEntryType.Warning, 22205);
                            continue;
                        }
                    }
                    websiteConfigurations.Add(websiteConfig);
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("SiteManager: Configuration file is missing, unreadable, or invalid!\n" + ex.Message, EventLogEntryType.Error, 22101);
                this.Stop();
                Thread.Sleep(Timeout.Infinite);
            }

            if (websiteConfigurations.Count < 1)
            {
                EventLog.WriteEntry("SiteManager: No valid websites could be located, so the service will now stop.", EventLogEntryType.Error, 22102);
                this.Stop();
                Thread.Sleep(Timeout.Infinite);
            }

            try
            {
                Thread dataCollectionThread = new Thread(new ThreadStart(GlobalDataCollector.Collect));
                dataCollectionThread.IsBackground = true;
                dataCollectionThread.Priority = ThreadPriority.Lowest;
                dataCollectionThread.Start();
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("WWSEService.SiteManager: Error while starting GlobalDataCollector!\n" + ex.Message, EventLogEntryType.Error, 22103);
                this.Stop();
                Thread.Sleep(Timeout.Infinite);
            }

            foreach (WebSiteConfiguration wc in websiteConfigurations)
            {
                EventLog.WriteEntry("SiteManager:\nSite configuration for " + wc.Name + ":\n  URL Prefix: " + wc.UrlPrefix + "\n  Root Directory: " + sitesRoot + @"\" + wc.RootDirectory + "\n  Default Document: " + wc.DefaultDocument + "\n  Certificate Hash: " + wc.CertHash, EventLogEntryType.Information, 22301);
                try
                {
                    WebServer webServer = new WebServer();
                    webServer.config = wc;
                    Thread wsThread = new Thread(new ThreadStart(webServer.Start));
                    wsThread.IsBackground = true;
                    wsThread.Start();
                    EventLog.WriteEntry("SiteManager: " + wc.Name + " thread started with Managed Thread ID " + wsThread.ManagedThreadId + ".", EventLogEntryType.Information, 22308);

                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry("SiteManager: Failed to start website " + wc.Name + ".\n" + ex.Message, EventLogEntryType.Warning, 22206);
                }
            }
        }        

        /// <summary>
        /// Event log code 62
        /// Return true if the certificate meets all the necessary criteria.
        /// Otherwise return false.
        /// </summary>
        /// <param name="certHash">A string that identifies the certificate</param>
        /// <returns>bool</returns>
        private static bool ValidateCertificate(string certHash)
        {
            X509Store cStore = null;
            X509Certificate2Collection certCollection = null;
            X509Certificate2 certificate = null;
            try
            {
                cStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                cStore.Open(OpenFlags.ReadOnly);
                certCollection = cStore.Certificates;
                cStore.Close();

                if (certCollection.Count < 1)
                {
                    using (EventLog el = new EventLog("Application", ".", "WWSE"))
                        el.WriteEntry("WWSEService.ValidateCertificate: No certificates found in the personal local machine store.", EventLogEntryType.Warning, 62200);

                    return false;
                }

                foreach (X509Certificate2 cer in certCollection)
                    if (cer.GetCertHashString().ToLower().Trim() == certHash)
                    {
                        certificate = cer;
                        break;
                    }

                if (certificate == null)
                {
                    using (EventLog el = new EventLog("Application", ".", "WWSE"))
                        el.WriteEntry("WWSEService.ValidateCertificate: No certificate found that matches the hash in the config file.", EventLogEntryType.Warning, 62201);

                    return false;
                }

                if (!certificate.HasPrivateKey || certificate.NotAfter < DateTime.Now || certificate.NotBefore > DateTime.Now)
                {
                    using (EventLog el = new EventLog("Application", ".", "WWSE"))
                        el.WriteEntry("WWSEService.ValidateCertificate: Certificate has no private key, or is expired.", EventLogEntryType.Warning, 62202);

                    return false;
                }

                foreach (X509Extension ext in certificate.Extensions)
                    if (ext.Oid.FriendlyName == "Enhanced Key Usage")
                    {
                        X509EnhancedKeyUsageExtension ekue = (X509EnhancedKeyUsageExtension)ext;
                        OidCollection oids = ekue.EnhancedKeyUsages;
                        foreach (Oid o in oids)
                            if (o.FriendlyName == "Server Authentication")
                                return true;
                    }

                using (EventLog el = new EventLog("Application", ".", "WWSE"))
                    el.WriteEntry("WWSEService.ValidateCertificate: The certificate cannot be used for Server Authentication.", EventLogEntryType.Warning, 62203);
            }
            catch
            {                
                return false;
            }
            return false;
        }

        /// <summary>
        /// Event Log Code 61
        /// Uses netsh.exe to bind the certificate to the given TCP port.
        /// Returns false if it fails.
        /// </summary>
        /// <param name="certHash">A string that identifies the certificate</param>
        /// <param name="port">The port to bind the certificate to</param>
        /// <returns>bool</returns>
        private static bool BindCertificate(string certHash, int port)
        {
            var appID = (GuidAttribute)System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), true)[0];

            try
            {
                if (port < 1 || port > 65535)
                    throw new Exception("HTTPS port does not make sense.");

                using (Process netsh = new Process())
                {
                    netsh.StartInfo.UseShellExecute = false;
                    netsh.StartInfo.RedirectStandardOutput = true;
                    netsh.StartInfo.FileName = Environment.GetEnvironmentVariable("SYSTEMROOT") + @"\System32\netsh.exe";
                    netsh.StartInfo.Arguments = "http delete sslcert ipport=0.0.0.0:" + port;
                    netsh.Start();
                    netsh.WaitForExit();
                }
                using (Process netsh = new Process())
                {
                    netsh.StartInfo.UseShellExecute = false;
                    netsh.StartInfo.RedirectStandardOutput = true;
                    netsh.StartInfo.FileName = Environment.GetEnvironmentVariable("SYSTEMROOT") + @"\System32\netsh.exe";
                    netsh.StartInfo.Arguments = "http add sslcert ipport=0.0.0.0:" + port + " certhash=" + certHash + " appid={" + appID.Value + "}";
                    netsh.Start();
                    string output = netsh.StandardOutput.ReadToEnd();
                    netsh.WaitForExit();
                    if (!output.Contains("SSL Certificate successfully added"))
                        throw new Exception(output);
                }
            }
            catch (Exception ex)
            {
                using (EventLog el = new EventLog("Application", ".", "WWSE"))
                    el.WriteEntry("WWSEService.BindCertificate: " + ex.Message, EventLogEntryType.Warning, 61200);

                return false;
            }
            return true;
        }
    }

    public class WebServer : IDisposable
    {
        public WebSiteConfiguration config = null;
        private HttpListener webListener = new HttpListener();
        private static readonly Regex tokensRegex = new Regex(@"%TOKEN:(\w+)%", RegexOptions.Compiled);
        private static readonly Regex serverSideIncludeRegex = new Regex(@"%INCLUDE:(\S+)%", RegexOptions.Compiled);

        /// <summary>
        /// Event Log Code 94
        /// Starts the web server and then the listener on a separate thread.
        /// </summary>
        public void Start()
        {
            try
            {
                webListener.Prefixes.Add(config.UrlPrefix);
                webListener.Start();
                Task listener = new Task(() => Listen(), TaskCreationOptions.LongRunning);
                listener.Start();
            }
            catch (Exception ex)
            {
                using (EventLog el = new EventLog("Application", ".", "WWSE"))
                    el.WriteEntry("WebServer.Start: Failed to start server for " + config.Name + ".\n" + ex.Message, EventLogEntryType.Warning, 94200);
                Thread.Sleep(Timeout.Infinite);
            }
        }

        /// <summary>
        /// Here we sit in a loop forever, waiting for incoming requests and 
        /// creating Threadpool tasks whenever one comes in.
        /// </summary>
        private async void Listen()
        {
            while (webListener.IsListening)
            {
                HttpListenerContext context = await webListener.GetContextAsync();
                Task.Run(() => ProcessRequest(context), CancellationToken.None);
            }
            this.Dispose();
        }

        /// <summary>
        /// Event Log Code 11
        /// </summary>
        /// <param name="state"></param>
        private void ProcessRequest(HttpListenerContext ctx)
        {
            HttpListenerRequest request = ctx.Request;
            HttpListenerResponse response = ctx.Response;
            byte[] responseBuffer = new byte[] { };
            string requestUrl = request.RawUrl.ToLower().Trim();

            if (requestUrl == "/")
                requestUrl += Path.GetFileName(config.DefaultDocument);

            try
            {
                if (request.HttpMethod == "OPTIONS")
                {
                    string localFile = WWSEService.sitesRoot + @"\" + config.RootDirectory + requestUrl.Replace('/', '\\');
                    if (!File.Exists(localFile))
                        response.StatusCode = (int)HttpStatusCode.NotFound;                    
                    else
                    {
                        if (WWSEService.mimeTypes.ContainsKey(Path.GetExtension(requestUrl)))
                            response.ContentType = WWSEService.mimeTypes[Path.GetExtension(requestUrl)];

                        response.Headers.Add("Allow", "GET,HEAD,POST,OPTIONS");
                        response.StatusCode = (int)HttpStatusCode.OK;
                    }                    
                }
                else if (request.HttpMethod == "HEAD")
                {
                    string localFile = WWSEService.sitesRoot + @"\" + config.RootDirectory + requestUrl.Replace('/', '\\');
                    if (!File.Exists(localFile))
                        response.StatusCode = (int)HttpStatusCode.NotFound;                    
                    else
                    {
                        if (WWSEService.mimeTypes.ContainsKey(Path.GetExtension(requestUrl)))
                            response.ContentType = WWSEService.mimeTypes[Path.GetExtension(requestUrl)];

                        response.StatusCode = (int)HttpStatusCode.OK;
                    }
                }                  
                else if (request.HttpMethod == "GET")
                {
                    string localFile = WWSEService.sitesRoot + @"\" + config.RootDirectory + requestUrl.Replace('/', '\\');
                    if (!File.Exists(localFile))                    
                        response.StatusCode = (int)HttpStatusCode.NotFound;                    
                    else
                    {
                        if (WWSEService.mimeTypes.ContainsKey(Path.GetExtension(requestUrl)))
                            response.ContentType = WWSEService.mimeTypes[Path.GetExtension(requestUrl)];

                        if (response.ContentType.StartsWith("text/"))
                        {
                            string responseString = File.ReadAllText(localFile);
                            foreach (Match ssi in serverSideIncludeRegex.Matches(responseString))
                            {
                                string includeFileName = ssi.Value;
                                string[] fileNames = Directory.GetFiles(WWSEService.sitesRoot + @"\" + config.RootDirectory + @"\posts");
                                Array.Sort(fileNames);
                                if (ssi.Value == "%INCLUDE:?MOSTRECENT%")
                                {
                                    responseString = responseString.Replace(ssi.Value, File.ReadAllText(fileNames[fileNames.Length - 1]));
                                }
                                else
                                {
                                    includeFileName = includeFileName.TrimStart('/');
                                    includeFileName = WWSEService.sitesRoot + @"\" + config.RootDirectory + @"\" + ssi.Value.Replace("%INCLUDE:", string.Empty).TrimEnd('%');
                                    if (File.Exists(includeFileName))
                                        responseString = responseString.Replace(ssi.Value, File.ReadAllText(includeFileName));
                                    else
                                        responseString = responseString.Replace(ssi.Value, "SERVER-SIDE INCLUDE ERROR!");
                                }
                            }

                            var tokens = new Dictionary<string, string>()
                            {
                                { "SERVERVERSION",       WWSEService.serverVersion },
                                { "CPUUSAGE",            GlobalDataCollector.totalCpuUsagePct },                                
                                { "LONGDATE",            GlobalDataCollector.now.ToLongDateString() },
                                { "SHORTDATE",           GlobalDataCollector.now.ToShortDateString() },
                                { "LONGTIME",            GlobalDataCollector.now.ToLongTimeString() },
                                { "SHORTTIME",           GlobalDataCollector.now.ToShortTimeString() },
                                { "CURRENTWORKINGSETMB", GlobalDataCollector.currentWorkingSetMb },
                                { "PEAKWORKINGSETMB",    GlobalDataCollector.peakWorkingSetMb },
                                { "PROCESSSTARTTIME",    GlobalDataCollector.processStartTime },
                                { "HANDLECOUNT",         GlobalDataCollector.handleCount }                                
                            };
                            responseBuffer = Encoding.UTF8.GetBytes(tokensRegex.Replace(responseString, match => tokens[match.Groups[1].Value]));
                        }
                        else
                            responseBuffer = File.ReadAllBytes(localFile);

                        response.StatusCode = (int)HttpStatusCode.OK;
                    }
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                }
            }
            catch (Exception ex)
            {
                using (EventLog el = new EventLog("Application", ".", "WWSE"))
                    el.WriteEntry("WebServer.ProcessRequest: " + ex.ToString(), EventLogEntryType.Warning, 11200);
            }
            finally
            {                
                if (response.StatusCode != (int)HttpStatusCode.OK ||  request.HttpMethod == "HEAD" || request.HttpMethod == "OPTIONS")
                    response.Close();
                else
                    response.Close(responseBuffer, false);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                webListener.Close();
        }

    }

    public static class GlobalDataCollector
    {
        public static DateTime now = DateTime.Now;
        public static string currentWorkingSetMb, peakWorkingSetMb, processStartTime, handleCount = string.Empty;
        public static string totalCpuUsagePct = string.Empty;

        public static void Collect()
        {
            while (true)
            {
                now = DateTime.Now;

                using (Process p = Process.GetCurrentProcess())
                {
                    currentWorkingSetMb = Math.Round(((p.WorkingSet64 / 1024f) / 1024f), 2).ToString();
                    peakWorkingSetMb = Math.Round(((p.PeakWorkingSet64 / 1024f) / 1024f), 2).ToString();
                    processStartTime = p.StartTime.ToString();
                    handleCount = p.HandleCount.ToString();                    
                }

                using (PerformanceCounter pfc = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                {
                    pfc.NextValue();
                    Thread.Sleep(2000);
                    totalCpuUsagePct = Math.Round(pfc.NextValue(), 2).ToString();
                }
            }
        }
    }

    public class WebSiteConfiguration
    {
        public string Name = string.Empty;
        public string UrlPrefix = string.Empty;
        public string RootDirectory = string.Empty;
        public string DefaultDocument = string.Empty;
        public string CertHash = string.Empty;
        public int Port = 0;
    }
}
