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

    public class TestDefaultPostingsFormatFactory : LuceneTestCase
    {
        [Test]
        public void TestScanLucene()
        {
            var factory = new DefaultPostingsFormatFactory();

            var postingsFormats = factory.AvailableServices;

            assertEquals(11, postingsFormats.Count);

            assertTrue(postingsFormats.Contains("Lucene41"));
            assertTrue(postingsFormats.Contains("Lucene40"));
            assertTrue(postingsFormats.Contains("SimpleText"));
            assertTrue(postingsFormats.Contains("Pulsing41"));
            assertTrue(postingsFormats.Contains("Direct"));
            assertTrue(postingsFormats.Contains("FSTOrd41"));
            assertTrue(postingsFormats.Contains("FSTOrdPulsing41"));
            assertTrue(postingsFormats.Contains("FST41"));
            assertTrue(postingsFormats.Contains("FSTPulsing41"));
            assertTrue(postingsFormats.Contains("Memory"));
            assertTrue(postingsFormats.Contains("BloomFilter"));
        }

        private class ScanningPostingsFormatFactory : DefaultPostingsFormatFactory
        {
            protected override void Initialize()
            {
                base.Initialize();
                base.ScanForPostingsFormats(this.GetType().Assembly);
            }
        }

        [Test]
        public void TestScanCustom()
        {
            var factory = new ScanningPostingsFormatFactory();

            var postingsFormats = factory.AvailableServices;

            assertEquals(13, postingsFormats.Count);

            assertTrue(postingsFormats.Contains("Lucene41"));
            assertTrue(postingsFormats.Contains("Lucene40"));
            assertTrue(postingsFormats.Contains("SimpleText"));
            assertTrue(postingsFormats.Contains("Pulsing41"));
            assertTrue(postingsFormats.Contains("Direct"));
            assertTrue(postingsFormats.Contains("FSTOrd41"));
            assertTrue(postingsFormats.Contains("FSTOrdPulsing41"));
            assertTrue(postingsFormats.Contains("FST41"));
            assertTrue(postingsFormats.Contains("FSTPulsing41"));
            assertTrue(postingsFormats.Contains("Memory"));
            assertTrue(postingsFormats.Contains("BloomFilter"));
            assertTrue(postingsFormats.Contains("Public"));
            assertTrue(postingsFormats.Contains("NotIgnored"));

            // Ensure our local Lucene40 named type overrides
            // the default.
            assertEquals(typeof(TestLucene40PostingsFormat), factory.GetPostingsFormat("Lucene40").GetType());
        }

        private class ExplicitPostingsFormatFactory : DefaultPostingsFormatFactory
        {
            protected override void Initialize()
            {
                base.Initialize();
                base.PutPostingsFormatType(typeof(PrivatePostingsFormat));
            }
        }

        [Test]
        public void TestPutExplicit()
        {
            var factory = new ExplicitPostingsFormatFactory();

            var postingsFormats = factory.AvailableServices;

            assertTrue(postingsFormats.Contains("Private"));
        }

        private class InvalidNamePostingsFormatFactory : DefaultPostingsFormatFactory
        {
            protected override void Initialize()
            {
                base.Initialize();
                base.PutPostingsFormatType(typeof(InvalidNamedPostingsFormat));
            }
        }

        [Test]
        public void TestInvalidName()
        {
            var factory = new InvalidNamePostingsFormatFactory();

            Assert.Throws<ArgumentException>(() => factory.GetPostingsFormat("SomeFormat"));
        }

        private class CustomNamePostingsFormatFactory : DefaultPostingsFormatFactory
        {
            protected override void Initialize()
            {
                base.Initialize();
                base.PutPostingsFormatType(typeof(CustomNamedPostingsFormat));
            }
        }

        [Test]
        public void TestCustomName()
        {
            var factory = new CustomNamePostingsFormatFactory();

            assertTrue(factory.AvailableServices.Contains("FooBar"));
        }

        [Test]
        public void TestRetrieve()
        {
            var factory = new DefaultPostingsFormatFactory();

            var PostingsFormat = factory.GetPostingsFormat("Lucene41");

            assertNotNull(PostingsFormat);
#pragma warning disable 612, 618
            assertEquals(typeof(Lucene41.Lucene41PostingsFormat), PostingsFormat.GetType());
#pragma warning restore 612, 618
        }

        [Test]
        public void TestRetrieveCustomNamed()
        {
            var factory = new CustomNamePostingsFormatFactory();

            var PostingsFormat = factory.GetPostingsFormat("FooBar");

            assertNotNull(PostingsFormat);
            assertEquals(typeof(CustomNamedPostingsFormat), PostingsFormat.GetType());
        }

        private class ReplacePostingsFormatFactory : DefaultPostingsFormatFactory
        {
            protected override void Initialize()
            {
                base.Initialize();
                base.PutPostingsFormatType(typeof(TestLucene40PostingsFormat));
            }
        }

        [Test]
        public void TestReplace()
        {
            var factory = new ReplacePostingsFormatFactory();

            var PostingsFormat = factory.GetPostingsFormat("Lucene40");

            assertNotNull(PostingsFormat);
            assertEquals(typeof(TestLucene40PostingsFormat), PostingsFormat.GetType());
        }

        private class CustomInstanceFactory : DefaultPostingsFormatFactory
        {
            public override PostingsFormat GetPostingsFormat(string name)
            {
                if (name.Equals("ThisIsATest", StringComparison.Ordinal))
                {
                    return new NotIgnoredPostingsFormat();
                }

                return base.GetPostingsFormat(name);
            }

            // NOTE: Typically, this would be the only method you need to override
            // for dependency injection support.
            protected override PostingsFormat GetPostingsFormat(Type type)
            {
#pragma warning disable 612, 618
                if (type.Equals(typeof(Lucene40.Lucene40PostingsFormat)))
#pragma warning restore 612, 618
                {
                    return new CustomNamedPostingsFormat();
                }

                return base.GetPostingsFormat(type);
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

            var PostingsFormat = factory.GetPostingsFormat("ThisIsATest");

            assertNotNull(PostingsFormat);
            assertEquals(typeof(NotIgnoredPostingsFormat), PostingsFormat.GetType());
        }

        /// <summary>
        /// This is a test to simulate what would happen if a dependency injection
        /// container were used to supply the instance
        /// </summary>
        [Test]
        public void TestCustomInstanceByType()
        {
            var factory = new CustomInstanceFactory();

            var PostingsFormat = factory.GetPostingsFormat("Lucene40");

            assertNotNull(PostingsFormat);
            assertEquals(typeof(CustomNamedPostingsFormat), PostingsFormat.GetType());
        }
    }

    #region Test Classes
    public class PublicPostingsFormat : PostingsFormat
    {
        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new NotImplementedException();
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            throw new NotImplementedException();
        }
    }

    internal class PrivatePostingsFormat : PostingsFormat
    {
        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new NotImplementedException();
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            throw new NotImplementedException();
        }
    }

    public class NotIgnoredPostingsFormat : PostingsFormat
    {
        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new NotImplementedException();
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            throw new NotImplementedException();
        }
    }

    [ExcludePostingsFormatFromScan]
    [PostingsFormatName("FooBar")]
    public class CustomNamedPostingsFormat : PostingsFormat
    {
        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new NotImplementedException();
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            throw new NotImplementedException();
        }
    }

    [ExcludePostingsFormatFromScan]
    [PostingsFormatName("My-PostingsFormat|With-Bad_Name")]
    public class InvalidNamedPostingsFormat : PostingsFormat
    {
        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new NotImplementedException();
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            throw new NotImplementedException();
        }
    }

    [PostingsFormatName("Lucene40")]
    public class TestLucene40PostingsFormat : PostingsFormat
    {
        public override FieldsConsumer FieldsConsumer(SegmentWriteState state)
        {
            throw new NotImplementedException();
        }

        public override FieldsProducer FieldsProducer(SegmentReadState state)
        {
            throw new NotImplementedException();
        }
    }
    #endregion Test Classes
}
