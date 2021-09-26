using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Globalization;
using System.Threading;

namespace Lucene.Net.Support
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
    /// This test was added for .NET compatibility - LUCENENET specific
    /// 
    /// It tests the Lucene.Net.Util.ToStringUtils which was untested in the Java counterpart,
    /// but required some help to ensure .NET compatibility.
    /// </summary>
    public class TestToStringUtils : LuceneTestCase
    {
        CultureInfo originalCulture;
        public override void SetUp()
        {
            base.SetUp();
            originalCulture = CultureInfo.CurrentCulture;
        }

        public override void TearDown()
        {
#if !FEATURE_CULTUREINFO_CURRENTCULTURE_SETTER
            Thread.CurrentThread.CurrentCulture = originalCulture;
#else
            CultureInfo.CurrentCulture = originalCulture;
#endif
            base.TearDown();
        }

        /// <summary>
        /// Check to ensure that the Boost function is properly converted in every possible culture.
        /// </summary>
        [Test, LuceneNetSpecific]
        public void TestBoost()
        {
            float boostNormal = 1f;
            float boostFractional = 2.5f;
            float boostNonFractional = 5f;
            float boostLong = 1.111111111f;
            float boostZeroNonFractional = 0f;
            float boostZeroFractional = 0.123f;

            var cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures | CultureTypes.NeutralCultures);

            foreach (CultureInfo culture in cultures)
            {
#if !FEATURE_CULTUREINFO_CURRENTCULTURE_SETTER
                Thread.CurrentThread.CurrentCulture = culture;
#else
                CultureInfo.CurrentCulture = culture;
#endif
                assertEquals("", ToStringUtils.Boost(boostNormal));
                assertEquals("^2.5", ToStringUtils.Boost(boostFractional));
                assertEquals("^5.0", ToStringUtils.Boost(boostNonFractional));
                assertEquals("^1.1111112", ToStringUtils.Boost(boostLong)); // LUCENENET: Confirmed this is the value returned in Java 7
                assertEquals("^0.0", ToStringUtils.Boost(boostZeroNonFractional));
                assertEquals("^0.123", ToStringUtils.Boost(boostZeroFractional));
            }
        }
    }
}
