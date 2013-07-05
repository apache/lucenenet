using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Util
{
    public class CharArraySet : ISet<object>
    {
        public static readonly CharArraySet EMPTY_SET = new CharArraySet(CharArrayMap.<Object>emptyMap());
  private static readonly object PLACEHOLDER = new object();
  
  private readonly CharArrayMap<Object> map;
    }
}
