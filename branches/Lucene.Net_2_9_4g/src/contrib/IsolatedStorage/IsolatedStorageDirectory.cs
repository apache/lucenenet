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
using System.IO.IsolatedStorage;

namespace Lucene.Net.Store
{

    public class IsolatedStorageDirectory : Lucene.Net.Store.Directory
    {
        System.IO.IsolatedStorage.IsolatedStorageFile _is = null;
        string _dirName;

        public IsolatedStorageDirectory(System.IO.DirectoryInfo dInfo)
            : this(dInfo.FullName)
        {
        }

        public IsolatedStorageDirectory(string dirName)
        {
            _is = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForAssembly();

            _dirName = dirName;
            _is.CreateDirectory(dirName);
        }

        public static void Remove(string dirName)
        {
            var dir = new IsolatedStorageDirectory(dirName);
            dir.Remove();
            dir.Close();
        }

        public void Remove()
        {
            foreach (var f in ListAll())
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        _is.DeleteFile(Path.Combine(_dirName, f));
                        break;
                    }
                    catch
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
            }

            try
            {
                _is.DeleteDirectory(_dirName);
            }
            catch { }
        }

        public override Lock MakeLock(string name)
        {
            return new IsolatedStorageLock(_is, Path.Combine(_dirName, name));
        }

        public override string[] ListAll()
        {
            return _is.GetFileNames(Path.Combine(_dirName, "*"));
        }

        public override string[] List()
        {
            return ListAll();
        }

        public override bool FileExists(string name)
        {
            return _is.FileExists(Path.Combine(_dirName, name));
        }

        public override long FileModified(string name)
        {
            return GetDate(_is.GetLastWriteTime(Path.Combine(_dirName, name)).LocalDateTime);
        }

        public override void TouchFile(string name)
        {
            _is.OpenFile(name, FileMode.OpenOrCreate).Close();
        }

        public override void DeleteFile(string name)
        {
            _is.DeleteFile(Path.Combine(_dirName, name));
        }

        public override void RenameFile(string from, string to)
        {
            _is.MoveFile(Path.Combine(_dirName, from), Path.Combine(_dirName, to));
        }

        public override long FileLength(string name)
        {
            using (var f = _is.OpenFile(Path.Combine(_dirName, name), FileMode.Open))
            {
                return f.Length;
            }
        }

        public override IndexOutput CreateOutput(string name)
        {
            return new IsolatedStorageIndexOutput(_is, Path.Combine(_dirName, name));
        }

        public override IndexInput OpenInput(string name)
        {
            return new IsolatedStorageIndexInput(_is, Path.Combine(_dirName, name));
        }

        public override void Close()
        {
            _is.Close();
        }

        public override void Dispose()
        {
            Close();
        }


        private long GetDate(DateTime dt)
        {
            return (long)dt.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
        }
    }
}