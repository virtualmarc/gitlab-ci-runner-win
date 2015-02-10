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

The configuration initialization begins on the first run, but must be via an administrative command line as the settings are written to the registry.

After the first run the config is written and you can start the runner in the background.

The application can be installed as a service by running the InstallUtil (e.g. c:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe gitlab-ci-runner.exe) from an administrative command line.

Options
-------

If your CI server is using a self-signed certificate you can add -sslbypass option in argument 
