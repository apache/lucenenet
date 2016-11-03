using System;
using System.Threading;
using Lucene.Net.Randomized.Generators;

namespace Lucene.Net.Facet
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


    using IOContext = Lucene.Net.Store.IOContext;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;

    /// <summary>
    /// Test utility - slow directory
    /// </summary>
    // TODO: move to test-framework and sometimes use in tests?
    public class SlowRAMDirectory : RAMDirectory
    {

        private const int IO_SLEEP_THRESHOLD = 50;

        internal Random random;
        private int sleepMillis;

        public virtual int SleepMillis
        {
            set
            {
                this.sleepMillis = value;
            }
        }

        public SlowRAMDirectory(int sleepMillis, Random random)
        {
            this.sleepMillis = sleepMillis;
            this.random = random;
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            if (sleepMillis != -1)
            {
                return new SlowIndexOutput(this, base.CreateOutput(name, context));
            }

            return base.CreateOutput(name, context);
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            if (sleepMillis != -1)
            {
                return new SlowIndexInput(this, base.OpenInput(name, context));
            }
            return base.OpenInput(name, context);
        }

        internal virtual void doSleep(Random random, int length)
        {
            int sTime = length < 10 ? sleepMillis : (int)(sleepMillis * Math.Log(length));
            if (random != null)
            {
                sTime = random.Next(sTime);
            }
#if !NETCORE
            try
            {
#endif
            Thread.Sleep(sTime);
#if !NETCORE
            }
            catch (ThreadInterruptedException e)
            {
                throw new ThreadInterruptedException("Thread Interrupted Exception", e);
            }
#endif
        }

        /// <summary>
        /// Make a private random. </summary>
        internal virtual Random forkRandom()
        {
            if (random == null)
            {
                return null;
            }
            return new Random((int)random.NextLong());
        }

        /// <summary>
        /// Delegate class to wrap an IndexInput and delay reading bytes by some
        /// specified time.
        /// </summary>
        private class SlowIndexInput : IndexInput
        {
            private readonly SlowRAMDirectory outerInstance;

            internal IndexInput ii;
            internal int numRead = 0;
            internal Random rand;

            public SlowIndexInput(SlowRAMDirectory outerInstance, IndexInput ii)
                : base("SlowIndexInput(" + ii + ")")
            {
                this.outerInstance = outerInstance;
                this.rand = outerInstance.forkRandom();
                this.ii = ii;
            }

            public override byte ReadByte()
            {
                if (numRead >= IO_SLEEP_THRESHOLD)
                {
                    outerInstance.doSleep(rand, 0);
                    numRead = 0;
                }
                ++numRead;
                return ii.ReadByte();
            }

            public override void ReadBytes(byte[] b, int offset, int len)
            {
                if (numRead >= IO_SLEEP_THRESHOLD)
                {
                    outerInstance.doSleep(rand, len);
                    numRead = 0;
                }
                numRead += len;
                ii.ReadBytes(b, offset, len);
            }


            // TODO: is it intentional that clone doesnt wrap?
            public override object Clone()
            {
                return ii.Clone();
            }


            public override void Dispose()
            {
                ii.Dispose();
            }
            public override bool Equals(object o)
            {
                return ii.Equals(o);
            }
            public override long FilePointer
            {
                get
                {
                    return ii.FilePointer;
                }
            }

            public override void Seek(long pos)
            {
                ii.Seek(pos);
            }


            public override int GetHashCode()
            {
                return ii.GetHashCode();
            }
            public override long Length()
            {
                return ii.Length();
            }

        }

        /// <summary>
        /// Delegate class to wrap an IndexOutput and delay writing bytes by some
        /// specified time.
        /// </summary>
        private class SlowIndexOutput : IndexOutput
        {
            private readonly SlowRAMDirectory outerInstance;


            internal IndexOutput io;
            internal int numWrote;
            internal readonly Random rand;

            public SlowIndexOutput(SlowRAMDirectory outerInstance, IndexOutput io)
            {
                this.outerInstance = outerInstance;
                this.io = io;
                this.rand = outerInstance.forkRandom();
            }

            public override void WriteByte(byte b)
            {
                if (numWrote >= IO_SLEEP_THRESHOLD)
                {
                    outerInstance.doSleep(rand, 0);
                    numWrote = 0;
                }
                ++numWrote;
                io.WriteByte(b);
            }

            public override void WriteBytes(byte[] b, int offset, int length)
            {
                if (numWrote >= IO_SLEEP_THRESHOLD)
                {
                    outerInstance.doSleep(rand, length);
                    numWrote = 0;
                }
                numWrote += length;
                io.WriteBytes(b, offset, length);
            }

            public override void Dispose()
            {
                io.Dispose();
            }
            public override void Flush()
            {
                io.Flush();
            }
            public override long FilePointer
            {
                get
                {
                    return io.FilePointer;
                }
            }

            [Obsolete]
            public override void Seek(long pos)
            {
                io.Seek(pos);
            }

            public override long Checksum
            {
                get
                {
                    return io.Checksum;
                }
            }
        }

    }

}