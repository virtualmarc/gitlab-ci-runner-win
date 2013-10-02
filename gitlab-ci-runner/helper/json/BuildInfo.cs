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
        public string[] commands;
        public string repo_url;
        public string reference;
        public string ref_name;
    }
}
