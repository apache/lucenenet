using System.Text;

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
    /// Parser for trec docs which selects the parser to apply according 
    /// to the source files path, defaulting to <see cref="TrecGov2Parser"/>.
    /// </summary>
    public class TrecParserByPath : TrecDocParser
    {
        public override DocData Parse(DocData docData, string name, TrecContentSource trecSrc,
            StringBuilder docBuf, ParsePathType pathType)
        {
            return pathType2parser[pathType].Parse(docData, name, trecSrc, docBuf, pathType);
        }
    }
}
