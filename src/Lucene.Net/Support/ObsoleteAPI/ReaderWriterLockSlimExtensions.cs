using System;
using System.Threading;

namespace Lucene.Net.Support.Threading
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

    /// <summary>
    /// Extensions to help obtain/release from a ReaderWriterSlimLock.
    /// Taken from:
    /// http://stackoverflow.com/questions/170028/how-would-you-simplify-entering-and-exiting-a-readerwriterlock
    /// 
    /// LUCENENET specific
    /// </summary>
    [Obsolete("Using these extensions will allocte memory. New code should call EnterReadLock(), ExitReadLock(), EnterWriteLock() or ExitWriteLock() directly in a try/finally block. This class will be removed in 4.8.0 release candidate.")]
    internal static class ReaderWriterLockSlimExtensions
    {
        private sealed class ReadLockToken : IDisposable
        {
            private ReaderWriterLockSlim _readerWriterLockSlim;

            public ReadLockToken(ReaderWriterLockSlim sync)
            {
                _readerWriterLockSlim = sync;
                sync.EnterReadLock();
            }

            public void Dispose()
            {
                if (_readerWriterLockSlim != null)
                {
                    _readerWriterLockSlim.ExitReadLock();
                    _readerWriterLockSlim = null;
                }
            }
        }

        private sealed class WriteLockToken : IDisposable
        {
            private ReaderWriterLockSlim _readerWriterLockSlim;

            public WriteLockToken(ReaderWriterLockSlim sync)
            {
                _readerWriterLockSlim = sync;
                sync.EnterWriteLock();
            }

            public void Dispose()
            {
                if (_readerWriterLockSlim != null)
                {
                    _readerWriterLockSlim.ExitWriteLock();
                    _readerWriterLockSlim = null;
                }
            }
        }

        public static IDisposable Read(this ReaderWriterLockSlim obj)
        {
            return new ReadLockToken(obj);
        }

        public static IDisposable Write(this ReaderWriterLockSlim obj)
        {
            return new WriteLockToken(obj);
        }
    }
}
