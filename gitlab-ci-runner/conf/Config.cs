using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;
using gitlab_ci_runner.helper;

namespace gitlab_ci_runner.conf
{
    class Config
    {
        /// <summary>
        /// URL to the Gitlab CI coordinator
        /// </summary>
        public static string url;

        /// <summary>
        /// Gitlab CI runner auth token
        /// </summary>
        public static string token;

        /// <summary>
        /// Gitlab CI project folder
        /// </summary>
        public static string projectFolder;

        /// <summary>
        /// Registry key name
        /// </summary>
        private static string keyName = "HKEY_LOCAL_MACHINE\\SOFTWARE\\GitLab\\CI-Runner";

        /// <summary>
        /// Load the configuration
        /// </summary>
        public static void loadConfig()
        {
            var value = Registry.GetValue(keyName, "url", null);

            if (value != null)
            {
                url = value.ToString();
            }

            value = Registry.GetValue(keyName, "token", null);

            if (value != null)
            {
                token = value.ToString();
            }

            value = Registry.GetValue(keyName, "folder", null);

            if (value != null)
            {
                projectFolder = value.ToString();
            }
            else
            {
                projectFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\projects";
            }
        }

        /// <summary>
        /// Determines if the user can save the config
        /// </summary>
        public static bool canSaveConfig()
        {
            WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());

            return pricipal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Save the configuration
        /// </summary>
        public static bool saveConfig()
        {
            if (canSaveConfig())
            {
                Registry.SetValue(keyName, "url", url, RegistryValueKind.String);
                Registry.SetValue(keyName, "token", token, RegistryValueKind.String);
                Registry.SetValue(keyName, "folder", projectFolder, RegistryValueKind.String);

                return true;
            }
            else
            {
                Console.WriteLine("This process needs to be run with administrative priviledges to save the configuration.");
            }

            return false;
        }

        /// <summary>
        /// Is the runner already configured?
        /// </summary>
        /// <returns>true if configured, false if not</returns>
        public static bool isConfigured()
        {
            return (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(token));
        }
    }
}
