using Lucene.Net.Index;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Benchmarks.Quality.Utils
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
    /// Utility: extract doc names from an index
    /// </summary>
    public class DocNameExtractor
    {
        private readonly string docNameField;

        /// <summary>
        /// Constructor for <see cref="DocNameExtractor"/>.
        /// </summary>
        /// <param name="docNameField">name of the stored field containing the doc name.</param>
        public DocNameExtractor(string docNameField)
        {
            this.docNameField = docNameField;
        }

        /// <summary>
        /// Extract the name of the input doc from the index.
        /// </summary>
        /// <param name="searcher">access to the index.</param>
        /// <param name="docid">ID of doc whose name is needed.</param>
        /// <returns>the name of the input doc as extracted from the index.</returns>
        /// <exception cref="IOException">if cannot extract the doc name from the index.</exception>
        public virtual string DocName(IndexSearcher searcher, int docid)
        {
            IList<string> name = new JCG.List<string>();
            searcher.IndexReader.Document(docid, new StoredFieldVisitorAnonymousClass(this, name));

            return name.Count > 0 ? name[0] : null;
        }

        private sealed class StoredFieldVisitorAnonymousClass : StoredFieldVisitor
        {
            private readonly DocNameExtractor outerInstance;
            private readonly IList<string> name;

            public StoredFieldVisitorAnonymousClass(DocNameExtractor outerInstance, IList<string> name)
            {
                this.outerInstance = outerInstance;
                this.name = name;
            }
            public override void StringField(FieldInfo fieldInfo, string value)
            {
                name.Add(value);
            }

            public override Status NeedsField(FieldInfo fieldInfo)
            {
                if (name.Count > 0)
                {
                    return Status.STOP;
                }
                else if (fieldInfo.Name.Equals(outerInstance.docNameField, StringComparison.Ordinal))
                {
                    return Status.YES;
                }
                else
                {
                    return Status.NO;
                }
            }
        }
    }
}
