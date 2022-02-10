using J2N.IO;
using System.Collections.Generic;
using System.IO;
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
    /// The <see cref="Row"/> class represents a row in a matrix representation of a <see cref="Trie"/>.
    /// </summary>
    public class Row
    {
        internal IDictionary<char, Cell> cells = new JCG.SortedDictionary<char, Cell>();
        internal int uniformCnt = 0;
        internal int uniformSkip = 0;

        /// <summary>
        /// Construct a <see cref="Row"/> object from input carried in via the given input stream.
        /// </summary>
        /// <param name="is">the input stream</param>
        /// <exception cref="IOException">if an I/O error occurs</exception>
        public Row(IDataInput @is)
        {
            for (int i = @is.ReadInt32(); i > 0; i--)
            {
                char ch = @is.ReadChar();
                Cell c = new Cell();
                c.cmd = @is.ReadInt32();
                c.cnt = @is.ReadInt32();
                c.@ref = @is.ReadInt32();
                c.skip = @is.ReadInt32();
                cells[ch] = c;
            }
        }

        /// <summary>
        /// The default constructor for the <see cref="Row"/> object.
        /// </summary>
        public Row() { }

        /// <summary>
        /// Construct a <see cref="Row"/> using the cells of the given <see cref="Row"/>.
        /// </summary>
        /// <param name="old">the <see cref="Row"/> to copy</param>
        public Row(Row old)
        {
            cells = old.cells;
        }

        /// <summary>
        /// Set the command in the <see cref="Cell"/> of the given <see cref="char"/> to the given <see cref="int"/>.
        /// </summary>
        /// <param name="way">the <see cref="char"/> defining the <see cref="Cell"/></param>
        /// <param name="cmd">the new command</param>
        public void SetCmd(char way, int cmd)
        {
            Cell c = At(way);
            if (c is null)
            {
                c = new Cell();
                c.cmd = cmd;
                cells[way] = c;
            }
            else
            {
                c.cmd = cmd;
            }
            c.cnt = (cmd >= 0) ? 1 : 0;
        }

        /// <summary>
        /// Set the reference to the next row in the <see cref="Cell"/> of the given <see cref="char"/> to the
        /// given <see cref="int"/>.
        /// </summary>
        /// <param name="way">the <see cref="char"/> defining the <see cref="Cell"/></param>
        /// <param name="ref">The new ref value</param>
        public void SetRef(char way, int @ref)
        {
            Cell c = At(way);
            if (c is null)
            {
                c = new Cell();
                c.@ref = @ref;
                cells[way] = c;
            }
            else
            {
                c.@ref = @ref;
            }
        }

        /// <summary>
        /// Return the number of cells in use.
        /// </summary>
        /// <returns>the number of cells in use</returns>
        public int GetCells()
        {
            int size = 0;
            foreach (char c in cells.Keys)
            {
                Cell e = At(c);
                if (e.cmd >= 0 || e.@ref >= 0)
                {
                    size++;
                }
            }
            return size;
        }

        /// <summary>
        /// Return the number of references (how many transitions) to other rows.
        /// </summary>
        /// <returns>the number of references</returns>
        public int GetCellsPnt()
        {
            int size = 0;
            foreach (char c in cells.Keys)
            {
                Cell e = At(c);
                if (e.@ref >= 0)
                {
                    size++;
                }
            }
            return size;
        }

        /// <summary>
        /// Return the number of patch commands saved in this Row.
        /// </summary>
        /// <returns>the number of patch commands</returns>
        public int GetCellsVal()
        {
            int size = 0;
            foreach (char c in cells.Keys)
            {
                Cell e = At(c);
                if (e.cmd >= 0)
                {
                    size++;
                }
            }
            return size;
        }

        /// <summary>
        /// Return the command in the <see cref="Cell"/> associated with the given <see cref="char"/>.
        /// </summary>
        /// <param name="way">the <see cref="char"/> associated with the <see cref="Cell"/> holding the desired command</param>
        /// <returns>the command</returns>
        public int GetCmd(char way)
        {
            Cell c = At(way);
            return (c is null) ? -1 : c.cmd;
        }

        /// <summary>
        /// Return the number of patch commands were in the <see cref="Cell"/> associated with the
        /// given <see cref="char"/> before the <see cref="Trie"/> containing this <see cref="Row"/> was reduced.
        /// </summary>
        /// <param name="way">the <see cref="char"/> associated with the desired <see cref="Cell"/></param>
        /// <returns>the number of patch commands before reduction</returns>
        public int GetCnt(char way)
        {
            Cell c = At(way);
            return (c is null) ? -1 : c.cnt;
        }

        /// <summary>
        /// Return the reference to the next <see cref="Row"/> in the <see cref="Cell"/> associated with the given
        /// <see cref="char"/>.
        /// </summary>
        /// <param name="way">the <see cref="char"/> associated with the desired <see cref="Cell"/></param>
        /// <returns>the reference, or -1 if the <see cref="Cell"/> is <c>null</c></returns>
        public int GetRef(char way)
        {
            Cell c = At(way);
            return (c is null) ? -1 : c.@ref;
        }

        /// <summary>
        /// Write the contents of this <see cref="Row"/> to the given output stream.
        /// </summary>
        /// <param name="os">the output stream</param>
        /// <exception cref="IOException">if an I/O error occurs</exception>
        public virtual void Store(IDataOutput os)
        {
            os.WriteInt32(cells.Count);
            foreach (char c in cells.Keys)
            {
                Cell e = At(c);
                if (e.cmd < 0 && e.@ref < 0)
                {
                    continue;
                }

                os.WriteChar(c);
                os.WriteInt32(e.cmd);
                os.WriteInt32(e.cnt);
                os.WriteInt32(e.@ref);
                os.WriteInt32(e.skip);
            }
        }

        /// <summary>
        /// Return the number of identical <see cref="Cell"/>s (containing patch commands) in this
        /// Row.
        /// </summary>
        /// <param name="eqSkip">when set to <c>false</c> the removed patch commands are considered</param>
        /// <returns>the number of identical <see cref="Cell"/>s, or -1 if there are (at least) two different <see cref="Cell"/>s</returns>
        public int UniformCmd(bool eqSkip)
        {
            int ret = -1;
            uniformCnt = 1;
            uniformSkip = 0;
            foreach (Cell c in cells.Values)
            {
                if (c.@ref >= 0)
                {
                    return -1;
                }
                if (c.cmd >= 0)
                {
                    if (ret < 0)
                    {
                        ret = c.cmd;
                        uniformSkip = c.skip;
                    }
                    else if (ret == c.cmd)
                    {
                        if (eqSkip)
                        {
                            if (uniformSkip == c.skip)
                            {
                                uniformCnt++;
                            }
                            else
                            {
                                return -1;
                            }
                        }
                        else
                        {
                            uniformCnt++;
                        }
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Write the contents of this <see cref="Row"/> to the <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="out"></param>
        public virtual void Print(TextWriter @out)
        {
            foreach (char ch in cells.Keys)
            {
                Cell c = At(ch);
                @out.Write("[" + ch + ":" + c + "]");
            }
            @out.WriteLine();
        }

        internal Cell At(char index)
        {
            cells.TryGetValue(index, out Cell value);
            return value;
        }
    }
}
