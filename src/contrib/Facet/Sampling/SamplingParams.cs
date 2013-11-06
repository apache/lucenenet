using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Sampling
{
    public class SamplingParams
    {
        public static readonly double DEFAULT_OVERSAMPLE_FACTOR = 2.0;
        public static readonly double DEFAULT_SAMPLE_RATIO = 0.0;
        public static readonly int DEFAULT_MAX_SAMPLE_SIZE = 10000;
        public static readonly int DEFAULT_MIN_SAMPLE_SIZE = 100;
        public static readonly int DEFAULT_SAMPLING_THRESHOLD = 75000;
        private int maxSampleSize = DEFAULT_MAX_SAMPLE_SIZE;
        private int minSampleSize = DEFAULT_MIN_SAMPLE_SIZE;
        private double sampleRatio = DEFAULT_SAMPLE_RATIO;
        private int samplingThreshold = DEFAULT_SAMPLING_THRESHOLD;
        private double oversampleFactor = DEFAULT_OVERSAMPLE_FACTOR;

        public int MaxSampleSize
        {
            get
            {
                return maxSampleSize;
            }
            set
            {
                maxSampleSize = value;
            }
        }

        public int MinSampleSize
        {
            get
            {
                return minSampleSize;
            }
            set
            {
                minSampleSize = value;
            }
        }

        public double SampleRatio
        {
            get
            {
                return sampleRatio;
            }
            set
            {
                sampleRatio = value;
            }
        }

        public int SamplingThreshold
        {
            get
            {
                return samplingThreshold;
            }
            set
            {
                samplingThreshold = value;
            }
        }
        
        public virtual bool Validate()
        {
            return samplingThreshold >= maxSampleSize && maxSampleSize >= minSampleSize && sampleRatio > 0 && sampleRatio < 1;
        }

        public double OversampleFactor
        {
            get
            {
                return oversampleFactor;
            }
            set
            {
                oversampleFactor = value;
            }
        }
    }
}
