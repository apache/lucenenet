/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// {{Aroush-2.3.1}} Remove from 2.3.1

/*
using System;
using NUnit.Framework;
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using IndexWriter = Lucene.Net.Index.IndexWriter;

namespace _delete_Lucene.Net.Index.Store
{
#if DELETE_ME
	/// <summary> Test to illustrate the problem found when trying to open an IndexWriter in
	/// a situation where the the property <code>Lucene.Net.lockDir</code>
	/// was not set and the one specified by <code>java.io.tmpdir</code> had been
	/// set to a non-existent path. What I observed is that this combination of
	/// conditions resulted in a <code>NullPointerException</code> being thrown in
	/// the <code>create()</code> method in <code>FSDirectory</code>, where
	/// <code>files.length</code> is de-referenced, but <code>files</code> is
	/// </code>null</code>.
	/// 
	/// </summary>
	/// <author>  Michael Goddard
	/// </author>
	
    [TestFixture]
	public class TestFSDirectory
	{
		
		/// <summary> What happens if the Lucene lockDir doesn't exist?
		/// 
		/// </summary>
		/// <throws>  Exception </throws>
		[Test]
        public virtual void  TestNonExistentTmpDir()
		{
            orgApacheLuceneLockDir = System.Configuration.ConfigurationSettings.AppSettings.Get("Lucene.Net.lockDir");
			//System.Configuration.ConfigurationSettings.AppSettings.Set("Lucene.Net.lockDir", NON_EXISTENT_DIRECTORY); // {{Aroush}} how do we setup an envirement variable in C#?
			System.String exceptionClassName = OpenIndexWriter();
			if (exceptionClassName == null || exceptionClassName.Equals("java.io.IOException"))
				NUnit.Framework.Assert.IsTrue(true);
			else
				NUnit.Framework.Assert.Fail("Caught an unexpected Exception");
		}
		
		/// <summary> What happens if the Lucene lockDir is a regular file instead of a
		/// directory?
		/// 
		/// </summary>
		/// <throws>  Exception </throws>
		[Test]
        public virtual void  TestTmpDirIsPlainFile()
		{
			shouldBeADirectory = new System.IO.FileInfo(NON_EXISTENT_DIRECTORY);
            shouldBeADirectory.Create().Close();
            System.String exceptionClassName = OpenIndexWriter();
			if (exceptionClassName == null || exceptionClassName.Equals("java.io.IOException"))
				NUnit.Framework.Assert.IsTrue(true);
			else
				NUnit.Framework.Assert.Fail("Caught an unexpected Exception");
		}
		
		public static readonly System.String FILE_SEP = System.IO.Path.DirectorySeparatorChar.ToString();
		
		public static readonly System.String NON_EXISTENT_DIRECTORY = System.IO.Path.GetTempPath() + FILE_SEP + "highly_improbable_directory_name";
		
		public static readonly System.String TEST_INDEX_DIR = System.IO.Path.GetTempPath() + FILE_SEP + "temp_index";
		
		private System.String orgApacheLuceneLockDir;
		
		private System.IO.FileInfo shouldBeADirectory;
		
        [TearDown]
		public virtual void  TearDown()
		{
			if (orgApacheLuceneLockDir != null)
			{
				System.Configuration.ConfigurationSettings.AppSettings.Set("Lucene.Net.lockDir", orgApacheLuceneLockDir);
			}
            bool tmpBool = false;
            if ((shouldBeADirectory != null) && 
                System.IO.File.Exists(shouldBeADirectory.FullName) && 
                System.IO.Directory.Exists(shouldBeADirectory.FullName))
            {
                tmpBool = true;
            }
            if (shouldBeADirectory != null && tmpBool)
			{
				try
				{
					bool tmpBool2;
					if (System.IO.File.Exists(shouldBeADirectory.FullName))
					{
						System.IO.File.Delete(shouldBeADirectory.FullName);
						tmpBool2 = true;
					}
					else if (System.IO.Directory.Exists(shouldBeADirectory.FullName))
					{
						System.IO.Directory.Delete(shouldBeADirectory.FullName);
						tmpBool2 = true;
					}
					else
						tmpBool2 = false;
					bool generatedAux = tmpBool2;
				}
				catch (System.Exception e)
				{
                    System.Console.Error.WriteLine(e.StackTrace);
				}
			}
			System.IO.FileInfo deletableIndex = new System.IO.FileInfo(TEST_INDEX_DIR);
			bool tmpBool3;
			if (System.IO.File.Exists(deletableIndex.FullName))
				tmpBool3 = true;
			else
				tmpBool3 = System.IO.Directory.Exists(deletableIndex.FullName);
			if (tmpBool3)
				try
				{
					RmDir(deletableIndex);
				}
				catch (System.Exception e)
				{
					System.Console.Error.WriteLine(e.StackTrace);
				}
		}
		
		/// <summary> Open an IndexWriter<br>
		/// Catch any (expected) IOException<br>
		/// Close the IndexWriter
		/// </summary>
		private static System.String OpenIndexWriter()
		{
			IndexWriter iw = null;
			System.String ret = null;
			try
			{
				iw = new IndexWriter(TEST_INDEX_DIR, new StandardAnalyzer(), true);
			}
			catch (System.IO.IOException e)
			{
				ret = e.ToString();
				System.Console.Error.WriteLine(e.StackTrace);
			}
			catch (System.NullReferenceException e)
			{
				ret = e.ToString();
				System.Console.Error.WriteLine(e.StackTrace);
			}
			finally
			{
				if (iw != null)
				{
					try
					{
						iw.Close();
					}
					catch (System.IO.IOException ioe)
					{
						// ignore this
					}
				}
			}
			return ret;
		}
		
		private static void  RmDir(System.IO.FileInfo dirName)
		{
			bool tmpBool;
			if (System.IO.File.Exists(dirName.FullName))
				tmpBool = true;
			else
				tmpBool = System.IO.Directory.Exists(dirName.FullName);
			if (tmpBool)
			{
				if (System.IO.Directory.Exists(dirName.FullName))
				{
					System.IO.FileInfo[] contents = SupportClass.FileSupport.GetFiles(dirName);
					for (int i = 0; i < contents.Length; i++)
						RmDir(contents[i]);
					bool tmpBool2;
					if (System.IO.File.Exists(dirName.FullName))
					{
						System.IO.File.Delete(dirName.FullName);
						tmpBool2 = true;
					}
					else if (System.IO.Directory.Exists(dirName.FullName))
					{
						System.IO.Directory.Delete(dirName.FullName);
						tmpBool2 = true;
					}
					else
						tmpBool2 = false;
					bool generatedAux = tmpBool2;
				}
				else
				{
					bool tmpBool3;
					if (System.IO.File.Exists(dirName.FullName))
					{
						System.IO.File.Delete(dirName.FullName);
						tmpBool3 = true;
					}
					else if (System.IO.Directory.Exists(dirName.FullName))
					{
						System.IO.Directory.Delete(dirName.FullName);
						tmpBool3 = true;
					}
					else
						tmpBool3 = false;
					bool generatedAux2 = tmpBool3;
				}
			}
		}
	}
#endif
}
*/