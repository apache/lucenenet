#if FEATURE_SERIALIZABLE_EXCEPTIONS
using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Runtime.Serialization;

namespace Lucene.Net.Support.ExceptionHandling
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

    [LuceneNetSpecific]
    public class TestExceptionSerialization : ExceptionScanningTestCase
    {
        [Test]
        public void TestCanSerialize([ValueSource("LuceneExceptionTypes")] Type luceneException)
        {
            var instance = TryInstantiate(luceneException);
            Assert.That(TypeCanSerialize(instance, out SerializationException se), $"Unable to serialize {luceneException.FullName}:\n\n{se}");
        }
    }
}
#endif