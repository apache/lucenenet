using System.Collections.Generic;
using System.Diagnostics;

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
    /// IOContext holds additional details on the merge/search context. A IOContext
    /// object can never be initialized as null as passed as a parameter to either
    /// <seealso cref="Lucene.Net.Store.Directory#openInput(String, IOContext)"/> or
    /// <seealso cref="Lucene.Net.Store.Directory#createOutput(String, IOContext)"/>
    /// </summary>
    public class IOContext
    {
        /// <summary>
        /// Context is a enumerator which specifies the context in which the Directory
        /// is being used for.
        /// </summary>
        public enum Context_e
        {
            MERGE,
            READ,
            FLUSH,
            DEFAULT
        }

        /// <summary>
        /// An object of a enumerator Context type
        /// </summary>
        public readonly Context_e Context;

        public readonly MergeInfo MergeInfo;

        public readonly FlushInfo FlushInfo;

        public readonly bool ReadOnce;

        public static readonly IOContext DEFAULT = new IOContext(Context_e.DEFAULT);

        public static readonly IOContext READONCE = new IOContext(true);

        public static readonly IOContext READ = new IOContext(false);

        public IOContext()
            : this(false)
        {
        }

        public IOContext(FlushInfo flushInfo)
        {
            Debug.Assert(flushInfo != null);
            this.Context = Context_e.FLUSH;
            this.MergeInfo = null;
            this.ReadOnce = false;
            this.FlushInfo = flushInfo;
        }

        public IOContext(Context_e context)
            : this(context, null)
        {
        }

        private IOContext(bool readOnce)
        {
            this.Context = Context_e.READ;
            this.MergeInfo = null;
            this.ReadOnce = readOnce;
            this.FlushInfo = null;
        }

        public IOContext(MergeInfo mergeInfo)
            : this(Context_e.MERGE, mergeInfo)
        {
        }

        private IOContext(Context_e context, MergeInfo mergeInfo)
        {
            Debug.Assert(context != Context_e.MERGE || mergeInfo != null, "MergeInfo must not be null if context is MERGE");
            Debug.Assert(context != Context_e.FLUSH, "Use IOContext(FlushInfo) to create a FLUSH IOContext");
            this.Context = context;
            this.ReadOnce = false;
            this.MergeInfo = mergeInfo;
            this.FlushInfo = null;
        }

        /// <summary>
        /// this constructor is used to initialize a <seealso cref="IOContext"/> instance with a new value for the readOnce variable. </summary>
        /// <param name="ctxt"> <seealso cref="IOContext"/> object whose information is used to create the new instance except the readOnce variable. </param>
        /// <param name="readOnce"> The new <seealso cref="IOContext"/> object will use this value for readOnce.  </param>
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
            result = prime * result + ((Context == null) ? 0 : Context.GetHashCode());
            result = prime * result + ((FlushInfo == null) ? 0 : FlushInfo.GetHashCode());
            result = prime * result + ((MergeInfo == null) ? 0 : MergeInfo.GetHashCode());
            result = prime * result + (ReadOnce ? 1231 : 1237);
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj == null)
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
            if (FlushInfo == null)
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
            if (MergeInfo == null)
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

        public static IEnumerable<Context_e> ContextValues()
        {
            // .NET port: This is to make up for enums in .NET not having a Values method.
            yield return Context_e.DEFAULT;
            yield return Context_e.FLUSH;
            yield return Context_e.MERGE;
            yield return Context_e.READ;
        }

        public override string ToString()
        {
            return "IOContext [context=" + Context + ", mergeInfo=" + MergeInfo + ", flushInfo=" + FlushInfo + ", readOnce=" + ReadOnce + "]";
        }
    }
}