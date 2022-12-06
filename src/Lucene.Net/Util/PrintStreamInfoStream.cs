using J2N.Threading.Atomic;
using Lucene.Net.Support.IO;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Util
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
    /// LUCENENET specific stub to assist with migration to <see cref="TextWriterInfoStream"/>.
    /// </summary>
    [Obsolete("Use TextWriterInfoStream in .NET. This class is provided only to assist with the transition.")]
    public class PrintStreamInfoStream : TextWriterInfoStream
    {
        public PrintStreamInfoStream(TextWriter stream)
            : base(stream)
        { }

        public PrintStreamInfoStream(TextWriter stream, int messageID)
            : base(stream, messageID)
        { }
    }

    /// <summary>
    /// <see cref="InfoStream"/> implementation over a <see cref="TextWriter"/>
    /// such as <see cref="System.Console.Out"/>.
    /// <para/>
    /// NOTE: This is analogous to PrintStreamInfoStream in Lucene.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public class TextWriterInfoStream : InfoStream
    {
        // Used for printing messages
        private static readonly AtomicInt32 MESSAGE_ID = new AtomicInt32();

        protected readonly int m_messageID;
        protected readonly TextWriter m_stream;
        private readonly bool isSystemStream;

        public TextWriterInfoStream(TextWriter stream)
            : this(stream, MESSAGE_ID.GetAndIncrement())
        {
        }

        public TextWriterInfoStream(TextWriter stream, int messageID)
        {
            // LUCENENET: Since we are wrapping our TextWriter to make it safe to use
            // after calling Dispose(), we need to determine whether it is a system stream
            // here instead of on demand.
            this.isSystemStream = stream == Console.Out || stream == Console.Error;
            this.m_stream = typeof(SafeTextWriterWrapper).IsAssignableFrom(stream.GetType()) ? stream : new SafeTextWriterWrapper(stream);
            this.m_messageID = messageID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Message(string component, string message)
        {
            m_stream.Write(component + " " + m_messageID + " [" + DateTime.Now + "; " + Thread.CurrentThread.Name + "]: " + message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsEnabled(string component)
        {
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsSystemStream)
            {
                m_stream.Dispose();
            }
            base.Dispose(disposing); // LUCENENET specific - disposable pattern requires calling the base class implementation
        }

        public virtual bool IsSystemStream => isSystemStream;
    }
}