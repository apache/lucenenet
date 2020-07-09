using Lucene.Net.Util;
using NUnit.Framework;
using System;

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

    public class TestDefaultCodecFactory : LuceneTestCase
    {
        [Test]
        public void TestScanLucene()
        {
            var factory = new DefaultCodecFactory();

            var codecs = factory.AvailableServices;

            assertEquals(8, codecs.Count);

            assertTrue(codecs.Contains("Lucene46"));
            assertTrue(codecs.Contains("Lucene45"));
            assertTrue(codecs.Contains("Lucene42"));
            assertTrue(codecs.Contains("Lucene41"));
            assertTrue(codecs.Contains("Lucene40"));
            assertTrue(codecs.Contains("Lucene3x"));
            assertTrue(codecs.Contains("SimpleText"));
            assertTrue(codecs.Contains("Appending"));
        }

        private class ScanningCodecFactory : DefaultCodecFactory
        {
            protected override void Initialize()
            {
                base.Initialize();
                base.ScanForCodecs(this.GetType().Assembly);
            }
        }

        [Test]
        public void TestScanCustom()
        {
            var factory = new ScanningCodecFactory();

            var codecs = factory.AvailableServices;

            assertEquals(10, codecs.Count);

            assertTrue(codecs.Contains("Lucene46"));
            assertTrue(codecs.Contains("Lucene45"));
            assertTrue(codecs.Contains("Lucene42"));
            assertTrue(codecs.Contains("Lucene41"));
            assertTrue(codecs.Contains("Lucene40"));
            assertTrue(codecs.Contains("Lucene3x"));
            assertTrue(codecs.Contains("SimpleText"));
            assertTrue(codecs.Contains("Appending"));
            assertTrue(codecs.Contains("Public"));
            assertTrue(codecs.Contains("NotIgnored"));

            // Ensure our local Lucene40 named type overrides
            // the default.
            assertEquals(typeof(TestLucene40Codec), factory.GetCodec("Lucene40").GetType());
        }

        private class ExplicitCodecFactory : DefaultCodecFactory
        {
            protected override void Initialize()
            {
                base.Initialize();
                base.PutCodecType(typeof(PrivateCodec));
            }
        }

        [Test]
        public void TestPutExplicit()
        {
            var factory = new ExplicitCodecFactory();

            var codecs = factory.AvailableServices;

            assertTrue(codecs.Contains("Private"));
        }

        private class InvalidNameCodecFactory : DefaultCodecFactory
        {
            protected override void Initialize()
            {
                base.Initialize();
                base.PutCodecType(typeof(InvalidNamedCodec));
            }
        }

        [Test]
        public void TestInvalidName()
        {
            var factory = new InvalidNameCodecFactory();
            Assert.Throws<ArgumentException>(() => factory.GetCodec("SomeCodec"));
        }

        private class CustomNameCodecFactory : DefaultCodecFactory
        {
            protected override void Initialize()
            {
                base.Initialize();
                base.PutCodecType(typeof(CustomNamedCodec));
            }
        }

        [Test]
        public void TestCustomName()
        {
            var factory = new CustomNameCodecFactory();

            assertTrue(factory.AvailableServices.Contains("FooBar"));
        }

        [Test]
        public void TestRetrieve()
        {
            var factory = new DefaultCodecFactory();

            var codec = factory.GetCodec("Lucene45");

            assertNotNull(codec);
#pragma warning disable 612, 618
            assertEquals(typeof(Lucene45.Lucene45Codec), codec.GetType());
#pragma warning restore 612, 618
        }

        [Test]
        public void TestRetrieveCustomNamed()
        {
            var factory = new CustomNameCodecFactory();

            var codec = factory.GetCodec("FooBar");

            assertNotNull(codec);
            assertEquals(typeof(CustomNamedCodec), codec.GetType());
        }

        private class ReplaceCodecFactory : DefaultCodecFactory
        {
            protected override void Initialize()
            {
                base.Initialize();
                base.PutCodecType(typeof(TestLucene40Codec));
            }
        }

        [Test]
        public void TestReplace()
        {
            var factory = new ReplaceCodecFactory();

            var codec = factory.GetCodec("Lucene40");

            assertNotNull(codec);
            assertEquals(typeof(TestLucene40Codec), codec.GetType());
        }

        private class CustomInstanceFactory : DefaultCodecFactory
        {
            public override Codec GetCodec(string name)
            {
                if (name.Equals("ThisIsATest", StringComparison.Ordinal))
                {
                    return new NotIgnoredCodec();
                }

                return base.GetCodec(name);
            }

            // NOTE: Typically, this would be the only method you need to override
            // for dependency injection support.
            protected override Codec GetCodec(Type type)
            {
                if (type.Equals(typeof(Lucene46.Lucene46Codec)))
                {
                    return new CustomNamedCodec();
                }

                return base.GetCodec(type);
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

            var codec = factory.GetCodec("ThisIsATest");

            assertNotNull(codec);
            assertEquals(typeof(NotIgnoredCodec), codec.GetType());
        }

        /// <summary>
        /// This is a test to simulate what would happen if a dependency injection
        /// container were used to supply the instance
        /// </summary>
        [Test]
        public void TestCustomInstanceByType()
        {
            var factory = new CustomInstanceFactory();

            var codec = factory.GetCodec("Lucene46");

            assertNotNull(codec);
            assertEquals(typeof(CustomNamedCodec), codec.GetType());
        }
    }

    #region Test Classes
    public class PublicCodec : Codec
    {
        public override DocValuesFormat DocValuesFormat => throw new NotImplementedException();

        public override FieldInfosFormat FieldInfosFormat => throw new NotImplementedException();

        public override LiveDocsFormat LiveDocsFormat => throw new NotImplementedException();

        public override NormsFormat NormsFormat => throw new NotImplementedException();

        public override PostingsFormat PostingsFormat => throw new NotImplementedException();

        public override SegmentInfoFormat SegmentInfoFormat => throw new NotImplementedException();

        public override StoredFieldsFormat StoredFieldsFormat => throw new NotImplementedException();

        public override TermVectorsFormat TermVectorsFormat => throw new NotImplementedException();
    }

    internal class PrivateCodec : Codec
    {
        public override DocValuesFormat DocValuesFormat => throw new NotImplementedException();

        public override FieldInfosFormat FieldInfosFormat => throw new NotImplementedException();

        public override LiveDocsFormat LiveDocsFormat => throw new NotImplementedException();

        public override NormsFormat NormsFormat => throw new NotImplementedException();

        public override PostingsFormat PostingsFormat => throw new NotImplementedException();

        public override SegmentInfoFormat SegmentInfoFormat => throw new NotImplementedException();

        public override StoredFieldsFormat StoredFieldsFormat => throw new NotImplementedException();

        public override TermVectorsFormat TermVectorsFormat => throw new NotImplementedException();
    }

    public class NotIgnoredCodec : Codec
    {
        public override DocValuesFormat DocValuesFormat => throw new NotImplementedException();

        public override FieldInfosFormat FieldInfosFormat => throw new NotImplementedException();

        public override LiveDocsFormat LiveDocsFormat => throw new NotImplementedException();

        public override NormsFormat NormsFormat => throw new NotImplementedException();

        public override PostingsFormat PostingsFormat => throw new NotImplementedException();

        public override SegmentInfoFormat SegmentInfoFormat => throw new NotImplementedException();

        public override StoredFieldsFormat StoredFieldsFormat => throw new NotImplementedException();

        public override TermVectorsFormat TermVectorsFormat => throw new NotImplementedException();
    }

    [ExcludeCodecFromScan]
    [CodecName("FooBar")]
    public class CustomNamedCodec : Codec
    {
        public override DocValuesFormat DocValuesFormat => throw new NotImplementedException();

        public override FieldInfosFormat FieldInfosFormat => throw new NotImplementedException();

        public override LiveDocsFormat LiveDocsFormat => throw new NotImplementedException();

        public override NormsFormat NormsFormat => throw new NotImplementedException();

        public override PostingsFormat PostingsFormat => throw new NotImplementedException();

        public override SegmentInfoFormat SegmentInfoFormat => throw new NotImplementedException();

        public override StoredFieldsFormat StoredFieldsFormat => throw new NotImplementedException();

        public override TermVectorsFormat TermVectorsFormat => throw new NotImplementedException();
    }

    [ExcludeCodecFromScan]
    [CodecName("My-Codec|With-Bad_Name")]
    public class InvalidNamedCodec : Codec
    {
        public override DocValuesFormat DocValuesFormat => throw new NotImplementedException();

        public override FieldInfosFormat FieldInfosFormat => throw new NotImplementedException();

        public override LiveDocsFormat LiveDocsFormat => throw new NotImplementedException();

        public override NormsFormat NormsFormat => throw new NotImplementedException();

        public override PostingsFormat PostingsFormat => throw new NotImplementedException();

        public override SegmentInfoFormat SegmentInfoFormat => throw new NotImplementedException();

        public override StoredFieldsFormat StoredFieldsFormat => throw new NotImplementedException();

        public override TermVectorsFormat TermVectorsFormat => throw new NotImplementedException();
    }

    [CodecName("Lucene40")]
    public class TestLucene40Codec : Codec
    {
        public override DocValuesFormat DocValuesFormat => throw new NotImplementedException();

        public override FieldInfosFormat FieldInfosFormat => throw new NotImplementedException();

        public override LiveDocsFormat LiveDocsFormat => throw new NotImplementedException();

        public override NormsFormat NormsFormat => throw new NotImplementedException();

        public override PostingsFormat PostingsFormat => throw new NotImplementedException();

        public override SegmentInfoFormat SegmentInfoFormat => throw new NotImplementedException();

        public override StoredFieldsFormat StoredFieldsFormat => throw new NotImplementedException();

        public override TermVectorsFormat TermVectorsFormat => throw new NotImplementedException();
    }
    #endregion Test Classes
}
