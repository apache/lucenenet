---
uid: contributing/setup-java-debugging
---

# How to Setup Java Lucene 4.8 Debugging

---

## Introduction

Sometimes when porting Lucene 4.8 to Lucene.NET 4.8 it is helpful, or even necessary, to be able to watch Java Lucene 4.8 run in a development environment.  The goal of this document is to help walk you through the process of setting up such a development environment on Windows 10.

The Java Lucene 4.8 repository indicates that the following development environments are supported:

+ Eclipse - Basic support (help/IDEs.txt).
+ IntelliJ - IntelliJ idea can import the project out of the box.
+ Netbeans - Not tested.

In this document however, we will be using Eclipse because it's open source and widely used. Because Java Lucene 4.8 uses an old version of the Java JDK that has known security issues, the approach we take here is to setup a virtual machine vis VirtualBox to quarantine our use of the insecure JDK.


## Setting up VirtualBox

### Introduction and Background
We don't need to setup network access for VirtualBox for our needs and since the old JVM required to run Lucene 4.8 has security issues, it's safer not to give VirtualBox network access.  And since VirtualBox can run in a window, it means that when running it that way you will still have access to the internet and a browser on your main OS for doing coding research and such.

### Download Virtual Box
You can get the installer from [https://www.virtualbox.org/wiki/Downloads](https://www.virtualbox.org/wiki/Downloads) . On that page download the binary version for **Windows hosts**.

After downloading the installer, run it.

<img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box-install01.png'>

Then you will get the dialog below where you can specify the location where you want to install the VM on your machine.

<img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box-install02.png'>

Then you will get the dialog below where you can specify the location where you want to install the VM on your machine.  

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box-install03.png'>


Then as you click next you will eventually come to this dialog:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box-install04.png'>

You may get some security warnings. If so, click Install.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box-install05.png'>

Then when the install is done you will see a dialog similar to the one below.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box-install06.png'>


### Configuring VirtualBox
Clicking the Finished button in the prior dialog will launch VirtualBox, or you can launch it manually via the programs menu in Windows 10 as you would with any other "application."


 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box02.png'>

### Default Machine Folder
To change the location where the machine will be stored, click the Preferences icon in the main window or go to File menu and select Preferences. Then in the dialog that opens in the General tab you can change the Default Machine Folder by selecting Other… in the drop down as I have done here.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box03.png'>

### Create Windows 10 Installation Media
In this walkthrough we will be running Windows 10 in the VM.  Note that to do this one needs a 2nd Windows 10 license other then the one installed on the physical machine.  So two licenses are needed, one for the physical machine OS install and one for the VM OS install.  If you don't have a spare Windows 10 license to use in the VM you can consider installing ubuntu or other open source OS.  Next to run Windows 10 in the VM we much download and create Windows 10 installation media.  This can be done from https://www.microsoft.com/en-us/software-download/windows10 . Start by downloading the installation media tool.  

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box04.png'>

Then run the exe that is downloaded. You need to agree to the terms if they are acceptable to you.  Our goal here is to create an iso file that we can use to install Windows in the VM.  We don't need that iso file burned to a cd, just having it saved to the computer is fine.

In this dialog select "Create installation media"

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box05.png'>

On this dialog select "ISO file"

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box06.png'>

Then in the dialog that comes up pick a place on the computer to save the ISO file

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box07.png'>

And then it will begin downloading the file and displaying a progress indicator.  Once the file is downloaded, we have now have the ISO file we need for installing the OS inside of VirtualBox. 

Once that's done, you can click Finished in the dialog that comes up since we don't need to burn this ISO file to a dvd.  Having it on the hard drive is fine.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box08.png'>

### Creating a Virtual Machine
Click the New button in the main dialog of VirtualBox Manager.
Fill out the info in this dialog:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box09.png'><br>
 And this one:<br>
 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box10.png'><br>
 And this one:<br>
 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box11.png'><br>
 <br>
The default choice below is fine:<br>
<br>
 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box12.png'>

In the following dialog "Dynamically allocated" is fine.  Then in the dialog after that set the max size. 

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box13.png'>
 <br>
 Then it will look something like this:
 <br>
 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box14.png'>

 Back in settings click on storage, on the left, then click on Empty in the storage devices, this represents the CD rom.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box15.png'>

Now click the little disk drop down next to the Optical Drive label.  From that menu select "Chose a Disk File…" and select the windows ISO file you previously downloaded. 

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box16.png'>


Now it's like that Windows Install DVD is installed on our virtual computer. (See below)

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box17.png'>

Since by default the VM is setup to boot from the virtual optical drive (as well as the virtual hd) we can click start in the VirtualBox Manager to begin the Windows 10 Install into the VM.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box18.png'>

 Then select the startup disk:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/virtual-box19.png'>

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/windows-install01.png'>

Click next in the dialog above, then install now. Then in the window below key in your product key or click I don't have a product key.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/windows-install02.png'>

Click agree on the license terms if you agree.
Then in the dialog below click "Custom" since this is a new install not an upgrade.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/windows-install03.png'>

It will then show the unallocated space of the virtual drive we setup earlier. Select that as the place to install the OS and click next

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/windows-install04.png'>

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/windows-install05.png'>

 And then:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/windows-install06.png'>

Then go through all the screens for the standard Windows setup and after that you will probably see the screen below.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/windows-install07.png'>

And then finally we have windows running in the vm, and in the screenshot below I launched edge to show that networking is working in the VM too.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/windows-install08.png'>

## Installing Eclipse 4.6

Source of download https://wiki.eclipse.org/Older_Versions_Of_Eclipse. We want "Eclipse Neon Packages (2016 - v 4.6.0)"  This version goes by the name "neon r"  and it's page is here: https://www.eclipse.org/downloads/packages/release/neon/r 
 
We want the 2nd package below, "Eclipse IDE for Java Developers"  so download that, in my case the x86_64 link.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/eclipse01.png'>

Once that zip file downloads, then extract the contents into a folder.



## Installing Java 8

Lucene 4.8.0 uses Java 8 to run according to the build.xml file but Eclipse needs Java 8.  And Eclipse can use Java 8 to emulate Java 7 when running Lucene.
So we will need to install "**Java SE Development Kit 8u25**" into our VM which can be downloaded from https://www.oracle.com/java/technologies/javase/javase8-archive-downloads.html  You will notice the following warning on that page.  This is why we chose to work inside a VirtualBox.

> **WARNING**: These older versions of the JRE and JDK are provided to help developers debug issues in older systems. **They are not updated with the latest security patches and are not recommended for use in production.**

You will need to scroll down a ways on the page to find "**Java SE Development Kit 8u25**" or better yet search the page for 8u25. Then download the Windows x64 one jre-8u25-windows-x64.exe 
Download and run that jdk-8u25-windows-x64.exe file that in the VM.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-java8-01.png'>

I just took all the default options and had it do the install.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-java8-02.png'>

 And then:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-java8-03.png'>

 And then:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-java8-04.png'>



Eclipse will now run but we have more to install before can load Lucene 4.8.



## Installing Apache Ant

Use version 1.9.7 if you can find it, but 1.9.15 should work.  But you MUST the 1.9 branch of Apache Ant, not a newer one.  Ant is currently running two branches, 1.9.X and 1.10.X.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-apache-ant01.png'>

You can get version 1.9.7 here: https://archive.apache.org/dist/ant/binaries/ The actual download link for the zip version from that page is https://archive.apache.org/dist/ant/binaries/apache-ant-1.9.7-bin.zip

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-apache-ant02.png'>

You'll have to unzip the downloaded `apache-ant-1.9.7-bin.zip` file.  

The unzipped folder will need to be moved to a place of your choosing where you want ant to live.  In my case I created a folder called "Apache Software Foundation" inside the "Program Files" folder and placed it there.  So my path was `c:\Program Files\Apache Software Foundation\apache-ant-1.9.7` Directions for installing Ant are on this page under "The Short Story" https://ant.apache.org/manual/install.html 

I typed "Environment Variables" into windows search and used that to open the System Properties window. Then clicked the Environment Variables button.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-apache-ant03.png'>

 And then:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-apache-ant04.png'>


In here we can set the Environment Variables we need.
See this page for more details of what we need to do: 
https://ant.apache.org/manual/install.html#setup 

From that Page:
1.	Add the bin directory to your path.
2.	Set the ANT_HOME environment variable to the directory where you installed Ant. On some operating systems, Ant's startup scripts can guess ANT_HOME (Unix dialects and Windows NT descendants), but it is better to not rely on this behavior.
3.	Optionally, set the JAVA_HOME environment variable (see the Advanced section below). This should be set to the directory where your JDK is installed.

Adding the Bin directory to my path environment variable:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-apache-ant05.png'>

 And then:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-apache-ant06.png'>

Then adding the ANT_HOME environment variable

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-apache-ant07.png'>



Then adding the JAVA_HOME environment variable

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-apache-ant08.png'>



## Installing Apache Maven

Use version 3.8.1.  https://maven.apache.org/download.cgi The installation process is basically the same as Apache Ant.  This is the Java equivalent of NuGet.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-apache-maven01.png'>

 Then scrolling down the page:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-apache-maven02.png'>

We are going to download `apache-maven-3.8.1-bin.zip` 
Move the folder out of the zip and into the location by the apache ant folder.

"install" instructions are here: https://maven.apache.org/install.html 

Add the `bin` directory of  `apache-maven-3.8.1 to` the `PATH` environment variable

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-apache-maven03.png'>


Then open a command prompt via cmd.exe and type  mvn –v to confirm that the path is setup correct.  Output should look something like:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-apache-maven04.png'>

 
## Installing Git 

https://git-scm.com/download/win

  <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-git01.png'>

Of the choices above I chose "64-bit Git for Windows Setup."  Download and run the installer. Agree to the license, pick a directory to install it in, I kept the default, I also kept the default components in the dialog below:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-git02.png'>

 Next:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-git03.png'>

In the dialog avove I ched the default Git editor from vim to Notepad.  Pick whatever you like.  In general for the other dialogs I kept the defaults including on this one

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-git04.png'>

Next:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/install-git05.png'>

## Clone Lucene 4.8 Repo to VM Drive

Shad says: The top level thing in Java Eclipse is called a workspace. I haven't really researched exactly what a workspace is, but it can have more than one java project in it. When you clone the repo, you will put it in your repos folder. But there needs to be another folder for Java to consider a workspace. Maybe in reality it can be the same folder, but I haven't worked out how to do that.

So create two folders one for the cloned repo and one for the Eclipse workspace.  In my case I created these directories to satisfy that:

C:\Users\Ron\source\eclipse_workspaces\lucene_workspace
C:\Users\Ron\source\repos\lucene

### Why not clone the code directly from Java Lucene Repo?
It turns out that it's no longer possible to directly compile the Lucene 4.8 code obtained from the Java Lucene Repo without modification.  Unlike NuGet which makes old versions available forever and is always online, Maven has lots of mirrors that may cease to exist at some point, and that is what's happed, so the configuration in the Java Lucene 4.8 Repo is out of date and no longer builds.

In addition, Java 8 (which we need for Eclipse) detects an error in the code that Java 7 did not, so the build doesn't complete. It is due to some fields that are marked final that are disposed at the end of the constructor (so they really don't need to be fields). Taking the final keyword off of the field removes the error. So, the project has to be modified slightly so all of this can happen. 

### Where can we get Java Lucene 4.8 code that compiles?
There is a fork of the Java Lucene Repo at https://github.com/NightOwl888/lucene that includes modifications to update the Maven and ant files so that the code will still compile.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/where-can-we-get-the-code01.png'>

Steps to Clone it into the VM

Open the folder that will contain the clone folder and then right click in there and select "Git Bash Here" from the content menu.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/where-can-we-get-the-code02.png'>

Then in the Bash window type the following (where lucene-4.8.0 is the name we want it to use for the destination folder):

`git clone https://github.com/NightOwl888/lucene.git lucene-4.8.0`

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/where-can-we-get-the-code03.png'>

This process will take several minutes because the repo it over 1GB in size.  Here is a view of the Bash window in the middle of the clone operations:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/where-can-we-get-the-code04.png'>

Once that completes Lets checkout the branch, in the Bash window type:

`cd lucene-4.8.0`

followed by 

`git checkout releases/lucene-solr/4.8.0/update`

You can see the name of the branches here:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/where-can-we-get-the-code05.png'>

Here's what I looked like when the clone was done;

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/where-can-we-get-the-code06.png'>

 Now cd into the directory that was created as part of the clone operation by typing the following Base command:  cd lucene-4.8.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/where-can-we-get-the-code07.png'>

In windows, this is what the directory looks like:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/where-can-we-get-the-code08.png'>


## Downloading the Project's Dependencies

### Boostrap ant
First we need to get ant bootstrapped.
Here is some background on Maven and Ivy from the web:
> Apache Maven is a software project management and comprehension tool, whereas Apache Ivy is only a dependency management tool, highly integrated with Apache Ant™, the popular build management tool. Source: https://ant.apache.org/ivy/m2comparison.html 

From inside of the lucene-4.8.0 directory, run this Bash command

`ant ivy-bootstrap`

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/download-dependencies01.png'>

What it just did is downloaded the ~/.ant/lib/ivy-2.3.0 file


### Use Ant to Download Project Dependencies

Apache Lucene has a ton of 3rd party dependencies. The next step is to have maven download all those dependencies.  This can take a while depending on your machine and internet connections.  
To see a list of the dependencies look at **lucene/ivy-versions.properties** file.

We should now be setup to build.  So run the following Bash command:

`ant eclipse`

Example screenshot while ant is doing it's work:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/download-dependencies02.png'>

In my case it took about 10 minutes for ant to download all the dependencies.  The bash window then looked like this:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/download-dependencies03.png'>

## Using Eclipse

### Getting Stared with Eclipse
Now we need to open eclipse and create a workspace.  So we use windows to go to the eclipse folder which in my case is here C:\Program Files\eclipse and we double click on eclipse to run it.
Eclipse will prompt you to create a workspace.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/using-eclipse01.png'>

Browse to find the location we created earlier to house our lucene workspace folders and create a folder in there for this version of lucene.  I called my lucene4.8.0_workspace  as you can see in the screenshot below.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/using-eclipse02.png'>

And after a few more seconds Eclipse will open in a window:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/using-eclipse03.png'>


We can zoom to take up the whole VM screen space.

Then click the **Create new Java project** in the welcome screen.

1)	Provide a Project name, in my case Lucene4.8.0,  
2)	Then uncheck the "Use default location" check box.  
3)	Then use the browse button to specify the **location of the git repo clone.**  

So the dialog will then look something like this: 

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/using-eclipse04.png'>

Click the Next button.  

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/using-eclipse05.png'>

Then click the Libraries Tab and scroll down and select the JRE System Library

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/using-eclipse06.png'>

Then click the Edit button.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/using-eclipse07.png'>

Then set the System Library radio button to "Workspace default JRE" if it's not selected already.  Then clicked the Finish button on that dialog, and then click the Finish button on the other dialog.

Then expand the window to take up the whole VM and you will see a build progress indicator in the lower status bar.  The project is now building in the background.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/using-eclipse08.png'>

Zoomed in to see the Building workspace message better in the status bar

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/using-eclipse09.png'>

Once the build complete my screen looked like the screenshot below.  There were a lot of code warnings but no errors.

### Debugging With Eclipse

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/debugging-with-eclipse01.png'>

Now Click the little icon to expand the packages under Lucne4.8.0 in the package explorer

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/debugging-with-eclipse02.png'>

Then scroll down to **lucene/analysis/phonetic/src/test** and select it

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/debugging-with-eclipse03.png'>

Then right click it and select Run As Junit Test

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/debugging-with-eclipse04.png'>

 Then

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/debugging-with-eclipse05.png'>

The above screen shows it ran the 35 tests in 32.196 seconds and all of them passed. Yea.

Click on Package Explorer tab (by the Junit tab) and expand the phonetics/src/test item.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/debugging-with-eclipse06.png'>

Double click the line highlighted above and it will open the code for that test.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/debugging-with-eclipse07.png'>

Then double click on line 108 and it will set a breakpoint.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/debugging-with-eclipse08.png'>

Now right-click on testEmptyTerm (make sure it is still highlighted) and click Debug As > JUnit Test

You will get this dialog:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/debugging-with-eclipse09.png'>

Eclipse has a different "perspective" for debugging than for browsing projects, similar to in VisualStudo. However, it gives you a choice whether you want to use it or not.  Click yes, but don't choose to remember the setting, because it's not clear where to find it again.

And it will launch into the debugger layout and be waiting on the breakpoint.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/debugging-with-eclipse10.png'>

If you hover the debugger icons you will see that

Resume is F8<br>
Step Into is F5<br>
Step Over is F6<br>
Step Return is F7<br>

So it works much like VisualStudio but the F key configuration is different.  You can of course just click the icons at the top of the window, or goggle and there is probably a way to change the F key configuration for the debugger actions.

<font style="font-weight:bold; font-size: 17px">There ya go.   You are now in Eclipse, and able to run the debugger for Lucene 4.8 Java code.</font>

<font style="font-weight:bold; font-size: 20px">How cool is that?!</font>

 You're welcome, and a big shout out to NightOwl888 who blazed this trail for us all.


## Two More Helpful Tips

### Cloning the Local Repo
It is sometimes useful to create a clone in another versioned directory and then checkout the corresponding branch.

Basically, it is cloning the whole repo again in the subdirectory lucene-4.8.1. Then to match that version number, we checkout the branch that matches it. Now, if we were doing that from the original lucene repository we would typically checkout the tag that corresponds to the release rather than a versioned branch. But since the maven dependencies are all broken, we have our own branches that patches the release that we have ported changes from.

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/clone-local-repo01.png'>


Zommed in a bit more:

 <img src='https://lucenenet.apache.org/images/contributing/java-lucene-4_8-setup/clone-local-repo02.png'>


When you clone the repository locally, it only shows the default branch that you have pulled (unless you use the switch to pull all of them). But Git keeps track of the remote branches as well as the local ones on your local copy (that is, all of the remote branches that you have called git fetch or git pull on). 

"git branch -r" shows a list of all of the remote branches that you have pulled.  "git checkout releases/lucene-solr/4.8.1/updated" command is a shortcut for creating a branch based off of the remote. It won't work if you have 2 remotes that have a branch with the same name - in that case you would need to specify the remote name, too. https://stackoverflow.com/questions/24301914/how-to-create-a-local-branch-from-an-existing-remote-branch   Of course, all of the posts on that SO question assume a single folder that is set up to checkout multiple remote branches. We are creating separate clones so it is clearer what version we have in Eclipse and so we don't accidentally break something by switching between git branches.


### Disable Java Update Checker
One more thing you might want to do is to disable the Java update check so it doesn't accidentally get rid of Java 8.  See: https://thegeekpage.com/turn-off-java-update-notification-in-windows-10/

