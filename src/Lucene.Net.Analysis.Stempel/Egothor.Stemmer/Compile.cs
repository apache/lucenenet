using J2N.IO;
using J2N.Text;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;
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
    /// The Compile class is used to compile a stemmer table.
    /// </summary>
    public static class Compile // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        private static bool backward;
        private static bool multi;
        private static Trie trie;

        /// <summary>
        /// Entry point to the Compile application.
        /// <para/>
        /// This program takes any number of arguments: the first is the name of the
        /// desired stemming algorithm to use (a list is available in the package
        /// description) , all of the rest should be the path or paths to a file or
        /// files containing a stemmer table to compile.
        /// </summary>
        /// <param name="args">the command line arguments</param>
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                return;
            }

            // LUCENENET NOTE: This line does nothing in .NET
            // and also does nothing in Java...what?
            //args[0].ToUpperInvariant();

            // Reads the first char of the first arg
            backward = args[0][0] == '-';
            int qq = (backward) ? 1 : 0;
            bool storeorig = false;

            if (args[0][qq] == '0')
            {
                storeorig = true;
                qq++;
            }

            multi = args[0][qq] == 'M';
            if (multi)
            {
                qq++;
            }
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

            char[] optimizer = new char[args[0].Length - qq];
            for (int i = 0; i < optimizer.Length; i++)
            {
                optimizer[i] = args[0][qq + i];
            }

            foreach (var stemmerTable in stemmerTables)
            {
                // System.out.println("[" + args[i] + "]");
                Diff diff = new Diff();
                //int stems = 0; // not used
                int words = 0;


                AllocTrie();

                Console.WriteLine(stemmerTable);
                using (TextReader input = new StreamReader(
                    new FileStream(stemmerTable, FileMode.Open, FileAccess.Read), Encoding.GetEncoding(charset)))
                {
                    string line;
                    while ((line = input.ReadLine()) != null)
                    {
                        line = line.ToLowerInvariant();
                        using StringTokenizer st = new StringTokenizer(line);
                        if (st.MoveNext())
                        {
                            string stem = st.Current;
                            if (storeorig)
                            {
                                trie.Add(stem, "-a");
                                words++;
                            }
                            while (st.MoveNext())
                            {
                                string token = st.Current;
                                if (token.Equals(stem, StringComparison.Ordinal) == false)
                                {
                                    trie.Add(token, diff.Exec(token, stem));
                                    words++;
                                }
                            }
                        }
                        else // LUCENENET: st.MoveNext() will return false rather than throwing a NoSuchElementException
                        {
                            // no base token (stem) on a line
                        }
                    }
                }

                Optimizer o = new Optimizer();
                Optimizer2 o2 = new Optimizer2();
                Lift l = new Lift(true);
                Lift e = new Lift(false);
                Gener g = new Gener();

                for (int j = 0; j < optimizer.Length; j++)
                {
                    string prefix;
                    switch (optimizer[j])
                    {
                        case 'G':
                            trie = trie.Reduce(g);
                            prefix = "G: ";
                            break;
                        case 'L':
                            trie = trie.Reduce(l);
                            prefix = "L: ";
                            break;
                        case 'E':
                            trie = trie.Reduce(e);
                            prefix = "E: ";
                            break;
                        case '2':
                            trie = trie.Reduce(o2);
                            prefix = "2: ";
                            break;
                        case '1':
                            trie = trie.Reduce(o);
                            prefix = "1: ";
                            break;
                        default:
                            continue;
                    }
                    trie.PrintInfo(Console.Out, prefix + " ");
                }

                using DataOutputStream os = new DataOutputStream(
                    new FileStream(stemmerTable + ".out", FileMode.OpenOrCreate, FileAccess.Write));
                os.WriteUTF(args[0]);
                trie.Store(os);
            }
        }

        internal static void AllocTrie()
        {
            if (multi)
            {
                trie = new MultiTrie2(!backward);
            }
            else
            {
                trie = new Trie(!backward);
            }
        }
    }
}
