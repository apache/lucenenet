// LUCENENET TODO: Port issues - missing Normalizer2 dependency from icu.net

//using Icu;
//using Lucene.Net.Support;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Lucene.Net.Analysis.ICU
//{
//    public sealed class ICUFoldingFilter : ICUNormalizer2Filter
//    {
//        private static readonly Normalizer2 normalizer;

//        /// <summary>
//        /// Create a new ICUFoldingFilter on the specified input
//        /// </summary>
//        public ICUFoldingFilter(TokenStream input)
//            : base(input, normalizer)
//        {
//        }

//        static ICUFoldingFilter()
//        {
//            normalizer = Normalizer2.GetInstance(
//                typeof(ICUFoldingFilter).Assembly.FindAndGetManifestResourceStream(typeof(ICUFoldingFilter), "utr30.nrm"),
//                "utr30", Normalizer2.Mode.COMPOSE);
//        }
//    }
//}
