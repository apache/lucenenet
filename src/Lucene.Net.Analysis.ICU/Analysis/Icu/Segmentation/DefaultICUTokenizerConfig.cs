// LUCENENET TODO: Port issues - missing dependencies

//using Icu;
//using Lucene.Net.Analysis.Standard;
//using Lucene.Net.Support;
//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Lucene.Net.Analysis.ICU.Segmentation
//{
//    /// <summary>
//    /// Default <see cref="ICUTokenizerConfig"/> that is generally applicable
//    /// to many languages.
//    /// </summary>
//    /// <remarks>
//    /// Generally tokenizes Unicode text according to UAX#29 
//    /// ({@link BreakIterator#getWordInstance(ULocale) BreakIterator.getWordInstance(ULocale.ROOT)}), 
//    /// but with the following tailorings:
//    /// <list type="bullet">
//    ///     <item><description>Thai, Lao, and CJK text is broken into words with a dictionary.</description></item>
//    ///     <item><description>Myanmar, and Khmer text is broken into syllables based on custom BreakIterator rules.</description></item>
//    /// </list>
//    /// <para/>
//    /// @lucene.experimental
//    /// </remarks>
//    public class DefaultICUTokenizerConfig : ICUTokenizerConfig
//    {
//        /** Token type for words containing ideographic characters */
//        public static readonly string WORD_IDEO = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.IDEOGRAPHIC];
//        /** Token type for words containing Japanese hiragana */
//        public static readonly string WORD_HIRAGANA = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.HIRAGANA];
//        /** Token type for words containing Japanese katakana */
//        public static readonly string WORD_KATAKANA = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.KATAKANA];
//        /** Token type for words containing Korean hangul  */
//        public static readonly string WORD_HANGUL = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.HANGUL];
//        /** Token type for words that contain letters */
//        public static readonly string WORD_LETTER = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.ALPHANUM];
//        /** Token type for words that appear to be numbers */
//        public static readonly string WORD_NUMBER = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.NUM];

//        /*
//         * the default breakiterators in use. these can be expensive to
//         * instantiate, cheap to clone.
//         */
//        // we keep the cjk breaking separate, thats because it cannot be customized (because dictionary
//        // is only triggered when kind = WORD, but kind = LINE by default and we have no non-evil way to change it)
//        private static readonly Icu.BreakIterator cjkBreakIterator = new Icu.RuleBasedBreakIterator(Icu.BreakIterator.UBreakIteratorType.WORD, new Locale()); //BreakIterator.getWordInstance(ULocale.ROOT);
//                                                                                                                                                                                // the same as ROOT, except no dictionary segmentation for cjk
//        private static readonly Icu.BreakIterator defaultBreakIterator =
//            ReadBreakIterator("Default.brk");
//        private static readonly Icu.BreakIterator khmerBreakIterator =
//            ReadBreakIterator("Khmer.brk");
//        private static readonly Icu.BreakIterator myanmarBreakIterator =
//            ReadBreakIterator("Myanmar.brk");

//        // TODO: deprecate this boolean? you only care if you are doing super-expert stuff...
//        private readonly bool cjkAsWords;

//        /** 
//         * Creates a new config. This object is lightweight, but the first
//         * time the class is referenced, breakiterators will be initialized.
//         * @param cjkAsWords true if cjk text should undergo dictionary-based segmentation, 
//         *                   otherwise text will be segmented according to UAX#29 defaults.
//         *                   If this is true, all Han+Hiragana+Katakana words will be tagged as
//         *                   IDEOGRAPHIC.
//         */
//        public DefaultICUTokenizerConfig(bool cjkAsWords)
//        {
//            this.cjkAsWords = cjkAsWords;
//        }

//        public override bool CombineCJ
//        {
//            get { return cjkAsWords; }
//        }

//        public override Icu.BreakIterator GetBreakIterator(int script)
//        {
//            switch (script)
//            {
//                case UScript.KHMER: return (Icu.BreakIterator)khmerBreakIterator.Clone();
//                case UScript.MYANMAR: return (Icu.BreakIterator)myanmarBreakIterator.Clone();
//                case UScript.JAPANESE: return (Icu.BreakIterator)cjkBreakIterator.Clone();
//                default: return (Icu.BreakIterator)defaultBreakIterator.Clone();
//            }
//        }

//        public override string GetType(int script, int ruleStatus)
//        {
//            switch (ruleStatus)
//            {
//                case RuleBasedBreakIterator.WORD_IDEO:
//                    return WORD_IDEO;
//                case RuleBasedBreakIterator.WORD_KANA:
//                    return script == UScript.HIRAGANA ? WORD_HIRAGANA : WORD_KATAKANA;
//                case RuleBasedBreakIterator.WORD_LETTER:
//                    return script == UScript.HANGUL ? WORD_HANGUL : WORD_LETTER;
//                case RuleBasedBreakIterator.WORD_NUMBER:
//                    return WORD_NUMBER;
//                default: /* some other custom code */
//                    return "<OTHER>";
//            }
//        }

//        private static RuleBasedBreakIterator ReadBreakIterator(string filename)
//        {
//            Stream @is =
//              typeof(DefaultICUTokenizerConfig).Assembly.FindAndGetManifestResourceStream(typeof(DefaultICUTokenizerConfig), filename);
//            try
//            {
//                RuleBasedBreakIterator bi =
//                    RuleBasedBreakIterator.GetInstanceFromCompiledRules(@is);
//                @is.Dispose();
//                return bi;
//            }
//            catch (IOException e)
//            {
//                throw new Exception(e.ToString(), e);
//            }
//        }
//    }
//}
