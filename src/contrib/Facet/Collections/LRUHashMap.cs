using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Collections
{
    public class LRUHashMap<K, V> : LRUCache<K, V>
    {
        //private int maxSize;

        public LRUHashMap(int maxSize)
            : base(maxSize)
        {
            //this.maxSize = maxSize;
        }

        //public virtual int GetMaxSize()
        //{
        //    return maxSize;
        //}

        //public virtual void SetMaxSize(int maxSize)
        //{
        //    this.maxSize = maxSize;
        //}

        //protected override bool RemoveEldestEntry(Map.Entry<K, V> eldest)
        //{
        //    return Size() > maxSize;
        //}

        //public override LRUHashMap<K, V> Clone()
        //{
        //    return (LRUHashMap<K, V>)base.Clone();
        //}
    }
}
