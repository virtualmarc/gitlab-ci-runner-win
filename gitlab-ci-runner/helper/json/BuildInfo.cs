using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gitlab_ci_runner.helper.json
{
    class BuildInfo
    {
        public int id;
        public int project_id;
        public string project_name;
        public string[] commands;
        public string repo_url;
        public string sha;
        public string before_sha;
        public string ref_name;         // ref
        public int timeout;
        public bool allow_git_fetch;
    }
}
