using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Benchmarks.ByTask.Feeds;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Benchmarks.ByTask.Tasks
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
    /// Simple task to test performance of tokenizers.  It just
    /// creates a token stream for each field of the document and
    /// read all tokens out of that stream.
    /// </summary>
    public class ReadTokensTask : PerfTask
    {
        public ReadTokensTask(PerfRunData runData)
            : base(runData)
        {
        }

        private int totalTokenCount = 0;

        // volatile data passed between setup(), doLogic(), tearDown().
        private Document doc = null;

        public override void Setup()
        {
            base.Setup();
            DocMaker docMaker = RunData.DocMaker;
            doc = docMaker.MakeDocument();
        }

        protected override string GetLogMessage(int recsCount)
        {
            return "read " + recsCount + " docs; " + totalTokenCount + " tokens";
        }

        public override void TearDown()
        {
            doc = null;
            base.TearDown();
        }

        public override int DoLogic()
        {
            IList<IIndexableField> fields = doc.Fields;
            Analyzer analyzer = RunData.Analyzer;
            int tokenCount = 0;
            foreach (IIndexableField field in fields)
            {
                if (!field.IndexableFieldType.IsTokenized ||
                    field is Int32Field ||
                    field is Int64Field ||
                    field is SingleField ||
                    field is DoubleField)
                {
                    continue;
                }

                using TokenStream stream = field.GetTokenStream(analyzer);
                // reset the TokenStream to the first token
                stream.Reset();

                ITermToBytesRefAttribute termAtt = stream.GetAttribute<ITermToBytesRefAttribute>();
                while (stream.IncrementToken())
                {
                    termAtt.FillBytesRef();
                    tokenCount++;
                }
                stream.End();
            }
            totalTokenCount += tokenCount;
            return tokenCount;
        }

        /// <summary>
        /// Simple StringReader that can be reset to a new string;
        /// we use this when tokenizing the string value from a
        /// Field.
        /// </summary>
        internal ReusableStringReader stringReader = new ReusableStringReader();

        internal sealed class ReusableStringReader : TextReader
        {
            private int upto;
            private int left;
            private string s;
            internal void Init(string s)
            {
                this.s = s;
                left = s.Length;
                this.upto = 0;
            }

            public override int Read()
            {
                char[] result = new char[1];
                if (Read(result, 0, 1, false) != -1)
                {
                    return result[0];
                }
                return -1;
            }
            public override int Read(char[] c, int off, int len)
            {
                return Read(c, off, len, true);
            }

            private int Read(char[] c, int off, int len, bool returnZeroWhenComplete)
            {
                if (left > len)
                {
                    s.CopyTo(upto, c, off, upto + len);
                    upto += len;
                    left -= len;
                    return len;
                }
                else if (0 == left)
                {
                    if (returnZeroWhenComplete)
                    {
                        return 0; // .NET semantics
                    }
                    return -1;
                }
                else
                {
                    s.CopyTo(upto, c, off, upto + left);
                    int r = left;
                    left = 0;
                    upto = s.Length;
                    return r;
                }
            }

            protected override void Dispose(bool disposing)
            {
                // LUCENENET: Intentionally blank
            }
        }

        /// <summary>
        /// Releases resources used by the <see cref="ReadTokensTask"/> and
        /// if overridden in a derived class, optionally releases unmanaged resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.</param>

        // LUCENENET specific
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    stringReader?.Dispose(); // LUCENENET specific - dispose stringReader and set to null
                    stringReader = null;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
