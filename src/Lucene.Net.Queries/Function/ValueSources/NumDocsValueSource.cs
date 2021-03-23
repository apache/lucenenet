// Lucene version compatibility level 4.8.1
using System.Collections;
using Lucene.Net.Index;

namespace Lucene.Net.Queries.Function.ValueSources
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
    /// Returns the value of <see cref="IndexReader.NumDocs"/>
    /// for every document. This is the number of documents
    /// excluding deletions.
    /// </summary>
    public class NumDocsValueSource : ValueSource
    {
        public virtual string Name => "numdocs";

        public override string GetDescription()
        {
            return Name + "()";
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            // Searcher has no numdocs so we must use the reader instead
            return new ConstInt32DocValues(ReaderUtil.GetTopLevelContext(readerContext).Reader.NumDocs, this);
        }

        public override bool Equals(object o)
        {
            return this.GetType() == o.GetType();
        }

        public override int GetHashCode()
        {
            return this.GetType().GetHashCode();
        }
    }
}