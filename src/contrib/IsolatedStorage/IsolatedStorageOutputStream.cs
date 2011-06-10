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

using System.IO;
using System.IO.IsolatedStorage;

namespace Lucene.Net.Store
{
    class IsolatedStorageOutputStream : BufferedIndexOutput
    {

        internal IsolatedStorageFileStream File = null;
        private volatile bool _isOpen; //remember if the file is open so we don't try to close it twice

        public IsolatedStorageOutputStream(string path, ref IsolatedStorageFile context)
        {
            File = context.OpenFile(path, FileMode.OpenOrCreate, FileAccess.Write);
            _isOpen = true;
        }

        /// <summary>output methods: </summary>
        public override void FlushBuffer(byte[] b, int offset, int size)
        {
            File.Write(b, offset, size);
            // {{dougsale-2.4.0}}
            // FSIndexOutput.Flush
            // When writing frequently with small amounts of data, the data isn't flushed to disk.
            // Thus, attempting to read the data soon after this method is invoked leads to
            // BufferedIndexInput.Refill() throwing an IOException for reading past EOF.
            // Test\Index\TestDoc.cs demonstrates such a situation.
            // Forcing a flush here prevents said issue.
            // {{DIGY 2.9.0}}
            // This code is not available in Lucene.Java 2.9.X.
            // Can there be a indexing-performance problem?
            File.Flush();
        }
    
        public override void Close()
        {
            // only close the file if it has not been closed yet
            if (_isOpen)
            {
                bool success = false;
                try
                {
                    base.Close();
                    success = true;
                }
                finally
                {
                    _isOpen = false;
                    if (!success)
                    {
                        try
                        {
                            File.Close();
                        }
                        catch (System.Exception t)
                        {
                            // Suppress so we don't mask original exception
                        }
                    }
                    else
                        File.Close();
                }
            }
        }

        /// <summary>Random-access methods </summary>
        public override void Seek(long pos)
        {
            base.Seek(pos);
            File.Seek(pos, System.IO.SeekOrigin.Begin);
        }
   
        public override long Length()
        {
            return File.Length;
        }
        
        public override void SetLength(long length)
        {
            File.SetLength(length);
        }

    }
}
