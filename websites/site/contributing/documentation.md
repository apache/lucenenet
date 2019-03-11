---
uid: contributing/documentation
---
Documentation & Website
===============

---------------

_Details about this website and the API documentation site and how to help contribute to them_

## Overview

The website and the api documentation source code is found in the same Git repository as the Lucene.Net code in the folder: `/websites/`. The site is built with a static site generator called [DocFx](https://dotnet.github.io/docfx/) and all of the content/pages are created using Markdown files.

To submit changes for the website, create a Pull Request to the [Lucene Git repositoriy](https://github.com/apache/lucenenet). (See [Contributing](xref:contributing#submit-a-pull-request) for details)

## Website

To build the website and run it on your machine, run the powershell script: `/websites/site/site.ps1`. You don't have to pass any parameters in and it will build the site and host it at [http://localhost:8080](http://localhost:8080). 

The script has 2 optional parameters:

* `-ServeDocs` _(default is 1)_ The value of `1` means it will build the docs and host the site, if `0` is specified, it will build the static site to be hosted elsewhere.
* `-Clean` _(default is 0)_ The value of `1` means that it will clear all caches and tool files before it builds again. This is handy if a new version of docfx is available or if there's odd things occuring with the incremental build.

The file/folder structure is within `/websites/site`:

* `site.ps1` - the build script
* `docfx.json` - the DocFx configuration file _(see docfx manual for further info)_
* `lucenetemplate/*` - the custom template files to style the website
* `*.md` - the root site content such as the index and download pages
* `toc.yml` - these files determine the menu structures _(see docfx manual for further info)_
* `contributing/*` - the Contributing section
* `tools/*` - during the build process some tools will be downloaded which are stored here
* `_site` - this is the exported static site that is generated

## API Docs

To build the api docs and run it on your machine, run the powershell script: `/websites/apidocs/docs.ps1`. You don't have to pass any parameters in and it will build the site and host it at [http://localhost:8080](http://localhost:8080). 

The script has 2 optional parameters:

* `-ServeDocs` _(default is 1)_ The value of `1` means it will build the docs and host the site, if `0` is specified, it will build the static site to be hosted elsewhere.
* `-Clean` _(default is 0)_ The value of `1` means that it will clear all caches and tool files before it builds again. This is handy if a new version of docfx is available or if there's odd things occuring with the incremental build.

The file/folder structure is within `/websites/apidocs`:

* `docs.ps1` - the build script
* `docfx.json` - the DocFx configuration file _(see docfx manual for further info)_
* `lucenetemplate/*` - the custom template files to style the website
* `*.md` - the root site content such as the index and download pages
* `toc.yml` - these files determine the menu structures _(see docfx manual for further info)_
* `tools/*` - during the build process some tools will be downloaded which are stored here
* `_site` - this is the exported static site that is generated