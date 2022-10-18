using System;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support.Threading;

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
    /// Debugging API for Lucene classes such as <see cref="Index.IndexWriter"/>
    /// and <see cref="Index.SegmentInfos"/>.
    /// <para>
    /// NOTE: Enabling infostreams may cause performance degradation
    /// in some components.
    /// </para>
    /// @lucene.internal
    /// </summary>
    public abstract class InfoStream : IDisposable // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        /// <summary>
        /// Instance of <see cref="InfoStream"/> that does no logging at all. </summary>
        public static readonly InfoStream NO_OUTPUT = new NoOutput();

        private sealed class NoOutput : InfoStream
        {
            public override void Message(string component, string message)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(false, "Message() should not be called when IsEnabled returns false");
            }

            public override bool IsEnabled(string component)
            {
                return false;
            }

            protected override void Dispose(bool disposing)
            {
                // LUCENENET: Intentionally blank
            }
        }

        /// <summary>
        /// Prints a message </summary>
        public abstract void Message(string component, string message);

        /// <summary>
        /// Returns <c>true</c> if messages are enabled and should be posted to <see cref="Message(string, string)"/>. </summary>
        public abstract bool IsEnabled(string component);

        private static InfoStream defaultInfoStream = NO_OUTPUT;

        /// <summary>
        /// Gets or Sets the default <see cref="InfoStream"/> used by a newly instantiated classes. </summary>
        public static InfoStream Default
        {
            get
            {
                UninterruptableMonitor.Enter(typeof(InfoStream));
                try
                {
                    return defaultInfoStream;
                }
                finally
                {
                    UninterruptableMonitor.Exit(typeof(InfoStream));
                }
            }
            set
            {
                UninterruptableMonitor.Enter(typeof(InfoStream));
                try
                {
                    defaultInfoStream = value ?? throw new ArgumentNullException(
                        nameof(Default),
                        "Cannot set InfoStream default implementation to null. " +
                        "To disable logging use InfoStream.NO_OUTPUT");
                }
                finally
                {
                    UninterruptableMonitor.Exit(typeof(InfoStream));
                }
            }
        }

        // LUCENENET specific - implementing proper dispose pattern
        /// <summary>
        /// Disposes this <see cref="InfoStream"/>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes this <see cref="InfoStream"/>
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Clones this <see cref="InfoStream"/>
        /// </summary>
        public virtual object Clone()
        {
            return MemberwiseClone(); // LUCENENET: No exception can occur in .NET and there is no need to cast.
        }
    }
}