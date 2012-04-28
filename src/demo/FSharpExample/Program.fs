// Learn more about F# at http://fsharp.net. See the 'F# Tutorial' project
// for more guidance on F# programming.

// To use an explicit namespace, uncomment the following line. You will need
// to place all value and function definitions into a module.
// 
// namespace FSharpExample
open System
open System.IO

open Lucene.Net

open Lucene.Net.Analysis
open Lucene.Net.Analysis.Standard
open Lucene.Net.Documents
open Lucene.Net.Index
open Lucene.Net.QueryParsers
open Lucene.Net.Search
open Lucene.Net.Store

module Lucene =
    let LuceneRead() = 
        let filename = AppDomain.CurrentDomain.BaseDirectory + "LuceneIndex"
        let directory = FSDirectory.Open(new System.IO.DirectoryInfo(filename));
        let version = new Lucene.Net.Util.Version()
        let indexReader = IndexReader.Open(directory, false)
        for i in 0 .. indexReader.MaxDoc - 1 do
            let doc = indexReader.Document(i)
            printfn "document %s" <| doc.Get("postBody")

    let AddTextToIndex txts text writer =
        let doc = new Document()
        doc.Add(new Field("id", txts.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED))
        doc.Add(new Field("postBody", text, Field.Store.YES, Field.Index.ANALYZED))
        (writer : IndexWriter).AddDocument(doc)

    let Search text searcher parser indexReader =
        let query = (parser : QueryParser).Parse(text)
        let hits = (searcher : IndexSearcher).Search(query, 10)
        let results = hits.TotalHits
        printfn "Found %d results" results
        for i in 0 .. results - 2 do //New value isn't here yet I guess
            let doc = (indexReader : IndexReader).Document(hits.ScoreDocs.[i].Doc)
            let score = hits.ScoreDocs.[i].Score
            printfn "--Result num %s , score %s" <| i.ToString() <| score.ToString()
            printfn "--ID: %s"          <| doc.Get("id")
            printfn "--Text found: %s"  <| doc.Get("postBody")

    let LuceneWrite() = 
        let filename = AppDomain.CurrentDomain.BaseDirectory + "LuceneIndex"
        printfn "Lucene index: %s" filename
        use directory = FSDirectory.Open(new System.IO.DirectoryInfo(filename));
        let version = new Lucene.Net.Util.Version()
        let analyzer = new StandardAnalyzer(version)
        use writer = new IndexWriter(directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED)
        let indexReader = IndexReader.Open(directory, false)
        AddTextToIndex 0 "hello" writer
        writer.Optimize()
        writer.Commit()
        //Sercher example
        use searcher = new IndexSearcher(directory, false)
        let parser = new QueryParser(version, "postBody", analyzer)
        Search "hello" searcher parser indexReader

open Lucene

LuceneWrite()
LuceneRead()

Console.ReadKey() |> ignore