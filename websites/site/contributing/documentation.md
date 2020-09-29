---
uid: contributing/documentation
---

# Documentation & Website

---

_Details about this website and the API documentation site and how to help contribute to them_

## Overview

The website and the api documentation source code is found in the same Git repository as the Lucene.Net code in the folder: `/websites/`. The website and documentation site is built with a static site generator called [DocFx](https://dotnet.github.io/docfx/) and all of the content/pages are created using Markdown files.

To submit changes for the website, create a Pull Request to the [Lucene Git repositoriy](https://github.com/apache/lucenenet). (See [Contributing](xref:contributing#submit-a-pull-request) for details)

## Website

### Build script

To build the website and run it on your machine, run the Powershell script: `/websites/site/site.ps1`. You don't have to pass any parameters in and it will build the site and host it at [http://localhost:8081](http://localhost:8081).

The script has 2 optional parameters:

- `-ServeDocs` _(default is 1)_ The value of `1` means it will build the docs and host the site, if `0` is specified, it will build the static site to be hosted elsewhere.
- `-Clean` _(default is 0)_ The value of `1` means that it will clear all caches and tool files before it builds again. This is handy if a new version of docfx is available or if there's odd things occurring with the incremental build.

### File/folder structure

The file/folder structure is within `/websites/site`:

- `site.ps1` - the build script
- `docfx.json` - the DocFx configuration file _(see docfx manual for further info)_
- `lucenetemplate/*` - the custom template files to style the website
- `*.md` - the root site content such as the index and download pages
- `toc.yml` - these files determine the menu structures _(see docfx manual for further info)_
- `contributing/*` - the Contributing section
- `tools/*` - during the build process some tools will be downloaded which are stored here
- `_site` - this is the exported static site that is generated

### Deploy the website

- The website is deployed via git
- Checkout the Git repo that hosts the documentation: https://github.com/apache/lucenenet-site/tree/asf-site _(ensure you have `asf-site` branch checked out, not `master`)_
- Copy the build output of the website to the root. The build output will be all of the files in the `/websites/site/_site` in your main Lucene.NET checked out Git repository.
- Commit and push these changes
- The new version of the website will be live. If the amount of new files committed is large, the new files may take some time to become live.

## API Docs

### Build script

To build the api docs and run it on your machine, run the powershell script `docs.ps1`. For example: 

```
/websites/apidocs/docs.ps1 -ServeDocs -LuceneNetVersion 4.8.0-beta00008 -BaseUrl http://localhost:8080
```

When executed this will build the site and host it at [http://localhost:8080](http://localhost:8080). _(Ensure to pass in the current version of Lucene.Net you are building.)_

To build the api docs for release, run the script:

```
/websites/apidocs/docs.ps1 -LuceneNetVersion 4.8.0-beta00008
```

This will build the site with all live parameters configured correctly and output the built static site into the `_site` folder. 

The script has several parameters:

* `-LuceneNetVersion` _(mandatory)_ This is the Lucene.Net version including pre-release information that is being built. For example: `4.8.0-beta00008`. _(This value will correspond to the folder and branch name where the docs get hosted, see below)_
* `-ServeDocs` _(optinonal)_ A boolean switch. If present, it will build the docs and host the site. If not present it will build the static site to be hosted elsewhere.
* `-Clean` _(optinonal)_ A boolean switch.  If present, it will clear all caches and tool files before it builds again. This is handy if a new version of docfx is available or if there's odd things occuring with the incremental build.
* `-DisableMetaData` _(optinonal)_ A boolean switch. If present it will disable the docfx metadata build operation of the docs build. Can be handy when debugging the docs build.
* `-DisableBuild` _(optinonal)_ A boolean switch. If present it will disable the site building operation of the docs build. Can be handy when debugging the docs build.
* `-DisablePlugins` _(optinonal)_ A boolean switch. If present it will not build the custom Lucene.Net `DocumentationTools.sln` docsfx plugins and exclude them from the build. 
* `-LogLevel` _(optinonal)_ Default is Warning. Options are: Diagnostic, Verbose, Info, Warning, Error.
* `-BaseUrl` _(optinonal)_ Default is https://lucenenet.apache.org/docs/. Used to set the base URL of the docfx xref map files for cross linking between project builds. 

### File/folder structure

The file/folder structure is within `/websites/apidocs`:

- `docs.ps1` - the build script
- `docfx.json` - the DocFx configuration file _(see docfx manual for further info)_
- `lucenetemplate/*` - the custom template files to style the website
- `*.md` - the root site content such as the index and download pages
- `toc.yml` - these files determine the menu structures _(see docfx manual for further info)_
- `tools/*` - during the build process some tools will be downloaded which are stored here
- `_site` - this is the exported static site that is generated

### Process overview

The documentation generation is a complex process because it needs to convert the Java Lucene project's documentation into a usable format to produce the output Lucene.NET's documentation.

The process overview is:

- Use the `JavaDocToMarkdownConverter` project within the `DocumentationTools.sln` solution to run the conversion of the Java Lucene projects docs into a useable format for DocFx. This tool takes uses a release tag output of the Java Lucene project as it's source to convert against the Lucene.NET's source.
- Run the documentation build script to produce the documentation site
- Publish the output to the [`lucenenet-site`](https://github.com/apache/lucenenet-site) repository into a corresponding named version directory

We don't want to manually change the converted resulting markdown files (`.md`) because they would get overwritten again when the conversion process is re-executed. Therefore to fix any formatting issues or customized output of the project docs, these customizations/fixes/tweaks are built directly into the conversion process itself in the `JavaDocToMarkdownConverter.csproj` project.

### Building the docs

- Checkout the Lucene.Net release tag to build the docs against
- Execute the `./src/docs/convert.ps1` script and enter the current Lucene version to convert from.
  - For example, for Lucene.Net 4.8.0 we are converting from the Java Lucene build release of ["4.8.1"](https://github.com/apache/lucene-solr/releases/tag/releases%2Flucene-solr%2F4.8.1) so in this case enter: 4.8.1 at the prompt or call the whole script like `./src/docs/convert.ps1 -JavaLuceneVersion 4.8.1`
  - This script will download and extract the Java Lucene release files, build the `DocumentationTools.sln` solution and execute the `JavaDocToMarkdownConverter.exe`
- Review and commit the files changed
  - Many times there will just be whitespace changes in the files especially if this process has been executed before for the same source/destination version.
  - If this is a new source/destination version there will be a **lot** of file changes, at least one file per folder.
  - If there are formatting issues or irregularities in the converted output then these will need to be addressed by making changes to the conversion tool itself `JavaDocToMarkdownConverter.csproj` (generally only needed for new major version releases)
- Execute the `./websites/apidocs/docs.ps1` script to build and serve the api docs website locally for testing.
  - Example: `./websites/apidocs/docs.ps1 -LuceneNetVersion 4.8.0-beta00008`
  - will serve a website on [http://localhost:8080](http://localhost:8080)
  - It will take quite a while (approx 10 minutes) to build

### Publishing the docs

- Checkout the Git repo that hosts the documentation: https://github.com/apache/lucenenet-site/tree/asf-site _(ensure you have `asf-site` branch checked out, not `master`)_
- Create a new folder in this repo: `/docs/[Version]`, for example: `/docs/4.8.0-beta00008`
- Copy the build output of the documentation site to this new folder. The build output will be all of the files in the `/websites/apidocs/_site` in your main Lucene.NET checked out Git repository.
- Commit and push these changes
- The new version documentation will be live. Due to the amount of new files committed, the new files may take up to 60 minutes to become live.
- Next the website needs updating which is a manual process currently:
  - In the `/websites/site/download` folder there should be a document per release. It's normally fine to copy the document of the latest release for the same major version. For a new major version some modifications may be needed.
  - Ensure the correct version number is listed in the header and the NuGet download snippet.
  - Update the `Status` and `Released` heading information.
  - Ensure the download links are correct.
  - Update the `/websites/site/download/toc.yml` and `/websites/site/download/download.md` files to include a reference to the new page which should maintain descending version order.
  - Update the `/websites/site/docs.md` file and add a link to the new documentation for the current version which should maintain descending version order.
  - [Build the website](#website) and test locally, then deploy the changes
- Once the website is committed/pushed, the last step is to create a named branch on the main [`lucenenet`](https://github.com/apache/lucenenet) repository with the name: `docs/[Version]`, for example `docs/4.8.0-beta00008` based on commit of the latest (if any) changes made to the docs in the `lucenenet` repository on the main branch. This branch is used for linking to on the API docs "Improve this Doc" button.
