// LUCENENET TODO: Port issues - missing Transliterator dependency from icu.net

//using Lucene.Net.Analysis.Util;
//using System;
//using System.Collections.Generic;

//namespace Lucene.Net.Analysis.ICU
//{
//    public class ICUTransformFilterFactory : TokenFilterFactory, IMultiTermAwareComponent
//    {
//        private readonly Transliterator transliterator;

//        // TODO: add support for custom rules
//        /// <summary>Creates a new ICUTransformFilterFactory</summary>
//        public ICUTransformFilterFactory(IDictionary<string, string> args)
//            : base(args)
//        {
//            string id = Require(args, "id");
//            string direction = Get(args, "direction", new string[] { "forward", "reverse" }, "forward", false);
//            int dir = "forward".Equals(direction) ? Transliterator.FORWARD : Transliterator.REVERSE;
//            transliterator = Transliterator.getInstance(id, dir);
//            if (args.Count != 0)
//            {
//                throw new ArgumentException("Unknown parameters: " + args);
//            }
//        }

//        public override TokenStream Create(TokenStream input)
//        {
//            return new ICUTransformFilter(input, transliterator);
//        }

//        public virtual AbstractAnalysisFactory GetMultiTermComponent()
//        {
//            return this;
//        }
//    }
//}
