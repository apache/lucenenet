using System;

namespace Lucene.Net.Search.Similarities
{
    public class BasicModelP : BasicModel
    {
        protected static double LOG2_E = SimilarityBase.Log2(Math.E);

        public override sealed float Score(BasicStats stats, float tfn)
        {
            float lambda = (float) (stats.TotalTermFreq + 1)/(stats.NumberOfDocuments + 1);
            return (float) (tfn*SimilarityBase.Log2(tfn/lambda)
                            + (lambda + 1/(12*tfn) - tfn)*LOG2_E
                            + 0.5*SimilarityBase.Log2(2*Math.PI*tfn));
        }

        public override string ToString()
        {
            return "P";
        }
    }
}