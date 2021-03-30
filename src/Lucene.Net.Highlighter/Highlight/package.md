---
uid: Lucene.Net.Search.Highlight
summary: *content
---

<!--
 Licensed to the Apache Software Foundation (ASF) under one or more
 contributor license agreements.  See the NOTICE file distributed with
 this work for additional information regarding copyright ownership.
 The ASF licenses this file to You under the Apache License, Version 2.0
 (the "License"); you may not use this file except in compliance with
 the License.  You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
-->


The highlight package contains classes to provide "keyword in context" features
typically used to highlight search terms in the text of results pages.
The Highlighter class is the central component and can be used to extract the
most interesting sections of a piece of text and highlight them, with the help of
[Fragmenter](xref:Lucene.Net.Search.Highlight.IFragmenter), fragment [Scorer](xref:Lucene.Net.Search.Highlight.IScorer), and [Formatter](xref:Lucene.Net.Search.Highlight.IFormatter) classes.

## Example Usage

```cs
const LuceneVersion matchVersion = LuceneVersion.LUCENE_48;
Analyzer analyzer = new StandardAnalyzer(matchVersion);

// Create an index to search
string indexPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
Directory dir = FSDirectory.Open(indexPath);
using IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(matchVersion, analyzer));

// This field must store term vectors and term vector offsets
var fieldType = new FieldType(TextField.TYPE_STORED)
{
    StoreTermVectors = true,
    StoreTermVectorOffsets = true
};
fieldType.Freeze();

// Create documents with two fields, one with term vectors (tv) and one without (notv)
writer.AddDocument(new Document {
    new Field("tv", "Thanks a million!", fieldType),
    new TextField("notv", "A million ways to win.", Field.Store.YES)
});
writer.AddDocument(new Document {
    new Field("tv", "Hopefully, this won't highlight a million times.", fieldType),
    new TextField("notv", "There are a million different ways to do that!", Field.Store.YES)
});

using IndexReader indexReader = writer.GetReader(applyAllDeletes: true);
writer.Dispose();

// Now search our index using an existing or new IndexReader

IndexSearcher searcher = new IndexSearcher(indexReader);
QueryParser parser = new QueryParser(matchVersion, "notv", analyzer);
Query query = parser.Parse("million");

TopDocs hits = searcher.Search(query, 10);

SimpleHTMLFormatter htmlFormatter = new SimpleHTMLFormatter();
Highlighter highlighter = new Highlighter(htmlFormatter, new QueryScorer(query));
int totalScoreDocs = hits.ScoreDocs.Length > 10 ? 10 : hits.ScoreDocs.Length;
for (int i = 0; i < totalScoreDocs; i++)
{
    int id = hits.ScoreDocs[i].Doc;
    Document doc = searcher.Doc(id);
    string text = doc.Get("notv");
    TokenStream tokenStream = TokenSources.GetAnyTokenStream(searcher.IndexReader, id, "notv", analyzer);
    TextFragment[] frag = highlighter.GetBestTextFragments(
        tokenStream, text, mergeContiguousFragments: false, maxNumFragments: 10); // highlighter.GetBestFragments(tokenStream, text, 3, "...");
    for (int j = 0; j < frag.Length; j++)
    {
        if (frag[j] != null && frag[j].Score > 0)
        {
            Console.WriteLine(frag[j].ToString());
        }
    }
    //Term vector
    text = doc.Get("tv");
    tokenStream = TokenSources.GetAnyTokenStream(searcher.IndexReader, hits.ScoreDocs[i].Doc, "tv", analyzer);
    frag = highlighter.GetBestTextFragments(tokenStream, text, false, 10);
    for (int j = 0; j < frag.Length; j++)
    {
        if (frag[j] != null && frag[j].Score > 0)
        {
            Console.WriteLine(frag[j].ToString());
        }
    }
    Console.WriteLine("-------------");
}
```

## New features 2005-02-06


This release adds options for encoding (thanks to Nicko Cadell).
An "Encoder" implementation such as the new SimpleHTMLEncoder class can be passed to the highlighter to encode
all those non-xhtml standard characters such as & into legal values. This simple class may not suffice for
some languages -  Commons Lang has an implementation that could be used: escapeHtml(String) in
http://svn.apache.org/viewcvs.cgi/jakarta/commons/proper/lang/trunk/src/java/org/apache/commons/lang/StringEscapeUtils.java?rev=137958&view=markup

## New features 2004-12-22


This release adds some new capabilities:

1.  Faster highlighting using Term vector support

2.  New formatting options to use color intensity to show informational value

3.  Options for better summarization by using term IDF scores to influence fragment selection

The highlighter takes a <xref:Lucene.Net.Analysis.TokenStream> as input. Until now these streams have typically been produced using an <xref:Lucene.Net.Analysis.Analyzer> but the new class TokenSources provides helper methods for obtaining TokenStreams from the new TermVector position support (see latest CVS version).

The new class <xref:Lucene.Net.Search.Highlight.GradientFormatter> can use a scale of colors to highlight terms according to their score. A subtle use of color can help emphasize the reasons for matching (useful when doing "MoreLikeThis" queries and you want to see what the basis of the similarities are).

The <xref:Lucene.Net.Search.Highlight.QueryScorer> class has a new constructor which can use an <xref:Lucene.Net.Index.IndexReader> to derive the IDF (inverse document frequency) for each term in order to influence the score. This is useful for helping to extracting the most significant sections of a document and in supplying scores used by the new GradientFormatter to color significant words more strongly. The [QueryScorer.MaxTermWeight](xref:Lucene.Net.Search.Highlight.QueryScorer#Lucene_Net_Search_Highlight_QueryScorer_MaxTermWeight) method is useful when passed to the GradientFormatter constructor to define the top score which is associated with the top color.