using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public sealed class NoDeletionPolicy : IndexDeletionPolicy
    {
        public static readonly IndexDeletionPolicy INSTANCE = new NoDeletionPolicy();

        private NoDeletionPolicy()
        {
            // keep private to avoid instantiation
        }

        public override void OnCommit<T>(IList<T> commits)
        {
        }

        public override void OnInit<T>(IList<T> commits)
        {
        }

        public override object Clone()
        {
            return this;
        }
    }
}
