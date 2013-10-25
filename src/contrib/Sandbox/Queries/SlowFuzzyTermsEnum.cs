using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Sandbox.Queries
{
    public sealed class SlowFuzzyTermsEnum : FuzzyTermsEnum
    {
        public SlowFuzzyTermsEnum(Terms terms, AttributeSource atts, Term term, float minSimilarity, int prefixLength)
            : base(terms, atts, term, minSimilarity, prefixLength, false)
        {
        }

        protected override void MaxEditDistanceChanged(BytesRef lastTerm, int maxEdits, bool init)
        {
            TermsEnum newEnum = GetAutomatonEnum(maxEdits, lastTerm);
            if (newEnum != null)
            {
                SetEnum(newEnum);
            }
            else if (init)
            {
                SetEnum(new LinearFuzzyTermsEnum(this));
            }
        }

        private class LinearFuzzyTermsEnum : FilteredTermsEnum
        {
            private int[] d;
            private int[] p;
            private readonly int[] text;
            private readonly IBoostAttribute boostAtt; // = Attributes.AddAttribute<IBoostAttribute>();
            
            public LinearFuzzyTermsEnum(SlowFuzzyTermsEnum parent)
                : base(parent.terms.Iterator(null))
            {
                this.parent = parent; 

                boostAtt = Attributes.AddAttribute<IBoostAttribute>();

                this.text = new int[parent.termLength - parent.realPrefixLength];
                Array.Copy(parent.termText, parent.realPrefixLength, text, 0, text.Length);
                string prefix = UnicodeUtil.NewString(parent.termText, 0, parent.realPrefixLength);
                prefixBytesRef = new BytesRef(prefix);
                this.d = new int[this.text.Length + 1];
                this.p = new int[this.text.Length + 1];
                InitialSeekTerm = prefixBytesRef;
            }

            private readonly BytesRef prefixBytesRef;
            private readonly IntsRef utf32 = new IntsRef(20);

            private readonly SlowFuzzyTermsEnum parent;

            protected override AcceptStatus Accept(BytesRef term)
            {
                if (StringHelper.StartsWith(term, prefixBytesRef))
                {
                    UnicodeUtil.UTF8toUTF32(term, utf32);
                    float similarity = Similarity(utf32.ints, parent.realPrefixLength, utf32.length - parent.realPrefixLength);
                    if (similarity > parent.minSimilarity)
                    {
                        boostAtt.Boost = (similarity - parent.minSimilarity) * parent.scale_factor;
                        return AcceptStatus.YES;
                    }
                    else
                        return AcceptStatus.NO;
                }
                else
                {
                    return AcceptStatus.END;
                }
            }

            private float Similarity(int[] target, int offset, int length)
            {
                int m = length;
                int n = text.Length;
                if (n == 0)
                {
                    return parent.realPrefixLength == 0 ? 0.0f : 1.0f - ((float)m / parent.realPrefixLength);
                }

                if (m == 0)
                {
                    return parent.realPrefixLength == 0 ? 0.0f : 1.0f - ((float)n / parent.realPrefixLength);
                }

                int maxDistance = CalculateMaxDistance(m);
                if (maxDistance < Math.Abs(m - n))
                {
                    return float.NegativeInfinity;
                }

                for (int i = 0; i <= n; ++i)
                {
                    p[i] = i;
                }

                for (int j = 1; j <= m; ++j)
                {
                    int bestPossibleEditDistance = m;
                    int t_j = target[offset + j - 1];
                    d[0] = j;
                    for (int i = 1; i <= n; ++i)
                    {
                        if (t_j != text[i - 1])
                        {
                            d[i] = Math.Min(Math.Min(d[i - 1], p[i]), p[i - 1]) + 1;
                        }
                        else
                        {
                            d[i] = Math.Min(Math.Min(d[i - 1] + 1, p[i] + 1), p[i - 1]);
                        }

                        bestPossibleEditDistance = Math.Min(bestPossibleEditDistance, d[i]);
                    }

                    if (j > maxDistance && bestPossibleEditDistance > maxDistance)
                    {
                        return float.NegativeInfinity;
                    }

                    int[] _d = p;
                    p = d;
                    d = _d;
                }

                return 1.0f - ((float)p[n] / (float)(parent.realPrefixLength + Math.Min(n, m)));
            }

            private int CalculateMaxDistance(int m)
            {
                return parent.raw ? parent.maxEdits : Math.Min(parent.maxEdits, (int)((1 - parent.minSimilarity) * (Math.Min(text.Length, m) + parent.realPrefixLength)));
            }
        }
    }
}
