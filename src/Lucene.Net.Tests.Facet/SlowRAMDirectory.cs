// Lucene version compatibility level 4.8.1
using RandomizedTesting.Generators;
using System;
using System.Diagnostics;
using System.Threading;

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


    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
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
        
        public virtual void SetSleepMillis(int sleepMillis)
        {
            this.sleepMillis = sleepMillis;
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

        internal virtual void DoSleep(Random random, int length)
        {
            int sTime = length < 10 ? sleepMillis : (int)(sleepMillis * Math.Log(length));
            if (random != null)
            {
                sTime = random.Next(sTime);
            }

            try
            {
                Sleep(sTime);
            }
            catch (Exception ie) when (ie.IsInterruptedException())
            {
                throw new Util.ThreadInterruptedException(ie);
            }
        }

        // LUCENENET specific - We can't use Thread.Sleep which depends on the clock
        // interrupt frequency, and that frequency might be too low for low values like 1 millisecond.
        private static void Sleep(int milliseconds)
        {
            long ticks = (long)((Stopwatch.Frequency / (double)1000) * milliseconds); // ticks per millisecond * milliseconds = total delay ticks
            long initialTick = Stopwatch.GetTimestamp();
            long targetTick = initialTick + ticks;
            while (Stopwatch.GetTimestamp() < targetTick)
            {
                Thread.SpinWait(1);
            }
        }

        /// <summary>
        /// Make a private random. </summary>
        internal virtual Random ForkRandom()
        {
            if (random is null)
            {
                return null;
            }
            return new J2N.Randomizer(random.NextInt64());
        }

        /// <summary>
        /// Delegate class to wrap an IndexInput and delay reading bytes by some
        /// specified time.
        /// </summary>
        private class SlowIndexInput : IndexInput
        {
            private readonly SlowRAMDirectory outerInstance;

            private readonly IndexInput ii;
            private int numRead = 0;
            private readonly Random rand;

            public SlowIndexInput(SlowRAMDirectory outerInstance, IndexInput ii)
                : base("SlowIndexInput(" + ii + ")")
            {
                this.outerInstance = outerInstance;
                this.rand = outerInstance.ForkRandom();
                this.ii = ii;
            }

            public override byte ReadByte()
            {
                if (numRead >= IO_SLEEP_THRESHOLD)
                {
                    outerInstance.DoSleep(rand, 0);
                    numRead = 0;
                }
                ++numRead;
                return ii.ReadByte();
            }

            public override void ReadBytes(byte[] b, int offset, int len)
            {
                if (numRead >= IO_SLEEP_THRESHOLD)
                {
                    outerInstance.DoSleep(rand, len);
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


            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    ii.Dispose();
                }
            }

            public override bool Equals(object o)
            {
                return ii.Equals(o);
            }

            public override long Position => ii.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream

            public override int GetHashCode()
            {
                return ii.GetHashCode();
            }

            public override long Length => ii.Length;

            public override void Seek(long pos)
            {
                ii.Seek(pos);
            }
        }

        /// <summary>
        /// Delegate class to wrap an IndexOutput and delay writing bytes by some
        /// specified time.
        /// </summary>
        private class SlowIndexOutput : IndexOutput
        {
            private readonly SlowRAMDirectory outerInstance;

            private readonly IndexOutput io;
            private int numWrote;
            private readonly Random rand;
            
            public SlowIndexOutput(SlowRAMDirectory outerInstance, IndexOutput io)
            {
                this.outerInstance = outerInstance;
                this.io = io;
                this.rand = outerInstance.ForkRandom();
            }

            public override void WriteByte(byte b)
            {
                if (numWrote >= IO_SLEEP_THRESHOLD)
                {
                    outerInstance.DoSleep(rand, 0);
                    numWrote = 0;
                }
                ++numWrote;
                io.WriteByte(b);
            }

            public override void WriteBytes(byte[] b, int offset, int length)
            {
                if (numWrote >= IO_SLEEP_THRESHOLD)
                {
                    outerInstance.DoSleep(rand, length);
                    numWrote = 0;
                }
                numWrote += length;
                io.WriteBytes(b, offset, length);
            }

            [Obsolete]
            public override void Seek(long pos)
            {
                io.Seek(pos);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    io.Dispose();
                }
            }

            public override void Flush()
            {
                io.Flush();
            }

            public override long Position => io.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream

            public override long Length
            {
                get => io.Length;
                set => throw IllegalStateException.Create("Length is readonly"); // LUCENENET specific: We cannot override get without also overriding set, so we throw if it is set
            }

            public override long Checksum => io.Checksum;
        }
    }
}