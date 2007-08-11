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

using Directory = Lucene.Net.Store.Directory;
using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using _TestUtil = Lucene.Net.Util._TestUtil;

namespace Lucene.Net
{
	
	class StoreTest
	{
		[STAThread]
		public static void  Main(System.String[] args)
		{
			try
			{
				Test(1000, true, true);
			}
			catch (System.Exception e)
			{
                System.Console.Out.WriteLine(e.StackTrace);
			}
		}
		
        public static void  Test(int count, bool ram, bool buffered)
		{
			System.Random gen = new System.Random((System.Int32) 1251971);
			int i;
			
			System.DateTime veryStart = System.DateTime.Now;
			System.DateTime start = System.DateTime.Now;
			
			Directory store;
			if (ram)
				store = new RAMDirectory();
			else
			{
				System.String dirName = "test.store";
				_TestUtil.RmDir(dirName);
				store = FSDirectory.GetDirectory(dirName);
			}
			
			int LENGTH_MASK = 0xFFF;

            byte[] buffer = new byte[LENGTH_MASK];
			
			for (i = 0; i < count; i++)
			{
				System.String name = i + ".dat";
				int length = gen.Next() & LENGTH_MASK;
				byte b = (byte) (gen.Next() & 0x7F);
				//System.out.println("filling " + name + " with " + length + " of " + b);
				
				IndexOutput file = store.CreateOutput(name);
				
                if (buffered)
                {
                    for (int j = 0; j < length; j++)
                        buffer[j] = b;
                    file.WriteBytes(buffer, length);
                }
                else
                {
                    for (int j = 0; j < length; j++)
                        file.WriteByte(b);
                }
				
				file.Close();
			}
			
			store.Close();
			
			System.DateTime end = System.DateTime.Now;
			
			System.Console.Out.Write(end.Ticks - start.Ticks);
			System.Console.Out.WriteLine(" total milliseconds to create");
			
			//UPGRADE_TODO: The differences in the expected value  of parameters for constructor 'java.util.Random.Random'  may cause compilation errors.  "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1092'"
			gen = new System.Random((System.Int32) 1251971);
			start = System.DateTime.Now;
			
			if (!ram)
				store = FSDirectory.GetDirectory("test.store");
			
			for (i = 0; i < count; i++)
			{
				System.String name = i + ".dat";
				int length = gen.Next() & LENGTH_MASK;
				byte b = (byte) (gen.Next() & 0x7F);
				//System.out.println("reading " + name + " with " + length + " of " + b);
				
				IndexInput file = store.OpenInput(name);
				
				if (file.Length() != length)
					throw new System.Exception("length incorrect");
				
                byte[] content = new byte[length];
                if (buffered)
                {
                    file.ReadBytes(content, 0, length);
                    // check the buffer
                    for (int j = 0; j < length; j++)
                        if (content[j] != b)
                            throw new System.Exception("contents incorrect");
                }
                else
                {
                    for (int j = 0; j < length; j++)
                        if (file.ReadByte() != b)
                            throw new System.Exception("contents incorrect");
                }
				
				file.Close();
			}
			
			end = System.DateTime.Now;
			
			System.Console.Out.Write(end.Ticks - start.Ticks);
			System.Console.Out.WriteLine(" total milliseconds to read");
			
			gen = new System.Random((System.Int32) 1251971);
			start = System.DateTime.Now;
			
			for (i = 0; i < count; i++)
			{
				System.String name = i + ".dat";
				//System.out.println("deleting " + name);
				store.DeleteFile(name);
			}
			
			end = System.DateTime.Now;
			
			System.Console.Out.Write(end.Ticks - start.Ticks);
			System.Console.Out.WriteLine(" total milliseconds to delete");
			
			System.Console.Out.Write(end.Ticks - veryStart.Ticks);
			System.Console.Out.WriteLine(" total milliseconds");
			
			store.Close();
		}
	}
}