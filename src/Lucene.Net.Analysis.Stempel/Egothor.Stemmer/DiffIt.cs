using J2N.Text;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;
using Integer = J2N.Numerics.Int32;
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
    /// The DiffIt class is a means generate patch commands from an already prepared
    /// stemmer table.
    /// </summary>
    public static class DiffIt // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        internal static int Get(int i, string s)
        {
            // LUCENENET: Optimized so we don't alocate a substring
            if (!Integer.TryParse(s, i, 1, radix: 10, out int result))
            {
                return 1;
            }

            return result;
        }

        /// <summary>
        /// Entry point to the DiffIt application.
        /// <para>
        /// This application takes one argument, the path to a file containing a
        /// stemmer table. The program reads the file and generates the patch commands
        /// for the stems.
        /// </para>
        /// </summary>
        /// <param name="args">the path to a file containing a stemmer table</param>
        public static void Main(string[] args)
        {
            int ins = Get(0, args[0]);
            int del = Get(1, args[0]);
            int rep = Get(2, args[0]);
            int nop = Get(3, args[0]);
            // LUCENENET specific - reformatted with : and changed "charset" to "encoding"
            string charset = SystemProperties.GetProperty("egothor:stemmer:encoding", "UTF-8");
            var stemmerTables = new JCG.List<string>();

            // LUCENENET specific
            // command line argument overrides environment variable or default, if supplied
            for (int i = 1; i < args.Length; i++)
            {
                if ("-e".Equals(args[i], StringComparison.Ordinal) || "--encoding".Equals(args[i], StringComparison.Ordinal))
                {
                    charset = args[i];
                }
                else
                {
                    stemmerTables.Add(args[i]);
                }
            }

            foreach (var stemmerTable in stemmerTables)
            {
                // System.out.println("[" + args[i] + "]");
                Diff diff = new Diff(ins, del, rep, nop);

                using TextReader input = new StreamReader(new FileStream(stemmerTable, FileMode.Open, FileAccess.Read), Encoding.GetEncoding(charset));
                string line;
                while ((line = input.ReadLine()) != null)
                {
                    line = line.ToLowerInvariant();
                    using StringTokenizer st = new StringTokenizer(line);
                    if (st.MoveNext())
                    {
                        string stem = st.Current;
                        Console.WriteLine(stem + " -a");
                        while (st.MoveNext())
                        {
                            string token = st.Current;
                            if (token.Equals(stem, StringComparison.Ordinal) == false)
                            {
                                Console.WriteLine(stem + " " + diff.Exec(token, stem));
                            }
                        }
                    }
                    else // LUCENENET: st.MoveNext() will return false rather than throwing a NoSuchElementException
                    {
                        // no base token (stem) on a line
                    }
                }
            }
        }
    }
}
