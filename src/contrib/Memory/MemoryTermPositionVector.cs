using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Index.Memory
{
    public partial class MemoryIndex
    {
        private sealed partial class MemoryIndexReader
        {
            private class MemoryTermPositionVector : TermPositionVector
            {
                private readonly MemoryIndex _index;
                private readonly string _fieldName;
                private readonly KeyValuePair<String,ArrayIntList>[] sortedTerms;

                public MemoryTermPositionVector(MemoryIndex index, Info info, string fieldName)
                {
                    _index = index;
                    _fieldName = fieldName;
                    sortedTerms = info.SortedTerms;
                }

                public string Field
                {
                    get { return _fieldName; }
                }

                public int Size
                {
                    get { return sortedTerms.Length; }
                }

                public string[] GetTerms()
                {
                    var terms = new String[sortedTerms.Length];
                    for (int i = sortedTerms.Length; --i >= 0; )
                    {
                        terms[i] = sortedTerms[i].Key;
                    }
                    return terms;
                }

                public int[] GetTermFrequencies()
                {
                    int[] freqs = new int[sortedTerms.Length];
                    for (int i = sortedTerms.Length; --i >= 0; )
                    {
                        freqs[i] = _index.NumPositions(sortedTerms[i].Value);
                    }
                    return freqs;
                }

                public int IndexOf(string term)
                {
                    int i = Array.BinarySearch(sortedTerms, new KeyValuePair<string, ArrayIntList>(term, null), Info.ArrayIntListComparer);
                    return i >= 0 ? i : -1;
                }

                public int[] IndexesOf(string[] terms, int start, int len)
                {
                    int[] indexes = new int[len];
                    for (int i = 0; i < len; i++)
                    {
                        indexes[i] = IndexOf(terms[start++]);
                    }
                    return indexes;
                }

                public int[] GetTermPositions(int index)
                {
                    return sortedTerms[index].Value.ToArray(_index.stride);
                }

                public TermVectorOffsetInfo[] GetOffsets(int index)
                {
                    if (_index.stride == 1) return null; // no offsets stored

                    ArrayIntList positions = sortedTerms[index].Value;
                    int size = positions.Size();
                    TermVectorOffsetInfo[] offsets = new TermVectorOffsetInfo[size / _index.stride];

                    for (int i = 0, j = 1; j < size; i++, j += _index.stride)
                    {
                        int start = positions.Get(j);
                        int end = positions.Get(j + 1);
                        offsets[i] = new TermVectorOffsetInfo(start, end);
                    }
                    return offsets;
                }
            }
        }
    }
}
