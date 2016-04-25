using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using gitlab_ci_runner.helper;
using gitlab_ci_runner.api;

namespace gitlab_ci_runner.conf
{

    public class Config
    {

        public class PrebuildConfig
        {
            private static string GetValue(IniParser.Model.KeyDataCollection keys, string name)
            {
                return keys.ContainsKey(name) ? keys[name] : "";
            }

            internal PrebuildConfig(BuildInfo b, IniParser.Model.SectionData data)
            {
                /*
                _clone = new CmdConfig(b, data, "Clone");
                _checkout = new CmdConfig(b, data, "Checkout");
                _fetch = new CmdConfig(b, data, "Fetch");
                */

                _projectDir = GetValue(data.Keys, "ProjectDir");
                _newRepoInit = GetValue(data.Keys, "NewRepoInitCommand");
                _existingRepoInit = GetValue(data.Keys, "ExistingRepoInitCommand");
                _postPrepare = GetValue(data.Keys, "PostPrepareCommand");
                var r = new[] { 
                    new { key = "{project_dir}", val = "" },
                    new { key = "{build_id}", val = b.id.ToString() }, 
                    new { key = "{project_id}", val = b.project_id.ToString() }, 
                    new { key = "{project_name}", val = Regex.Replace(b.project_name, @"\s+", "") }, 
                    new { key = "{commit}", val = b.sha }, 
                    new { key = "{previous_commit}", val = b.before_sha }, 
                    new { key = "{repo_url}", val = b.repo_url }, 
                    new { key = "{ref_name}", val = b.ref_name } };
 
                foreach(var rule in r)
                {
                    _projectDir = _projectDir.Replace(rule.key, rule.val);
                }

                r[0] = new { key = "{project_dir}", val =_projectDir};
                foreach (var rule in r)
                {
                    _newRepoInit = _newRepoInit.Replace(rule.key, rule.val);
                    _existingRepoInit = _existingRepoInit.Replace(rule.key, rule.val);
                    _postPrepare = _postPrepare.Replace(rule.key, rule.val);
                }

            }

            private string _projectDir;
            private string _postPrepare;
            private string _newRepoInit;
            private string _existingRepoInit;

            public string ProjectDir { get { return _projectDir; } }
            public string NewRepoInit { get { return _newRepoInit; } }
            public string ExistingRepoInit { get { return _existingRepoInit; } }
            public string PostPrepare { get { return _postPrepare; } }
        }
        
        /// <summary>
        /// URL to the Gitlab CI coordinator
        /// </summary>
        public static string url;

        /// <summary>
        /// Gitlab CI runner auth token
        /// </summary>
        public static string token;

        /// <summary>
        /// Configuration Path
        /// </summary>
        private static string confPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\runner.cfg";

        /// <summary>
        /// Load the configuration
        /// </summary>
        public static void loadConfig()
        {
            if (File.Exists(confPath))
            {
                IniFile ini = new IniFile(confPath);
                url = ini.IniReadValue("main", "url");
                token = ini.IniReadValue("main", "token");
            }
        }

        /// <summary>
        /// Save the configuration
        /// </summary>
        public static void saveConfig()
        {
            if (File.Exists(confPath))
            {
                File.Delete(confPath);
            }

            IniFile ini = new IniFile(confPath);
            ini.IniWriteValue("main", "url", url);
            ini.IniWriteValue("main", "token", token);
        }

        public static PrebuildConfig getDataForBuild(gitlab_ci_runner.api.BuildInfo b)
        {
            if (!isConfigured())
            {
                throw new NotImplementedException("Situation when no configuration file is provided is not supported yet. Make sure that runner.cfg exists at the file location.");
            }
            IniParser.FileIniDataParser ini = new IniParser.FileIniDataParser();
            IniParser.Model.IniData data = ini.ReadFile(confPath);

            IniParser.Model.SectionData section = null;
            foreach (var sect in data.Sections)
            {
                string[] projectIdentifiers = sect.SectionName.Split(new char[] {'|'} );
                foreach(var p in projectIdentifiers)
                {
                    string[] components = p.Split( new char[] {'='} );
                    if (components.Length == 2)
                    {
                        switch (components[0])
                        {
                            case "id":
                                if (Convert.ToInt32(components[1]) == b.project_id)
                                {
                                    section = sect;
                                }
                                break;
                            case "name":
                                if (components[1] == b.project_name || components[1] == Regex.Replace(b.project_name, @"\s+", ""))
                                {
                                    section = sect;
                                }
                                break;
                            default:
                                throw new Exception(String.Format("Cannot parse the {0} due to unknown project filter {1}", confPath, p));
                        }
                    }
                    else 
                    {
                        if (p == "*")
                        {
                            section = sect;
                        }
                        else 
                        {
                            if (p!="main")
                                throw new Exception(String.Format("Cannot parse the {0} while searching for the project prebuild steps commands. Section {1} cannot be recognized.", confPath, p));
                        }
                    }
                    if (section != null)
                    {
                        break;
                    }
                }
                if (section != null)
                {
                    break;
                }
            }
            return new PrebuildConfig(b, section);
        }

        /// <summary>
        /// Is the runner already configured?
        /// </summary>
        /// <returns>true if configured, false if not</returns>
        public static bool isConfigured()
        {
            if (url != null && url != "" && token != null && token != "")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
