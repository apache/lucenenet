using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Cn.Smart.Support
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
    /// LUCENENET specific tests for ensuring API conventions are followed
    /// </summary>
    public class TestApiConsistency : ApiScanTestBase
    {
        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Cn.Smart.AnalyzerProfile))]
        public override void TestProtectedFieldNames(Type typeFromTargetAssembly)
        {
            base.TestProtectedFieldNames(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Cn.Smart.AnalyzerProfile))]
        public override void TestPrivateFieldNames(Type typeFromTargetAssembly)
        {
            base.TestPrivateFieldNames(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Cn.Smart.AnalyzerProfile))]
        public override void TestPublicFields(Type typeFromTargetAssembly)
        {
            base.TestPublicFields(typeFromTargetAssembly, @"^System\.Runtime\.CompilerServices");
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Cn.Smart.AnalyzerProfile))]
        public override void TestMethodParameterNames(Type typeFromTargetAssembly)
        {
            base.TestMethodParameterNames(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Cn.Smart.AnalyzerProfile))]
        public override void TestInterfaceNames(Type typeFromTargetAssembly)
        {
            base.TestInterfaceNames(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Cn.Smart.AnalyzerProfile))]
        public override void TestClassNames(Type typeFromTargetAssembly)
        {
            base.TestClassNames(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Cn.Smart.AnalyzerProfile))]
        public override void TestForPropertiesWithNoGetter(Type typeFromTargetAssembly)
        {
            base.TestForPropertiesWithNoGetter(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Cn.Smart.AnalyzerProfile))]
        public override void TestForPropertiesThatReturnArray(Type typeFromTargetAssembly)
        {
            base.TestForPropertiesThatReturnArray(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Cn.Smart.AnalyzerProfile))]
        public override void TestForMethodsThatReturnWritableArray(Type typeFromTargetAssembly)
        {
            base.TestForMethodsThatReturnWritableArray(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Cn.Smart.AnalyzerProfile))]
        public override void TestForPublicMembersContainingComparer(Type typeFromTargetAssembly)
        {
            base.TestForPublicMembersContainingComparer(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Cn.Smart.AnalyzerProfile))]
        public override void TestForPublicMembersNamedSize(Type typeFromTargetAssembly)
        {
            base.TestForPublicMembersNamedSize(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Cn.Smart.AnalyzerProfile))]
        public override void TestForPublicMembersContainingNonNetNumeric(Type typeFromTargetAssembly)
        {
            base.TestForPublicMembersContainingNonNetNumeric(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Cn.Smart.AnalyzerProfile))]
        public override void TestForTypesContainingNonNetNumeric(Type typeFromTargetAssembly)
        {
            base.TestForTypesContainingNonNetNumeric(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Cn.Smart.AnalyzerProfile))]
        public override void TestForPublicMembersWithNullableEnum(Type typeFromTargetAssembly)
        {
            base.TestForPublicMembersWithNullableEnum(typeFromTargetAssembly);
        }

        // LUCENENET NOTE: This test is only for identifying members who were changed from
        // ICollection, IList or ISet to IEnumerable during the port (that should be changed back)
        //[Test, LuceneNetSpecific]
        //[TestCase(typeof(Lucene.Net.Analysis.Cn.Smart.AnalyzerProfile))]
        //public override void TestForMembersAcceptingOrReturningIEnumerable(Type typeFromTargetAssembly)
        //{
        //    base.TestForMembersAcceptingOrReturningIEnumerable(typeFromTargetAssembly);
        //}

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Cn.Smart.AnalyzerProfile))]
        public override void TestForMembersAcceptingOrReturningListOrDictionary(Type typeFromTargetAssembly)
        {
            base.TestForMembersAcceptingOrReturningListOrDictionary(typeFromTargetAssembly);
        }
    }
}
