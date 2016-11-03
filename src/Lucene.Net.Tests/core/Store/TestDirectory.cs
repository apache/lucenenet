using Lucene.Net.Attributes;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Store
{
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;

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

    using Throttling = Lucene.Net.Store.MockDirectoryWrapper.Throttling_e;

    [TestFixture]
    public class TestDirectory : LuceneTestCase
    {
        [Test]
        public virtual void TestDetectClose()
        {
            DirectoryInfo tempDir = CreateTempDir(GetType().Name);
            Directory[] dirs = new Directory[] { new RAMDirectory(), new SimpleFSDirectory(tempDir), new NIOFSDirectory(tempDir) };

            foreach (Directory dir in dirs)
            {
                dir.Dispose();
                try
                {
                    dir.CreateOutput("test", NewIOContext(Random()));
                    Assert.Fail("did not hit expected exception");
                }
                catch (AlreadyClosedException ace)
                {
                }
            }
        }

        // test is occasionally very slow, i dont know why
        // try this seed: 7D7E036AD12927F5:93333EF9E6DE44DE
        [Test, LongRunningTest, MaxTime(int.MaxValue)]
        public virtual void TestThreadSafety()
        {
            BaseDirectoryWrapper dir = NewDirectory();
            dir.CheckIndexOnClose = false; // we arent making an index
            if (dir is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)dir).Throttling = Throttling.NEVER; // makes this test really slow
            }

            if (VERBOSE)
            {
                Console.WriteLine(dir);
            }

            TheThread theThread = new TheThread("t1", dir);
            TheThread2 theThread2 = new TheThread2("t2", dir);
            theThread.Start();
            theThread2.Start();

            theThread.Join();
            theThread2.Join();

            dir.Dispose();
        }

        private class TheThread : ThreadClass
        {
            private readonly string name;
            private readonly BaseDirectoryWrapper outerBDWrapper;

            public TheThread(string name, BaseDirectoryWrapper baseDirectoryWrapper)
            {
                this.name = name;
                outerBDWrapper = baseDirectoryWrapper;
            }

            public override void Run()
            {
                for (int i = 0; i < 3000; ++i)
                {
                    string fileName = this.name + i;

                    try
                    {
                        using (IndexOutput output = outerBDWrapper.CreateOutput(fileName, NewIOContext(Random()))) { }
                        Assert.IsTrue(SlowFileExists(outerBDWrapper, fileName));
                    }
                    catch (IOException e)
                    {
                        throw new Exception(e.Message, e);
                    }
                }
            }
        }

        private class TheThread2 : ThreadClass
        {
            private string _name;
            private readonly BaseDirectoryWrapper outerBDWrapper;

            public TheThread2(string name, BaseDirectoryWrapper baseDirectoryWrapper)
            {
                this._name = name;
                outerBDWrapper = baseDirectoryWrapper;
            }

            public override void Run()
            {
                for (int i = 0; i < 10000; i++)
                {
                    try
                    {
                        string[] files = outerBDWrapper.ListAll();
                        foreach (string file in files)
                        {
                            try
                            {
                                using (IndexInput input = outerBDWrapper.OpenInput(file, NewIOContext(Random()))) { }
                            }
                            catch (FileNotFoundException fne)
                            {
                            }
                            catch (IOException e)
                            {
                                if (!e.Message.Contains("still open for writing"))
                                {
                                    throw new Exception(e.Message, e);
                                }
                            }
                            if (Random().NextBoolean())
                            {
                                break;
                            }
                        }
                    }
                    catch (IOException e)
                    {
                        throw new Exception(e.Message, e);
                    }
                }
            }
        }

        // Test that different instances of FSDirectory can coexist on the same
        // path, can read, write, and lock files.
        [Test, MaxTime(int.MaxValue)]
        public virtual void TestDirectInstantiation()
        {
            DirectoryInfo path = CreateTempDir("testDirectInstantiation");

            byte[] largeBuffer = new byte[Random().Next(256 * 1024)], largeReadBuffer = new byte[largeBuffer.Length];
            for (int i = 0; i < largeBuffer.Length; i++)
            {
                largeBuffer[i] = (byte)i; // automatically loops with modulo
            }

            var dirs = new FSDirectory[] { new SimpleFSDirectory(path, null), new NIOFSDirectory(path, null), new MMapDirectory(path, null) };

            for (int i = 0; i < dirs.Length; i++)
            {
                FSDirectory dir = dirs[i];
                dir.EnsureOpen();
                string fname = "foo." + i;
                string lockname = "foo" + i + ".lck";
                IndexOutput @out = dir.CreateOutput(fname, NewIOContext(Random()));
                @out.WriteByte((byte)(sbyte)i);
                @out.WriteBytes(largeBuffer, largeBuffer.Length);
                @out.Dispose();

                for (int j = 0; j < dirs.Length; j++)
                {
                    FSDirectory d2 = dirs[j];
                    d2.EnsureOpen();
                    Assert.IsTrue(SlowFileExists(d2, fname));
                    Assert.AreEqual(1 + largeBuffer.Length, d2.FileLength(fname));

                    // don't do read tests if unmapping is not supported!
                    if (d2 is MMapDirectory && !((MMapDirectory)d2).UseUnmap)
                    {
                        continue;
                    }

                    IndexInput input = d2.OpenInput(fname, NewIOContext(Random()));
                    Assert.AreEqual((byte)i, input.ReadByte());
                    // read array with buffering enabled
                    Arrays.Fill(largeReadBuffer, (byte)0);
                    input.ReadBytes(largeReadBuffer, 0, largeReadBuffer.Length, true);
                    Assert.AreEqual(largeBuffer, largeReadBuffer);
                    // read again without using buffer
                    input.Seek(1L);
                    Arrays.Fill(largeReadBuffer, (byte)0);
                    input.ReadBytes(largeReadBuffer, 0, largeReadBuffer.Length, false);
                    Assert.AreEqual(largeBuffer, largeReadBuffer);
                    input.Dispose();
                }

                // delete with a different dir
                dirs[(i + 1) % dirs.Length].DeleteFile(fname);

                for (int j = 0; j < dirs.Length; j++)
                {
                    FSDirectory d2 = dirs[j];
                    Assert.IsFalse(SlowFileExists(d2, fname));
                }

                Lock @lock = dir.MakeLock(lockname);
                Assert.IsTrue(@lock.Obtain());

                for (int j = 0; j < dirs.Length; j++)
                {
                    FSDirectory d2 = dirs[j];
                    Lock lock2 = d2.MakeLock(lockname);
                    try
                    {
                        Assert.IsFalse(lock2.Obtain(1));
                    }
                    catch (LockObtainFailedException e)
                    {
                        // OK
                    }
                }

                @lock.Dispose();

                // now lock with different dir
                @lock = dirs[(i + 1) % dirs.Length].MakeLock(lockname);
                Assert.IsTrue(@lock.Obtain());
                @lock.Dispose();
            }

            for (int i = 0; i < dirs.Length; i++)
            {
                FSDirectory dir = dirs[i];
                dir.EnsureOpen();
                dir.Dispose();
                Assert.IsFalse(dir.IsOpen);
            }
        }

        // LUCENE-1464
        [Test]
        public virtual void TestDontCreate()
        {
            var parentFolder = CreateTempDir(this.GetType().Name.ToLowerInvariant());
            var path = new DirectoryInfo(Path.Combine(parentFolder.FullName, "doesnotexist"));
            try
            {
                Assert.IsTrue(!path.Exists);
                Directory dir = new SimpleFSDirectory(path, null);
                Assert.IsTrue(!path.Exists);
                dir.Dispose();
            }
            finally
            {
                if (path.Exists) System.IO.Directory.Delete(path.FullName, true);
            }
        }

        // LUCENE-1468
        [Test]
        public virtual void TestRAMDirectoryFilter()
        {
            CheckDirectoryFilter(new RAMDirectory());
        }

        // LUCENE-1468
        [Test]
        public virtual void TestFSDirectoryFilter()
        {
            CheckDirectoryFilter(NewFSDirectory(CreateTempDir("test")));
        }

        // LUCENE-1468
        private void CheckDirectoryFilter(Directory dir)
        {
            string name = "file";
            try
            {
                dir.CreateOutput(name, NewIOContext(Random())).Dispose();
                Assert.IsTrue(SlowFileExists(dir, name));
                Assert.IsTrue(Arrays.AsList(dir.ListAll()).Contains(name));
            }
            finally
            {
                dir.Dispose();
            }
        }

        // LUCENE-1468
        [Test]
        public virtual void TestCopySubdir()
        {
            DirectoryInfo path = CreateTempDir("testsubdir");
            try
            {
                //path.mkdirs();
                System.IO.Directory.CreateDirectory(path.FullName);
                //(new File(path, "subdir")).mkdirs();
                System.IO.Directory.CreateDirectory(new DirectoryInfo(Path.Combine(path.FullName, "subdir")).FullName);
                Directory fsDir = new SimpleFSDirectory(path, null);
                Assert.AreEqual(0, (new RAMDirectory(fsDir, NewIOContext(Random()))).ListAll().Length);
            }
            finally
            {
                System.IO.Directory.Delete(path.FullName, true);
            }
        }

        // LUCENE-1468
        [Test]
        public virtual void TestNotDirectory()
        {
            DirectoryInfo path = CreateTempDir("testnotdir");
            Directory fsDir = new SimpleFSDirectory(path, null);
            try
            {
                IndexOutput @out = fsDir.CreateOutput("afile", NewIOContext(Random()));
                @out.Dispose();
                Assert.IsTrue(SlowFileExists(fsDir, "afile"));
                try
                {
                    var d = new SimpleFSDirectory(new DirectoryInfo(Path.Combine(path.FullName, "afile")), null);
                    Assert.Fail("did not hit expected exception");
                }
                catch (NoSuchDirectoryException nsde)
                {
                    // Expected
                }
            }
            finally
            {
                fsDir.Dispose();
                System.IO.Directory.Delete(path.FullName, true);
            }
        }

        [Test]
        public virtual void TestFsyncDoesntCreateNewFiles()
        {
            var path = CreateTempDir("nocreate");
            Console.WriteLine(path.FullName);

            using (Directory fsdir = new SimpleFSDirectory(path))
            {
                // write a file
                using (var o = fsdir.CreateOutput("afile", NewIOContext(Random())))
                {
                    o.WriteString("boo");
                }

                // delete it
                try
                {
                    File.Delete(Path.Combine(path.FullName, "afile"));
                }
                catch (Exception e)
                {
                    Assert.Fail("Deletion of new Directory should never fail.\nException thrown: {0}", e);
                }

                // directory is empty
                Assert.AreEqual(0, fsdir.ListAll().Length);

                // fsync it
                try
                {
                    fsdir.Sync(Collections.Singleton("afile"));
                    Assert.Fail("didn't get expected exception, instead fsync created new files: " +
                                Arrays.AsList(fsdir.ListAll()));
                }
                catch (FileNotFoundException)
                {
                    // ok
                }

                // directory is still empty
                Assert.AreEqual(0, fsdir.ListAll().Length);
            }
        }
    }
}