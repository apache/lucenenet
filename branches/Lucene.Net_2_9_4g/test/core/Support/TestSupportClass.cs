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
using System.Dynamic;
using Lucene.Net.Util;
using NUnit.Framework;



namespace Lucene.Net.Support
{
    /// <summary>
    /// </summary>
    [TestFixture]
    public class TestSupportClass
    {
        /// <summary></summary>
        /// <throws></throws>
        [Test]
        public virtual void TestCRC32()
        {
            byte[] b = new byte[256];
            for (int i = 0; i < b.Length; i++)
                b[i] = (byte)i;

            Support.Checksum digest = new Support.CRC32();
            digest.Update(b, 0, b.Length);

            Int64 expected = 688229491;
            Assert.AreEqual(expected, digest.GetValue());
        }
    }

    internal class BigObject
    {
        public int i = 0;
        public byte[] buf = null;

        public BigObject(int i)
        {
            this.i = i;
            buf = new byte[1024 * 1024]; //1MB
        }
    }

    internal class SmallObject
    {
        public int i = 0;

        public SmallObject(int i)
        {
            this.i = i;
        }
    }
}


namespace Lucene.Net
{
    /// <summary>
    /// Support for junit.framework.TestCase.getName().
    /// {{Lucene.Net-2.9.1}} Move to another location after LUCENENET-266
    /// </summary>
    public class TestCase
    {
        public static string GetName()
        {
            return GetTestCaseName(false);
        }

        public static string GetFullName()
        {
            return GetTestCaseName(true);
        }

        static string GetTestCaseName(bool fullName)
        {
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                System.Reflection.MethodBase method = stackTrace.GetFrame(i).GetMethod();
                object[] testAttrs = method.GetCustomAttributes(typeof(NUnit.Framework.TestAttribute), false);
                if (testAttrs != null && testAttrs.Length > 0)
                    if (fullName) return method.DeclaringType.FullName + "." + method.Name;
                    else return method.Name;
            }
            return "GetTestCaseName[UnknownTestMethod]";
        }
    }
}