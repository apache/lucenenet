using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Reflection;
using Lucene.Net.Index;

namespace Lucene.Net.Codecs
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

    public class TestDefaultDocValuesFormatFactory : LuceneTestCase
    {
        [Test]
        public void TestScanLucene()
        {
            var factory = new DefaultDocValuesFormatFactory();

            var docValuesFormats = factory.AvailableServices;

            assertEquals(7, docValuesFormats.Count);

            
            assertTrue(docValuesFormats.Contains("Lucene45"));
            assertTrue(docValuesFormats.Contains("Lucene42"));
            assertTrue(docValuesFormats.Contains("Lucene40"));
            assertTrue(docValuesFormats.Contains("SimpleText"));
            assertTrue(docValuesFormats.Contains("Direct"));
            assertTrue(docValuesFormats.Contains("Memory"));
            assertTrue(docValuesFormats.Contains("Disk"));
        }

        private class ScanningDocValuesFormatFactory : DefaultDocValuesFormatFactory
        {
            protected override void Initialize()
            {
                base.Initialize();
                base.ScanForDocValuesFormats(this.GetType().Assembly);
            }
        }

        [Test]
        public void TestScanCustom()
        {
            var factory = new ScanningDocValuesFormatFactory();

            var docValuesFormats = factory.AvailableServices;

            assertEquals(9, docValuesFormats.Count);

            assertTrue(docValuesFormats.Contains("Lucene45"));
            assertTrue(docValuesFormats.Contains("Lucene42"));
            assertTrue(docValuesFormats.Contains("Lucene40"));
            assertTrue(docValuesFormats.Contains("SimpleText"));
            assertTrue(docValuesFormats.Contains("Direct"));
            assertTrue(docValuesFormats.Contains("Memory"));
            assertTrue(docValuesFormats.Contains("Disk"));
            assertTrue(docValuesFormats.Contains("Public"));
            assertTrue(docValuesFormats.Contains("NotIgnored"));

            // Ensure our local Lucene40 named type overrides
            // the default.
            assertEquals(typeof(TestLucene40DocValuesFormat), factory.GetDocValuesFormat("Lucene40").GetType());
        }

        private class ExplicitDocValuesFormatFactory : DefaultDocValuesFormatFactory
        {
            protected override void Initialize()
            {
                base.Initialize();
                base.PutDocValuesFormatType(typeof(PrivateDocValuesFormat));
            }
        }

        [Test]
        public void TestPutExplicit()
        {
            var factory = new ExplicitDocValuesFormatFactory();

            var docValuesFormats = factory.AvailableServices;

            assertTrue(docValuesFormats.Contains("Private"));
        }

        private class InvalidNameDocValuesFormatFactory : DefaultDocValuesFormatFactory
        {
            protected override void Initialize()
            {
                base.Initialize();
                base.PutDocValuesFormatType(typeof(InvalidNamedDocValuesFormat));
            }
        }

        [Test]
        public void TestInvalidName()
        {
            var factory = new InvalidNameDocValuesFormatFactory();
            Assert.Throws<ArgumentException>(() => factory.GetDocValuesFormat("SomeFormat"));
        }

        private class CustomNameDocValuesFormatFactory : DefaultDocValuesFormatFactory
        {
            protected override void Initialize()
            {
                base.Initialize();
                base.PutDocValuesFormatType(typeof(CustomNamedDocValuesFormat));
            }
        }

        [Test]
        public void TestCustomName()
        {
            var factory = new CustomNameDocValuesFormatFactory();

            assertTrue(factory.AvailableServices.Contains("FooBar"));
        }

        [Test]
        public void TestRetrieve()
        {
            var factory = new DefaultDocValuesFormatFactory();

            var DocValuesFormat = factory.GetDocValuesFormat("Lucene45");

            assertNotNull(DocValuesFormat);
#pragma warning disable 612, 618
            assertEquals(typeof(Lucene45.Lucene45DocValuesFormat), DocValuesFormat.GetType());
#pragma warning restore 612, 618
        }

        [Test]
        public void TestRetrieveCustomNamed()
        {
            var factory = new CustomNameDocValuesFormatFactory();

            var DocValuesFormat = factory.GetDocValuesFormat("FooBar");

            assertNotNull(DocValuesFormat);
            assertEquals(typeof(CustomNamedDocValuesFormat), DocValuesFormat.GetType());
        }

        private class ReplaceDocValuesFormatFactory : DefaultDocValuesFormatFactory
        {
            protected override void Initialize()
            {
                base.Initialize();
                base.PutDocValuesFormatType(typeof(TestLucene40DocValuesFormat));
            }
        }

        [Test]
        public void TestReplace()
        {
            var factory = new ReplaceDocValuesFormatFactory();

            var DocValuesFormat = factory.GetDocValuesFormat("Lucene40");

            assertNotNull(DocValuesFormat);
            assertEquals(typeof(TestLucene40DocValuesFormat), DocValuesFormat.GetType());
        }

        private class CustomInstanceFactory : DefaultDocValuesFormatFactory
        {
            public override DocValuesFormat GetDocValuesFormat(string name)
            {
                if (name.Equals("ThisIsATest", StringComparison.Ordinal))
                {
                    return new NotIgnoredDocValuesFormat();
                }

                return base.GetDocValuesFormat(name);
            }

            // NOTE: Typically, this would be the only method you need to override
            // for dependency injection support.
            protected override DocValuesFormat GetDocValuesFormat(Type type)
            {
                if (type.Equals(typeof(Lucene45.Lucene45DocValuesFormat)))
                {
                    return new CustomNamedDocValuesFormat();
                }

                return base.GetDocValuesFormat(type);
            }
        }

        /// <summary>
        /// This is a test to simulate what would happen if a dependency injection
        /// container were used to supply the instance
        /// </summary>
        [Test]
        public void TestCustomInstanceByName()
        {
            var factory = new CustomInstanceFactory();

            var DocValuesFormat = factory.GetDocValuesFormat("ThisIsATest");

            assertNotNull(DocValuesFormat);
            assertEquals(typeof(NotIgnoredDocValuesFormat), DocValuesFormat.GetType());
        }

        /// <summary>
        /// This is a test to simulate what would happen if a dependency injection
        /// container were used to supply the instance
        /// </summary>
        [Test]
        public void TestCustomInstanceByType()
        {
            var factory = new CustomInstanceFactory();

            var DocValuesFormat = factory.GetDocValuesFormat("Lucene45");

            assertNotNull(DocValuesFormat);
            assertEquals(typeof(CustomNamedDocValuesFormat), DocValuesFormat.GetType());
        }
    }

    #region Test Classes
    public class PublicDocValuesFormat : DocValuesFormat
    {
        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new NotImplementedException();
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            throw new NotImplementedException();
        }
    }

    internal class PrivateDocValuesFormat : DocValuesFormat
    {
        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new NotImplementedException();
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            throw new NotImplementedException();
        }
    }

    public class NotIgnoredDocValuesFormat : DocValuesFormat
    {
        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new NotImplementedException();
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            throw new NotImplementedException();
        }
    }

    [ExcludeDocValuesFormatFromScan]
    [DocValuesFormatName("FooBar")]
    public class CustomNamedDocValuesFormat : DocValuesFormat
    {
        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new NotImplementedException();
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            throw new NotImplementedException();
        }
    }

    [ExcludeDocValuesFormatFromScan]
    [DocValuesFormatName("My-DocValuesFormat|With-Bad_Name")]
    public class InvalidNamedDocValuesFormat : DocValuesFormat
    {
        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new NotImplementedException();
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            throw new NotImplementedException();
        }
    }

    [DocValuesFormatName("Lucene40")]
    public class TestLucene40DocValuesFormat : DocValuesFormat
    {
        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new NotImplementedException();
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            throw new NotImplementedException();
        }
    }
    #endregion Test Classes
}
