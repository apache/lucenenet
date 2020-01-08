---
uid: Lucene.Net.Benchmarks.ByTask
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

Benchmarking Lucene By Tasks.
<div>

 This package provides "task based" performance benchmarking of Lucene. One can use the predefined benchmarks, or create new ones. 

 Contained packages: 


<table border="1" cellpadding="4">
 <tr>
   <td>__Package__</td>
   <td>__Description__</td>
 </tr>
 <tr>
   <td>[stats](stats/package-summary.html)</td>
   <td>Statistics maintained when running benchmark tasks.</td>
 </tr>
 <tr>
   <td>[tasks](tasks/package-summary.html)</td>
   <td>Benchmark tasks.</td>
 </tr>
 <tr>
   <td>[feeds](feeds/package-summary.html)</td>
   <td>Sources for benchmark inputs: documents and queries.</td>
 </tr>
 <tr>
   <td>[utils](utils/package-summary.html)</td>
   <td>Utilities used for the benchmark, and for the reports.</td>
 </tr>
 <tr>
   <td>[programmatic](programmatic/package-summary.html)</td>
   <td>Sample performance test written programmatically.</td>
 </tr>
</table>

## Table Of Contents

 1. [Benchmarking By Tasks](#concept) 2. [How to use](#usage) 3. [Benchmark "algorithm"](#algorithm) 4. [Supported tasks/commands](#tasks) 5. [Benchmark properties](#properties) 6. [Example input algorithm and the result benchmark report.](#example) 7. [Results record counting clarified](#recscounting) 

## Benchmarking By Tasks

 Benchmark Lucene using task primitives. 

 A benchmark is composed of some predefined tasks, allowing for creating an index, adding documents, optimizing, searching, generating reports, and more. A benchmark run takes an "algorithm" file that contains a description of the sequence of tasks making up the run, and some properties defining a few additional characteristics of the benchmark run. 

## How to use

 Easiest way to run a benchmarks is using the predefined ant task: * ant run-task   
- would run the `micro-standard.alg` "algorithm". * ant run-task -Dtask.alg=conf/compound-penalty.alg   
- would run the `compound-penalty.alg` "algorithm". * ant run-task -Dtask.alg=[full-path-to-your-alg-file]   
- would run `your perf test` "algorithm". * java org.apache.lucene.benchmark.byTask.programmatic.Sample   
- would run a performance test programmatically - without using an alg file. This is less readable, and less convenient, but possible. 

 You may find existing tasks sufficient for defining the benchmark _you_ need, otherwise, you can extend the framework to meet your needs, as explained herein. 

 Each benchmark run has a DocMaker and a QueryMaker. These two should usually match, so that "meaningful" queries are used for a certain collection. Properties set at the header of the alg file define which "makers" should be used. You can also specify your own makers, extending DocMaker and implementing QueryMaker. 

> __Note:__ since 2.9, DocMaker is a concrete class which accepts a ContentSource. In most cases, you can use the DocMaker class to create Documents, while providing your own ContentSource implementation. For example, the current Benchmark package includes ContentSource implementations for TREC, Enwiki and Reuters collections, as well as others like LineDocSource which reads a 'line' file produced by WriteLineDocTask.

 Benchmark .alg file contains the benchmark "algorithm". The syntax is described below. Within the algorithm, you can specify groups of commands, assign them names, specify commands that should be repeated, do commands in serial or in parallel, and also control the speed of "firing" the commands. 

 This allows, for instance, to specify that an index should be opened for update, documents should be added to it one by one but not faster than 20 docs a minute, and, in parallel with this, some N queries should be searched against that index, again, no more than 2 queries a second. You can have the searches all share an index reader, or have them each open its own reader and close it afterwords. 

 If the commands available for use in the algorithm do not meet your needs, you can add commands by adding a new task under org.apache.lucene.benchmark.byTask.tasks - you should extend the PerfTask abstract class. Make sure that your new task class name is suffixed by Task. Assume you added the class "WonderfulTask" - doing so also enables the command "Wonderful" to be used in the algorithm. 

 <u>External classes</u>: It is sometimes useful to invoke the benchmark package with your external alg file that configures the use of your own doc/query maker and or html parser. You can work this out without modifying the benchmark package code, by passing your class path with the benchmark.ext.classpath property: * ant run-task -Dtask.alg=[full-path-to-your-alg-file] <font color="#FF0000">-Dbenchmark.ext.classpath=/mydir/classes </font> -Dtask.mem=512M <u>External tasks</u>: When writing your own tasks under a package other than __org.apache.lucene.benchmark.byTask.tasks__ specify that package thru the <font color="#FF0000">alt.tasks.packages</font> property. 

## Benchmark "algorithm"

 The following is an informal description of the supported syntax. 

1.  __Measuring__: When a command is executed, statistics for the elapsed
 execution time and memory consumption are collected.
 At any time, those statistics can be printed, using one of the
 available ReportTasks.

2.  __Comments__ start with '<font color="#FF0066">#</font>'.

3.  __Serial__ sequences are enclosed within '<font color="#FF0066">{ }</font>'.

4.  __Parallel__ sequences are enclosed within
 '<font color="#FF0066">[ ]</font>'

5.  __Sequence naming:__ To name a sequence, put
 '<font color="#FF0066">"name"</font>' just after
 '<font color="#FF0066">{</font>' or '<font color="#FF0066">[</font>'.

Example - <font color="#FF0066">{ "ManyAdds" AddDoc } : 1000000</font> -
 would
 name the sequence of 1M add docs "ManyAdds", and this name would later appear
 in statistic reports.
 If you don't specify a name for a sequence, it is given one: you can see it as
 the  algorithm is printed just before benchmark execution starts.

6.  __Repeating__:
 To repeat sequence tasks N times, add '<font color="#FF0066">: N</font>' just
 after the
 sequence closing tag - '<font color="#FF0066">}</font>' or
 '<font color="#FF0066">]</font>' or '<font color="#FF0066">></font>'.

Example -  <font color="#FF0066">[ AddDoc ] : 4</font>  - would do 4 addDoc
 in parallel, spawning 4 threads at once.

Example -  <font color="#FF0066">[ AddDoc AddDoc ] : 4</font>  - would do
 8 addDoc in parallel, spawning 8 threads at once.

Example -  <font color="#FF0066">{ AddDoc } : 30</font> - would do addDoc
 30 times in a row.

Example -  <font color="#FF0066">{ AddDoc AddDoc } : 30</font> - would do
 addDoc 60 times in a row.

__Exhaustive repeating__: use <font color="#FF0066">*</font> instead of
 a number to repeat exhaustively.
 This is sometimes useful, for adding as many files as a doc maker can create,
 without iterating over the same file again, especially when the exact
 number of documents is not known in advance. For instance, TREC files extracted
 from a zip file. Note: when using this, you must also set
 <font color="#FF0066">doc.maker.forever</font> to false.

Example -  <font color="#FF0066">{ AddDoc } : *</font>  - would add docs
 until the doc maker is "exhausted".

7.  __Command parameter__: a command can optionally take a single parameter.
 If the certain command does not support a parameter, or if the parameter is of
 the wrong type,
 reading the algorithm will fail with an exception and the test would not start.
 Currently the following tasks take optional parameters:

    *   __AddDoc__ takes a numeric parameter, indicating the required size of
       added document. Note: if the DocMaker implementation used in the test
       does not support makeDoc(size), an exception would be thrown and the test
       would fail.

    *   __DeleteDoc__ takes numeric parameter, indicating the docid to be
       deleted. The latter is not very useful for loops, since the docid is
       fixed, so for deletion in loops it is better to use the
       `doc.delete.step` property.

    *   __SetProp__ takes a `name,value` mandatory param,
       ',' used as a separator.

    *   __SearchTravRetTask__ and __SearchTravTask__ take a numeric
              parameter, indicating the required traversal size.

    *   __SearchTravRetLoadFieldSelectorTask__ takes a string
              parameter: a comma separated list of Fields to load.

    *   __SearchTravRetHighlighterTask__ takes a string
              parameter: a comma separated list of parameters to define highlighting.  See that
     tasks javadocs for more information

Example - <font color="#FF0066">AddDoc(2000)</font> - would add a document
 of size 2000 (~bytes).

See conf/task-sample.alg for how this can be used, for instance, to check
 which is faster, adding
 many smaller documents, or few larger documents.
 Next candidates for supporting a parameter may be the Search tasks,
 for controlling the query size.

8.  __Statistic recording elimination__: - a sequence can also end with
 '<font color="#FF0066">></font>',
 in which case child tasks would not store their statistics.
 This can be useful to avoid exploding stats data, for adding say 1M docs.

Example - <font color="#FF0066">{ "ManyAdds" AddDoc > : 1000000</font> -
 would add million docs, measure that total, but not save stats for each addDoc.

Notice that the granularity of System.currentTimeMillis() (which is used
 here) is system dependant,
 and in some systems an operation that takes 5 ms to complete may show 0 ms
 latency time in performance measurements.
 Therefore it is sometimes more accurate to look at the elapsed time of a larger
 sequence, as demonstrated here.

9.  __Rate__:
 To set a rate (ops/sec or ops/min) for a sequence, add
 '<font color="#FF0066">: N : R</font>' just after sequence closing tag.
 This would specify repetition of N with rate of R operations/sec.
 Use '<font color="#FF0066">R/sec</font>' or
 '<font color="#FF0066">R/min</font>'
 to explicitly specify that the rate is per second or per minute.
 The default is per second,

Example -  <font color="#FF0066">[ AddDoc ] : 400 : 3</font> - would do 400
 addDoc in parallel, starting up to 3 threads per second.

Example -  <font color="#FF0066">{ AddDoc } : 100 : 200/min</font> - would
 do 100 addDoc serially,
 waiting before starting next add, if otherwise rate would exceed 200 adds/min.

10.  __Disable Counting__: Each task executed contributes to the records count.
 This count is reflected in reports under recs/s and under recsPerRun.
 Most tasks count 1, some count 0, and some count more.
 (See [Results record counting clarified](#recscounting) for more details.)
 It is possible to disable counting for a task by preceding it with <font color="#FF0066">-</font>.

Example -  <font color="#FF0066"> -CreateIndex </font> - would count 0 while
 the default behavior for CreateIndex is to count 1.

11.  __Command names__: Each class "AnyNameTask" in the
 package org.apache.lucene.benchmark.byTask.tasks,
 that extends PerfTask, is supported as command "AnyName" that can be
 used in the benchmark "algorithm" description.
 This allows to add new commands by just adding such classes.

## Supported tasks/commands

 Existing tasks can be divided into a few groups: regular index/search work tasks, report tasks, and control tasks. 

1.  __Report tasks__: There are a few Report commands for generating reports.
 Only task runs that were completed are reported.
 (The 'Report tasks' themselves are not measured and not reported.)

    *   <font color="#FF0066">RepAll</font> - all (completed) task runs.

    *   <font color="#FF0066">RepSumByName</font> - all statistics,
            aggregated by name. So, if AddDoc was executed 2000 times,
            only 1 report line would be created for it, aggregating all those
            2000 statistic records.

    *   <font color="#FF0066">RepSelectByPref   prefixWord</font> - all
            records for tasks whose name start with
            <font color="#FF0066">prefixWord</font>.

    *   <font color="#FF0066">RepSumByPref   prefixWord</font> - all
            records for tasks whose name start with
            <font color="#FF0066">prefixWord</font>,
            aggregated by their full task name.

    *   <font color="#FF0066">RepSumByNameRound</font> - all statistics,
            aggregated by name and by <font color="#FF0066">Round</font>.
            So, if AddDoc was executed 2000 times in each of 3
            <font color="#FF0066">rounds</font>, 3 report lines would be
            created for it,
            aggregating all those 2000 statistic records in each round.
            See more about rounds in the <font color="#FF0066">NewRound</font>
            command description below.

    *   <font color="#FF0066">RepSumByPrefRound   prefixWord</font> -
            similar to <font color="#FF0066">RepSumByNameRound</font>,
            just that only tasks whose name starts with
            <font color="#FF0066">prefixWord</font> are included.

 If needed, additional reports can be added by extending the abstract class
 ReportTask, and by
 manipulating the statistics data in Points and TaskStats.

2.  __Control tasks__: Few of the tasks control the benchmark algorithm
 all over:

    *   <font color="#FF0066">ClearStats</font> - clears the entire statistics.
     Further reports would only include task runs that would start after this
     call.

    *   <font color="#FF0066">NewRound</font> - virtually start a new round of
     performance test.
     Although this command can be placed anywhere, it mostly makes sense at
     the end of an outermost sequence.

This increments a global "round counter". All task runs that
     would start now would
     record the new, updated round counter as their round number.
     This would appear in reports.
     In particular, see <font color="#FF0066">RepSumByNameRound</font> above.

An additional effect of NewRound, is that numeric and boolean
     properties defined (at the head
     of the .alg file) as a sequence of values, e.g. <font color="#FF0066">
     merge.factor=mrg:10:100:10:100</font> would
     increment (cyclic) to the next value.
     Note: this would also be reflected in the reports, in this case under a
     column that would be named "mrg".

    *   <font color="#FF0066">ResetInputs</font> - DocMaker and the
     various QueryMakers
     would reset their counters to start.
     The way these Maker interfaces work, each call for makeDocument()
     or makeQuery() creates the next document or query
     that it "knows" to create.
     If that pool is "exhausted", the "maker" start over again.
     The ResetInputs command
     therefore allows to make the rounds comparable.
     It is therefore useful to invoke ResetInputs together with NewRound.

    *   <font color="#FF0066">ResetSystemErase</font> - reset all index
     and input data and call gc.
     Does NOT reset statistics. This contains ResetInputs.
     All writers/readers are nullified, deleted, closed.
     Index is erased.
     Directory is erased.
     You would have to call CreateIndex once this was called...

    *   <font color="#FF0066">ResetSystemSoft</font> -  reset all
     index and input data and call gc.
     Does NOT reset statistics. This contains ResetInputs.
     All writers/readers are nullified, closed.
     Index is NOT erased.
     Directory is NOT erased.
     This is useful for testing performance on an existing index,
     for instance if the construction of a large index
     took a very long time and now you would to test
     its search or update performance.

3.  Other existing tasks are quite straightforward and would
 just be briefly described here.

    *   <font color="#FF0066">CreateIndex</font> and
     <font color="#FF0066">OpenIndex</font> both leave the
     index open for later update operations.
     <font color="#FF0066">CloseIndex</font> would close it.

    *   <font color="#FF0066">OpenReader</font>, similarly, would
     leave an index reader open for later search operations.
     But this have further semantics.
     If a Read operation is performed, and an open reader exists,
     it would be used.
     Otherwise, the read operation would open its own reader
     and close it when the read operation is done.
     This allows testing various scenarios - sharing a reader,
     searching with "cold" reader, with "warmed" reader, etc.
     The read operations affected by this are:
     <font color="#FF0066">Warm</font>,
     <font color="#FF0066">Search</font>,
     <font color="#FF0066">SearchTrav</font> (search and traverse),
     and <font color="#FF0066">SearchTravRet</font> (search
     and traverse and retrieve).
     Notice that each of the 3 search task types maintains
     its own queryMaker instance.

    *   <font color="#FF0066">CommitIndex</font> and 
	 <font color="#FF0066">ForceMerge</font> can be used to commit
	 changes to the index then merge the index segments. The integer
   parameter specifies how many segments to merge down to (default
   1).

    *   <font color="#FF0066">WriteLineDoc</font> prepares a 'line'
	 file where each line holds a document with _title_, 
	 _date_ and _body_ elements, separated by [TAB].
	 A line file is useful if one wants to measure pure indexing
	 performance, without the overhead of parsing the data.  

	 You can use LineDocSource as a ContentSource over a 'line'
	 file.

    *   <font color="#FF0066">ConsumeContentSource</font> consumes
	 a ContentSource. Useful for e.g. testing a ContentSource
	 performance, without the overhead of preparing a Document
	 out of it.

## Benchmark properties

 Properties are read from the header of the .alg file, and define several parameters of the performance test. As mentioned above for the <font color="#FF0066">NewRound</font> task, numeric and boolean properties that are defined as a sequence of values, e.g. <font color="#FF0066">merge.factor=mrg:10:100:10:100</font> would increment (cyclic) to the next value, when NewRound is called, and would also appear as a named column in the reports (column name would be "mrg" in this example). 

 Some of the currently defined properties are: 

1.  <font color="#FF0066">analyzer</font> - full
    class name for the analyzer to use.
    Same analyzer would be used in the entire test.

2.  <font color="#FF0066">directory</font> - valid values are
    This tells which directory to use for the performance test.

3.  __Index work parameters__:
    Multi int/boolean values would be iterated with calls to NewRound.
    There would be also added as columns in the reports, first string in the
    sequence is the column name.
    (Make sure it is no shorter than any value in the sequence).

    *   <font color="#FF0066">max.buffered</font>

Example: max.buffered=buf:10:10:100:100 -
        this would define using maxBufferedDocs of 10 in iterations 0 and 1,
        and 100 in iterations 2 and 3.

    *   <font color="#FF0066">merge.factor</font> - which
        merge factor to use.

    *   <font color="#FF0066">compound</font> - whether the index is
        using the compound format or not. Valid values are "true" and "false".

 Here is a list of currently defined properties: 

1.  __Root directory for data and indexes:__

    *   work.dir (default is System property "benchmark.work.dir" or "work".)

2.  __Docs and queries creation:__

    *   analyzer

    *   doc.maker

    *   doc.maker.forever

    *   html.parser

    *   doc.stored

    *   doc.tokenized

    *   doc.term.vector

    *   doc.term.vector.positions

    *   doc.term.vector.offsets

    *   doc.store.body.bytes

    *   docs.dir

    *   query.maker

    *   file.query.maker.file

    *   file.query.maker.default.field

    *   search.num.hits

3.  __Logging__:

    *   log.step

    *   log.step.[class name]Task ie log.step.DeleteDoc (e.g. log.step.Wonderful for the WonderfulTask example above).

    *   log.queries

    *   task.max.depth.log

4.  __Index writing__:

    *   compound

    *   merge.factor

    *   max.buffered

    *   directory

    *   ram.flush.mb

5.  __Doc deletion__:

    *   doc.delete.step

6.  __Spatial__: Numerous; see spatial.alg

7.  __Task alternative packages__:

    *   alt.tasks.packages
      - comma separated list of additional packages where tasks classes will be looked for
      when not found in the default package (that of PerfTask).  If the same task class 
      appears in more than one package, the package indicated first in this list will be used.

 For sample use of these properties see the *.alg files under conf. 

## Example input algorithm and the result benchmark report

 The following example is in conf/sample.alg: <font color="#003333"># -------------------------------------------------------- # # Sample: what is the effect of doc size on indexing time? # # There are two parts in this test: # - PopulateShort adds 2N documents of length L # - PopulateLong adds N documents of length 2L # Which one would be faster? # The comparison is done twice. # # -------------------------------------------------------- </font> <font color="#990066"># ------------------------------------------------------------------------------------- # multi val params are iterated by NewRound's, added to reports, start with column name. merge.factor=mrg:10:20 max.buffered=buf:100:1000 compound=true analyzer=org.apache.lucene.analysis.standard.StandardAnalyzer directory=FSDirectory doc.stored=true doc.tokenized=true doc.term.vector=false doc.add.log.step=500 docs.dir=reuters-out doc.maker=org.apache.lucene.benchmark.byTask.feeds.SimpleDocMaker query.maker=org.apache.lucene.benchmark.byTask.feeds.SimpleQueryMaker # task at this depth or less would print when they start task.max.depth.log=2 log.queries=false # -------------------------------------------------------------------------------------</font> <font color="#3300FF">{ { "PopulateShort" CreateIndex { AddDoc(4000) > : 20000 Optimize CloseIndex > ResetSystemErase { "PopulateLong" CreateIndex { AddDoc(8000) > : 10000 Optimize CloseIndex > ResetSystemErase NewRound } : 2 RepSumByName RepSelectByPref Populate </font> 

 The command line for running this sample:   
`ant run-task -Dtask.alg=conf/sample.alg` 

 The output report from running this test contains the following: Operation round mrg buf runCnt recsPerRun rec/s elapsedSec avgUsedMem avgTotalMem PopulateShort 0 10 100 1 20003 119.6 167.26 12,959,120 14,241,792 PopulateLong - - 0 10 100 - - 1 - - 10003 - - - 74.3 - - 134.57 - 17,085,208 - 20,635,648 PopulateShort 1 20 1000 1 20003 143.5 139.39 63,982,040 94,756,864 PopulateLong - - 1 20 1000 - - 1 - - 10003 - - - 77.0 - - 129.92 - 87,309,608 - 100,831,232 

## Results record counting clarified

 Two columns in the results table indicate records counts: records-per-run and records-per-second. What does it mean? 

 Almost every task gets 1 in this count just for being executed. Task sequences aggregate the counts of their child tasks, plus their own count of 1. So, a task sequence containing 5 other task sequences, each running a single other task 10 times, would have a count of 1 + 5 * (1 + 10) = 56. 

 The traverse and retrieve tasks "count" more: a traverse task would add 1 for each traversed result (hit), and a retrieve task would additionally add 1 for each retrieved doc. So, regular Search would count 1, SearchTrav that traverses 10 hits would count 11, and a SearchTravRet task that retrieves (and traverses) 10, would count 21. 

 Confusing? this might help: always examine the `elapsedSec` column, and always compare "apples to apples", .i.e. it is interesting to check how the `rec/s` changed for the same task (or sequence) between two different runs, but it is not very useful to know how the `rec/s` differs between `Search` and `SearchTrav` tasks. For the latter, `elapsedSec` would bring more insight. 


</div>
<div> </div>