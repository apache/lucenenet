using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Automaton
{
    public class RegExp
    {
        public enum Kind
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

        /**
        * Syntax flag, enables intersection (<tt>&amp;</tt>).
        */
        public const int INTERSECTION = 0x0001;

        /**
         * Syntax flag, enables complement (<tt>~</tt>).
         */
        public const int COMPLEMENT = 0x0002;

        /**
         * Syntax flag, enables empty language (<tt>#</tt>).
         */
        public const int EMPTY = 0x0004;

        /**
         * Syntax flag, enables anystring (<tt>@</tt>).
         */
        public const int ANYSTRING = 0x0008;

        /**
         * Syntax flag, enables named automata (<tt>&lt;</tt>identifier<tt>&gt;</tt>).
         */
        public const int AUTOMATON = 0x0010;

        /**
         * Syntax flag, enables numerical intervals (
         * <tt>&lt;<i>n</i>-<i>m</i>&gt;</tt>).
         */
        public const int INTERVAL = 0x0020;

        /**
         * Syntax flag, enables all optional regexp syntax.
         */
        public const int ALL = 0xffff;

        /**
         * Syntax flag, enables no optional regexp syntax.
         */
        public const int NONE = 0x0000;

        private static bool allow_mutation = false;

        internal Kind kind;
        internal RegExp exp1, exp2;
        internal String s;
        internal int c;
        internal int min, max, digits;
        internal int from, to;

        internal String b;
        internal int flags;
        internal int pos;

        internal RegExp()
        {
        }

        public RegExp(String s)
            : this(s, ALL)
        {
        }

        public RegExp(String s, int syntax_flags)
        {
            b = s;
            flags = syntax_flags;
            RegExp e;
            if (s.Length == 0) e = MakeString("");
            else
            {
                e = ParseUnionExp();
                if (pos < b.Length) throw new ArgumentException(
                    "end-of-string expected at position " + pos);
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

        public Automaton ToAutomaton()
        {
            return ToAutomatonAllowMutate(null, null);
        }

        public Automaton ToAutomaton(IAutomatonProvider automaton_provider)
        {
            return ToAutomatonAllowMutate(null, automaton_provider);
        }

        public Automaton ToAutomaton(IDictionary<String, Automaton> automata)
        {
            return ToAutomatonAllowMutate(automata, null);
        }

        public bool SetAllowMutate(bool flag)
        {
            bool b = allow_mutation;
            allow_mutation = flag;
            return b;
        }

        private Automaton ToAutomatonAllowMutate(IDictionary<String, Automaton> automata,
            IAutomatonProvider automaton_provider)
        {
            bool b = false;
            if (allow_mutation) b = Automaton.SetAllowMutate(true); // thread unsafe
            Automaton a = ToAutomaton(automata, automaton_provider);
            if (allow_mutation) Automaton.SetAllowMutate(b);
            return a;
        }

        private Automaton ToAutomaton(IDictionary<String, Automaton> automata,
            IAutomatonProvider automaton_provider)
        {
            List<Automaton> list;
            Automaton a = null;
            switch (kind)
            {
                case Kind.REGEXP_UNION:
                    list = new List<Automaton>();
                    FindLeaves(exp1, Kind.REGEXP_UNION, list, automata, automaton_provider);
                    FindLeaves(exp2, Kind.REGEXP_UNION, list, automata, automaton_provider);
                    a = BasicOperations.Union(list);
                    MinimizationOperations.Minimize(a);
                    break;
                case Kind.REGEXP_CONCATENATION:
                    list = new List<Automaton>();
                    FindLeaves(exp1, Kind.REGEXP_CONCATENATION, list, automata,
                        automaton_provider);
                    FindLeaves(exp2, Kind.REGEXP_CONCATENATION, list, automata,
                        automaton_provider);
                    a = BasicOperations.Concatenate(list);
                    MinimizationOperations.Minimize(a);
                    break;
                case Kind.REGEXP_INTERSECTION:
                    a = exp1.ToAutomaton(automata, automaton_provider).Intersection(
                        exp2.ToAutomaton(automata, automaton_provider));
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
                    if (automata != null) aa = automata[s];
                    if (aa == null && automaton_provider != null)
                        aa = automaton_provider.GetAutomaton(s);
                    if (aa == null) throw new ArgumentException("'" + s
                        + "' not found");
                    a = (Automaton)aa.Clone(); // always clone here (ignore allow_mutate)
                    break;
                case Kind.REGEXP_INTERVAL:
                    a = BasicAutomata.MakeInterval(min, max, digits);
                    break;
            }
            return a;
        }

        private void FindLeaves(RegExp exp, Kind kind, IList<Automaton> list,
            IDictionary<String, Automaton> automata, IAutomatonProvider automaton_provider)
        {
            if (exp.kind == kind)
            {
                FindLeaves(exp.exp1, kind, list, automata, automaton_provider);
                FindLeaves(exp.exp2, kind, list, automata, automaton_provider);
            }
            else list.Add(exp.ToAutomaton(automata, automaton_provider));
        }

        public override string ToString()
        {
            return ToStringBuilder(new StringBuilder()).ToString();
        }

        internal StringBuilder ToStringBuilder(StringBuilder b)
        {
            switch (kind)
            {
                case Kind.REGEXP_UNION:
                    b.Append("(");
                    exp1.ToStringBuilder(b);
                    b.Append("|");
                    exp2.ToStringBuilder(b);
                    b.Append(")");
                    break;
                case Kind.REGEXP_CONCATENATION:
                    exp1.ToStringBuilder(b);
                    exp2.ToStringBuilder(b);
                    break;
                case Kind.REGEXP_INTERSECTION:
                    b.Append("(");
                    exp1.ToStringBuilder(b);
                    b.Append("&");
                    exp2.ToStringBuilder(b);
                    b.Append(")");
                    break;
                case Kind.REGEXP_OPTIONAL:
                    b.Append("(");
                    exp1.ToStringBuilder(b);
                    b.Append(")?");
                    break;
                case Kind.REGEXP_REPEAT:
                    b.Append("(");
                    exp1.ToStringBuilder(b);
                    b.Append(")*");
                    break;
                case Kind.REGEXP_REPEAT_MIN:
                    b.Append("(");
                    exp1.ToStringBuilder(b);
                    b.Append("){").Append(min).Append(",}");
                    break;
                case Kind.REGEXP_REPEAT_MINMAX:
                    b.Append("(");
                    exp1.ToStringBuilder(b);
                    b.Append("){").Append(min).Append(",").Append(max).Append("}");
                    break;
                case Kind.REGEXP_COMPLEMENT:
                    b.Append("~(");
                    exp1.ToStringBuilder(b);
                    b.Append(")");
                    break;
                case Kind.REGEXP_CHAR:
                    b.Append("\\").Append((char)c);
                    break;
                case Kind.REGEXP_CHAR_RANGE:
                    b.Append("[\\").Append((char)from).Append("-\\").Append((char)to).Append("]");
                    break;
                case Kind.REGEXP_ANYCHAR:
                    b.Append(".");
                    break;
                case Kind.REGEXP_EMPTY:
                    b.Append("#");
                    break;
                case Kind.REGEXP_STRING:
                    b.Append("\"").Append(s).Append("\"");
                    break;
                case Kind.REGEXP_ANYSTRING:
                    b.Append("@");
                    break;
                case Kind.REGEXP_AUTOMATON:
                    b.Append("<").Append(s).Append(">");
                    break;
                case Kind.REGEXP_INTERVAL:
                    String s1 = min.ToString();
                    String s2 = max.ToString();
                    b.Append("<");
                    if (digits > 0) for (int i = s1.Length; i < digits; i++)
                            b.Append('0');
                    b.Append(s1).Append("-");
                    if (digits > 0) for (int i = s2.Length; i < digits; i++)
                            b.Append('0');
                    b.Append(s2).Append(">");
                    break;
            }
            return b;
        }

        public ISet<String> GetIdentifiers()
        {
            HashSet<String> set = new HashSet<String>();
            GetIdentifiers(set);
            return set;
        }

        internal void GetIdentifiers(ISet<String> set)
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

        internal static RegExp MakeUnion(RegExp exp1, RegExp exp2)
        {
            RegExp r = new RegExp();
            r.kind = Kind.REGEXP_UNION;
            r.exp1 = exp1;
            r.exp2 = exp2;
            return r;
        }

        internal static RegExp MakeConcatenation(RegExp exp1, RegExp exp2)
        {
            if ((exp1.kind == Kind.REGEXP_CHAR || exp1.kind == Kind.REGEXP_STRING)
                && (exp2.kind == Kind.REGEXP_CHAR || exp2.kind == Kind.REGEXP_STRING)) return MakeString(
                exp1, exp2);
            RegExp r = new RegExp();
            r.kind = Kind.REGEXP_CONCATENATION;
            if (exp1.kind == Kind.REGEXP_CONCATENATION
                && (exp1.exp2.kind == Kind.REGEXP_CHAR || exp1.exp2.kind == Kind.REGEXP_STRING)
                && (exp2.kind == Kind.REGEXP_CHAR || exp2.kind == Kind.REGEXP_STRING))
            {
                r.exp1 = exp1.exp1;
                r.exp2 = MakeString(exp1.exp2, exp2);
            }
            else if ((exp1.kind == Kind.REGEXP_CHAR || exp1.kind == Kind.REGEXP_STRING)
              && exp2.kind == Kind.REGEXP_CONCATENATION
              && (exp2.exp1.kind == Kind.REGEXP_CHAR || exp2.exp1.kind == Kind.REGEXP_STRING))
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

        internal static RegExp MakeString(RegExp exp1, RegExp exp2)
        {
            StringBuilder b = new StringBuilder();
            if (exp1.kind == Kind.REGEXP_STRING) b.Append(exp1.s);
            else b.Append((char)exp1.c);
            if (exp2.kind == Kind.REGEXP_STRING) b.Append(exp2.s);
            else b.Append((char)exp2.c);
            return MakeString(b.ToString());
        }

        internal static RegExp MakeIntersection(RegExp exp1, RegExp exp2)
        {
            RegExp r = new RegExp();
            r.kind = Kind.REGEXP_INTERSECTION;
            r.exp1 = exp1;
            r.exp2 = exp2;
            return r;
        }

        internal static RegExp MakeOptional(RegExp exp)
        {
            RegExp r = new RegExp();
            r.kind = Kind.REGEXP_OPTIONAL;
            r.exp1 = exp;
            return r;
        }

        internal static RegExp MakeRepeat(RegExp exp)
        {
            RegExp r = new RegExp();
            r.kind = Kind.REGEXP_REPEAT;
            r.exp1 = exp;
            return r;
        }

        internal static RegExp MakeRepeat(RegExp exp, int min)
        {
            RegExp r = new RegExp();
            r.kind = Kind.REGEXP_REPEAT_MIN;
            r.exp1 = exp;
            r.min = min;
            return r;
        }

        internal static RegExp MakeRepeat(RegExp exp, int min, int max)
        {
            RegExp r = new RegExp();
            r.kind = Kind.REGEXP_REPEAT_MINMAX;
            r.exp1 = exp;
            r.min = min;
            r.max = max;
            return r;
        }

        internal static RegExp MakeComplement(RegExp exp)
        {
            RegExp r = new RegExp();
            r.kind = Kind.REGEXP_COMPLEMENT;
            r.exp1 = exp;
            return r;
        }

        internal static RegExp MakeChar(int c)
        {
            RegExp r = new RegExp();
            r.kind = Kind.REGEXP_CHAR;
            r.c = c;
            return r;
        }

        internal static RegExp MakeCharRange(int from, int to)
        {
            if (from > to)
                throw new ArgumentException("invalid range: from (" + from + ") cannot be > to (" + to + ")");
            RegExp r = new RegExp();
            r.kind = Kind.REGEXP_CHAR_RANGE;
            r.from = from;
            r.to = to;
            return r;
        }

        internal static RegExp MakeAnyChar()
        {
            RegExp r = new RegExp();
            r.kind = Kind.REGEXP_ANYCHAR;
            return r;
        }

        internal static RegExp MakeEmpty()
        {
            RegExp r = new RegExp();
            r.kind = Kind.REGEXP_EMPTY;
            return r;
        }

        internal static RegExp MakeString(String s)
        {
            RegExp r = new RegExp();
            r.kind = Kind.REGEXP_STRING;
            r.s = s;
            return r;
        }

        internal static RegExp MakeAnyString()
        {
            RegExp r = new RegExp();
            r.kind = Kind.REGEXP_ANYSTRING;
            return r;
        }

        internal static RegExp MakeAutomaton(String s)
        {
            RegExp r = new RegExp();
            r.kind = Kind.REGEXP_AUTOMATON;
            r.s = s;
            return r;
        }

        internal static RegExp MakeInterval(int min, int max, int digits)
        {
            RegExp r = new RegExp();
            r.kind = Kind.REGEXP_INTERVAL;
            r.min = min;
            r.max = max;
            r.digits = digits;
            return r;
        }

        private bool Peek(String s)
        {
            return More() && s.IndexOf(b[pos]) != -1;
        }

        private bool Match(int c)
        {
            if (pos >= b.Length) return false;
            if (b[pos] == c)
            {
                pos += 1;
                return true;
            }
            return false;
        }

        private bool More()
        {
            return pos < b.Length;
        }

        private int Next()
        {
            if (!More()) throw new ArgumentException("unexpected end-of-string");
            int ch = b[pos];
            pos += 1;
            return ch;
        }

        private bool Check(int flag)
        {
            return (flags & flag) != 0;
        }

        internal RegExp ParseUnionExp()
        {
            RegExp e = ParseInterExp();
            if (Match('|')) e = MakeUnion(e, ParseUnionExp());
            return e;
        }

        internal RegExp ParseInterExp()
        {
            RegExp e = ParseConcatExp();
            if (Check(INTERSECTION) && Match('&')) e = MakeIntersection(e,
                ParseInterExp());
            return e;
        }

        internal RegExp ParseConcatExp()
        {
            RegExp e = ParseRepeatExp();
            if (More() && !Peek(")|") && (!Check(INTERSECTION) || !Peek("&"))) e = MakeConcatenation(
                e, ParseConcatExp());
            return e;
        }

        internal RegExp ParseRepeatExp()
        {
            RegExp e = ParseComplExp();
            while (Peek("?*+{"))
            {
                if (Match('?')) e = MakeOptional(e);
                else if (Match('*')) e = MakeRepeat(e);
                else if (Match('+')) e = MakeRepeat(e, 1);
                else if (Match('{'))
                {
                    int start = pos;
                    while (Peek("0123456789"))
                        Next();
                    if (start == pos) throw new ArgumentException(
                        "integer expected at position " + pos);
                    int n = int.Parse(b.Substring(start, pos));
                    int m = -1;
                    if (Match(','))
                    {
                        start = pos;
                        while (Peek("0123456789"))
                            Next();
                        if (start != pos) m = int.Parse(b.Substring(start, pos));
                    }
                    else m = n;
                    if (!Match('}')) throw new ArgumentException(
                        "expected '}' at position " + pos);
                    if (m == -1) e = MakeRepeat(e, n);
                    else e = MakeRepeat(e, n, m);
                }
            }
            return e;
        }

        internal RegExp ParseComplExp()
        {
            if (Check(COMPLEMENT) && Match('~')) return MakeComplement(ParseComplExp());
            else return ParseCharClassExp();
        }

        internal RegExp ParseCharClassExp()
        {
            if (Match('['))
            {
                bool negate = false;
                if (Match('^')) negate = true;
                RegExp e = ParseCharClasses();
                if (negate) e = MakeIntersection(MakeAnyChar(), MakeComplement(e));
                if (!Match(']')) throw new ArgumentException(
                    "expected ']' at position " + pos);
                return e;
            }
            else return ParseSimpleExp();
        }

        internal RegExp ParseCharClasses()
        {
            RegExp e = ParseCharClass();
            while (More() && !Peek("]"))
                e = MakeUnion(e, ParseCharClass());
            return e;
        }

        internal RegExp ParseCharClass()
        {
            int c = ParseCharExp();
            if (Match('-')) return MakeCharRange(c, ParseCharExp());
            else return MakeChar(c);
        }

        internal RegExp ParseSimpleExp()
        {
            if (Match('.')) return MakeAnyChar();
            else if (Check(EMPTY) && Match('#')) return MakeEmpty();
            else if (Check(ANYSTRING) && Match('@')) return MakeAnyString();
            else if (Match('"'))
            {
                int start = pos;
                while (More() && !Peek("\""))
                    Next();
                if (!Match('"')) throw new ArgumentException(
                    "expected '\"' at position " + pos);
                return MakeString(b.Substring(start, pos - 1));
            }
            else if (Match('('))
            {
                if (Match(')')) return MakeString("");
                RegExp e = ParseUnionExp();
                if (!Match(')')) throw new ArgumentException(
                    "expected ')' at position " + pos);
                return e;
            }
            else if ((Check(AUTOMATON) || Check(INTERVAL)) && Match('<'))
            {
                int start = pos;
                while (More() && !Peek(">"))
                    Next();
                if (!Match('>')) throw new ArgumentException(
                    "expected '>' at position " + pos);
                String s = b.Substring(start, pos - 1);
                int i = s.IndexOf('-');
                if (i == -1)
                {
                    if (!Check(AUTOMATON)) throw new ArgumentException(
                        "interval syntax error at position " + (pos - 1));
                    return MakeAutomaton(s);
                }
                else
                {
                    if (!Check(INTERVAL)) throw new ArgumentException(
                        "illegal identifier at position " + (pos - 1));
                    try
                    {
                        if (i == 0 || i == s.Length - 1 || i != s.LastIndexOf('-')) throw new FormatException();
                        String smin = s.Substring(0, i);
                        String smax = s.Substring(i + 1, s.Length);
                        int imin = int.Parse(smin);
                        int imax = int.Parse(smax);
                        int digits;
                        if (smin.Length == smax.Length) digits = smin.Length;
                        else digits = 0;
                        if (imin > imax)
                        {
                            int t = imin;
                            imin = imax;
                            imax = t;
                        }
                        return MakeInterval(imin, imax, digits);
                    }
                    catch (FormatException)
                    {
                        throw new ArgumentException(
                            "interval syntax error at position " + (pos - 1));
                    }
                }
            }
            else return MakeChar(ParseCharExp());
        }

        internal int ParseCharExp()
        {
            Match('\\');
            return Next();
        }
    }
}
