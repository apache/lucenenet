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

using CheckIndex = Lucene.Net.Index.CheckIndex;
using ConcurrentMergeScheduler = Lucene.Net.Index.ConcurrentMergeScheduler;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using MergeScheduler = Lucene.Net.Index.MergeScheduler;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Util
{
	
	public class _TestUtil
	{
		
		public static void  RmDir(System.IO.FileInfo dir)
		{
			bool tmpBool;
			if (System.IO.File.Exists(dir.FullName))
				tmpBool = true;
			else
				tmpBool = System.IO.Directory.Exists(dir.FullName);
			if (tmpBool)
			{
				System.IO.FileInfo[] files = SupportClass.FileSupport.GetFiles(dir);
				for (int i = 0; i < files.Length; i++)
				{
					bool tmpBool2;
					if (System.IO.File.Exists(files[i].FullName))
					{
						System.IO.File.Delete(files[i].FullName);
						tmpBool2 = true;
					}
					else if (System.IO.Directory.Exists(files[i].FullName))
					{
						System.IO.Directory.Delete(files[i].FullName);
						tmpBool2 = true;
					}
					else
						tmpBool2 = false;
					if (!tmpBool2)
					{
						throw new System.IO.IOException("could not delete " + files[i]);
					}
				}
				bool tmpBool3;
				if (System.IO.File.Exists(dir.FullName))
				{
					System.IO.File.Delete(dir.FullName);
					tmpBool3 = true;
				}
				else if (System.IO.Directory.Exists(dir.FullName))
				{
					System.IO.Directory.Delete(dir.FullName);
					tmpBool3 = true;
				}
				else
					tmpBool3 = false;
				bool generatedAux = tmpBool3;
			}
		}
		
		public static void  RmDir(System.String dir)
		{
			RmDir(new System.IO.FileInfo(dir));
		}
		
		public static void  SyncConcurrentMerges(IndexWriter writer)
		{
			SyncConcurrentMerges(writer.GetMergeScheduler());
		}
		
		public static void  SyncConcurrentMerges(MergeScheduler ms)
		{
			if (ms is ConcurrentMergeScheduler)
				((ConcurrentMergeScheduler) ms).Sync();
		}

        /// <summary>
        /// This runs the CheckIndex tool on the index in dir.
        /// If any issues are hit, a System.Exception is thrown; else,
        /// true is returned.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static bool CheckIndex(Directory dir)
        {
            System.IO.StringWriter sw = new System.IO.StringWriter();
            CheckIndex checker = new CheckIndex(dir);
            checker.SetInfoStream(sw);
            CheckIndex.Status indexStatus = checker.CheckIndex_Renamed();
            if (indexStatus == null || !indexStatus.clean)
            {
                System.Console.WriteLine("CheckIndex failed");
                System.Console.WriteLine(sw.ToString());
                throw new System.Exception("CheckIndex failed");
            }
            else
                return true;
        }
	}
}