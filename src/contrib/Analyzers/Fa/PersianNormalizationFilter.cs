using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Analyzers.Fa
{
    /**
 * A {@link TokenFilter} that applies {@link PersianNormalizer} to normalize the
 * orthography.
 * 
 */

public sealed class PersianNormalizationFilter : TokenFilter {

  private readonly PersianNormalizer normalizer;
  private readonly TermAttribute termAtt;

  public PersianNormalizationFilter(TokenStream input) 
      :base(input)
  {
    normalizer = new PersianNormalizer();
    termAtt = AddAttribute<TermAttribute>();
  }

  public override bool IncrementToken()
{
    if (input.IncrementToken()) {
      int newlen = normalizer.Normalize(termAtt.TermBuffer(), termAtt.TermLength());
      termAtt.SetTermLength(newlen);
      return true;
    } 
    return false;
  }
}
}
