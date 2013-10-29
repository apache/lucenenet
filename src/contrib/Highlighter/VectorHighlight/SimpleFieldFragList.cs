using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.VectorHighlight
{
    public class SimpleFieldFragList : FieldFragList
    {
        public SimpleFieldFragList(int fragCharSize)
            : base(fragCharSize)
        {
        }

        public override void Add(int startOffset, int endOffset, IList<FieldPhraseList.WeightedPhraseInfo> phraseInfoList)
        {
            float totalBoost = 0;
            var subInfos = new List<FieldFragList.WeightedFragInfo.SubInfo>();
            foreach (FieldPhraseList.WeightedPhraseInfo phraseInfo in phraseInfoList)
            {
                subInfos.Add(new FieldFragList.WeightedFragInfo.SubInfo(phraseInfo.Text, phraseInfo.TermsOffsets, phraseInfo.Seqnum));
                totalBoost += phraseInfo.Boost;
            }

            FragInfos.Add(new WeightedFragInfo(startOffset, endOffset, subInfos, totalBoost));
        }
    }
}
