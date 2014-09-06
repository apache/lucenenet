using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.BlockTerms
{
    /// <summary>
    /// Used as key for the terms cache
    /// </summary>
    internal class BlockTermsFieldAndTerm : DoubleBarrelLRUCache.CloneableKey
    {

        public String Field { get; set; }
        public BytesRef Term { get; set; }

        public FieldAndTerm()
        {
        }

        public FieldAndTerm(FieldAndTerm other)
        {
            Field = other.Field;
            Term = BytesRef.DeepCopyOf(other.Term);
        }

        public override bool Equals(Object _other)
        {
            FieldAndTerm other = (FieldAndTerm) _other;
            return other.Field.equals(field) && Term.BytesEquals(other.Term);
        }

        public override FieldAndTerm Clone()
        {
            return new FieldAndTerm(this);
        }

        public override int GetHashCode()
        {
            return Field.GetHashCode()*31 + Term.GetHashCode();
        }
    }
}

