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
    /// <summary>
    /// Isolated Storage Input Stream
    /// </summary>
    public class IsolatedStorageIndexInput : Lucene.Net.Store.BufferedIndexInput, IDisposable
    {
        System.IO.IsolatedStorage.IsolatedStorageFileStream _file;
        bool _isClone = false;

        public IsolatedStorageIndexInput(System.IO.IsolatedStorage.IsolatedStorageFile dir, string name)
        {
            try
            {
                _file = dir.OpenFile(name, FileMode.Open);
            }
            catch (InvalidOperationException e)
            {
                throw new AlreadyClosedException(e.Message);
            }

        }

        public override System.Object Clone()
        {
            IsolatedStorageIndexInput clone = (IsolatedStorageIndexInput)base.Clone();
            clone._isClone = true;
            return clone;
        }

        public override void ReadInternal(byte[] b, int offset, int len)
        {
            lock (_file)
            {
                long position = GetFilePointer();
                if (position != _file.Position)
                {
                    _file.Position = position;
                }

                int read = 0;
                while (read != len)
                {
                    read += _file.Read(b, offset + read, len - read);
                }
            }
        }

        public override void Close()
        {
            if (!_isClone)
                _file.Close();
        }

        public void Dispose()
        {
            Close();
        }

        public override void SeekInternal(long pos)
        {

        }

        public override long Length()
        {
            return _file.Length;
        }
    }
}