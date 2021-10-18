using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Util.Automaton
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    [TestFixture]
    public class TestLevenshteinAutomata : LuceneTestCase
    {
        [Test]
        public virtual void TestLev0()
        {
            AssertLev("", 0);
            AssertCharVectors(0);
        }

        [Test]
        public virtual void TestLev1()
        {
            AssertLev("", 1);
            AssertCharVectors(1);
        }

        [Test]
        public virtual void TestLev2()
        {
            AssertLev("", 2);
            AssertCharVectors(2);
        }

        // LUCENE-3094
        [Test]
        public virtual void TestNoWastedStates()
        {
            AutomatonTestUtil.AssertNoDetachedStates((new LevenshteinAutomata("abc", false)).ToAutomaton(1));
        }

        /// <summary>
        /// Tests all possible characteristic vectors for some n
        /// this exhaustively tests the parametric transitions tables.
        /// </summary>
        private void AssertCharVectors(int n)
        {
            int k = 2 * n + 1;
            // use k + 2 as the exponent: the formula generates different transitions
            // for w, w-1, w-2
            int limit = (int)Math.Pow(2, k + 2);
            for (int i = 0; i < limit; i++)
            {
                string encoded = Convert.ToString(i, 2);
                AssertLev(encoded, n);
            }
        }

        /// <summary>
        /// Builds a DFA for some string, and checks all Lev automata
        /// up to some maximum distance.
        /// </summary>
        private void AssertLev(string s, int maxDistance)
        {
            LevenshteinAutomata builder = new LevenshteinAutomata(s, false);
            LevenshteinAutomata tbuilder = new LevenshteinAutomata(s, true);
            Automaton[] automata = new Automaton[maxDistance + 1];
            Automaton[] tautomata = new Automaton[maxDistance + 1];
            for (int n = 0; n < automata.Length; n++)
            {
                automata[n] = builder.ToAutomaton(n);
                tautomata[n] = tbuilder.ToAutomaton(n);
                Assert.IsNotNull(automata[n]);
                Assert.IsNotNull(tautomata[n]);
                Assert.IsTrue(automata[n].IsDeterministic);
                Assert.IsTrue(tautomata[n].IsDeterministic);
                Assert.IsTrue(SpecialOperations.IsFinite(automata[n]));
                Assert.IsTrue(SpecialOperations.IsFinite(tautomata[n]));
                AutomatonTestUtil.AssertNoDetachedStates(automata[n]);
                AutomatonTestUtil.AssertNoDetachedStates(tautomata[n]);
                // check that the dfa for n-1 accepts a subset of the dfa for n
                if (n > 0)
                {
                    Assert.IsTrue(automata[n - 1].SubsetOf(automata[n]));
                    Assert.IsTrue(automata[n - 1].SubsetOf(tautomata[n]));
                    Assert.IsTrue(tautomata[n - 1].SubsetOf(automata[n]));
                    Assert.IsTrue(tautomata[n - 1].SubsetOf(tautomata[n]));
                    Assert.AreNotSame(automata[n - 1], automata[n]);
                }
                // check that Lev(N) is a subset of LevT(N)
                Assert.IsTrue(automata[n].SubsetOf(tautomata[n]));
                // special checks for specific n
                switch (n)
                {
                    case 0:
                        // easy, matches the string itself
                        Assert.IsTrue(BasicOperations.SameLanguage(BasicAutomata.MakeString(s), automata[0]));
                        Assert.IsTrue(BasicOperations.SameLanguage(BasicAutomata.MakeString(s), tautomata[0]));
                        break;

                    case 1:
                        // generate a lev1 naively, and check the accepted lang is the same.
                        Assert.IsTrue(BasicOperations.SameLanguage(NaiveLev1(s), automata[1]));
                        Assert.IsTrue(BasicOperations.SameLanguage(NaiveLev1T(s), tautomata[1]));
                        break;

                    default:
                        AssertBruteForce(s, automata[n], n);
                        AssertBruteForceT(s, tautomata[n], n);
                        break;
                }
            }
        }

        /// <summary>
        /// Return an automaton that accepts all 1-character insertions, deletions, and
        /// substitutions of s.
        /// </summary>
        private Automaton NaiveLev1(string s)
        {
            Automaton a = BasicAutomata.MakeString(s);
            a = BasicOperations.Union(a, InsertionsOf(s));
            MinimizationOperations.Minimize(a);
            a = BasicOperations.Union(a, DeletionsOf(s));
            MinimizationOperations.Minimize(a);
            a = BasicOperations.Union(a, SubstitutionsOf(s));
            MinimizationOperations.Minimize(a);

            return a;
        }

        /// <summary>
        /// Return an automaton that accepts all 1-character insertions, deletions,
        /// substitutions, and transpositions of s.
        /// </summary>
        private Automaton NaiveLev1T(string s)
        {
            Automaton a = NaiveLev1(s);
            a = BasicOperations.Union(a, TranspositionsOf(s));
            MinimizationOperations.Minimize(a);
            return a;
        }

        /// <summary>
        /// Return an automaton that accepts all 1-character insertions of s (inserting
        /// one character)
        /// </summary>
        private Automaton InsertionsOf(string s)
        {
            IList<Automaton> list = new JCG.List<Automaton>();

            for (int i = 0; i <= s.Length; i++)
            {
                Automaton au = BasicAutomata.MakeString(s.Substring(0, i));
                au = BasicOperations.Concatenate(au, BasicAutomata.MakeAnyChar());
                au = BasicOperations.Concatenate(au, BasicAutomata.MakeString(s.Substring(i)));
                list.Add(au);
            }

            Automaton a = BasicOperations.Union(list);
            MinimizationOperations.Minimize(a);
            return a;
        }

        /// <summary>
        /// Return an automaton that accepts all 1-character deletions of s (deleting
        /// one character).
        /// </summary>
        private Automaton DeletionsOf(string s)
        {
            IList<Automaton> list = new JCG.List<Automaton>();

            for (int i = 0; i < s.Length; i++)
            {
                Automaton au = BasicAutomata.MakeString(s.Substring(0, i));
                au = BasicOperations.Concatenate(au, BasicAutomata.MakeString(s.Substring(i + 1)));
                au.ExpandSingleton();
                list.Add(au);
            }

            Automaton a = BasicOperations.Union(list);
            MinimizationOperations.Minimize(a);
            return a;
        }

        /// <summary>
        /// Return an automaton that accepts all 1-character substitutions of s
        /// (replacing one character)
        /// </summary>
        private Automaton SubstitutionsOf(string s)
        {
            IList<Automaton> list = new JCG.List<Automaton>();

            for (int i = 0; i < s.Length; i++)
            {
                Automaton au = BasicAutomata.MakeString(s.Substring(0, i));
                au = BasicOperations.Concatenate(au, BasicAutomata.MakeAnyChar());
                au = BasicOperations.Concatenate(au, BasicAutomata.MakeString(s.Substring(i + 1)));
                list.Add(au);
            }

            Automaton a = BasicOperations.Union(list);
            MinimizationOperations.Minimize(a);
            return a;
        }

        /// <summary>
        /// Return an automaton that accepts all transpositions of s
        /// (transposing two adjacent characters)
        /// </summary>
        private Automaton TranspositionsOf(string s)
        {
            if (s.Length < 2)
            {
                return BasicAutomata.MakeEmpty();
            }
            IList<Automaton> list = new JCG.List<Automaton>();
            for (int i = 0; i < s.Length - 1; i++)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(s.Substring(0, i));
                sb.Append(s[i + 1]);
                sb.Append(s[i]);
                sb.Append(s.Substring(i + 2, s.Length - (i + 2)));
                string st = sb.ToString();
                if (!st.Equals(s, StringComparison.Ordinal))
                {
                    list.Add(BasicAutomata.MakeString(st));
                }
            }
            Automaton a = BasicOperations.Union(list);
            MinimizationOperations.Minimize(a);
            return a;
        }

        private void AssertBruteForce(string input, Automaton dfa, int distance)
        {
            CharacterRunAutomaton ra = new CharacterRunAutomaton(dfa);
            int maxLen = input.Length + distance + 1;
            int maxNum = (int)Math.Pow(2, maxLen);
            for (int i = 0; i < maxNum; i++)
            {
                string encoded = Convert.ToString(i, 2);
                bool accepts = ra.Run(encoded);
                if (accepts)
                {
                    Assert.IsTrue(GetDistance(input, encoded) <= distance);
                }
                else
                {
                    Assert.IsTrue(GetDistance(input, encoded) > distance);
                }
            }
        }

        private void AssertBruteForceT(string input, Automaton dfa, int distance)
        {
            CharacterRunAutomaton ra = new CharacterRunAutomaton(dfa);
            int maxLen = input.Length + distance + 1;
            int maxNum = (int)Math.Pow(2, maxLen);
            for (int i = 0; i < maxNum; i++)
            {
                string encoded = Convert.ToString(i, 2);
                bool accepts = ra.Run(encoded);
                if (accepts)
                {
                    Assert.IsTrue(GetTDistance(input, encoded) <= distance);
                }
                else
                {
                    Assert.IsTrue(GetTDistance(input, encoded) > distance);
                }
            }
        }

        //*****************************
        // Compute Levenshtein distance: see org.apache.commons.lang.StringUtils#getLevenshteinDistance(String, String)
        //*****************************
        private int GetDistance(string target, string other)
        {
            char[] sa;
            int n;
            int[] p; //'previous' cost array, horizontally
            int[] d; // cost array, horizontally
            int[] _d; //placeholder to assist in swapping p and d

            /*
               The difference between this impl. and the previous is that, rather
               than creating and retaining a matrix of size s.Length()+1 by t.Length()+1,
               we maintain two single-dimensional arrays of length s.Length()+1.  The first, d,
               is the 'current working' distance array that maintains the newest distance cost
               counts as we iterate through the characters of String s.  Each time we increment
               the index of String t we are comparing, d is copied to p, the second int[].  Doing so
               allows us to retain the previous cost counts as required by the algorithm (taking
               the minimum of the cost count to the left, up one, and diagonally up and to the left
               of the current cost count being calculated).  (Note that the arrays aren't really
               copied anymore, just switched...this is clearly much better than cloning an array
               or doing a System.arraycopy() each time  through the outer loop.)

               Effectively, the difference between the two implementations is this one does not
               cause an out of memory condition when calculating the LD over two very large strings.
             */

            sa = target.ToCharArray();
            n = sa.Length;
            p = new int[n + 1];
            d = new int[n + 1];

            int m = other.Length;
            if (n == 0 || m == 0)
            {
                if (n == m)
                {
                    return 0;
                }
                else
                {
                    return Math.Max(n, m);
                }
            }

            // indexes into strings s and t
            int i; // iterates through s
            int j; // iterates through t

            char t_j; // jth character of t

            int cost; // cost

            for (i = 0; i <= n; i++)
            {
                p[i] = i;
            }

            for (j = 1; j <= m; j++)
            {
                t_j = other[j - 1];
                d[0] = j;

                for (i = 1; i <= n; i++)
                {
                    cost = sa[i - 1] == t_j ? 0 : 1;
                    // minimum of cell to the left+1, to the top+1, diagonally left and up +cost
                    d[i] = Math.Min(Math.Min(d[i - 1] + 1, p[i] + 1), p[i - 1] + cost);
                }

                // copy current distance counts to 'previous row' distance counts
                _d = p;
                p = d;
                d = _d;
            }

            // our last action in the above loop was to switch d and p, so p now
            // actually has the most recent cost counts
            return Math.Abs(p[n]);
        }

        private int GetTDistance(string target, string other)
        {
            char[] sa;
            int n;
            int[][] d; // cost array

            sa = target.ToCharArray();
            n = sa.Length;
            int m = other.Length;
            d = RectangularArrays.ReturnRectangularArray<int>(n + 1, m + 1);

            if (n == 0 || m == 0)
            {
                if (n == m)
                {
                    return 0;
                }
                else
                {
                    return Math.Max(n, m);
                }
            }

            // indexes into strings s and t
            int i; // iterates through s
            int j; // iterates through t

            char t_j; // jth character of t

            int cost; // cost

            for (i = 0; i <= n; i++)
            {
                d[i][0] = i;
            }

            for (j = 0; j <= m; j++)
            {
                d[0][j] = j;
            }

            for (j = 1; j <= m; j++)
            {
                t_j = other[j - 1];

                for (i = 1; i <= n; i++)
                {
                    cost = sa[i - 1] == t_j ? 0 : 1;
                    // minimum of cell to the left+1, to the top+1, diagonally left and up +cost
                    d[i][j] = Math.Min(Math.Min(d[i - 1][j] + 1, d[i][j - 1] + 1), d[i - 1][j - 1] + cost);
                    // transposition
                    if (i > 1 && j > 1 && target[i - 1] == other[j - 2] && target[i - 2] == other[j - 1])
                    {
                        d[i][j] = Math.Min(d[i][j], d[i - 2][j - 2] + cost);
                    }
                }
            }

            // our last action in the above loop was to switch d and p, so p now
            // actually has the most recent cost counts
            return Math.Abs(d[n][m]);
        }
    }
}