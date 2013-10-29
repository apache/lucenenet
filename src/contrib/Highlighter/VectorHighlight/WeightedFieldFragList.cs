using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.VectorHighlight
{
    public class WeightedFieldFragList : FieldFragList
    {
        public WeightedFieldFragList(int fragCharSize)
            : base(fragCharSize)
        {
        }

        public override void Add(int startOffset, int endOffset, IList<FieldPhraseList.WeightedPhraseInfo> phraseInfoList)
        {
            float totalBoost = 0;
            List<FieldFragList.WeightedFragInfo.SubInfo> subInfos = new List<FieldFragList.WeightedFragInfo.SubInfo>();
            HashSet<String> distinctTerms = new HashSet<String>();
            int length = 0;
            foreach (FieldPhraseList.WeightedPhraseInfo phraseInfo in phraseInfoList)
            {
                subInfos.Add(new FieldFragList.WeightedFragInfo.SubInfo(phraseInfo.Text, phraseInfo.TermsOffsets, phraseInfo.Seqnum));
                foreach (FieldTermStack.TermInfo ti in phraseInfo.TermsInfos)
                {
                    if (distinctTerms.Add(ti.Text))
                        totalBoost += ti.Weight * phraseInfo.Boost;
                    length++;
                }
            }

            totalBoost *= length * (1 / (float)Math.Sqrt(length));
            FragInfos.Add(new WeightedFragInfo(startOffset, endOffset, subInfos, totalBoost));
        }
    }
}
