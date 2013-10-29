using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.VectorHighlight
{
    public class SingleFragListBuilder : IFragListBuilder
    {
        public FieldFragList CreateFieldFragList(FieldPhraseList fieldPhraseList, int fragCharSize)
        {
            FieldFragList ffl = new SimpleFieldFragList(fragCharSize);
            List<FieldPhraseList.WeightedPhraseInfo> wpil = new List<FieldPhraseList.WeightedPhraseInfo>();
            IEnumerator<FieldPhraseList.WeightedPhraseInfo> ite = fieldPhraseList.phraseList.GetEnumerator();
            FieldPhraseList.WeightedPhraseInfo phraseInfo = null;
            while (true)
            {
                if (!ite.MoveNext())
                    break;
                phraseInfo = ite.Current;
                if (phraseInfo == null)
                    break;
                wpil.Add(phraseInfo);
            }

            if (wpil.Count > 0)
                ffl.Add(0, int.MaxValue, wpil);
            return ffl;
        }
    }
}
