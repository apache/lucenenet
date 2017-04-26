/*
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

using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Base class for tests from JDK/nio/Basic.java
    /// </summary>
    public class BaseBufferTestCase : LuceneTestCase
    {
        internal static TextWriter output = VERBOSE ? Console.Out : new NullWriter();

        internal static long Ic(int i)
        {
            int j = i % 54;
            return j + 'a' + ((j > 26) ? 128 : 0);
        }

        internal static string ToString(Lucene.Net.Support.Buffer b)
        {
            return (b.GetType().Name
                    + "[pos=" + b.Position
                    + " lim=" + b.Limit
                    + " cap=" + b.Capacity
                    + "]");
        }

        internal static void Show(int level, Lucene.Net.Support.Buffer b)
        {
            for (int i = 0; i < level; i++)
                output.Write("  ");
            output.WriteLine(ToString(b) + " " + b.GetHashCode().ToString("x4", CultureInfo.InvariantCulture));
        }

        //static void fail(string s)
        //{
        //    Assert.Fail(s);
        //}

        internal static void fail(string s, Lucene.Net.Support.Buffer b)
        {
            fail(s + ": " + ToString(b));
        }

        internal static void fail(String s, Lucene.Net.Support.Buffer b, Lucene.Net.Support.Buffer b2)
        {
            fail(s + ": "
                                       + ToString(b) + ", " + ToString(b2));
        }

        internal static void fail(Lucene.Net.Support.Buffer b,
                         string expected, char expectedChar,
                         string got, char gotChar)
        {
            if (b is ByteBuffer) {
                ByteBuffer bb = (ByteBuffer)b;
                int n = Math.Min(16, bb.Limit);
                for (int i = 0; i < n; i++)
                output.Write(" " + (bb.Get(i) & 0xff).ToString("x4", CultureInfo.InvariantCulture));
            output.WriteLine();
            }
            //if (b is CharBuffer) {
            //    CharBuffer bb = (CharBuffer)b;
            //    int n = Math.Min(16, bb.Limit);
            //    for (int i = 0; i < n; i++)
            //        output.Write(" " + (bb.get(i) & 0xffff).ToString("x4", CultureInfo.InvariantCulture));
            //    output.WriteLine();
            //}
            Assert.Fail(ToString(b)
                                       + ": Expected '" + expectedChar + "'=0x"
                                       + expected
                                       + ", got '" + gotChar + "'=0x"
                                       + got);
        }

        internal static void fail(Lucene.Net.Support.Buffer b, long expected, long got)
        {
            fail(b,
                 (expected).ToString("x4", CultureInfo.InvariantCulture), (char)expected,
                 (got).ToString("x4", CultureInfo.InvariantCulture), (char)got);
        }

        internal static void ck(Lucene.Net.Support.Buffer b, bool cond)
        {
            if (!cond)
                fail("Condition failed", b);
        }

        internal static void ck(Lucene.Net.Support.Buffer b, long got, long expected)
        {
            if (expected != got)
                fail(b, expected, got);
        }

        internal static void ck(Lucene.Net.Support.Buffer b, float got, float expected)
        {
            if (expected != got)
                fail(b,
                     expected.ToString("0.0##########", CultureInfo.InvariantCulture), (char)expected,
                     got.ToString("0.0##########", CultureInfo.InvariantCulture), (char)got);
        }

        internal static void ck(Lucene.Net.Support.Buffer b, double got, double expected)
        {
            if (expected != got)
                fail(b,
                     expected.ToString("0.0##########", CultureInfo.InvariantCulture), (char)expected,
                     got.ToString("0.0##########", CultureInfo.InvariantCulture), (char)got);
        }

        /// <summary>
        /// A simple writer that implements the null object pattern
        /// that we can swap in when verbosity is turned off.
        /// </summary>
        private class NullWriter : TextWriter
        {
            public override Encoding Encoding
            {
                get
                {
                    return Encoding.UTF8;
                }
            }

            public override void Write(char value)
            {
                
            }
        }
    }
}
