﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34003
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Install.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Install.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to # mimeTypes.txt
        ///# Worst Web Server Ever
        ///# Written by Ryan Ries in 2014
        ///#
        ///# You can add new MIME types here if you want.
        ///# 
        ///# Format: .ext,mediatype/subtype
        ///# One pair per line. You must include the period in the file extension.
        ///# A change to this file will require that the service be restarted.
        ///#
        ///.asp,text/asp
        ///.asx,video/x-ms-asf
        ///.au,audio/basic
        ///.avi,video/avi
        ///.bmp,image/bmp
        ///.bz,application/x-bzip
        ///.bz2,application/x-bzip2
        ///.c,text/plain
        ///.cer,application/x-x509-ca-cert
        ///.cpp,text/x-c
        ///.crl,a [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string mimeTypes {
            get {
                return ResourceManager.GetString("mimeTypes", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        internal static string Readme {
            get {
                return ResourceManager.GetString("Readme", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        internal static byte[] WorstWebServerEver {
            get {
                object obj = ResourceManager.GetObject("WorstWebServerEver", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot; ?&gt;
        ///&lt;configuration&gt;
        ///    &lt;startup&gt;
        ///        &lt;supportedRuntime version=&quot;v4.0&quot; sku=&quot;.NETFramework,Version=v4.5.1&quot; /&gt;
        ///    &lt;/startup&gt;
        ///    &lt;appSettings&gt;
        ///		&lt;!-- There is a README to help you figure out how to use this config file! --&gt;
        ///		&lt;add key=&quot;HTTP1&quot; value=&quot;http://+:80/;main;index.html&quot; /&gt;
        ///		&lt;add key=&quot;HTTPS1&quot; value=&quot;https://+:443/;main;index.html;5e600893eb2e9a5751867ce1830157c0ab6b15f6&quot; /&gt;
        ///    &lt;/appSettings&gt;
        ///&lt;/configuration&gt;.
        /// </summary>
        internal static string wwseCfgFile {
            get {
                return ResourceManager.GetString("wwseCfgFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Drawing.Icon similar to (Icon).
        /// </summary>
        internal static System.Drawing.Icon wwseicon {
            get {
                object obj = ResourceManager.GetObject("wwseicon", resourceCulture);
                return ((System.Drawing.Icon)(obj));
            }
        }
    }
}