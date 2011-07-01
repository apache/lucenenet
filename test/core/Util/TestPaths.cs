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

using System;
using System.IO;

namespace Lucene.Net.Util
{
	using NUnit.Framework;
	
	[TestFixture]
	[Category("Infrastructure")]
	public class TestPaths
	{
		
		[Test]
		public void ValidateTempDirectory()
		{
			string tempDir = null;
			
			try 
			{
			   tempDir = Paths.TempDirectory;
			} 
			catch(Exception ex) 
			{	
				Assert.Fail(
					"An exception occurred when attempting to get the temp directory. " +
					" The tests are heavily dependant on being able to access that directory existing " +
					" and being accessible to the account running the tests. \n\n exception: " +
					ex.Message + "\n\n stack trace: " + ex.StackTrace); 
			}
			Console.WriteLine(tempDir);
			Assert.True(Directory.Exists(tempDir), "The temp directory should exist: " + tempDir);
		}
	}
}

