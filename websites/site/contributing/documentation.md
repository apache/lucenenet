---
uid: contributing/documentation
---
Documentation & Website
===============

---------------

_If you wish to help out with this website and the API documentation site, here's some info that you'll need_

## Website

The website source code is found in the same Git repository as the Lucene.Net code in the folder: `/websites/site`. The site is built with a static site generator called [DocFx](https://dotnet.github.io/docfx/) and all of the content/pages are created using Markdown files.

To build the website and run it on your machine, run the powershell script: `/websites/site/site.ps1`. You don't have to pass any parameters in and it will build the site and host it at [http://localhost:8080](http://localhost:8080). There are 2 parameters that you can use:

* `-ServeDocs` _(default is 1)_ The value of `1` means it will build the docs and host the site, if `0` is specified, it will build the static site to be hosted elsewhere.
* `-Clean` _(default is 0)_ The value of `1` means that it will clear all caches and tool files before it builds again. This is handy if a new version of docfx is available or if there's odd things occuring with the incremental build.

The file/folder structure is within `/websites/site`:

* `site.ps1` - the build script
* `docfx.json` - the DocFx configuration file _(see docfx manual for further info)_
* `*.md` - the root site content such as the index and download pages
* `toc.yml` - these files determine the menu structures _(see docfx manual for further info)_
* `contributing/*` - the Contributing section
* `lucenetemplate/*` - the custom template files to style the website
* `tools/*` - during the build process some tools will be downloaded which are stored here
* `_site` - this is the exported static site that is generated