using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using NUnit.Framework;
using Version=Lucene.Net.Util.Version;

namespace Lucene.Net.Analyzers.El
{
/**
 * A unit test class for verifying the correct operation of the GreekAnalyzer.
 *
 */
    [TestFixture]
public class GreekAnalyzerTest : BaseTokenStreamTestCase {

	/**
	 * Test the analysis of various greek strings.
	 *
	 * @throws Exception in case an error occurs
	 */
    [Test]
	public void testAnalyzer(){
		Analyzer a = new GreekAnalyzer(Version.LUCENE_CURRENT);
		// Verify the correct analysis of capitals and small accented letters
        AssertAnalyzesTo(a,
                         "\u039c\u03af\u03b1 \u03b5\u03be\u03b1\u03b9\u03c1\u03b5\u03c4\u03b9\u03ba\u03ac \u03ba\u03b1\u03bb\u03ae \u03ba\u03b1\u03b9 \u03c0\u03bb\u03bf\u03cd\u03c3\u03b9\u03b1 \u03c3\u03b5\u03b9\u03c1\u03ac \u03c7\u03b1\u03c1\u03b1\u03ba\u03c4\u03ae\u03c1\u03c9\u03bd \u03c4\u03b7\u03c2 \u0395\u03bb\u03bb\u03b7\u03bd\u03b9\u03ba\u03ae\u03c2 \u03b3\u03bb\u03ce\u03c3\u03c3\u03b1\u03c2",
                         new String[]
                             {
                                 "\u03bc\u03b9\u03b1", "\u03b5\u03be\u03b1\u03b9\u03c1\u03b5\u03c4\u03b9\u03ba\u03b1",
                                 "\u03ba\u03b1\u03bb\u03b7", "\u03c0\u03bb\u03bf\u03c5\u03c3\u03b9\u03b1",
                                 "\u03c3\u03b5\u03b9\u03c1\u03b1",
                                 "\u03c7\u03b1\u03c1\u03b1\u03ba\u03c4\u03b7\u03c1\u03c9\u03bd",
                                 "\u03b5\u03bb\u03bb\u03b7\u03bd\u03b9\u03ba\u03b7\u03c3",
                                 "\u03b3\u03bb\u03c9\u03c3\u03c3\u03b1\u03c3"
                             });
		// Verify the correct analysis of small letters with diaeresis and the elimination
		// of punctuation marks
        AssertAnalyzesTo(a,
                         "\u03a0\u03c1\u03bf\u03ca\u03cc\u03bd\u03c4\u03b1 (\u03ba\u03b1\u03b9)     [\u03c0\u03bf\u03bb\u03bb\u03b1\u03c0\u03bb\u03ad\u03c2]	-	\u0391\u039d\u0391\u0393\u039a\u0395\u03a3",
                         new String[]
                             {
                                 "\u03c0\u03c1\u03bf\u03b9\u03bf\u03bd\u03c4\u03b1",
                                 "\u03c0\u03bf\u03bb\u03bb\u03b1\u03c0\u03bb\u03b5\u03c3",
                                 "\u03b1\u03bd\u03b1\u03b3\u03ba\u03b5\u03c3"
                             });
		// Verify the correct analysis of capital accented letters and capitalletters with diaeresis,
		// as well as the elimination of stop words
        AssertAnalyzesTo(a,
                         "\u03a0\u03a1\u039f\u03ab\u03a0\u039f\u0398\u0395\u03a3\u0395\u0399\u03a3  \u0386\u03c8\u03bf\u03b3\u03bf\u03c2, \u03bf \u03bc\u03b5\u03c3\u03c4\u03cc\u03c2 \u03ba\u03b1\u03b9 \u03bf\u03b9 \u03ac\u03bb\u03bb\u03bf\u03b9",
                         new String[]
                             {
                                 "\u03c0\u03c1\u03bf\u03c5\u03c0\u03bf\u03b8\u03b5\u03c3\u03b5\u03b9\u03c3",
                                 "\u03b1\u03c8\u03bf\u03b3\u03bf\u03c3", "\u03bc\u03b5\u03c3\u03c4\u03bf\u03c3",
                                 "\u03b1\u03bb\u03bb\u03bf\u03b9"
                             });
	}

    [Test]
	public void testReusableTokenStream(){
	    Analyzer a = new GreekAnalyzer(Version.LUCENE_CURRENT);
	    // Verify the correct analysis of capitals and small accented letters
        AssertAnalyzesToReuse(a,
                              "\u039c\u03af\u03b1 \u03b5\u03be\u03b1\u03b9\u03c1\u03b5\u03c4\u03b9\u03ba\u03ac \u03ba\u03b1\u03bb\u03ae \u03ba\u03b1\u03b9 \u03c0\u03bb\u03bf\u03cd\u03c3\u03b9\u03b1 \u03c3\u03b5\u03b9\u03c1\u03ac \u03c7\u03b1\u03c1\u03b1\u03ba\u03c4\u03ae\u03c1\u03c9\u03bd \u03c4\u03b7\u03c2 \u0395\u03bb\u03bb\u03b7\u03bd\u03b9\u03ba\u03ae\u03c2 \u03b3\u03bb\u03ce\u03c3\u03c3\u03b1\u03c2",
                              new String[]
                                  {
                                      "\u03bc\u03b9\u03b1",
                                      "\u03b5\u03be\u03b1\u03b9\u03c1\u03b5\u03c4\u03b9\u03ba\u03b1",
                                      "\u03ba\u03b1\u03bb\u03b7", "\u03c0\u03bb\u03bf\u03c5\u03c3\u03b9\u03b1",
                                      "\u03c3\u03b5\u03b9\u03c1\u03b1",
                                      "\u03c7\u03b1\u03c1\u03b1\u03ba\u03c4\u03b7\u03c1\u03c9\u03bd",
                                      "\u03b5\u03bb\u03bb\u03b7\u03bd\u03b9\u03ba\u03b7\u03c3",
                                      "\u03b3\u03bb\u03c9\u03c3\u03c3\u03b1\u03c3"
                                  });
	    // Verify the correct analysis of small letters with diaeresis and the elimination
	    // of punctuation marks
        AssertAnalyzesToReuse(a,
                              "\u03a0\u03c1\u03bf\u03ca\u03cc\u03bd\u03c4\u03b1 (\u03ba\u03b1\u03b9)     [\u03c0\u03bf\u03bb\u03bb\u03b1\u03c0\u03bb\u03ad\u03c2] -   \u0391\u039d\u0391\u0393\u039a\u0395\u03a3",
                              new String[]
                                  {
                                      "\u03c0\u03c1\u03bf\u03b9\u03bf\u03bd\u03c4\u03b1",
                                      "\u03c0\u03bf\u03bb\u03bb\u03b1\u03c0\u03bb\u03b5\u03c3",
                                      "\u03b1\u03bd\u03b1\u03b3\u03ba\u03b5\u03c3"
                                  });
	    // Verify the correct analysis of capital accented letters and capitalletters with diaeresis,
	    // as well as the elimination of stop words
        AssertAnalyzesToReuse(a,
                              "\u03a0\u03a1\u039f\u03ab\u03a0\u039f\u0398\u0395\u03a3\u0395\u0399\u03a3  \u0386\u03c8\u03bf\u03b3\u03bf\u03c2, \u03bf \u03bc\u03b5\u03c3\u03c4\u03cc\u03c2 \u03ba\u03b1\u03b9 \u03bf\u03b9 \u03ac\u03bb\u03bb\u03bf\u03b9",
                              new String[]
                                  {
                                      "\u03c0\u03c1\u03bf\u03c5\u03c0\u03bf\u03b8\u03b5\u03c3\u03b5\u03b9\u03c3",
                                      "\u03b1\u03c8\u03bf\u03b3\u03bf\u03c3", "\u03bc\u03b5\u03c3\u03c4\u03bf\u03c3",
                                      "\u03b1\u03bb\u03bb\u03bf\u03b9"
                                  });
	}
}

}
