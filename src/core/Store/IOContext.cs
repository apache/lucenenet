using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Store
{
    public class IOContext
    {
        public enum Context
        {
            MERGE, READ, FLUSH, DEFAULT
        };

        public readonly Context context;

        public readonly MergeInfo mergeInfo;

        public readonly FlushInfo flushInfo;

        public readonly bool readOnce;

        public static readonly IOContext DEFAULT = new IOContext(Context.DEFAULT);

        public static readonly IOContext READONCE = new IOContext(true);

        public static readonly IOContext READ = new IOContext(false);

        public IOContext()
            : this(false)
        {
        }

        public IOContext(FlushInfo flushInfo)
        {
            //assert flushInfo != null;
            this.context = Context.FLUSH;
            this.mergeInfo = null;
            this.readOnce = false;
            this.flushInfo = flushInfo;
        }

        public IOContext(Context context)
            : this(context, null)
        {
        }

        private IOContext(bool readOnce)
        {
            this.context = Context.READ;
            this.mergeInfo = null;
            this.readOnce = readOnce;
            this.flushInfo = null;
        }

        public IOContext(MergeInfo mergeInfo)
            : this(Context.MERGE, mergeInfo)
        {
        }

        private IOContext(Context context, MergeInfo mergeInfo)
        {
            //assert context != Context.MERGE || mergeInfo != null : "MergeInfo must not be null if context is MERGE";
            //assert context != Context.FLUSH : "Use IOContext(FlushInfo) to create a FLUSH IOContext";
            this.context = context;
            this.readOnce = false;
            this.mergeInfo = mergeInfo;
            this.flushInfo = null;
        }

        public IOContext(IOContext ctxt, bool readOnce)
        {
            this.context = ctxt.context;
            this.mergeInfo = ctxt.mergeInfo;
            this.flushInfo = ctxt.flushInfo;
            this.readOnce = readOnce;
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + ((context == null) ? 0 : context.GetHashCode());
            result = prime * result + ((flushInfo == null) ? 0 : flushInfo.GetHashCode());
            result = prime * result + ((mergeInfo == null) ? 0 : mergeInfo.GetHashCode());
            result = prime * result + (readOnce ? 1231 : 1237);
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            IOContext other = (IOContext)obj;
            if (context != other.context)
                return false;
            if (flushInfo == null)
            {
                if (other.flushInfo != null)
                    return false;
            }
            else if (!flushInfo.Equals(other.flushInfo))
                return false;
            if (mergeInfo == null)
            {
                if (other.mergeInfo != null)
                    return false;
            }
            else if (!mergeInfo.Equals(other.mergeInfo))
                return false;
            if (readOnce != other.readOnce)
                return false;
            return true;
        }

        public override string ToString()
        {
            return "IOContext [context=" + context + ", mergeInfo=" + mergeInfo
                + ", flushInfo=" + flushInfo + ", readOnce=" + readOnce + "]";
        }
    }
}
