---
title: Welcome to the Lucene.Net website!
description: Lucene.Net is a port of the Lucene search engine library, written in C# and targeted at .NET runtime users.
documentType: index
---

Lucene.NET
===============

<section id="quick-start" class="home-section">
<div class="container">
<div class="row">
<div class="col-xs-12 col-md-6 no-padding">
<p class="no-padding text-center">Create an index and define a text analyzer</p>
<pre class="clean">
<code class="csharp">// Ensures index backwards compatibility
var AppLuceneVersion = LuceneVersion.LUCENE_48;

var indexLocation = @"C:\Index";
var dir = FSDirectory.Open(indexLocation);

//create an analyzer to process the text
var analyzer = new StandardAnalyzer(AppLuceneVersion);

//create an index writer
var indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);
var writer = new IndexWriter(dir, indexConfig);
</code>
</pre>
</div>
<div class="col-xs-12 col-md-6">
<p class="no-padding text-center">Add to the index</p>
<pre class="clean">
<code class="csharp">var source = new
{
    Name = "Kermit the Frog",
    FavouritePhrase = "The quick brown fox jumps over the lazy dog"
};
var doc = new Document();
// StringField indexes but doesn't tokenise
doc.Add(new StringField("name", source.Name, Field.Store.YES));

doc.Add(new TextField("favouritePhrase", source.FavouritePhrase, Field.Store.YES));

writer.AddDocument(doc);
writer.Flush(triggerMerge: false, applyAllDeletes: false);
</code>
</div>
</div>
<div class="row">
<div class="col-xs-12 col-md-6">
<p class="no-padding text-center">Construct a query</p>
<pre class="clean"><code class="csharp">// search with a phrase
var phrase = new MultiPhraseQuery();
phrase.Add(new Term("favouritePhrase", "brown"));
phrase.Add(new Term("favouritePhrase", "fox"));
</code>
</pre>
</div>                    
<div class="col-xs-12 col-md-6">
<p class="no-padding text-center">Fetch the results</p>
<pre class="clean">
<code class="csharp">// re-use the writer to get real-time updates
var searcher = new IndexSearcher(writer.GetReader(applyAllDeletes: true));
var hits = searcher.Search(phrase, 20 /* top 20 */).ScoreDocs;
foreach (var hit in hits)
{
&nbsp;&nbsp;&nbsp;&nbsp;var foundDoc = searcher.Doc(hit.Doc);
&nbsp;&nbsp;&nbsp;&nbsp;hit.Score.Dump("Score");
&nbsp;&nbsp;&nbsp;&nbsp;foundDoc.Get("name").Dump("Name");
&nbsp;&nbsp;&nbsp;&nbsp;foundDoc.Get("favouritePhrase").Dump("Favourite Phrase");
}
</code>
</pre>
</div>
</div>
</div>
</section>

<section id="about" class="home-section">
    <div class="container">
        <div class="row">
            <h2 class="text-center">About the project</h2>
            <p>
            Lucene.Net is a port of the Lucene search engine library, written in C# and targeted at .NET runtime users. The Lucene search library is based on an <a href="http://lucene.sourceforge.net/talks/pisa/" target="_blank">inverted index</a>. 
            </p>
            <h3>Our Goals</h3>
            <ul>
                <li><p>Maintain the existing line-by-line port from Java to C#, fully automating and commoditizing the process such that the project can easily synchronize with the Java Lucene release scheduleM</p></li>
                <li><p>Maintaining the high-performance requirements expected of a first class C# search engine library</p></li>
                <li><p>Maximize usability and power when used within the .NET runtime. To that end, it will present a highly idiomatic, carefully tailored API that takes advantage of many of the special features of the .NET runtime</p></li>
            </ul>
        </div>        
    </div>
</div>

<section class="home-section">
    <div class="container">
        <div class="row">
        </div>
    </div>
</div>