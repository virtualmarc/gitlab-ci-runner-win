using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using gitlab_ci_runner.conf;
using gitlab_ci_runner.helper;

namespace gitlab_ci_runner.setup
{
    class Setup
    {
        /// <summary>
        /// Start the Setup
        /// </summary>
        public static void run()
        {
            Console.WriteLine("This seems to be the first run,");

            if (!Config.canSaveConfig())
            {
                Console.WriteLine("Please run the application with administrative priviledges which are required to save configuration.");
                return;
            }

            Console.WriteLine("please provide the following info to proceed:");
            Console.WriteLine();

            // Read coordinator URL
            String sCoordUrl = "";
            while (sCoordUrl == "")
            {
                Console.WriteLine("Please enter the gitlab-ci coordinator URL (e.g. http(s)://gitlab-ci.org:3000/ )");
                sCoordUrl = Console.ReadLine();
            }
            Config.url = sCoordUrl;
            Console.WriteLine();

            // Register Runner
            registerRunner();
        }

        /// <summary>
        /// Register the runner with the coordinator
        /// </summary>
        private static void registerRunner()
        {
            // Read Token
            string sToken = "";
            while (sToken == "")
            {
                Console.WriteLine("Please enter the gitlab-ci token for this runner:");
                sToken = Console.ReadLine();
            }

            // Register Runner
            string sTok = Network.registerRunner(sToken);
            if (sTok != null)
            {
                // Save Config
                Config.token = sTok;
                if (Config.saveConfig())
                { 
                    Console.WriteLine();
                    Console.WriteLine("Runner registered successfully. Feel free to start it!");
                }
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Failed to register this runner. Perhaps you are having network problems");
            }
        }
    }
}
