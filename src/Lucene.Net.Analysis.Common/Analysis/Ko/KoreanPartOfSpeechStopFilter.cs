using Lucene.Net.Analysis.Ko.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Ko
{
    public sealed class KoreanPartOfSpeechStopFilter : FilteringTokenFilter
    {
        private readonly ISet<POS.Tag> stopTags;
        private readonly IPartOfSpeechAttribute posAtt;

        public static readonly HashSet<POS.Tag> DEFAULT_STOP_TAGS = new (){
            POS.Tags["E"],
            POS.Tags["IC"],
            POS.Tags["J"],
            POS.Tags["MAG"],
            POS.Tags["MAJ"],
            POS.Tags["MM"],
            POS.Tags["SP"],
            POS.Tags["SSC"],
            POS.Tags["SSO"],
            POS.Tags["SC"],
            POS.Tags["SE"],
            POS.Tags["XPN"],
            POS.Tags["XSA"],
            POS.Tags["XSN"],
            POS.Tags["XSV"],
            POS.Tags["UNA"],
            POS.Tags["NA"],
            POS.Tags["VSV"]
        };

        /// <summary>
        /// Create a new <see cref="KoreanPartOfSpeechFilter"/>.
        /// </summary>
        /// <param name="version">The Lucene match version.</param>
        /// <param name="input">The <see cref="TokenStream"/> to consume.</param>
        /// <param name="stopTags">The part-of-speech tags that should be removed.</param>
        public KoreanPartOfSpeechStopFilter(LuceneVersion version, TokenStream input, HashSet<POS.Tag> stopTags)
            : base(version, input)
        {
            this.stopTags = stopTags;
            this.posAtt = AddAttribute<IPartOfSpeechAttribute>();
        }

        protected override bool Accept()
        {
            POS.Tag leftPOS = posAtt.GetLeftPOS();
            return leftPOS is null || !stopTags.Contains(leftPOS);
        }
    }
}