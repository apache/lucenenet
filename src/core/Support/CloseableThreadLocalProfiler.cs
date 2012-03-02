using System;

namespace Lucene.Net.Support
{
    /// <summary>
    /// For Debuging purposes.
    /// </summary>
    public class CloseableThreadLocalProfiler
    {
        private static bool _enableCloseableThreadLocalProfiler = false;
        public static System.Collections.Generic.List<WeakReference> Instances = new System.Collections.Generic.List<WeakReference>();

        public static bool EnableCloseableThreadLocalProfiler
        {
            get { return _enableCloseableThreadLocalProfiler; }
            set
            {
                _enableCloseableThreadLocalProfiler = value;
                lock (Instances)
                    Instances.Clear();
            }
        }
    }
}