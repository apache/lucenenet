using Lucene.Net.Benchmarks.ByTask.Feeds;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using System;
using System.IO;
using System.Text;

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
    /// A <see cref="WriteLineDocTask"/> which for Wikipedia input, will write category pages 
    /// to another file, while remaining pages will be written to the original file.
    /// The categories file is derived from the original file, by adding a prefix "categories-". 
    /// </summary>
    public class WriteEnwikiLineDocTask : WriteLineDocTask
    {
        private readonly TextWriter categoryLineFileOut;

        public WriteEnwikiLineDocTask(PerfRunData runData)
                  : base(runData)
        {
            Stream @out = StreamUtils.GetOutputStream(CategoriesLineFile(new FileInfo(m_fname)));
            categoryLineFileOut = new StreamWriter(@out, Encoding.UTF8);
            WriteHeader(categoryLineFileOut);
        }

        /// <summary>Compose categories line file out of original line file</summary>
        public static FileInfo CategoriesLineFile(FileInfo f)
        {
            DirectoryInfo dir = f.Directory;
            string categoriesName = "categories-" + f.Name;
            return dir is null ? new FileInfo(categoriesName) : new FileInfo(System.IO.Path.Combine(dir.FullName, categoriesName));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                categoryLineFileOut.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override TextWriter LineFileOut(Document doc)
        {
            IIndexableField titleField = doc.GetField(DocMaker.TITLE_FIELD);
            if (titleField != null && titleField.GetStringValue().StartsWith("Category:", StringComparison.Ordinal))
            {
                return categoryLineFileOut;
            }
            return base.LineFileOut(doc);
        }
    }
}
