---
uid: contributing/make-release
---

# Making a release of Lucene.NET

> [!NOTE]
> This is a project-specific procedure, based on the [Apache Release Creation Process](https://infra.apache.org/release-publishing.html)

> [!NOTE]
> All commands should be executed from the root of the Lucene.NET repository unless otherwise stated.

## Versioning

For Package Version, NuGet version and branch naming guidelines see the [Versioning Procedure Overview](xref:contributing/versioning) document.

## Release Preparation

- Nominate a release manager (must be a Lucene.NET committer).

- Review all GitHub issues associated with the release milestone. All issues should be resolved or closed.

- Any issues that have been assigned to the release milestone that are still in progress should be moved to the next milestone. Any critical or blocker issues should be resolved on the `dev` mailing list. Discuss any issues that you are unsure of on the `dev` mailing list.

## Steps for the Release Manager

The following steps need only to be performed once.

- Install the [Java Runtime Environment](https://www.oracle.com/java/technologies/javase-jre8-downloads.html). This is a dependency of the Release Audit Tool.

- Install a subversion command line client, such as [TortoiseSVN](https://tortoisesvn.net/downloads.html)

- Install [GNU Privacy Guard](https://www.gnupg.org/)

- [Generate a code signing key](https://infra.apache.org/release-signing.html#generate) if you don't already have one

- Make sure you have your PGP key entered into [https://id.apache.org/](https://id.apache.org/). Your KEYS will then be present in [https://people.apache.org/keys/group/lucenenet.asc](https://people.apache.org/keys/group/lucenenet.asc).

- Make sure you have your PGP keys password.

- Append your PGP key to the `KEYS` file in [https://dist.apache.org/repos/dist/release](https://dist.apache.org/repos/dist/release):

  ```powershell
  dotnet msbuild -t:AppendSignature -p:ApacheID=<apacheId>
  ```

  > [!NOTE]
  > You may be prompted for your password

## Release Steps

- Checkout the Lucene.NET master branch:
  - If you have no local clone:
    - `git clone https://github.com/apache/lucenenet.git`
  - If you are updating your local clone:
    - Save your local work (if any, run `git status` to determine this) in the stash or commit it to a branch other than master
    - `git checkout master` and make sure it succeeds to be on the master branch
    - `git pull https://gitbox.apache.org/repos/asf/lucenenet.git master` to update your local copy

- Prepare a GitHub Release. Review the [master branch commit history](https://github.com/apache/lucenenet/commits/master), and [create a new **DRAFT** GitHub release](https://github.com/apache/lucenenet/releases/new) using the following template:

  ```txt
  > This release contains <any important changes that users should be aware of>

  ## Change Log

  ### Breaking Changes
  * #<GitHub Issue ID (optional)> - <A descriptive title (may need to add context or summarize)>

  ### Bugs
  * #<GitHub Issue ID (optional)> - <A descriptive title (may need to add context or summarize)>

  ### Improvements
  * #<GitHub Issue ID (optional)> - <A descriptive title (may need to add context or summarize)>

  ### New Features
  * #<GitHub Issue ID (optional)> - <A descriptive title (may need to add context or summarize)>
  ```

  > [!TIP]
  > Click the "Save draft" button frequently during the above updates to ensure you don't lose them if your browser crashes.

  > [!NOTE]
  > Be sure to check the "This is a pre-release" box as appropriate.
  
- Click the "Save draft" button when finished.
  

- Update Website with the release notes
  
  Only people with permissions on GitHub will be able to see the draft release notes. [PMC members](https://people.apache.org/phonebook.html?ctte=lucenenet) and [committers](https://people.apache.org/phonebook.html?unix=lucenenet) can have permissions, but only if they set them up themselves. So, we duplicate the above information on a release notes page that all subscribers of the dev@lucenenet.apache.org mailing list can view during the release vote.

  > [!NOTE]
  > If the release notes require updating, don't forget the primary place to update them are on the GitHub Releases draft page above. It is best to update that page and then copy the changes from GitHub Releases to the release notes page on the website, then commit the changes and [publish the website](https://lucenenet.apache.org/contributing/documentation.html#website) as usual.

  - Create a new release notes page in the `/websites/site/release-notes` directory, in most cases it's easiest to just copy the previous release notes page.
    - Ensure the `uid` in the header is correct
    - Update all headers to the release version number (i.e. `4.8.0-beta00008`)
  - Copy the release notes from the GitHub Releases draft to the new website release notes page
    - Delete the prior information in the current version release notes except for the header (whether from a prior version or an update)
    - Copy and paste the GitHub Releases draft just below the header
    - Add the URLs to issues on GitHub. These are the instructions for Notepad++. Modify as needed if you are using another text editor.
      - Find: `#(\d+)`
      - Replace with: `[#$1]\(https://github.com/apache/lucenenet/pull/$1\)`
      - Search mode: "regular expression"
      - Click "Replace All" and verify the URLs work correctly

  - Follow the instructions on how to [build, test & publish the website](https://lucenenet.apache.org/contributing/documentation.html#website) and run/test the website locally.

  - Commit changes and [publish the website](https://lucenenet.apache.org/contributing/documentation.html#website).

- Add Missing License Headers

  - Run [Apache Release Audit Tool](https://creadur.apache.org/rat/):

    ```powershell
    dotnet msbuild -t:AuditRelease
    ```

  - Review and commit the changes to your local Git clone, adding exclusions to `.rat-excludes` and re-running as necessary
    - Exclude files that already have license headers
    - Exclude files that are automatically generated
    - Exclude files that don't work properly with license headers included
  - Review the LICENSE.txt file.
    - If any new files with an alternate license were flagged by the Apache Release Audit Tool in the last step, add the full path to the files and the full licenses (see the LICENSE.txt file for examples). If the license is not found in the approved license list https://www.apache.org/legal/resolved.html#category-a, the release **must be postponed** until the file is replaced with one that has an acceptable license.
    - If any files have been removed from the repository, delete the corresponding license.
    - If any files have been moved or renamed, update the corresponding license to reflect the new path(s).
    - Check the copyright date and update it if the year has changed since the prior release. Update the date in the NOTICE.txt file, also.
  - Push the changes to the remote `lucenenet` repository (`https://gitbox.apache.org/repos/asf/lucenenet.gif`)

    ```powershell
    git push <remote> master
    ```

- Execute a complete test locally (it can take around 20 minutes, but you may do the next step in parallel):

  ```powershell
  build -pv <packageVersion> -t -mp 10
  ```

  > [!NOTE]
  > Adjust the `-mp` (maximum number of parallel jobs) number as appropriate based on the machine that runs them. In general, a powerful machine should be able to run 10 jobs or more and a weaker machine may only be able to run 7 or 8 jobs in parallel. It may take some experimentation to find the right number on your hardware to execute the tests fastest.

- Execute a complete test on a temporary Azure DevOps organization (it can take around 40-50 minutes) (see [build instructions on README.md](https://github.com/apache/lucenenet#azure-devops)).

## Successful Release Preparation

### Perform the Release Build

- Login to the [Lucene.NET build pipeline](https://dev.azure.com/lucene-net/Lucene.NET/_build?definitionId=3&_a=summary) on Azure DevOps

- Click the `Run pipeline` button

- Ensure the `master` branch is selected

- Expand `Variables`

- Update the `PackageVersion` variable to the release version number (i.e. `4.8.0-beta00008`)

- Click the back arrow to return to the main view

- Click the `Run` button to begin the build (it will take about 40 minutes)

### Check the Release Artifacts

- Upon successful Azure DevOps build, download the `release` build artifact and unzip it. Note you will need to copy the files in a later step.

Perform basic checks against the release binary:

- Check presence and appropriateness of LICENSE, NOTICE, and README files.

- Check the `nupkg` files to ensure they can be referenced in Visual Studio.

### Sign the Release

- On your local Git clone, check out the SVN distribution repositories

  ```powershell
  dotnet msbuild -t:CheckoutRelease
  ```

- From the `release` build artifact that you downloaded from Azure DevOps in the **Check the Release Artifacts** section, copy both the `.src.zip` and `.bin.zip` files to a new folder named `<repo root>/svn-dev/<packageVersion>/`

- On your local Git clone, tag the repository using the info in `RELEASE-TODO.txt`

  ```powershell
  git tag -a <tag from RELEASE-TODO.txt> <commit hash from RELEASE-TODO.txt> -m "<tag from RELEASE-TODO.txt>"
  git push <remote-name (defaults to origin)> master --tags
  ```

- [Sign the `release` artifacts](https://infra.apache.org/release-signing.html) using GnuPG

  ```powershell
  dotnet msbuild -t:SignReleaseCandidate -p:PackageVersion=<packageVersion>
  ```

  > [!NOTE]
  > You may be prompted for your password

- Check signature of generated artifacts (the `SignReleaseCandidate` target above runs the commands)

### Add Release Artifacts to the SVN `dev` Distribution Repository

  > [!WARNING]
  > The following command copies the `<repo root>/svn-release/KEYS` file to `<repo-root>/svn-dev/KEYS` and overwrites any local changes to it.

```powershell
dotnet msbuild -t:CommitReleaseCandidate -p:PackageVersion=<packageVersion>
```

### Create a VOTE Thread

Notify the developer mailing list of a new version vote. Be sure to replace all values in [] with the appropriate values. Use the [Countdown Timer Tool](https://www.timeanddate.com/countdown/create) to create a timer to show exactly when the vote ends.

Use the [Apache URL Shortener](https://s.apache.org/) to replace the URLs in the release vote email to ensure they are not broken using the following ID pattern:

- `<packageVersion>-tag`
- `<packageVersion>-notes`
- `<packageVersion>-test`
- `<packageVersion>-countdown`

> [!NOTE]
> Apache doesn't allow usage of 3rd party URL shortener services.

```txt
To: dev@lucenenet.apache.org
Message Subject: [VOTE] Apache Lucene.NET [version]

------------------------------------------------------

I have posted a new release for the Apache Lucene.NET [version] release and it is ready for testing.

The binaries can be downloaded from:
https://dist.apache.org/repos/dist/dev/lucenenet

The release was made from the Apache Lucene.NET [version] tag at:
https://github.com/apache/lucenenet/tree/[tag]

The release notes are listed at:
https://github.com/apache/lucenenet/releases/tag/[tag-url]

The tests for this release can be viewed at:
https://dev.azure.com/lucene-net/Lucene.NET/_build/results?buildId=<Build ID for Release>&view=ms.vss-test-web.build-test-results-tab

The release was made using the Lucene.NET release process, documented on the website:
https://lucenenet.apache.org/contributing/make-release.html

Please vote on releasing these packages as Apache Lucene.NET [version]. The vote is open for at least the next 72 hours, i.e. midnight UTC on [YYYY-MM-DD]
http://www.timeanddate.com/counters/customcounter.html?year=[YYYY]&month=[MM]&day=[DD]



Only votes from Lucene.NET PMC are binding, but everyone is welcome to check the release candidate and vote.
The vote passes if at least three binding +1 votes are cast.

[ ] +1 Release the packages as Apache Lucene.NET [VERSION]

[ ] -1 Do not release the packages because...

```

## After a Successful Vote

The vote is successful if at least 3 +1 votes are received from [Lucene.NET PMC members](https://people.apache.org/phonebook.html?ctte=lucenenet) after a minimum of 72 hours of sending the vote email. Acknowledge the voting results on the mailing list in the VOTE thread.

```txt
To: dev@lucenenet.apache.org
Message Subject: [RESULT] [VOTE] Apache Lucene.NET [version]

------------------------------------------------------

The vote has now closed. The results are:

Binding Votes:

+1 [TOTAL BINDING +1 VOTES]
-1 [TOTAL BINDING -1 VOTES]

The vote is ***successful/not successful***
```

> [!TIP]
> Due to [spam issues](https://issues.apache.org/jira/browse/INFRA-20098) you may want to bcc each person who voted on the RESULT email to ensure they receive it.

### Release to NuGet.org

> [!TIP]
> NuGet.org's API keys expire every year, so it would be good to check the NuGet.org API Key section to see whether the current API key is current and update it prior to attempting the steps below.

- Login to the [Lucene.NET release pipeline](https://dev.azure.com/lucene-net/Lucene.NET/_release?_a=releases&view=mine&definitionId=1) on Azure DevOps

- Click the release that corresponds to the version that is being released

- The `Release [VOTE]` step should be waiting for manual intervention, click the `Resume` button

- Enter the result of the vote in the following format, updating the values within `[ ]` appropriately

  ```
  Binding Votes +1: [3] 0: [0] -1: [0]
  Non Binding Votes +1: [3] 0: [0] -1: [0]
  ```

- Upon clicking `Resume` again the release will finish, submitting the NuGet packages to NuGet.org

### Release Binaries to SVN

Commit the distribution via SVN to [https://dist.apache.org/repos/dist/release](https://dist.apache.org/repos/dist/release):

```powershell
dotnet msbuild -t:CommitRelease -p:PackageVersion=<packageVersion>
```

> [!NOTE]
> If preferred or if the above command fails, this step can be done manually using Windows Explorer/TortoiseSVN by doing the following steps:
>
> - Copy `<repo root>/svn-dev/KEYS` to `<repo root>/svn-release/KEYS`
> - Copy `<repo root>/svn-dev/<packageVersion>` to `<repo root>/svn-release/<packageVersion>`
> - Right-click on `<repo root>/svn-release`, and click "SVN Commit..."
> - Add the commit message `Added Apache-Lucene.Net-<packageVersion> to release/lucenenet` and click OK
> - Right-click on `<repo root>/svn-dev/<packageVersion>` and click "TortoiseSVN > Delete"
> - Right-click on `<repo root>/svn-dev` and click "SVN Commit..."
> - Add the commit message `Removed Apache-Lucene.Net-<packageVersion> from dev/lucenenet` and click OK

### Archive Old Release(s)

To reduce the load on the ASF mirrors, projects are required to delete old releases (see http://www.apache.org/legal/release-policy.html#when-to-archive).

Remove the old releases from SVN under https://dist.apache.org/repos/dist/release/lucenenet/.

### Post-Release Steps

- Log the new version at https://reporter.apache.org/addrelease.html?lucenenet

- Publish the Draft [GitHub Release](https://github.com/apache/lucenenet/releases) that was [created earlier](#release-steps), updating the tag if necessary

- Update Website with new release

  - Update the `/websites/site/lucenetemplate/doap_Lucene_Net.rdf` file to reflect the new version and ensure other links/info in the file are correct.
    > [!IMPORTANT]
    > Only update the version if it's a new stable version.

  - Create a new release page in the `/websites/site/download`, in most cases it's easiest to just copy the previous release page.
    - Ensure the `uid` in the header is correct
    - Update all headers, status, release date to be correct
    - Ensure supported frameworks and packages section is accurate for the new release

  - Add the new release page to the `/websites/site/download/toc.yml` file

  - Add the new release version and release date to the `/websites/site/download/download.md` file

  - Follow the instructions on how to [build, test & publish the website](https://lucenenet.apache.org/contributing/documentation.html#website) and run/test the website locally.

  - Commit changes and [publish the website](https://lucenenet.apache.org/contributing/documentation.html#website).

- Update the API Documentation with new release

  - Follow the instructions on how to [build](https://lucenenet.apache.org/contributing/documentation.html#api-docs), test and [publish](https://lucenenet.apache.org/contributing/documentation.html#publishing-the-docs) the docs.

- Send announcement email 24 hours after the release (to ensure the mirrors have propagated the download locations)

  > [!IMPORTANT]
  > Only include announce@apache.org if it is a stable release version

  ```txt
  To: announce@apache.org; dev@lucenenet.apache.org; user@lucenenet.apache.org
  Message Subject: [ANNOUNCE] Apache Lucene.NET [version] Released

  ------------------------------------------------------

  The Apache Lucene.NET team is pleased to announce the release of version [version] of Apache Lucene.NET. Apache Lucene.NET is a .NET full-text search engine framework, a C# port of the popular Apache Lucene project. Apache Lucene.NET is not a complete application, but rather a code library and API that can easily be used to add search capabilities to applications.

  The Lucene.NET [version] binary and source distributions are available for download from our download page:
  https://lucenenet.apache.org/download/download.html

  The Lucene.NET library is distributed by NuGet.org as well. See the README.md page for more details:
  https://github.com/apache/lucenenet#all-packages-1

  Changes in this version:
  https://github.com/apache/lucenenet/releases/tag/[tag]

  The Apache Lucene.NET Team

  ```

## After an Unsuccessful Vote

The release vote may fail due to an issue discovered in the release candidate. If the vote fails the release should be canceled by:

- Sending an email to [dev@lucenenet.apache.org](mailto:dev@lucenenet.apache.org) on the VOTE thread notifying of the vote's cancellation.

A new release candidate can now be prepared. When complete, a new VOTE thread can be started as described in the steps above.

> [!NOTE]
> Due to the fact that an Azure Pipeline cannot be re-run with the same version number and users who reference `.nupkg` files will have them in their local `.nuget` cache, there are 2 choices:
>
> 1. To use the same version number that failed:
>   - Create a new Azure DevOps pipeline by copying the settings from the pipeline named Lucene.NET-Release.
>   - Test the Lucene.NET-Release pipeline thoroughly, using different version numbers than the one you are releasing.
>   - Create a release pipeline and copy the settings from the release pipeline named Lucene.NET-Release.   
>   - Delete the tag for this release and recreate it with the same version number at the appropriate commit.
>   - If the vote has already started, the new release vote email should provide instructions for removing the previous version from the `.nuget` cache to ensure the new release with the same version is the one being scrutinized.
> 2. To use a new version number, simply re-start the process from the beginning, copy the release notes from the previous version (both on GitHub and the website) and follow all of the remaining steps above. Discard the draft release notes on GitHub when complete.
