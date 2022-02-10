using Lucene;
using Lucene.Net.Support;
using System;
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
    /// The Diff object generates a patch string.
    /// <para>
    /// A patch string is actually a command to a stemmer telling it how to reduce a
    /// word to its root. For example, to reduce the word teacher to its root teach
    /// the patch string Db would be generated. This command tells the stemmer to
    /// delete the last 2 characters from the word teacher to reach the stem (the
    /// patch commands are applied starting from the last character in order to save
    /// </para>
    /// </summary>
    public class Diff
    {
        private int sizex = 0;
        private int sizey = 0;
        private int[][] net;
        private int[][] way;

        private readonly int INSERT; // LUCENENET: marked readonly
        private readonly int DELETE; // LUCENENET: marked readonly
        private readonly int REPLACE; // LUCENENET: marked readonly
        private readonly int NOOP; // LUCENENET: marked readonly

        /// <summary>
        /// Constructor for the Diff object.
        /// </summary>
        public Diff()
            : this(1, 1, 1, 0)
        {
        }

        /// <summary>
        /// Constructor for the Diff object
        /// </summary>
        /// <param name="ins">Description of the Parameter</param>
        /// <param name="del">Description of the Parameter</param>
        /// <param name="rep">Description of the Parameter</param>
        /// <param name="noop">Description of the Parameter</param>
        public Diff(int ins, int del, int rep, int noop)
        {
            INSERT = ins;
            DELETE = del;
            REPLACE = rep;
            NOOP = noop;
        }

        /// <summary>
        /// Apply the given patch string <paramref name="diff"/> to the given string
        /// <paramref name="dest"/>
        /// </summary>
        /// <param name="dest">Destination string</param>
        /// <param name="diff">Patch string</param>
        public static void Apply(StringBuilder dest, string diff)
        {
            try
            {

                if (diff is null)
                {
                    return;
                }

                int pos = dest.Length - 1;
                if (pos < 0)
                {
                    return;
                }
                // orig == ""
                for (int i = 0; i < diff.Length / 2; i++)
                {
                    char cmd = diff[2 * i];
                    char param = diff[2 * i + 1];
                    int par_num = (param - 'a' + 1);
                    switch (cmd)
                    {
                        case '-':
                            pos = pos - par_num + 1;
                            break;
                        case 'R':
                            dest[pos] = param;
                            break;
                        case 'D':
                            int o = pos;
                            pos -= par_num - 1;
                            /*
                             * delete par_num chars from index pos
                             */
                            // String s = orig.toString();
                            // s = s.substring( 0, pos ) + s.substring( o + 1 );
                            // orig = new StringBuffer( s );
                            dest.Remove(pos, (o + 1) - pos);
                            break;
                        case 'I':
                            dest.Insert(pos += 1, param);
                            break;
                    }
                    pos--;
                }
            }
            catch (Exception x) when (x.IsStringIndexOutOfBoundsException())
            {
                // x.printStackTrace();
            }
            catch (Exception x) when (x.IsArrayIndexOutOfBoundsException())
            {
                // x.printStackTrace();
            }
        }

        /// <summary>
        /// Construct a patch string that transforms a to b.
        /// </summary>
        /// <param name="a">1st string</param>
        /// <param name="b">2nd string</param>
        /// <returns></returns>
        public string Exec(string a, string b)
        {
            if (a is null || b is null)
            {
                return null;
            }

            int x;
            int y;
            int maxx;
            int maxy;
            int[] go = new int[4];
            const int X = 1;
            const int Y = 2;
            const int R = 3;
            const int D = 0;

            /*
             * setup memory if needed => processing speed up
             */
            maxx = a.Length + 1;
            maxy = b.Length + 1;
            if ((maxx >= sizex) || (maxy >= sizey))
            {
                sizex = maxx + 8;
                sizey = maxy + 8;
                net = RectangularArrays.ReturnRectangularArray<int>(sizex, sizey);
                way = RectangularArrays.ReturnRectangularArray<int>(sizex, sizey);
            }

            /*
             * clear the network
             */
            for (x = 0; x < maxx; x++)
            {
                for (y = 0; y < maxy; y++)
                {
                    net[x][y] = 0;
                }
            }

            /*
             * set known persistent values
             */
            for (x = 1; x < maxx; x++)
            {
                net[x][0] = x;
                way[x][0] = X;
            }
            for (y = 1; y < maxy; y++)
            {
                net[0][y] = y;
                way[0][y] = Y;
            }

            for (x = 1; x < maxx; x++)
            {
                for (y = 1; y < maxy; y++)
                {
                    go[X] = net[x - 1][y] + DELETE;
                    // way on x costs 1 unit
                    go[Y] = net[x][y - 1] + INSERT;
                    // way on y costs 1 unit
                    go[R] = net[x - 1][y - 1] + REPLACE;
                    go[D] = net[x - 1][y - 1]
                        + ((a[x - 1] == b[y - 1]) ? NOOP : 100);
                    // diagonal costs 0, when no change
                    ushort min = (ushort)D;
                    if (go[min] >= go[X])
                    {
                        min = (ushort)X;
                    }
                    if (go[min] > go[Y])
                    {
                        min = (ushort)Y;
                    }
                    if (go[min] > go[R])
                    {
                        min = (ushort)R;
                    }
                    way[x][y] = min;
                    net[x][y] = (ushort)go[min];
                }
            }

            // read the patch string
            StringBuilder result = new StringBuilder();
            char @base = (char)('a' - 1);
            char deletes = @base;
            char equals = @base;
            for (x = maxx - 1, y = maxy - 1; x + y != 0;)
            {
                switch (way[x][y])
                {
                    case X:
                        if (equals != @base)
                        {
                            result.Append("-" + (equals));
                            equals = @base;
                        }
                        deletes++;
                        x--;
                        break;
                    // delete
                    case Y:
                        if (deletes != @base)
                        {
                            result.Append("D" + (deletes));
                            deletes = @base;
                        }
                        if (equals != @base)
                        {
                            result.Append("-" + (equals));
                            equals = @base;
                        }
                        result.Append('I');
                        result.Append(b[--y]);
                        break;
                    // insert
                    case R:
                        if (deletes != @base)
                        {
                            result.Append("D" + (deletes));
                            deletes = @base;
                        }
                        if (equals != @base)
                        {
                            result.Append("-" + (equals));
                            equals = @base;
                        }
                        result.Append('R');
                        result.Append(b[--y]);
                        x--;
                        break;
                    // replace
                    case D:
                        if (deletes != @base)
                        {
                            result.Append("D" + (deletes));
                            deletes = @base;
                        }
                        equals++;
                        x--;
                        y--;
                        break;
                        // no change
                }
            }
            if (deletes != @base)
            {
                result.Append("D" + (deletes));
                //deletes = @base; // LUCENENET: IDE0059: Remove unnecessary value assignment
            }

            return result.ToString();
        }
    }
}
