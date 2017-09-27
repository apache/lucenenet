// LUCENENET TODO: Port issues - missing Normalizer2 dependency from icu.net

//using Icu;
//using Lucene.Net.Analysis.TokenAttributes;
//using Lucene.Net.Support;

//namespace Lucene.Net.Analysis.ICU
//{
//    public class ICUNormalizer2Filter : TokenFilter
//    {
//        private readonly ICharTermAttribute termAtt;
//        private readonly Normalizer2 normalizer;

//        /// <summary>
//        /// Create a new <see cref="Normalizer2Filter"/> that combines NFKC normalization, Case
//        /// Folding, and removes Default Ignorables (NFKC_Casefold)
//        /// </summary>
//        /// <param name="input"></param>
//        public ICUNormalizer2Filter(TokenStream input)
//            : this(input, new Normalizer2(Normalizer.UNormalizationMode.UNORM_NFKC) /*Normalizer2.getInstance(null, "nfkc_cf", Normalizer2.Mode.COMPOSE)*/)
//        {
//        }

//        /// <summary>
//        /// Create a new <see cref="Normalizer2Filter"/> with the specified <see cref="Normalizer2"/>
//        /// </summary>
//        /// <param name="input">stream</param>
//        /// <param name="normalizer">normalizer to use</param>
//        public ICUNormalizer2Filter(TokenStream input, Normalizer2 normalizer)
//            : base(input)
//        {
//            this.normalizer = normalizer;
//            this.termAtt = AddAttribute<ICharTermAttribute>();
//        }

//        public override sealed bool IncrementToken()
//        {
//            if (m_input.IncrementToken())
//            {
//                var term = termAtt.ToString();
//                try
//                {
//                    if (!normalizer.IsNormalized(term))
//                    {
//                        termAtt.SetEmpty().Append(normalizer.Normalize(term));
//                    }
//                }
//                catch (System.Exception ex)
//                {

//                }
//                return true;
//            }
//            else
//            {
//                return false;
//            }
//        }
//    }
//}
