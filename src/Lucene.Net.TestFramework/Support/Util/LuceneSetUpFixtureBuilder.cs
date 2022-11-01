using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Lucene.Net.Util
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
    /// Custom class to build the assembly-wide setup/teardown. NUnit doesn't scan attributes from dependent assemblies,
    /// so this is a workaround to inject our initializer so we can get a teardown for the assembly.
    /// </summary>
    internal class LuceneSetUpFixtureBuilder
    {
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design")]
        public TestSuite BuildFrom(ITypeInfo typeInfo)
        {
            var setUpFixtureType = new DefaultNamespaceTypeWrapper(typeof(LuceneTestCase.SetUpFixture));
            SetUpFixture fixture = new SetUpFixture(setUpFixtureType);

            if (fixture.RunState != RunState.NotRunnable)
            {
                string reason = null;
                if (!IsValidFixtureType(setUpFixtureType, ref reason))
                    fixture.MakeInvalid(reason);
            }

            fixture.ApplyAttributesToTest(setUpFixtureType.Type.GetTypeInfo());

            return fixture;
        }

        #region Helper Methods

        private static bool IsValidFixtureType(ITypeInfo typeInfo, ref string reason)
        {
            if (!typeInfo.IsStaticClass)
            {
                if (typeInfo.IsAbstract)
                {
                    reason = string.Format("{0} is an abstract class", typeInfo.FullName);
                    return false;
                }

                if (!typeInfo.HasConstructor(new Type[0]))
                {
                    reason = string.Format("{0} does not have a default constructor", typeInfo.FullName);
                    return false;
                }
            }

            var invalidAttributes = new Type[] {
                typeof(SetUpAttribute),
                typeof(TearDownAttribute)
            };

            foreach (Type invalidType in invalidAttributes)
                if (typeInfo.HasMethodWithAttribute(invalidType))
                {
                    reason = invalidType.Name + $" attribute not allowed in a {nameof(LuceneTestCase.SetUpFixture)}.";
                    return false;
                }

            return true;
        }

        #endregion
    }
}
