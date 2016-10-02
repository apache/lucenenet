using Lucene.Net.Support;
using System.Collections.Generic;
using System.IO;
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
    /// The <see cref="MultiTrie"/> is a <see cref="Trie"/> of <see cref="Trie"/>s. It stores words and their associated patch
    /// commands. The <see cref="MultiTrie"/> handles patch commands individually (each command by
    /// itself).
    /// </summary>
    public class MultiTrie : Trie
    {
        internal static char EOM = '*';
        internal static string EOM_NODE = "" + EOM;

        protected List<Trie> tries = new List<Trie>();

        int BY = 1;

        /// <summary>
        /// Constructor for the <see cref="MultiTrie"/> object.
        /// </summary>
        /// <param name="is">the input stream</param>
        /// <exception cref="IOException">if an I/O error occurs</exception>
        public MultiTrie(IDataInput @is)
            : base(false)
        {
            forward = @is.ReadBoolean();
            BY = @is.ReadInt();
            for (int i = @is.ReadInt(); i > 0; i--)
            {
                tries.Add(new Trie(@is));
            }
        }

        /// <summary>
        /// Constructor for the <see cref="MultiTrie"/> object
        /// </summary>
        /// <param name="forward">set to <c>true</c> if the elements should be read left to right</param>
        public MultiTrie(bool forward)
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
            for (int i = 0; i < tries.Count; i++)
            {
                string r = tries[i].GetFully(key);
                if (r == null || (r.Length == 1 && r[0] == EOM))
                {
                    return result.ToString();
                }
                result.Append(r);
            }
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
            for (int i = 0; i < tries.Count; i++)
            {
                string r = tries[i].GetLastOnPath(key);
                if (r == null || (r.Length == 1 && r[0] == EOM))
                {
                    return result.ToString();
                }
                result.Append(r);
            }
            return result.ToString();
        }

        /// <summary>
        /// Write this data structure to the given output stream.
        /// </summary>
        /// <param name="os">the output stream</param>
        /// <exception cref="IOException">if an I/O error occurs</exception>
        public override void Store(IDataOutput os)
        {
            os.WriteBoolean(forward);
            os.WriteInt(BY);
            os.WriteInt(tries.Count);
            foreach (Trie trie in tries)
                trie.Store(os);
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
            int levels = cmd.Length / BY;
            while (levels >= tries.Count)
            {
                tries.Add(new Trie(forward));
            }
            for (int i = 0; i < levels; i++)
            {
                tries[i].Add(key, cmd.Substring(BY * i, BY));
            }
            tries[levels].Add(key, EOM_NODE);
        }

        /// <summary>
        /// Remove empty rows from the given <see cref="Trie"/> and return the newly reduced <see cref="Trie"/>.
        /// </summary>
        /// <param name="by">the <see cref="Trie"/> to reduce</param>
        /// <returns>the newly reduced Trie</returns>
        public override Trie Reduce(Reduce by)
        {
            List<Trie> h = new List<Trie>();
            foreach (Trie trie in tries)
                h.Add(trie.Reduce(by));

            MultiTrie m = new MultiTrie(forward);
            m.tries = h;
            return m;
        }

        /// <summary>
        /// Print the given prefix and the position(s) in the Trie where it appears.
        /// </summary>
        /// <param name="out"></param>
        /// <param name="prefix">the desired prefix</param>
        public override void PrintInfo(TextWriter @out, string prefix)
        {
            int c = 0;
            foreach (Trie trie in tries)
                trie.PrintInfo(@out, prefix + "[" + (++c) + "] ");
        }
    }
}
