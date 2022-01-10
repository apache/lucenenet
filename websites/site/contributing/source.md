# Source code

---

## Git repository

Apache Lucene.Net uses git as its source code management system.  More specifically, it use Apache's [two-master](https://git.apache.org/) setup with a master repo on [GitHub](https://github.com/apache/lucenenet) and on [GitBox](https://gitbox.apache.org/repos/asf?p=lucenenet.git). Either repo may be used for commits and pull requests as they automatically sync with one and other.

In practice, the team primarily uses the GitHub repo at **[https://github.com/apache/lucenenet](https://github.com/apache/lucenenet)** for it's work. You can find current issues that need worked on in the [issues list](https://github.com/apache/lucenenet/issues) there.

## Setting Up Your Fork
If you would like to contribute to the project, typically the first thing you will want to do is to [create a github account](https://docs.github.com/en/get-started/signing-up-for-github/signing-up-for-a-new-github-account) and then [fork](https://docs.github.com/en/get-started/quickstart/fork-a-repo) the apache/LuceneNET GitHub repo.  Forking the repo will place a copy of the repo (the "Fork") in your GitHub account. A fork is a copy of a repository that you manage.

You use this fork to make changes without affecting the upstream repository. For more information, see GitHub Docs "[Working with forks](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/working-with-forks)."


## Cloning that Fork
Next, you will typically clone that forked repo from your GitHub account so that you have a clone of it on your local machine to work with.  If you are unfamiliar with cloning a GitHub repo see GitHub Docs ["Cloning a repository"](https://docs.github.com/en/repositories/creating-and-managing-repositories/cloning-a-repository).  The git command for cloning the repo is:

```
$ git clone https://github.com/YOUR-GITHUB-USERNAME/lucenenet.git
```


At this point you have a local copy of the Lucene.NET repo on your development machine.  
Most work currently happens on the branch named **master**. But typically you will create a new branch on your local repo for the changes you'd like to make and you will ultimately do a pull request to get that branch merged back into master.  More about that later.

## Building & testing

It's totally possible to build the project and run the unit tests all from the command line.  You can find documentation for doing that in the Git repository **[here](https://github.com/apache/lucenenet/blob/master/README.md#building-and-testing)**.

However, it's common for developers to build and test the project using Visual Studio by opening the `Lucene.Net.sln` solution file located in the root of the local repo.  Some developers are currently using Visual Studio 2019 and some are using Visual Studio 2022. You may use whichever you prefer.

Once the solution has been opened in Visual Studio you can build it as you would any solution by selecting "Build Solution" from the "Build" menu. Likewise you can run the unit tests for the solution just like you would for any other solution by selecting "Run All Tests" from the "Tests" menu.

This is a large solution with more than [644K+ lines](https://lucenenet.apache.org/images/contributing/source/lucenenet-repo-lines-of-code--jan-2022.png) of code so it may take a bit longer for Visual Studio to perform these operations then you are use to but it should display progress information while it does it work.


## Making Changes

If you would like to make a change to the source code or other files, typically you will first make a new branch in your local repository. Then make the changes in that branch and commit them to your local repository.  If there are several different types of changes you'd like to make it's best to put each type of change into a seperate commit so that each commit description can be more specific.

## Contributing Your Changes Back
Once you have made your change to a newly created branch of your local repo, push those changes to the remote repository located in your GitHub account.  Then visit that your GitHub repo via the browser and it should display a button you can click to compare it to the upstream repo (ie. the apache/lucenenet rep you forked from) and to submit a pull request.  That pull request (PR) is your way of letting the Lucene.NET core team know you have a contribution that you would like to have merged into the official Lucene.NET repo.

Someone will get back to you with feedback if needed, or will directly merge your changes into the official repo.  Any action they take on the PR will trigger an email to the email address of your GitHub account so that you have visibility as to what's going on with your submission.

In addition to what has been written here, there are lots of blog post on the web about how to get started with Open Source like [this one](https://www.freecodecamp.org/news/how-to-contribute-to-open-source-projects-beginners-guide/) which provides a great orientation and top level overview.  



