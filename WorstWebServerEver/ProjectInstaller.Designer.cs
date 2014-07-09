namespace WorstWebServerEver
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceProcessInstallerWWSE = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstallerWWSE = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceProcessInstallerWWSE
            // 
            this.serviceProcessInstallerWWSE.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstallerWWSE.Password = null;
            this.serviceProcessInstallerWWSE.Username = null;
            // 
            // serviceInstallerWWSE
            // 
            this.serviceInstallerWWSE.DelayedAutoStart = true;
            this.serviceInstallerWWSE.Description = "Worst Web Server Ever";
            this.serviceInstallerWWSE.DisplayName = "Worst Web Server Ever";
            this.serviceInstallerWWSE.ServiceName = "WWSE";
            this.serviceInstallerWWSE.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcessInstallerWWSE,
            this.serviceInstallerWWSE});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstallerWWSE;
        private System.ServiceProcess.ServiceInstaller serviceInstallerWWSE;
    }
}