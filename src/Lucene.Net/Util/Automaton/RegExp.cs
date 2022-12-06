using J2N;
using J2N.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Integer = J2N.Numerics.Int32;
using JCG = J2N.Collections.Generic;

/*
 * dk.brics.automaton
 *
 * Copyright (c) 2001-2009 Anders Moeller
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products
 *    derived from this software without specific prior written permission.
 *
 * this SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * this SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace Lucene.Net.Util.Automaton
{
    // LUCENENET specific - converted constants from RegExp
    // into a flags enum.
    [Flags]
    public enum RegExpSyntax
    {
        /// <summary>
        /// Syntax flag, enables intersection (<c>&amp;</c>).
        /// </summary>
        INTERSECTION = 0x0001,

        /// <summary>
        /// Syntax flag, enables complement (<c>~</c>).
        /// </summary>
        COMPLEMENT = 0x0002,

        /// <summary>
        /// Syntax flag, enables empty language (<c>#</c>).
        /// </summary>
        EMPTY = 0x0004,

        /// <summary>
        /// Syntax flag, enables anystring (<c>@</c>).
        /// </summary>
        ANYSTRING = 0x0008,

        /// <summary>
        /// Syntax flag, enables named automata (<c>&lt;</c>identifier<c>&gt;</c>).
        /// </summary>
        AUTOMATON = 0x0010,

        /// <summary>
        /// Syntax flag, enables numerical intervals (
        /// <c>&lt;<i>n</i>-<i>m</i>&gt;</c>).
        /// </summary>
        INTERVAL = 0x0020,

        /// <summary>
        /// Syntax flag, enables all optional regexp syntax.
        /// </summary>
        ALL = 0xffff,

        /// <summary>
        /// Syntax flag, enables no optional regexp syntax.
        /// </summary>
        NONE = 0x0000
    }


    /// <summary>
    /// Regular Expression extension to <see cref="Util.Automaton.Automaton"/>.
    /// <para/>
    /// Regular expressions are built from the following abstract syntax:
    /// <para/>
    /// <list type="table">
    ///     <item>
    ///         <term><i>regexp</i></term>
    ///         <term>::=</term>
    ///         <term><i>unionexp</i></term>
    ///         <term></term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term></term>
    ///         <term></term>
    ///         <term></term>
    ///     </item>
    ///     
    ///     <item>
    ///         <term><i>unionexp</i></term>
    ///         <term>::=</term>
    ///         <term><i>interexp</i>&#160;<tt><b>|</b></tt>&#160;<i>unionexp</i></term>
    ///         <term>(union)</term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><i>interexp</i></term>
    ///         <term></term>
    ///         <term></term>
    ///     </item>
    ///     
    ///     <item>
    ///         <term><i>interexp</i></term>
    ///         <term>::=</term>
    ///         <term><i>concatexp</i>&#160;<tt><b>&amp;</b></tt>&#160;<i>interexp</i></term>
    ///         <term>(intersection)</term>
    ///         <term><small>[OPTIONAL]</small></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><i>concatexp</i></term>
    ///         <term></term>
    ///         <term></term>
    ///     </item>
    ///     
    ///     <item>
    ///         <term><i>concatexp</i></term>
    ///         <term>::=</term>
    ///         <term><i>repeatexp</i>&#160;<i>concatexp</i></term>
    ///         <term>(concatenation)</term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><i>repeatexp</i></term>
    ///         <term></term>
    ///         <term></term>
    ///     </item>
    ///     
    ///     <item>
    ///         <term><i>repeatexp</i></term>
    ///         <term>::=</term>
    ///         <term><i>repeatexp</i>&#160;<tt><b>?</b></tt></term>
    ///         <term>(zero or one occurrence)</term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><i>repeatexp</i>&#160;<tt><b>*</b></tt></term>
    ///         <term>(zero or more occurrences)</term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><i>repeatexp</i>&#160;<tt><b>+</b></tt></term>
    ///         <term>(one or more occurrences)</term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><i>repeatexp</i>&#160;<tt><b>{</b><i>n</i><b>}</b></tt></term>
    ///         <term>(<tt><i>n</i></tt> occurrences)</term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><i>repeatexp</i>&#160;<tt><b>{</b><i>n</i><b>,}</b></tt></term>
    ///         <term>(<tt><i>n</i></tt> or more occurrences)</term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><i>repeatexp</i>&#160;<tt><b>{</b><i>n</i><b>,</b><i>m</i><b>}</b></tt></term>
    ///         <term>(<tt><i>n</i></tt> to <tt><i>m</i></tt> occurrences, including both)</term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><i>complexp</i></term>
    ///         <term></term>
    ///         <term></term>
    ///     </item>
    ///     
    ///     <item>
    ///         <term><i>complexp</i></term>
    ///         <term>::=</term>
    ///         <term><tt><b>~</b></tt>&#160;<i>complexp</i></term>
    ///         <term>(complement)</term>
    ///         <term><small>[OPTIONAL]</small></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><i>charclassexp</i></term>
    ///         <term></term>
    ///         <term></term>
    ///     </item>
    ///     
    ///     <item>
    ///         <term><i>charclassexp</i></term>
    ///         <term>::=</term>
    ///         <term><tt><b>[</b></tt>&#160;<i>charclasses</i>&#160;<tt><b>]</b></tt></term>
    ///         <term>(character class)</term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><tt><b>[^</b></tt>&#160;<i>charclasses</i>&#160;<tt><b>]</b></tt></term>
    ///         <term>(negated character class)</term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><i>simpleexp</i></term>
    ///         <term></term>
    ///         <term></term>
    ///     </item>
    ///     
    ///     <item>
    ///         <term><i>charclasses</i></term>
    ///         <term>::=</term>
    ///         <term><i>charclass</i>&#160;<i>charclasses</i></term>
    ///         <term></term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><i>charclass</i></term>
    ///         <term></term>
    ///         <term></term>
    ///     </item>
    ///     
    ///     <item>
    ///         <term><i>charclass</i></term>
    ///         <term>::=</term>
    ///         <term><i>charexp</i>&#160;<tt><b>-</b></tt>&#160;<i>charexp</i></term>
    ///         <term>(character range, including end-points)</term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><i>charexp</i></term>
    ///         <term></term>
    ///         <term></term>
    ///     </item>
    ///     
    ///     <item>
    ///         <term><i>simpleexp</i></term>
    ///         <term>::=</term>
    ///         <term><i>charexp</i></term>
    ///         <term></term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><tt><b>.</b></tt></term>
    ///         <term>(any single character)</term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><tt><b>#</b></tt></term>
    ///         <term>(the empty language)</term>
    ///         <term><small>[OPTIONAL]</small></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><tt><b>@</b></tt></term>
    ///         <term>(any string)</term>
    ///         <term><small>[OPTIONAL]</small></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><tt><b>"</b></tt>&#160;&lt;Unicode string without double-quotes&gt;&#160; <tt><b>"</b></tt></term>
    ///         <term>(a string)</term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><tt><b>(</b></tt>&#160;<tt><b>)</b></tt></term>
    ///         <term>(the empty string)</term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><tt><b>(</b></tt>&#160;<i>unionexp</i>&#160;<tt><b>)</b></tt></term>
    ///         <term>(precedence override)</term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><tt><b>&lt;</b></tt>&#160;&lt;identifier&gt;&#160;<tt><b>&gt;</b></tt></term>
    ///         <term>(named automaton)</term>
    ///         <term><small>[OPTIONAL]</small></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><tt><b>&lt;</b><i>n</i>-<i>m</i><b>&gt;</b></tt></term>
    ///         <term>(numerical interval)</term>
    ///         <term><small>[OPTIONAL]</small></term>
    ///     </item>
    ///     
    ///     <item>
    ///         <term><i>charexp</i></term>
    ///         <term>::=</term>
    ///         <term>&lt;Unicode character&gt;</term>
    ///         <term>(a single non-reserved character)</term>
    ///         <term></term>
    ///     </item>
    ///     <item>
    ///         <term></term>
    ///         <term>|</term>
    ///         <term><tt><b>\</b></tt>&#160;&lt;Unicode character&gt;&#160;</term>
    ///         <term>(a single character)</term>
    ///         <term></term>
    ///     </item>
    ///     
    /// </list>
    /// 
    /// <para/>
    /// The productions marked <small>[OPTIONAL]</small> are only allowed if
    /// specified by the syntax flags passed to the <see cref="RegExp"/> constructor.
    /// The reserved characters used in the (enabled) syntax must be escaped with
    /// backslash (<c>\</c>) or double-quotes (<c>"..."</c>). (In
    /// contrast to other regexp syntaxes, this is required also in character
    /// classes.) Be aware that dash (<c>-</c>) has a special meaning in
    /// <i>charclass</i> expressions. An identifier is a string not containing right
    /// angle bracket (<c>&gt;</c>) or dash (<c>-</c>). Numerical
    /// intervals are specified by non-negative decimal integers and include both end
    /// points, and if <c>n</c> and <c>m</c> have the same number
    /// of digits, then the conforming strings must have that length (i.e. prefixed
    /// by 0's).
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class RegExp
    {
        internal enum Kind
        {
            REGEXP_UNION,
            REGEXP_CONCATENATION,
            REGEXP_INTERSECTION,
            REGEXP_OPTIONAL,
            REGEXP_REPEAT,
            REGEXP_REPEAT_MIN,
            REGEXP_REPEAT_MINMAX,
            REGEXP_COMPLEMENT,
            REGEXP_CHAR,
            REGEXP_CHAR_RANGE,
            REGEXP_ANYCHAR,
            REGEXP_EMPTY,
            REGEXP_STRING,
            REGEXP_ANYSTRING,
            REGEXP_AUTOMATON,
            REGEXP_INTERVAL
        }

        // LUCENENET specific - made flags into their own [Flags] enum named RegExpSyntax and de-nested from this type


        private static bool allow_mutation = false;

        internal Kind kind;
        internal RegExp exp1, exp2;
        internal string s;
        internal int c;
        internal int min, max, digits;
        internal int from, to;

        internal string b;
        internal RegExpSyntax flags;
        internal int pos;

        internal RegExp()
        {
        }

        /// <summary>
        /// Constructs new <see cref="RegExp"/> from a string. Same as
        /// <c>RegExp(s, RegExpSyntax.ALL)</c>.
        /// </summary>
        /// <param name="s"> Regexp string. </param>
        /// <exception cref="ArgumentException"> If an error occured while parsing the
        ///              regular expression. </exception>
        public RegExp(string s)
            : this(s, RegExpSyntax.ALL)
        {
        }

        /// <summary>
        /// Constructs new <see cref="RegExp"/> from a string.
        /// </summary>
        /// <param name="s"> Regexp string. </param>
        /// <param name="syntax_flags"> Boolean 'or' of optional <see cref="RegExpSyntax"/> constructs to be
        ///          enabled. </param>
        /// <exception cref="ArgumentException"> If an error occured while parsing the
        ///              regular expression </exception>
        public RegExp(string s, RegExpSyntax syntax_flags)
        {
            b = s;
            flags = syntax_flags;
            RegExp e;
            if (s.Length == 0)
            {
                e = MakeString("");
            }
            else
            {
                e = ParseUnionExp();
                if (pos < b.Length)
                {
                    throw new ArgumentException("end-of-string expected at position " + pos);
                }
            }
            kind = e.kind;
            exp1 = e.exp1;
            exp2 = e.exp2;
            this.s = e.s;
            c = e.c;
            min = e.min;
            max = e.max;
            digits = e.digits;
            from = e.from;
            to = e.to;
            b = null;
        }

        /// <summary>
        /// Constructs new <see cref="Automaton"/> from this <see cref="RegExp"/>. Same
        /// as <c>ToAutomaton(null)</c> (empty automaton map).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Automaton ToAutomaton()
        {
            return ToAutomatonAllowMutate(null, null);
        }

        /// <summary>
        /// Constructs new <see cref="Automaton"/> from this <see cref="RegExp"/>. The
        /// constructed automaton is minimal and deterministic and has no transitions
        /// to dead states.
        /// </summary>
        /// <param name="automaton_provider"> Provider of automata for named identifiers. </param>
        /// <exception cref="ArgumentException"> If this regular expression uses a named
        ///              identifier that is not available from the automaton provider. </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Automaton ToAutomaton(IAutomatonProvider automaton_provider)
        {
            return ToAutomatonAllowMutate(null, automaton_provider);
        }

        /// <summary>
        /// Constructs new <see cref="Automaton"/> from this <see cref="RegExp"/>. The
        /// constructed automaton is minimal and deterministic and has no transitions
        /// to dead states.
        /// </summary>
        /// <param name="automata"> A map from automaton identifiers to automata (of type
        ///          <see cref="Automaton"/>). </param>
        /// <exception cref="ArgumentException"> If this regular expression uses a named
        ///              identifier that does not occur in the automaton map. </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Automaton ToAutomaton(IDictionary<string, Automaton> automata)
        {
            return ToAutomatonAllowMutate(automata, null);
        }

        /// <summary>
        /// Sets or resets allow mutate flag. If this flag is set, then automata
        /// construction uses mutable automata, which is slightly faster but not thread
        /// safe. By default, the flag is not set.
        /// </summary>
        /// <param name="flag"> If <c>true</c>, the flag is set </param>
        /// <returns> Previous value of the flag. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool SetAllowMutate(bool flag)
        {
            bool b = allow_mutation;
            allow_mutation = flag;
            return b;
        }

        private Automaton ToAutomatonAllowMutate(IDictionary<string, Automaton> automata, IAutomatonProvider automaton_provider)
        {
            bool b = false;
            if (allow_mutation) // thread unsafe
            {
                b = Automaton.SetAllowMutate(true);
            }
            Automaton a = ToAutomaton(automata, automaton_provider);
            if (allow_mutation)
            {
                Automaton.SetAllowMutate(b);
            }
            return a;
        }

        private Automaton ToAutomaton(IDictionary<string, Automaton> automata, IAutomatonProvider automaton_provider)
        {
            IList<Automaton> list;
            Automaton a = null;
            switch (kind)
            {
                case Kind.REGEXP_UNION:
                    list = new JCG.List<Automaton>();
                    FindLeaves(exp1, Kind.REGEXP_UNION, list, automata, automaton_provider);
                    FindLeaves(exp2, Kind.REGEXP_UNION, list, automata, automaton_provider);
                    a = BasicOperations.Union(list);
                    MinimizationOperations.Minimize(a);
                    break;

                case Kind.REGEXP_CONCATENATION:
                    list = new JCG.List<Automaton>();
                    FindLeaves(exp1, Kind.REGEXP_CONCATENATION, list, automata, automaton_provider);
                    FindLeaves(exp2, Kind.REGEXP_CONCATENATION, list, automata, automaton_provider);
                    a = BasicOperations.Concatenate(list);
                    MinimizationOperations.Minimize(a);
                    break;

                case Kind.REGEXP_INTERSECTION:
                    a = exp1.ToAutomaton(automata, automaton_provider).Intersection(exp2.ToAutomaton(automata, automaton_provider));
                    MinimizationOperations.Minimize(a);
                    break;

                case Kind.REGEXP_OPTIONAL:
                    a = exp1.ToAutomaton(automata, automaton_provider).Optional();
                    MinimizationOperations.Minimize(a);
                    break;

                case Kind.REGEXP_REPEAT:
                    a = exp1.ToAutomaton(automata, automaton_provider).Repeat();
                    MinimizationOperations.Minimize(a);
                    break;

                case Kind.REGEXP_REPEAT_MIN:
                    a = exp1.ToAutomaton(automata, automaton_provider).Repeat(min);
                    MinimizationOperations.Minimize(a);
                    break;

                case Kind.REGEXP_REPEAT_MINMAX:
                    a = exp1.ToAutomaton(automata, automaton_provider).Repeat(min, max);
                    MinimizationOperations.Minimize(a);
                    break;

                case Kind.REGEXP_COMPLEMENT:
                    a = exp1.ToAutomaton(automata, automaton_provider).Complement();
                    MinimizationOperations.Minimize(a);
                    break;

                case Kind.REGEXP_CHAR:
                    a = BasicAutomata.MakeChar(c);
                    break;

                case Kind.REGEXP_CHAR_RANGE:
                    a = BasicAutomata.MakeCharRange(from, to);
                    break;

                case Kind.REGEXP_ANYCHAR:
                    a = BasicAutomata.MakeAnyChar();
                    break;

                case Kind.REGEXP_EMPTY:
                    a = BasicAutomata.MakeEmpty();
                    break;

                case Kind.REGEXP_STRING:
                    a = BasicAutomata.MakeString(s);
                    break;

                case Kind.REGEXP_ANYSTRING:
                    a = BasicAutomata.MakeAnyString();
                    break;

                case Kind.REGEXP_AUTOMATON:
                    Automaton aa = null;
                    if (automata != null)
                    {
                        aa = automata[s];
                    }
                    if (aa is null && automaton_provider != null)
                    {
                        try
                        {
                            aa = automaton_provider.GetAutomaton(s);
                        }
                        catch (Exception e) when (e.IsIOException())
                        {
                            throw new ArgumentException(e.ToString(), e);
                        }
                    }
                    if (aa is null)
                    {
                        throw new ArgumentException("'" + s + "' not found");
                    }
                    a = (Automaton)aa.Clone(); // always clone here (ignore allow_mutate)
                    break;

                case Kind.REGEXP_INTERVAL:
                    a = BasicAutomata.MakeInterval(min, max, digits);
                    break;
            }
            return a;
        }

        private void FindLeaves(RegExp exp, Kind kind, IList<Automaton> list, IDictionary<string, Automaton> automata, IAutomatonProvider automaton_provider)
        {
            if (exp.kind == kind)
            {
                FindLeaves(exp.exp1, kind, list, automata, automaton_provider);
                FindLeaves(exp.exp2, kind, list, automata, automaton_provider);
            }
            else
            {
                list.Add(exp.ToAutomaton(automata, automaton_provider));
            }
        }

        /// <summary>
        /// Constructs string from parsed regular expression.
        /// </summary>
        public override string ToString()
        {
            return ToStringBuilder(new StringBuilder()).ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual StringBuilder ToStringBuilder(StringBuilder b)
        {
            switch (kind)
            {
                case Kind.REGEXP_UNION:
                    b.Append('(');
                    exp1.ToStringBuilder(b);
                    b.Append('|');
                    exp2.ToStringBuilder(b);
                    b.Append(')');
                    break;

                case Kind.REGEXP_CONCATENATION:
                    exp1.ToStringBuilder(b);
                    exp2.ToStringBuilder(b);
                    break;

                case Kind.REGEXP_INTERSECTION:
                    b.Append('(');
                    exp1.ToStringBuilder(b);
                    b.Append('&');
                    exp2.ToStringBuilder(b);
                    b.Append(')');
                    break;

                case Kind.REGEXP_OPTIONAL:
                    b.Append('(');
                    exp1.ToStringBuilder(b);
                    b.Append(")?");
                    break;

                case Kind.REGEXP_REPEAT:
                    b.Append('(');
                    exp1.ToStringBuilder(b);
                    b.Append(")*");
                    break;

                case Kind.REGEXP_REPEAT_MIN:
                    b.Append('(');
                    exp1.ToStringBuilder(b);
                    b.Append("){").Append(min).Append(",}");
                    break;

                case Kind.REGEXP_REPEAT_MINMAX:
                    b.Append('(');
                    exp1.ToStringBuilder(b);
                    b.Append("){").Append(min).Append(',').Append(max).Append('}');
                    break;

                case Kind.REGEXP_COMPLEMENT:
                    b.Append("~(");
                    exp1.ToStringBuilder(b);
                    b.Append(')');
                    break;

                case Kind.REGEXP_CHAR:
                    b.Append('\\').AppendCodePoint(c);
                    break;

                case Kind.REGEXP_CHAR_RANGE:
                    b.Append("[\\").AppendCodePoint(from).Append("-\\").AppendCodePoint(to).Append(']');
                    break;

                case Kind.REGEXP_ANYCHAR:
                    b.Append('.');
                    break;

                case Kind.REGEXP_EMPTY:
                    b.Append('#');
                    break;

                case Kind.REGEXP_STRING:
                    b.Append('\"').Append(s).Append('\"');
                    break;

                case Kind.REGEXP_ANYSTRING:
                    b.Append('@');
                    break;

                case Kind.REGEXP_AUTOMATON:
                    b.Append('<').Append(s).Append('>');
                    break;

                case Kind.REGEXP_INTERVAL:
                    string s1 = Convert.ToString(min, CultureInfo.InvariantCulture);
                    string s2 = Convert.ToString(max, CultureInfo.InvariantCulture);
                    b.Append('<');
                    if (digits > 0)
                    {
                        for (int i = s1.Length; i < digits; i++)
                        {
                            b.Append('0');
                        }
                    }
                    b.Append(s1).Append('-');
                    if (digits > 0)
                    {
                        for (int i = s2.Length; i < digits; i++)
                        {
                            b.Append('0');
                        }
                    }
                    b.Append(s2).Append('>');
                    break;
            }
            return b;
        }

        /// <summary>
        /// Returns set of automaton identifiers that occur in this regular expression.
        /// </summary>
        public virtual ISet<string> GetIdentifiers()
        {
            ISet<string> set = new JCG.HashSet<string>();
            GetIdentifiers(set);
            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void GetIdentifiers(ISet<string> set)
        {
            switch (kind)
            {
                case Kind.REGEXP_UNION:
                case Kind.REGEXP_CONCATENATION:
                case Kind.REGEXP_INTERSECTION:
                    exp1.GetIdentifiers(set);
                    exp2.GetIdentifiers(set);
                    break;

                case Kind.REGEXP_OPTIONAL:
                case Kind.REGEXP_REPEAT:
                case Kind.REGEXP_REPEAT_MIN:
                case Kind.REGEXP_REPEAT_MINMAX:
                case Kind.REGEXP_COMPLEMENT:
                    exp1.GetIdentifiers(set);
                    break;

                case Kind.REGEXP_AUTOMATON:
                    set.Add(s);
                    break;

                default:
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RegExp MakeUnion(RegExp exp1, RegExp exp2)
        {
            return new RegExp
            {
                kind = Kind.REGEXP_UNION,
                exp1 = exp1,
                exp2 = exp2
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RegExp MakeConcatenation(RegExp exp1, RegExp exp2)
        {
            if ((exp1.kind == Kind.REGEXP_CHAR || exp1.kind == Kind.REGEXP_STRING) && (exp2.kind == Kind.REGEXP_CHAR || exp2.kind == Kind.REGEXP_STRING))
            {
                return MakeString(exp1, exp2);
            }
            RegExp r = new RegExp
            {
                kind = Kind.REGEXP_CONCATENATION
            };
            if (exp1.kind == Kind.REGEXP_CONCATENATION && (exp1.exp2.kind == Kind.REGEXP_CHAR || exp1.exp2.kind == Kind.REGEXP_STRING) && (exp2.kind == Kind.REGEXP_CHAR || exp2.kind == Kind.REGEXP_STRING))
            {
                r.exp1 = exp1.exp1;
                r.exp2 = MakeString(exp1.exp2, exp2);
            }
            else if ((exp1.kind == Kind.REGEXP_CHAR || exp1.kind == Kind.REGEXP_STRING) && exp2.kind == Kind.REGEXP_CONCATENATION && (exp2.exp1.kind == Kind.REGEXP_CHAR || exp2.exp1.kind == Kind.REGEXP_STRING))
            {
                r.exp1 = MakeString(exp1, exp2.exp1);
                r.exp2 = exp2.exp2;
            }
            else
            {
                r.exp1 = exp1;
                r.exp2 = exp2;
            }
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static RegExp MakeString(RegExp exp1, RegExp exp2)
        {
            StringBuilder b = new StringBuilder();
            if (exp1.kind == Kind.REGEXP_STRING)
            {
                b.Append(exp1.s);
            }
            else
            {
                b.AppendCodePoint(exp1.c);
            }
            if (exp2.kind == Kind.REGEXP_STRING)
            {
                b.Append(exp2.s);
            }
            else
            {
                b.AppendCodePoint(exp2.c);
            }
            return MakeString(b.ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RegExp MakeIntersection(RegExp exp1, RegExp exp2)
        {
            return new RegExp
            {
                kind = Kind.REGEXP_INTERSECTION,
                exp1 = exp1,
                exp2 = exp2
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RegExp MakeOptional(RegExp exp)
        {
            return new RegExp
            {
                kind = Kind.REGEXP_OPTIONAL,
                exp1 = exp
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RegExp MakeRepeat(RegExp exp)
        {
            return new RegExp
            {
                kind = Kind.REGEXP_REPEAT,
                exp1 = exp
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RegExp MakeRepeat(RegExp exp, int min)
        {
            return new RegExp
            {
                kind = Kind.REGEXP_REPEAT_MIN,
                exp1 = exp,
                min = min
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RegExp MakeRepeat(RegExp exp, int min, int max)
        {
            return new RegExp
            {
                kind = Kind.REGEXP_REPEAT_MINMAX,
                exp1 = exp,
                min = min,
                max = max
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RegExp MakeComplement(RegExp exp)
        {
            return new RegExp
            {
                kind = Kind.REGEXP_COMPLEMENT,
                exp1 = exp
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RegExp MakeChar(int c)
        {
            return new RegExp
            {
                kind = Kind.REGEXP_CHAR,
                c = c
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RegExp MakeCharRange(int from, int to)
        {
            if (from > to)
            {
                throw new ArgumentException($"invalid range: from ({from}) cannot be > to ({to})");
            }
            return new RegExp
            {
                kind = Kind.REGEXP_CHAR_RANGE,
                from = from,
                to = to
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RegExp MakeAnyChar()
        {
            return new RegExp
            {
                kind = Kind.REGEXP_ANYCHAR
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RegExp MakeEmpty()
        {
            return new RegExp
            {
                kind = Kind.REGEXP_EMPTY
            };
        }

        internal static RegExp MakeString(string s)
        {
            return new RegExp
            {
                kind = Kind.REGEXP_STRING,
                s = s
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RegExp MakeAnyString()
        {
            return new RegExp
            {
                kind = Kind.REGEXP_ANYSTRING
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RegExp MakeAutomaton(string s)
        {
            return new RegExp
            {
                kind = Kind.REGEXP_AUTOMATON,
                s = s
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RegExp MakeInterval(int min, int max, int digits)
        {
            return new RegExp
            {
                kind = Kind.REGEXP_INTERVAL,
                min = min,
                max = max,
                digits = digits
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Peek(string s)
        {
            return More() && s.IndexOf(b.CodePointAt(pos)) != -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Match(int c)
        {
            if (pos >= b.Length)
            {
                return false;
            }
            if (b.CodePointAt(pos) == c)
            {
                pos += Character.CharCount(c);
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool More()
        {
            return pos < b.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Next()
        {
            if (!More())
            {
                throw new ArgumentException("unexpected end-of-string");
            }
            int ch = b.CodePointAt(pos);
            pos += Character.CharCount(ch);
            return ch;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Check(RegExpSyntax flag)
        {
            return (flags & flag) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RegExp ParseUnionExp()
        {
            RegExp e = ParseInterExp();
            if (Match('|'))
            {
                e = MakeUnion(e, ParseUnionExp());
            }
            return e;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RegExp ParseInterExp()
        {
            RegExp e = ParseConcatExp();
            if (Check(RegExpSyntax.INTERSECTION) && Match('&'))
            {
                e = MakeIntersection(e, ParseInterExp());
            }
            return e;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RegExp ParseConcatExp()
        {
            RegExp e = ParseRepeatExp();
            if (More() && !Peek(")|") && (!Check(RegExpSyntax.INTERSECTION) || !Peek("&")))
            {
                e = MakeConcatenation(e, ParseConcatExp());
            }
            return e;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RegExp ParseRepeatExp()
        {
            RegExp e = ParseComplExp();
            while (Peek("?*+{"))
            {
                if (Match('?'))
                {
                    e = MakeOptional(e);
                }
                else if (Match('*'))
                {
                    e = MakeRepeat(e);
                }
                else if (Match('+'))
                {
                    e = MakeRepeat(e, 1);
                }
                else if (Match('{'))
                {
                    int start = pos;
                    while (Peek("0123456789"))
                    {
                        Next();
                    }
                    if (start == pos)
                    {
                        throw new ArgumentException("integer expected at position " + pos);
                    }
                    // LUCENENET: Optimized so we don't allocate a substring during the parse
                    int n = Integer.Parse(b, start, pos - start, radix: 10);
                    int m = -1;
                    if (Match(','))
                    {
                        start = pos;
                        while (Peek("0123456789"))
                        {
                            Next();
                        }
                        if (start != pos)
                        {
                            // LUCENENET: Optimized so we don't allocate a substring during the parse
                            m = Integer.Parse(b, start, pos - start, radix: 10);
                        }
                    }
                    else
                    {
                        m = n;
                    }
                    if (!Match('}'))
                    {
                        throw new ArgumentException("expected '}' at position " + pos);
                    }
                    if (m == -1)
                    {
                        e = MakeRepeat(e, n);
                    }
                    else
                    {
                        e = MakeRepeat(e, n, m);
                    }
                }
            }
            return e;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RegExp ParseComplExp()
        {
            if (Check(RegExpSyntax.COMPLEMENT) && Match('~'))
            {
                return MakeComplement(ParseComplExp());
            }
            else
            {
                return ParseCharClassExp();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RegExp ParseCharClassExp()
        {
            if (Match('['))
            {
                bool negate = false;
                if (Match('^'))
                {
                    negate = true;
                }
                RegExp e = ParseCharClasses();
                if (negate)
                {
                    e = MakeIntersection(MakeAnyChar(), MakeComplement(e));
                }
                if (!Match(']'))
                {
                    throw new ArgumentException("expected ']' at position " + pos);
                }
                return e;
            }
            else
            {
                return ParseSimpleExp();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RegExp ParseCharClasses()
        {
            RegExp e = ParseCharClass();
            while (More() && !Peek("]"))
            {
                e = MakeUnion(e, ParseCharClass());
            }
            return e;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RegExp ParseCharClass()
        {
            int c = ParseCharExp();
            if (Match('-'))
            {
                return MakeCharRange(c, ParseCharExp());
            }
            else
            {
                return MakeChar(c);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RegExp ParseSimpleExp()
        {
            if (Match('.'))
            {
                return MakeAnyChar();
            }
            else if (Check(RegExpSyntax.EMPTY) && Match('#'))
            {
                return MakeEmpty();
            }
            else if (Check(RegExpSyntax.ANYSTRING) && Match('@'))
            {
                return MakeAnyString();
            }
            else if (Match('"'))
            {
                int start = pos;
                while (More() && !Peek("\""))
                {
                    Next();
                }
                if (!Match('"'))
                {
                    throw new ArgumentException("expected '\"' at position " + pos);
                }
                return MakeString(b.Substring(start, pos - 1 - start));
            }
            else if (Match('('))
            {
                if (Match(')'))
                {
                    return MakeString("");
                }
                RegExp e = ParseUnionExp();
                if (!Match(')'))
                {
                    throw new ArgumentException("expected ')' at position " + pos);
                }
                return e;
            }
            else if ((Check(RegExpSyntax.AUTOMATON) || Check(RegExpSyntax.INTERVAL)) && Match('<'))
            {
                int start = pos;
                while (More() && !Peek(">"))
                {
                    Next();
                }
                if (!Match('>'))
                {
                    throw new ArgumentException("expected '>' at position " + pos);
                }
                string s = b.Substring(start, pos - 1 - start);
                int i = s.IndexOf('-');
                if (i == -1)
                {
                    if (!Check(RegExpSyntax.AUTOMATON))
                    {
                        throw new ArgumentException("interval syntax error at position " + (pos - 1));
                    }
                    return MakeAutomaton(s);
                }
                else
                {
                    if (!Check(RegExpSyntax.INTERVAL))
                    {
                        throw new ArgumentException("illegal identifier at position " + (pos - 1));
                    }

                    // LUCENENET: Refactored so we don't throw exceptions in the normal flow
                    if (i == 0 || i == s.Length - 1 || i != s.LastIndexOf('-'))
                    {
                        throw new ArgumentException("interval syntax error at position " + (pos - 1));
                    }
                    string smin = s.Substring(0, i);
                    string smax = s.Substring(i + 1, s.Length - (i + 1));

                    if (!int.TryParse(smin, NumberStyles.Integer, CultureInfo.InvariantCulture, out int imin) ||
                        !int.TryParse(smax, NumberStyles.Integer, CultureInfo.InvariantCulture, out int imax))
                        throw new ArgumentException("interval syntax error at position " + (pos - 1));

                    int digits;
                    if (smin.Length == smax.Length)
                    {
                        digits = smin.Length;
                    }
                    else
                    {
                        digits = 0;
                    }
                    if (imin > imax)
                    {
                        int t = imin;
                        imin = imax;
                        imax = t;
                    }
                    return MakeInterval(imin, imax, digits);
                }
            }
            else
            {
                return MakeChar(ParseCharExp());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int ParseCharExp()
        {
            Match('\\');
            return Next();
        }
    }
}