using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace gitlab_ci_runner.helper
{
    class SSHKey
    {
        /// <summary>
        /// Generate a keypair
        /// </summary>
        public static void generateKeypair()
        {
            Process p = new Process();
            p.StartInfo.FileName = "ssh-keygen";
            p.StartInfo.Arguments = "-t rsa -f " + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\.ssh\id_rsa -N """;
            p.Start();
        }

        /// <summary>
        /// Get the public key
        /// </summary>
        /// <returns></returns>
        public static string getPublicKey()
        {
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\.ssh\id_rsa.pub"))
            {
                return TextFile.ReadFile(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\.ssh\id_rsa.pub");
            }
            else
            {
                return null;
            }
        }
    }
}
