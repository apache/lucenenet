using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Support;
using Lucene.Net.Util.Automaton;

namespace Lucene.Net.Analysis
{
/**
 * A tokenfilter for testing that removes terms accepted by a DFA.
 * <ul>
 *  <li>Union a list of singletons to act like a stopfilter.
 *  <li>Use the complement to act like a keepwordfilter
 *  <li>Use a regex like <code>.{12,}</code> to act like a lengthfilter
 * </ul>
 */
    internal class MockTokenFilter : TokenFilter
    {
        /** Empty set of stopwords */

        public static CharacterRunAutomaton EMPTY_STOPSET =
            new CharacterRunAutomaton(BasicAutomata.MakeEmpty());

        /** Set of common english stopwords */

        public static CharacterRunAutomaton ENGLISH_STOPSET =
            new CharacterRunAutomaton(BasicOperations.Union(Arrays.asList<Automaton>(
                makeString("a"), makeString("an"), makeString("and"), makeString("are"),
                makeString("as"), makeString("at"), makeString("be"), makeString("but"),
                makeString("by"), makeString("for"), makeString("if"), makeString("in"),
                makeString("into"), makeString("is"), makeString("it"), makeString("no"),
                makeString("not"), makeString("of"), makeString("on"), makeString("or"),
                makeString("such"), makeString("that"), makeString("the"), makeString("their"),
                makeString("then"), makeString("there"), makeString("these"), makeString("they"),
                makeString("this"), makeString("to"), makeString("was"), makeString("will"),
                makeString("with"))));

        private static Automaton makeString(string an)
        {
            return BasicAutomata.MakeString(an);
        }

        private CharacterRunAutomaton filter;
        private bool enablePositionIncrements = true;

        private readonly CharTermAttribute termAtt;
        private readonly PositionIncrementAttribute posIncrAtt;
       
        public MockTokenFilter(TokenStream input, CharacterRunAutomaton filter):base(input)
        {
            this.filter = filter;
            termAtt = AddAttribute<CharTermAttribute>();
            posIncrAtt = AddAttribute<PositionIncrementAttribute>();
        }

        public override bool IncrementToken()
        {
            // TODO: fix me when posInc=false, to work like FilteringTokenFilter in that case and not return
            // initial token with posInc=0 ever

            // return the first non-stop word found
            int skippedPositions = 0;
            while (input.IncrementToken())
            {
                if (!filter.Run(termAtt.Buffer, 0, termAtt.Length))
                {
                    if (enablePositionIncrements)
                    {
                        posIncrAtt.PositionIncrement = posIncrAtt.PositionIncrement + skippedPositions;
                    }
                    return true;
                }
                skippedPositions += posIncrAtt.PositionIncrement;
            }
            // reached EOS -- return false
            return false;
        }

        /**
   * @see #setEnablePositionIncrements(boolean)
   */

        public bool getEnablePositionIncrements()
        {
            return enablePositionIncrements;
        }

        /**
   * If <code>true</code>, this Filter will preserve
   * positions of the incoming tokens (ie, accumulate and
   * set position increments of the removed stop tokens).
   */

        public void setEnablePositionIncrements(bool enable)
        {
            this.enablePositionIncrements = enable;
        }
    }
}
