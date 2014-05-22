using System;

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


	using Throttling = Lucene.Net.Store.MockDirectoryWrapper.Throttling;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestDirectory : LuceneTestCase
	{
	  public virtual void TestDetectClose()
	  {
		File tempDir = createTempDir(LuceneTestCase.TestClass.SimpleName);
		Directory[] dirs = new Directory[] {new RAMDirectory(), new SimpleFSDirectory(tempDir), new NIOFSDirectory(tempDir)};

		foreach (Directory dir in dirs)
		{
		  dir.close();
		  try
		  {
			dir.createOutput("test", newIOContext(random()));
			Assert.Fail("did not hit expected exception");
		  }
		  catch (AlreadyClosedException ace)
		  {
		  }
		}
	  }

	  // test is occasionally very slow, i dont know why
	  // try this seed: 7D7E036AD12927F5:93333EF9E6DE44DE
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Nightly public void testThreadSafety() throws Exception
	  public virtual void TestThreadSafety()
	  {
		BaseDirectoryWrapper dir = newDirectory();
		dir.CheckIndexOnClose = false; // we arent making an index
		if (dir is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)dir).Throttling = Throttling.NEVER; // makes this test really slow
		}

		if (VERBOSE)
		{
		  Console.WriteLine(dir);
		}

//JAVA TO C# CONVERTER TODO TASK: Local classes are not converted by Java to C# Converter:
//		class TheThread extends Thread
	//	{
	//	  private String name;
	//
	//	  public TheThread(String name)
	//	  {
	//		this.name = name;
	//	  }
	//
	//	  @@Override public void run()
	//	  {
	//		for (int i = 0; i < 3000; i++)
	//		{
	//		  String fileName = this.name + i;
	//		  try
	//		  {
	//			//System.out.println("create:" + fileName);
	//			IndexOutput output = dir.createOutput(fileName, newIOContext(random()));
	//			output.close();
	//			Assert.IsTrue(slowFileExists(dir, fileName));
	//		  }
	//		  catch (IOException e)
	//		  {
	//			throw new RuntimeException(e);
	//		  }
	//		}
	//	  }
	//	};

//JAVA TO C# CONVERTER TODO TASK: Local classes are not converted by Java to C# Converter:
//		class TheThread2 extends Thread
	//	{
	//	  private String name;
	//
	//	  public TheThread2(String name)
	//	  {
	//		this.name = name;
	//	  }
	//
	//	  @@Override public void run()
	//	  {
	//		for (int i = 0; i < 10000; i++)
	//		{
	//		  try
	//		  {
	//			String[] files = dir.listAll();
	//			for (String file : files)
	//			{
	//			  //System.out.println("file:" + file);
	//			 try
	//			 {
	//			  IndexInput input = dir.openInput(file, newIOContext(random()));
	//			  input.close();
	//			  }
	//			  catch (FileNotFoundException | NoSuchFileException e)
	//			  {
	//				// ignore
	//			  }
	//			  catch (IOException e)
	//			  {
	//				if (e.getMessage().contains("still open for writing"))
	//				{
	//				  // ignore
	//				}
	//				else
	//				{
	//				  throw new RuntimeException(e);
	//				}
	//			  }
	//			  if (random().nextBoolean())
	//			  {
	//				break;
	//			  }
	//			}
	//		  }
	//		  catch (IOException e)
	//		  {
	//			throw new RuntimeException(e);
	//		  }
	//		}
	//	  }
	//	};

		TheThread theThread = new TheThread("t1");
		TheThread2 theThread2 = new TheThread2("t2");
		theThread.start();
		theThread2.start();

		theThread.join();
		theThread2.join();

		dir.close();
	  }


	  // Test that different instances of FSDirectory can coexist on the same
	  // path, can read, write, and lock files.
	  public virtual void TestDirectInstantiation()
	  {
		File path = createTempDir("testDirectInstantiation");

		const sbyte[] largeBuffer = new sbyte[random().Next(256 * 1024)], largeReadBuffer = new sbyte[largeBuffer.Length];
		for (int i = 0; i < largeBuffer.Length; i++)
		{
		  largeBuffer[i] = (sbyte) i; // automatically loops with modulo
		}

		FSDirectory[] dirs = new FSDirectory[] {new SimpleFSDirectory(path, null), new NIOFSDirectory(path, null), new MMapDirectory(path, null)};

		for (int i = 0; i < dirs.Length; i++)
		{
		  FSDirectory dir = dirs[i];
		  dir.ensureOpen();
		  string fname = "foo." + i;
		  string lockname = "foo" + i + ".lck";
		  IndexOutput @out = dir.createOutput(fname, newIOContext(random()));
		  @out.writeByte((sbyte)i);
		  @out.writeBytes(largeBuffer, largeBuffer.Length);
		  @out.close();

		  for (int j = 0; j < dirs.Length; j++)
		  {
			FSDirectory d2 = dirs[j];
			d2.ensureOpen();
			Assert.IsTrue(slowFileExists(d2, fname));
			Assert.AreEqual(1 + largeBuffer.Length, d2.fileLength(fname));

			// don't do read tests if unmapping is not supported!
			if (d2 is MMapDirectory && !((MMapDirectory) d2).UseUnmap)
			{
			  continue;
			}

			IndexInput input = d2.openInput(fname, newIOContext(random()));
			Assert.AreEqual((sbyte)i, input.readByte());
			// read array with buffering enabled
			Arrays.fill(largeReadBuffer, (sbyte)0);
			input.readBytes(largeReadBuffer, 0, largeReadBuffer.Length, true);
			assertArrayEquals(largeBuffer, largeReadBuffer);
			// read again without using buffer
			input.seek(1L);
			Arrays.fill(largeReadBuffer, (sbyte)0);
			input.readBytes(largeReadBuffer, 0, largeReadBuffer.Length, false);
			assertArrayEquals(largeBuffer, largeReadBuffer);
			input.close();
		  }

		  // delete with a different dir
		  dirs[(i + 1) % dirs.Length].deleteFile(fname);

		  for (int j = 0; j < dirs.Length; j++)
		  {
			FSDirectory d2 = dirs[j];
			Assert.IsFalse(slowFileExists(d2, fname));
		  }

		  Lock @lock = dir.makeLock(lockname);
		  Assert.IsTrue(@lock.obtain());

		  for (int j = 0; j < dirs.Length; j++)
		  {
			FSDirectory d2 = dirs[j];
			Lock lock2 = d2.makeLock(lockname);
			try
			{
			  Assert.IsFalse(lock2.obtain(1));
			}
			catch (LockObtainFailedException e)
			{
			  // OK
			}
		  }

		  @lock.close();

		  // now lock with different dir
		  @lock = dirs[(i + 1) % dirs.Length].makeLock(lockname);
		  Assert.IsTrue(@lock.obtain());
		  @lock.close();
		}

		for (int i = 0; i < dirs.Length; i++)
		{
		  FSDirectory dir = dirs[i];
		  dir.ensureOpen();
		  dir.close();
		  Assert.IsFalse(dir.isOpen);
		}

		TestUtil.rm(path);
	  }

	  // LUCENE-1464
	  public virtual void TestDontCreate()
	  {
		File path = new File(createTempDir(LuceneTestCase.TestClass.SimpleName), "doesnotexist");
		try
		{
		  Assert.IsTrue(!path.exists());
		  Directory dir = new SimpleFSDirectory(path, null);
		  Assert.IsTrue(!path.exists());
		  dir.close();
		}
		finally
		{
		  TestUtil.rm(path);
		}
	  }

	  // LUCENE-1468
	  public virtual void TestRAMDirectoryFilter()
	  {
		CheckDirectoryFilter(new RAMDirectory());
	  }

	  // LUCENE-1468
	  public virtual void TestFSDirectoryFilter()
	  {
		CheckDirectoryFilter(newFSDirectory(createTempDir("test")));
	  }

	  // LUCENE-1468
	  private void CheckDirectoryFilter(Directory dir)
	  {
		string name = "file";
		try
		{
		  dir.createOutput(name, newIOContext(random())).close();
		  Assert.IsTrue(slowFileExists(dir, name));
		  Assert.IsTrue(Arrays.asList(dir.listAll()).contains(name));
		}
		finally
		{
		  dir.close();
		}
	  }

	  // LUCENE-1468
	  public virtual void TestCopySubdir()
	  {
		File path = createTempDir("testsubdir");
		try
		{
		  path.mkdirs();
		  (new File(path, "subdir")).mkdirs();
		  Directory fsDir = new SimpleFSDirectory(path, null);
		  Assert.AreEqual(0, (new RAMDirectory(fsDir, newIOContext(random()))).listAll().length);
		}
		finally
		{
		  TestUtil.rm(path);
		}
	  }

	  // LUCENE-1468
	  public virtual void TestNotDirectory()
	  {
		File path = createTempDir("testnotdir");
		Directory fsDir = new SimpleFSDirectory(path, null);
		try
		{
		  IndexOutput @out = fsDir.createOutput("afile", newIOContext(random()));
		  @out.close();
		  Assert.IsTrue(slowFileExists(fsDir, "afile"));
		  try
		  {
			new SimpleFSDirectory(new File(path, "afile"), null);
			Assert.Fail("did not hit expected exception");
		  }
		  catch (NoSuchDirectoryException nsde)
		  {
			// Expected
		  }
		}
		finally
		{
		  fsDir.close();
		  TestUtil.rm(path);
		}
	  }

	  public virtual void TestFsyncDoesntCreateNewFiles()
	  {
		File path = createTempDir("nocreate");
		Console.WriteLine(path.AbsolutePath);
		Directory fsdir = new SimpleFSDirectory(path);

		// write a file
		IndexOutput @out = fsdir.createOutput("afile", newIOContext(random()));
		@out.writeString("boo");
		@out.close();

		// delete it
		Assert.IsTrue((new File(path, "afile")).delete());

		// directory is empty
		Assert.AreEqual(0, fsdir.listAll().length);

		// fsync it
		try
		{
		  fsdir.sync(Collections.singleton("afile"));
		  Assert.Fail("didn't get expected exception, instead fsync created new files: " + Arrays.asList(fsdir.listAll()));
		}
//JAVA TO C# CONVERTER TODO TASK: There is no equivalent in C# to Java 'multi-catch' syntax:
		catch (FileNotFoundException | NoSuchFileException expected)
		{
		  // ok
		}

		// directory is still empty
		Assert.AreEqual(0, fsdir.listAll().length);

		fsdir.close();
	  }
	}


}