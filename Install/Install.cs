// Install.cs
// Worst Web Server Ever: Installer
// Ryan Ries, 2014
using System;
using System.IO;
using System.Text;
using System.Management;
using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32;

namespace Install
{    
    class Install
    {
        private static string systemRootPath, programFilesPath, installUtilExe, installedServiceExe = string.Empty;
        private static string currentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        private static string logFile = currentDirectory + @"\WorstInstallLogEver.log";

        private static void Main(string[] args)
        {
            try
            {
                if (File.Exists(logFile))
                    File.Delete(logFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception thrown while attempting to clear logfile! No changes were made.\n" + ex.Message);
                return;
            }

            Trace.Listeners.Clear();
            using (TextWriterTraceListener twtl = new TextWriterTraceListener(logFile))
            {
                twtl.Name = "TextLogger";
                using (ConsoleTraceListener ctl = new ConsoleTraceListener(false))
                {
                    Trace.Listeners.Add(twtl);
                    Trace.Listeners.Add(ctl);
                    Trace.AutoFlush = true;
                    Trace.WriteLine("╔═════════════════════════════════════════╗");
                    Trace.WriteLine("║ Worst Web Server Ever - Install Utility ║");
                    Trace.WriteLine("║ Ryan Ries - myotherpcisacloud.com, 2014 ║");
                    Trace.WriteLine("╚═════════════════════════════════════════╝");
                    Trace.WriteLine("Logging to:");
                    Trace.WriteLine(logFile);


                    if (!IsCurrentUserAdmin())
                        return;

                    if (!GetEnvironmentVars())
                        return;

                    if (!GetInstallUtilPath())
                        return;

                    if (IsServiceInstalled("WWSE"))
                    {
                        Trace.WriteLine("Uninstalling the service currently installed at:");
                        Trace.WriteLine(installedServiceExe);
                        UninstallService();
                    }
                    else
                    {
                        Trace.WriteLine("Service is not currently installed - Installing...");
                        InstallService();
                    }
                }
            }
        }

        private static void InstallService()
        {
            #region Copy Files to Installation Directory
            string wwseInstallPath = programFilesPath + @"\WorstWebServerEver";
            try
            {
                if (!Directory.Exists(wwseInstallPath))
                    Directory.CreateDirectory(wwseInstallPath);

                if (!Directory.Exists(wwseInstallPath + @"\sites"))
                    Directory.CreateDirectory(wwseInstallPath + @"\sites");

                if (!Directory.Exists(wwseInstallPath + @"\sites\main"))
                    Directory.CreateDirectory(wwseInstallPath + @"\sites\main");

                if (!File.Exists(wwseInstallPath + @"\sites\main\index.html"))
                    File.WriteAllText(wwseInstallPath + @"\sites\main\index.html", "<!DOCTYPE html><html><head><title>Worst Web Server Ever by Ryan Ries</title></head><body><p>HELLO, WORLD!</p><p>This is the Worst Web Server Ever, written by Ryan Ries. You're meant to replace this page.</p></body></html>");

                File.Copy(Process.GetCurrentProcess().MainModule.FileName, wwseInstallPath + @"\" + Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName), true);
                File.WriteAllText(wwseInstallPath + @"\WWSE.exe.config", Properties.Resources.wwseCfgFile, Encoding.UTF8);
                File.WriteAllBytes(wwseInstallPath + @"\WWSE.exe", Properties.Resources.WorstWebServerEver);
                File.WriteAllText(wwseInstallPath + @"\mimeTypes.txt", Properties.Resources.mimeTypes, Encoding.UTF8);
                File.WriteAllText(wwseInstallPath + @"\Readme.txt", Properties.Resources.Readme, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Failed to copy files to the installation directory: " + ex.Message);
                return;
            }
            Trace.WriteLine("Files copied to " + wwseInstallPath);
            #endregion
            #region Install Service
            try
            {
                string cmdOutput = string.Empty;
                ProcessStartInfo procStartInfo = new ProcessStartInfo(installUtilExe, "\"" + wwseInstallPath + "\\WWSE.exe\"");               
                procStartInfo.UseShellExecute = false;
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.CreateNoWindow = true;
                using (Process p = new Process())
                {
                    p.StartInfo = procStartInfo;
                    p.Start();
                    cmdOutput = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                }
                if (cmdOutput.Contains("Service WWSE has been successfully installed."))
                    Trace.WriteLine("Service WWSE has been successfully installed.");
                else
                    throw new Exception("Review " + wwseInstallPath + "\\WWSE.InstallLog file for more info.");
            }
            catch (Exception ex)
            {
                Trace.WriteLine("An error occured while installing the WWSE service! " + ex.Message);                
                return;
            }
            #endregion
            #region Add Programs and Features Entry
            try
            {
                using (RegistryKey wwseRegKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Worst Web Server Ever", true))
                {
                    if (wwseRegKey == null)                    
                        using (RegistryKey uninstallRegKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true))
                            uninstallRegKey.CreateSubKey("Worst Web Server Ever");
                }
                using (RegistryKey wwseRegKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Worst Web Server Ever", true))
                {
                    wwseRegKey.SetValue("DisplayName", "Worst Web Server Ever", RegistryValueKind.String);
                    wwseRegKey.SetValue("DisplayVersion", FileVersionInfo.GetVersionInfo(wwseInstallPath + @"\WWSE.exe").FileVersion.ToString(), RegistryValueKind.String);
                    wwseRegKey.SetValue("InstallDate", DateTime.Now.ToShortDateString(), RegistryValueKind.String);
                    wwseRegKey.SetValue("Publisher", "Ryan Ries | myotherpcisacloud.com", RegistryValueKind.String);
                    wwseRegKey.SetValue("UninstallString", "\"" + wwseInstallPath + "\\Install.exe\"", RegistryValueKind.String);
                    wwseRegKey.SetValue("DisplayIcon", "\"" + wwseInstallPath + "\\Install.exe\"", RegistryValueKind.String);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Non-fatal exception occured while adding the application's registry key: " + ex.Message);
            }
            #endregion
            Trace.WriteLine("Success! Don't forget to customzie the config file:");
            Trace.WriteLine(wwseInstallPath + @"\WWSE.exe.config");
            Trace.WriteLine("before starting the service.");
        }

        private static void UninstallService()
        {
            #region Uninstall Service
            try
            {
                string cmdOutput = string.Empty;
                ProcessStartInfo procStartInfo = new ProcessStartInfo(installUtilExe, "/u \"" + installedServiceExe + "\"");
                procStartInfo.UseShellExecute = false;
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.CreateNoWindow = true;
                using (Process p = new Process())
                {
                    p.StartInfo = procStartInfo;
                    p.Start();
                    cmdOutput = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                }
                if (cmdOutput.Contains("Service WWSE was successfully removed from the system."))
                    Trace.WriteLine("Service WWSE has been successfully uninstalled.");
                else
                    throw new Exception("Review installutil log file for more info.");
            }
            catch (Exception ex)
            {
                Trace.WriteLine("An error occured while uninstalling the WWSE service! " + ex.Message);
                return;
            }
            #endregion
            #region Delete Registry Entry
            try
            {
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true))
                {
                    if (regKey.OpenSubKey("Worst Web Server Ever") != null)
                    {
                        regKey.DeleteSubKey("Worst Web Server Ever");
                        Trace.WriteLine("Registry key deleted.");
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Non-fatal exception occured while deleting the application's registry key: " + ex.Message);
            }
            #endregion
            Trace.WriteLine("Uninstall is complete.");
            Trace.WriteLine("You may delete the directory " + Path.GetDirectoryName(installedServiceExe) + " if you wish.");
        }

        private static bool IsCurrentUserAdmin()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    Trace.WriteLine("This application only runs with elevated permissions.");
                    return false;
                }
            }
            Trace.WriteLine("Current user has elevated permissions.");
            return true;
        }

        private static bool GetEnvironmentVars()
        {
            try
            {
                systemRootPath = Environment.GetEnvironmentVariable("SYSTEMROOT");
                if (systemRootPath.Length < 1)
                    throw new Exception("Unable to read SYSTEMROOT environment variable!");

                Trace.WriteLine("System Root: " + systemRootPath);

                programFilesPath = Environment.GetEnvironmentVariable("PROGRAMFILES");
                if (programFilesPath.Length < 1)
                    throw new Exception("Unable to read PROGRAMFILES environment variable!");

                Trace.WriteLine("Program Files: " + programFilesPath);

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("GetEnvironmentVars: " + ex.Message);
                return false;
            }
        }

        private static bool GetInstallUtilPath()
        {
            if(File.Exists(systemRootPath + @"\Microsoft.NET\Framework64\v4.0.30319\installutil.exe"))
            {
                installUtilExe = systemRootPath + @"\Microsoft.NET\Framework64\v4.0.30319\installutil.exe";
                Trace.WriteLine(".NET 4 x64 InstallUtil.exe found.");
                return true;
            }
            else
            {
                Trace.WriteLine("Unable to locate x64 .NET 4 InstallUtil.exe");
                return false;
            }
        }

        private static bool IsServiceInstalled(string serviceName)
        {
            using (ManagementObjectSearcher serviceSearcher = new ManagementObjectSearcher(new WqlObjectQuery("SELECT * FROM Win32_Service WHERE Name = '" + serviceName + "'")))
            {
                try
                {
                    ManagementObjectCollection collection = serviceSearcher.Get();
                    if (collection.Count < 0 || collection.Count > 1)
                        throw new Exception("Unable to query the Service Controller. Cannot continue.");

                    else if (collection.Count == 0)
                    {
                        return false;
                    }
                    else
                    {                        
                        foreach (ManagementObject mo in collection)                        
                            installedServiceExe = mo.GetPropertyValue("PathName").ToString();

                        if (installedServiceExe.StartsWith("\""))
                            installedServiceExe = installedServiceExe.Trim(new char[] { '"' });

                        if (installedServiceExe == string.Empty || installedServiceExe.Length < 1)
                            throw new Exception("Error getting the path of installed service!");                        

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("IsServiceInstalled: " + ex.Message);
                    return false;
                }
            }
        }
    }
}
