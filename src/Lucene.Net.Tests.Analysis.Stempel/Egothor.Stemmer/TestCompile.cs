/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using J2N.IO;
using J2N.Text;
using Lucene.Net;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
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
    public class TestCompile_ : LuceneTestCase
    {
        [Test]
        public void TestCompile()
        {
            DirectoryInfo dir = CreateTempDir("testCompile");
            dir.Create();
            FileInfo output;

            using (Stream input = GetType().getResourceAsStream("testRules.txt"))
            {
                output = new FileInfo(Path.Combine(dir.FullName, "testRules.txt"));
                Copy(input, output);
            }
            string path = output.FullName;
            Compile.Main(new string[] {"test", path });
            string compiled = path + ".out";
            Trie trie = LoadTrie(compiled);
            AssertTrie(trie, path, true, true);
            AssertTrie(trie, path, false, true);
            new FileInfo(compiled).Delete();
        }

        [Test]
        public void TestCompileBackwards()
        {
            DirectoryInfo dir = CreateTempDir("testCompile");
            dir.Create();
            FileInfo output;
            using (Stream input = GetType().getResourceAsStream("testRules.txt"))
            {
                output = new FileInfo(Path.Combine(dir.FullName, "testRules.txt"));
                Copy(input, output);
            }
            string path = output.FullName;
            Compile.Main(new string[] { "-test", path });
            string compiled = path + ".out";
            Trie trie = LoadTrie(compiled);
            AssertTrie(trie, path, true, true);
            AssertTrie(trie, path, false, true);
            new FileInfo(compiled).Delete();
        }

        [Test]
        public void TestCompileMulti()
        {
            DirectoryInfo dir = CreateTempDir("testCompile");
            dir.Create();
            FileInfo output;
            using (Stream input = GetType().getResourceAsStream("testRules.txt"))
            {
                output = new FileInfo(Path.Combine(dir.FullName, "testRules.txt"));
                Copy(input, output);
            }
            string path = output.FullName;
            Compile.Main(new string[] { "Mtest", path });
            string compiled = path + ".out";
            Trie trie = LoadTrie(compiled);
            AssertTrie(trie, path, true, true);
            AssertTrie(trie, path, false, true);
            new FileInfo(compiled).Delete();
        }

        internal static Trie LoadTrie(string path)
        {
            Trie trie;
            using (DataInputStream @is = new DataInputStream(
                new FileStream(path, FileMode.Open, FileAccess.Read)))
            {
                string method = @is.ReadUTF().ToUpperInvariant();
                if (method.IndexOf('M') < 0)
                {
                    trie = new Trie(@is);
                }
                else
                {
                    trie = new MultiTrie(@is);
                }
            }
            return trie;
        }

        private static void AssertTrie(Trie trie, string file, bool usefull,
            bool storeorig)
        {
            using TextReader @in = new StreamReader(new FileStream(file, FileMode.Open), Encoding.UTF8);
            for (string line = @in.ReadLine(); line != null; line = @in.ReadLine())
            {
                line = line.ToLowerInvariant();
                using StringTokenizer st = new StringTokenizer(line);
                if (st.MoveNext())
                {
                    string stem = st.Current;
                    if (storeorig)
                    {
                        string cmd = (usefull) ? trie.GetFully(stem) : trie
                            .GetLastOnPath(stem);
                        StringBuilder stm = new StringBuilder(stem);
                        Diff.Apply(stm, cmd);
                        assertEquals(stem.ToLowerInvariant(), stm.ToString().ToLowerInvariant());
                    }
                    while (st.MoveNext())
                    {
                        string token = st.Current;
                        if (token.Equals(stem, StringComparison.Ordinal))
                        {
                            continue;
                        }
                        string cmd = (usefull) ? trie.GetFully(token) : trie
                            .GetLastOnPath(token);
                        StringBuilder stm = new StringBuilder(token);
                        Diff.Apply(stm, cmd);
                        assertEquals(stem.ToLowerInvariant(), stm.ToString().ToLowerInvariant());
                    }
                }
                else // LUCENENET: st.MoveNext() will return false rather than throwing a NoSuchElementException
                {
                    // no base token (stem) on a line
                }
            }
        }

        private static void Copy(Stream input, FileInfo output)
        {
            FileStream os = new FileStream(output.FullName, FileMode.OpenOrCreate, FileAccess.Write);
            try
            {
                byte[] buffer = new byte[1024];
                int len;
                while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    os.Write(buffer, 0, len);
                }
            }
            finally
            {
                os.Dispose();
            }
        }
    }
}
