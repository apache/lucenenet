// Lucene version compatibility level 4.8.1
/*

Copyright (c) 2001, Dr Martin Porter
Copyright (c) 2002, Richard Boulton
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
    * this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
    * notice, this list of conditions and the following disclaimer in the
    * documentation and/or other materials provided with the distribution.
    * Neither the name of the copyright holders nor the names of its contributors
    * may be used to endorse or promote products derived from this software
    * without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

 */

using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Lucene.Net.Tartarus.Snowball
{
    /// <summary>
    /// This is the rev 502 of the Snowball SVN trunk,
    /// but modified:
    /// made abstract and introduced abstract method stem to avoid expensive reflection in filter class.
    /// refactored StringBuffers to StringBuilder
    /// uses char[] as buffer instead of StringBuffer/StringBuilder
    /// eq_s,eq_s_b,insert,replace_s take CharSequence like eq_v and eq_v_b
    /// reflection calls (Lovins, etc) use EMPTY_ARGS/EMPTY_PARAMS
    /// </summary>
    public abstract class SnowballProgram
    {
        // LUCENENET specific: Factored out EMPTY_ARGS by using Func<bool> instead of Reflection

        protected SnowballProgram()
        {
            m_current = new char[8];
            // LUCENENET specific - calling private method instead of public virtual
            SetCurrentInternal("");
        }

        public abstract bool Stem();

        /// <summary>
        /// Set the current string.
        /// </summary>
        public virtual void SetCurrent(string value)
            => SetCurrentInternal(value);

        /// <summary>
        /// Set the current string.
        /// </summary>
        /// <param name="text">character array containing input</param>
        /// <param name="length">valid length of text.</param>
        public virtual void SetCurrent(char[] text, int length) =>
            SetCurrentInternal(text, length);

        private void SetCurrentInternal(string value)
            => SetCurrentInternal(value.ToCharArray(), value.Length);

        // LUCENENET specific - S1699 - introduced this to allow the constructor to
        // still call "SetCurrent" functionality without having to call the virtual method
        // that could be overridden by a subclass and don't have the state it expects
        private void SetCurrentInternal(char[] value, int length)
        {
            m_current = value;
            m_cursor = 0;
            m_limit = length;
            m_limit_backward = 0;
            m_bra = m_cursor;
            m_ket = m_limit;
        }

        /// <summary>
        /// Get the current string.
        /// </summary>
        public virtual string Current => new string(m_current, 0, m_limit);

        /// <summary>
        /// Get the current buffer containing the stem.
        /// <para/>
        /// NOTE: this may be a reference to a different character array than the
        /// one originally provided with setCurrent, in the exceptional case that 
        /// stemming produced a longer intermediate or result string.
        /// <para/>
        /// It is necessary to use <see cref="CurrentBufferLength"/> to determine
        /// the valid length of the returned buffer. For example, many words are
        /// stemmed simply by subtracting from the length to remove suffixes.
        /// </summary>
        /// <seealso cref="CurrentBufferLength"/>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public virtual char[] CurrentBuffer => m_current;

        /// <summary>
        /// Get the valid length of the character array in <seealso cref="CurrentBuffer"/>
        /// </summary>
        public virtual int CurrentBufferLength => m_limit;

        // current string
        protected char[] m_current;

        protected int m_cursor;
        protected int m_limit;
        protected int m_limit_backward;
        protected int m_bra;
        protected int m_ket;

        protected virtual void CopyFrom(SnowballProgram other)
        {
            m_current = other.m_current;
            m_cursor = other.m_cursor;
            m_limit = other.m_limit;
            m_limit_backward = other.m_limit_backward;
            m_bra = other.m_bra;
            m_ket = other.m_ket;
        }

        protected virtual bool InGrouping(char[] s, int min, int max)
        {
            if (m_cursor >= m_limit) return false;
            char ch = m_current[m_cursor];
            if (ch > max || ch < min) return false;
            ch -= (char)min;
            if ((s[ch >> 3] & (0X1 << (ch & 0X7))) == 0) return false;
            m_cursor++;
            return true;
        }

        protected virtual bool InGroupingB(char[] s, int min, int max)
        {
            if (m_cursor <= m_limit_backward) return false;
            char ch = m_current[m_cursor - 1];
            if (ch > max || ch < min) return false;
            ch -= (char)min;
            if ((s[ch >> 3] & (0X1 << (ch & 0X7))) == 0) return false;
            m_cursor--;
            return true;
        }

        protected virtual bool OutGrouping(char[] s, int min, int max)
        {
            if (m_cursor >= m_limit) return false;
            char ch = m_current[m_cursor];
            if (ch > max || ch < min)
            {
                m_cursor++;
                return true;
            }
            ch -= (char)min;
            if ((s[ch >> 3] & (0X1 << (ch & 0X7))) == 0)
            {
                m_cursor++;
                return true;
            }
            return false;
        }

        protected virtual bool OutGroupingB(char[] s, int min, int max)
        {
            if (m_cursor <= m_limit_backward) return false;
            char ch = m_current[m_cursor - 1];
            if (ch > max || ch < min)
            {
                m_cursor--;
                return true;
            }
            ch -= (char)min;
            if ((s[ch >> 3] & (0X1 << (ch & 0X7))) == 0)
            {
                m_cursor--;
                return true;
            }
            return false;
        }

        protected virtual bool InRange(int min, int max)
        {
            if (m_cursor >= m_limit) return false;
            char ch = m_current[m_cursor];
            if (ch > max || ch < min) return false;
            m_cursor++;
            return true;
        }

        protected virtual bool InRangeB(int min, int max)
        {
            if (m_cursor <= m_limit_backward) return false;
            char ch = m_current[m_cursor - 1];
            if (ch > max || ch < min) return false;
            m_cursor--;
            return true;
        }

        protected virtual bool OutRange(int min, int max)
        {
            if (m_cursor >= m_limit) return false;
            char ch = m_current[m_cursor];
            if (!(ch > max || ch < min)) return false;
            m_cursor++;
            return true;
        }

        protected virtual bool OutRangeB(int min, int max)
        {
            if (m_cursor <= m_limit_backward) return false;
            char ch = m_current[m_cursor - 1];
            if (!(ch > max || ch < min)) return false;
            m_cursor--;
            return true;
        }

        protected virtual bool Eq_S(int s_size, string s)
        {
            if (m_limit - m_cursor < s_size) return false;
            int i;
            for (i = 0; i != s_size; i++)
            {
                if (m_current[m_cursor + i] != s[i]) return false;
            }
            m_cursor += s_size;
            return true;
        }

        protected virtual bool Eq_S_B(int s_size, string s)
        {
            if (m_cursor - m_limit_backward < s_size) return false;
            int i;
            for (i = 0; i != s_size; i++)
            {
                if (m_current[m_cursor - s_size + i] != s[i]) return false;
            }
            m_cursor -= s_size;
            return true;
        }

        protected virtual bool Eq_V(string s)
        {
            return Eq_S(s.Length, s);
        }

        protected virtual bool Eq_V_B(string s)
        {
            return Eq_S_B(s.Length, s);
        }

        protected virtual int FindAmong(Among[] v, int v_size)
        {
            int i = 0;
            int j = v_size;

            int c = m_cursor;
            int l = m_limit;

            int common_i = 0;
            int common_j = 0;

            bool first_key_inspected = false;

            while (true)
            {
                int k = i + ((j - i) >> 1);
                int diff = 0;
                int common = common_i < common_j ? common_i : common_j; // smaller
                Among w = v[k];
                int i2;
                for (i2 = common; i2 < w.SearchString.Length; i2++)
                {
                    if (c + common == l)
                    {
                        diff = -1;
                        break;
                    }
                    diff = m_current[c + common] - w.SearchString[i2];
                    if (diff != 0) break;
                    common++;
                }
                if (diff < 0)
                {
                    j = k;
                    common_j = common;
                }
                else
                {
                    i = k;
                    common_i = common;
                }
                if (j - i <= 1)
                {
                    if (i > 0) break; // v->s has been inspected
                    if (j == i) break; // only one item in v

                    // - but now we need to go round once more to get
                    // v->s inspected. This looks messy, but is actually
                    // the optimal approach.

                    if (first_key_inspected) break;
                    first_key_inspected = true;
                }
            }
            while (true)
            {
                Among w = v[i];
                // LUCENENET specific: Refactored Among to remove expensive
                // reflection calls and replaced with Func<bool> as was done in
                // the original code at:
                // https://github.com/snowballstem/snowball
                if (common_i >= w.SearchString.Length)
                {
                    m_cursor = c + w.SearchString.Length;
                    if (w.Action is null) return w.Result;
                    bool res = w.Action.Invoke();
                    m_cursor = c + w.SearchString.Length;
                    if (res) return w.Result;
                }
                i = w.MatchIndex;
                if (i < 0) return 0;
            }
        }

        // find_among_b is for backwards processing. Same comments apply
        protected virtual int FindAmongB(Among[] v, int v_size)
        {
            int i = 0;
            int j = v_size;

            int c = m_cursor;
            int lb = m_limit_backward;

            int common_i = 0;
            int common_j = 0;

            bool first_key_inspected = false;

            while (true)
            {
                int k = i + ((j - i) >> 1);
                int diff = 0;
                int common = common_i < common_j ? common_i : common_j;
                Among w = v[k];
                int i2;
                for (i2 = w.SearchString.Length - 1 - common; i2 >= 0; i2--)
                {
                    if (c - common == lb)
                    {
                        diff = -1;
                        break;
                    }
                    diff = m_current[c - 1 - common] - w.SearchString[i2];
                    if (diff != 0) break;
                    common++;
                }
                if (diff < 0)
                {
                    j = k;
                    common_j = common;
                }
                else
                {
                    i = k;
                    common_i = common;
                }
                if (j - i <= 1)
                {
                    if (i > 0) break;
                    if (j == i) break;
                    if (first_key_inspected) break;
                    first_key_inspected = true;
                }
            }
            while (true)
            {
                Among w = v[i];
                // LUCENENET specific: Refactored Among to remove expensive
                // reflection calls and replaced with Func<bool> as was done in
                // the original code at:
                // https://github.com/snowballstem/snowball
                if (common_i >= w.SearchString.Length)
                {
                    m_cursor = c - w.SearchString.Length;
                    if (w.Action is null) return w.Result;

                    bool res = w.Action.Invoke();
                    m_cursor = c - w.SearchString.Length;
                    if (res) return w.Result;
                }
                i = w.MatchIndex;
                if (i < 0) return 0;
            }
        }

        /// <summary>
        /// to replace chars between <paramref name="c_bra"/> and <paramref name="c_ket"/> in current by the
        /// chars in <paramref name="s"/>.
        /// </summary>
        protected virtual int ReplaceS(int c_bra, int c_ket, string s)
        {
            int adjustment = s.Length - (c_ket - c_bra);
            int newLength = m_limit + adjustment;
            //resize if necessary
            if (newLength > m_current.Length)
            {
                char[] newBuffer = new char[ArrayUtil.Oversize(newLength, RamUsageEstimator.NUM_BYTES_CHAR)];
                Arrays.Copy(m_current, 0, newBuffer, 0, m_limit);
                m_current = newBuffer;
            }
            // if the substring being replaced is longer or shorter than the
            // replacement, need to shift things around
            if (adjustment != 0 && c_ket < m_limit)
            {
                Arrays.Copy(m_current, c_ket, m_current, c_bra + s.Length,
                    m_limit - c_ket);
            }
            // insert the replacement text
            // Note, faster is s.getChars(0, s.length(), current, c_bra);
            // but would have to duplicate this method for both String and StringBuilder
            for (int i = 0; i < s.Length; i++)
                m_current[c_bra + i] = s[i];

            m_limit += adjustment;
            if (m_cursor >= c_ket) m_cursor += adjustment;
            else if (m_cursor > c_bra) m_cursor = c_bra;
            return adjustment;
        }

        protected virtual void SliceCheck()
        {
            if (m_bra < 0 ||
                m_bra > m_ket ||
                m_ket > m_limit)
            {
                throw new ArgumentException("faulty slice operation: bra=" + m_bra + ",ket=" + m_ket + ",limit=" + m_limit);
                // FIXME: report error somehow.
                /*
                fprintf(stderr, "faulty slice operation:\n");
                debug(z, -1, 0);
                exit(1);
                */
            }
        }

        protected virtual void SliceFrom(string s)
        {
            SliceCheck();
            ReplaceS(m_bra, m_ket, s);
        }

        protected virtual void SliceDel()
        {
            SliceFrom("");
        }

        protected virtual void Insert(int c_bra, int c_ket, string s)
        {
            int adjustment = ReplaceS(c_bra, c_ket, s);
            if (c_bra <= m_bra) m_bra += adjustment;
            if (c_bra <= m_ket) m_ket += adjustment;
        }

        /// <summary>
        /// Copy the slice into the supplied <see cref="StringBuilder"/>
        /// </summary>
        protected virtual StringBuilder SliceTo(StringBuilder s)
        {
            SliceCheck();
            int len = m_ket - m_bra;
            s.Length = 0;
            s.Append(m_current, m_bra, len);
            return s;
        }

        protected virtual StringBuilder AssignTo(StringBuilder s)
        {
            s.Length = 0;
            s.Append(m_current, 0, m_limit);
            return s;
        }

        /*
        extern void debug(struct SN_env * z, int number, int line_count)
        {   int i;
            int limit = SIZE(z->p);
            //if (number >= 0) printf("%3d (line %4d): '", number, line_count);
            if (number >= 0) printf("%3d (line %4d): [%d]'", number, line_count,limit);
            for (i = 0; i <= limit; i++)
            {   if (z->lb == i) printf("{");
                if (z->bra == i) printf("[");
                if (z->c == i) printf("|");
                if (z->ket == i) printf("]");
                if (z->l == i) printf("}");
                if (i < limit)
                {   int ch = z->p[i];
                    if (ch == 0) ch = '#';
                    printf("%c", ch);
                }
            }
            printf("'\n");
        }
        */
    }
}