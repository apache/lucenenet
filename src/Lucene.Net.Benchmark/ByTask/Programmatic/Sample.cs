using Lucene.Net.Benchmarks.ByTask.Tasks;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Support;
using System.Collections.Generic;

namespace Lucene.Net.Benchmarks.ByTask.Programmatic
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Sample performance test written programmatically - no algorithm file is needed here.
    /// </summary>
    public class Sample
    {
        public static void Main(string[] args)
        {
            var p = InitProps();
            Config conf = new Config(p);
            PerfRunData runData = new PerfRunData(conf);

            // 1. top sequence
            TaskSequence top = new TaskSequence(runData, null, null, false); // top level, not parallel

            // 2. task to create the index
            CreateIndexTask create = new CreateIndexTask(runData);
            top.AddTask(create);

            // 3. task seq to add 500 docs (order matters - top to bottom - add seq to top, only then add to seq)
            TaskSequence seq1 = new TaskSequence(runData, "AddDocs", top, false);
            seq1.SetRepetitions(500);
            seq1.SetNoChildReport();
            top.AddTask(seq1);

            // 4. task to add the doc
            AddDocTask addDoc = new AddDocTask(runData);
            //addDoc.setParams("1200"); // doc size limit if supported
            seq1.AddTask(addDoc); // order matters 9see comment above)

            // 5. task to close the index
            CloseIndexTask close = new CloseIndexTask(runData);
            top.AddTask(close);

            // task to report
            RepSumByNameTask rep = new RepSumByNameTask(runData);
            top.AddTask(rep);

            // print algorithm
            SystemConsole.WriteLine(top.ToString());

            // execute
            top.DoLogic();
        }

        // Sample programmatic settings. Could also read from file.
        private static IDictionary<string, string> InitProps()
        {
            var p = new Dictionary<string, string>();
            p["task.max.depth.log"] = "3";
            p["max.buffered"] = "buf:10:10:100:100:10:10:100:100";
            p["doc.maker"] = "Lucene.Net.Benchmarks.ByTask.Feeds.ReutersContentSource, Lucene.Net.Benchmark";
            p["log.step"] = "2000";
            p["doc.delete.step"] = "8";
            p["analyzer"] = "Lucene.Net.Analysis.Standard.StandardAnalyzer, Lucene.Net.Analysis.Common";
            p["doc.term.vector"] = "false";
            p["directory"] = "FSDirectory";
            p["query.maker"] = "Lucene.Net.Benchmarks.ByTask.Feeds.ReutersQueryMaker, Lucene.Net.Benchmark";
            p["doc.stored"] = "true";
            p["docs.dir"] = "reuters-out";
            p["compound"] = "cmpnd:true:true:true:true:false:false:false:false";
            p["doc.tokenized"] = "true";
            p["merge.factor"] = "mrg:10:100:10:100:10:100:10:100";
            return p;
        }
    }
}
