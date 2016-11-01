using Lucene.Net.Support;
using System.Collections.Generic;
using System.IO;

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
    /// A <see cref="Trie"/> is used to store a dictionary of words and their stems.
    /// <para>
    /// Actually, what is stored are words with their respective patch commands. A
    /// trie can be termed forward (keys read from left to right) or backward (keys
    /// read from right to left). This property will vary depending on the language
    /// for which a <see cref="Trie"/> is constructed.
    /// </para>
    /// </summary>
    public class Trie
    {
        internal IList<Row> rows = new List<Row>();
        internal IList<string> cmds = new List<string>();
        internal int root;

        internal bool forward = false;

        /// <summary>
        /// Constructor for the <see cref="Trie"/> object.
        /// </summary>
        /// <param name="is">the input stream</param>
        /// <exception cref="IOException">if an I/O error occurs</exception>
        public Trie(IDataInput @is)
        {
            forward = @is.ReadBoolean();
            root = @is.ReadInt();
            for (int i = @is.ReadInt(); i > 0; i--)
            {
                cmds.Add(@is.ReadUTF());
            }
            for (int i = @is.ReadInt(); i > 0; i--)
            {
                rows.Add(new Row(@is));
            }
        }

        /// <summary>
        /// Constructor for the <see cref="Trie"/> object.
        /// </summary>
        /// <param name="forward">set to <c>true</c></param>
        public Trie(bool forward)
        {
            rows.Add(new Row());
            root = 0;
            this.forward = forward;
        }

        /// <summary>
        /// Constructor for the <see cref="Trie"/> object.
        /// </summary>
        /// <param name="forward"><c>true</c> if read left to right, <c>false</c> if read right to left</param>
        /// <param name="root">index of the row that is the root node</param>
        /// <param name="cmds">the patch commands to store</param>
        /// <param name="rows">a Vector of Vectors. Each inner Vector is a node of this <see cref="Trie"/></param>
        public Trie(bool forward, int root, IList<string> cmds, IList<Row> rows)
        {
            this.rows = rows;
            this.cmds = cmds;
            this.root = root;
            this.forward = forward;
        }

        /// <summary>
        /// Gets the all attribute of the <see cref="Trie"/> object
        /// </summary>
        /// <param name="key">Description of the Parameter</param>
        /// <returns>The all value</returns>
        public virtual string[] GetAll(string key)
        {
            int[] res = new int[key.Length];
            int resc = 0;
            Row now = GetRow(root);
            int w;
            StrEnum e = new StrEnum(key, forward);
            bool br = false;

            for (int i = 0; i < key.Length - 1; i++)
            {
                char ch = e.Next();
                w = now.GetCmd(ch);
                if (w >= 0)
                {
                    int n = w;
                    for (int j = 0; j < resc; j++)
                    {
                        if (n == res[j])
                        {
                            n = -1;
                            break;
                        }
                    }
                    if (n >= 0)
                    {
                        res[resc++] = n;
                    }
                }
                w = now.GetRef(ch);
                if (w >= 0)
                {
                    now = GetRow(w);
                }
                else
                {
                    br = true;
                    break;
                }
            }
            if (br == false)
            {
                w = now.GetCmd(e.Next());
                if (w >= 0)
                {
                    int n = w;
                    for (int j = 0; j < resc; j++)
                    {
                        if (n == res[j])
                        {
                            n = -1;
                            break;
                        }
                    }
                    if (n >= 0)
                    {
                        res[resc++] = n;
                    }
                }
            }

            if (resc < 1)
            {
                return null;
            }
            string[] R = new string[resc];
            for (int j = 0; j < resc; j++)
            {
                R[j] = cmds[res[j]];
            }
            return R;
        }

        /// <summary>
        /// Return the number of cells in this <see cref="Trie"/> object.
        /// </summary>
        /// <returns>the number of cells</returns>
        public virtual int GetCells()
        {
            int size = 0;
            foreach (Row row in rows)
                size += row.GetCells();
            return size;
        }

        /// <summary>
        /// Gets the cellsPnt attribute of the <see cref="Trie"/> object
        /// </summary>
        /// <returns>The cellsPnt value</returns>
        public virtual int GetCellsPnt()
        {
            int size = 0;
            foreach (Row row in rows)
                size += row.GetCellsPnt();
            return size;
        }

        /// <summary>
        /// Gets the cellsVal attribute of the <see cref="Trie"/> object
        /// </summary>
        /// <returns>The cellsVal value</returns>
        public virtual int GetCellsVal()
        {
            int size = 0;
            foreach (Row row in rows)
                size += row.GetCellsVal();
            return size;
        }

        /// <summary>
        /// Return the element that is stored in a cell associated with the given key.
        /// </summary>
        /// <param name="key">the key</param>
        /// <returns>the associated element</returns>
        public virtual string GetFully(string key)
        {
            Row now = GetRow(root);
            int w;
            Cell c;
            int cmd = -1;
            StrEnum e = new StrEnum(key, forward);
            char ch;
            char aux;

            for (int i = 0; i < key.Length;)
            {
                ch = e.Next();
                i++;

                c = now.At(ch);
                if (c == null)
                {
                    return null;
                }

                cmd = c.cmd;

                for (int skip = c.skip; skip > 0; skip--)
                {
                    if (i < key.Length)
                    {
                        aux = e.Next();
                    }
                    else
                    {
                        return null;
                    }
                    i++;
                }

                w = now.GetRef(ch);
                if (w >= 0)
                {
                    now = GetRow(w);
                }
                else if (i < key.Length)
                {
                    return null;
                }
            }
            return (cmd == -1) ? null : cmds[cmd];
        }

        /// <summary>
        /// Return the element that is stored as last on a path associated with the
        /// given key.
        /// </summary>
        /// <param name="key">the key associated with the desired element</param>
        /// <returns>the last on path element</returns>
        public virtual string GetLastOnPath(string key)
        {
            Row now = GetRow(root);
            int w;
            string last = null;
            StrEnum e = new StrEnum(key, forward);

            for (int i = 0; i < key.Length - 1; i++)
            {
                char ch = e.Next();
                w = now.GetCmd(ch);
                if (w >= 0)
                {
                    last = cmds[w];
                }
                w = now.GetRef(ch);
                if (w >= 0)
                {
                    now = GetRow(w);
                }
                else
                {
                    return last;
                }
            }
            w = now.GetCmd(e.Next());
            return (w >= 0) ? cmds[w] : last;
        }

        /// <summary>
        /// Return the <see cref="Row"/> at the given index.
        /// </summary>
        /// <param name="index">the index containing the desired <see cref="Row"/></param>
        /// <returns>the <see cref="Row"/></returns>
        private Row GetRow(int index)
        {
            if (index < 0 || index >= rows.Count)
            {
                return null;
            }
            return rows[index];
        }

        /// <summary>
        /// Write this <see cref="Trie"/> to the given output stream.
        /// </summary>
        /// <param name="os">the output stream</param>
        /// <exception cref="IOException">if an I/O error occurs</exception>
        public virtual void Store(IDataOutput os)
        {
            os.WriteBoolean(forward);
            os.WriteInt(root);
            os.WriteInt(cmds.Count);
            foreach (string cmd in cmds)
                os.WriteUTF(cmd);

            os.WriteInt(rows.Count);
            foreach (Row row in rows)
                row.Store(os);
        }

        /// <summary>
        /// Add the given key associated with the given patch command. If either
        /// parameter is null this method will return without executing.
        /// </summary>
        /// <param name="key">the key</param>
        /// <param name="cmd">the patch command</param>
        public virtual void Add(string key, string cmd)
        {
            if (key == null || cmd == null)
            {
                return;
            }
            if (cmd.Length == 0)
            {
                return;
            }
            int id_cmd = cmds.IndexOf(cmd);
            if (id_cmd == -1)
            {
                id_cmd = cmds.Count;
                cmds.Add(cmd);
            }

            int node = root;
            Row r = GetRow(node);

            StrEnum e = new StrEnum(key, forward);

            for (int i = 0; i < e.Length - 1; i++)
            {
                char ch = e.Next();
                node = r.GetRef(ch);
                if (node >= 0)
                {
                    r = GetRow(node);
                }
                else
                {
                    node = rows.Count;
                    Row n;
                    rows.Add(n = new Row());
                    r.SetRef(ch, node);
                    r = n;
                }
            }
            r.SetCmd(e.Next(), id_cmd);
        }

        /// <summary>
        /// Remove empty rows from the given <see cref="Trie"/> and return the newly reduced <see cref="Trie"/>.
        /// </summary>
        /// <param name="by">the <see cref="Trie"/> to reduce</param>
        /// <returns>newly reduced <see cref="Trie"/></returns>
        public virtual Trie Reduce(Reduce by)
        {
            return by.Optimize(this);
        }

        /// <summary>
        /// writes debugging info to the printstream
        /// </summary>
        public virtual void PrintInfo(TextWriter @out, string prefix)
        {
            @out.WriteLine(prefix + "nds " + rows.Count + " cmds " + cmds.Count
                + " cells " + GetCells() + " valcells " + GetCellsVal() + " pntcells "
                + GetCellsPnt());
        }

        /// <summary>
        /// This class is part of the Egothor Project
        /// </summary>
        internal class StrEnum
        {
            private string s;
            private int from;
            private int by;

            /// <summary>
            /// Constructor for the <see cref="StrEnum"/> object
            /// </summary>
            /// <param name="s">Description of the Parameter</param>
            /// <param name="up">Description of the Parameter</param>
            internal StrEnum(string s, bool up)
            {
                this.s = s;
                if (up)
                {
                    from = 0;
                    by = 1;
                }
                else
                {
                    from = s.Length - 1;
                    by = -1;
                }
            }

            internal int Length
            {
                get
                {
                    return s.Length;
                }
            }

            internal char Next()
            {
                char ch = s[from];
                from += by;
                return ch;
            }
        }
    }
}
