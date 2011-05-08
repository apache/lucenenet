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
using NUnit.Framework;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class SupportTestCases
    {
        [Test]
        public void Count()
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
            Type[] types = asm.GetTypes();

            int countSupport = 0;
            int countOther = 0;
            foreach (Type type in types)
            {
                object[] o1 = type.GetCustomAttributes(typeof(NUnit.Framework.TestFixtureAttribute), true);
                if (o1 == null || o1.Length == 0) continue;

                foreach (System.Reflection.MethodInfo mi in type.GetMethods())
                {
                    object[] o2 = mi.GetCustomAttributes(typeof(NUnit.Framework.TestAttribute), true);
                    if (o2 == null || o2.Length == 0) continue;

                    if (type.FullName.StartsWith("Lucene.Net.Support"))
                    {
                        countSupport++;
                    }
                    else
                    {
                        countOther++;
                    }
                }
            }
            string msg = "Lucene.Net TestCases:" + countSupport + "     Other TestCases:" + countOther;
            Console.WriteLine(msg);
            Assert.Ignore("[Intentionally ignored test case] " + msg);
        }
    }
}