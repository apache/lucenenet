using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;

namespace Lucene.Net.Test.Util.Automaton
{
    [TestFixture]
    public class TestLevenshteinAutomata : LuceneTestCase
    {
        [Test]
        public void TestLev0()
        {
            AssertLev("", 0);
            AssertCharVectors(0);
        }

        [Test]
        public void TestLev1()
        {
            AssertLev("", 1);
            AssertCharVectors(1);
        }

        [Test]
        public void TestLev2()
        {
            AssertLev("", 2);
            AssertCharVectors(2);
        }

        // LUCENE-3094
        [Test]
        public void TestNoWastedStates()
        {
            AutomatonTestUtil.assertNoDetachedStates(new LevenshteinAutomata("abc", false).ToAutomaton(1));
        }

        /** 
         * Tests all possible characteristic vectors for some n
         * This exhaustively tests the parametric transitions tables.
         */
        private void AssertCharVectors(int n)
        {
            var k = 2 * n + 1;
            // use k + 2 as the exponent: the formula generates different transitions
            // for w, w-1, w-2
            var limit = (int)Math.Pow(2, k + 2);
            for (var i = 0; i < limit; i++)
            {
                String encoded = Integer.toString(i, 2);
                AssertLev(encoded, n);
            }
        }

        /**
         * Builds a DFA for some string, and checks all Lev automata
         * up to some maximum distance.
         */
        private void AssertLev(String s, int maxDistance)
        {
            var builder = new LevenshteinAutomata(s, false);
            var tbuilder = new LevenshteinAutomata(s, true);
            var automata = new Lucene.Net.Util.Automaton.Automaton[maxDistance + 1];
            var tautomata = new Lucene.Net.Util.Automaton.Automaton[maxDistance + 1];
            for (var n = 0; n < automata.Length; n++)
            {
                automata[n] = builder.ToAutomaton(n);
                tautomata[n] = tbuilder.ToAutomaton(n);
                Assert.IsNotNull(automata[n]);
                Assert.IsNotNull(tautomata[n]);
                Assert.IsTrue(automata[n].Deterministic);
                Assert.IsTrue(tautomata[n].Deterministic);
                Assert.IsTrue(SpecialOperations.IsFinite(automata[n]));
                Assert.IsTrue(SpecialOperations.IsFinite(tautomata[n]));
                AutomatonTestUtil.assertNoDetachedStates(automata[n]);
                AutomatonTestUtil.assertNoDetachedStates(tautomata[n]);
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

        private Lucene.Net.Util.Automaton.Automaton NaiveLev1(String s)
        {
            var a = BasicAutomata.MakeString(s);
            a = BasicOperations.Union(a, InsertionsOf(s));
            MinimizationOperations.Minimize(a);
            a = BasicOperations.Union(a, DeletionsOf(s));
            MinimizationOperations.Minimize(a);
            a = BasicOperations.Union(a, SubstitutionsOf(s));
            MinimizationOperations.Minimize(a);

            return a;
        }

        private Lucene.Net.Util.Automaton.Automaton NaiveLev1T(String s)
        {
            var a = NaiveLev1(s);
            a = BasicOperations.Union(a, TranspositionsOf(s));
            MinimizationOperations.Minimize(a);
            return a;
        }

        private Lucene.Net.Util.Automaton.Automaton InsertionsOf(String s)
        {
            var list = new List<Lucene.Net.Util.Automaton.Automaton>();

            for (var i = 0; i <= s.Length; i++)
            {
                var a = BasicAutomata.MakeString(s.Substring(0, i));
                a = BasicOperations.Concatenate(a, BasicAutomata.MakeAnyChar());
                a = BasicOperations.Concatenate(a, BasicAutomata.MakeString(s
                    .Substring(i)));
                list.Add(a);
            }

            var automaton = BasicOperations.Union(list);
            MinimizationOperations.Minimize(automaton);
            return automaton;
        }

        private Lucene.Net.Util.Automaton.Automaton DeletionsOf(String s)
        {
            var list = new List<Lucene.Net.Util.Automaton.Automaton>();

            for (var i = 0; i < s.Length; i++)
            {
                var a = BasicAutomata.MakeString(s.Substring(0, i));
                a = BasicOperations.Concatenate(a, BasicAutomata.MakeString(s
                    .Substring(i + 1)));
                a.ExpandSingleton();
                list.Add(a);
            }

            var automaton = BasicOperations.Union(list);
            MinimizationOperations.Minimize(automaton);
            return automaton;
        }

        private Lucene.Net.Util.Automaton.Automaton SubstitutionsOf(String s)
        {
            var list = new List<Lucene.Net.Util.Automaton.Automaton>();

            for (var i = 0; i < s.Length; i++)
            {
                var a = BasicAutomata.MakeString(s.Substring(0, i));
                a = BasicOperations.Concatenate(a, BasicAutomata.MakeAnyChar());
                a = BasicOperations.Concatenate(a, BasicAutomata.MakeString(s
                    .Substring(i + 1)));
                list.Add(a);
            }

            var automaton = BasicOperations.Union(list);
            MinimizationOperations.Minimize(automaton);
            return automaton;
        }

        private Lucene.Net.Util.Automaton.Automaton TranspositionsOf(String s)
        {
            if (s.Length < 2)
                return BasicAutomata.MakeEmpty();
            var list = new List<Lucene.Net.Util.Automaton.Automaton>();
            for (var i = 0; i < s.Length - 1; i++)
            {
                var sb = new StringBuilder();
                sb.Append(s.Substring(0, i));
                sb.Append(s[i + 1]);
                sb.Append(s[i]);
                sb.Append(s.Substring(i + 2, s.Length));
                var st = sb.ToString();
                if (!st.Equals(s))
                    list.Add(BasicAutomata.MakeString(st));
            }
            var a = BasicOperations.Union(list);
            MinimizationOperations.Minimize(a);
            return a;
        }

        private void AssertBruteForce(String input, Lucene.Net.Util.Automaton.Automaton dfa, int distance)
        {
            var ra = new CharacterRunAutomaton(dfa);
            var maxLen = input.Length + distance + 1;
            var maxNum = (int)Math.Pow(2, maxLen);
            for (var i = 0; i < maxNum; i++)
            {
                String encoded = Integer.toString(i, 2);
                var accepts = ra.Run(encoded);
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

        private void AssertBruteForceT(String input, Lucene.Net.Util.Automaton.Automaton dfa, int distance)
        {
            var ra = new CharacterRunAutomaton(dfa);
            var maxLen = input.Length + distance + 1;
            var maxNum = (int)Math.Pow(2, maxLen);
            for (var i = 0; i < maxNum; i++)
            {
                String encoded = Integer.toString(i, 2);
                var accepts = ra.Run(encoded);
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
        private int GetDistance(String target, String other)
        {
            char[] sa;
            int n;
            int[] p; //'previous' cost array, horizontally
            int[] d; // cost array, horizontally
            int[] _d; //placeholder to assist in swapping p and d

            /*
               The difference between this impl. and the previous is that, rather
               than creating and retaining a matrix of size s.length()+1 by t.length()+1,
               we maintain two single-dimensional arrays of length s.length()+1.  The first, d,
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

            var m = other.Length;
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

        private int GetTDistance(String target, String other)
        {
            char[] sa;
            int n;
            int[,] d; // cost array

            sa = target.ToCharArray();
            n = sa.Length;
            int m = other.Length;
            d = new int[n + 1, m + 1];

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
                d[i,0] = i;
            }

            for (j = 0; j <= m; j++)
            {
                d[0,j] = j;
            }

            for (j = 1; j <= m; j++)
            {
                t_j = other[j - 1];

                for (i = 1; i <= n; i++)
                {
                    cost = sa[i - 1] == t_j ? 0 : 1;
                    // minimum of cell to the left+1, to the top+1, diagonally left and up +cost
                    d[i,j] = Math.Min(Math.Min(d[i - 1,j] + 1, d[i,j - 1] + 1), d[i - 1,j - 1] + cost);
                    // transposition
                    if (i > 1 && j > 1 && target[i - 1] == other[j - 2] && target[i - 2] == other[j - 1])
                    {
                        d[i,j] = Math.Min(d[i,j], d[i - 2,j - 2] + cost);
                    }
                }
            }

            // our last action in the above loop was to switch d and p, so p now
            // actually has the most recent cost counts
            return Math.Abs(d[n,m]);
        }
    }
}
