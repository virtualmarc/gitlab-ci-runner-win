using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using gitlab_ci_runner.api;
using gitlab_ci_runner.helper;

namespace gitlab_ci_runner.runner
{
    class Runner
    {
        /// <summary>
        /// Build process
        /// </summary>
        private static Build build = null;

        /// <summary>
        /// Set to true to shutdown the process
        /// </summary>
        private static volatile bool shutdown = false;

        /// <summary>
        /// The polling thread
        /// </summary>
        private static Thread waitForBuildThread = null;

        /// <summary>
        /// Event Log string
        /// </summary>
        private const string eventSource = "GitLab CI Runner";

        /// <summary>
        /// Set to true when running as a service so it knows to write to the event logs
        /// </summary>
        private static bool runningAsService = false;

        /// <summary>
        /// Start the configured runner
        /// </summary>
        public static void run(bool isService = false)
        {
            runningAsService = isService;

            // Create event source for logging if not present
            if (runningAsService && !EventLog.SourceExists(eventSource))
            {
                EventLog.CreateEventSource(eventSource, eventSource);
            }

            Console.WriteLine("* Gitlab CI Runner started");
            Console.WriteLine("* Waiting for builds");

            waitForBuildThread = new Thread(waitForBuild);
            waitForBuildThread.Start();
        }

        /// <summary>
        /// Stop the runner
        /// </summary>
        public static void stop()
        {
            shutdown = true;

            if (waitForBuildThread != null)
            {
                waitForBuildThread.Join();
                waitForBuildThread = null;
            }
        }

        /// <summary>
        /// Build completed?
        /// </summary>
        public static bool completed
        {
            get
            {
                return running && build.completed;
            }
        }

        /// <summary>
        /// Build running?
        /// </summary>
        public static bool running
        {
            get
            {
                return build != null;
            }
        }

        /// <summary>
        /// Wait for an incoming build or update current Build
        /// </summary>
        private static void waitForBuild()
        {
            while (!shutdown || completed || running)
            {
                if (completed || running)
                {
                    // Build is running or completed
                    // Update build
                    updateBuild();
                }
                else
                {
                    // Get new build
                    getBuild();
                }

                Thread.Sleep(2000);
            }
        }

        /// <summary>
        /// Update the current running build progress
        /// </summary>
        private static void updateBuild()
        {
            if (build.completed)
            {
                // Build finished
                if (pushBuild())
                {
                    if (runningAsService)
                    {
                        EventLog.WriteEntry(eventSource, string.Format("Completed build of '{1}', build No {0}", build.buildInfo.id, build.buildInfo.project_name), EventLogEntryType.Information);
                    }

                    Console.WriteLine("[" + DateTime.Now.ToString() + "] Completed build " + build.buildInfo.id);
                    build = null;
                }
            }
            else
            {
                // Build is currently running
                pushBuild();
            }
        }

        /// <summary>
        /// PUSH Build Status to Gitlab CI
        /// </summary>
        /// <returns>true on success, false on fail</returns>
        private static bool pushBuild()
        {
            return Network.pushBuild(build.buildInfo.id, build.state, build.output);
        }

        /// <summary>
        /// Get a new build job
        /// </summary>
        private static void getBuild()
        {
            BuildInfo binfo = Network.getBuild();
            if (binfo != null)
            {
                // Create Build Job
                build = new Build(binfo);
                Console.WriteLine("[" + DateTime.Now.ToString() + "] Build " + binfo.id + " started...");

                if (runningAsService)
                { 
                    EventLog.WriteEntry(eventSource, string.Format("Starting build of '{1}', build No {0}", binfo.id, binfo.project_name), EventLogEntryType.Information);
                }

                Thread t = new Thread(build.run);
                t.Start();
            }
        }
    }
}
