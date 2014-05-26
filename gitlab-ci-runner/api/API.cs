using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack;


/// <summary>
/// V1 API
/// </summary>
namespace gitlab_ci_runner.api
{
	[Route ("/runners/register.json","POST")]
	public class RegisterRunner : IReturn<RunnerInfo>
	{
		public string token
		{
			get;
			set;
		}

		public string public_key
		{
			get;
			set;
		}
	}

	[Route ("/builds/register.json", "POST")]
	public class CheckForBuild : IReturn<BuildInfo>
	{
		public string token
		{
			get;
			set;
		}
	}

	[Route ("/builds/{id}", "PUT")]
	public class PushBuild : IReturn<string>
	{
		public string id
		{
			get;
			set;
		}

		public string token
		{
			get;
			set;
		}

		public string state
		{
			get;
			set;
		}

		public string trace
		{
			get;
			set;
		}
	}

	public class RunnerInfo
	{
		public int id
		{
			get;
			set;
		}
		public string token
		{
			get;
			set;
		}
	}

	public class BuildInfo
	{
		public int id
		{
			get;
			set;
		}

		public int project_id
		{
			get;
			set;
		}

		public string project_name
		{
			get;
			set;
		}

		public string commands
		{
			get;
			set;
		}

		public string repo_url
		{
			get;
			set;
		}

		public string sha
		{
			get;
			set;
		}

		public string before_sha
		{
			get;
			set;
		}

		public string ref_name
		{
			get;
			set;
		}
		public int timeout
		{
			get;
			set;
		}

		public bool allow_git_fetch
		{
			get;
			set;
		}

		public string[] GetCommands()
		{
			return System.Text.RegularExpressions.Regex.Replace (this.commands, "(\r|\n)+", "\n").Split ('\n');
		}
	}
}
