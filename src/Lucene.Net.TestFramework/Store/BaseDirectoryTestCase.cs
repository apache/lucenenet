// Lucene version compatibility level 8.2.0
// LUCENENET NOTE: This class now exists both here and in Lucene.Net.Tests
using J2N.Threading;
using Lucene.Net.Attributes;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using RandomizedTesting.Generators;
using System;
using System.IO;
using System.Threading;
using Assert = Lucene.Net.TestFramework.Assert;
using Test = NUnit.Framework.TestAttribute;

namespace Lucene.Net.Store
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
    /// Base class for per-Directory tests.
    /// </summary>
    public abstract class BaseDirectoryTestCase : LuceneTestCase
    {
        /// <summary>
        /// A subclass returns the <see cref="Directory"/> to be tested; if it's
        /// an FS-based directory it should point to the specified
        /// path, else it can ignore it.
        /// </summary>
        protected abstract Directory GetDirectory(DirectoryInfo path);

        // LUCENENET: This test is not compatible with 4.8.0, as it was ported from 8.2.0
        //[Test]
        //public virtual void TestCopyFrom()
        //{
        //    using (Directory source = GetDirectory(CreateTempDir("testCopy")))
        //    using (Directory dest = NewDirectory())
        //    {
        //        RunCopyFrom(source, dest);
        //    }

        //    using (Directory source = NewDirectory())
        //    using (Directory dest = GetDirectory(CreateTempDir("testCopyDestination")))
        //    {
        //        RunCopyFrom(source, dest);
        //    }
        //}

        //private void RunCopyFrom(Directory source, Directory dest)
        //{
        //    byte[] bytes = RandomBytes.RandomBytesOfLength(Random, 20000);
        //    using (IndexOutput output = source.CreateOutput("foobar", NewIOContext(Random)))
        //    {


        //        output.WriteBytes(bytes, bytes.Length);
        //    } // output.close();

        //    dest.CopyFrom(source, "foobar", "foobaz", NewIOContext(Random));
        //    assertTrue(SlowFileExists(dest, "foobaz"));

        //    byte[] bytes2 = new byte[bytes.Length];
        //    using (IndexInput input = dest.OpenInput("foobaz", NewIOContext(Random)))
        //    {

        //        input.ReadBytes(bytes2, 0, bytes2.Length);
        //    } // input.close();

        //    assertArrayEquals(bytes, bytes2);
        //}

        // LUCENENET: This test is not compatible with 4.8.0, as it was ported from 8.2.0
        //[Test]
        //public virtual void TestRename()
        //{
        //    using (Directory dir = GetDirectory(CreateTempDir("testRename")))
        //    {
        //        int numBytes = Random.nextInt(20000);
        //        byte[] bytes = new byte[numBytes];
        //        using (IndexOutput output = dir.CreateOutput("foobar", NewIOContext(Random)))
        //        {


        //            Random.NextBytes(bytes);
        //            output.WriteBytes(bytes, bytes.Length);
        //        } // output.close();

        //        dir.Rename("foobar", "foobaz");

        //        byte[] bytes2 = new byte[numBytes];
        //        using (IndexInput input = dir.OpenInput("foobaz", NewIOContext(Random)))
        //        {

        //            input.ReadBytes(bytes2, 0, bytes2.Length);
        //            assertEquals(input.Length, numBytes);
        //        } // input.close();

        //        assertArrayEquals(bytes, bytes2);
        //    }
        //}

        private static bool ContainsFile(Directory directory, string file) // LUCENENET specific method to prevent having to use Arrays.AsList(), which creates unnecessary memory allocations
        {
            return Array.IndexOf(directory.ListAll(), file) > -1;
        }


        [Test]
        public virtual void TestDeleteFile()
        {
            using Directory dir = GetDirectory(CreateTempDir("testDeleteFile"));
            string file = "foo.txt";
            Assert.IsFalse(ContainsFile(dir, file));

            using (dir.CreateOutput("foo.txt", IOContext.DEFAULT)) { }
            Assert.IsTrue(ContainsFile(dir, file));

            dir.DeleteFile("foo.txt");
            Assert.IsFalse(ContainsFile(dir, file));

            try
            {
                dir.DeleteFile("foo.txt");
                fail();
            }
            catch (Exception e) when (e.IsNoSuchFileExceptionOrFileNotFoundException())
            {
                // expected
            }
        }

        [Test]
        public virtual void TestByte()
        {
            using Directory dir = GetDirectory(CreateTempDir("testByte"));
            using (IndexOutput output = dir.CreateOutput("byte", NewIOContext(Random)))
            {
                output.WriteByte((byte)128);
            } // output.close();

            using IndexInput input = dir.OpenInput("byte", NewIOContext(Random));
            assertEquals(1, input.Length);
            assertEquals((byte)128, input.ReadByte());
        }

        [Test]
        public virtual void TestInt16() // LUCENENET: Changed from TestShort
        {
            using Directory dir = GetDirectory(CreateTempDir("testShort"));
            using (IndexOutput output = dir.CreateOutput("short", NewIOContext(Random)))
            {
                output.WriteInt16((short)-20);
            } // output.close();

            using IndexInput input = dir.OpenInput("short", NewIOContext(Random));
            assertEquals(2, input.Length);
            assertEquals((short)-20, input.ReadInt16());
        }

        [Test]
        public virtual void TestInt32() // LUCENENET: Changed from TestInt
        {
            using Directory dir = GetDirectory(CreateTempDir("testInt"));
            using (IndexOutput output = dir.CreateOutput("int", NewIOContext(Random)))
            {
                output.WriteInt32(-500);
            } // output.close();

            using IndexInput input = dir.OpenInput("int", NewIOContext(Random));
            assertEquals(4, input.Length);
            assertEquals(-500, input.ReadInt32());
        }

        [Test]
        public virtual void TestInt64()  // LUCENENET: Changed from TestLong
        {
            using Directory dir = GetDirectory(CreateTempDir("testLong"));
            using (IndexOutput output = dir.CreateOutput("long", NewIOContext(Random)))
            {
                output.WriteInt64(-5000);
            } // output.close();

            using IndexInput input = dir.OpenInput("long", NewIOContext(Random));
            assertEquals(8, input.Length);
            assertEquals(-5000L, input.ReadInt64());
        }

        [Test]
        public virtual void TestString()
        {
            using Directory dir = GetDirectory(CreateTempDir("testString"));
            using (IndexOutput output = dir.CreateOutput("string", NewIOContext(Random)))
            {
                output.WriteString("hello!");
            } // output.close();

            using IndexInput input = dir.OpenInput("string", NewIOContext(Random));
            assertEquals("hello!", input.ReadString());
            assertEquals(7, input.Length);
        }

        [Test]
        public virtual void TestVInt32() // LUCENENET: Renamed from TestVInt
        {
            using Directory dir = GetDirectory(CreateTempDir("testVInt"));
            using (IndexOutput output = dir.CreateOutput("vint", NewIOContext(Random)))
            {
                output.WriteVInt32(500);
            } // output.close();

            using IndexInput input = dir.OpenInput("vint", NewIOContext(Random));
            assertEquals(2, input.Length);
            assertEquals(500, input.ReadVInt32());
        }

        [Test]
        public virtual void TestVInt64() // LUCENENET: Renamed from TestVLong
        {
            using Directory dir = GetDirectory(CreateTempDir("testVLong"));
            using (IndexOutput output = dir.CreateOutput("vlong", NewIOContext(Random)))
            {
                output.WriteVInt64(long.MaxValue);
            } // output.close();

            using IndexInput input = dir.OpenInput("vlong", NewIOContext(Random));
            assertEquals(9, input.Length);
            assertEquals(long.MaxValue, input.ReadVInt64());
        }

        // LUCENENET: This test is not compatible with 4.8.0, as it was ported from 8.2.0
        //[Test]
        //public virtual void TestZInt32() // LUCENENET: Renamed from TestZInt
        //{
        //    int[] ints = new int[Random.nextInt(10)];
        //    for (int i = 0; i < ints.Length; ++i)
        //    {
        //        switch (Random.nextInt(3))
        //        {
        //            case 0:
        //                ints[i] = Random.nextInt();
        //                break;
        //            case 1:
        //                ints[i] = Random.nextBoolean() ? int.MinValue : int.MaxValue;
        //                break;
        //            case 2:
        //                ints[i] = (Random.nextBoolean() ? -1 : 1) * Random.nextInt(1024);
        //                break;
        //            default:
        //                throw AssertionError.Create();
        //        }
        //    }

        //    using (Directory dir = GetDirectory(CreateTempDir("testZInt")))
        //    {
        //        using (IndexOutput output = dir.CreateOutput("zint", NewIOContext(Random)))
        //        {
        //            foreach (int i in ints)
        //            {
        //                output.WriteZInt32(i);
        //            }
        //        } // output.close();

        //        using (IndexInput input = dir.OpenInput("zint", NewIOContext(Random)))
        //        {
        //            foreach (int i in ints)
        //            {
        //                assertEquals(i, input.ReadZInt32());
        //            }
        //            assertEquals(input.Length, input.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
        //        } // input.close();
        //    }
        //}

        // LUCENENET: This test is not compatible with 4.8.0, as it was ported from 8.2.0
        //[Test]
        //public virtual void TestZInt64() // LUCENENET: Renamed from TestZLong
        //{
        //    long[]
        //    longs = new long[Random.nextInt(10)];
        //    for (int i = 0; i < longs.Length; ++i)
        //    {
        //        switch (Random.nextInt(3))
        //        {
        //            case 0:
        //                longs[i] = Random.nextLong();
        //                break;
        //            case 1:
        //                longs[i] = Random.nextBoolean() ? long.MinValue : long.MaxValue;
        //                break;
        //            case 2:
        //                longs[i] = (Random.nextBoolean() ? -1 : 1) * Random.nextInt(1024);
        //                break;
        //            default:
        //                throw AssertionError.Create();
        //        }
        //    }

        //    using (Directory dir = GetDirectory(CreateTempDir("testZLong")))
        //    {
        //        using (IndexOutput output = dir.CreateOutput("zlong", NewIOContext(Random)))
        //        {
        //            foreach (long l in longs)
        //            {
        //                output.WriteZInt64(l);
        //            }
        //        } // output.close();

        //        using (IndexInput input = dir.OpenInput("zlong", NewIOContext(Random)))
        //        {
        //            foreach (long l in longs)
        //            {
        //                assertEquals(l, input.ReadZInt64());
        //            }
        //            assertEquals(input.Length, input.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
        //        } // input.close();
        //    }
        //}

        // LUCENENET: This test compiles, but is not compatible with 4.8.0 (tested in Java Lucene), as it was ported from 8.2.0
        //[Test]
        //public virtual void TestSetOfStrings()
        //{
        //    using (Directory dir = GetDirectory(CreateTempDir("testSetOfStrings")))
        //    {

        //        using (IndexOutput output = dir.CreateOutput("stringset", NewIOContext(Random)))
        //        {
        //            output.WriteSetOfStrings(AsSet("test1", "test2"));
        //            output.WriteSetOfStrings(new JCG.HashSet<string>());
        //            output.WriteSetOfStrings(AsSet("test3"));
        //        } // output.close();

        //        using (IndexInput input = dir.OpenInput("stringset", NewIOContext(Random)))
        //        {
        //            ISet<string> set1 = input.ReadSetOfStrings();
        //            assertEquals(AsSet("test1", "test2"), set1);
        //            // set should be immutable
        //            Assert.Throws<NotSupportedException>(() => {
        //                set1.Add("bogus");
        //            });

        //            ISet<string> set2 = input.ReadSetOfStrings();
        //            assertEquals(new JCG.HashSet<string>(), set2);
        //            // set should be immutable
        //            Assert.Throws<NotSupportedException>(() => {
        //                set2.Add("bogus");
        //            });

        //            ISet<string> set3 = input.ReadSetOfStrings();
        //            assertEquals(new JCG.HashSet<string> { "test3" }, set3);
        //            // set should be immutable
        //            Assert.Throws<NotSupportedException>(() => {
        //                set3.Add("bogus");
        //            });

        //            assertEquals(input.Length, input.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
        //        } // input.close();
        //    }
        //}

        // LUCENENET: This test compiles, but is not compatible with 4.8.0 (tested in Java Lucene), as it was ported from 8.2.0
        //[Test]
        //public virtual void TestMapOfStrings()
        //{
        //    IDictionary<string, string> m = new Dictionary<string, string>()
        //    {
        //        ["test1"] = "value1",
        //        ["test2"] = "value2"
        //    };

        //    using (Directory dir = GetDirectory(CreateTempDir("testMapOfStrings")))
        //    {
        //        using (IndexOutput output = dir.CreateOutput("stringmap", NewIOContext(Random)))
        //        {
        //            output.WriteMapOfStrings(m);
        //            output.WriteMapOfStrings(Collections.EmptyMap<string, string>());
        //            output.WriteMapOfStrings(Collections.SingletonMap<string, string>("key", "value"));
        //        } // output.close();

        //        using (IndexInput input = dir.OpenInput("stringmap", NewIOContext(Random)))
        //        {
        //            IDictionary<string, string> map1 = input.ReadMapOfStrings();
        //            assertEquals(m, map1);
        //            // map should be immutable
        //            Assert.Throws<NotSupportedException>(() => {
        //                map1["bogus1"] = "bogus2";
        //            });

        //            IDictionary<string, string> map2 = input.ReadMapOfStrings();
        //            assertEquals(Collections.EmptyMap<string, string>(), map2);
        //            // map should be immutable
        //            Assert.Throws<NotSupportedException>(() => {
        //                map2["bogus1"] = "bogus2";
        //            });

        //            IDictionary<string, string> map3 = input.ReadMapOfStrings();
        //            assertEquals(Collections.SingletonMap<string, string>("key", "value"), map3);
        //            // map should be immutable
        //            Assert.Throws<NotSupportedException>(() => {
        //                map3["bogus1"] = "bogus2";
        //            });

        //            assertEquals(input.Length, input.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
        //        } // input.close();
        //    }
        //}

        // TODO: fold in some of the testing of o.a.l.index.TestIndexInput in here!
        [Test]
        public virtual void TestChecksum()
        {
            CRC32 expected = new CRC32();
            int numBytes = Random.Next(20000);
            byte[] bytes = new byte[numBytes];
            Random.NextBytes(bytes);
            expected.Update(bytes);

            using Directory dir = GetDirectory(CreateTempDir("testChecksum"));
            using (IndexOutput output = dir.CreateOutput("checksum", NewIOContext(Random)))
            {
                output.WriteBytes(bytes, 0, bytes.Length);
            } // output.close();

            using ChecksumIndexInput input = dir.OpenChecksumInput("checksum", NewIOContext(Random));
            input.SkipBytes(numBytes);

            assertEquals(expected.Value, input.Checksum);
        }

        /// <summary>
        /// Make sure directory throws <see cref="ObjectDisposedException"/> if
        /// you try to <see cref="Directory.CreateOutput(string, IOContext)"/> after disposing.
        /// </summary>
        [Test]
        public virtual void TestDetectClose()
        {
            Directory dir = GetDirectory(CreateTempDir("testDetectClose"));
            dir.Dispose();

            Assert.Throws<ObjectDisposedException>(() => {
                dir.CreateOutput("test", NewIOContext(Random));
            });
        }

        /// <summary>
        /// Make sure directory allows double-dispose as per the
        /// <a href="https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern">dispose pattern docs</a>.
        /// </summary>
        [Test]
        [LuceneNetSpecific] // GH-841, GH-265
        public virtual void TestDoubleDispose()
        {
            using Directory dir = GetDirectory(CreateTempDir("testDoubleDispose"));
            Assert.DoesNotThrow(() => dir.Dispose());
        }

        //        private class ListAllThread : ThreadJob
        //        {
        //            private readonly BaseDirectoryTestCase outerInstance;
        //            private readonly Directory dir;
        //            private readonly AtomicBoolean stop;

        //            public ListAllThread(BaseDirectoryTestCase baseDirectoryTestCase, Directory dir, AtomicBoolean stop)
        //            {
        //                this.outerInstance = baseDirectoryTestCase ?? throw new ArgumentNullException(nameof(baseDirectoryTestCase));
        //                this.dir = dir ?? throw new ArgumentNullException(nameof(dir));
        //                this.stop = stop ?? throw new ArgumentNullException(nameof(stop));
        //            }

        //            public override void Run()
        //            {
        //                try
        //                {
        //                    Random rnd = new J2N.Randomizer(Random.NextInt64() + 1);
        //                    for (int i = 0, max = RandomInts.RandomInt32Between(Random, 500, 1000); i < max; i++)
        //                    {
        //                        string fileName = "file-" + i;
        //                        using (IndexOutput output = this.dir.CreateOutput(fileName, NewIOContext(Random)))
        //                        {
        //                            // Add some lags so that the other thread can read the content of the directory.
        //                            Thread.Yield();
        //                        }
        //                        assertTrue(SlowFileExists(this.dir, fileName));
        //                    }
        //                }
        //                //catch (Exception e) when (e.IsIOException())
        //                //{
        //                //    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
        //                //}
        //                finally
        //                {
        //                    this.stop.Set(true);
        //                }
        //            }
        //        }

        //        private class ListAllThread2 : ThreadJob
        //        {
        //            private readonly BaseDirectoryTestCase outerInstance;
        //            private readonly Directory dir;
        //            private readonly AtomicBoolean stop;

        //            public ListAllThread2(BaseDirectoryTestCase baseDirectoryTestCase, Directory dir, AtomicBoolean stop)
        //            {
        //                this.outerInstance = baseDirectoryTestCase ?? throw new ArgumentNullException(nameof(baseDirectoryTestCase));
        //                this.dir = dir ?? throw new ArgumentNullException(nameof(dir));
        //                this.stop = stop ?? throw new ArgumentNullException(nameof(stop));
        //            }

        //            public override void Run()
        //            {
        //                try
        //                {
        //                    Random rnd = new J2N.Randomizer(Random.NextInt64());
        //                    while (!stop.Get())
        //                    {
        //                        string[] files = dir.ListAll()
        //                            .Where(name => !ExtrasFS.IsExtra(name)) // Ignore anything from ExtraFS.
        //                            .ToArray();

        //                        if (files.Length > 0)
        //                        {
        //                            do
        //                            {
        //                                string file = RandomPicks.RandomFrom(rnd, files);
        //                                try
        //                                {
        //                                    IndexInput input = dir.OpenInput(file, NewIOContext(Random));

        //                                    // Just open, nothing else.
        //                                }
        //                                catch (Exception e) when (e.IsAccessDeniedException())
        //                                {
        //                                    // Access denied is allowed for files for which the output is still open (MockDirectoryWriter enforces
        //                                    // this, for example). Since we don't synchronize with the writer thread, just ignore it.
        //                                }
        //                                catch (Exception e) when (e.IsIOException())
        //                                {
        //                                    throw new IOException("Something went wrong when opening: " + file, e);
        //                                }
        //                            } while (rnd.Next(3) != 0); // Sometimes break and list files again.
        //                        }
        //                    }
        //                }
        //                catch (Exception e) when (e.IsIOException())
        //                {
        //                    //throw new UncheckedIOException(e);
        //                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
        //                }
        //            }
        //        }

        // LUCENENET: This test compiles, but is not compatible with 4.8.0 (tested in Java Lucene), as it was ported from 8.2.0
        //        [Test]
        //        public virtual void TestThreadSafetyInListAll()
        //        {
        //            using (Directory dir = GetDirectory(CreateTempDir("testThreadSafety")))
        //            {
        //                if (dir is BaseDirectoryWrapper)
        //                {
        //                    // we are not making a real index, just writing, reading files.
        //                    ((BaseDirectoryWrapper)dir).CheckIndexOnDispose = false;
        //                }
        //                if (dir is MockDirectoryWrapper)
        //                {
        //                    // makes this test really slow
        //                    ((MockDirectoryWrapper)dir).Throttling = (Throttling.NEVER);
        //                }

        //                AtomicBoolean stop = new AtomicBoolean();
        //                ThreadJob writer = new ListAllThread(this, dir, stop);
        //                ThreadJob reader = new ListAllThread2(this, dir, stop);

        //                reader.Start();
        //                writer.Start();

        //                writer.Join();
        //                reader.Join();
        //            }
        //        }

        /// <summary>
        /// LUCENE-1468: once we create an output, we should see
        /// it in the dir listing.
        /// </summary>
        [Test]
        public virtual void TestFileExistsInListAfterCreated()
        {
            using Directory dir = GetDirectory(CreateTempDir("testFileExistsInListAfterCreated"));
            string name = "file";
            using (dir.CreateOutput(name, NewIOContext(Random))) { }
            assertTrue(SlowFileExists(dir, name));
            assertTrue(ContainsFile(dir, name));
        }

        // LUCENE-2852
        [Test]
        public void TestSeekToEOFThenBack()
        {
            using Directory dir = GetDirectory(CreateTempDir("testSeekToEOFThenBack"));
            int bufferLength = 1024;
            byte[] bytes = new byte[3 * bufferLength];
            using (IndexOutput o = dir.CreateOutput("out", NewIOContext(Random)))
            {
                o.WriteBytes(bytes, 0, bytes.Length);
            } // o.close();

            using IndexInput i = dir.OpenInput("out", NewIOContext(Random));
            i.Seek(2 * bufferLength - 1);
            i.Seek(3 * bufferLength);
            i.Seek(bufferLength);
            i.ReadBytes(bytes, 0, 2 * bufferLength);
        }

        // LUCENE-1196
        [Test]
        public virtual void TestIllegalEOF()
        {
            using Directory dir = GetDirectory(CreateTempDir("testIllegalEOF"));
            using (IndexOutput o = dir.CreateOutput("out", NewIOContext(Random)))
            {
                byte[] b = new byte[1024];
                o.WriteBytes(b, 0, 1024);
            } // o.close();
            using IndexInput i = dir.OpenInput("out", NewIOContext(Random));
            i.Seek(1024);
        }

        // LUCENENET: This test compiles, but is not compatible with 4.8.0 (tested in Java Lucene), as it was ported from 8.2.0
        //[Test]
        //public virtual void TestSeekPastEOF()
        //{
        //    using (Directory dir = GetDirectory(CreateTempDir("testSeekPastEOF")))
        //    {
        //        int len = Random.Next(2048);
        //        using (IndexOutput o = dir.CreateOutput("out", NewIOContext(Random)))
        //        {
        //            byte[] b = new byte[len];
        //            o.WriteBytes(b, 0, len);
        //        } // o.close();
        //        using (IndexInput i = dir.OpenInput("out", NewIOContext(Random)))
        //        {

        //            // Seeking past EOF should always throw EOFException
        //            Assert.Throws<EndOfStreamException>(() => i.Seek(len + Random.Next(1, 2048 + 1)));

        //            // Seeking exactly to EOF should never throw any exception.
        //            i.Seek(len);

        //            // But any read following the seek(len) should throw an EOFException.
        //            Assert.Throws<EndOfStreamException>(() => i.ReadByte());
        //            Assert.Throws<EndOfStreamException>(() => {
        //                i.ReadBytes(new byte[1], 0, 1);
        //            });

        //        } // i.close();
        //    }
        //}

        // LUCENENET: This test is not compatible with 4.8.0, as it was ported from 8.2.0
        //[Test]
        //public virtual void TestSliceOutOfBounds()
        //{
        //    using (Directory dir = GetDirectory(CreateTempDir("testSliceOutOfBounds")))
        //    {
        //        int len = Random.Next(2040) + 8;
        //        using (IndexOutput o = dir.CreateOutput("out", NewIOContext(Random)))
        //        {
        //            byte[] b = new byte[len];
        //            o.WriteBytes(b, 0, len);
        //        } // o.close();
        //        using (IndexInput i = dir.OpenInput("out", NewIOContext(Random)))
        //        {
        //            Assert.Throws<ArgumentException>(() => {
        //                i.Slice("slice1", 0, len + 1);
        //            });

        //            Assert.Throws<ArgumentException>(() => {
        //                i.Slice("slice2", -1, len);
        //            });

        //            IndexInput slice = i.Slice("slice3", 4, len / 2);
        //            Assert.Throws<ArgumentException>(() => {
        //                slice.Slice("slice3sub", 1, len / 2);
        //            });

        //        } // i.close();
        //    }
        //}

        // LUCENE-3382 -- make sure we get exception if the directory really does not exist.
        [Test]
        public virtual void TestNoDir()
        {
            DirectoryInfo tempDir = CreateTempDir("doesnotexist");
            tempDir.Delete();
            //IOUtils.rm(tempDir);
            using Directory dir = GetDirectory(tempDir);
            try
            {
                DirectoryReader.Open(dir);
                fail();
            }
            catch (Exception e) when (e.IsNoSuchFileExceptionOrFileNotFoundException())
            {
                // expected
            }
        }

        [Test]
        public virtual void TestCopyBytes()
        {
            using Directory dir = GetDirectory(CreateTempDir("testCopyBytes"));
            byte[] bytes = new byte[TestUtil.NextInt32(Random, 1, 77777)];
            int size = TestUtil.NextInt32(Random, 1, 1777777);
            int upto = 0;
            int byteUpto = 0;
            using (IndexOutput @out = dir.CreateOutput("test", NewIOContext(Random)))
            {
                while (upto < size)
                {
                    bytes[byteUpto++] = Value(upto);
                    upto++;
                    if (byteUpto == bytes.Length)
                    {
                        @out.WriteBytes(bytes, 0, bytes.Length);
                        byteUpto = 0;
                    }
                }

                @out.WriteBytes(bytes, 0, byteUpto);
                assertEquals(size, @out.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            } // @out.close();
            assertEquals(size, dir.FileLength("test"));

            // copy from test -> test2
            using (IndexInput @in = dir.OpenInput("test", NewIOContext(Random)))
            using (IndexOutput @out = dir.CreateOutput("test2", NewIOContext(Random)))
            {

                upto = 0;
                while (upto < size)
                {
                    if (Random.nextBoolean())
                    {
                        @out.WriteByte(@in.ReadByte());
                        upto++;
                    }
                    else
                    {
                        int chunk = Math.Min(
                            TestUtil.NextInt32(Random, 1, bytes.Length), size - upto);
                        @out.CopyBytes(@in, chunk);
                        upto += chunk;
                    }
                }
                assertEquals(size, upto);
            } // @out.close(); @in.close();

            // verify
            using (IndexInput in2 = dir.OpenInput("test2", NewIOContext(Random)))
            {
                upto = 0;
                while (upto < size)
                {
                    if (Random.nextBoolean())
                    {
                        byte v = in2.ReadByte();
                        assertEquals(Value(upto), v);
                        upto++;
                    }
                    else
                    {
                        int limit = Math.Min(
                            TestUtil.NextInt32(Random, 1, bytes.Length), size - upto);
                        in2.ReadBytes(bytes, 0, limit);
                        for (int byteIdx = 0; byteIdx < limit; byteIdx++)
                        {
                            assertEquals(Value(upto), bytes[byteIdx]);
                            upto++;
                        }
                    }
                }
            } // in2.close();

            dir.DeleteFile("test");
            dir.DeleteFile("test2");
        }

        private static byte Value(int idx)
        {
            return (byte)((idx % 256) * (1 + (idx / 256)));
        }

        private class CopyBytesThread : ThreadJob
        {
            private readonly Barrier start;
            private readonly IndexInput src;
            private readonly Directory d;
            private readonly int i;

            public CopyBytesThread(Barrier start, IndexInput input, Directory d, int i)
            {
                this.start = start ?? throw new ArgumentNullException(nameof(start)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
                this.src = (IndexInput)input.Clone();
                this.d = d ?? throw new ArgumentNullException(nameof(d)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
                this.i = i;
            }

            public override void Run()
            {
                try
                {
                    start.SignalAndWait();
                    using IndexOutput dst = d.CreateOutput("copy" + i, IOContext.DEFAULT);
                    dst.CopyBytes(src, src.Length - 100);
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }

        // LUCENE-3541
        [Test]
        public virtual void TestCopyBytesWithThreads()
        {
            using Directory d = GetDirectory(CreateTempDir("testCopyBytesWithThreads"));
            byte[] data = RandomBytes.RandomBytesOfLengthBetween(Random, 101, 10000);

            using (IndexOutput output = d.CreateOutput("data", IOContext.DEFAULT))
            {
                output.WriteBytes(data, 0, data.Length);
            } // output.close();

            using IndexInput input = d.OpenInput("data", IOContext.DEFAULT);
            using (IndexOutput outputHeader = d.CreateOutput("header", IOContext.DEFAULT))
            {
                // copy our 100-byte header
                outputHeader.CopyBytes(input, 100);
            } // outputHeader.close();

            // now make N copies of the remaining bytes
            int threads = 10;
            Barrier start = new Barrier(threads);
            ThreadJob[] copies = new ThreadJob[threads];
            for (int i = 0; i < threads; i++)
            {
                copies[i] = new CopyBytesThread(start, input, d, i);
                copies[i].Start();
            }

            foreach (ThreadJob t in copies)
            {
                t.Join();
            }

            for (int i = 0; i < threads; i++)
            {
                using IndexInput copiedData = d.OpenInput("copy" + i, IOContext.DEFAULT);
                byte[] dataCopy = new byte[data.Length];
                Arrays.Copy(data, 0, dataCopy, 0, 100);
                copiedData.ReadBytes(dataCopy, 100, data.Length - 100);
                Assert.AreEqual(data, dataCopy);
            }
        }

        // this test backdoors the directory via the filesystem. so it must actually use the filesystem
        // TODO: somehow change this test to
        [Test]
        public virtual void TestFsyncDoesntCreateNewFiles()
        {
            DirectoryInfo path = CreateTempDir("nocreate");
            using Directory fsdir = GetDirectory(path);
            // this test backdoors the directory via the filesystem. so it must be an FSDir (for now)
            // TODO: figure a way to test this better/clean it up. E.g. we should be testing for FileSwitchDir,
            // if it's using two FSdirs and so on
            if (!(fsdir is FSDirectory))
            {
                AssumeTrue("test only works for FSDirectory subclasses", false);
                return;
            }

            // create a file
            using (IndexOutput @out = fsdir.CreateOutput("afile", NewIOContext(Random)))
            {
                @out.WriteString("boo");
            } // @out.close();

            // delete it in the file system.
            File.Delete(Path.Combine(path.FullName, "afile"));
            //Files.delete(path.resolve("afile"));

            int fileCount = fsdir.ListAll().Length;

            // fsync it
            try
            {
                fsdir.Sync(new string[] { "afile" });
                fail();
            }
            catch (Exception e) when (e.IsNoSuchFileExceptionOrFileNotFoundException())
            {
                // expected
            }

            // no new files created
            assertEquals(fileCount, fsdir.ListAll().Length);
        }

        // random access APIs

        // LUCENENET: This test is not compatible with 4.8.0, as it was ported from 8.2.0
        //[Test]
        //public virtual void TestRandomInt64() // LUCENENET: Renamed from TestRandomLong
        //{
        //    using (Directory dir = GetDirectory(CreateTempDir("testLongs")))
        //    {
        //        using (IndexOutput output = dir.CreateOutput("longs", NewIOContext(Random)))
        //        {
        //            int num = TestUtil.NextInt32(Random, 50, 3000);
        //            long[] longs = new long[num];
        //            for (int i = 0; i < longs.Length; i++)
        //            {
        //                longs[i] = TestUtil.NextInt64(Random, long.MinValue, long.MaxValue);
        //                output.WriteInt64(longs[i]);
        //            }
        //        } // output.close();

        //        // slice
        //        using (IndexInput input = dir.OpenInput("longs", NewIOContext(Random)))
        //        {
        //            RandomAccessInput slice = input.RandomAccessSlice(0, input.Length);
        //            for (int i = 0; i < longs.Length; i++)
        //            {
        //                assertEquals(longs[i], slice.ReadInt64(i * 8));
        //            }

        //            // subslices
        //            for (int i = 1; i < longs.Length; i++)
        //            {
        //                long offset = i * 8;
        //                RandomAccessInput subslice = input.RandomAccessSlice(offset, input.Length - offset);
        //                for (int j = i; j < longs.Length; j++)
        //                {
        //                    assertEquals(longs[j], subslice.ReadInt64((j - i) * 8));
        //                }
        //            }

        //            // with padding
        //            for (int i = 0; i < 7; i++)
        //            {
        //                string name = "longs-" + i;
        //                using (IndexOutput o = dir.CreateOutput(name, NewIOContext(Random)))
        //                {
        //                    byte[] junk = new byte[i];
        //                    Random.NextBytes(junk);
        //                    o.WriteBytes(junk, junk.Length);
        //                    input.Seek(0);
        //                    o.CopyBytes(input, input.Length);
        //                } // o.close();
        //                using (IndexInput padded = dir.OpenInput(name, NewIOContext(Random)))
        //                {
        //                    RandomAccessInput whole = padded.RandomAccessSlice(i, padded.Length - i);
        //                    for (int j = 0; j < longs.Length; j++)
        //                    {
        //                        assertEquals(longs[j], whole.ReadInt64(j * 8));
        //                    }
        //                } // padded.close();
        //            }

        //        } // input.close();
        //    }
        //}

        // LUCENENET: This test is not compatible with 4.8.0, as it was ported from 8.2.0
        //[Test]
        //public virtual void TestRandomInt32() // LUCENENET: Renamed from TestRandomInt
        //{
        //    using (Directory dir = GetDirectory(CreateTempDir("testInts")))
        //    {
        //        using (IndexOutput output = dir.CreateOutput("ints", NewIOContext(Random)))
        //        {
        //            int num = TestUtil.NextInt32(Random, 50, 3000);
        //            int[] ints = new int[num];
        //            for (int i = 0; i < ints.Length; i++)
        //            {
        //                ints[i] = Random.Next();
        //                output.WriteInt32(ints[i]);
        //            }
        //        } // output.close();

        //        // slice
        //        using (IndexInput input = dir.OpenInput("ints", NewIOContext(Random)))
        //        {
        //            RandomAccessInput slice = input.RandomAccessSlice(0, input.Length);
        //            for (int i = 0; i < ints.Length; i++)
        //            {
        //                assertEquals(ints[i], slice.ReadInt32(i * 4));
        //            }

        //            // subslices
        //            for (int i = 1; i < ints.Length; i++)
        //            {
        //                long offset = i * 4;
        //                RandomAccessInput subslice = input.RandomAccessSlice(offset, input.Length - offset);
        //                for (int j = i; j < ints.Length; j++)
        //                {
        //                    assertEquals(ints[j], subslice.ReadInt32((j - i) * 4));
        //                }
        //            }

        //            // with padding
        //            for (int i = 0; i < 7; i++)
        //            {
        //                string name = "ints-" + i;
        //                using (IndexOutput o = dir.CreateOutput(name, NewIOContext(Random)))
        //                {
        //                    byte[] junk = new byte[i];
        //                    Random.NextBytes(junk);
        //                    o.WriteBytes(junk, junk.Length);
        //                    input.Seek(0);
        //                    o.CopyBytes(input, input.Length);
        //                } // o.close();
        //                using (IndexInput padded = dir.OpenInput(name, NewIOContext(Random)))
        //                {
        //                    RandomAccessInput whole = padded.RandomAccessSlice(i, padded.Length - i);
        //                    for (int j = 0; j < ints.Length; j++)
        //                    {
        //                        assertEquals(ints[j], whole.ReadInt32(j * 4));
        //                    }
        //                } // padded.close();
        //            }
        //        } // input.close();
        //    }
        //}

        // LUCENENET: This test is not compatible with 4.8.0, as it was ported from 8.2.0
        //[Test]
        //public virtual void TestRandomInt16() // LUCENENET: Renamed from TestRandomShort
        //{
        //    using (Directory dir = GetDirectory(CreateTempDir("testShorts")))
        //    {
        //        using (IndexOutput output = dir.CreateOutput("shorts", NewIOContext(Random)))
        //        {
        //            int num = TestUtil.NextInt32(Random, 50, 3000);
        //            short[] shorts = new short[num];
        //            for (int i = 0; i < shorts.Length; i++)
        //            {
        //                shorts[i] = (short)Random.Next();
        //                output.WriteInt16(shorts[i]);
        //            }
        //        } // output.close();

        //        // slice
        //        using (IndexInput input = dir.OpenInput("shorts", NewIOContext(Random)))
        //        {
        //            RandomAccessInput slice = input.RandomAccessSlice(0, input.Length);
        //            for (int i = 0; i < shorts.Length; i++)
        //            {
        //                assertEquals(shorts[i], slice.ReadInt16(i * 2));
        //            }

        //            // subslices
        //            for (int i = 1; i < shorts.Length; i++)
        //            {
        //                long offset = i * 2;
        //                RandomAccessInput subslice = input.RandomAccessSlice(offset, input.Length - offset);
        //                for (int j = i; j < shorts.Length; j++)
        //                {
        //                    assertEquals(shorts[j], subslice.ReadInt16((j - i) * 2));
        //                }
        //            }

        //            // with padding
        //            for (int i = 0; i < 7; i++)
        //            {
        //                string name = "shorts-" + i;
        //                using (IndexOutput o = dir.CreateOutput(name, NewIOContext(Random)))
        //                {
        //                    byte[] junk = new byte[i];
        //                    Random.NextBytes(junk);
        //                    o.WriteBytes(junk, junk.Length);
        //                    input.Seek(0);
        //                    o.CopyBytes(input, input.Length);
        //                } // o.close();
        //                using (IndexInput padded = dir.OpenInput(name, NewIOContext(Random)))
        //                {
        //                    RandomAccessInput whole = padded.RandomAccessSlice(i, padded.Length - i);
        //                    for (int j = 0; j < shorts.Length; j++)
        //                    {
        //                        assertEquals(shorts[j], whole.ReadInt16(j * 2));
        //                    }
        //                } // padded.close();
        //            }
        //        } // input.close();
        //    }
        //}

        // LUCENENET: This test is not compatible with 4.8.0, as it was ported from 8.2.0
        //[Test]
        //public virtual void TestRandomByte()
        //{
        //    using (Directory dir = GetDirectory(CreateTempDir("testBytes")))
        //    {
        //        using (IndexOutput output = dir.CreateOutput("bytes", NewIOContext(Random)))
        //        {
        //            int num = TestUtil.NextInt32(Random, 50, 3000);
        //            byte[] bytes = new byte[num];
        //            Random.NextBytes(bytes);
        //            for (int i = 0; i < bytes.Length; i++)
        //            {
        //                output.WriteByte(bytes[i]);
        //            }
        //        } // output.close();

        //        // slice
        //        using (IndexInput input = dir.OpenInput("bytes", NewIOContext(Random)))
        //        {
        //            RandomAccessInput slice = input.RandomAccessSlice(0, input.Length);
        //            for (int i = 0; i < bytes.Length; i++)
        //            {
        //                assertEquals(bytes[i], slice.ReadByte(i));
        //            }

        //            // subslices
        //            for (int i = 1; i < bytes.Length; i++)
        //            {
        //                long offset = i;
        //                RandomAccessInput subslice = input.RandomAccessSlice(offset, input.Length - offset);
        //                for (int j = i; j < bytes.Length; j++)
        //                {
        //                    assertEquals(bytes[j], subslice.ReadByte(j - i));
        //                }
        //            }

        //            // with padding
        //            for (int i = 0; i < 7; i++)
        //            {
        //                string name = "bytes-" + i;
        //                using (IndexOutput o = dir.CreateOutput(name, NewIOContext(Random)))
        //                {
        //                    byte[] junk = new byte[i];
        //                    Random.NextBytes(junk);
        //                    o.WriteBytes(junk, junk.Length);
        //                    input.Seek(0);
        //                    o.CopyBytes(input, input.Length);
        //                } // o.close();
        //                using (IndexInput padded = dir.OpenInput(name, NewIOContext(Random)))
        //                {
        //                    RandomAccessInput whole = padded.RandomAccessSlice(i, padded.Length - i);
        //                    for (int j = 0; j < bytes.Length; j++)
        //                    {
        //                        assertEquals(bytes[j], whole.ReadByte(j));
        //                    }
        //                } // padded.close();
        //            }
        //        } // input.close();
        //    }
        //}

        // LUCENENET: This test is not compatible with 4.8.0, as it was ported from 8.2.0
        ///// <summary>
        ///// try to stress slices of slices
        ///// </summary>
        //[Test]
        //public virtual void TestSliceOfSlice()
        //{
        //    using (Directory dir = GetDirectory(CreateTempDir("sliceOfSlice")))
        //    {
        //        int num;
        //        if (TEST_NIGHTLY)
        //        {
        //            num = TestUtil.NextInt32(Random, 250, 2500);
        //        }
        //        else
        //        {
        //            num = TestUtil.NextInt32(Random, 50, 250);
        //        }
        //        byte[] bytes = new byte[num];
        //        using (IndexOutput output = dir.CreateOutput("bytes", NewIOContext(Random)))
        //        {
        //            Random.NextBytes(bytes);
        //            for (int i = 0; i < bytes.Length; i++)
        //            {
        //                output.WriteByte(bytes[i]);
        //            }
        //        } // output.close();

        //        using (IndexInput input = dir.OpenInput("bytes", NewIOContext(Random)))
        //        {
        //            // seek to a random spot shouldnt impact slicing.
        //            input.Seek(TestUtil.NextInt64(Random, 0, input.Length));
        //            for (int i = 0; i < num; i += 16)
        //            {
        //                IndexInput slice1 = input.Slice("slice1", i, num - i);
        //                assertEquals(0, slice1.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
        //                assertEquals(num - i, slice1.Length);

        //                // seek to a random spot shouldnt impact slicing.
        //                slice1.Seek(TestUtil.NextInt64(Random, 0, slice1.Length));
        //                for (int j = 0; j < slice1.Length; j += 16)
        //                {
        //                    IndexInput slice2 = slice1.Slice("slice2", j, num - i - j);
        //                    assertEquals(0, slice2.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
        //                    assertEquals(num - i - j, slice2.Length);
        //                    byte[] data = new byte[num];
        //                    Arrays.Copy(bytes, 0, data, 0, i + j);
        //                    if (Random.nextBoolean())
        //                    {
        //                        // read the bytes for this slice-of-slice
        //                        slice2.ReadBytes(data, i + j, num - i - j);
        //                    }
        //                    else
        //                    {
        //                        // seek to a random spot in between, read some, seek back and read the rest
        //                        long seek = TestUtil.NextInt64(Random, 0, slice2.Length);
        //                        slice2.Seek(seek);
        //                        slice2.ReadBytes(data, (int)(i + j + seek), (int)(num - i - j - seek));
        //                        slice2.Seek(0);
        //                        slice2.ReadBytes(data, i + j, (int)seek);
        //                    }
        //                    Assert.AreEqual(bytes, data);
        //                }
        //            }

        //        } // input.close();
        //    }
        //}

        /// <summary>
        /// This test that writes larger than the size of the buffer output
        /// will correctly increment the file pointer.
        /// </summary>
        [Test]
        public virtual void TestLargeWrites()
        {
            using Directory dir = GetDirectory(CreateTempDir("largeWrites"));
            using IndexOutput os = dir.CreateOutput("testBufferStart.txt", NewIOContext(Random));

            byte[] largeBuf = new byte[2048];
            Random.NextBytes(largeBuf);

            long currentPos = os.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            os.WriteBytes(largeBuf, largeBuf.Length);

            assertEquals(currentPos + largeBuf.Length, os.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
        }

        // LUCENENET: This test compiles, but is not compatible with 4.8.0 (tested in Java Lucene), as it was ported from 8.2.0
        // LUCENE-6084
        //[Test]
        //public virtual void TestIndexOutputToString()
        //{
        //    using (Directory dir = GetDirectory(CreateTempDir()))
        //    {
        //        using (IndexOutput @out = dir.CreateOutput("camelCase.txt", NewIOContext(Random)))
        //        {
        //            assertTrue(@out.ToString(), @out.ToString().Contains("camelCase.txt"));
        //        } // @out.close();
        //    }
        //}

        // LUCENENET: This test compiles, but is not compatible with 4.8.0 (randomly fails), as it was ported from 8.2.0
        //[Test]
        //public virtual void TestDoubleDisposeOutput()
        //{
        //    using (Directory dir = GetDirectory(CreateTempDir()))
        //    {
        //        IndexOutput @out = dir.CreateOutput("foobar", NewIOContext(Random));
        //        @out.WriteString("testing");
        //        @out.Dispose();
        //        @out.Dispose(); // close again
        //    }
        //}

        // LUCENENET: This test compiles, but is not compatible with 4.8.0 (randomly fails), as it was ported from 8.2.0
        //[Test]
        //public virtual void TestDoubleDisposeInput()
        //{
        //    using (Directory dir = GetDirectory(CreateTempDir()))
        //    {
        //        using (IndexOutput @out = dir.CreateOutput("foobar", NewIOContext(Random)))
        //        {
        //            @out.WriteString("testing");
        //        } // @out.close();
        //        IndexInput @in = dir.OpenInput("foobar", NewIOContext(Random));
        //        assertEquals("testing", @in.ReadString());
        //        @in.Dispose();
        //        @in.Dispose(); // close again
        //    }
        //}

        // LUCENENET: This test is not compatible with 4.8.0, as it was ported from 8.2.0
        //[Test]
        //public virtual void TestCreateTempOutput()
        //{
        //    using (Directory dir = GetDirectory(CreateTempDir()))
        //    {
        //        IList<string> names = new JCG.List<string>();
        //        int iters = AtLeast(50);
        //        for (int iter = 0; iter < iters; iter++)
        //        {
        //            using (IndexOutput @out = dir.CreateTempOutput("foo", "bar", NewIOContext(Random)))
        //            {
        //                names.Add(@out.Name);
        //                @out.WriteVInt32(iter);
        //            } // @out.close();
        //        }
        //        for (int iter = 0; iter < iters; iter++)
        //        {
        //            using (IndexInput @in = dir.OpenInput(names[iter], NewIOContext(Random)))
        //            {
        //                assertEquals(iter, @in.ReadVInt32());
        //            } // @in.close();
        //        }

        //        var files = dir.ListAll()
        //            .Where(file => !ExtraFS.IsExtra(file)) // remove any ExtrasFS stuff.
        //            .ToList();

        //        assertEquals(new JCG.List<string>(names), files);
        //    }
        //}

        // LUCENENET: This test compiles, but is not compatible with 4.8.0 (tested in Java Lucene), as it was ported from 8.2.0
        //[Test]
        //public virtual void TestCreateOutputForExistingFile()
        //{
        //    var tempDirectory = CreateTempDir();
        //    using (Directory dir = GetDirectory(tempDirectory))
        //    {
        //        string name = "file";
        //        using (IndexOutput @out = dir.CreateOutput(name, IOContext.DEFAULT))
        //        {
        //        }

        //        // Try to create an existing file should fail.
        //        Assert.ThrowsFileAlreadyExistsException(Path.Combine(tempDirectory.FullName, name), () => {
        //            using (IndexOutput @out = dir.CreateOutput(name, IOContext.DEFAULT))
        //            {
        //            }
        //        });

        //        // Delete file and try to recreate it.
        //        dir.DeleteFile(name);
        //        using (dir.CreateOutput(name, IOContext.DEFAULT)) { }
        //    }
        //}

        [Test]
        public virtual void TestSeekToEndOfFile()
        {
            using Directory dir = GetDirectory(CreateTempDir());
            using (IndexOutput @out = dir.CreateOutput("a", IOContext.DEFAULT))
            {
                for (int i = 0; i < 1024; ++i)
                {
                    @out.WriteByte((byte)0);
                }
            }
            using IndexInput @in = dir.OpenInput("a", IOContext.DEFAULT);
            @in.Seek(100);
            assertEquals(100, @in.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            @in.Seek(1024);
            assertEquals(1024, @in.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
        }

        // LUCENENET: This test compiles, but is not compatible with 4.8.0 (tested in Java Lucene), as it was ported from 8.2.0
        //[Test]
        //public virtual void TestSeekBeyondEndOfFile()
        //{
        //    using (Directory dir = GetDirectory(CreateTempDir()))
        //    {
        //        using (IndexOutput @out = dir.CreateOutput("a", IOContext.DEFAULT))
        //        {
        //            for (int i = 0; i < 1024; ++i)
        //            {
        //                @out.WriteByte((byte)0);
        //            }
        //        }
        //        using (IndexInput @in = dir.OpenInput("a", IOContext.DEFAULT))
        //        {
        //            @in.Seek(100);
        //            assertEquals(100, @in.Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
        //            Assert.Throws<EndOfStreamException>(() => {
        //                @in.Seek(1025);
        //            });
        //        }
        //    }
        //}

        // LUCENENET: This test is not compatible with 4.8.0, as it was ported from 8.2.0
        //// Make sure the FSDirectory impl properly "emulates" deletions on filesystems (Windows) with buggy deleteFile:
        //[Test]
        //public virtual void TestPendingDeletions()
        //{
        //    using (Directory dir = GetDirectory(AddVirusChecker(CreateTempDir())))
        //    {
        //        AssumeTrue("we can only install VirusCheckingFS on an FSDirectory", dir is FSDirectory);
        //        FSDirectory fsDir = (FSDirectory)dir;

        //        // Keep trying until virus checker refuses to delete:
        //        string fileName;
        //        while (true)
        //        {
        //            // create a random filename (segment file name style), so it cannot hit windows problem with special filenames ("con", "com1",...):
        //            string candidate = IndexFileNames.SegmentFileName(TestUtil.RandomSimpleString(Random, 1, 6), TestUtil.RandomSimpleString(Random), "test");
        //            using (IndexOutput @out = dir.CreateOutput(candidate, IOContext.DEFAULT))
        //            {
        //                @out.Position; // just fake access to prevent compiler warning // LUCENENET specific: Renamed from getFilePointer() to match FileStream
        //            }
        //            fsDir.DeleteFile(candidate);
        //            if (fsDir.GetPendingDeletions().Count > 0)
        //            {
        //                // good: virus checker struck and prevented deletion of fileName
        //                fileName = candidate;
        //                break;
        //            }
        //        }

        //        // Make sure listAll does NOT include the file:
        //        Assert.IsFalse(ContainsFile(fsDir, fileName));

        //        // Make sure fileLength claims it's deleted:
        //        Assert.Throws<FileNotFoundException>(() => { // LUCENENET: If this is ever uncommented, we need to use e.IsNoSuchFileExceptionOrFileNotFoundException()
        //            fsDir.FileLength(fileName);
        //        });

        //        // Make sure rename fails:
        //        Assert.Throws<FileNotFoundException>(() => { // LUCENENET: If this is ever uncommented, we need to use e.IsNoSuchFileExceptionOrFileNotFoundException()
        //            fsDir.Rename(fileName, "file2");
        //        });

        //        // Make sure delete fails:
        //        Assert.Throws<FileNotFoundException>(() => { // LUCENENET: If this is ever uncommented, we need to use e.IsNoSuchFileExceptionOrFileNotFoundException()
        //            fsDir.DeleteFile(fileName);
        //        });

        //        // Make sure we cannot open it for reading:
        //        Assert.Throws<FileNotFoundException>(() => { // LUCENENET: If this is ever uncommented, we need to use e.IsNoSuchFileExceptionOrFileNotFoundException()
        //            fsDir.OpenInput(fileName, IOContext.DEFAULT);
        //        });
        //    }
        //}

        // LUCENENET: This test is not compatible with 4.8.0, as it was ported from 8.2.0
        //[Test]
        //public virtual void TestListAllIsSorted()
        //{
        //    using (Directory dir = GetDirectory(CreateTempDir()))
        //    {
        //        int count = AtLeast(20);
        //        ISet<string> names = new JCG.HashSet<string>();
        //        while (names.Count < count)
        //        {
        //            // create a random filename (segment file name style), so it cannot hit windows problem with special filenames ("con", "com1",...):
        //            string name = IndexFileNames.SegmentFileName(TestUtil.RandomSimpleString(Random, 1, 6), TestUtil.RandomSimpleString(Random), "test");
        //            if (Random.Next(5) == 1)
        //            {
        //                using (IndexOutput @out = dir.CreateTempOutput(name, "foo", IOContext.DEFAULT))
        //                {
        //                    names.add(@out.Name);
        //                } // @out.close();
        //            }
        //            else if (names.Contains(name) == false)
        //            {
        //                using (IndexOutput @out = dir.CreateOutput(name, IOContext.DEFAULT))
        //                {
        //                    names.add(@out.Name);
        //                } // @out.close();
        //            }
        //        }
        //        string[] actual = dir.ListAll();
        //        string[] expected = actual.clone();
        //        CollectionUtil.TimSort(expected);
        //        assertArrayEquals(expected, actual);
        //    }
        //}
    }

    //internal static class IndexInputOutputExtensions
    //{
    //    public static void WriteSetOfStrings(this IndexOutput indexOutput, ISet<string> set)
    //    {
    //        indexOutput.WriteStringSet(set);
    //    }

    //    public static ISet<string> ReadSetOfStrings(this IndexInput indexInput)
    //    {
    //        return indexInput.ReadStringSet();
    //    }

    //    public static void WriteMapOfStrings(this IndexOutput indexOutput, IDictionary<string, string> map)
    //    {
    //        indexOutput.WriteStringStringMap(map);
    //    }

    //    public static IDictionary<string, string> ReadMapOfStrings(this IndexInput indexInput)
    //    {
    //        return indexInput.ReadStringStringMap();
    //    }
    //}
}
