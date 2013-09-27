using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using gitlab_ci_runner.conf;
using gitlab_ci_runner.helper.json;
using ServiceStack.Text;

namespace gitlab_ci_runner.helper
{
    class Network
    {
        /// <summary>
        /// PUT a String to an URL
        /// </summary>
        /// <param name="sUrl">URL</param>
        /// <param name="sContent">String to PUT</param>
        /// <returns>Server Response</returns>
        private static string put(String sUrl, String sContent)
        {
            try
            {
                WebClient wc = new WebClient();
                wc.Headers["Content-Type"] = "application/json";
                return wc.UploadString(sUrl, "PUT", sContent);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Gitlab CI API URL
        /// </summary>
        private static string apiurl
        {
            get
            {
                return Config.url + "/api/v1";
            }
        }

        /// <summary>
        /// Register the runner with the coordinator
        /// </summary>
        /// <param name="sPubKey">SSH Public Key</param>
        /// <param name="sToken">Token</param>
        /// <returns>Token</returns>
        public static string registerRunner(String sPubKey, String sToken)
        {
            string sJsonBody = new { public_key = sPubKey, token = sToken }.ToJson();
            string sResp = put(apiurl + "/runners/register.json", sJsonBody);
            if (sResp != null)
            {
                try
                {
                    return sResp.FromJson<RegisterResponse>().token;
                }
                catch (Exception)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
