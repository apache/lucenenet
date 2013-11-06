using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class ReusingFacetArrays : FacetArrays
    {
        private readonly ArraysPool arraysPool;

        public ReusingFacetArrays(ArraysPool arraysPool)
            : base(arraysPool.arrayLength)
        {
            this.arraysPool = arraysPool;
        }

        protected override int[] NewIntArray()
        {
            return arraysPool.AllocateIntArray();
        }

        protected override float[] NewFloatArray()
        {
            return arraysPool.AllocateFloatArray();
        }

        protected override void DoFree(float[] floats, int[] ints)
        {
            arraysPool.Free(floats);
            arraysPool.Free(ints);
        }
    }
}
