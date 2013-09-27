Gitlab CI Runner for Windows
============================

This is an inofficial runner for [Gitlab CI](https://github.com/gitlabhq/gitlab-ci) under Windows.

Requirements
------------

This program only runs under windows operating systems.

For Linux operating systems, take a look at the [official runner for linux](https://github.com/gitlabhq/gitlab-ci-runner).

The following programs are needed:

[mysysgit](http://msysgit.github.io/) (Install with openssh style ssh key support, not with putty keys)

Don't forget to add the git/bin directory (usually C:\Program Files\Git\bin) to your PATH variable!

Installation
------------

Just checkout this repository, open the project in visual studio 2013 and compile yourself or use one of our precompiled binaries.

The configuration initialization begins on the first run.

After the first run the config is written and you can start the runner in the background.

To register the runner as service try the [NSSM](http://nssm.cc/). But run the service as the user you created the config with!
