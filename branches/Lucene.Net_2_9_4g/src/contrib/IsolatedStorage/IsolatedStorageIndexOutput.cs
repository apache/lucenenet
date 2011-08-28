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
    public class IsolatedStorageIndexOutput : Lucene.Net.Store.BufferedIndexOutput, IDisposable
    {
        System.IO.IsolatedStorage.IsolatedStorageFileStream _file;
        bool _isOpen;

        public IsolatedStorageIndexOutput(System.IO.IsolatedStorage.IsolatedStorageFile dir, string name)
        {
            try
            {
                _file = dir.CreateFile(name);
                _isOpen = true;
            }
            catch (InvalidOperationException e)
            {
                throw new AlreadyClosedException(e.Message);
            }
        }


        public override void FlushBuffer(byte[] b, int offset, int len)
        {
            _file.Write(b, offset, len);
            _file.Flush();
        }


        public override void Close()
        {
            if (_isOpen)
            {
                _isOpen = false;
                base.Close();
                _file.Close();
            }
        }

        public void Dispose()
        {
            Close();
        }


        public override void Seek(long pos)
        {
            base.Seek(pos);
            _file.Seek(pos, SeekOrigin.Begin);
        }

        public override long Length()
        {
            return _file.Length;
        }

        public override void SetLength(long length)
        {
            _file.SetLength(length);
        }
    }
}
