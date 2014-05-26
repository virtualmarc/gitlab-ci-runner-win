using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using gitlab_ci_runner.api;
using gitlab_ci_runner.conf;
using gitlab_ci_runner.runner;
using ServiceStack;
using ServiceStack.Text;

namespace gitlab_ci_runner.helper
{
    class Network
    {
        /// <summary>
        /// Gitlab CI API URL
        /// </summary>
        private static string apiurl
        {
            get
            {
                return Config.url + "/api/v1/";
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
			var client = new JsonServiceClient (apiurl);
			try
			{
				var authToken = client.Post (new RegisterRunner
				{
					token = Uri.EscapeDataString (sToken),
					public_key = Uri.EscapeDataString (sPubKey)
				});

				if (!authToken.token.IsNullOrEmpty ())
				{
						Console.WriteLine ("Runner registered with id {0}", authToken.id);
						return authToken.token;
				}
				else
				{
					return null;
				}
			}
			catch(WebException ex)
			{
				Console.WriteLine ("Error while registering runner :", ex.Message);
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
			var client = new JsonServiceClient (apiurl);
			try
			{
				var buildInfo = client.Post (new CheckForBuild
				{
					token = Uri.EscapeDataString (Config.token)
				});

				if (buildInfo != null)
				{
					return buildInfo;                  
				}			
			}
			catch (WebServiceException ex)
			{
				if(ex.StatusCode == 404)
				{
					Console.WriteLine ("* Nothing");
				}
				else
				{
					Console.WriteLine ("* Failed");
				}
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
            Console.WriteLine("[" + DateTime.Now + "] Submitting build " + iId + " to coordinator ...");
        
            var stateValue = "";
            if (state == State.RUNNING)
            {
                stateValue = "running";
            }
            else if (state == State.SUCCESS)
            {
                stateValue = "success";
            }
            else if (state == State.FAILED)
            {
                stateValue = "failed";
            }
            else
            {
                stateValue = "waiting";
            }
            
            var trace = new StringBuilder();
            foreach (string t in sTrace.Split('\n'))
                trace.Append(Uri.EscapeDataString(t)).Append("\n");

            int iTry = 0;
            while (iTry <= 5)
            {
                try
                {
					var client = new JsonServiceClient(apiurl);
					var resp = client.Put (new PushBuild { 
						id = iId + ".json",
						token = Uri.EscapeDataString(Config.token),
						state = stateValue,
						trace = trace.ToString () });

                    if (resp != null)
                    {
                        return true;
                    }
                }
                catch
                { }

                iTry++;
                Thread.Sleep(1000);
            }

            return false;
        }
    }
}
