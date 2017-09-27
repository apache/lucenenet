using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Support;
using Lucene.Net.Support.IO;
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
        public class Iterator : IEnumerator<FileInfo>
        {

            private class Comparer : IComparer<FileInfo>
            {
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

            private Comparer c = new Comparer();

            private FileInfo current;

            public Iterator(DirectoryInfo f)
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

            public virtual int Count
            {
                get { return count; }
            }

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

            public virtual FileInfo Current
            {
                get { return current; }
            }

            object IEnumerator.Current
            {
                get { return current; }
            }

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
        private Iterator inputFiles = null;

        private DateTime? ParseDate(string dateStr)
        {
            DateTime temp;
            if (DateTime.TryParseExact(dateStr, "dd-MMM-yyyy hh:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out temp))
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
            if (disposing)
            {
                inputFiles = null;
            }
        }

        public override DocData GetNextDocData(DocData docData)
        {
            FileInfo f = null;
            string name = null;
            lock (this)
            {
                if (!inputFiles.MoveNext())
                {
                    // exhausted files, start a new round, unless forever set to false.
                    if (!m_forever)
                    {
                        throw new NoMoreDataException();
                    }
                    inputFiles = new Iterator(dataDir);
                    iteration++;
                }
                f = inputFiles.Current;
                // System.err.println(f);
                name = f.GetCanonicalPath() + "_" + iteration;
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
            lock (this)
            {
                base.ResetInputs();
                inputFiles = new Iterator(dataDir);
                iteration = 0;
            }
        }

        public override void SetConfig(Config config)
        {
            base.SetConfig(config);

            DirectoryInfo workDir = new DirectoryInfo(config.Get("work.dir", "work"));
            string d = config.Get("docs.dir", "dir-out");
            dataDir = new DirectoryInfo(d);

            inputFiles = new Iterator(dataDir);

            if (inputFiles == null)
            {
                throw new Exception("No txt files in dataDir: " + dataDir.FullName);
            }
        }
    }
}
