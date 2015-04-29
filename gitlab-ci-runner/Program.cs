using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Reflection;
using gitlab_ci_runner.conf;
using gitlab_ci_runner.runner;
using gitlab_ci_runner.setup;

namespace gitlab_ci_runner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.Default;
            Console.OutputEncoding = Encoding.Default;
            ServicePointManager.DefaultConnectionLimit = 999;

            if (args.Contains ("-sslbypass"))
            {
                Program.RegisterSecureSocketsLayerBypass ();
            }

            if (Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Substring(0, 1) == @"\") {
                Console.WriteLine("Can't run on UNC Path");
            } else {
                Console.WriteLine("Starting Gitlab CI Runner for Windows");
                Config.loadConfig();
                if (Config.isConfigured()) {
                    // Load the runner
                    Runner.run();
                } else {
                    // Load the setup
                    Setup.run();
                }
            }
            Console.WriteLine();
            Console.WriteLine("Runner quit. Press any key to exit!");
            Console.ReadKey();
        }

        static void RegisterSecureSocketsLayerBypass()
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback +=
            delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                                    System.Security.Cryptography.X509Certificates.X509Chain chain,
                                    System.Net.Security.SslPolicyErrors sslPolicyErrors)
            {
                return true; // **** Always accept
            };
        }
    }
}
