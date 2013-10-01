using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using gitlab_ci_runner.conf;
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
                    //HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(sUrl);
                    //webReq.Method = "POST";
                    //webReq.ContentType = "text/html";
                    //webReq.Accept = "*/*";
                    //webReq.Headers.Add("Accept", "*/*");
                    //byte[] data = Encoding.ASCII.GetBytes(sContent);
                    //webReq.ContentLength = data.Length;
                    //webReq.KeepAlive = false;
                    //webReq.Timeout = 10000;
                    //Stream srequest = webReq.GetRequestStream();
                    //srequest.Write(data, 0, data.Length);
                    //srequest.Close();
                    //StreamReader srresponse = new StreamReader(webReq.GetResponse().GetResponseStream());
                    //String sresponse = srresponse.ReadToEnd();
                    //srresponse.Close();
                    //Console.WriteLine("RESP: " + sresponse);
                    //return sresponse;
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
    }
}
