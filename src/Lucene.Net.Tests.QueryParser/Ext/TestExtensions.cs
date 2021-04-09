using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.QueryParsers.Ext
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
    /// Testcase for the <see cref="Extensions"/> class
    /// </summary>
    [TestFixture]
    public class TestExtensions : LuceneTestCase
    {
        private Extensions ext;

        public override void SetUp()
        {
            base.SetUp();
            this.ext = new Extensions();
        }

        [Test]
        public virtual void TestBuildExtensionField()
        {
            assertEquals("field\\:key", ext.BuildExtensionField("key", "field"));
            assertEquals("\\:key", ext.BuildExtensionField("key"));

            ext = new Extensions('.');
            assertEquals("field.key", ext.BuildExtensionField("key", "field"));
            assertEquals(".key", ext.BuildExtensionField("key"));
        }

        [Test]
        public virtual void TestSplitExtensionField()
        {
            assertEquals("field\\:key", ext.BuildExtensionField("key", "field"));
            assertEquals("\\:key", ext.BuildExtensionField("key"));
            
            ext = new Extensions('.');
            assertEquals("field.key", ext.BuildExtensionField("key", "field"));
            assertEquals(".key", ext.BuildExtensionField("key"));
        }

        [Test]
        public virtual void TestAddGetExtension()
        {
            ParserExtension extension = new ExtensionStub();
            assertNull(ext.GetExtension("foo"));
            ext.Add("foo", extension);
            Assert.AreSame(extension, ext.GetExtension("foo"));
            ext.Add("foo", null);
            assertNull(ext.GetExtension("foo"));
        }

        [Test]
        public virtual void TestGetExtDelimiter()
        {
            assertEquals(Extensions.DEFAULT_EXTENSION_FIELD_DELIMITER, this.ext
                .ExtensionFieldDelimiter);
            ext = new Extensions('?');
            assertEquals('?', this.ext.ExtensionFieldDelimiter);
        }

        [Test]
        public virtual void TestEscapeExtension()
        {
            assertEquals("abc\\:\\?\\{\\}\\[\\]\\\\\\(\\)\\+\\-\\!\\~", ext
                .EscapeExtensionField("abc:?{}[]\\()+-!~"));
            try
            {
                ext.EscapeExtensionField(null);
                fail("should throw NPE - escape string is null");
            }
            catch (ArgumentNullException /*e*/) // LUCENENET specific - Added guard clause to throw ArgumentNullException instead of letting NullReferenceException happen.
            {
                // 
            }
        }
    }
}
