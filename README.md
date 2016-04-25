Gitlab CI Runner for Windows
============================

This is an unofficial runner for [Gitlab CI](https://github.com/gitlabhq/gitlab-ci) under Windows.

Requirements
------------

This program only runs under Windows operating systems.

For Linux operating systems, take a look at the [official runner for linux](https://github.com/gitlabhq/gitlab-ci-runner).

The following programs are needed:

[msysgit](http://msysgit.github.io/) (Install with openssh style ssh key support, not with putty keys)

Don't forget to add the `git/bin` directory (usually `C:\Program Files\Git\bin`) to your `PATH` variable!

Installation
------------

Just checkout this repository, open the project in Visual Studio 2013 and compile it yourself.

The configuration initialization begins on the first run.

After the first run the config is written and you can start the runner in the background.

To register the runner as service try the [NSSM](http://nssm.cc/). But remember to run the service as the user you created the config with!

Options
-------

If your CI server is using a self-signed certificate you can add -sslbypass option in argument

How it works
------------

The whole thing works in the following way:

  1. The runner should be endlessly up and listen to the commands from CI (this is why you should use NSSM or its equivalent).
  2.  As soon as CI sends a command to the runner it performs the preparation actions (clones or fetches the project from the server to some folder)
  3. When the project is on the runner's machine, it executes the script you write in the CI control panel.

You may need an additional control how the runner updates the project. For example, you may want to use a particular project folder. Or avoid using `git reset --hard` before each update. Or use `git clone --recursive` instead of simple `git clone`.

To do it, you can additionally configure runner as explained in the next section.

Additional configuration
------------------------

After the first launch, the runner creates a `runner.cfg` file which holds the URL to the Gitlab CI server and a runner token you should insert when registering the runner there:

```
[main]
url=http://ci.gitlab.example.com/
token=<sometoken>
```

You may add extra sections (see below how you name them) which may hold the following keys:

- `ProjectDir` - a directory where the project should be created. It may be relative to the runner folder or absolute.
- `NewRepoInitCommand` - a command which should prepare a new repo
- `ExistingRepoInitCommand` - a command which should update a repo if it already exists
- `PostPrepareCommand` - a command which is called after `NewRepoInitCommand` or `ExistingRepoInitCommand` is executed.

### About commands

Commands are executed in the `ProjectDir`, so there is no need to `cd` there.

If you need multiple commands, you can either put concatenate them with `&&` (`git clone {repo_url} && git checkout {commit}`) or put
them to a `.bat` file.  

If you don't specify commands, by default it will be:

```
NewRepoInitCommand=git clone {repo_url} {project_dir} && cd {project_dir} && git checkout {commit}
ExistingRepoInitCommand=git reset --hard && git clean -f && git fetch && git checkout {commit}
```

### Placeholders

Each of these keys may contain placeholders which will be replaced by a data sent from CI:

  - `{build_id}` - build ID provided by Gitlab CI server
  - `{project_id}` - project ID provided by Gitlab CI (note, not by Gitlab itself!)
  - `{project_name}` - a project name including namespace, without whitespaces (e.g. Group1/myproject)
  - `{project_dir}` - won't be applied for the ProjectDir key.
  - `{commit}` - uuid which identifies a current commit
  - `{previous_commit}` - uuid of the previous commit
  - `{repo_url}` - repo URL
  - `{ref_name}` - ref name

### Defining different settings for different projects

The same runner may be reused by multiple projects. You may want to have different project with different settings.

To achieve this, you should refer the project in the section name. You can do it either using ID or name:

`[id=123]`

or

`[name=group1/myprojectname]`

You can use the same commands for several projects by separating them with a `|` sign:

`[id=123|id=234|name=group1/someproject]`

At last, you may specify the universal rules using `*` sign:

`[*]`

Note, you should put `[*]` in the end of config, otherwise it will ignore settings for your specific project. You may think about it as `default` in the `switch...case` block.

### Putting things together

It is a time for an example:

``` ini
[main]
url=http://ci.gitlab.example.com/
token=<sometoken>
[id=123]
ProjectDir=C:\CI\MyTest
NewRepoInitCommand=git clone --recursive {repo_url}
[name=somegroup/someproject|id=124]
ProjectDir=C:\CI\{project_name}
NewRepoInitCommand=myclonecommands.bat {project_id} {repo_url}
ExistingRepoInitCommand=myfetchcommands.bat {project_id} {repo_url}
[*]
ProjectDir=C:\defaultprojectdir\project_{project_id}
PostPrepareDir=echo "ProjDir: {project_dir}, ProjID: {project_id}, ProjName={project_name}, BuildID: {build_id}, RepoUrl: {repo_url}, RefName: {ref_name}, SHA: {sha}, BeforeSHA: {before_sha}"
```
