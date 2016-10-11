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
    /// The <see cref="Optimizer"/> class is a <see cref="Trie"/> that will be reduced (have empty rows removed).
    /// <para>
    /// The reduction will be made by joining two rows where the first is a subset of
    /// the second.
    /// </para>
    /// </summary>
    public class Optimizer : Reduce
    {
        /// <summary>
        /// Constructor for the <see cref="Optimizer"/> object.
        /// </summary>
        public Optimizer() { }

        /// <summary>
        /// Optimize (remove empty rows) from the given Trie and return the resulting
        /// Trie.
        /// </summary>
        /// <param name="orig">the <see cref="Trie"/> to consolidate</param>
        /// <returns>the newly consolidated Trie</returns>
        public override Trie Optimize(Trie orig)
        {
            IList<string> cmds = orig.cmds;
            IList<Row> rows = new List<Row>();
            IList<Row> orows = orig.rows;
            int[] remap = new int[orows.Count];

            for (int j = orows.Count - 1; j >= 0; j--)
            {
                Row now = new Remap(orows[j], remap);
                bool merged = false;

                for (int i = 0; i < rows.Count; i++)
                {
                    Row q = Merge(now, rows[i]);
                    if (q != null)
                    {
                        rows[i] = q;
                        merged = true;
                        remap[j] = i;
                        break;
                    }
                }

                if (merged == false)
                {
                    remap[j] = rows.Count;
                    rows.Add(now);
                }
            }

            int root = remap[orig.root];
            Arrays.Fill(remap, -1);
            rows = RemoveGaps(root, rows, new List<Row>(), remap);

            return new Trie(orig.forward, remap[root], cmds, rows);
        }

        /// <summary>
        /// Merge the given rows and return the resulting <see cref="Row"/>.
        /// </summary>
        /// <param name="master">the master <see cref="Row"/></param>
        /// <param name="existing">the existing <see cref="Row"/></param>
        /// <returns>the resulting <see cref="Row"/>, or <c>null</c> if the operation cannot be realized</returns>
        public Row Merge(Row master, Row existing)
        {
            var i = master.cells.Keys.GetEnumerator();
            Row n = new Row();
            for (; i.MoveNext();)
            {
                char ch = i.Current;
                // XXX also must handle Cnt and Skip !!
                Cell a = master.cells.ContainsKey(ch) ? master.cells[ch] : null;
                Cell b = existing.cells.ContainsKey(ch) ? existing.cells[ch] : null;

                Cell s = (b == null) ? new Cell(a) : Merge(a, b);
                if (s == null)
                {
                    return null;
                }
                n.cells[ch] = s;
            }
            i = existing.cells.Keys.GetEnumerator();
            for (; i.MoveNext();)
            {
                char ch = i.Current;
                if (master.At(ch) != null)
                {
                    continue;
                }
                n.cells[ch] = existing.At(ch);
            }
            return n;
        }

        /// <summary>
        /// Merge the given <see cref="Cell"/>s and return the resulting <see cref="Cell"/>.
        /// </summary>
        /// <param name="m">the master <see cref="Cell"/></param>
        /// <param name="e">the existing <see cref="Cell"/></param>
        /// <returns>the resulting <see cref="Cell"/>, or <c>null</c> if the operation cannot be realized</returns>
        public virtual Cell Merge(Cell m, Cell e)
        {
            Cell n = new Cell();

            if (m.skip != e.skip)
            {
                return null;
            }

            if (m.cmd >= 0)
            {
                if (e.cmd >= 0)
                {
                    if (m.cmd == e.cmd)
                    {
                        n.cmd = m.cmd;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    n.cmd = m.cmd;
                }
            }
            else
            {
                n.cmd = e.cmd;
            }
            if (m.@ref >= 0)
            {
                if (e.@ref >= 0)
                {
                    if (m.@ref == e.@ref)
                    {
                        if (m.skip == e.skip)
                        {
                            n.@ref = m.@ref;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    n.@ref = m.@ref;
                }
            }
            else
            {
                n.@ref = e.@ref;
            }
            n.cnt = m.cnt + e.cnt;
            n.skip = m.skip;
            return n;
        }
    }
}
