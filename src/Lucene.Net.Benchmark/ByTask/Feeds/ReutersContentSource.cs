using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Support.IO;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
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
    /// A <see cref="ContentSource"/> reading from the Reuters collection.
    /// <para/>
    /// Config properties:
    /// <list type="bullet">
    ///     <item><term><b>work.dir</b></term><description>path to the root of docs and indexes dirs (default <b>work</b>).</description></item>
    ///     <item><term><b>docs.dir</b></term><description>path to the docs dir (default <b>reuters-out</b>).</description></item>
    /// </list>
    /// </summary>
    public class ReutersContentSource : ContentSource
    {
        // LUCENENET specific: DateFormatInfo not used

        private DirectoryInfo dataDir = null;
        private readonly IList<FileInfo> inputFiles = new JCG.List<FileInfo>(); // LUCENENET: marked readonly
        private int nextFile = 0;
        private int iteration = 0;

        public override void SetConfig(Config config)
        {
            base.SetConfig(config);
            DirectoryInfo workDir = new DirectoryInfo(config.Get("work.dir", "work"));
            string d = config.Get("docs.dir", "reuters-out");
            dataDir = new DirectoryInfo(Path.Combine(workDir.FullName, d));
            inputFiles.Clear();
            CollectFiles(dataDir, inputFiles);
            if (inputFiles.Count == 0)
            {
                throw RuntimeException.Create("No txt files in dataDir: " + dataDir.FullName);
            }
        }

        // LUCENENET specific: DateFormatInfo not used

        private static DateTime? ParseDate(string dateStr) // LUCENENET: CA1822: Mark members as static
        {
            if (DateTime.TryParseExact(dateStr, "dd-MMM-yyyy hh:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime temp))
            {
                return temp;
            }
            else if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out temp))
            {
                return temp;
            }

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            // TODO implement?
        }

        public override DocData GetNextDocData(DocData docData)
        {
            FileInfo f = null;
            string name = null;
            UninterruptableMonitor.Enter(this);
            try
            {
                if (nextFile >= inputFiles.Count)
                {
                    // exhausted files, start a new round, unless forever set to false.
                    if (!m_forever)
                    {
                        throw new NoMoreDataException();
                    }
                    nextFile = 0;
                    iteration++;
                }
                f = inputFiles[nextFile++];
                name = f.GetCanonicalPath() + "_" + iteration;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }

            using TextReader reader = new StreamReader(new FileStream(f.FullName, FileMode.Open, FileAccess.Read), Encoding.UTF8);
            // First line is the date, 3rd is the title, rest is body
            string dateStr = reader.ReadLine();
            reader.ReadLine();// skip an empty line
            string title = reader.ReadLine();
            reader.ReadLine();// skip an empty line
            StringBuilder bodyBuf = new StringBuilder(1024);
            string line = null;
            while ((line = reader.ReadLine()) != null)
            {
                bodyBuf.Append(line).Append(' ');
            }
            reader.Dispose();


            AddBytes(f.Length);

            DateTime? date = ParseDate(dateStr.Trim());

            docData.Clear();
            docData.Name = name;
            docData.Body = bodyBuf.ToString();
            docData.Title = title;
            docData.SetDate(date);
            return docData;
        }

        public override void ResetInputs()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                base.ResetInputs();
                nextFile = 0;
                iteration = 0;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }
    }
}
