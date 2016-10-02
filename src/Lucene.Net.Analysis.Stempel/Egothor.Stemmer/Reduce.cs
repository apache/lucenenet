using Lucene.Net.Support;
using System.Collections.Generic;

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
    /// The <see cref="Reduce"/> object is used to remove gaps in a <see cref="Trie"/> which stores a dictionary.
    /// </summary>
    public class Reduce
    {
        /// <summary>
        /// Constructor for the <see cref="Reduce"/> object.
        /// </summary>
        public Reduce() { }

        /// <summary>
        /// Optimize (remove holes in the rows) the given <see cref="Trie"/> and return the
        /// restructured <see cref="Trie"/>.
        /// </summary>
        /// <param name="orig">the <see cref="Trie"/> to optimize</param>
        /// <returns>the restructured <see cref="Trie"/></returns>
        public virtual Trie Optimize(Trie orig)
        {
            IList<string> cmds = orig.cmds;
            IList<Row> rows = new List<Row>();
            IList<Row> orows = orig.rows;
            int[] remap = new int[orows.Count];

            Arrays.Fill(remap, -1);
            rows = RemoveGaps(orig.root, rows, new List<Row>(), remap);

            return new Trie(orig.forward, remap[orig.root], cmds, rows);
        }

        internal virtual IList<Row> RemoveGaps(int ind, IList<Row> old, IList<Row> to, int[] remap)
        {
            remap[ind] = to.Count;

            Row now = old[ind];
            to.Add(now);
            IEnumerator<Cell> i = now.cells.Values.GetEnumerator();
            for (; i.MoveNext();)
            {
                Cell c = i.Current;
                if (c.@ref >= 0 && remap[c.@ref] < 0)
                {
                    RemoveGaps(c.@ref, old, to, remap);
                }
            }
            to[remap[ind]] = new Remap(now, remap);
            return to;
        }

        /// <summary>
        /// This class is part of the Egothor Project
        /// </summary>
        internal class Remap : Row
        {
            /**
             * Constructor for the <see cref="Remap"/> object
             * 
             * @param old Description of the Parameter
             * @param remap Description of the Parameter
             */
            public Remap(Row old, int[] remap)
                        : base()
            {
                var i = old.cells.Keys.GetEnumerator();
                for (; i.MoveNext();)
                {
                    char ch = i.Current;
                    Cell c = old.At(ch);
                    Cell nc;
                    if (c.@ref >= 0)
                    {
                        nc = new Cell(c);
                        nc.@ref = remap[nc.@ref];
                    }
                    else
                    {
                        nc = new Cell(c);
                    }
                    cells[ch] = nc;
                }
            }
        }
    }
}
