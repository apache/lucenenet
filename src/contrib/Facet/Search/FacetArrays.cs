using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class FacetArrays
    {
        private int[] ints;
        private float[] floats;
        public readonly int arrayLength;

        public FacetArrays(int arrayLength)
        {
            this.arrayLength = arrayLength;
        }

        protected virtual float[] NewFloatArray()
        {
            return new float[arrayLength];
        }

        protected virtual int[] NewIntArray()
        {
            return new int[arrayLength];
        }

        protected virtual void DoFree(float[] floats, int[] ints)
        {
        }

        public void Free()
        {
            DoFree(floats, ints);
            ints = null;
            floats = null;
        }

        public int[] GetIntArray()
        {
            if (ints == null)
            {
                ints = NewIntArray();
            }

            return ints;
        }

        public float[] GetFloatArray()
        {
            if (floats == null)
            {
                floats = NewFloatArray();
            }

            return floats;
        }
    }
}
