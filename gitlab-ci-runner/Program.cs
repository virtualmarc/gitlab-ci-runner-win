using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using gitlab_ci_runner.conf;
using gitlab_ci_runner.runner;
using gitlab_ci_runner.setup;

namespace gitlab_ci_runner
{
    class Program
    {
        static volatile bool exitSystem = false;

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
            Console.WriteLine("Exiting system due to external CTRL-C, or process kill, or shutdown");

            Runner.stop();

            Console.WriteLine("Cleanup complete");

            exitSystem = true;

            return true;
        }

        static void Main(string[] args)
        {
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);

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
                    Console.WriteLine("Press Ctrl+C to shutdown");
                    
                    Runner.run();

                    while (!exitSystem)
                    {
                        Thread.Sleep(500);
                    }
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
