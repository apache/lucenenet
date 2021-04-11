// Lucene version compatibility level 8.2.0
// LUCENENET NOTE: This class now exists both here and in Lucene.Net.Tests
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using System;
using System.IO;
using Lucene.Net.TestFramework;
using Lucene.Net.Documents;
using Lucene.Net.Index;

#if TESTFRAMEWORK_MSTEST
using Test = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;
using Assert = Lucene.Net.TestFramework.Assert;
#elif TESTFRAMEWORK_NUNIT
using Test = NUnit.Framework.TestAttribute;
using Assert = Lucene.Net.TestFramework.Assert;
#elif TESTFRAMEWORK_XUNIT
using Test = Lucene.Net.TestFramework.SkippableFactAttribute;
using Assert = Lucene.Net.TestFramework.Assert;
#endif

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

    // See: https://issues.apache.org/jira/browse/SOLR-12028 Tests cannot remove files on Windows machines occasionally
#if TESTFRAMEWORK_MSTEST
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute]
#endif
    public class TestMockDirectoryWrapper : BaseDirectoryTestCase
#if TESTFRAMEWORK_XUNIT
        , Xunit.IClassFixture<BeforeAfterClass>
    {
        public TestMockDirectoryWrapper(BeforeAfterClass beforeAfter)
            : base(beforeAfter)
        {
        }
#else
    {
#endif

        protected override Directory GetDirectory(DirectoryInfo path)
        {
            MockDirectoryWrapper dir;
            if (Random.nextBoolean())
            {
                dir = NewMockDirectory();
            }
            else
            {
                dir = NewMockFSDirectory(path);
            }
            return dir;
        }

        // we wrap the directory in slow stuff, so only run nightly

        // LUCENENET: This test compiles, but is not compatible with 4.8.0 (tested in Java Lucene), as it was ported from 8.2.0
        ////@Nightly
        //[Test]
        ////[Ignore("Very slow - run manually")]
        //public override void TestThreadSafetyInListAll()
        //{
        //    base.TestThreadSafetyInListAll();
        //}

        [Test]
        public void TestDiskFull()
        {
            byte[] bytes = new byte[] { 1, 2 };
            // test writeBytes
            using (MockDirectoryWrapper dir = NewMockDirectory())
            {
                dir.MaxSizeInBytes = 3;
                using (IndexOutput @out = dir.CreateOutput("foo", IOContext.DEFAULT))
                {
                    @out.WriteBytes(bytes, bytes.Length); // first write should succeed
                                                          // close() to ensure the written bytes are not buffered and counted
                                                          // against the directory size
                } // @out.close();
                using (IndexOutput @out = dir.CreateOutput("bar", IOContext.DEFAULT))
                {
                    try
                    {
                        @out.WriteBytes(bytes, bytes.Length);
                        fail("should have failed on disk full");
                    }
#pragma warning disable 168
                    catch (Exception e)
#pragma warning restore 168
                    {
                        // expected
                    }
                } // @out.close();
            } // dir.close();

            // test copyBytes
            using (MockDirectoryWrapper dir = NewMockDirectory())
            {
                dir.MaxSizeInBytes = 3;
                using (IndexOutput @out = dir.CreateOutput("foo", IOContext.DEFAULT))
                {
                    @out.CopyBytes(new ByteArrayDataInput(bytes), bytes.Length); // first copy should succeed
                                                                                 // close() to ensure the written bytes are not buffered and counted
                                                                                 // against the directory size
                } // @out.close();
                using (IndexOutput @out = dir.CreateOutput("bar", IOContext.DEFAULT))
                {
                    try
                    {
                        @out.CopyBytes(new ByteArrayDataInput(bytes), bytes.Length);
                        fail("should have failed on disk full");
                    }
#pragma warning disable 168
                    catch (Exception e)
#pragma warning restore 168
                    {
                        // expected
                    }
                } // @out.close();
            } // dir.close();
        }

        // LUCENENET: This test compiles, but is not compatible with 4.8.0 (tested in Java Lucene), as it was ported from 8.2.0
//        [Test]
//        public void TestMDWinsideOfMDW()
//        {
//            // add MDW inside another MDW
//#if FEATURE_INSTANCE_CODEC_IMPERSONATION
//            using (Directory dir = new MockDirectoryWrapper(this, Random, NewMockDirectory()))
//#else
//            using (Directory dir = new MockDirectoryWrapper(Random, NewMockDirectory()))
//#endif
//#if !FEATURE_INSTANCE_TESTDATA_INITIALIZATION
//            using (RandomIndexWriter iw = new RandomIndexWriter(Random, dir))
//#elif FEATURE_INSTANCE_CODEC_IMPERSONATION
//            using (RandomIndexWriter iw = new RandomIndexWriter(this, Random, dir))
//#else
//            using (RandomIndexWriter iw = new RandomIndexWriter(Random, dir, ClassEnvRule.similarity, ClassEnvRule.timeZone))
//#endif
//            {
//                for (int i = 0; i < 20; i++)
//                {
//                    iw.AddDocument(new Document());
//                }
//                iw.Commit();
//            } // iw.close(); dir.close();
//        }

        // just shields the wrapped directory from being disposed
        private class PreventDisposeDirectoryWrapper : FilterDirectory
        {
            public PreventDisposeDirectoryWrapper(Directory @in)
                : base(@in)
            { }

            protected override void Dispose(bool disposing)
            { }
        }

        // LUCENENET: This test is not compatible with 4.8.0, as it was ported from 8.2.0
        //        [Test]
        //        public void TestCorruptOnDisposeIsWorkingFSDir()
        //        {
        //            DirectoryInfo path = CreateTempDir();
        //            using (Directory dir = NewFSDirectory(path))
        //            {
        //                TestCorruptOnDisposeIsWorking(dir);
        //            }
        //        }

        // LUCENENET: This test is not compatible with 4.8.0, as it was ported from 8.2.0
        //        [Test]
        //        public void TestCorruptOnDisposeIsWorkingRAMDir()
        //        {
        //            using (Directory dir = new RAMDirectory())
        //            {
        //                TestCorruptOnDisposeIsWorking(dir);
        //            }
        //        }

        //        private void TestCorruptOnDisposeIsWorking(Directory dir)
        //        {

        //            dir = new PreventCloseDirectoryWrapper(dir);

        //            using (MockDirectoryWrapper wrapped = new MockDirectoryWrapper(Random, dir))
        //            {

        //                // otherwise MDW sometimes randomly leaves the file intact and we'll see false test failures:
        //                wrapped.AlwaysCorrupt = true;

        //                // MDW will only try to corrupt things if it sees an index:
        //#if FEATURE_INSTANCE_TESTDATA_INITIALIZATION
        //                using (RandomIndexWriter iw = new RandomIndexWriter(this, Random, dir))
        //#else
        //                using (RandomIndexWriter iw = new RandomIndexWriter(Random, dir))
        //#endif
        //                {
        //                    iw.AddDocument(new Document());
        //                } // iw.close();

        //                // not sync'd!
        //                using (IndexOutput @out = wrapped.CreateOutput("foo", IOContext.DEFAULT))
        //                {
        //                    for (int i = 0; i < 100; i++)
        //                    {
        //                        @out.WriteInt32(i);
        //                    }
        //                }

        //                // MDW.close now corrupts our unsync'd file (foo):
        //            }

        //            bool changed = false;
        //            IndexInput @in = null;
        //            try
        //            {
        //                @in = dir.OpenInput("foo", IOContext.DEFAULT);
        //            }
        //            catch (Exception e) when (e.IsNoSuchFileExceptionOrFileNotFoundException())
        //            {
        //                // ok
        //                changed = true;
        //            }
        //            if (@in != null)
        //            {
        //                for (int i = 0; i < 100; i++)
        //                {
        //                    int x;
        //                    try
        //                    {
        //                        x = @in.ReadInt32();
        //                    }
        //                    catch (Exception e) when (e.IsEOFException())
        //                    {
        //                        changed = true;
        //                        break;
        //                    }
        //                    if (x != i)
        //                    {
        //                        changed = true;
        //                        break;
        //                    }
        //                }

        //                @in.Dispose();
        //            }

        //            Assert.IsTrue(changed, "MockDirectoryWrapper on dir=" + dir + " failed to corrupt an unsync'd file");
        //        }

        [Test]
        public void TestAbuseClosedIndexInput()
        {
            using MockDirectoryWrapper dir = NewMockDirectory();
            using (IndexOutput @out = dir.CreateOutput("foo", IOContext.DEFAULT))
            {
                @out.WriteByte((byte)42);
            } // @out.close();
            IndexInput @in = dir.OpenInput("foo", IOContext.DEFAULT);
            @in.Dispose();
            Assert.Throws<Exception>(() => @in.ReadByte());
        }

        // LUCENENET: This test compiles, but is not compatible with 4.8.0 (tested in Java Lucene), as it was ported from 8.2.0
        //[Test]
        //public void TestAbuseCloneAfterParentClosed()
        //{
        //    using (MockDirectoryWrapper dir = NewMockDirectory())
        //    {
        //        using (IndexOutput @out = dir.CreateOutput("foo", IOContext.DEFAULT))
        //        {
        //            @out.WriteByte((byte)42);
        //        } // @out.close();
        //        IndexInput @in = dir.OpenInput("foo", IOContext.DEFAULT);
        //        IndexInput clone = (IndexInput)@in.Clone();
        //        @in.Dispose();
        //        Assert.Throws<Exception>(() => clone.ReadByte());
        //    } // dir.close();
        //}

        // LUCENENET: This test compiles, but is not compatible with 4.8.0 (tested in Java Lucene), as it was ported from 8.2.0
        //[Test]
        //public void TestAbuseCloneOfCloneAfterParentClosed()
        //{
        //    using (MockDirectoryWrapper dir = NewMockDirectory())
        //    {
        //        using (IndexOutput @out = dir.CreateOutput("foo", IOContext.DEFAULT))
        //        {
        //            @out.WriteByte((byte)42);
        //        } // @out.close();
        //        IndexInput @in = dir.OpenInput("foo", IOContext.DEFAULT);
        //        IndexInput clone1 = (IndexInput)@in.Clone();
        //        IndexInput clone2 = (IndexInput)clone1.Clone();
        //        @in.Dispose();
        //        Assert.Throws<Exception>(() => clone2.ReadByte());
        //    } // dir.close();
        //}
    }
}
