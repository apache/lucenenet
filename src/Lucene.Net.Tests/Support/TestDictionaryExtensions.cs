using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

    public class TestDictionaryExtensions : LuceneTestCase
    {
        [Test]
        public void Test_loadLSystem_IO_Stream_ArgumentNullException()
        {
            Dictionary<string, string> p = new Dictionary<string, string>();
            try
            {
                p.Load((Stream)null);
                fail("should throw NullPointerException");
            }
#pragma warning disable 168
            catch (ArgumentNullException e)
#pragma warning restore 168
            {
                // Expected
            }
        }

        /**
     * @tests java.util.Properties#load(java.io.InputStream)
     */
        [Test]
        public void Test_loadLSystem_IO_Stream()
        {
            Dictionary<string, string> prop = new Dictionary<string, string>();
            using (Stream @is = new MemoryStream(writeProperties()))
            {
                prop.Load(@is);
            }
            assertEquals("Failed to load correct properties", "harmony.tests", prop.get("test.pkg"));
            assertNull("Load failed to parse incorrectly", prop
                    .get("commented.entry"));

            prop = new Dictionary<string, string>();
            prop.Load(new MemoryStream("=".getBytes()));
            assertEquals("Failed to add empty key", "", prop.get(""));

            prop = new Dictionary<string, string>();
            prop.Load(new MemoryStream(" = ".getBytes()));
            assertEquals("Failed to add empty key2", "", prop.get(""));

            prop = new Dictionary<string, string>();
            prop.Load(new MemoryStream(" a= b".getBytes()));
            assertEquals("Failed to ignore whitespace", "b", prop.get("a"));

            prop = new Dictionary<string, string>();
            prop.Load(new MemoryStream(" a b".getBytes()));
            assertEquals("Failed to interpret whitespace as =", "b", prop.get("a"));

            prop = new Dictionary<string, string>();
            prop.Load(new MemoryStream("#comment\na=value"
                    .getBytes("UTF-8")));
            assertEquals("value", prop.get("a"));

            prop = new Dictionary<string, string>();
            prop.Load(new MemoryStream("#\u008d\u00d2\na=\u008d\u00d3"
                    .getBytes("ISO-8859-1")));
            assertEquals("Failed to parse chars >= 0x80", "\u008d\u00d3", prop
                    .get("a"));

            prop = new Dictionary<string, string>();
            prop.Load(new MemoryStream(
                    "#properties file\r\nfred=1\r\n#last comment"
                            .getBytes("ISO-8859-1")));
            assertEquals("Failed to load when last line contains a comment", "1",
                    prop.get("fred"));

            // Regression tests for HARMONY-5414
            prop = new Dictionary<string, string>();
            prop.Load(new MemoryStream("a=\\u1234z".getBytes()));

            prop = new Dictionary<string, string>();
            try
            {
                prop.Load(new MemoryStream("a=\\u123".getBytes()));
                fail("should throw IllegalArgumentException");
            }
#pragma warning disable 168
            catch (ArgumentException e)
#pragma warning restore 168
            {
                // Expected
            }

            prop = new Dictionary<string, string>();
            try
            {
                prop.Load(new MemoryStream("a=\\u123z".getBytes()));
                fail("should throw IllegalArgumentException");
            }
            catch (ArgumentException /*expected*/)
            {
                // Expected
            }

            prop = new Dictionary<string, string>();
            Dictionary<string, string> expected = new Dictionary<string, string>();
            expected.Put("a", "\u0000");
            prop.Load(new MemoryStream("a=\\".getBytes()));
            assertEquals("Failed to read trailing slash value", expected, prop);

            prop = new Dictionary<string, string>();
            expected = new Dictionary<string, string>();
            expected.Put("a", "\u1234\u0000");
            prop.Load(new MemoryStream("a=\\u1234\\".getBytes()));
            assertEquals("Failed to read trailing slash value #2", expected, prop);

            prop = new Dictionary<string, string>();
            expected = new Dictionary<string, string>();
            expected.Put("a", "q");
            prop.Load(new MemoryStream("a=\\q".getBytes()));
            assertEquals("Failed to read slash value #3", expected, prop);
        }

        /**
         * @tests java.util.Properties#load(java.io.InputStream)
         */
        [Test]
        public void Test_loadLSystem_IO_Stream_Special()
        {
            // Test for method void java.util.Properties.load(java.io.InputStream)
            Dictionary<string, string> prop = null;
            prop = new Dictionary<string, string>();
            prop.Load(new MemoryStream("=".getBytes()));
            assertTrue("Failed to add empty key", prop.get("").Equals("", StringComparison.Ordinal));

            prop = new Dictionary<string, string>();
            prop.Load(new MemoryStream("=\r\n".getBytes()));
            assertTrue("Failed to add empty key", prop.get("").Equals("", StringComparison.Ordinal));

            prop = new Dictionary<string, string>();
            prop.Load(new MemoryStream("=\n\r".getBytes()));
            assertTrue("Failed to add empty key", prop.get("").Equals("", StringComparison.Ordinal));
        }

        /**
         * @tests java.util.Properties#load(java.io.InputStream)
         */
        [Test]
        public void Test_loadLSystem_IO_Stream_subtest0()
        {
            Dictionary<string, string> props = new Dictionary<string, string>();
            using (Stream input = GetType().getResourceAsStream("hyts_PropertiesTest.properties"))
                props.Load(input);

            assertEquals("1", "\n \t \f", props.getProperty(" \r"));
            assertEquals("2", "a", props.getProperty("a"));
            assertEquals("3", "bb as,dn   ", props.getProperty("b"));
            assertEquals("4", ":: cu", props.getProperty("c\r \t\nu"));
            assertEquals("5", "bu", props.getProperty("bu"));
            assertEquals("6", "d\r\ne=e", props.getProperty("d"));
            assertEquals("7", "fff", props.getProperty("f"));
            assertEquals("8", "g", props.getProperty("g"));
            assertEquals("9", "", props.getProperty("h h"));
            assertEquals("10", "i=i", props.getProperty(" "));
            assertEquals("11", "   j", props.getProperty("j"));
            assertEquals("12", "   c", props.getProperty("space"));
            assertEquals("13", "\\", props.getProperty("dblbackslash"));
        }

        /**
     * @tests java.util.Properties#store(java.io.OutputStream, java.lang.String)
     */
        [Test]
        public void Test_storeLSystem_IO_StreamLSystem_String()
        {
            Dictionary<string, string> myProps = new Dictionary<string, string>();
            myProps.Put("Property A", " aye\\\f\t\n\r\b");
            myProps.Put("Property B", "b ee#!=:");
            myProps.Put("Property C", "see");

            MemoryStream @out = new MemoryStream();
            myProps.Store(@out, "A Header");
            @out.Dispose();

            MemoryStream @in = new MemoryStream(@out.ToArray());
            Dictionary<string, string> myProps2 = new Dictionary<string, string>();
            myProps2.Load(@in);
            @in.Dispose();

            using (var e = myProps.Keys.GetEnumerator())
            {
                String nextKey;
                while (e.MoveNext())
                {
                    nextKey = e.Current;
                    assertTrue("Stored property list not equal to original", myProps2
                        .getProperty(nextKey).Equals(myProps.getProperty(nextKey), StringComparison.Ordinal));
                }
            }
        }

        /**
        * if loading from single line like "hello" without "\n\r" neither "=", it
        * should be same as loading from "hello="
        */
        [Test]
        public void TestLoadSingleLine()
        {
            Dictionary<string, string> props = new Dictionary<string, string>();
            Stream sr = new MemoryStream("hello".getBytes());
            props.Load(sr);
            assertEquals(1, props.size());
        }

        private String comment1 = "comment1";

        private String comment2 = "comment2";

        private void validateOutput(String[] expectStrings, byte[] output)
        {
            MemoryStream bais = new MemoryStream(output);
            TextReader br = new StreamReader(bais,
                    Encoding.GetEncoding("ISO-8859-1"));
            foreach (String expectString in expectStrings)
            {
                assertEquals(expectString, br.ReadLine());
            }
            br.ReadLine();
            assertNull(br.ReadLine());
            br.Dispose();
        }

        [Test]
        public void TestStore_scenario0()
        {
            MemoryStream baos = new MemoryStream();
            Dictionary<string, string> props = new Dictionary<string, string>();
            props.Store(baos, comment1 + '\r' + comment2);
            validateOutput(new String[] { "#comment1", "#comment2" },
                    baos.ToArray());
            baos.Dispose();
        }

        [Test]
        public void TestStore_scenario1()
        {
            MemoryStream baos = new MemoryStream();
            Dictionary<string, string> props = new Dictionary<string, string>();
            props.Store(baos, comment1 + '\n' + comment2);
            validateOutput(new String[] { "#comment1", "#comment2" },
                    baos.ToArray());
            baos.Dispose();
        }

        [Test]
        public void TestStore_scenario2()
        {
            MemoryStream baos = new MemoryStream();
            Dictionary<string, string> props = new Dictionary<string, string>();
            props.Store(baos, comment1 + '\r' + '\n' + comment2);
            validateOutput(new String[] { "#comment1", "#comment2" },
                    baos.ToArray());
            baos.Dispose();
        }

        [Test]
        public void TestStore_scenario3()
        {
            MemoryStream baos = new MemoryStream();
            Dictionary<string, string> props = new Dictionary<string, string>();
            props.Store(baos, comment1 + '\n' + '\r' + comment2);
            validateOutput(new String[] { "#comment1", "#", "#comment2" },
                    baos.ToArray());
            baos.Dispose();
        }

        [Test]
        public void TestStore_scenario4()
        {
            MemoryStream baos = new MemoryStream();
            Dictionary<string, string> props = new Dictionary<string, string>();
            props.Store(baos, comment1 + '\r' + '#' + comment2);
            validateOutput(new String[] { "#comment1", "#comment2" },
                    baos.ToArray());
            baos.Dispose();
        }

        [Test]
        public void TestStore_scenario5()
        {
            MemoryStream baos = new MemoryStream();
            Dictionary<string, string> props = new Dictionary<string, string>();
            props.Store(baos, comment1 + '\r' + '!' + comment2);
            validateOutput(new String[] { "#comment1", "!comment2" },
                    baos.ToArray());
            baos.Dispose();
        }

        [Test]
        public void TestStore_scenario6()
        {
            MemoryStream baos = new MemoryStream();
            Dictionary<string, string> props = new Dictionary<string, string>();
            props.Store(baos, comment1 + '\n' + '#' + comment2);
            validateOutput(new String[] { "#comment1", "#comment2" },
                    baos.ToArray());
            baos.Dispose();
        }

        [Test]
        public void TestStore_scenario7()
        {
            MemoryStream baos = new MemoryStream();
            Dictionary<string, string> props = new Dictionary<string, string>();
            props.Store(baos, comment1 + '\n' + '!' + comment2);
            validateOutput(new String[] { "#comment1", "!comment2" },
                    baos.ToArray());
            baos.Dispose();
        }

        [Test]
        public void TestStore_scenario8()
        {
            MemoryStream baos = new MemoryStream();
            Dictionary<string, string> props = new Dictionary<string, string>();
            props.Store(baos, comment1 + '\r' + '\n' + '#' + comment2);
            validateOutput(new String[] { "#comment1", "#comment2" },
                    baos.ToArray());
            baos.Dispose();
        }

        [Test]
        public void TestStore_scenario9()
        {
            MemoryStream baos = new MemoryStream();
            Dictionary<string, string> props = new Dictionary<string, string>();
            props.Store(baos, comment1 + '\n' + '\r' + '#' + comment2);
            validateOutput(new String[] { "#comment1", "#", "#comment2" },
                    baos.ToArray());
            baos.Dispose();
        }

        [Test]
        public void TestStore_scenario10()
        {
            MemoryStream baos = new MemoryStream();
            Dictionary<string, string> props = new Dictionary<string, string>();
            props.Store(baos, comment1 + '\r' + '\n' + '!' + comment2);
            validateOutput(new String[] { "#comment1", "!comment2" },
                    baos.ToArray());
            baos.Dispose();
        }

        [Test]
        public void TestStore_scenario11()
        {
            MemoryStream baos = new MemoryStream();
            Dictionary<string, string> props = new Dictionary<string, string>();
            props.Store(baos, comment1 + '\n' + '\r' + '!' + comment2);
            validateOutput(new String[] { "#comment1", "#", "!comment2" },
                    baos.ToArray());
            baos.Dispose();
        }



        protected byte[] writeProperties()
        {
            MemoryStream bout = new MemoryStream();
            TextWriter ps = new StreamWriter(bout);
            ps.WriteLine("#commented.entry=Bogus");
            ps.WriteLine("test.pkg=harmony.tests");
            ps.WriteLine("test.proj=Automated Tests");
            ps.Dispose();
            return bout.ToArray();
        }

    }

    public static class Extensions
    {
        public static byte[] getBytes(this string input)
        {
            return Encoding.UTF8.GetBytes(input);
        }

        public static byte[] getBytes(this string input, string encoding)
        {
            return Encoding.GetEncoding(encoding).GetBytes(input);
        }

        public static string get(this IDictionary<string, string> dict, string key)
        {
            string result;
            dict.TryGetValue(key, out result);
            return result;
        }

        public static string getProperty(this IDictionary<string, string> dict, string key)
        {
            string result;
            dict.TryGetValue(key, out result);
            return result;
        }
    }
}
