using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lucene.Net.Facet.Taxonomy
{
    public class CategoryPath : IComparable<CategoryPath>
    {
        public static readonly CategoryPath EMPTY = new CategoryPath();
        public readonly String[] components;
        public readonly int length;

        private CategoryPath()
        {
            components = null;
            length = 0;
        }

        private CategoryPath(CategoryPath copyFrom, int prefixLen)
        {
            this.components = copyFrom.components;
            length = prefixLen;
        }

        public CategoryPath(params string[] components)
        {
            foreach (string comp in components)
            {
                if (string.IsNullOrEmpty(comp))
                {
                    throw new ArgumentException(@"empty or null components not allowed: " + Arrays.ToString(components));
                }
            }

            this.components = components;
            length = components.Length;
        }

        public CategoryPath(string pathString, char delimiter)
        {
            String[] comps = pathString.Split(new[] { Regex.Escape(delimiter.ToString()) }, StringSplitOptions.None);
            if (comps.Length == 1 && string.IsNullOrEmpty(comps[0]))
            {
                components = null;
                length = 0;
            }
            else
            {
                foreach (string comp in comps)
                {
                    if (string.IsNullOrEmpty(comp))
                    {
                        throw new ArgumentException(@"empty or null components not allowed: " + Arrays.ToString(comps));
                    }
                }

                components = comps;
                length = components.Length;
            }
        }

        public virtual int FullPathLength()
        {
            if (length == 0)
                return 0;
            int charsNeeded = 0;
            for (int i = 0; i < length; i++)
            {
                charsNeeded += components[i].Length;
            }

            charsNeeded += length - 1;
            return charsNeeded;
        }

        public int CompareTo(CategoryPath other)
        {
            int len = length < other.length ? length : other.length;
            for (int i = 0, j = 0; i < len; i++, j++)
            {
                int cmp = components[i].CompareTo(other.components[j]);
                if (cmp < 0)
                    return -1;
                if (cmp > 0)
                    return 1;
            }

            return length - other.length;
        }

        private void HasDelimiter(string offender, char delimiter)
        {
            throw new ArgumentException(@"delimiter character '" + delimiter + @"' (U+" + Convert.ToString((int)delimiter, 16) + @") appears in path component \" + offender + @"\");
        }

        private void NoDelimiter(char[] buf, int offset, int len, char delimiter)
        {
            for (int idx = 0; idx < len; idx++)
            {
                if (buf[offset + idx] == delimiter)
                {
                    HasDelimiter(new string(buf, offset, len), delimiter);
                }
            }
        }

        public virtual int CopyFullPath(char[] buf, int start, char delimiter)
        {
            if (length == 0)
            {
                return 0;
            }

            int idx = start;
            int upto = length - 1;
            for (int i = 0; i < upto; i++)
            {
                int len = components[i].Length;
                components[i].CopyTo(0, buf, idx, len); //.GetChars(0, len, buf, idx);
                NoDelimiter(buf, idx, len, delimiter);
                idx += len;
                buf[idx++] = delimiter;
            }

            components[upto].CopyTo(0, buf, idx, components[upto].Length); //.GetChars(0, components[upto].Length(), buf, idx);
            NoDelimiter(buf, idx, components[upto].Length, delimiter);
            return idx + components[upto].Length - start;
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is CategoryPath))
            {
                return false;
            }

            CategoryPath other = (CategoryPath)obj;
            if (length != other.length)
            {
                return false;
            }

            for (int i = length - 1; i >= 0; i--)
            {
                if (!components[i].Equals(other.components[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            if (length == 0)
            {
                return 0;
            }

            int hash = length;
            for (int i = 0; i < length; i++)
            {
                hash = hash * 31 + components[i].GetHashCode();
            }

            return hash;
        }

        public virtual long LongHashCode()
        {
            if (length == 0)
            {
                return 0;
            }

            long hash = length;
            for (int i = 0; i < length; i++)
            {
                hash = hash * 65599 + components[i].GetHashCode();
            }

            return hash;
        }

        public virtual CategoryPath Subpath(int length)
        {
            if (length >= this.length || length < 0)
            {
                return this;
            }
            else if (length == 0)
            {
                return EMPTY;
            }
            else
            {
                return new CategoryPath(this, length);
            }
        }

        public override string ToString()
        {
            return ToString('/');
        }

        public virtual string ToString(char delimiter)
        {
            if (length == 0)
                return @"";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                if (components[i].IndexOf(delimiter) != -1)
                {
                    HasDelimiter(components[i], delimiter);
                }

                sb.Append(components[i]).Append(delimiter);
            }

            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }
    }
}
