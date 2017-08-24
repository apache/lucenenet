// LUCENENET TODO: Port issues - missing Normalizer2 dependency from icu.net

//using Lucene.Net.Analysis.Util;
//using System;
//using System.Collections.Generic;

//namespace Lucene.Net.Analysis.ICU
//{
//    public class ICUFoldingFilterFactory : TokenFilterFactory, IMultiTermAwareComponent
//    {
//        /// <summary>Creates a new ICUFoldingFilterFactory</summary>
//        public ICUFoldingFilterFactory(IDictionary<string, string> args)
//            : base(args)
//        {
//            if (args.Count != 0)
//            {
//                throw new ArgumentException("Unknown parameters: " + args);
//            }
//        }

//        public override TokenStream Create(TokenStream input)
//        {
//            return new ICUFoldingFilter(input);
//        }

//        public virtual AbstractAnalysisFactory GetMultiTermComponent()
//        {
//            return this;
//        }
//    }
//}
