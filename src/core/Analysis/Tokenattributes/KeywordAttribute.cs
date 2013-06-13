using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Attribute = Lucene.Net.Util.Attribute;

namespace Lucene.Net.Analysis.Tokenattributes
{
    public class KeywordAttribute : Attribute, IKeywordAttribute
    {
        public override void Clear()
        {
            IsKeyword = false;
        }

        public override void CopyTo(Attribute target)
        {
            var attr = (IKeywordAttribute) target;
            attr.IsKeyword = IsKeyword;
        }

        public bool IsKeyword { get; set; }

        public override bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (obj.GetType() != GetType())
                return false;
            var other = (KeywordAttribute) obj;
            return IsKeyword == other.IsKeyword;
        }

        public override int GetHashCode()
        {
            return IsKeyword ? 31 : 37;
        }
    }
}