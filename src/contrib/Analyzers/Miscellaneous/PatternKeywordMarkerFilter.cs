using System.Text.RegularExpressions;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Analysis.Miscellaneous
{
    public class PatternKeywordMarkerFilter : KeywordMarkerFilter 
    {
         private readonly ICharTermAttribute termAtt;
         private readonly Regex pattern;

        public PatternKeywordMarkerFilter(TokenStream input, Regex pattern) : base(input)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            this.pattern = pattern;
        }

        protected override bool IsKeyword()
        {
            return pattern.IsMatch(termAtt.ToString());
        }
    }
}
