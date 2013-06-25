using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public sealed class NoMergeScheduler : MergeScheduler
    {
        public static readonly MergeScheduler INSTANCE = new NoMergeScheduler();

        private NoMergeScheduler()
        {
            // prevent instantiation
        }

        protected override void Dispose(bool disposing)
        {
        }

        public override void Merge(IndexWriter writer)
        {
        }

        public override object Clone()
        {
            return this;
        }
    }
}
