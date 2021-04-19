---
uid: contributing/documentation
---

# Documentation & Website

---

_Details about this website and the API documentation site and how to help contribute to them_

## Overview

The website and the api documentation source code is found in the same Git repository as the Lucene.Net code in the folder: `./websites/`. The website and documentation site is built with a static site generator called [DocFx](https://dotnet.github.io/docfx/) and all of the content/pages are created using Markdown files.

To submit changes for the website, create a Pull Request to the [Lucene Git repositoriy](https://github.com/apache/lucenenet). (See [Contributing](xref:contributing#submit-a-pull-request) for details)

## Website

### Build script

To build the website and run it on your machine, run the Powershell script: `./websites/site/site.ps1` with the `-ServeDocs` flag. For example:

```
./websites/site/site.ps1 -ServeDocs
```

When executed this will build the site and host it at [http://localhost:8081](http://localhost:8081).

To build the website for release, run the script:

```
./websites/site/site.ps1
```

This will build the site with all live parameters configured correctly and output the built static site into the `_site` folder.

The script parameters are:

- `-ServeDocs` _(optional)_ A boolean switch. If present, it will build the docs and host the site. If not present it will build the static site to be hosted elsewhere.
- `-Clean` _(optional)_ A boolean switch.  If present, it will clear all caches and tool files before it builds again. This is handy if a new version of docfx is available or if there's odd things occurring with the incremental build.

### File/folder structure

The file/folder structure is within `./websites/site`:

- `site.ps1` - the build script
- `docfx.json` - the DocFx configuration file _(see docfx manual for further info)_
- `lucenetemplate/*` - the custom template files to style the website
- `*.md` - the root site content such as the index and download pages
- `toc.yml` - these files determine the menu structures _(see docfx manual for further info)_
- `contributing/*` - the Contributing section
- `tools/*` - during the build process some tools will be downloaded which are stored here
- `_site` - this is the exported static site that is generated

### Deploy the website

- The website is deployed via GitHub and is hosted by static files here: https://github.com/apache/lucenenet-site/tree/asf-site _(ensure you have `asf-site` branch checked out, not `master`)_
- Any file changes made in the `master` branch of the Lucene.Net repository under the path `./websites/site/*` will trigger a GitHub action to build the site and publish a Pull Request to the https://github.com/apache/lucenenet-site repository where it can be accepted
- Review and merge the Pull Request. The new version of the website will be live. If the amount of new files committed is large, the new files may take some time to become live.

## API Docs

### Build script

To build the api docs and run it on your machine, run the Powershell script: `./websites/apidocs/docs.ps1`. For example:

```
./websites/apidocs/docs.ps1 -ServeDocs -LuceneNetVersion 4.8.0-beta00008 -BaseUrl http://localhost:8080
```

When executed this will build the site and host it at [http://localhost:8080](http://localhost:8080). _(Ensure to pass in the current version of Lucene.Net you are building.)_

To build the api docs for release, run the script:

```
./websites/apidocs/docs.ps1 -LuceneNetVersion 4.8.0-beta00008
```

This will build the site with all live parameters configured correctly and output the built static site into the `_site` folder.

The script parameters are:

- `-LuceneNetVersion` _(mandatory)_ This is the Lucene.Net version including pre-release information that is being built. For example: `4.8.0-beta00008`. _(This value will correspond to the folder and branch name where the docs get hosted, see below)_
* `-ServeDocs` _(optional)_ A boolean switch. If present, it will build the docs and host the site. If not present it will build the static site to be hosted elsewhere.
* `-Clean` _(optional)_ A boolean switch.  If present, it will clear all caches and tool files before it builds again. This is handy if a new version of docfx is available or if there's odd things occurring with the incremental build.
* `-DisableMetaData` _(optional)_ A boolean switch. If present it will disable the docfx metadata build operation of the docs build. Can be handy when debugging the docs build.
* `-DisableBuild` _(optional)_ A boolean switch. If present it will disable the site building operation of the docs build. Can be handy when debugging the docs build.
* `-DisablePlugins` _(optional)_ A boolean switch. If present it will not build the custom Lucene.Net `DocumentationTools.sln` docsfx plugins and exclude them from the build.
* `-LogLevel` _(optional)_ Default is Warning. Options are: Diagnostic, Verbose, Info, Warning, Error.
* `-BaseUrl` _(optional)_ Default is https://lucenenet.apache.org/docs/. Used to set the base URL of the docfx xref map files for cross linking between project builds.

### File/folder structure

The file/folder structure is within `./websites/apidocs`:

- `docs.ps1` - The build script
- `docfx.*.json` - The DocFx configuration files _(see docfx manual for further info)_
  - `docfx.{library}.json` - Where {library} is an individual Lucene.NET project (i.e. `codecs`). Each library is built as it's own individual DocFx site and it's xref maps are exported to file to be shared between DocFx builds.
  - `docfx.global.json` - Each library DocFx json references this file for global metadata. This is where all global metadata such as Title, Logo, Footer, etc... are declared.
  - `docfx.global.subsite.json` - Each library DocFx json references this file for global metadata which denotes the [`_rel`](https://dotnet.github.io/docfx/tutorial/intro_template.html#system-generated-properties) (The relative path of the root output folder from current output file. i.e. the base URL). For example: `https://lucenenet.apache.org/docs/4.8.0-beta00009/`.
  - `docfx.site.json` - Once each library is built and it's xref maps are exported, the main documentation site container is built with this definition.
- `lucenetemplate/*` - The custom template files to style the website
- `*.md` - The root site content such as the index and download pages
- `toc.yml` - These files determine the menu structures _(see docfx manual for further info)_
- `tools/*` - During the build process some tools will be downloaded which are stored here
- `_site` - This is the exported static site that is generated

### Java to Markdown converter

The documentation generation is a complex process because it needs to convert the Java Lucene project's documentation into a usable format to produce the output Lucene.NET's documentation.

The `JavaDocToMarkdownConverter` dotnet tool to is used to convert the Java Lucene project's docs into a useable format for DocFx. This tool uses a release tag output of the Java Lucene project as it's source to convert against the Lucene.NET's source. This tool must **only** be executed against the current documentation branch, for example in 4.8.0 it is: `docs/markdown-converted/4.8.1`. This conversion process does not need to be executed everytime the docs are built, it is executed rarely when:
- A new major or minor version of Lucene.Net is created and the conversion needs to be re-executed again the Java Lucene source. In this case a new documentation branch should be created from the previous documentation branch.
- Updates to the `JavaDocToMarkdownConverter` are made and the conversion needs to be re-executed.

#### Manual execution

To use the dotnet tool you must download the current tag of the Java Lucene project, for example: ["4.8.1"](https://github.com/apache/lucene-solr/releases/tag/releases%2Flucene-solr%2F4.8.1)

Then install the tool:

```
dotnet tool install javadoc2markdown --add-source https://pkgs.dev.azure.com/lucene-net/_packaging/lucene-net-tools/nuget/v3/index.json --tool-path ./
```

Then run the command:
```
javadoc2markdown <LUCENE DIRECTORY> <LUCENENET DIRECTORY>
```

Where `<LUCENE DIRECTORY>` is the `lucene` sub folder location of the Java Lucene tag downloaded above. The `<LUCENENET DIRECTORY>` is the folder of your locally checked out Lucene.Net git repository of the documentation tag (i.e. `docs/markdown-converted/4.8.1`).

#### Automated execution

A powershell script has been created to automate the above. Execute the `./src/docs/convert.ps1` script and enter the current Lucene version to convert from. For example, for Lucene.Net 4.8.0 we are converting from the Java Lucene build release of ["4.8.1"](https://github.com/apache/lucene-solr/releases/tag/releases%2Flucene-solr%2F4.8.1) so in this case enter: 4.8.1 at the prompt or call the whole script like `./src/docs/convert.ps1 -JavaLuceneVersion 4.8.1`

#### Review, commit and merge

Once the conversion is done, review, commit and push those changes. Many times there will just be whitespace changes in the files especially if this process has been executed before for the same source/destination version. If this is a new source/destination version there will be a **lot** of file changes, at least one file per folder. If there are formatting issues or irregularities in the converted output then these will need to be addressed by making changes to the conversion tool itself (generally only needed for new major version releases).

Once pushed, you can merge those changes to the `master` branch. Doing this may trigger merge conflicts because the documentation files may have been manually edited. In these cases you will need to manually review and fix the merge conflicts with your favorite merge tool ensuring that the most recent manual changes done are kept.

#### Tool info

- [Source code is here](https://github.com/NightOwl888/lucenenet-javadoc2markdown)
- Tool name: `javadoc2markdown`
- [NuGet feed here](https://dev.azure.com/lucene-net/Lucene.NET/_packaging?_a=feed&feed=lucene-net-tools)

### Publishing the docs

> [!NOTE]
> Before publishing, when testing locally ensure that both the "Improve this doc" button on each documentation page and the "View Source" button (when viewing a Class) links correctly to the newly created version branch on GitHub.

- Create and checkout a new branch based on the release tag on the main branch with the name: `docs/[Version]`, for example `docs/4.8.0-beta00008`. This branch is used for linking to on the API docs "Improve this Doc" and "View Source" buttons. Then build the docs, for example: `./websites/apidocs/docs.ps1 -LuceneNetVersion 4.8.0-beta00008` (For testing [see above](#build-script-1)).
- Commit and push any changes you may need to make for the API docs.

- Checkout the Git repo that hosts the documentation: https://github.com/apache/lucenenet-site/tree/asf-site _(ensure you have `asf-site` branch checked out, not `master`)_.
- Create a new folder in this repo: `/docs/[Version]`, for example: `/docs/4.8.0-beta00008`.
- Copy the build output of the documentation site to this new folder. The build output will be all of the files in the `./websites/apidocs/_site` in your main Lucene.NET checked out Git repository. Commit and push these changes.
- The new version documentation will be live. Due to the amount of new files committed, the new files may take up to 60 minutes to become live.
- Next the website needs updating which is a manual process currently:
  - In the `./websites/site/download` folder there should be a document per release. It's normally fine to copy the document of the latest release for the same major version. For a new major version some modifications may be needed.
  - Ensure the correct version number is listed in the header and the NuGet download snippet.
  - Update the `Status` and `Released` heading information.
  - Ensure the download links are correct.
  - Update the `./websites/site/download/toc.yml` and `./websites/site/download/download.md` files to include a reference to the new page which should maintain descending version order.
  - Update the `./websites/site/docs.md` file and add a link to the new documentation for the current version which should maintain descending version order.
  - [Build the website](#website) and test locally, then deploy the changes
