using Lucene.Net.Diagnostics;
using Lucene.Net.Support;

namespace Lucene.Net.Store
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
    /// <see cref="IOContext"/> holds additional details on the merge/search context. A <see cref="IOContext"/>
    /// object can never be initialized as null as passed as a parameter to either
    /// <see cref="Directory.OpenInput(string, IOContext)"/> or
    /// <see cref="Directory.CreateOutput(string, IOContext)"/>
    /// </summary>
    [ExceptionToClassNameConvention]
    public class IOContext
    {
        /// <summary>
        /// <see cref="UsageContext"/> is a enumeration which specifies the context in which the <see cref="Directory"/>
        /// is being used for.
        /// <para/>
        /// NOTE: This was Context in Lucene
        /// </summary>
        public enum UsageContext
        {
            DEFAULT = 0, // LUCENENET NOTE: 0 is the default for any value type, so when not initialized, this is the value we get
            MERGE,
            READ,
            FLUSH
        }

        /// <summary>
        /// A <see cref="UsageContext"/> setting
        /// </summary>
        public UsageContext Context { get; private set; }

        public MergeInfo MergeInfo { get; private set; }

        public FlushInfo FlushInfo { get; private set; }

        public bool ReadOnce { get; private set; }

        public static readonly IOContext DEFAULT = new IOContext(UsageContext.DEFAULT);

        public static readonly IOContext READ_ONCE = new IOContext(true);

        public static readonly IOContext READ = new IOContext(false);

        public IOContext()
            : this(false)
        {
        }

        public IOContext(FlushInfo flushInfo)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(flushInfo != null);
            this.Context = UsageContext.FLUSH;
            this.MergeInfo = null;
            this.ReadOnce = false;
            this.FlushInfo = flushInfo;
        }

        public IOContext(UsageContext context)
            : this(context, null)
        {
        }

        private IOContext(bool readOnce)
        {
            this.Context = UsageContext.READ;
            this.MergeInfo = null;
            this.ReadOnce = readOnce;
            this.FlushInfo = null;
        }

        public IOContext(MergeInfo mergeInfo)
            : this(UsageContext.MERGE, mergeInfo)
        {
        }

        private IOContext(UsageContext context, MergeInfo mergeInfo)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(context != UsageContext.MERGE || mergeInfo != null, "MergeInfo must not be null if context is MERGE");
                Debugging.Assert(context != UsageContext.FLUSH, "Use IOContext(FlushInfo) to create a FLUSH IOContext");
            }
            this.Context = context;
            this.ReadOnce = false;
            this.MergeInfo = mergeInfo;
            this.FlushInfo = null;
        }

        /// <summary>
        /// This constructor is used to initialize a <see cref="IOContext"/> instance with a new value for the <see cref="ReadOnce"/> property. </summary>
        /// <param name="ctxt"> <see cref="IOContext"/> object whose information is used to create the new instance except the <see cref="ReadOnce"/> property. </param>
        /// <param name="readOnce"> The new <see cref="IOContext"/> object will use this value for <see cref="ReadOnce"/>.  </param>
        public IOContext(IOContext ctxt, bool readOnce)
        {
            this.Context = ctxt.Context;
            this.MergeInfo = ctxt.MergeInfo;
            this.FlushInfo = ctxt.FlushInfo;
            this.ReadOnce = readOnce;
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + /*((Context is null) ? 0 :*/ Context.GetHashCode()/*)*/; // LUCENENET NOTE: Enum can never be null in .NET
            result = prime * result + ((FlushInfo is null) ? 0 : FlushInfo.GetHashCode());
            result = prime * result + ((MergeInfo is null) ? 0 : MergeInfo.GetHashCode());
            result = prime * result + (ReadOnce ? 1231 : 1237);
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj is null)
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            IOContext other = (IOContext)obj;
            if (Context != other.Context)
            {
                return false;
            }
            if (FlushInfo is null)
            {
                if (other.FlushInfo != null)
                {
                    return false;
                }
            }
            else if (!FlushInfo.Equals(other.FlushInfo))
            {
                return false;
            }
            if (MergeInfo is null)
            {
                if (other.MergeInfo != null)
                {
                    return false;
                }
            }
            else if (!MergeInfo.Equals(other.MergeInfo))
            {
                return false;
            }
            if (ReadOnce != other.ReadOnce)
            {
                return false;
            }
            return true;
        }

        public override string ToString()
        {
            return "IOContext [context=" + Context + ", mergeInfo=" + MergeInfo + ", flushInfo=" + FlushInfo + ", readOnce=" + ReadOnce + "]";
        }
    }
}