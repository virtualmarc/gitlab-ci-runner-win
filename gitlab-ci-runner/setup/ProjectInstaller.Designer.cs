namespace gitlab_ci_runner
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
            this.gitLabServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.gitlabServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // gitLabServiceProcessInstaller
            // 
            this.gitLabServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.gitLabServiceProcessInstaller.Password = null;
            this.gitLabServiceProcessInstaller.Username = null;
            // 
            // gitlabServiceInstaller
            // 
            this.gitlabServiceInstaller.Description = "GitLab CI Windows Runner";
            this.gitlabServiceInstaller.DisplayName = "The Runner for Windows for GitLab CI";
            this.gitlabServiceInstaller.ServiceName = "GitlabCIRunner";
            this.gitlabServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.gitLabServiceProcessInstaller,
            this.gitlabServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller gitLabServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller gitlabServiceInstaller;
    }
}