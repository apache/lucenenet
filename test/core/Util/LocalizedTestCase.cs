/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Lucene.Net.Util
{

    /// <summary> Base test class for Lucene test classes that test Locale-sensitive behavior.
    /// <p/>
    /// This class will run tests under the default Locale, but then will also run
    /// tests under all available JVM locales. This is helpful to ensure tests will
    /// not fail under a different environment.
    /// </summary>
    public class LocalizedTestCase : LuceneTestCase
    {
        /// <summary> An optional limited set of testcases that will run under different Locales.</summary>
        private readonly HashSet<string> testWithDifferentLocales;

        public LocalizedTestCase()
        {
            testWithDifferentLocales = null;
        }

        public LocalizedTestCase(System.String name)
            : base(name)
        {
            testWithDifferentLocales = null;
        }

        public LocalizedTestCase(HashSet<string> testWithDifferentLocales)
        {
            this.testWithDifferentLocales = testWithDifferentLocales;
        }

        public LocalizedTestCase(System.String name, HashSet<string> testWithDifferentLocales)
            : base(name)
        {
            this.testWithDifferentLocales = testWithDifferentLocales;
        }

        // @Override
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        // @Override
        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }
        
        [Test]
        public void RunLocalizedTests()
        {
            // No need to test with default locale.  Already done when actualy test was called by NUnit
            var currentMethodName = MethodBase.GetCurrentMethod().Name;


            // Get all the methods, and if there is a list of specific methods
            // to test, only use those.
            IEnumerable<MethodInfo> methodList = GetType().GetMethods();
            if(testWithDifferentLocales != null)
            {
                methodList = methodList.Where(mi => testWithDifferentLocales.Contains(mi.Name));
            }

            // Only get methods that have a TestAttribute on them...Ignore the rest
            var methodsToTest = methodList.Where(mi => mi.Name != currentMethodName)
                                        .Where(mi => mi.GetCustomAttributes(typeof (TestAttribute), true).Any())
                                        .ToList();

            // Get a list of all locales to run the test against
            var systemLocales = CultureInfo.GetCultures(CultureTypes.InstalledWin32Cultures);

            // Store the original cultures used, so they can be restored
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUICulture = CultureInfo.CurrentUICulture;
            try
            {
                // Do the test again under different Locales
                foreach (CultureInfo t in systemLocales)
                {
                    // Set the new test culture
                    System.Threading.Thread.CurrentThread.CurrentCulture = t;
                    System.Threading.Thread.CurrentThread.CurrentUICulture = t;

                    foreach (var test in methodsToTest)
                    {
                        try
                        {
                            test.Invoke(this, null);
                        }
                        catch (Exception)
                        {
                            Console.Out.WriteLine("Test failure of '" + test.Name + "' occurred under a different Locale " + t.Name);
                            throw;
                        }
                    }
                }
            }
            finally
            {
                // Restore the cultures
                System.Threading.Thread.CurrentThread.CurrentCulture = originalCulture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = originalUICulture;
            }
        }
    }
}