// LUCENENET: Rather than using AssertFinal() to run Reflection code at runtime,
// we are using a Roslyn code analyzer to ensure the rules are followed at compile time.

//using System.Diagnostics;

//namespace Lucene.Net.Tests
//{
//    using NUnit.Framework;
//    using System;
//    /*
//     * Licensed to the Apache Software Foundation(ASF) under one or more
//     * contributor license agreements.See the NOTICE file distributed with
//     * this work for additional information regarding copyright ownership.
//     * The ASF licenses this file to You under the Apache License, Version 2.0
//     * (the "License"); you may not use this file except in compliance with
//     * the License.  You may obtain a copy of the License at
//     *
//     * http://www.apache.org/licenses/LICENSE-2.0
//     *
//     * Unless required by applicable law or agreed to in writing, software
//     * distributed under the License is distributed on an "AS IS" BASIS,
//     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     * See the License for the specific language governing permissions and
//     * limitations under the License.
//     */

//    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
//    using TokenStream = Lucene.Net.Analysis.TokenStream;

//    /// <summary>
//    /// validate that assertions are enabled during tests
//    /// </summary>
//    public class TestAssertions : LuceneTestCase
//    {

//        internal class TestTokenStream1 : TokenStream
//        {
//            public sealed override bool IncrementToken()
//            {
//                return false;
//            }
//        }

//        internal sealed class TestTokenStream2 : TokenStream
//        {
//            public override bool IncrementToken()
//            {
//                return false;
//            }
//        }

//        internal class TestTokenStream3 : TokenStream
//        {
//            public override bool IncrementToken()
//            {
//                return false;
//            }
//        }

//        [Test]
//        public virtual void TestTokenStreams()
//        {
//            // In Java, an AssertionError is expected: TokenStream implementation classes or at least their incrementToken() implementation must be final

//            var a = new TestTokenStream1();
//            var b = new TestTokenStream2();
//            var doFail = false;
//            try
//            {
//                var c = new TestTokenStream3();
//                doFail = true;
//            }
//            catch (Exception e) when (e.IsAssertionError())
//            {
//                // expected
//            }
//            assertFalse("TestTokenStream3 should fail assertion", doFail);
//        }
//    }

//}