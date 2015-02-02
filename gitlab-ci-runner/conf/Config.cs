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
        }

        /// <summary>
        /// Save the configuration
        /// </summary>
        public static void saveConfig()
        {
            WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            bool hasAdministrativeRight = pricipal.IsInRole(WindowsBuiltInRole.Administrator);

            if (hasAdministrativeRight)
            {
                Registry.SetValue(keyName, "url", url, RegistryValueKind.String);
                Registry.SetValue(keyName, "token", token, RegistryValueKind.String);
            }
            else
            {
                Console.WriteLine("This process needs to be run with administrative priviledges to save the configuration.");
            }
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
