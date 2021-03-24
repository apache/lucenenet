---
uid: Lucene.Net.Analysis
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

API and code to convert text into indexable/searchable tokens. Covers <xref:Lucene.Net.Analysis.Analyzer> and related classes.

## Parsing? Tokenization? Analysis!

Lucene.NET, an indexing and search library, accepts only plain text input.

## Parsing

Applications that build their search capabilities upon Lucene.NET may support documents in various formats – HTML, XML, PDF, Word – just to name a few.
Lucene.NET does not care about the _Parsing_ of these and other document formats, and it is the responsibility of the 
application using Lucene.NET to use an appropriate _Parser_ to convert the original format into plain text before passing that plain text to Lucene.NET.

## Tokenization

Plain text passed to Lucene.NET for indexing goes through a process generally called tokenization. Tokenization is the process
of breaking input text into small indexing elements – tokens.
The way input text is broken into tokens heavily influences how people will then be able to search for that text. 
For instance, sentences beginnings and endings can be identified to provide for more accurate phrase 
and proximity searches (though sentence identification is not provided by Lucene.NET).

 In some cases simply breaking the input text into tokens is not enough – a deeper _Analysis_ may be needed. Lucene includes both pre- and post-tokenization analysis facilities. 

 Pre-tokenization analysis can include (but is not limited to) stripping HTML markup, and transforming or removing text matching arbitrary patterns or sets of fixed strings. 

 There are many post-tokenization steps that can be done, including (but not limited to): 

*   [Stemming](http://en.wikipedia.org/wiki/Stemming) – 
      Replacing words with their stems. 
      For instance with English stemming "bikes" is replaced with "bike"; 
      now query "bike" can find both documents containing "bike" and those containing "bikes".

*   [Stop Words Filtering](http://en.wikipedia.org/wiki/Stop_words) – 
      Common words like "the", "and" and "a" rarely add any value to a search.
      Removing them shrinks the index size and increases performance.
      It may also reduce some "noise" and actually improve search quality.

*   [Text Normalization](http://en.wikipedia.org/wiki/Text_normalization) – 
      Stripping accents and other character markings can make for better searching.

*   [Synonym Expansion](http://en.wikipedia.org/wiki/Synonym) – 
      Adding in synonyms at the same token position as the current word can mean better 
      matching when users search with words in the synonym set.

## Core Analysis

 The analysis package provides the mechanism to convert Strings and TextReaders into tokens that can be indexed by Lucene.NET. There are four main classes in the package from which all analysis processes are derived. These are: 

*   <xref:Lucene.Net.Analysis.Analyzer> – An Analyzer is 
    responsible for building a 
    <xref:Lucene.Net.Analysis.TokenStream> which can be consumed
    by the indexing and searching processes.  See below for more information
    on implementing your own Analyzer.

*   <xref:Lucene.Net.Analysis.CharFilter> – CharFilter extends
    [System.Text.TextReader](https://docs.microsoft.com/en-us/dotnet/api/system.io.textreader) to perform pre-tokenization substitutions, 
    deletions, and/or insertions on an input TextReader's text, while providing
    corrected character offsets to account for these modifications.  This
    capability allows highlighting to function over the original text when 
    indexed tokens are created from CharFilter-modified text with offsets
    that are not the same as those in the original text. Tokenizers'
    constructors and Reset() methods accept a CharFilter.  CharFilters may
    be chained to perform multiple pre-tokenization modifications.

*   <xref:Lucene.Net.Analysis.Tokenizer> – A Tokenizer is a 
    <xref:Lucene.Net.Analysis.TokenStream> and is responsible for
    breaking up incoming text into tokens. In most cases, an Analyzer will
    use a Tokenizer as the first step in the analysis process.  However,
    to modify text prior to tokenization, use a CharStream subclass (see
    above).

*   <xref:Lucene.Net.Analysis.TokenFilter> – A TokenFilter is
    also a <xref:Lucene.Net.Analysis.TokenStream> and is responsible
    for modifying tokens that have been created by the Tokenizer.  Common 
    modifications performed by a TokenFilter are: deletion, stemming, synonym 
    injection, and down casing.  Not all Analyzers require TokenFilters.

## Hints, Tips and Traps

 The synergy between <xref:Lucene.Net.Analysis.Analyzer> and <xref:Lucene.Net.Analysis.Tokenizer> is sometimes confusing. To ease this confusion, some clarifications: 

*   The <xref:Lucene.Net.Analysis.Analyzer> is responsible for the entire task of 
    <u>creating</u> tokens out of the input text, while the <xref:Lucene.Net.Analysis.Tokenizer>
    is only responsible for <u>breaking</u> the input text into tokens. Very likely, tokens created 
    by the <xref:Lucene.Net.Analysis.Tokenizer> would be modified or even omitted 
    by the <xref:Lucene.Net.Analysis.Analyzer> (via one or more
    <xref:Lucene.Net.Analysis.TokenFilter>s) before being returned.

*   <xref:Lucene.Net.Analysis.Tokenizer> is a <xref:Lucene.Net.Analysis.TokenStream>, 
    but <xref:Lucene.Net.Analysis.Analyzer> is not.

*   <xref:Lucene.Net.Analysis.Analyzer> is "field aware", but 
    <xref:Lucene.Net.Analysis.Tokenizer> is not.

 Lucene.NET provides a number of analysis capabilities, the most commonly used one being the StandardAnalyzer. Many applications will have a long and industrious life with nothing more than the StandardAnalyzer. However, there are a few other classes/packages that are worth mentioning: 

1.  PerFieldAnalyzerWrapper – Most Analyzers perform the same operation on all
    <xref:Lucene.Net.Documents.Field>s.  The PerFieldAnalyzerWrapper can be used to associate a different Analyzer with different
    <xref:Lucene.Net.Documents.Field>s.

2.  The analysis library located at the root of the Lucene distribution has a number of different Analyzer implementations to solve a variety
    of different problems related to searching.  Many of the Analyzers are designed to analyze non-English languages.

3.  There are a variety of Tokenizer and TokenFilter implementations in this package.  Take a look around, chances are someone has implemented what you need.

 Analysis is one of the main causes of performance degradation during indexing. Simply put, the more you analyze the slower the indexing (in most cases). Perhaps your application would be just fine using the simple WhitespaceTokenizer combined with a StopFilter. The benchmark/ library can be useful for testing out the speed of the analysis process. 

## Invoking the Analyzer

 Applications usually do not invoke analysis – Lucene.NET does it for them: 

*   At indexing, as a consequence of 
    [AddDocument()](xref:Lucene.Net.Index.IndexWriter#Lucene_Net_Index_IndexWriter_AddDocument_System_Collections_Generic_IEnumerable_Lucene_Net_Index_IIndexableField__),
    the Analyzer in effect for indexing is invoked for each indexed field of the added document.

*   At search, a QueryParser may invoke the Analyzer during parsing.  Note that for some queries, analysis does not
    take place, e.g. wildcard queries.

 However an application might invoke Analysis of any text for testing or for any other purpose, something like:

<a id="analysis-workflow"></a>

```cs
LuceneVersion matchVersion = LuceneVersion.LUCENE_XY; // Substitute desired Lucene version for XY
Analyzer analyzer = new StandardAnalyzer(matchVersion); // or any other analyzer
TokenStream ts = analyzer.GetTokenStream("myfield", new StringReader("some text goes here"));
IOffsetAttribute offsetAtt = ts.AddAttribute<IOffsetAttribute>();

try
{
    ts.Reset(); // Resets this stream to the beginning. (Required)
    while (ts.IncrementToken())
    {
        // Use [ReflectAsString(bool)](xref:Lucene.Net.Util.AttributeSource#Lucene_Net_Util_AttributeSource_ReflectAsString_System_Boolean_)
        // for token stream debugging.
        Console.WriteLine("token: " + ts.ReflectAsString(true));

        Console.WriteLine("token start offset: " + offsetAtt.StartOffset);
        Console.WriteLine("  token end offset: " + offsetAtt.EndOffset);
    }
    ts.End();   // Perform end-of-stream operations, e.g. set the final offset.
}
finally
{
    ts.Dispose(); // Release resources associated with this stream.
}
```

## Indexing Analysis vs. Search Analysis

 Selecting the "correct" analyzer is crucial for search quality, and can also affect indexing and search performance. The "correct" analyzer differs between applications. Lucene java's wiki page [AnalysisParalysis](http://wiki.apache.org/lucene-java/AnalysisParalysis) provides some data on "analyzing your analyzer". Here are some rules of thumb:

1. Test test test... (did we say test?)
2. Beware of over analysis – might hurt indexing performance.
3. Start with same analyzer for indexing and search, otherwise searches would not find what they are supposed to...
4. In some cases a different analyzer is required for indexing and search, for instance:
  * Certain searches require more stop words to be filtered. (I.e. more than those that were filtered at indexing.)
  * Query expansion by synonyms, acronyms, auto spell correction, etc.
  This might sometimes require a modified analyzer – see the next section on how to do that. 

## Implementing your own Analyzer

 Creating your own Analyzer is straightforward. Your Analyzer can wrap existing analysis components — CharFilter(s) _(optional)_, a Tokenizer, and TokenFilter(s) _(optional)_ — or components you create, or a combination of existing and newly created components. Before pursuing this approach, you may find it worthwhile to explore the [Lucene.Net.Analysis.Common](xref:Lucene.Net.Analysis.Common) library and/or ask on the [user@lucenenet.apache.org mailing list](https://lucenenet.apache.org/contributing/mailing-lists.html#users-user) first to see if what you need already exists. If you are still committed to creating your own Analyzer, have a look at the source code of any one of the many samples located in this package. 

 The following sections discuss some aspects of implementing your own analyzer. 

### Field Section Boundaries

 When [Document.Add()](xref:Lucene.Net.Documents.Document#Lucene_Net_Documents_Document_Add_Lucene_Net_Index_IIndexableField_) is called multiple times for the same field name, we could say that each such call creates a new section for that field in that document. In fact, a separate call to [GetTokenStream()](xref:Lucene.Net.Analysis.Analyzer#Lucene_Net_Analysis_Analyzer_GetTokenStream_System_String_System_IO_TextReader_) would take place for each of these so called "sections". However, the default Analyzer behavior is to treat all these sections as one large section. This allows phrase search and proximity search to seamlessly cross boundaries between these "sections". In other words, if a certain field "f" is added like this: 

```cs
document.Add(new Field("f","first ends",...);
document.Add(new Field("f","starts two",...);
indexWriter.AddDocument(document);
```

 Then, a phrase search for "ends starts" would find that document. Where desired, this behavior can be modified by introducing a "position gap" between consecutive field "sections", by overriding [Analyzer.GetPositionIncrementGap(string)](xref:Lucene.Net.Analysis.Analyzer#Lucene_Net_Analysis_Analyzer_GetPositionIncrementGap_System_String_): 


```cs
/// <summary>
/// Similar to <see cref="StandardAnalyzer"/>, but has a different position increment gap.
/// </summary>
public sealed class PositionIncrementGapAnalyzer : StopwordAnalyzerBase
{
    public PositionIncrementGapAnalyzer(LuceneVersion matchVersion)
        : base(matchVersion, StopAnalyzer.ENGLISH_STOP_WORDS_SET)
    {
    }

    public int MaxTokenLength { get; set; } = 255;

    /// <summary>
    /// Returns a fixed position increment gap of 10.
    /// </summary>
    public override int GetPositionIncrementGap(string fieldName)
    {
        return 10;
    }

    protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
    {
        // Same components as StandardAnalyzer
        var src = new StandardTokenizer(m_matchVersion, reader);
        src.MaxTokenLength = MaxTokenLength;
        TokenStream tok = new StandardFilter(m_matchVersion, src);
        tok = new LowerCaseFilter(m_matchVersion, tok);
        tok = new StopFilter(m_matchVersion, tok, m_stopwords);
        return new PositionIncrementGapTokenStreamComponents(this, src, tok);
    }

    private class PositionIncrementGapTokenStreamComponents : TokenStreamComponents
    {
        private readonly PositionIncrementGapAnalyzer analyzer;
        private readonly StandardTokenizer src;

        public PositionIncrementGapTokenStreamComponents(PositionIncrementGapAnalyzer analyzer,
            StandardTokenizer src, TokenStream tok)
            : base(src, tok)
        {
            this.analyzer = analyzer;
            this.src = src;
        }

        protected override void SetReader(TextReader reader)
        {
            src.MaxTokenLength = analyzer.MaxTokenLength;
            base.SetReader(reader);
        }
    }
}

LuceneVersion matchVersion = LuceneVersion.LUCENE_XY; // Substitute desired Lucene version for XY
Analyzer myAnalyzer = new PositionIncrementGapAnalyzer(matchVersion);
```

### Token Position Increments

 By default, all tokens created by Analyzers and Tokenizers have a [PositionIncrement](xref:Lucene.Net.Analysis.TokenAttributes.IPositionIncrementAttribute#Lucene_Net_Analysis_TokenAttributes_IPositionIncrementAttribute_PositionIncrement) of one. This means that the position stored for that token in the index would be one more than that of the previous token. Recall that phrase and proximity searches rely on position info. 

 If the selected analyzer filters the stop words "is" and "the", then for a document containing the string "blue is the sky", only the tokens "blue", "sky" are indexed, with position("sky") = 3 + position("blue"). Now, a phrase query "blue is the sky" would find that document, because the same analyzer filters the same stop words from that query. But the phrase query "blue sky" would not find that document because the position increment between "blue" and "sky" is only 1. 

 If this behavior does not fit the application needs, the query parser needs to be configured to not take position increments into account when generating phrase queries. 

 Note that a StopFilter MUST increment the position increment in order not to generate corrupt TokenStream graphs. Here is the logic used by StopFilter to increment positions when filtering out tokens: 

```cs
public class MyTokenStream : TokenStream
{
    private readonly TokenStream ts;
    private readonly CharArraySet stopWords;
    private readonly ICharTermAttribute termAtt;
    private readonly IPositionIncrementAttribute posIncrAtt;
    
    public MyTokenStream(TokenStream ts, CharArraySet stopWords)
    {
        this.ts = ts;
        termAtt = AddAttribute<ICharTermAttribute>();
        posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
    }
    
    public override bool IncrementToken()
    {
        int extraIncrement = 0;
        while (true)
        {
            bool hasNext = ts.IncrementToken();
            if (hasNext)
            {
                if (stopWords.Contains(termAttr.ToString()))
                {
                    extraIncrement += posIncrAtt.PositionIncrement; // filter this word
                    continue;
                }
                if (extraIncrement > 0)
                {
                    posIncrAtt.PositionIncrement += extraIncrement;
                }
            }
            return hasNext;
        }
    }
}

public override TokenStream GetTokenStream(string fieldName, Reader reader)
{
    TokenStream ts = someAnalyzer.GetTokenStream(fieldName, reader);
    return new MyTokenStream(ts, stopWords);
}
```

 A few more use cases for modifying position increments are: 

1.  Inhibiting phrase and proximity matches in sentence boundaries – for this, a tokenizer that 
    identifies a new sentence can add 1 to the position increment of the first token of the new sentence.

2.  Injecting synonyms – here, synonyms of a token should be added after that token, 
    and their position increment should be set to 0.
    As result, all synonyms of a token would be considered to appear in exactly the 
    same position as that token, and so would they be seen by phrase and proximity searches.

### Token Position Length

 By default, all tokens created by Analyzers and Tokenizers have a [Length](xref:Lucene.Net.Analysis.TokenAttributes.IPositionLengthAttribute#Lucene_Net_Analysis_TokenAttributes_IPositionLengthAttribute_PositionLength) of one. This means that the token occupies a single position. This attribute is not indexed and thus not taken into account for positional queries, but is used by eg. suggesters. 

 The main use case for positions lengths is multi-word synonyms. With single-word synonyms, setting the position increment to 0 is enough to denote the fact that two words are synonyms, for example: 

<table>
<tr><td>Term</td><td>red</td><td>magenta</td></tr>
<tr><td>Position increment</td><td>1</td><td>0</td></tr>
</table>

 Given that position(magenta) = 0 + position(red), they are at the same position, so anything working with analyzers will return the exact same result if you replace "magenta" with "red" in the input. However, multi-word synonyms are more tricky. Let's say that you want to build a TokenStream where "IBM" is a synonym of "Internal Business Machines". Position increments are not enough anymore: 

<table>
<tr><td>Term</td><td>IBM</td><td>International</td><td>Business</td><td>Machines</td></tr>
<tr><td>Position increment</td><td>1</td><td>0</td><td>1</td><td>1</td></tr>
</table>

 The problem with this token stream is that "IBM" is at the same position as "International" although it is a synonym with "International Business Machines" as a whole. Setting the position increment of "Business" and "Machines" to 0 wouldn't help as it would mean than "International" is a synonym of "Business". The only way to solve this issue is to make "IBM" span across 3 positions, this is where position lengths come to rescue. 

<table>
<tr><td>Term</td><td>IBM</td><td>International</td><td>Business</td><td>Machines</td></tr>
<tr><td>Position increment</td><td>1</td><td>0</td><td>1</td><td>1</td></tr>
<tr><td>Position length</td><td>3</td><td>1</td><td>1</td><td>1</td></tr>
</table>

 This new attribute makes clear that "IBM" and "International Business Machines" start and end at the same positions. 

### How to not write corrupt token streams

 There are a few rules to observe when writing custom Tokenizers and TokenFilters: 

*   The first position increment must be > 0.

*   Positions must not go backward.

*   Tokens that have the same start position must have the same start offset.

*   Tokens that have the same end position (taking into account the
  position length) must have the same end offset.

*   Tokenizers must call [ClearAttributes()](xref:Lucene.Net.Util.AttributeSource#Lucene_Net_Util_AttributeSource_ClearAttributes) in
  IncrementToken().

*   Tokenizers must override [End()](xref:Lucene.Net.Analysis.TokenStream#Lucene_Net_Analysis_TokenStream_End), and pass the final
  offset (the total number of input characters processed) to both
  parameters of [OffsetAttribute.SetOffset(int, int)](xref:Lucene.Net.Analysis.TokenAttributes.IOffsetAttribute#Lucene_Net_Analysis_TokenAttributes_IOffsetAttribute_SetOffset_System_Int32_System_Int32_).

 Although these rules might seem easy to follow, problems can quickly happen when chaining badly implemented filters that play with positions and offsets, such as synonym or n-grams filters. Here are good practices for writing correct filters: 

*   Token filters should not modify offsets. If you feel that your filter would need to modify offsets, then it should probably be implemented as a Tokenizer.

*   Token filters should not insert positions. If a filter needs to add tokens, then they should all have a position increment of 0.

*   When they add tokens, token filters should call [ClearAttributes()](xref:Lucene.Net.Util.AttributeSource#Lucene_Net_Util_AttributeSource_ClearAttributes) first.

*   When they remove tokens, token filters should increment the position increment of the following token.

*   Token filters should preserve position lengths.

## TokenStream API

 "Flexible Indexing" summarizes the effort of making the Lucene.NET indexer pluggable and extensible for custom index formats. A fully customizable indexer means that users will be able to store custom data structures on disk. Therefore an API is necessary that can transport custom types of data from the documents to the indexer. 

### Attribute and AttributeSource

 Classes <xref:Lucene.Net.Util.Attribute> and <xref:Lucene.Net.Util.AttributeSource> serve as the basis upon which the analysis elements of "Flexible Indexing" are implemented. An Attribute holds a particular piece of information about a text token. For example, <xref:Lucene.Net.Analysis.TokenAttributes.ICharTermAttribute> contains the term text of a token, and <xref:Lucene.Net.Analysis.TokenAttributes.IOffsetAttribute> contains the start and end character offsets of a token. An AttributeSource is a collection of Attributes with a restriction: there may be only one instance of each attribute type. TokenStream now extends AttributeSource, which means that one can add Attributes to a TokenStream. Since TokenFilter extends TokenStream, all filters are also AttributeSources. 

 Lucene.NET provides seven Attributes out of the box: 

<table rules="all" frame="box" cellpadding="3">
  <colgroup>
    <col span="1" style="width:30%">
    <col span="1" style="width:70%">
  </colgroup>
  <tr>
    <td><xref:Lucene.Net.Analysis.TokenAttributes.ICharTermAttribute></td>
    <td>
      The term text of a token.  Implements J2N.Text.ICharSequence 
      (providing properties Length and this[int], and allowing e.g. for direct
      use with J2N.Text.IAppendable (allowing the term text to be appended to.)
      In .NET, we can only use this indirectly with the [Regex](https://docs.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex) class by first calling ToString() and then passing the string to the Regex.
    </td>
  </tr>
  <tr>
    <td><xref:Lucene.Net.Analysis.TokenAttributes.IOffsetAttribute></td>
    <td>The start and end offset of a token in characters.</td>
  </tr>
  <tr>
    <td><xref:Lucene.Net.Analysis.TokenAttributes.IPositionIncrementAttribute></td>
    <td>See above for detailed information about position increment.</td>
  </tr>
  <tr>
    <td><xref:Lucene.Net.Analysis.TokenAttributes.IPositionLengthAttribute></td>
    <td>The number of positions occupied by a token.</td>
  </tr>
  <tr>
    <td><xref:Lucene.Net.Analysis.TokenAttributes.IPayloadAttribute></td>
    <td>The payload that a Token can optionally have.</td>
  </tr>
  <tr>
    <td><xref:Lucene.Net.Analysis.TokenAttributes.ITypeAttribute></td>
    <td>The type of the token. Default is 'word'.</td>
  </tr>
  <tr>
    <td><xref:Lucene.Net.Analysis.TokenAttributes.IFlagsAttribute></td>
    <td>Optional flags a token can have.</td>
  </tr>
  <tr>
    <td><xref:Lucene.Net.Analysis.TokenAttributes.IKeywordAttribute></td>
    <td>
      Keyword-aware TokenStreams/-Filters skip modification of tokens that
      return true from this attribute's [IsKeyword](xref:Lucene.Net.Analysis.TokenAttributes.IKeywordAttribute#Lucene_Net_Analysis_TokenAttributes_IKeywordAttribute_IsKeyword) property. 
    </td>
  </tr>
</table>

### More Requirements for Analysis Component Classes

Due to the historical development of the API, there are some perhaps
less than obvious requirements to implement analysis components
classes.

#### Token Stream Lifetime

The code fragment of the [analysis workflow
protocol](#analysis-workflow) above shows a token stream being obtained, used, and then
left for garbage. However, that does not mean that the components of
that token stream will, in fact, be discarded. The default is just the
opposite. <xref:Lucene.Net.Analysis.Analyzer> applies a reuse
strategy to the tokenizer and the token filters. It will reuse
them. For each new input, it calls [SetReader(System.IO.TextReader)](xref:Lucene.Net.Analysis.Tokenizer#Lucene_Net_Analysis_Tokenizer_SetReader_System_IO_TextReader_) 
to set the input. Your components must be prepared for this scenario,
as described below.

#### Tokenizer

*   You should create your tokenizer class by extending <xref:Lucene.Net.Analysis.Tokenizer>.

*   Your tokenizer must __never__ make direct use of the
  [System.Text.TextReader](https://docs.microsoft.com/en-us/dotnet/api/system.io.textreader) supplied to its constructor(s). (A future
  release of Apache Lucene.NET will remove the reader parameters from the
  Tokenizer constructors.)
  <xref:Lucene.Net.Analysis.Tokenizer> wraps the text reader in an
  object that helps enforce that applications comply with the [analysis workflow](#analysis-workflow). Thus, your class
  should only reference the input via the protected 'input' field
  of Tokenizer.

*   Your tokenizer __must__ override [End()](xref:Lucene.Net.Analysis.TokenStream#Lucene_Net_Analysis_TokenStream_End).
  Your implementation __must__ call
  `base.End()`. It must set a correct final offset into
  the offset attribute, and finish up and other attributes to reflect
  the end of the stream.

*   If your tokenizer overrides [Reset()](xref:Lucene.Net.Analysis.TokenStream#Lucene_Net_Analysis_TokenStream_Reset)
  or [Dispose()](xref:Lucene.Net.Analysis.TokenStream#Lucene_Net_Analysis_TokenStream_Dispose_System_Boolean_), it
  __must__ call the corresponding superclass method.

#### Token Filter

  You should create your token filter class by extending <xref:Lucene.Net.Analysis.TokenFilter>.
  If your token filter overrides [Reset()](xref:Lucene.Net.Analysis.TokenStream#Lucene_Net_Analysis_TokenStream_Reset),
  [End()](xref:Lucene.Net.Analysis.TokenStream#Lucene_Net_Analysis_TokenStream_End)
  or [Dispose()](xref:Lucene.Net.Analysis.TokenStream#Lucene_Net_Analysis_TokenStream_Dispose_System_Boolean_), it
  __must__ call the corresponding superclass method.

#### Creating delegates

  Forwarding classes (those which extend <xref:Lucene.Net.Analysis.Tokenizer> but delegate
  selected logic to another tokenizer) must also set the reader to the delegate in the overridden
  [Reset()](xref:Lucene.Net.Analysis.Tokenizer#Lucene_Net_Analysis_Tokenizer_Reset) method, e.g.:

```cs
public class ForwardingTokenizer : Tokenizer
{
    private Tokenizer @delegate;
    ...
   
    public override void Reset()
    {
        base.Reset();
        @delegate.SetReader(this.m_input);
        @delegate.Reset();
    }
}
```

### Testing Your Analysis Component

 The Lucene.Net.TestFramework component defines [BaseTokenStreamTestCase](../test-framework/Lucene.Net.Analysis.BaseTokenStreamTestCase.html). By extending this class, you can create NUnit tests that validate that your Analyzer and/or analysis components correctly implement the protocol. The CheckRandomData()methods of that class are particularly effective in flushing out errors. 

### Using the TokenStream API

There are a few important things to know in order to use the new API efficiently which are summarized here. You may want
to walk through the example below first and come back to this section afterwards.

1.  Please keep in mind that an AttributeSource can only have one instance of a particular Attribute. Furthermore, if 
a chain of a TokenStream and multiple TokenFilters is used, then all TokenFilters in that chain share the Attributes
with the TokenStream.

2.  Attribute instances are reused for all tokens of a document. Thus, a TokenStream/-Filter needs to update
the appropriate Attribute(s) in IncrementToken(). The consumer, commonly the Lucene.NET indexer, consumes the data in the
Attributes and then calls IncrementToken() again until it returns false, which indicates that the end of the stream
was reached. This means that in each call of IncrementToken() a TokenStream/-Filter can safely overwrite the data in
the Attribute instances.

3.  For performance reasons a TokenStream/-Filter should add/get Attributes during instantiation; i.e., create an attribute in the
constructor and store references to it in an instance variable.  Using an instance variable instead of calling `AddAttribute<T>()`/`GetAttribute<T>()` 
in IncrementToken() will avoid attribute lookups for every token in the document.

4.  All methods in AttributeSource are idempotent, which means calling them multiple times always yields the same
result. This is especially important to know for `AddAttribute<T>()`. The method takes the __interface__ 
of an Attribute as an generic closing type and returns an __instance__. If an Attribute of the same type was previously added, then
the already existing instance is returned, otherwise a new instance is created and returned. Therefore TokenStreams/-Filters
can safely call `AddAttribute<T>()` with the same Attribute type multiple times. Even consumers of TokenStreams should
normally call `AddAttribute<T>()` instead of `GetAttribute<T>()`, because it would not fail if the TokenStream does not have this
Attribute (`GetAttribute<T>()` would throw an ArgumentException, if the Attribute is missing). More advanced code
could simply check with `HasAttribute<T>()`, if a TokenStream has it, and may conditionally leave out processing for
extra performance.

### Example

 In this example we will create a WhiteSpaceTokenizer and use a LengthFilter to suppress all words that have only two or fewer characters. The LengthFilter is part of the Lucene.Net assembly and its implementation will be explained here to illustrate the usage of the TokenStream API. 

 Then we will develop a custom Attribute, a IPartOfSpeechAttribute, and add another filter to the chain which utilizes the new custom attribute, and call it PartOfSpeechTaggingFilter. 

#### Whitespace tokenization

```cs
public class MyAnalyzer : Analyzer
{
    private LuceneVersion matchVersion;

    public MyAnalyzer(LuceneVersion matchVersion)
    {
        this.matchVersion = matchVersion;
    }

    protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
    {
        return new TokenStreamComponents(new WhitespaceTokenizer(matchVersion, reader));
    }
}
    
public static class Program
{
    public static void Main(string[] args)
    {
        // text to tokenize
        string text = "This is a demo of the TokenStream API";

        LuceneVersion matchVersion = Version.LUCENE_XY; // Substitute desired Lucene version for XY
        MyAnalyzer analyzer = new MyAnalyzer(matchVersion);
        TokenStream stream = analyzer.GetTokenStream("field", new StringReader(text));

        // get the CharTermAttribute from the TokenStream
        ICharTermAttribute termAtt = stream.AddAttribute<ICharTermAttribute>();

        try
        {
            stream.Reset();

            // print all tokens until stream is exhausted
            while (stream.IncrementToken())
            {
                Console.WriteLine(termAtt.ToString());
            }

            stream.End();
    }
    finally
    {
        stream.Dispose();
    }
}
```

In this easy example a simple white space tokenization is performed. In Main() a loop consumes the stream and
prints the term text of the tokens by accessing the ICharTermAttribute that the WhitespaceTokenizer provides. 
Here is the output:

```
This
is
a
demo
of
the
new
TokenStream
API
```

#### Adding a LengthFilter

We want to suppress all tokens that have 2 or less characters. We can do that
easily by adding a LengthFilter to the chain. Only the
`CreateComponents()` method in our analyzer needs to be changed:

```cs
protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
{
    Tokenizer source = new WhitespaceTokenizer(matchVersion, reader);
    TokenStream result = new LengthFilter(true, source, 3, int.MaxValue);
    return new TokenStreamComponents(source, result);
}
```

Note how now only words with 3 or more characters are contained in the output:

```
This
demo
the
new
TokenStream
API
```

Now let's take a look how the LengthFilter is implemented:

```cs
public sealed class LengthFilter : FilteringTokenFilter
{
    private readonly int min;
    private readonly int max;

    private readonly ICharTermAttribute termAtt = AddAttribute<ICharTermAttribute>();

    /// <summary>
    /// Create a new LengthFilter. This will filter out tokens whose
    /// ICharTermAttribute is either too short
    /// (< min) or too long (> max).
    /// </summary>
    /// <param name="version">the Lucene match version</param>
    /// <param name="input">the TokenStream to consume</param>
    /// <param name="min">the minimum length</param>
    /// <param name="max">the maximum length</param>
    public LengthFilter(LuceneVersion version, TokenStream input, int min, int max)
        : base(version, input)
    {

        this.min = min;
        this.max = max;
    }

    public override bool Accept()
    {
        int len = termAtt.Length;
        return len >= min && len <= max;
    }
}
```

 In LengthFilter, the ICharTermAttribute is added and stored in the instance variable `termAtt`. Remember that there can only be a single instance of ICharTermAttribute in the chain, so in our example the `AddAttribute<T>()` call in LengthFilter returns the ICharTermAttribute that the WhitespaceTokenizer already added. 

 The tokens are retrieved from the input stream in FilteringTokenFilter's `IncrementToken()` method (see below), which calls LengthFilter's `Accept()` method. By looking at the term text in the ICharTermAttribute, the length of the term can be determined and tokens that are either too short or too long are skipped. Note how `Accept()` can efficiently access the instance variable; no attribute lookup is necessary. The same is true for the consumer, which can simply use local references to the Attributes. 

```cs
public abstract class FilteringTokenFilter : TokenFilter
{
    private readonly IPositionIncrementAttribute posIncrAtt = AddAttribute<IPositionIncrementAttribute>();

    /// <summary>
    /// Create a new FilteringTokenFilter.
    /// </summary>
    /// <param name="version">the Lucene match version</param>
    /// <param name="input">the TokenStream to consume</param>
    public FilteringTokenFilter(LuceneVersion version, TokenStream input)
        : base(input)
    {
    }

    /// <summary>
    /// Override this method and return if the current input token should be returned by IncrementToken.
    /// </summary>
    protected abstract bool Accept();

    public sealed override bool IncrementToken()
    {
        int skippedPositions = 0;
        while (m_input.IncrementToken())
        {
            if (Accept())
            {
                if (skippedPositions != 0)
                {
                    posIncrAtt.PositionIncrement += skippedPositions;
                }
                return true;
            }
            skippedPositions += posIncrAtt.PositionIncrement;
        }
        // reached end of stream -- return false
        return false;
    }

    public override void Reset()
    {
        base.Reset();
    }
}
```

#### Adding a custom IAttribute

Now we're going to implement our own custom IAttribute for part-of-speech tagging and call it consequently 
`IPartOfSpeechAttribute`. First we need to define the interface of the new IAttribute:

```cs
public enum PartOfSpeech
{
	Noun, Verb, Adjective, Adverb, Pronoun, Preposition, Conjunction, Article, Unknown
}

public interface IPartOfSpeechAttribute : IAttribute
{
	PartOfSpeech PartOfSpeech { get; set; }
}
```

 Now we also need to write the implementing class. The name of that class is important here: By default, Lucene checks if there is a class with the name of the IAttribute without the prefix 'I'. In this example, we would consequently call the implementing class `PartOfSpeechAttribute`. 

 This should be the usual behavior. However, there is also an expert-API that allows changing these naming conventions: <xref:Lucene.Net.Util.AttributeSource.AttributeFactory>. The factory accepts an IAttribute interface as argument and returns an actual instance. You can implement your own factory if you need to change the default behavior. 

 Now here is the actual class that implements our new Attribute. Notice that the class has to extend <xref:Lucene.Net.Util.Attribute>: 

```cs
public sealed class PartOfSpeechAttribute : Lucene.Net.Util.Attribute, IPartOfSpeechAttribute
{
	public PartOfSpeech PartOfSpeech { get; set; } = PartOfSpeech.Unknown;

	public override void Clear()
	{
		PartOfSpeech = PartOfSpeech.Unknown;
	}

	public override void CopyTo(IAttribute target)
	{
		((IPartOfSpeechAttribute)target).PartOfSpeech = PartOfSpeech;
	}
}
```

 This is a simple Attribute implementation has only a single variable that stores the part-of-speech of a token. It extends the `Attribute` class and therefore implements its abstract methods `Clear()` and `CopyTo()`. Now we need a TokenFilter that can set this new PartOfSpeechAttribute for each token. In this example we show a very naive filter that tags every word with a leading upper-case letter as a 'Noun' and all other words as 'Unknown'. 

```cs
public class PartOfSpeechTaggingFilter : TokenFilter
{
    private readonly IPartOfSpeechAttribute posAtt;
    private readonly ICharTermAttribute termAtt;

    protected PartOfSpeechTaggingFilter(TokenStream input)
        : base(input)
    {
        posAtt = AddAttribute<IPartOfSpeechAttribute>();
        termAtt = AddAttribute<ICharTermAttribute>();
    }

    public bool IncrementToken()
    {
        if (!m_input.IncrementToken()) { return false; }
        posAtt.PartOfSpeech = DeterminePartOfSpeech(termAtt.Buffer, 0, termAtt.Length);
        return true;
    }

    // determine the part of speech for the given term
    protected PartOfSpeech DeterminePartOfSpeech(char[] term, int offset, int length)
    {
        // naive implementation that tags every uppercased word as noun
        if (length > 0 && char.IsUpper(term[0]))
        {
            return PartOfSpeech.Noun;
        }
        return PartOfSpeech.Unknown;
    }
}
```

 Just like the LengthFilter, this new filter stores references to the attributes it needs in instance variables. Notice how you only need to pass in the interface of the new Attribute and instantiating the correct class is automatically taken care of. 

Now we need to add the filter to the chain in MyAnalyzer:

```cs
protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
{
    Tokenizer source = new WhitespaceTokenizer(matchVersion, reader);
    TokenStream result = new LengthFilter(matchVersion, source, 3, int.MaxValue);
    result = new PartOfSpeechTaggingFilter(result);
    return new TokenStreamComponents(source, result);
}
```

Now let's look at the output:

```
This
demo
the
new
TokenStream
API
```

Apparently it hasn't changed, which shows that adding a custom attribute to a TokenStream/Filter chain does not
affect any existing consumers, simply because they don't know the new Attribute. Now let's change the consumer
to make use of the new PartOfSpeechAttribute and print it out:

```cs
public static class Program
{
	public static void Main(string[] args)
	{
		// text to tokenize
		string text = "This is a demo of the TokenStream API";

		MyAnalyzer analyzer = new MyAnalyzer();
		TokenStream stream = analyzer.GetTokenStream("field", new StringReader(text));

		// get the ICharTermAttribute from the TokenStream
		ICharTermAttribute termAtt = stream.AddAttribute<ICharTermAttribute>();

		// get the PartOfSpeechAttribute from the TokenStream
		IPartOfSpeechAttribute posAtt = stream.AddAttribute<IPartOfSpeechAttribute>();

		try
		{
			stream.Reset();

			// print all tokens until stream is exhausted
			while (stream.IncrementToken())
			{
				Console.WriteLine(termAtt.ToString() + ": " + posAtt.PartOfSpeech);
			}

			stream.End();
		}
		finally
		{
			stream.Dispose();
		}
	}
}
```

The change that was made is to get the PartOfSpeechAttribute from the TokenStream and print out its contents in
the while loop that consumes the stream. Here is the new output:

```
This: Noun
demo: Unknown
the: Unknown
new: Unknown
TokenStream: Noun
API: Noun
```

Each word is now followed by its assigned PartOfSpeech tag. Of course this is a naive 
part-of-speech tagging. The word 'This' should not even be tagged as noun; it is only spelled capitalized because it
is the first word of a sentence. Actually this is a good opportunity for an exercise. To practice the usage of the new
API the reader could now write an IAttribute and TokenFilter that can specify for each word if it was the first token
of a sentence or not. Then the PartOfSpeechTaggingFilter can make use of this knowledge and only tag capitalized words
as nouns if not the first word of a sentence (we know, this is still not a correct behavior, but hey, it's a good exercise). 
As a small hint, this is how the new IAttribute class could begin:

```cs
public class FirstTokenOfSentenceAttribute : Lucene.Net.Util.Attribute, IFirstTokenOfSentenceAttribute
{
	public bool FirstToken { get; set; }

	public override void Clear()
	{
		FirstToken = false;
	}

	// ...
}
```

#### Adding a CharFilter chain

Analyzers take .NET [System.Text.TextReader](https://docs.microsoft.com/en-us/dotnet/api/system.io.textreader)s as input. In Java you can wrap Readers with [java.io.FilterReader](https://docs.oracle.com/javase/7/docs/api/java/io/FilterReader.html)s
to manipulate content, but this would have the big disadvantage that character offsets might be inconsistent with your original
text.

<xref:Lucene.Net.Analysis.CharFilter> is designed to allow you to pre-process input like a FilterReader would, but also
preserve the original offsets associated with those characters. This way mechanisms like highlighting still work correctly.
CharFilters can be chained.

Example:

```cs
public class MyAnalyzer : Analyzer
{
	protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
	{
		return new TokenStreamComponents(new MyTokenizer(reader));
	}

	protected override TextReader InitReader(string fieldName, TextReader reader)
	{
		// wrap the Reader in a CharFilter chain.
		return new SecondCharFilter(new FirstCharFilter(reader));
	}
}
```