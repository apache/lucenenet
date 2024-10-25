---
uid: quick-start/tutorial
---

# Tutorial

---

## Let's Build a Search App!
Sometimes the best way to learn is just to see some working code. So that's what we are going to do. If you haven't read the [Introduction](xref:quick-start/introduction) page yet, do that first so that you have some context for understanding the code we are going to write.

Now let's build a simple console application that can index a few documents, search those documents, and return some results.  Actually, let's build two apps that do that.  The first example will show how to do exact match searches and the 2nd example will show how to do a full text search.  These example console applications will give you some working code that can serve as a great starting point for trying out various Lucene.NET features.

## Multi-Platform
It's worth mentioning that Lucene.NET runs everywhere that .NET runs. That means that Lucene.NET can be used in Windows and Unix applications, ASP.NET websites (Windows, Mac or Unix), iOS Apps, Android Apps and even on the Raspberry Pi.

## Why the .NET CLI?
In these examples we will use the .NET CLI (Command Line Interface) because it's a cross platform way to generate the project file we need and to add references to Nuget packages.  We will be using PowerShell to invoke the .NET CLI because PowerShell provides a command line environment that is also cross platform.

However you are totally free to use [Visual Studio](https://visualstudio.microsoft.com/) (Windows/Mac) or [Visual Studio Code](https://code.visualstudio.com/) (Windows/Unix/Max) to create the console application project and to add references to the Nuget packages. Whichever tool you use, you should end up with the same files and you can compare their contents to the contents that we show in the examples.

## Download and Install the .NET SDK
First you must install the .NET Core SDK, if it's not already installed on your machine. The .NET Core SDK contains the .NET runtime, .NET Libraries and the .NET CLI. If you haven't installed it yet, download it from https://dotnet.microsoft.com/en-us/download and run the installer. It's a pretty straightforward process. I'll be using the **.NET 6.0 SDK** in this tutorial.

> [!NOTE]
> The C# code we present **requires the .NET 6.0 SDK or later**.  However, with a few simple modifications it can run on older SDKs including 4.x. To do that, the Program.cs file will need to have a namespace, Program class and a static void main method.  See Microsoft docs [here](https://docs.microsoft.com/en-us/dotnet/core/tutorials/with-visual-studio?pivots=dotnet-5-0#code-try-3) for details.  You will also need to add [braces to the using statements](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/using-statement#example). 

## Download and Install PowerShell
PowerShell is cross platform and runs everywhere .NET runs, so we will be using PowerShell for all of our command line work. If you don't already have PowerShell installed you can download and find instructions for installing it on Window, Unix or Mac on this [Installing PowerShell](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell) page. In my examples I'm using PowerShell 7.2 but the specific version probably doesn't make a difference.

## Verify dotnet CLI Installed
Let's use PowerShell now to verify that you have the .NET SDK with the .NET CLI installed.  Launch PowerShell however you do that on your OS, for Windows I'll search for it in the start menu and select it from there. Once you have the PowerShell window open, execute the following command in PowerShell:

`dotnet --info`

This command will show the latest version of the .NET SDK installed and also show a list of all versions installed. If the .NET SDK is not installed this the command will return an error indicating the command was not found.

Below I show the top of the results for the `dotnet --info` command ran on my machine. You can see I'm using .NET SDK 6.0.200 on windows for this demo. In my case I had to scroll the screen up to see this info since I have many versions of the .NET SDK installed and it shows info on each version which scrolled the info about the latest version off the screen. Your latest version will likely be different than mine and perhaps you may be running on Unix or Mac. That's fine. But remember **you need .NET SDK 6 or later**. Or you need to modify the examples according to the note above.
 
<img src='https://lucenenet.apache.org/images/quick-start/tutorial/power-shell01.png'>

Now that our prerequisites are installed, we are ready to get started with our first example of using Lucene.NET.

## Example 1 - Step by Step
We are going to create a console application that uses Lucene.NET to index three documents that each have two fields and then the app will search those docs on a certain field doing an exact match search and output some info about the results.

This is actually pretty simple to do in Lucene.NET but since this in our very first Lucene.NET application we are going to walk through it step by step and provide a lot of explanation along the way.

### Create a Directory for the Project
Create a directory where you would like this project to live on your hard drive and call that directory `lucene-example1`. In my case that will be ` C:\Users\Ron\source\repos\lucene-example1` but you can chose any location you like.  Then make that directory the current directory in PowerShell.

In my case, since I'm on Windows, I'll create the directory using the GUI and use the `cd` command in PowerShell to change directory to the one I created.  So the exact PowerShell command I used was  `cd C:\Users\Ron\source\repos\lucene-example1` but you will need to modify that command to specify the directory you created.

<img src='https://lucenenet.apache.org/images/quick-start/tutorial/power-shell02.png'>

### Create the Project Files
To create a C# console application project in the current directory, type this command in PowerShell:

`dotnet new console`
 
<img src='https://lucenenet.apache.org/images/quick-start/tutorial/power-shell03.png'>

### Add NuGet References
We need to add references from our project to the Lucene.NET Nuget packages we need -- two separate packages in this case.  Execute the first command in PowerShell: (Please note there are two dashes before prerelease not one.)

`dotnet add package Lucene.Net --prerelease`

 
<img src='https://lucenenet.apache.org/images/quick-start/tutorial/power-shell04.png'>

And now add the 2nd Nuget package by executing this command in PowerShell:

`dotnet add package Lucene.Net.Analysis.Common --prerelease`

<img src='https://lucenenet.apache.org/images/quick-start/tutorial/power-shell05.png'>

At this point, our directory has two files in it plus an obj directory with some additional files.  We are mostly concerned with the lucene-example1.csproj project file and the Program.cs C# code file.

Our directory looks like this:
 
<img src='https://lucenenet.apache.org/images/quick-start/tutorial/directory-files-example1.png'>

### Viewing the Two Main files
From here on out, you can use your favorite editor to view and edit files as we walk through the rest of the example.  I'll be using Visual Studio 2022 on Windows, but you could just as easily use VIM, Visual Studio Code or any other editor and even be doing that on Ubuntu on a Raspberry Pi if you like. Remember, Lucene.NET and the .NET framework both support a wide variety of platforms.

Below is what the project file looks like which we created using the dotnet CLI. Notice that it contains package references to the two Lucene.NET Nuget packages we specified.
 
<img src='https://lucenenet.apache.org/images/quick-start/tutorial/example1.csproj.png'>

Here is that file's contents:

```
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <RootNamespace>lucene_example1</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Lucene.Net" Version="4.8.0-beta00016" />
    <PackageReference Include="Lucene.Net.Analysis.Common" Version="4.8.0-beta00016" />
  </ItemGroup>

</Project>
```


Now let's look at the `Program.cs` file that got generated.  It looks like:
 
<img src='https://lucenenet.apache.org/images/quick-start/tutorial/program01.png'>


### Running the Application
Before going further lets just run this console application and see that it generates the "Hello World!" output we expect.

If you are using Visual Studio or Visual Studio Code you can just hit F5 to run it. But what if are using a plain text editor to do your work?  No problem, we can run console application from PowerShell.  Just type this command in PowerShell:

`dotnet run`

This will run the project from the PowerShell current directory after it does a restore of the Nuget packages for the project.
 
<img src='https://lucenenet.apache.org/images/quick-start/tutorial/hello-world-example1.png'>

And there you go.  You can see in the window above that we get the output we expected.

### Writing Some Lucene.NET Code
Now use your editor to replace the existing code in the Program.cs with the following code that uses Lucene.NET:

```c#
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System.Diagnostics;
using LuceneDirectory = Lucene.Net.Store.Directory;

// Specify the compatibility version we want
const LuceneVersion luceneVersion = LuceneVersion.LUCENE_48; 

//Open the Directory using a Lucene Directory class
string indexName = "example_index";
string indexPath = Path.Combine(Environment.CurrentDirectory, indexName);

using LuceneDirectory indexDir = FSDirectory.Open(indexPath);

//Create an analyzer to process the text 
Analyzer standardAnalyzer = new StandardAnalyzer(luceneVersion);

//Create an index writer
IndexWriterConfig indexConfig = new IndexWriterConfig(luceneVersion, standardAnalyzer);
indexConfig.OpenMode = OpenMode.CREATE;                             // create/overwrite index
IndexWriter writer = new IndexWriter(indexDir, indexConfig);

//Add three documents to the index
Document doc = new Document();
doc.Add(new TextField("titleTag", "The Apache Software Foundation - The world's largest open source foundation.", Field.Store.YES));
doc.Add(new StringField("domain", "www.apache.org/", Field.Store.YES));
writer.AddDocument(doc);

doc = new Document();
doc.Add(new TextField("title", "Powerful open source search library for .NET", Field.Store.YES));
doc.Add(new StringField("domain", "lucenenet.apache.org", Field.Store.YES));
writer.AddDocument(doc);

doc = new Document();
doc.Add(new TextField("title", "Unique gifts made by small businesses in North Carolina.", Field.Store.YES));
doc.Add(new StringField("domain", "www.giftoasis.com", Field.Store.YES));
writer.AddDocument(doc);

//Flush and commit the index data to the directory
writer.Commit();

using DirectoryReader reader = writer.GetReader(applyAllDeletes: true);
IndexSearcher searcher = new IndexSearcher(reader);

Query query = new TermQuery(new Term("domain", "lucenenet.apache.org"));
TopDocs topDocs = searcher.Search(query, n: 2);         //indicate we want the first 2 results


int numMatchingDocs = topDocs.TotalHits;
Document resultDoc = searcher.Doc(topDocs.ScoreDocs[0].Doc);  //read back first doc from results (ie 0 offset)
string title = resultDoc.Get("title");

Console.WriteLine($"Matching results: {topDocs.TotalHits}");
Console.WriteLine($"Title of first result: {title}");
```

> [!WARNING]
> As mentioned earlier, if you are not running .NET 6.0 SDK or later you will need to modify the above code in the following two ways: 1) Program.cs file will need to have a namespace, Program class and a static void main method.  See Microsoft docs [here](https://docs.microsoft.com/en-us/dotnet/core/tutorials/with-visual-studio?pivots=dotnet-5-0#code-try-3) for details; and 2) you will need to add [braces to the using statements](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/using-statement#example). 

### Code Walkthrough
Before running the code let's talk about what it does.

The using declarations at the top of the file specify the various namespaces we are going to use. Then we have this block of code that basically specifies that our Lucene.NET index will be in a subdirectory called "example_index".

 ```c#
// Specify the compatibility version we want
const LuceneVersion luceneVersion = LuceneVersion.LUCENE_48; 

//Open the Directory using a Lucene Directory class
string indexName = "example_index";
string indexPath = Path.Combine(Environment.CurrentDirectory, indexName);

using LuceneDirectory indexDir = FSDirectory.Open(indexPath);
```

Then in the next block we create an `IndexWriter` that will use our `LuceneDirectory`. The `IndexWriter` is a important class in Lucene.NET and is used to write documents to the Index (among other things).

The `IndexWriter` will create our subdirectory for us since it doesn't yet exist and it will create the index since it also doesn't yet exist.  By using  `OpenMode.CREATE` we are telling Lucene.NET that we want to recreate the index if it already exists.  This works great for a demo like this since every time  the console app is ran we will be recreating our LuceneIndex which means we will get the same output each time.  

```c#
//Create an index writer
IndexWriterConfig indexConfig = new IndexWriterConfig(luceneVersion, standardAnalyzer);
indexConfig.OpenMode = OpenMode.CREATE;      //create/overwrite index
IndexWriter writer = new IndexWriter(indexDir, indexConfig);
```

Then in the next block we add three documents to the index.  In this example we happen to specify that each document has two fields: title and domain. A document however could have as many fields as we like.

We also specify here that title is a `TextField` which means that we want the field to support full text searches, and we specify domain as a `StringField` which means we want to do exact match searches against that field.

It's worth noting that the documents are buffered in RAM initially and are not written to the index in the `Directory` until we call `writer.Commit();`

```c#
//Add three documents to the index
Document doc = new Document();
doc.Add(new TextField("title", "The Apache Software Foundation - The world's largest open source foundation.", Field.Store.YES));
doc.Add(new StringField("domain", "www.apache.org/", Field.Store.YES));
writer.AddDocument(doc);

doc = new Document();
doc.Add(new TextField("title", "Powerful open source search library for .NET", Field.Store.YES));
doc.Add(new StringField("domain", "lucenenet.apache.org", Field.Store.YES));
writer.AddDocument(doc);

doc = new Document();
doc.Add(new TextField("title", "Unique gifts made by small businesses in North Carolina.", Field.Store.YES));
doc.Add(new StringField("domain", "www.giftoasis.com", Field.Store.YES));
writer.AddDocument(doc);

//Flush and commit the index data to the directory
writer.Commit();
```

So now our documents are in the index and we want to see how to read a document from that index. That is exactly what the following block of code does.

In the block of code below we search the index for all the documents that have a domain field value of "lucenenet.apache.org".

> [!NOTE]
> Note that in the block of code below we specify `applyAllDeletes: true` when getting a `DirectoryReader`. This means that uncommitted deleted documents will be applied to the reader we obtain.  If this value were false then only committed deletes would be applied to the reader. In our example we don't delete any documents but when getting a `DirectoryReader` we must still specify some value for this parameter.

We happen to specify that we want just the top 2 matching results from the search but based on the data in our example only one result matches and so only that one result will be returned.  The code then writes out to the console the number of matching documents and the title of the first (and in this case only) matching result.


```c#
using DirectoryReader reader = writer.GetReader(applyAllDeletes: true);
IndexSearcher searcher = new IndexSearcher(reader);

Query query = new TermQuery(new Term("domain", "lucenenet.apache.org"));
TopDocs topDocs = searcher.Search(query, n: 2);         //indicate we want the first 2 results


int numMatchingDocs = topDocs.TotalHits;
Document resultDoc = searcher.Doc(topDocs.ScoreDocs[0].Doc);  //read back first doc from results (ie 0 offset)
string title = resultDoc.Get("title");

Console.WriteLine($"Matching results: {topDocs.TotalHits}");
Console.WriteLine($"Title of first result: {title}");
```



### View of the Project.cs file with Our Code
The `Program.cs` file should now look something like this in your editor:
 
<img src='https://lucenenet.apache.org/images/quick-start/tutorial/example1-new-program.cs.png'>

### Run the Lucene.NET Code
So now you can hit F5 in Visual Studio or VS Code or you can execute `dotnet run` in PowerShell to see the code run and to see if it outputs what we expect.
 
<img src='https://lucenenet.apache.org/images/quick-start/tutorial/run-example1.png'>

And in the above screenshot we can see that the 2nd time we executed `dotnet run` (ie. after we modified the Program.cs file, out output says:

>Matching results: 1<br>
Title of first result: Powerful open source search library for .NET

This is exactly what we would expect.


### Conclusion - Example 1 
While this example is not particularly complicated, it will get you started. It provides fully working code that uses Lucne.NET that you now understand.

When looking at this code it's pretty easy to imagine how one might use a while loop instead of inline code for adding documents and how one could perhaps add 10,000 documents (or a million documents) instead of just three.  And it's pretty easy to imagine how one would add several fields per document rather then just two.

I would encourage you to play with this code, modify it (maybe by adding more fields, or changing the field name or field values) and then run it to see the results.  This iterative process is a great way to grow your knowledge of Lucene.NET.

Then move onto the next example that demonstrates full text search.

## Example 2 – Full Text Search
We are going to create a console application that uses Lucene.NET to index three documents that each have two fields and then the app will search those docs on a certain field doing an full text search and output some info about the results.

This example assumes you did Example 1 so:
1.	You already have the .NET SDK installed, 
2.	You already have PowerShell installed,
3.	You know how to create a C# console application project,
4.	You are familiar with the Example 1 code.

### Create the Project
Create a directory where you would like this project to live, call it `lucene-example2`. Then create a .NET console application project in that folder of the same name and add Nuget references to the following packages:

* Lucene.Net
* Lucene.Net.Analysis.Common
* Lucene.Net.QueryParser

You can use whatever tool you choose for Example 1 to accomplish these steps. In my case I will created the directory in the GUI then make it the current directory in PowerShell and then execute these commands in PowerShell one at at time (similar to how I did it in Example 1):

` dotnet new console`

`dotnet add package Lucene.Net --prerelease`

`dotnet add package Lucene.Net.Analysis.Common --prerelease`

`dotnet add package Lucene.Net.QueryParser --prerelease`


Technically the line above to `dotnet add package Lucene.Net --prerelease` is not needed because the `Lucene.Net.Analysis.Common` Nuget package has a dependency on the `Lucene.Net` Nuget package which means that when you execute this line `dotnet add package Lucene.Net.Analysis.Common --prerelease` it will automatically pull that dependency into the project too.  But since this is another introductory example I chose to add each Nuget package explicitly so that I'm not counting on one package being a dependency of the other.  Either way is fine.

### View the Project Files
Just like in the prior example the project folder will have two files and an obj directory with some files.  Now use your favorite editor to view the project's .proj file. It should look like this:
 
<img src='https://lucenenet.apache.org/images/quick-start/tutorial/example2.csproj.png'>

Here is that file's contents:

```
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <RootNamespace>lucene_example2</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Lucene.Net" Version="4.8.0-beta00016" />
    <PackageReference Include="Lucene.Net.Analysis.Common" Version="4.8.0-beta00016" />
    <PackageReference Include="Lucene.Net.QueryParser" Version="4.8.0-beta00016" />
  </ItemGroup>

</Project>

```

And the `Program.cs` file that got generated will look like this again:
 
<img src='https://lucenenet.apache.org/images/quick-start/tutorial/program02.png'>

### Run the App
If you are using Visual Studio or VS Code you can hit F5 to run the app.  I will execute `dotnet run` in PowerShell to run it:
 
<img src='https://lucenenet.apache.org/images/quick-start/tutorial/hello-world-example2.png'>

And we can see it output Hello World! Just as it did in Example 1.

### Writing Some Lucene.NET Code
Now use your editor to replace the existing code in the Program.cs with the following:

```c#
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System.Diagnostics;
using LuceneDirectory = Lucene.Net.Store.Directory;

// Specify the compatibility version we want
const LuceneVersion luceneVersion = LuceneVersion.LUCENE_48;

//Open the Directory using a Lucene Directory class
string indexName = "example_index";
string indexPath = Path.Combine(Environment.CurrentDirectory, indexName);

using LuceneDirectory indexDir = FSDirectory.Open(indexPath);

// Create an analyzer to process the text 
Analyzer standardAnalyzer = new StandardAnalyzer(luceneVersion);

//Create an index writer
IndexWriterConfig indexConfig = new IndexWriterConfig(luceneVersion, standardAnalyzer);
indexConfig.OpenMode = OpenMode.CREATE;                             // create/overwrite index
IndexWriter writer = new IndexWriter(indexDir, indexConfig);

//Add three documents to the index
Document doc = new Document();
doc.Add(new TextField("title", "The Apache Software Foundation - The world's largest open source foundation.", Field.Store.YES));
doc.Add(new StringField("domain", "www.apache.org", Field.Store.YES));
writer.AddDocument(doc);

doc = new Document();
doc.Add(new TextField("title", "Powerful open source search library for .NET", Field.Store.YES));
doc.Add(new StringField("domain", "lucenenet.apache.org", Field.Store.YES));
writer.AddDocument(doc);

doc = new Document();
doc.Add(new TextField("title", "Unique gifts made by small businesses in North Carolina.", Field.Store.YES));
doc.Add(new StringField("domain", "www.giftoasis.com", Field.Store.YES));
writer.AddDocument(doc);

//Flush and commit the index data to the directory
writer.Commit();

using DirectoryReader reader = writer.GetReader(applyAllDeletes: true);
IndexSearcher searcher = new IndexSearcher(reader);

QueryParser parser = new QueryParser(luceneVersion, "title", standardAnalyzer);
Query query = parser.Parse("open source");
TopDocs topDocs = searcher.Search(query, n: 3);         //indicate we want the first 3 results


Console.WriteLine($"Matching results: {topDocs.TotalHits}");

for (int i = 0; i < topDocs.TotalHits; i++)
{
    //read back a doc from results
    Document resultDoc = searcher.Doc(topDocs.ScoreDocs[i].Doc);

    string domain = resultDoc.Get("domain");
    Console.WriteLine($"Domain of result {i + 1}: {domain}");
}
```

> [!WARNING]
> As mentioned earlier, if you are not running .NET 6.0 SDK or later you will need to modify the above code in the following two ways: 1) Program.cs file will need to have a namespace, Program class and a static void main method.  See Microsoft docs [here](https://docs.microsoft.com/en-us/dotnet/core/tutorials/with-visual-studio?pivots=dotnet-5-0#code-try-3) for details; and 2) you will need to add [braces to the using statements](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/using-statement#example).

### Code Walkthrough
Before we run the code let's talk about what's different then the code in Example 1.

As you might guess we have an additional using declaration `using Lucene.Net.QueryParsers.Classic` related to the additional Nuget package we added.  But other than that the rest of the code at beginning and even middle of the code is just like what we already covered in Example1.

We are creating a `LuceneDirectory` and `IndexWriter` the same way and we are adding the same documents and then committing them.   All stuff we saw in Example1.  Also in this example we get our index reader and searcher the same way we did in the last example.

**But** the way we query back documents in this example is different. 

This time around, instead of using a `TermQuery` to do an exact match search, have these two lines of code:

```c#
QueryParser parser = new QueryParser(luceneVersion, "title", standardAnalyzer);
Query query = parser.Parse("open source");
```

These lines allow us to create a query that will perform a full text search. This type of search is similar to what you are use to when doing a google or bing search.

What we are saying in these two lines is that we want to create a query that will search the `title` field of our documents and we want back document that contain "open source" or just "open" or just "source" and we want them sorted by how well they match our "open source" query.  

So when the line of code below runs, Lucene.NET will score each of our docs that match the query and return the top 3 matching documents sorted by score.

```c#
TopDocs topDocs = searcher.Search(query, n: 3);         //indicate we want the first 3 results
```

In our case only two documents match and they will be returned in `topDocs`.  Then our final block of code just prints out the results.

```c#
Console.WriteLine($"Matching results: {topDocs.TotalHits}");

for (int i = 0; i < topDocs.TotalHits; i++)
{
    //read back a doc from results
    Document resultDoc = searcher.Doc(topDocs.ScoreDocs[i].Doc);

    string domain = resultDoc.Get("domain");
    Console.WriteLine($"Domain of result {i + 1}: {domain}");
}
```

### View of the Project.cs file with Our Code
The `Program.cs` file should now look something like this in your editor:
 
<img src='https://lucenenet.apache.org/images/quick-start/tutorial/example2-new-program.cs.png'>

### Run the Lucene.NET Code
So now you can hit F5 in Visual Studio or VS Code or you can execute `dotnet run` in PowerShell to see the code run and to see if it outputs what we expect.
 
<img src='https://lucenenet.apache.org/images/quick-start/tutorial/run-example2.png'>

If you go back and review the contents of the `title` field for each document you will see the output from running the code does indeed return the only two documents that that contain "open source" in the title field. 

### Conclusion - Example 2 
In this Example we saw Lucene.NET's full text search feature.  But we only scratched the surface.

It's the responsibility of the analyzer to tokenize the text and it's the tokens that are stored in the index as terms.  In our case we used the `StandardAnalyzer` which removes punctuation, lower cases the text so it's not case sensitive and removes stop words (common words like "a" "an" and "the").

But there are other analyzers we could choose.  For example the `EnglishAnalyzer` does everything the `StandardAnalyzer` does but also "stems" the terms via the Porter Stemming algorithm.  Without going into the details of what the stemmer does, it provides the ability for us to perform a search and match documents that contain other forms of the word we are searching on.

So for example if we used the `EnglishAnalyzer` both for indexing our documents and searching our documents then if we searched on "run" we could match documents that contained "run", "runs", and "running".  And not only that, Lucene.NET contains Analyzers for 100s of other languages besides English.

Based on what you just learned, I suspect you could find some fun ways to change the code in Example2 to further your experimenting and learning.  For example you could add other documents with different field values, or use a different Analyzer and see how the results change.

## Final Thoughts
Now that you have working code and have seen at least the basics of how to use Lucene.NET I would encourage you to play with the code and see what you can accomplish.  Depending on your skill level you might be able to read a tab delimited text file (which could for example be created via Excel) and build a Lucene.NET index from that data. Then search it.

I would also encourage you to review the [Learning Resources](xref:quick-start/learning-resources) page to get up to speed on all the places you can go to learn more about Lucene.NET.

Apache Lucene.NET is a powerful open source search library.  Have fun with it!
