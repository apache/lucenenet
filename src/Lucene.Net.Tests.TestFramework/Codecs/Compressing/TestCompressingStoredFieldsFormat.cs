//LUCENENET TODO: Incompatibility issues because this was ported from Lucene 8.2.0, and we are 4.8.0

//Lucene version compatibility level 8.2.0
//using Lucene.Net.Analysis;
//using Lucene.Net.Documents;
//using Lucene.Net.Index;
//using Lucene.Net.Store;
//using Lucene.Net.Support;
//using System;
//using System.Collections.Generic;

//using System.Text;

//namespace Lucene.Net.Codecs.Compressing
//{
//    /*
//    * Licensed to the Apache Software Foundation (ASF) under one or more
//    * contributor license agreements.  See the NOTICE file distributed with
//    * this work for additional information regarding copyright ownership.
//    * The ASF licenses this file to You under the Apache License, Version 2.0
//    * (the "License"); you may not use this file except in compliance with
//    * the License.  You may obtain a copy of the License at
//    *
//    *     http://www.apache.org/licenses/LICENSE-2.0
//    *
//    * Unless required by applicable law or agreed to in writing, software
//    * distributed under the License is distributed on an "AS IS" BASIS,
//    * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    * See the License for the specific language governing permissions and
//    * limitations under the License.
//    */

//    public class TestCompressingStoredFieldsFormat : BaseStoredFieldsFormatTestCase
//    {
//        const long SECOND = 1000L;
//        const long HOUR = 60 * 60 * SECOND;
//        const long DAY = 24 * HOUR;

//        protected override Codec GetCodec()
//        {
//            return CompressingCodec.RandomInstance(Random);
//        }

//        //        public void testDeletePartiallyWrittenFilesIfAbort() 
//        //        {
//        //            Directory dir = NewDirectory();
//        //    IndexWriterConfig iwConf = NewIndexWriterConfig(new MockAnalyzer(Random));
//        //        iwConf.SetMaxBufferedDocs(Random.Next(2, 30 + 1));
//        //    iwConf.SetCodec(CompressingCodec.RandomInstance(Random));
//        //    // disable CFS because this test checks file names
//        //    iwConf.SetMergePolicy(NewLogMergePolicy(false));
//        //    iwConf.SetUseCompoundFile(false);

//        //    // Cannot use RIW because this test wants CFS to stay off:
//        //    IndexWriter iw = new IndexWriter(dir, iwConf);

//        //        Document validDoc = new Document();
//        //        validDoc.Add(new IntPoint("id", 0));
//        //    validDoc.Add(new StoredField("id", 0));
//        //    iw.AddDocument(validDoc);
//        //    iw.Commit();

//        //    // make sure that #writeField will fail to trigger an abort
//        //    Document invalidDoc = new Document();
//        //        FieldType fieldType = new FieldType();
//        //        fieldType.IsStored=(true);
//        //            invalidDoc.Add(new Field("invalid", fieldType)
//        //        {

//        //            @Override
//        //      public String stringValue()
//        //            {
//        //                // TODO: really bad & scary that this causes IW to
//        //                // abort the segment!!  We should fix this.
//        //                return null;
//        //            }

//        //        });

//        //    try {
//        //      iw.AddDocument(invalidDoc);
//        //      iw.Commit();
//        //    } catch(ArgumentException iae) {
//        //      // expected
//        //      assertEquals(iae, iw.GetTragicException());
//        //}
//        // Writer should be closed by tragedy
//        //assertFalse(iw.isOpen());
//        //dir.Dispose();
//        //  }

//        public void testZFloat()
//        {
//            byte[] buffer = new byte[5]; // we never need more than 5 bytes
//            ByteArrayDataOutput @out = new ByteArrayDataOutput(buffer);
//            ByteArrayDataInput @in = new ByteArrayDataInput(buffer);

//            // round-trip small integer values
//            for (int i = short.MinValue; i < short.MaxValue; i++)
//            {
//                float f = (float)i;
//                CompressingStoredFieldsWriter.writeZFloat(@out, f);
//                @in.Reset(buffer, 0, @out.Position);
//                float g = CompressingStoredFieldsReader.readZFloat(@in);
//                assertTrue(@in.Eof);
//                assertEquals(J2N.BitConversion.SingleToInt32Bits(f), J2N.BitConversion.SingleToInt32Bits(g));

//                // check that compression actually works
//                if (i >= -1 && i <= 123)
//                {
//                    assertEquals(1, @out.Position); // single byte compression
//                }
//                @out.Reset(buffer);
//            }

//            // round-trip special values
//            float[] special = {
//        -0.0f,
//        +0.0f,
//        float.NegativeInfinity,
//        float.PositiveInfinity,
//        float.Epsilon, // LUCENENET: Epsilon in .NET is the same as MIN_VALUE in Java
//        float.MaxValue,
//        float.NaN,
//    };

//            foreach (float f in special)
//            {
//                CompressingStoredFieldsWriter.writeZFloat(out, f);
//                @in.Reset(buffer, 0, @out.Position);
//                float g = CompressingStoredFieldsReader.readZFloat(in);
//                assertTrue(@in.Eof);
//                assertEquals(J2N.BitConversion.SingleToInt32Bits(f), J2N.BitConversion.SingleToInt32Bits(g));
//                @out.Reset(buffer);
//            }

//            // round-trip random values
//            Random r = Random;
//            for (int i = 0; i < 100000; i++)
//            {
//                float f = r.nextFloat() * (Random.nextInt(100) - 50);
//                CompressingStoredFieldsWriter.writeZFloat(@out, f);
//                assertTrue("length=" + @out.Position + ", f=" + f, @out.Position <= ((J2N.BitConversion.SingleToInt32Bits(f) >>> 31) == 1 ? 5 : 4));
//                @in.Reset(buffer, 0, @out.Position);
//                float g = CompressingStoredFieldsReader.readZFloat(@in);
//                assertTrue(@in.Eof);
//                assertEquals(J2N.BitConversion.SingleToInt32Bits(f), J2N.BitConversion.SingleToInt32Bits(g));
//                @out.Reset(buffer);
//            }
//        }

//        public void testZDouble()
//        {
//            byte[] buffer = new byte[9]; // we never need more than 9 bytes
//            ByteArrayDataOutput @out = new ByteArrayDataOutput(buffer);
//            ByteArrayDataInput @in = new ByteArrayDataInput(buffer);

//            // round-trip small integer values
//            for (int i = short.MinValue; i < short.MaxValue; i++)
//            {
//                double x = (double)i;
//                CompressingStoredFieldsWriter.WriteZDouble(@out, x);
//                @in.Reset(buffer, 0, @out.Position);
//                double y = CompressingStoredFieldsReader.ReadZDouble(@in);
//                assertTrue(@in.Eof);
//                assertEquals(J2N.BitConversion.DoubleToInt64Bits(x), J2N.BitConversion.DoubleToInt64Bits(y));

//                // check that compression actually works
//                if (i >= -1 && i <= 124)
//                {
//                    assertEquals(1, @out.Position); // single byte compression
//                }
//                @out.Reset(buffer);
//            }

//            // round-trip special values
//            double[] special = {
//        -0.0d,
//        +0.0d,
//        double.NegativeInfinity,
//        double.PositiveInfinity,
//        double.Epsilon, // LUCENENET: Epsilon in .NET is the same as MIN_VALUE in Java
//        double.MaxValue,
//        double.NaN
//    };

//            foreach (double x in special)
//            {
//                CompressingStoredFieldsWriter.writeZDouble(@out, x);
//                @in.Reset(buffer, 0, @out.Position);
//                double y = CompressingStoredFieldsReader.readZDouble(@in);
//                assertTrue(@in.Eof);
//                assertEquals(J2N.BitConversion.DoubleToInt64Bits(x), J2N.BitConversion.DoubleToInt64Bits(y));
//                @out.reset(buffer);
//            }

//            // round-trip random values
//            Random r = Random;
//            for (int i = 0; i < 100000; i++)
//            {
//                double x = r.NextDouble() * (Random.nextInt(100) - 50);
//                CompressingStoredFieldsWriter.writeZDouble(@out, x);
//                assertTrue("length=" + @out.Position + ", d=" + x, @out.Position <= (x < 0 ? 9 : 8));
//                @in.Reset(buffer, 0, @out.Position);
//                double y = CompressingStoredFieldsReader.readZDouble(@in);
//                assertTrue(@in.Eof);
//                assertEquals(J2N.BitConversion.DoubleToInt64Bits(x), J2N.BitConversion.DoubleToInt64Bits(y));
//                @out.Reset(buffer);
//            }

//            // same with floats
//            for (int i = 0; i < 100000; i++)
//            {
//                double x = (double)(r.nextFloat() * (Random.nextInt(100) - 50));
//                CompressingStoredFieldsWriter.writeZDouble(out, x);
//                assertTrue("length=" + @out.Position + ", d=" + x, @out.Position <= 5);
//                @in.Reset(buffer, 0, @out.Position);
//                double y = CompressingStoredFieldsReader.readZDouble(@in);
//                assertTrue(@in.Eof);
//                assertEquals(J2N.BitConversion.DoubleToInt64Bits(x), J2N.BitConversion.DoubleToInt64Bits(y));
//                @out.Reset(buffer);
//            }
//        }

//        public void testTLong()
//        {
//            byte[] buffer = new byte[10]; // we never need more than 10 bytes
//            ByteArrayDataOutput @out = new ByteArrayDataOutput(buffer);
//            ByteArrayDataInput @in = new ByteArrayDataInput(buffer);

//            // round-trip small integer values
//            for (int i = short.MinValue; i < short.MaxValue; i++)
//            {
//                foreach (long mul in new long[] { SECOND, HOUR, DAY })
//                {
//                    long l1 = (long)i * mul;
//                    CompressingStoredFieldsWriter.writeTLong(@out, l1);
//                    @in.Reset(buffer, 0, @out.Position);
//                    long l2 = CompressingStoredFieldsReader.readTLong(@in);
//                    assertTrue(@in.Eof);
//                    assertEquals(l1, l2);

//                    // check that compression actually works
//                    if (i >= -16 && i <= 15)
//                    {
//                        assertEquals(1, @out.Position); // single byte compression
//                    }
//                    @out.Reset(buffer);
//                }
//            }

//            // round-trip random values
//            Random r = Random;
//            for (int i = 0; i < 100000; i++)
//            {
//                int numBits = r.nextInt(65);
//                long l1 = r.nextLong() & ((1L << numBits) - 1);
//                switch (r.nextInt(4))
//                {
//                    case 0:
//                        l1 *= SECOND;
//                        break;
//                    case 1:
//                        l1 *= HOUR;
//                        break;
//                    case 2:
//                        l1 *= DAY;
//                        break;
//                    default:
//                        break;
//                }
//                CompressingStoredFieldsWriter.writeTLong(@out, l1);
//                @in.Reset(buffer, 0, @out.Position);
//                long l2 = CompressingStoredFieldsReader.readTLong(@in);
//                assertTrue(@in.Eof);
//                assertEquals(l1, l2);
//                @out.Reset(buffer);
//            }
//        }

//        /**
//         * writes some tiny segments with incomplete compressed blocks,
//         * and ensures merge recompresses them.
//         */
//        public void testChunkCleanup()
//        {
//            Directory dir = NewDirectory();
//            IndexWriterConfig iwConf = NewIndexWriterConfig(new MockAnalyzer(Random));
//            iwConf.SetMergePolicy(NoMergePolicy.INSTANCE);

//            // we have to enforce certain things like maxDocsPerChunk to cause dirty chunks to be created
//            // by this test.
//            iwConf.SetCodec(CompressingCodec.RandomInstance(Random, 4 * 1024, 100, false, 8));
//            IndexWriter iw = new IndexWriter(dir, iwConf);
//            DirectoryReader ir = DirectoryReader.Open(iw);
//            for (int i = 0; i < 5; i++)
//            {
//                Document doc = new Document();
//                doc.Add(new StoredField("text", "not very long at all"));
//                iw.AddDocument(doc);
//                // force flush
//                DirectoryReader ir2 = DirectoryReader.OpenIfChanged(ir);
//                assertNotNull(ir2);
//                ir.Dispose();
//                ir = ir2;
//                // examine dirty counts:
//                foreach (LeafReaderContext leaf in ir2.leaves())
//                {
//                    CodecReader sr = (CodecReader)leaf.reader();
//                    CompressingStoredFieldsReader reader = (CompressingStoredFieldsReader)sr.getFieldsReader();
//                    assertEquals(1, reader.getNumChunks());
//                    assertEquals(1, reader.getNumDirtyChunks());
//                }
//            }
//            iw.Config.SetMergePolicy(NewLogMergePolicy());
//            iw.ForceMerge(1);
//            DirectoryReader ir2 = DirectoryReader.OpenIfChanged(ir);
//            assertNotNull(ir2);
//            ir.close();
//            ir = ir2;
//            CodecReader sr = (CodecReader)getOnlyLeafReader(ir);
//            CompressingStoredFieldsReader reader = (CompressingStoredFieldsReader)sr.getFieldsReader();
//            // we could get lucky, and have zero, but typically one.
//            assertTrue(reader.getNumDirtyChunks() <= 1);
//            ir.Dispose();
//            iw.Dispose();
//            dir.Dispose();
//        }
//    }
//}
