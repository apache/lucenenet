using Lucene.Net.Support;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
    /// The Lift class is a data structure that is a variation of a Patricia trie.
    /// <para>
    /// Lift's <i>raison d'etre</i> is to implement reduction of the trie via the
    /// Lift-Up method., which makes the data structure less liable to overstemming.
    /// </para>
    /// </summary>
    public class Lift : Reduce
    {
        private readonly bool changeSkip; // LUCENENET: marked readonly

        /// <summary>
        /// Constructor for the Lift object.
        /// </summary>
        /// <param name="changeSkip">
        /// when set to <c>true</c>, comparison of two Cells takes
        /// a skip command into account
        /// </param>
        public Lift(bool changeSkip)
        {
            this.changeSkip = changeSkip;
        }

        /// <summary>
        /// Optimize (eliminate rows with no content) the given Trie and return the
        /// reduced Trie.
        /// </summary>
        /// <param name="orig">the Trie to optimized</param>
        /// <returns>the reduced Trie</returns>
        public override Trie Optimize(Trie orig)
        {
            IList<string> cmds = orig.cmds;
            IList<Row> rows; // LUCENENET: IDE0059: Remove unnecessary value assignment
            IList<Row> orows = orig.rows;
            int[] remap = new int[orows.Count];

            for (int j = orows.Count - 1; j >= 0; j--)
            {
                LiftUp(orows[j], orows);
            }

            Arrays.Fill(remap, -1);
            rows = RemoveGaps(orig.root, orows, new JCG.List<Row>(), remap);

            return new Trie(orig.forward, remap[orig.root], cmds, rows);
        }

        /// <summary>
        /// Reduce the trie using Lift-Up reduction.
        /// <para>
        /// The Lift-Up reduction propagates all leaf-values (patch commands), where
        /// possible, to higher levels which are closer to the root of the trie.
        /// </para>
        /// </summary>
        /// <param name="in">the Row to consider when optimizing</param>
        /// <param name="nodes">contains the patch commands</param>
        public void LiftUp(Row @in, IList<Row> nodes)
        {
            foreach (Cell c in @in.cells.Values)
            {
                if (c.@ref >= 0)
                {
                    Row to = nodes[c.@ref];
                    int sum = to.UniformCmd(changeSkip);
                    if (sum >= 0)
                    {
                        if (sum == c.cmd)
                        {
                            if (changeSkip)
                            {
                                if (c.skip != to.uniformSkip + 1)
                                {
                                    continue;
                                }
                                c.skip = to.uniformSkip + 1;
                            }
                            else
                            {
                                c.skip = 0;
                            }
                            c.cnt += to.uniformCnt;
                            c.@ref = -1;
                        }
                        else if (c.cmd < 0)
                        {
                            c.cnt = to.uniformCnt;
                            c.cmd = sum;
                            c.@ref = -1;
                            if (changeSkip)
                            {
                                c.skip = to.uniformSkip + 1;
                            }
                            else
                            {
                                c.skip = 0;
                            }
                        }
                    }
                }
            }
        }
    }
}
