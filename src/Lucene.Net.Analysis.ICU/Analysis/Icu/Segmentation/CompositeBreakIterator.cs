// LUCENENET TODO: Port issues - missing dependencies

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Lucene.Net.Analysis.ICU.Segmentation
//{
//    /// <summary>
//    /// An internal BreakIterator for multilingual text, following recommendations
//    /// from: UAX #29: Unicode Text Segmentation. (http://unicode.org/reports/tr29/)
//    /// <para/>
//    /// See http://unicode.org/reports/tr29/#Tailoring for the motivation of this
//    /// design.
//    /// <para/>
//    /// Text is first divided into script boundaries. The processing is then
//    /// delegated to the appropriate break iterator for that specific script.
//    /// <para/>
//    /// This break iterator also allows you to retrieve the ISO 15924 script code
//    /// associated with a piece of text.
//    /// <para/>
//    /// See also UAX #29, UTR #24
//    /// <para/>
//    /// @lucene.experimental
//    /// </summary>
//    internal sealed class CompositeBreakIterator
//    {
//        private readonly ICUTokenizerConfig config;
//        private readonly BreakIteratorWrapper[] wordBreakers = new BreakIteratorWrapper[UScript.CODE_LIMIT];

//        private BreakIteratorWrapper rbbi;
//        private readonly ScriptIterator scriptIterator;

//        private char[] text;

//        public CompositeBreakIterator(ICUTokenizerConfig config)
//        {
//            this.config = config;
//            this.scriptIterator = new ScriptIterator(config.CombineCJ);
//        }

//        /**
//         * Retrieve the next break position. If the RBBI range is exhausted within the
//         * script boundary, examine the next script boundary.
//         * 
//         * @return the next break position or BreakIterator.DONE
//         */
//        public int Next()
//        {
//            int next = rbbi.Next();
//            while (next == Support.BreakIterator.DONE && scriptIterator.Next())
//            {
//                rbbi = GetBreakIterator(scriptIterator.GetScriptCode());
//                rbbi.SetText(text, scriptIterator.GetScriptStart(),
//                    scriptIterator.GetScriptLimit() - scriptIterator.GetScriptStart());
//                next = rbbi.Next();
//            }
//            return (next == Support.BreakIterator.DONE) ? Support.BreakIterator.DONE : next
//                + scriptIterator.GetScriptStart();
//        }

//        /**
//         * Retrieve the current break position.
//         * 
//         * @return the current break position or BreakIterator.DONE
//         */
//        public int Current
//        {
//            get
//            {
//                int current = rbbi.Current;
//                return (current == Support.BreakIterator.DONE) ? Support.BreakIterator.DONE : current
//                    + scriptIterator.GetScriptStart();
//            }
//        }

//        /**
//         * Retrieve the rule status code (token type) from the underlying break
//         * iterator
//         * 
//         * @return rule status code (see RuleBasedBreakIterator constants)
//         */
//        public int GetRuleStatus()
//        {
//            return rbbi.GetRuleStatus();
//        }

//        /**
//         * Retrieve the UScript script code for the current token. This code can be
//         * decoded with UScript into a name or ISO 15924 code.
//         * 
//         * @return UScript script code for the current token.
//         */
//        public int GetScriptCode()
//        {
//            return scriptIterator.GetScriptCode();
//        }

//        /**
//         * Set a new region of text to be examined by this iterator
//         * 
//         * @param text buffer of text
//         * @param start offset into buffer
//         * @param length maximum length to examine
//         */
//        public void SetText(char[] text, int start, int length)
//        {
//            this.text = text;
//            scriptIterator.SetText(text, start, length);
//            if (scriptIterator.Next())
//            {
//                rbbi = GetBreakIterator(scriptIterator.GetScriptCode());
//                rbbi.SetText(text, scriptIterator.GetScriptStart(),
//                    scriptIterator.GetScriptLimit() - scriptIterator.GetScriptStart());
//            }
//            else
//            {
//                rbbi = GetBreakIterator(UScript.COMMON);
//                rbbi.SetText(text, 0, 0);
//            }
//        }

//        private BreakIteratorWrapper GetBreakIterator(int scriptCode)
//        {
//            if (wordBreakers[scriptCode] == null)
//                wordBreakers[scriptCode] = BreakIteratorWrapper.Wrap(config.GetBreakIterator(scriptCode));
//            return wordBreakers[scriptCode];
//        }
//    }
//}
