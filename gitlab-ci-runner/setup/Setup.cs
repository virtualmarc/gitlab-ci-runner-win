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
            Console.WriteLine("please provide the following info to proceed:");
            Console.WriteLine();

            // Read coordinator URL
            String sCoordUrl = "";
            while (sCoordUrl == "")
            {
                Console.WriteLine("Please enter the gitlab-ci coordinator URL (e.g. http://gitlab-ci.org:3000/ )");
                sCoordUrl = Console.ReadLine();
            }
            Config.url = sCoordUrl;
            Console.WriteLine();

            // Generate SSH Keys
            SSHKey.generateKeypair();

            // Register Runner
            registerRunner();
        }

        /// <summary>
        /// Register the runner with the coordinator
        /// </summary>
        private static void registerRunner()
        {
        }
    }
}
