/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using Lucene.Net.Util;

namespace Lucene.Net.Support
{
    /// <summary>
    /// LUCENENET specific for mimicking the JUnit rule functionality.
    /// We simplify things by just running the rules inside LuceneTestCase.
    /// </summary>
    public abstract class AbstractBeforeAfterRule
    {
        public virtual void Before(
#if !FEATURE_STATIC_TESTDATA_INITIALIZATION
            LuceneTestCase testInstance
#endif
            )
        {
        }

        public virtual void After(
#if !FEATURE_STATIC_TESTDATA_INITIALIZATION
            LuceneTestCase testInstance
#endif
            )
        {
        }
    }
}
