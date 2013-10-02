using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gitlab_ci_runner.runner
{
    enum State
    {
        RUNNING,
        FAILED,
        SUCCESS,
        WAITING
    }
}
