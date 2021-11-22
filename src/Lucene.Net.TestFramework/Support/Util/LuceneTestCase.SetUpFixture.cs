using J2N.Threading.Atomic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System;
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

    public abstract partial class LuceneTestCase
    {
        /// <summary>
        /// Captures all of the OneTimeSetUp and OneTimeTearDown calls from NUnit and uses reference counting to ensure
        /// a single <see cref="LuceneTestFrameworkInitializer"/> instance is shared between all <see cref="SetUpFixture"/>
        /// test wrappers and that each of its <see cref="LuceneTestFrameworkInitializer.Initialize()"/>,
        /// <see cref="LuceneTestFrameworkInitializer.TestFrameworkSetUp()"/>
        /// <see cref="LuceneTestFrameworkInitializer.TestFrameworkTearDown()"/> methods are only executed once.
        /// </summary>
        internal class SetUpFixture
        {
            /// <summary>
            /// The singleton instance of LuceneTestFrameworkInitializer that is called before and after all tests.
            /// </summary>
            private static LuceneTestFrameworkInitializer initializer;
            private readonly static AtomicInt32 stackCount = new AtomicInt32(0);

            /// <summary>
            /// 
            /// </summary>
            /// <param name="setUpFixture">The setup wrapper fixture for the current context. This is the top level parent class. It is used to report exceptions.</param>
            /// <param name="testFixture">The setup fixture for the current tests. This is the original NUnit test fixture.
            /// We use it's assembly to scan for <see cref="LuceneTestFrameworkInitializer"/> subclasses.</param>
            public static void EnsureInitialized(Test setUpFixture, Test testFixture)
            {
                LazyInitializer.EnsureInitialized(ref initializer, () => Initialize(setUpFixture, testFixture));
            }

            private static LuceneTestFrameworkInitializer Initialize(Test setUpFixture, Test testFixture)
            {
                var initializer = CreateInitializer(setUpFixture, testFixture);

                // Allow the factories to be instantiated first. We do this to allow a
                // dependency injection step, which can only be done once. No random context
                // is availble here because we need to load the SystemProperties setup here before
                // we can read them.
                initializer.DoInitialize();

                return initializer;
            }

            [OneTimeSetUp]
            public void OneTimeSetUpWrapper()
            {
                //var fixture = TestExecutionContext.CurrentContext.CurrentTest;
                if (stackCount.GetAndIncrement() == 0)
                {
                    // Set up for assembly
                
                    // This is where a global application setup can be done that includes a randomized context,
                    // therefore LuceneTestCase.Random calls are allowed here and are repeatable.
                    initializer.DoTestFrameworkSetUp();
                }
            }

            [OneTimeTearDown]
            public void OneTimeTearDownWrapper()
            {
                if (stackCount.DecrementAndGet() == 0)
                {
                    // Tear down for assembly
                    initializer.DoTestFrameworkTearDown();
                }
            }

            private static LuceneTestFrameworkInitializer CreateInitializer(Test wrapperSetUpFixture, Test testFixture)
            {
                if (TryGetSetUpFixtureType(testFixture.TypeInfo.Assembly, out ITypeInfo setUpFixture, out Type[] candidateTypes))
                {
                    // No need for Reflection in this case.
                    if (setUpFixture.Type.Equals(typeof(LuceneTestFrameworkInitializer.DefaultLuceneTestFrameworkInitializer)))
                        return new LuceneTestFrameworkInitializer.DefaultLuceneTestFrameworkInitializer();

                    if (!IsValidFixtureType(new TypeWrapper(setUpFixture.Type), out string reason))
                    {
                        wrapperSetUpFixture.MakeInvalid(reason);
                        return new LuceneTestFrameworkInitializer.DefaultLuceneTestFrameworkInitializer();
                    }

                    try
                    {
                        return (LuceneTestFrameworkInitializer)Reflect.Construct(setUpFixture.Type);
                    }
                    catch (Exception ex)
                    {
                        var exception = ex is TargetInvocationException ? ex.InnerException : ex;

                        wrapperSetUpFixture.MakeInvalid("The LuceneTestFrameworkInitializer subclass could not be instantiated.\n\n" + exception.ToString());
                        return new LuceneTestFrameworkInitializer.DefaultLuceneTestFrameworkInitializer();
                    }
                }
                else
                {
                    wrapperSetUpFixture.MakeInvalid($"Multiple subclasses of {typeof(LuceneTestFrameworkInitializer).FullName} were found in {testFixture.TypeInfo.Assembly.FullName}. " +
                        $"Only 1 non-abstract subclass is allowed per assembly.\n\n" +
                        $"Types Found:\n" +
                        $"{string.Join("\n  ", candidateTypes.Select(t => t.FullName))}");
                    return new LuceneTestFrameworkInitializer.DefaultLuceneTestFrameworkInitializer();
                }
            }

            private static bool TryGetSetUpFixtureType(Assembly assembly, out ITypeInfo setUpFixtureType, out Type[] candidateTypes)
            {
                candidateTypes = (from assemblyType in assembly.GetTypes()
                                  where typeof(LuceneTestFrameworkInitializer).IsAssignableFrom(assemblyType)
                                     && !assemblyType.GetTypeInfo().IsAbstract
                                  select assemblyType).ToArray();

                bool result;
                switch (candidateTypes.Length)
                {
                    case 0: // The user didn't subclass, so use ours
                        candidateTypes = new Type[] { typeof(LuceneTestFrameworkInitializer.DefaultLuceneTestFrameworkInitializer) };
                        result = true;
                        break;
                    case 1: // The user created 1 subclass, so use theirs
                        result = true;
                        break;
                    default: // The user created multiple subclasses - this is invalid. Return the first type so we can mark it invalid.
                        result = false;
                        break;
                }

                setUpFixtureType = new DefaultNamespaceTypeWrapper(candidateTypes[0]);
                return result;
            }

            private static bool IsValidFixtureType(ITypeInfo typeInfo, out string reason)
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
                    typeof(OneTimeSetUpAttribute),
                    typeof(OneTimeTearDownAttribute),
                    typeof(SetUpAttribute),
                    typeof(TearDownAttribute)
                };

                foreach (Type invalidType in invalidAttributes)
                    if (typeInfo.HasMethodWithAttribute(invalidType))
                    {
                        reason = invalidType.Name + $" not allowed in a {nameof(LuceneTestFrameworkInitializer)} subclass.";
                        return false;
                    }

                if (typeInfo.IsDefined<SetUpFixtureAttribute>(inherit: true))
                {
                    reason = $"{nameof(SetUpFixtureAttribute)} not allowed on a {nameof(LuceneTestFrameworkInitializer)} subclass.";
                    return false;
                }

                reason = null;
                return true;
            }
        }
    }
}
