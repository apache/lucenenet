using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Taxonomy.WriterCache.Cl2o
{
    class CategoryPathUtils
    {
        public static void Serialize(CategoryPath cp, CharBlockArray charBlockArray)
        {
            charBlockArray.Append((char)cp.length);
            if (cp.length == 0)
            {
                return;
            }

            for (int i = 0; i < cp.length; i++)
            {
                charBlockArray.Append((char)cp.components[i].Length);
                charBlockArray.Append(cp.components[i]);
            }
        }

        public static int HashCodeOfSerialized(CharBlockArray charBlockArray, int offset)
        {
            int length = (short)charBlockArray.CharAt(offset++);
            if (length == 0)
            {
                return 0;
            }

            int hash = length;
            for (int i = 0; i < length; i++)
            {
                int len = (short)charBlockArray.CharAt(offset++);
                hash = hash * 31 + charBlockArray.SubSequence(offset, offset + len).GetHashCode();
                offset += len;
            }

            return hash;
        }

        public static bool EqualsToSerialized(CategoryPath cp, CharBlockArray charBlockArray, int offset)
        {
            int n = charBlockArray.CharAt(offset++);
            if (cp.length != n)
            {
                return false;
            }

            if (cp.length == 0)
            {
                return true;
            }

            for (int i = 0; i < cp.length; i++)
            {
                int len = (short)charBlockArray.CharAt(offset++);
                if (len != cp.components[i].Length)
                {
                    return false;
                }

                if (!cp.components[i].Equals(charBlockArray.SubSequence(offset, offset + len).ToString()))
                {
                    return false;
                }

                offset += len;
            }

            return true;
        }
    }
}
