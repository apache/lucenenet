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
    public class IsolatedStorageInputStream : BufferedIndexInput, IDisposable
    {
        private const int ChunkSize = Int32.MaxValue;
        internal bool IsClone;

        internal IsolatedStorageFileStream File;

        /// <summary>
        /// Isolated Storage Input Stream
        /// </summary>
        /// <param name="path"></param>
        /// <param name="context"></param>
        /// <remarks>BUFFER_SIZE is from BufferedIndexInput: 1024 * 4</remarks>
        public IsolatedStorageInputStream(string path, ref IsolatedStorageFile context)
            : base(BUFFER_SIZE)
        {
            File = context.OpenFile(path, FileMode.OpenOrCreate, FileAccess.Write);
        }

        /// <summary>IndexInput methods </summary>
        public override void ReadInternal(byte[] b, int offset, int len)
        {
            lock (File)
            {
                var position = GetFilePointer();
                if (position != File.Position)
                {
                    File.Seek(position, SeekOrigin.Begin);
                    File.Position = position;
                }

                var total = 0;

                do
                {
                    int readLength;
                    if (total + ChunkSize > len)
                    {
                        readLength = len - total;
                    }
                    else
                    {
                        // LUCENE-1566 - work around JVM Issue by breaking very large reads into chunks
                        readLength = ChunkSize;
                    }
                    int i = File.Read(b, offset + total, readLength);
                    if (i == -1)
                    {
                        throw new IOException("read past EOF");
                    }
                    File.Position += i;
                    total += i;
                } while (total < len);
            }
        }

        public override void Close()
        {
            // only close the file if this is not a clone
            if (!IsClone)
                File.Close();
        }

        public override void SeekInternal(long position)
        {
        }

        public override long Length()
        {
            return File.Length;
        }

        public override Object Clone()
        {
            var clone = (IsolatedStorageInputStream) base.Clone();
            clone.IsClone = true;

            return clone;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Close();
        }

    }
}