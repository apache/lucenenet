# Psake Build Automation

psake is a build automation tool written in PowerShell. It avoids the angle-bracket tax associated with executable XML by leveraging the PowerShell syntax in your build scripts. psake has a syntax inspired by rake (aka make in Ruby) and bake (aka make in Boo), but is easier to script because it leverages your existing command-line knowledge.

psake is pronounced sake – as in Japanese rice wine. It does NOT rhyme with make, bake, or rake.

## Documentation

The docs for the latest version of Psake are at: https://psake.readthedocs.io/en/latest/. There is also some good info on the [README.md](https://github.com/psake/psake#readme).

## Upgrading

Simply download the [latest release of Psake](https://github.com/psake/psake/releases) and drop the files from the `/src` folder it into this directory to upgrade. In general, we don't use very much advanced functionality and the main file names remain stable from one release to the next.