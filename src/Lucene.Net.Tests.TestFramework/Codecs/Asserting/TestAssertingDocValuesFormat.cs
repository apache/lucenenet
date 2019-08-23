// Lucene version compatibility level 8.2.0
using Lucene.Net.Index;

namespace Lucene.Net.Codecs.Asserting
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
    /// Test <see cref="AssertingDocValuesFormat"/> directly
    /// </summary>
#if TESTFRAMEWORK_MSTEST
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute]
#endif
    public class TestAssertingDocValuesFormat : BaseDocValuesFormatTestCase
    {
        // LUCENENET TODO: MSTest is seemingly being fixed to deal with initialization with inheritance for version 2.0. See: https://github.com/microsoft/testfx/issues/143

        // LUCENENET TODO: Message: Method Lucene.Net.Codecs.Asserting.TestAssertingDocValuesFormat.BeforeClass has wrong signature. The method must be static, public, does not return a value and should take a single parameter of type TestContext. Additionally, if you are using async-await in method then return-type must be Task.
        //#if TESTFRAMEWORK_MSTEST
        //        [Microsoft.VisualStudio.TestTools.UnitTesting.ClassInitializeAttribute]
        //#endif
        //        public override void BeforeClass()
        //        {
        //            base.BeforeClass();
        //        }

        //#if TESTFRAMEWORK_MSTEST
        //        [Microsoft.VisualStudio.TestTools.UnitTesting.ClassCleanupAttribute]
        //#endif
        //        public override void AfterClass()
        //        {
        //            base.AfterClass();
        //        }

        private readonly Codec codec = new AssertingCodec();
        protected override Codec GetCodec()
        {
            return codec;
        }
    }
}
