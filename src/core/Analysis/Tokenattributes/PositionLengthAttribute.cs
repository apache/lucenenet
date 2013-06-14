using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Attribute = Lucene.Net.Util.Attribute;

namespace Lucene.Net.Analysis.Tokenattributes
{
    [Serializable]
    public class PositionLengthAttribute : Attribute, IPositionLengthAttribute
    {
        private int positionLength = 1;

        public PositionLengthAttribute()
        {
        }

        public override void Clear()
        {
            positionLength = 1;
        }

        public override void CopyTo(Attribute target)
        {
            var t = (IPositionLengthAttribute) target;
            t.PositionLength = positionLength;
        }

        public int PositionLength
        {
            set
            {
                if (positionLength < 1)
                {
                    throw new ArgumentException
                        ("Position length must be 1 or greater: got " + positionLength);
                }
                this.positionLength = value;
            }
            get { return positionLength; }
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (obj is PositionLengthAttribute)
            {
                PositionLengthAttribute _other = (PositionLengthAttribute) obj;
                return positionLength == _other.positionLength;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return positionLength;
        }
    }
}