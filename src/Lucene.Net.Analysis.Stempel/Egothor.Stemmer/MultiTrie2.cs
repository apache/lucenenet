using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Text;

/*
                    Egothor Software License version 1.00
                    Copyright (C) 1997-2004 Leo Galambos.
                 Copyright (C) 2002-2004 "Egothor developers"
                      on behalf of the Egothor Project.
                             All rights reserved.

   This  software  is  copyrighted  by  the "Egothor developers". If this
   license applies to a single file or document, the "Egothor developers"
   are the people or entities mentioned as copyright holders in that file
   or  document.  If  this  license  applies  to the Egothor project as a
   whole,  the  copyright holders are the people or entities mentioned in
   the  file CREDITS. This file can be found in the same location as this
   license in the distribution.

   Redistribution  and  use  in  source and binary forms, with or without
   modification, are permitted provided that the following conditions are
   met:
    1. Redistributions  of  source  code  must retain the above copyright
       notice, the list of contributors, this list of conditions, and the
       following disclaimer.
    2. Redistributions  in binary form must reproduce the above copyright
       notice, the list of contributors, this list of conditions, and the
       disclaimer  that  follows  these  conditions  in the documentation
       and/or other materials provided with the distribution.
    3. The name "Egothor" must not be used to endorse or promote products
       derived  from  this software without prior written permission. For
       written permission, please contact Leo.G@seznam.cz
    4. Products  derived  from this software may not be called "Egothor",
       nor  may  "Egothor"  appear  in  their name, without prior written
       permission from Leo.G@seznam.cz.

   In addition, we request that you include in the end-user documentation
   provided  with  the  redistribution  and/or  in the software itself an
   acknowledgement equivalent to the following:
   "This product includes software developed by the Egothor Project.
    http://egothor.sf.net/"

   THIS  SOFTWARE  IS  PROVIDED  ``AS  IS''  AND ANY EXPRESSED OR IMPLIED
   WARRANTIES,  INCLUDING,  BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
   MERCHANTABILITY  AND  FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
   IN  NO  EVENT  SHALL THE EGOTHOR PROJECT OR ITS CONTRIBUTORS BE LIABLE
   FOR   ANY   DIRECT,   INDIRECT,  INCIDENTAL,  SPECIAL,  EXEMPLARY,  OR
   CONSEQUENTIAL  DAMAGES  (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
   SUBSTITUTE  GOODS  OR  SERVICES;  LOSS  OF  USE,  DATA, OR PROFITS; OR
   BUSINESS  INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
   WHETHER  IN  CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE
   OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN
   IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

   This  software  consists  of  voluntary  contributions  made  by  many
   individuals  on  behalf  of  the  Egothor  Project  and was originally
   created by Leo Galambos (Leo.G@seznam.cz).
 */

namespace Egothor.Stemmer
{
    /// <summary>
    /// The <see cref="MultiTrie"/> is a <see cref="Trie"/> of <see cref="Trie"/>s.
    /// <para>
    /// It stores words and their associated patch commands. The <see cref="MultiTrie"/> handles
    /// patch commands broken into their constituent parts, as a <see cref="MultiTrie"/> does, but
    /// the commands are delimited by the skip command.
    /// </para>
    /// </summary>
    public class MultiTrie2 : MultiTrie
    {
        /// <summary>
        /// Constructor for the <see cref="MultiTrie"/> object.
        /// </summary>
        /// <param name="is">the input stream</param>
        /// <exception cref="IOException">if an I/O error occurs</exception>
        public MultiTrie2(IDataInput @is)
            : base(@is)
        {
        }

        /// <summary>
        /// Constructor for the <see cref="MultiTrie2"/> object
        /// </summary>
        /// <param name="forward">set to <c>true</c> if the elements should be read left to right</param>
        public MultiTrie2(bool forward)
            : base(forward)
        {
        }

        /// <summary>
        /// Return the element that is stored in a cell associated with the given key.
        /// </summary>
        /// <param name="key">the key to the cell holding the desired element</param>
        /// <returns>the element</returns>
        public override string GetFully(string key)
        {
            StringBuilder result = new StringBuilder(tries.Count * 2);
            try
            {
                string lastkey = key;
                string[] p = new string[tries.Count];
                char lastch = ' ';
                for (int i = 0; i < tries.Count; i++)
                {
                    string r = tries[i].GetFully(lastkey);
                    if (r == null || (r.Length == 1 && r[0] == EOM))
                    {
                        return result.ToString();
                    }
                    if (CannotFollow(lastch, r[0]))
                    {
                        return result.ToString();
                    }
                    else
                    {
                        lastch = r[r.Length - 2];
                    }
                    // key=key.substring(lengthPP(r));
                    p[i] = r;
                    if (p[i][0] == '-')
                    {
                        if (i > 0)
                        {
                            if (!TrySkip(key, LengthPP(p[i - 1]), out key))
                            {
                                break;
                            }
                        }
                        if (!TrySkip(key, LengthPP(p[i - 1]), out key))
                        {
                            break;
                        }
                    }
                    // key = skip(key, lengthPP(r));
                    result.Append(r);
                    if (key.Length != 0)
                    {
                        lastkey = key;
                    }
                }
            }
            catch (ArgumentOutOfRangeException /*x*/) { }
            return result.ToString();
        }

        /// <summary>
        /// Return the element that is stored as last on a path belonging to the given
        /// key.
        /// </summary>
        /// <param name="key">the key associated with the desired element</param>
        /// <returns>the element that is stored as last on a path</returns>
        public override string GetLastOnPath(string key)
        {
            StringBuilder result = new StringBuilder(tries.Count * 2);
            try
            {
                string lastkey = key;
                string[] p = new string[tries.Count];
                char lastch = ' ';
                for (int i = 0; i < tries.Count; i++)
                {
                    string r = tries[i].GetLastOnPath(lastkey);
                    if (r == null || (r.Length == 1 && r[0] == EOM))
                    {
                        return result.ToString();
                    }
                    // System.err.println("LP:"+key+" last:"+lastch+" new:"+r);
                    if (CannotFollow(lastch, r[0]))
                    {
                        return result.ToString();
                    }
                    else
                    {
                        lastch = r[r.Length - 2];
                    }
                    // key=key.substring(lengthPP(r));
                    p[i] = r;
                    if (p[i][0] == '-')
                    {
                        if (i > 0)
                        {
                            if (!TrySkip(key, LengthPP(p[i - 1]), out key))
                            {
                                break;
                            }
                        }
                        if (!TrySkip(key, LengthPP(p[i]), out key))
                        {
                            break;
                        }
                    }
                    // key = skip(key, lengthPP(r));
                    result.Append(r);
                    if (key.Length != 0)
                    {
                        lastkey = key;
                    }
                }
            }
            catch (ArgumentOutOfRangeException /*x*/) { }
            return result.ToString();
        }

        /// <summary>
        /// Write this data structure to the given output stream.
        /// </summary>
        /// <param name="os">the output stream</param>
        /// <exception cref="IOException">if an I/O error occurs</exception>
        public override void Store(IDataOutput os)
        {
            base.Store(os);
        }

        /// <summary>
        /// Add an element to this structure consisting of the given key and patch
        /// command.
        /// <para>
        /// This method will return without executing if the <paramref name="cmd"/>
        /// parameter's length is 0.
        /// </para>
        /// </summary>
        /// <param name="key">the key</param>
        /// <param name="cmd">the patch command</param>
        public override void Add(string key, string cmd)
        {
            if (cmd.Length == 0)
            {
                return;
            }
            // System.err.println( cmd );
            string[] p = Decompose(cmd);
            int levels = p.Length;
            // System.err.println("levels "+key+" cmd "+cmd+"|"+levels);
            while (levels >= tries.Count)
            {
                tries.Add(new Trie(forward));
            }
            string lastkey = key;
            for (int i = 0; i < levels; i++)
            {
                if (key.Length > 0)
                {
                    tries[i].Add(key, p[i]);
                    lastkey = key;
                }
                else
                {
                    tries[i].Add(lastkey, p[i]);
                }
                // System.err.println("-"+key+" "+p[i]+"|"+key.length());
                /*
                 * key=key.substring(lengthPP(p[i]));
                 */
                if (p[i].Length > 0 && p[i][0] == '-')
                {
                    if (i > 0)
                    {
                        if (!TrySkip(key, LengthPP(p[i - 1]), out key))
                        {
                            // LUCENENET: Should never happen, but since we don't
                            // have a catch block here who knows what might happen if
                            // we don't do this.
                            throw new ArgumentOutOfRangeException();
                        }
                    }
                    if (!TrySkip(key, LengthPP(p[i]), out key))
                    {
                        // LUCENENET: Should never happen, but since we don't
                        // have a catch block here who knows what might happen if
                        // we don't do this.
                        throw new ArgumentOutOfRangeException();
                    }
                }
                // System.err.println("--->"+key);
            }
            if (key.Length > 0)
            {
                tries[levels].Add(key, EOM_NODE);
            }
            else
            {
                tries[levels].Add(lastkey, EOM_NODE);
            }
        }

        /// <summary>
        /// Break the given patch command into its constituent pieces. The pieces are
        /// delimited by NOOP commands.
        /// </summary>
        /// <param name="cmd">the patch command</param>
        /// <returns>an array containing the pieces of the command</returns>
        public virtual string[] Decompose(string cmd)
        {
            int parts = 0;

            for (int i = 0; 0 <= i && i < cmd.Length;)
            {
                int next = DashEven(cmd, i);
                if (i == next)
                {
                    parts++;
                    i = next + 2;
                }
                else
                {
                    parts++;
                    i = next;
                }
            }

            string[] part = new string[parts];
            int x = 0;

            for (int i = 0; 0 <= i && i < cmd.Length;)
            {
                int next = DashEven(cmd, i);
                if (i == next)
                {
                    part[x++] = cmd.Substring(i, 2);
                    i = next + 2;
                }
                else
                {
                    part[x++] = (next < 0) ? cmd.Substring(i, cmd.Length - i) : cmd.Substring(i, next - i);
                    i = next;
                }
            }
            return part;
        }

        /// <summary>
        /// Remove empty rows from the given Trie and return the newly reduced Trie.
        /// </summary>
        /// <param name="by">the <see cref="Trie"/> to reduce</param>
        /// <returns>the newly reduced Trie</returns>
        public override Trie Reduce(Reduce by)
        {
            List<Trie> h = new List<Trie>();
            foreach (Trie trie in tries)
                h.Add(trie.Reduce(by));

            MultiTrie2 m = new MultiTrie2(forward);
            m.tries = h;
            return m;
        }

        private bool CannotFollow(char after, char goes)
        {
            switch (after)
            {
                case '-':
                case 'D':
                    return after == goes;
            }
            return false;
        }

        private bool TrySkip(string @in, int count, out string result)
        {
            // LUCENENET: Rather than relying on this to throw an exception by passing a negative
            // length to Substring like they did in Java, we check that the value
            // is negative and return false to the caller so it can safely break out
            // of the loop.
            int skipLength = @in.Length - count;
            if (skipLength < 0)
            {
                result = string.Empty;
                return false;
            }
            if (forward)
            {
                result = @in.Substring(count, skipLength);
            }
            else
            {
                result = @in.Substring(0, (skipLength) - 0);
            }
            return true;
        }

        private int DashEven(string @in, int from)
        {
            while (from < @in.Length)
            {
                if (@in[from] == '-')
                {
                    return from;
                }
                else
                {
                    from += 2;
                }
            }
            return -1;
        }


        private int LengthPP(string cmd)
        {
            int len = 0;
            for (int i = 0; i < cmd.Length; i++)
            {
                switch (cmd[i++])
                {
                    case '-':
                    case 'D':
                        len += cmd[i] - 'a' + 1;
                        break;
                    case 'R':
                        len++; /* intentional fallthrough */
                        goto case 'I';
                    case 'I':
                        break;
                }
            }
            return len;
        }
    }
}
