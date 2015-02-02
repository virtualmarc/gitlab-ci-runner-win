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
        }

        protected override void OnStart(string[] args)
        {
            Console.InputEncoding = Encoding.Default;
            Console.OutputEncoding = Encoding.Default;
            ServicePointManager.DefaultConnectionLimit = 999;

            if (args.Contains("-sslbypass"))
            {
                Network.RegisterSecureSocketsLayerBypass();
            }

            Config.loadConfig();

            if (Config.isConfigured())
            {
                Runner.run();
            }
            else
            {
                throw new Exception("Please configure by run the application in the console in admintration mode.");
            }
        }

        protected override void OnStop()
        {
            Runner.stop();
        }
    }
}
