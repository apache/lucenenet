using J2N.Text;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Support.IO;
using Lucene.Net.Support.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

// LUCENENET TODO: This had to be refactored significantly. We need tests to confirm it works.

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
    /// A <see cref="ContentSource"/> using the Dir collection for its input. Supports
    /// the following configuration parameters (on top of <see cref="ContentSource"/>):
    /// <list type="bullet">
    ///     <item><term>work.dir</term><description>specifies the working directory. Required if "docs.dir" denotes a relative path (<b>default=work</b>).</description></item>
    ///     <item><term>docs.dir</term><description>specifies the directory the Dir collection. Can be set to a relative path if "work.dir" is also specified (<b>default=dir-out</b>).</description></item>
    /// </list>
    /// </summary>
    public class DirContentSource : ContentSource
    {
        /// <summary>
        /// Iterator over the files in the directory.
        /// </summary>
        public class Enumerator : IEnumerator<FileInfo>
        {

            private class Comparer : IComparer<FileInfo>
            {
                private Comparer() { } // LUCENENET: Made into singleton

                public static IComparer<FileInfo> Default { get; } = new Comparer();

                public int Compare(FileInfo a, FileInfo b)
                {
                    string a2 = a.ToString();
                    string b2 = b.ToString();
                    int diff = a2.Length - b2.Length;

                    if (diff > 0)
                    {
                        while (diff-- > 0)
                        {
                            b2 = "0" + b2;
                        }
                    }
                    else if (diff < 0)
                    {
                        diff = -diff;
                        while (diff-- > 0)
                        {
                            a2 = "0" + a2;
                        }
                    }

                    /* note it's reversed because we're going to push,
                       which reverses again */
                    return b2.CompareToOrdinal(a2);
                }
            }

            internal int count = 0;

            internal Stack<FileInfo> stack = new Stack<FileInfo>();

            /* this seems silly ... there must be a better way ...
               not that this is good, but can it matter? */

            private readonly IComparer<FileInfo> c = Comparer.Default; // LUCENENET: marked readonly

            private FileInfo current;

            public Enumerator(DirectoryInfo f)
            {
                Push(f);
            }

            internal void Push(DirectoryInfo f)
            {
                foreach (var dir in f.GetDirectories())
                {
                    Push(dir);
                }

                Push(f.GetFiles("*.txt"));
            }

            internal void Push(FileInfo[] files)
            {
                Array.Sort(files, c);
                for (int i = 0; i < files.Length; i++)
                {
                    // System.err.println("push " + files[i]);
                    stack.Push(files[i]);
                }
            }

            public virtual int Count => count;

            public virtual bool MoveNext()
            {
                if (stack.Count == 0)
                {
                    current = null;
                    return false;
                }
                count++;
                current = stack.Pop();
                // System.err.println("pop " + object);
                return true;
            }

            public virtual FileInfo Current => current;

            object IEnumerator.Current => current;

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
            }

            public virtual void Reset()
            {
            }
        }

        private DirectoryInfo dataDir = null;
        private int iteration = 0;
        private Enumerator inputFiles = null;

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

        /// <summary>
        /// Releases resources used by the <see cref="DirContentSource"/> and
        /// if overridden in a derived class, optionally releases unmanaged resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inputFiles?.Dispose(); // LUCENENET specific - dispose inputFiles
                inputFiles = null;
            }
        }

        public override DocData GetNextDocData(DocData docData)
        {
            FileInfo f = null;
            string name = null;
            UninterruptableMonitor.Enter(this);
            try
            {
                if (!inputFiles.MoveNext())
                {
                    // exhausted files, start a new round, unless forever set to false.
                    if (!m_forever)
                    {
                        throw new NoMoreDataException();
                    }
                    inputFiles = new Enumerator(dataDir);
                    iteration++;
                }
                f = inputFiles.Current;
                // System.err.println(f);
                name = f.GetCanonicalPath() + "_" + iteration;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }

            string line = null;
            string dateStr;
            string title;
            StringBuilder bodyBuf = new StringBuilder(1024);

            using (TextReader reader = new StreamReader(new FileStream(f.FullName, FileMode.Open, FileAccess.Read), Encoding.UTF8))
            {
                //First line is the date, 3rd is the title, rest is body
                dateStr = reader.ReadLine();
                reader.ReadLine();//skip an empty line
                title = reader.ReadLine();
                reader.ReadLine();//skip an empty line
                while ((line = reader.ReadLine()) != null)
                {
                    bodyBuf.Append(line).Append(' ');
                }
            }
            AddBytes(f.Length);

            DateTime? date = ParseDate(dateStr);

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
                inputFiles = new Enumerator(dataDir);
                iteration = 0;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        public override void SetConfig(Config config)
        {
            base.SetConfig(config);

            DirectoryInfo workDir = new DirectoryInfo(config.Get("work.dir", "work"));
            string d = config.Get("docs.dir", "dir-out");

            if (Path.IsPathRooted(d))
                dataDir = new DirectoryInfo(d);
            else
                dataDir = new DirectoryInfo(Path.Combine(workDir.FullName, d));

            inputFiles = new Enumerator(dataDir);

            if (inputFiles is null)
            {
                throw RuntimeException.Create("No txt files in dataDir: " + dataDir.FullName);
            }
        }
    }
}
