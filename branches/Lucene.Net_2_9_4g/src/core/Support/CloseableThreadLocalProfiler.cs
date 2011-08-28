using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support
{
    /// <summary>
    /// For Debuging purposes.
    /// </summary>
    public class CloseableThreadLocalProfiler
    {
        public static bool _EnableCloseableThreadLocalProfiler = false;
        public static List<WeakReference> Instances = new List<WeakReference>();

        public static bool EnableCloseableThreadLocalProfiler
        {
            get { return _EnableCloseableThreadLocalProfiler; }
            set
            {
                _EnableCloseableThreadLocalProfiler = value;
                lock (Instances)
                    Instances.Clear();
            }
        }
    }
}
