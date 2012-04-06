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
            private class MemoryTermEnum : TermEnum
            {
                private readonly MemoryIndex _index;
                private readonly MemoryIndexReader _reader;
                private int _i; // index into info.sortedTerms
                private int _j; // index into sortedFields

                public MemoryTermEnum(MemoryIndex index, MemoryIndexReader reader, int ix, int jx)
                {
                    _index = index;
                    _reader = reader;
                    _i = ix; // index into info.sortedTerms
                    _j = jx; // index into sortedFields
                }

                public override bool Next()
                {
                    if (DEBUG) System.Diagnostics.Debug.WriteLine("TermEnum.next");
                    if (_j >= _index.sortedFields.Length) return false;
                    Info info = _reader.GetInfo(_j);
                    if (++_i < info.SortedTerms.Length) return true;

                    // move to successor
                    _j++;
                    _i = 0;
                    if (_j >= _index.sortedFields.Length) return false;
                    _reader.GetInfo(_j).SortTerms();
                    return true;
                }

                public override Term Term()
                {
                              if (DEBUG) System.Diagnostics.Debug.WriteLine("TermEnum.term: " + _i);
                              if (_j >= _index.sortedFields.Length) return null;
                              Info info = _reader.GetInfo(_j);
                              if (_i >= info.SortedTerms.Length) return null;
                    //          if (DEBUG) System.Diagnostics.Debug.WriteLine("TermEnum.term: " + i + ", " + info.sortedTerms[i].getKey());
                              return CreateTerm(info, _j, info.SortedTerms[_i].Key);
                }

                public override int DocFreq()
                {                
                              if (DEBUG) System.Diagnostics.Debug.WriteLine("TermEnum.docFreq");
                              if (_j >= _index.sortedFields.Length) return 0;
                              Info info = _reader.GetInfo(_j);
                              if (_i >= info.SortedTerms.Length) return 0;
                              return _index.NumPositions(info.GetPositions(_i));
                }

                protected override void Dispose(bool disposing)
                {
                              if (DEBUG) System.Diagnostics.Debug.WriteLine("TermEnum.close");
                }

                private Term CreateTerm(Info info, int pos, string text)
                {
                    // Assertion: sortFields has already been called before
                    Term template = info.template;
                    if (template == null) { // not yet cached?
                        String fieldName = _index.sortedFields[pos].Key;
                    template = new Term(fieldName);
                    info.template = template;
                    }

                    return template.CreateTerm(text);
                }
            }
        }
    }
}
