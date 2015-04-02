using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Reflection;
using gitlab_ci_runner.conf;
using gitlab_ci_runner.helper;
using gitlab_ci_runner.runner;
using gitlab_ci_runner.setup;
using NDesk.Options;

namespace gitlab_ci_runner {
    class Program {
        static OptionSet ApplicationArgs;
        static bool IgnoreIntegrityLevelCheck = false;
        static void Main(string[] args) {
            Console.InputEncoding = Encoding.Default;
            Console.OutputEncoding = Encoding.Default;
            ApplicationArgs = new OptionSet() {
                {
                    "admin", "Prevent invoke as lower integrity level", v => {
                        IgnoreIntegrityLevelCheck = true;
                        Console.BackgroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("* Warnings: Using `--admin` flag in High or System integrity level may cause your system changed by CI command or malicious script.");
                        Console.BackgroundColor = ConsoleColor.Black;
                    }
                }, {
                    "sslbypass", "Bypass self-signed SSL Certificate warnings", v => {
                        Program.RegisterSecureSocketsLayerBypass();
                    }
                }, {
                    "h|help", "Show this message and exit", v => {
                        Console.WriteLine("Usage: {0} [OPTIONS]+", System.Reflection.Assembly.GetExecutingAssembly().Location.Split('\\').LastOrDefault());
                        Console.WriteLine("GitLab CI runner for Windows");
                        Console.WriteLine();
                        Console.WriteLine("Options:");
                        ApplicationArgs.WriteOptionDescriptions(Console.Out);
                        Environment.Exit(0);
                    }
                }
			};
            try {
                ApplicationArgs.Parse(args);
            } catch (OptionException e) {
                Console.Write("bundling: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try {0} --help' for more information.", System.Reflection.Assembly.GetExecutingAssembly().Location.Split('\\').LastOrDefault());
                Console.ReadKey();
                return;
            }

            Win32IntegrityLevel WIL = new Win32IntegrityLevel();
            try {
                Win32IntegrityLevel.SECURITY_MANDATORY_RID CurrentSID = WIL.GetCurrentIntegrityLevel();
                Win32IntegrityLevel.SECURITY_MANDATORY_RID TargetSID = Win32IntegrityLevel.SECURITY_MANDATORY_RID.Medium;
                if (System.Diagnostics.Debugger.IsAttached) {
                    // never change SID when in debugging mode in Visual Studio Environment
                    Console.BackgroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("* Warnings: You're currenty running in debugging mode, but you also running CI-runner as too high integrity level, this may cause your system changed by CI command or malicious script.");
                    Console.BackgroundColor = ConsoleColor.Black;
                    TargetSID = CurrentSID;
                }
                if (!IgnoreIntegrityLevelCheck && CurrentSID != TargetSID) {
                    Console.BackgroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("Why you need so high level permission? (Current: {0} Level)\n\tTo ignore this, please invoke by appending `--admin`.", CurrentSID.ToString());
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.WriteLine("Invoking self into {0} integrity secure level.", TargetSID.ToString());
                    string CommandWithArgs = string.Format(@"""{0}"" {1}", System.Reflection.Assembly.GetExecutingAssembly().Location, string.Join(" ", args)).Trim();
                    using (Process pc = Process.GetProcessById(WIL.CreateIntegrityProcess(TargetSID, CommandWithArgs))) {
                        pc.Refresh();
                        pc.WaitForExit();
                    }
                    return;
                }
            } catch (System.ComponentModel.Win32Exception) {
                // always occurred exception prior to Windows Vista
            }

            ServicePointManager.DefaultConnectionLimit = 999;

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

        static void RegisterSecureSocketsLayerBypass() {
            System.Net.ServicePointManager.ServerCertificateValidationCallback +=
            delegate(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                                    System.Security.Cryptography.X509Certificates.X509Chain chain,
                                    System.Net.Security.SslPolicyErrors sslPolicyErrors) {
                return true; // **** Always accept
            };
        }
    }
}
