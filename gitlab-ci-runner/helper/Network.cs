using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using gitlab_ci_runner.conf;
using gitlab_ci_runner.helper.json;
using gitlab_ci_runner.runner;
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
                wc.Headers["Content-Type"] = "application/x-www-form-urlencoded";
                wc.Headers["Accept"] = "*/*";
                return wc.UploadString(sUrl, "PUT", sContent);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// POST a String to an URL
        /// </summary>
        /// <param name="sUrl">URL</param>
        /// <param name="sContent">String to POST</param>
        /// <returns>Server Response</returns>
        private static string post(String sUrl, String sContent)
        {
            int iTry = 0;
            while (iTry <= 5)
            {
                try
                {
                    WebClient wc = new WebClient();
                    wc.Headers["Content-Type"] = "application/x-www-form-urlencoded";
                    wc.Headers["Accept"] = "*/*";
                    return wc.UploadString(sUrl, "POST", sContent);
                }
                catch (Exception)
                {
                    iTry++;
                    Thread.Sleep(1000);
                }
            }
            return null;
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
            //string sJsonBody = new { public_key = sPubKey, token = sToken }.ToJson();
            string sPostBody = "token=" + Uri.EscapeDataString(sToken) + "&public_key=" + Uri.EscapeDataString(sPubKey);
            string sResp = post(apiurl + "/runners/register.json", sPostBody);
            if (sResp != null)
            {
                try
                {
                    return JsonObject.Parse(sResp).Get("token");
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

        /// <summary>
        /// Get a new build
        /// </summary>
        /// <returns>BuildInfo object or null on error/no build</returns>
        public static BuildInfo getBuild()
        {
            Console.WriteLine("* Checking for builds...");
            string sPostBody = "token=" + Uri.EscapeDataString(Config.token);
            string sResp = post(apiurl + "/builds/register.json", sPostBody);
            try
            {
                if (!String.IsNullOrEmpty(sResp))
                {
                    JsonObject obj = JsonObject.Parse(sResp);
                    if (obj != null)
                    {
                        BuildInfo info = new BuildInfo();
                        info.id = obj.Get<int>("id");
                        info.project_id = obj.Get<int>("project_id");
                        info.commands = obj.Get<string[]>("commands");
                        info.repo_url = obj.Get("repo_url");
                        info.reference = obj.Get("sha");
                        info.ref_name = obj.Get("ref");
                        return info;
                    }
                }
                else
                {
                    Console.WriteLine("* Nothing");
                }
            }
            catch (Exception)
            {
                Console.WriteLine("* Failed");
            }
            return null;
        }

        /// <summary>
        /// PUSH the Build to the Gitlab CI Coordinator
        /// </summary>
        /// <param name="iId">Build ID</param>
        /// <param name="state">State</param>
        /// <param name="sTrace">Command output</param>
        /// <returns></returns>
        public static bool pushBuild(int iId, State state, string sTrace)
        {
            Console.WriteLine("[" + DateTime.Now.ToString() + "] Submitting build " + iId + " to coordinator ...");
            String sPutBody = "token=" + Uri.EscapeDataString(Config.token) + "&state=";
            if (state == State.RUNNING)
            {
                sPutBody += "running";
            }
            else if (state == State.SUCCESS)
            {
                sPutBody += "success";
            }
            else if (state == State.FAILED)
            {
                sPutBody += "failed";
            }
            else
            {
                sPutBody += "waiting";
            }
            sPutBody += "&trace=" + Uri.EscapeDataString(sTrace);

            int iTry = 0;
            while (iTry <= 5)
            {
                try
                {
                    if (put(apiurl + "/builds/" + iId + ".json", sPutBody) != null)
                    {
                        return true;
                    }
                    else
                    {
                        iTry++;
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception)
                {
                    iTry++;
                    Thread.Sleep(1000);
                }
            }

            return false;
        }
    }
}
