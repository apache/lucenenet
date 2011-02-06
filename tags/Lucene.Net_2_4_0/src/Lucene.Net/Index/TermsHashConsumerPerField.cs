/**
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

using Fieldable = Lucene.Net.Documents.Fieldable;
using Token = Lucene.Net.Analysis.Token;

namespace Lucene.Net.Index
{
    /** Implement this class to plug into the TermsHash
     *  processor, which inverts & stores Tokens into a hash
     *  table and provides an API for writing bytes into
     *  multiple streams for each unique Token. */
    internal abstract class TermsHashConsumerPerField
    {
        internal abstract  bool start(Fieldable[] fields, int count);
        internal abstract  void finish();
        internal abstract  void skippingLongTerm(Token t);
        internal abstract  void newTerm(Token t, RawPostingList p);
        internal abstract  void addTerm(Token t, RawPostingList p);
        internal abstract  int getStreamCount();
    }
}
