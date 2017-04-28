using Lucene.Net.Util;
using NUnit.Framework;

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
    public class TestStemmer : LuceneTestCase
    {
        [Test]
        public void TestTrie()
        {
            Trie t = new Trie(true);

            string[] keys = { "a", "ba", "bb", "c" };
            string[] vals = { "1", "2", "2", "4" };

            for (int i = 0; i < keys.Length; i++)
            {
                t.Add(keys[i], vals[i]);
            }

            assertEquals(0, t.root);
            assertEquals(2, t.rows.Count);
            assertEquals(3, t.cmds.Count);
            AssertTrieContents(t, keys, vals);
        }

        [Test]
        public void TestTrieBackwards()
        {
            Trie t = new Trie(false);

            string[] keys = { "a", "ba", "bb", "c" };
            string[] vals = { "1", "2", "2", "4" };

            for (int i = 0; i < keys.Length; i++)
            {
                t.Add(keys[i], vals[i]);
            }

            AssertTrieContents(t, keys, vals);
        }

        [Test]
        public void TestMultiTrie()
        {
            Trie t = new MultiTrie(true);

            string[] keys = { "a", "ba", "bb", "c" };
            string[] vals = { "1", "2", "2", "4" };

            for (int i = 0; i < keys.Length; i++)
            {
                t.Add(keys[i], vals[i]);
            }

            AssertTrieContents(t, keys, vals);
        }

        [Test]
        public void TestMultiTrieBackwards()
        {
            Trie t = new MultiTrie(false);

            string[] keys = { "a", "ba", "bb", "c" };
            string[] vals = { "1", "2", "2", "4" };

            for (int i = 0; i < keys.Length; i++)
            {
                t.Add(keys[i], vals[i]);
            }

            AssertTrieContents(t, keys, vals);
        }

        [Test]
        public void TestMultiTrie2()
        {
            Trie t = new MultiTrie2(true);

            string[] keys = { "a", "ba", "bb", "c" };
            /* 
             * short vals won't work, see line 155 for example
             * the IOOBE is caught (wierd), but shouldnt affect patch cmds?
             */
            string[] vals = { "1111", "2222", "2223", "4444" };

            for (int i = 0; i < keys.Length; i++)
            {
                t.Add(keys[i], vals[i]);
            }

            AssertTrieContents(t, keys, vals);
        }

        [Test]
        public void TestMultiTrie2Backwards()
        {
            Trie t = new MultiTrie2(false);

            string[] keys = { "a", "ba", "bb", "c" };
            /* 
             * short vals won't work, see line 155 for example
             * the IOOBE is caught (wierd), but shouldnt affect patch cmds?
             */
            string[] vals = { "1111", "2222", "2223", "4444" };

            for (int i = 0; i < keys.Length; i++)
            {
                t.Add(keys[i], vals[i]);
            }

            AssertTrieContents(t, keys, vals);
        }

        private static void AssertTrieContents(Trie trie, string[] keys, string[] vals)
        {
            Trie[] tries = new Trie[] {
                trie,
                trie.Reduce(new Optimizer()),
                trie.Reduce(new Optimizer2()),
                trie.Reduce(new Gener()),
                trie.Reduce(new Lift(true)),
                trie.Reduce(new Lift(false))
            };

            foreach (Trie t in tries)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    assertEquals(vals[i], t.GetFully(keys[i]).ToString());
                    assertEquals(vals[i], t.GetLastOnPath(keys[i]).ToString());
                }
            }
        }
    }
}
