using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using gitlab_ci_runner.conf;
using gitlab_ci_runner.runner;
using gitlab_ci_runner.setup;
using gitlab_ci_runner.helper;

namespace gitlab_ci_runner.service
{
    partial class runnerservice : ServiceBase
    {
        public runnerservice()
        {
            InitializeComponent();

            Config.loadConfig();

            if (!Config.isConfigured())
                throw new Exception("Please configure by running the application via an administrative command prompt.");
        }
                

        protected override void OnStart(string[] args)
        {
            Runner.run();
        }

        protected override void OnStop()
        {
            Runner.stop();
        }
    }
}
