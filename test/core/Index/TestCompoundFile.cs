using System;

namespace Lucene.Net.Index
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

	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using SimpleFSDirectory = Lucene.Net.Store.SimpleFSDirectory;
	using IOUtils = Lucene.Net.Util.IOUtils;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;


//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Store.TestHelper.isSimpleFSIndexInput;
//JAVA TO C# CONVERTER TODO TASK: this Java 'import static' statement cannot be converted to .NET:
	import static Lucene.Net.Store.TestHelper.isSimpleFSIndexInputOpen;

	public class TestCompoundFile : LuceneTestCase
	{
		private Directory Dir;

		public override void SetUp()
		{
		   base.setUp();
		   File file = createTempDir("testIndex");
		   // use a simple FSDir here, to be sure to have SimpleFSInputs
		   Dir = new SimpleFSDirectory(file,null);
		}

		public override void TearDown()
		{
		   Dir.close();
		   base.tearDown();
		}

		/// <summary>
		/// Creates a file of the specified size with random data. </summary>
		private void CreateRandomFile(Directory dir, string name, int size)
		{
			IndexOutput os = dir.createOutput(name, newIOContext(random()));
			for (int i = 0; i < size; i++)
			{
				sbyte b = unchecked((sbyte)(new Random(1).NextDouble() * 256));
				os.writeByte(b);
			}
			os.close();
		}

		/// <summary>
		/// Creates a file of the specified size with sequential data. The first
		///  byte is written as the start byte provided. All subsequent bytes are
		///  computed as start + offset where offset is the number of the byte.
		/// </summary>
		private void CreateSequenceFile(Directory dir, string name, sbyte start, int size)
		{
			IndexOutput os = dir.createOutput(name, newIOContext(random()));
			for (int i = 0; i < size; i++)
			{
				os.writeByte(start);
				start++;
			}
			os.close();
		}


		private void AssertSameStreams(string msg, IndexInput expected, IndexInput test)
		{
			Assert.IsNotNull(msg + " null expected", expected);
			Assert.IsNotNull(msg + " null test", test);
			Assert.AreEqual(msg + " length", expected.length(), test.length());
			Assert.AreEqual(msg + " position", expected.FilePointer, test.FilePointer);

			sbyte[] expectedBuffer = new sbyte[512];
			sbyte[] testBuffer = new sbyte[expectedBuffer.Length];

			long remainder = expected.length() - expected.FilePointer;
			while (remainder > 0)
			{
				int readLen = (int) Math.Min(remainder, expectedBuffer.Length);
				expected.readBytes(expectedBuffer, 0, readLen);
				test.readBytes(testBuffer, 0, readLen);
				AssertEqualArrays(msg + ", remainder " + remainder, expectedBuffer, testBuffer, 0, readLen);
				remainder -= readLen;
			}
		}


		private void AssertSameStreams(string msg, IndexInput expected, IndexInput actual, long seekTo)
		{
			if (seekTo >= 0 && seekTo < expected.length())
			{
				expected.seek(seekTo);
				actual.seek(seekTo);
				AssertSameStreams(msg + ", seek(mid)", expected, actual);
			}
		}



		private void AssertSameSeekBehavior(string msg, IndexInput expected, IndexInput actual)
		{
			// seek to 0
			long point = 0;
			AssertSameStreams(msg + ", seek(0)", expected, actual, point);

			// seek to middle
			point = expected.length() / 2l;
			AssertSameStreams(msg + ", seek(mid)", expected, actual, point);

			// seek to end - 2
			point = expected.length() - 2;
			AssertSameStreams(msg + ", seek(end-2)", expected, actual, point);

			// seek to end - 1
			point = expected.length() - 1;
			AssertSameStreams(msg + ", seek(end-1)", expected, actual, point);

			// seek to the end
			point = expected.length();
			AssertSameStreams(msg + ", seek(end)", expected, actual, point);

			// seek past end
			point = expected.length() + 1;
			AssertSameStreams(msg + ", seek(end+1)", expected, actual, point);
		}


		private void AssertEqualArrays(string msg, sbyte[] expected, sbyte[] test, int start, int len)
		{
			Assert.IsNotNull(msg + " null expected", expected);
			Assert.IsNotNull(msg + " null test", test);

			for (int i = start; i < len; i++)
			{
				Assert.AreEqual(msg + " " + i, expected[i], test[i]);
			}
		}


		// ===========================================================
		//  Tests of the basic CompoundFile functionality
		// ===========================================================


		/// <summary>
		/// this test creates compound file based on a single file.
		///  Files of different sizes are tested: 0, 1, 10, 100 bytes.
		/// </summary>
		public virtual void TestSingleFile()
		{
			int[] data = new int[] {0, 1, 10, 100};
			for (int i = 0; i < data.Length; i++)
			{
				string name = "t" + data[i];
				CreateSequenceFile(Dir, name, (sbyte) 0, data[i]);
				CompoundFileDirectory csw = new CompoundFileDirectory(Dir, name + ".cfs", newIOContext(random()), true);
				Dir.copy(csw, name, name, newIOContext(random()));
				csw.close();

				CompoundFileDirectory csr = new CompoundFileDirectory(Dir, name + ".cfs", newIOContext(random()), false);
				IndexInput expected = Dir.openInput(name, newIOContext(random()));
				IndexInput actual = csr.openInput(name, newIOContext(random()));
				AssertSameStreams(name, expected, actual);
				AssertSameSeekBehavior(name, expected, actual);
				expected.close();
				actual.close();
				csr.close();
			}
		}


		/// <summary>
		/// this test creates compound file based on two files.
		/// 
		/// </summary>
		public virtual void TestTwoFiles()
		{
			CreateSequenceFile(Dir, "d1", (sbyte) 0, 15);
			CreateSequenceFile(Dir, "d2", (sbyte) 0, 114);

			CompoundFileDirectory csw = new CompoundFileDirectory(Dir, "d.cfs", newIOContext(random()), true);
			Dir.copy(csw, "d1", "d1", newIOContext(random()));
			Dir.copy(csw, "d2", "d2", newIOContext(random()));
			csw.close();

			CompoundFileDirectory csr = new CompoundFileDirectory(Dir, "d.cfs", newIOContext(random()), false);
			IndexInput expected = Dir.openInput("d1", newIOContext(random()));
			IndexInput actual = csr.openInput("d1", newIOContext(random()));
			AssertSameStreams("d1", expected, actual);
			AssertSameSeekBehavior("d1", expected, actual);
			expected.close();
			actual.close();

			expected = Dir.openInput("d2", newIOContext(random()));
			actual = csr.openInput("d2", newIOContext(random()));
			AssertSameStreams("d2", expected, actual);
			AssertSameSeekBehavior("d2", expected, actual);
			expected.close();
			actual.close();
			csr.close();
		}

		/// <summary>
		/// this test creates a compound file based on a large number of files of
		///  various length. The file content is generated randomly. The sizes range
		///  from 0 to 1Mb. Some of the sizes are selected to test the buffering
		///  logic in the file reading code. For this the chunk variable is set to
		///  the length of the buffer used internally by the compound file logic.
		/// </summary>
		public virtual void TestRandomFiles()
		{
			// Setup the test segment
			string segment = "test";
			int chunk = 1024; // internal buffer size used by the stream
			CreateRandomFile(Dir, segment + ".zero", 0);
			CreateRandomFile(Dir, segment + ".one", 1);
			CreateRandomFile(Dir, segment + ".ten", 10);
			CreateRandomFile(Dir, segment + ".hundred", 100);
			CreateRandomFile(Dir, segment + ".big1", chunk);
			CreateRandomFile(Dir, segment + ".big2", chunk - 1);
			CreateRandomFile(Dir, segment + ".big3", chunk + 1);
			CreateRandomFile(Dir, segment + ".big4", 3 * chunk);
			CreateRandomFile(Dir, segment + ".big5", 3 * chunk - 1);
			CreateRandomFile(Dir, segment + ".big6", 3 * chunk + 1);
			CreateRandomFile(Dir, segment + ".big7", 1000 * chunk);

			// Setup extraneous files
			CreateRandomFile(Dir, "onetwothree", 100);
			CreateRandomFile(Dir, segment + ".notIn", 50);
			CreateRandomFile(Dir, segment + ".notIn2", 51);

			// Now test
			CompoundFileDirectory csw = new CompoundFileDirectory(Dir, "test.cfs", newIOContext(random()), true);
			string[] data = new string[] {".zero", ".one", ".ten", ".hundred", ".big1", ".big2", ".big3", ".big4", ".big5", ".big6", ".big7"};
			for (int i = 0; i < data.Length; i++)
			{
				string fileName = segment + data[i];
				Dir.copy(csw, fileName, fileName, newIOContext(random()));
			}
			csw.close();

			CompoundFileDirectory csr = new CompoundFileDirectory(Dir, "test.cfs", newIOContext(random()), false);
			for (int i = 0; i < data.Length; i++)
			{
				IndexInput check = Dir.openInput(segment + data[i], newIOContext(random()));
				IndexInput test = csr.openInput(segment + data[i], newIOContext(random()));
				AssertSameStreams(data[i], check, test);
				AssertSameSeekBehavior(data[i], check, test);
				test.close();
				check.close();
			}
			csr.close();
		}


		/// <summary>
		/// Setup a larger compound file with a number of components, each of
		///  which is a sequential file (so that we can easily tell that we are
		///  reading in the right byte). The methods sets up 20 files - f0 to f19,
		///  the size of each file is 1000 bytes.
		/// </summary>
		private void SetUp_2()
		{
			CompoundFileDirectory cw = new CompoundFileDirectory(Dir, "f.comp", newIOContext(random()), true);
			for (int i = 0; i < 20; i++)
			{
				CreateSequenceFile(Dir, "f" + i, (sbyte) 0, 2000);
				string fileName = "f" + i;
				Dir.copy(cw, fileName, fileName, newIOContext(random()));
			}
			cw.close();
		}


		public virtual void TestReadAfterClose()
		{
			Demo_FSIndexInputBug(Dir, "test");
		}

		private void Demo_FSIndexInputBug(Directory fsdir, string file)
		{
			// Setup the test file - we need more than 1024 bytes
			IndexOutput os = fsdir.createOutput(file, IOContext.DEFAULT);
			for (int i = 0; i < 2000; i++)
			{
				os.writeByte((sbyte) i);
			}
			os.close();

			IndexInput @in = fsdir.openInput(file, IOContext.DEFAULT);

			// this read primes the buffer in IndexInput
			@in.readByte();

			// Close the file
			@in.close();

			// ERROR: this call should fail, but succeeds because the buffer
			// is still filled
			@in.readByte();

			// ERROR: this call should fail, but succeeds for some reason as well
			@in.seek(1099);

			try
			{
				// OK: this call correctly fails. We are now past the 1024 internal
				// buffer, so an actual IO is attempted, which fails
				@in.readByte();
				Assert.Fail("expected readByte() to throw exception");
			}
			catch (IOException e)
			{
			  // expected exception
			}
		}

		public virtual void TestClonedStreamsClosing()
		{
			SetUp_2();
			CompoundFileDirectory cr = new CompoundFileDirectory(Dir, "f.comp", newIOContext(random()), false);

			// basic clone
			IndexInput expected = Dir.openInput("f11", newIOContext(random()));

			// this test only works for FSIndexInput
			Assert.IsTrue(isSimpleFSIndexInput(expected));
			Assert.IsTrue(isSimpleFSIndexInputOpen(expected));

			IndexInput one = cr.openInput("f11", newIOContext(random()));

			IndexInput two = one.clone();

			AssertSameStreams("basic clone one", expected, one);
			expected.seek(0);
			AssertSameStreams("basic clone two", expected, two);

			// Now close the first stream
			one.close();

			// The following should really fail since we couldn't expect to
			// access a file once close has been called on it (regardless of
			// buffering and/or clone magic)
			expected.seek(0);
			two.seek(0);
			AssertSameStreams("basic clone two/2", expected, two);


			// Now close the compound reader
			cr.close();

			// The following may also fail since the compound stream is closed
			expected.seek(0);
			two.seek(0);
			//assertSameStreams("basic clone two/3", expected, two);


			// Now close the second clone
			two.close();
			expected.seek(0);
			two.seek(0);
			//assertSameStreams("basic clone two/4", expected, two);

			expected.close();
		}


		/// <summary>
		/// this test opens two files from a compound stream and verifies that
		///  their file positions are independent of each other.
		/// </summary>
		public virtual void TestRandomAccess()
		{
			SetUp_2();
			CompoundFileDirectory cr = new CompoundFileDirectory(Dir, "f.comp", newIOContext(random()), false);

			// Open two files
			IndexInput e1 = Dir.openInput("f11", newIOContext(random()));
			IndexInput e2 = Dir.openInput("f3", newIOContext(random()));

			IndexInput a1 = cr.openInput("f11", newIOContext(random()));
			IndexInput a2 = Dir.openInput("f3", newIOContext(random()));

			// Seek the first pair
			e1.seek(100);
			a1.seek(100);
			Assert.AreEqual(100, e1.FilePointer);
			Assert.AreEqual(100, a1.FilePointer);
			sbyte be1 = e1.readByte();
			sbyte ba1 = a1.readByte();
			Assert.AreEqual(be1, ba1);

			// Now seek the second pair
			e2.seek(1027);
			a2.seek(1027);
			Assert.AreEqual(1027, e2.FilePointer);
			Assert.AreEqual(1027, a2.FilePointer);
			sbyte be2 = e2.readByte();
			sbyte ba2 = a2.readByte();
			Assert.AreEqual(be2, ba2);

			// Now make sure the first one didn't move
			Assert.AreEqual(101, e1.FilePointer);
			Assert.AreEqual(101, a1.FilePointer);
			be1 = e1.readByte();
			ba1 = a1.readByte();
			Assert.AreEqual(be1, ba1);

			// Now more the first one again, past the buffer length
			e1.seek(1910);
			a1.seek(1910);
			Assert.AreEqual(1910, e1.FilePointer);
			Assert.AreEqual(1910, a1.FilePointer);
			be1 = e1.readByte();
			ba1 = a1.readByte();
			Assert.AreEqual(be1, ba1);

			// Now make sure the second set didn't move
			Assert.AreEqual(1028, e2.FilePointer);
			Assert.AreEqual(1028, a2.FilePointer);
			be2 = e2.readByte();
			ba2 = a2.readByte();
			Assert.AreEqual(be2, ba2);

			// Move the second set back, again cross the buffer size
			e2.seek(17);
			a2.seek(17);
			Assert.AreEqual(17, e2.FilePointer);
			Assert.AreEqual(17, a2.FilePointer);
			be2 = e2.readByte();
			ba2 = a2.readByte();
			Assert.AreEqual(be2, ba2);

			// Finally, make sure the first set didn't move
			// Now make sure the first one didn't move
			Assert.AreEqual(1911, e1.FilePointer);
			Assert.AreEqual(1911, a1.FilePointer);
			be1 = e1.readByte();
			ba1 = a1.readByte();
			Assert.AreEqual(be1, ba1);

			e1.close();
			e2.close();
			a1.close();
			a2.close();
			cr.close();
		}

		/// <summary>
		/// this test opens two files from a compound stream and verifies that
		///  their file positions are independent of each other.
		/// </summary>
		public virtual void TestRandomAccessClones()
		{
			SetUp_2();
			CompoundFileDirectory cr = new CompoundFileDirectory(Dir, "f.comp", newIOContext(random()), false);

			// Open two files
			IndexInput e1 = cr.openInput("f11", newIOContext(random()));
			IndexInput e2 = cr.openInput("f3", newIOContext(random()));

			IndexInput a1 = e1.clone();
			IndexInput a2 = e2.clone();

			// Seek the first pair
			e1.seek(100);
			a1.seek(100);
			Assert.AreEqual(100, e1.FilePointer);
			Assert.AreEqual(100, a1.FilePointer);
			sbyte be1 = e1.readByte();
			sbyte ba1 = a1.readByte();
			Assert.AreEqual(be1, ba1);

			// Now seek the second pair
			e2.seek(1027);
			a2.seek(1027);
			Assert.AreEqual(1027, e2.FilePointer);
			Assert.AreEqual(1027, a2.FilePointer);
			sbyte be2 = e2.readByte();
			sbyte ba2 = a2.readByte();
			Assert.AreEqual(be2, ba2);

			// Now make sure the first one didn't move
			Assert.AreEqual(101, e1.FilePointer);
			Assert.AreEqual(101, a1.FilePointer);
			be1 = e1.readByte();
			ba1 = a1.readByte();
			Assert.AreEqual(be1, ba1);

			// Now more the first one again, past the buffer length
			e1.seek(1910);
			a1.seek(1910);
			Assert.AreEqual(1910, e1.FilePointer);
			Assert.AreEqual(1910, a1.FilePointer);
			be1 = e1.readByte();
			ba1 = a1.readByte();
			Assert.AreEqual(be1, ba1);

			// Now make sure the second set didn't move
			Assert.AreEqual(1028, e2.FilePointer);
			Assert.AreEqual(1028, a2.FilePointer);
			be2 = e2.readByte();
			ba2 = a2.readByte();
			Assert.AreEqual(be2, ba2);

			// Move the second set back, again cross the buffer size
			e2.seek(17);
			a2.seek(17);
			Assert.AreEqual(17, e2.FilePointer);
			Assert.AreEqual(17, a2.FilePointer);
			be2 = e2.readByte();
			ba2 = a2.readByte();
			Assert.AreEqual(be2, ba2);

			// Finally, make sure the first set didn't move
			// Now make sure the first one didn't move
			Assert.AreEqual(1911, e1.FilePointer);
			Assert.AreEqual(1911, a1.FilePointer);
			be1 = e1.readByte();
			ba1 = a1.readByte();
			Assert.AreEqual(be1, ba1);

			e1.close();
			e2.close();
			a1.close();
			a2.close();
			cr.close();
		}


		public virtual void TestFileNotFound()
		{
			SetUp_2();
			CompoundFileDirectory cr = new CompoundFileDirectory(Dir, "f.comp", newIOContext(random()), false);

			// Open two files
			try
			{
				cr.openInput("bogus", newIOContext(random()));
				Assert.Fail("File not found");

			}
			catch (IOException e)
			{
				/* success */
				//System.out.println("SUCCESS: File Not Found: " + e);
			}

			cr.close();
		}


		public virtual void TestReadPastEOF()
		{
			SetUp_2();
			CompoundFileDirectory cr = new CompoundFileDirectory(Dir, "f.comp", newIOContext(random()), false);
			IndexInput @is = cr.openInput("f2", newIOContext(random()));
			@is.seek(@is.length() - 10);
			sbyte[] b = new sbyte[100];
			@is.readBytes(b, 0, 10);

			try
			{
				@is.readByte();
				Assert.Fail("Single byte read past end of file");
			}
			catch (IOException e)
			{
				/* success */
				//System.out.println("SUCCESS: single byte read past end of file: " + e);
			}

			@is.seek(@is.length() - 10);
			try
			{
				@is.readBytes(b, 0, 50);
				Assert.Fail("Block read past end of file");
			}
			catch (IOException e)
			{
				/* success */
				//System.out.println("SUCCESS: block read past end of file: " + e);
			}

			@is.close();
			cr.close();
		}

		/// <summary>
		/// this test that writes larger than the size of the buffer output
		/// will correctly increment the file pointer.
		/// </summary>
		public virtual void TestLargeWrites()
		{
			IndexOutput os = Dir.createOutput("testBufferStart.txt", newIOContext(random()));

			sbyte[] largeBuf = new sbyte[2048];
			for (int i = 0; i < largeBuf.Length; i++)
			{
				largeBuf[i] = unchecked((sbyte)(new Random(1).NextDouble() * 256));
			}

			long currentPos = os.FilePointer;
			os.writeBytes(largeBuf, largeBuf.Length);

			try
			{
				Assert.AreEqual(currentPos + largeBuf.Length, os.FilePointer);
			}
			finally
			{
				os.close();
			}

		}

	   public virtual void TestAddExternalFile()
	   {
		   CreateSequenceFile(Dir, "d1", (sbyte) 0, 15);

		   Directory newDir = newDirectory();
		   CompoundFileDirectory csw = new CompoundFileDirectory(newDir, "d.cfs", newIOContext(random()), true);
		   Dir.copy(csw, "d1", "d1", newIOContext(random()));
		   csw.close();

		   CompoundFileDirectory csr = new CompoundFileDirectory(newDir, "d.cfs", newIOContext(random()), false);
		   IndexInput expected = Dir.openInput("d1", newIOContext(random()));
		   IndexInput actual = csr.openInput("d1", newIOContext(random()));
		   AssertSameStreams("d1", expected, actual);
		   AssertSameSeekBehavior("d1", expected, actual);
		   expected.close();
		   actual.close();
		   csr.close();

		   newDir.close();
	   }


	  public virtual void TestAppend()
	  {
		Directory newDir = newDirectory();
		CompoundFileDirectory csw = new CompoundFileDirectory(newDir, "d.cfs", newIOContext(random()), true);
		int size = 5 + random().Next(128);
		for (int j = 0; j < 2; j++)
		{
		  IndexOutput os = csw.createOutput("seg_" + j + "_foo.txt", newIOContext(random()));
		  for (int i = 0; i < size; i++)
		  {
			os.writeInt(i * j);
		  }
		  os.close();
		  string[] listAll = newDir.listAll();
		  Assert.AreEqual(1, listAll.Length);
		  Assert.AreEqual("d.cfs", listAll[0]);
		}
		CreateSequenceFile(Dir, "d1", (sbyte) 0, 15);
		Dir.copy(csw, "d1", "d1", newIOContext(random()));
		string[] listAll = newDir.listAll();
		Assert.AreEqual(1, listAll.Length);
		Assert.AreEqual("d.cfs", listAll[0]);
		csw.close();
		CompoundFileDirectory csr = new CompoundFileDirectory(newDir, "d.cfs", newIOContext(random()), false);
		for (int j = 0; j < 2; j++)
		{
		  IndexInput openInput = csr.openInput("seg_" + j + "_foo.txt", newIOContext(random()));
		  Assert.AreEqual(size * 4, openInput.length());
		  for (int i = 0; i < size; i++)
		  {
			Assert.AreEqual(i * j, openInput.readInt());
		  }

		  openInput.close();

		}
		IndexInput expected = Dir.openInput("d1", newIOContext(random()));
		IndexInput actual = csr.openInput("d1", newIOContext(random()));
		AssertSameStreams("d1", expected, actual);
		AssertSameSeekBehavior("d1", expected, actual);
		expected.close();
		actual.close();
		csr.close();
		newDir.close();
	  }

	  public virtual void TestAppendTwice()
	  {
		Directory newDir = newDirectory();
		CompoundFileDirectory csw = new CompoundFileDirectory(newDir, "d.cfs", newIOContext(random()), true);
		CreateSequenceFile(newDir, "d1", (sbyte) 0, 15);
		IndexOutput @out = csw.createOutput("d.xyz", newIOContext(random()));
		@out.writeInt(0);
		@out.close();
		Assert.AreEqual(1, csw.listAll().length);
		Assert.AreEqual("d.xyz", csw.listAll()[0]);

		csw.close();

		CompoundFileDirectory cfr = new CompoundFileDirectory(newDir, "d.cfs", newIOContext(random()), false);
		Assert.AreEqual(1, cfr.listAll().length);
		Assert.AreEqual("d.xyz", cfr.listAll()[0]);
		cfr.close();
		newDir.close();
	  }

	  public virtual void TestEmptyCFS()
	  {
		Directory newDir = newDirectory();
		CompoundFileDirectory csw = new CompoundFileDirectory(newDir, "d.cfs", newIOContext(random()), true);
		csw.close();

		CompoundFileDirectory csr = new CompoundFileDirectory(newDir, "d.cfs", newIOContext(random()), false);
		Assert.AreEqual(0, csr.listAll().length);
		csr.close();

		newDir.close();
	  }

	  public virtual void TestReadNestedCFP()
	  {
		Directory newDir = newDirectory();
		CompoundFileDirectory csw = new CompoundFileDirectory(newDir, "d.cfs", newIOContext(random()), true);
		CompoundFileDirectory nested = new CompoundFileDirectory(newDir, "b.cfs", newIOContext(random()), true);
		IndexOutput @out = nested.createOutput("b.xyz", newIOContext(random()));
		IndexOutput out1 = nested.createOutput("b_1.xyz", newIOContext(random()));
		@out.writeInt(0);
		out1.writeInt(1);
		@out.close();
		out1.close();
		nested.close();
		newDir.copy(csw, "b.cfs", "b.cfs", newIOContext(random()));
		newDir.copy(csw, "b.cfe", "b.cfe", newIOContext(random()));
		newDir.deleteFile("b.cfs");
		newDir.deleteFile("b.cfe");
		csw.close();

		Assert.AreEqual(2, newDir.listAll().length);
		csw = new CompoundFileDirectory(newDir, "d.cfs", newIOContext(random()), false);

		Assert.AreEqual(2, csw.listAll().length);
		nested = new CompoundFileDirectory(csw, "b.cfs", newIOContext(random()), false);

		Assert.AreEqual(2, nested.listAll().length);
		IndexInput openInput = nested.openInput("b.xyz", newIOContext(random()));
		Assert.AreEqual(0, openInput.readInt());
		openInput.close();
		openInput = nested.openInput("b_1.xyz", newIOContext(random()));
		Assert.AreEqual(1, openInput.readInt());
		openInput.close();
		nested.close();
		csw.close();
		newDir.close();
	  }

	  public virtual void TestDoubleClose()
	  {
		Directory newDir = newDirectory();
		CompoundFileDirectory csw = new CompoundFileDirectory(newDir, "d.cfs", newIOContext(random()), true);
		IndexOutput @out = csw.createOutput("d.xyz", newIOContext(random()));
		@out.writeInt(0);
		@out.close();

		csw.close();
		// close a second time - must have no effect according to IDisposable
		csw.close();

		csw = new CompoundFileDirectory(newDir, "d.cfs", newIOContext(random()), false);
		IndexInput openInput = csw.openInput("d.xyz", newIOContext(random()));
		Assert.AreEqual(0, openInput.readInt());
		openInput.close();
		csw.close();
		// close a second time - must have no effect according to IDisposable
		csw.close();

		newDir.close();

	  }

	  // Make sure we don't somehow use more than 1 descriptor
	  // when reading a CFS with many subs:
	  public virtual void TestManySubFiles()
	  {

		Directory d = newFSDirectory(createTempDir("CFSManySubFiles"));
		int FILE_COUNT = atLeast(500);

		for (int fileIdx = 0;fileIdx < FILE_COUNT;fileIdx++)
		{
		  IndexOutput @out = d.createOutput("file." + fileIdx, newIOContext(random()));
		  @out.writeByte((sbyte) fileIdx);
		  @out.close();
		}

		CompoundFileDirectory cfd = new CompoundFileDirectory(d, "c.cfs", newIOContext(random()), true);
		for (int fileIdx = 0;fileIdx < FILE_COUNT;fileIdx++)
		{
		  string fileName = "file." + fileIdx;
		  d.copy(cfd, fileName, fileName, newIOContext(random()));
		}
		cfd.close();

		IndexInput[] ins = new IndexInput[FILE_COUNT];
		CompoundFileDirectory cfr = new CompoundFileDirectory(d, "c.cfs", newIOContext(random()), false);
		for (int fileIdx = 0;fileIdx < FILE_COUNT;fileIdx++)
		{
		  ins[fileIdx] = cfr.openInput("file." + fileIdx, newIOContext(random()));
		}

		for (int fileIdx = 0;fileIdx < FILE_COUNT;fileIdx++)
		{
		  Assert.AreEqual((sbyte) fileIdx, ins[fileIdx].readByte());
		}

		for (int fileIdx = 0;fileIdx < FILE_COUNT;fileIdx++)
		{
		  ins[fileIdx].close();
		}
		cfr.close();
		d.close();
	  }

	  public virtual void TestListAll()
	  {
		Directory dir = newDirectory();
		// riw should sometimes create docvalues fields, etc
		RandomIndexWriter riw = new RandomIndexWriter(random(), dir);
		Document doc = new Document();
		// these fields should sometimes get term vectors, etc
		Field idField = newStringField("id", "", Field.Store.NO);
		Field bodyField = newTextField("body", "", Field.Store.NO);
		doc.add(idField);
		doc.add(bodyField);
		for (int i = 0; i < 100; i++)
		{
		  idField.StringValue = Convert.ToString(i);
		  bodyField.StringValue = TestUtil.randomUnicodeString(random());
		  riw.addDocument(doc);
		  if (random().Next(7) == 0)
		  {
			riw.commit();
		  }
		}
		riw.close();
		CheckFiles(dir);
		dir.close();
	  }

	  // checks that we can open all files returned by listAll!
	  private void CheckFiles(Directory dir)
	  {
		foreach (string file in dir.listAll())
		{
		  if (file.EndsWith(IndexFileNames.COMPOUND_FILE_EXTENSION))
		  {
			CompoundFileDirectory cfsDir = new CompoundFileDirectory(dir, file, newIOContext(random()), false);
			CheckFiles(cfsDir); // recurse into cfs
			cfsDir.close();
		  }
		  IndexInput @in = null;
		  bool success = false;
		  try
		  {
			@in = dir.openInput(file, newIOContext(random()));
			success = true;
		  }
		  finally
		  {
			if (success)
			{
			  IOUtils.Close(@in);
			}
			else
			{
			  IOUtils.CloseWhileHandlingException(@in);
			}
		  }
		}
	  }
	}

}