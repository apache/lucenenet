/**
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

namespace Lucene.Net.Util
{
    using Lucene.Net.TestFramework;
    using System;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Summary description for TestVersion
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Java <see href="https://github.com/apache/lucene-solr/blob/trunk/lucene/core/src/test/org/apache/lucene/util/TestVersion.java">Source</see>
    ///     </para>
    /// </remarks>
    public class TestVersion : LuceneTestCase
    {
        #pragma warning disable 612,618
        [Test("On or after", JavaMethodName = "test()")]
        public void OnOrAfter()
        {

            var values = EnumUtil.ValuesOf<Version>();

            foreach (Version v in values)
            {
                Ok(Version.LUCENE_CURRENT.OnOrAfter(v), "LUCENE_CURRENT must be always onOrAfter(" + v + ")");
            }

            Ok(Version.LUCENE_5_0.OnOrAfter(Version.LUCENE_4_3));
            Ok(Version.LUCENE_4_3.OnOrAfter(Version.LUCENE_5_0) == false);

        }

        [Test(JavaMethodName = "testParseLenietly")]
        public void ParseLenietly()
        {
            // There isn't a C# equivelant of having static methods on enums

            Equal(Version.LUCENE_4_3, default(Version).ParseLeniently("4.3"));
            Equal(Version.LUCENE_4_3, default(Version).ParseLeniently("LUCENE_43"));
            Equal(Version.LUCENE_CURRENT, default(Version).ParseLeniently("LUCENE_CURRENT"));
        }

        [Test(JavaMethodName = "testDeprecations")]
        public void Deprecations()
        {

            var values = EnumUtil.ValuesOf<Version>();
            var type = typeof(Version);


            for (int i = 0; i < values.Count; i++)
            {
                var version = values[i];
                var name = version.ToString();
                var isObsolete = type.GetRuntimeField(name).GetCustomAttributes(false).Any(o => o is ObsoleteAttribute);

                if (i + 1 == values.Count)
                {
                    // Object.ReferenceEquals will not work for enums
                    var same = Enum.Equals(Version.LUCENE_CURRENT, version);

                    Ok(same, "Last constant must be LUCENE_CURRENT, version was {0}", name);
                }

                if (i + 2 != values.Count)
                {
                    Ok(isObsolete, "{0} should be deprecated", name);
                }
                else
                {
                    Ok(!isObsolete, "{0} should not be deprecated", name);
                }
            }
        }

        // TODO: 1.0 verify the need for this test.
        // [Test(JavaMethodName = "testAgainstMainVersionConstant")]
        public void VerifyAgainstMainVersionContant()
        {

        }

        #pragma warning restore 618, 612
     
    }
}