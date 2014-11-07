using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace org.apache.lucene.analysis.pt
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


	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;
	using Version = org.apache.lucene.util.Version;

	using org.apache.lucene.analysis.util;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.apache.lucene.analysis.util.StemmerUtil.*;

	/// <summary>
	/// Base class for stemmers that use a set of RSLP-like stemming steps.
	/// <para>
	/// RSLP (Removedor de Sufixos da Lingua Portuguesa) is an algorithm designed
	/// originally for stemming the Portuguese language, described in the paper
	/// <i>A Stemming Algorithm for the Portuguese Language</i>, Orengo et. al.
	/// </para>
	/// <para>
	/// Since this time a plural-only modification (RSLP-S) as well as a modification
	/// for the Galician language have been implemented. This class parses a configuration
	/// file that describes <seealso cref="Step"/>s, where each Step contains a set of <seealso cref="Rule"/>s.
	/// </para>
	/// <para>
	/// The general rule format is: 
	/// <blockquote>{ "suffix", N, "replacement", { "exception1", "exception2", ...}}</blockquote>
	/// where:
	/// <ul>
	///   <li><code>suffix</code> is the suffix to be removed (such as "inho").
	///   <li><code>N</code> is the min stem size, where stem is defined as the candidate stem 
	///       after removing the suffix (but before appending the replacement!)
	///   <li><code>replacement</code> is an optimal string to append after removing the suffix.
	///       This can be the empty string.
	///   <li><code>exceptions</code> is an optional list of exceptions, patterns that should 
	///       not be stemmed. These patterns can be specified as whole word or suffix (ends-with) 
	///       patterns, depending upon the exceptions format flag in the step header.
	/// </ul>
	/// </para>
	/// <para>
	/// A step is an ordered list of rules, with a structure in this format:
	/// <blockquote>{ "name", N, B, { "cond1", "cond2", ... }
	///               ... rules ... };
	/// </blockquote>
	/// where:
	/// <ul>
	///   <li><code>name</code> is a name for the step (such as "Plural").
	///   <li><code>N</code> is the min word size. Words that are less than this length bypass
	///       the step completely, as an optimization. Note: N can be zero, in this case this 
	///       implementation will automatically calculate the appropriate value from the underlying 
	///       rules.
	///   <li><code>B</code> is a "boolean" flag specifying how exceptions in the rules are matched.
	///       A value of 1 indicates whole-word pattern matching, a value of 0 indicates that 
	///       exceptions are actually suffixes and should be matched with ends-with.
	///   <li><code>conds</code> are an optional list of conditions to enter the step at all. If
	///       the list is non-empty, then a word must end with one of these conditions or it will
	///       bypass the step completely as an optimization.
	/// </ul>
	/// </para>
	/// <para>
	/// </para>
	/// </summary>
	/// <seealso cref= <a href="http://www.inf.ufrgs.br/~viviane/rslp/index.htm">RSLP description</a>
	/// @lucene.internal </seealso>
	public abstract class RSLPStemmerBase
	{

	  /// <summary>
	  /// A basic rule, with no exceptions.
	  /// </summary>
	  protected internal class Rule
	  {
		protected internal readonly char[] suffix;
		protected internal readonly char[] replacement;
		protected internal readonly int min;

		/// <summary>
		/// Create a rule. </summary>
		/// <param name="suffix"> suffix to remove </param>
		/// <param name="min"> minimum stem length </param>
		/// <param name="replacement"> replacement string </param>
		public Rule(string suffix, int min, string replacement)
		{
		  this.suffix = suffix.ToCharArray();
		  this.replacement = replacement.ToCharArray();
		  this.min = min;
		}

		/// <returns> true if the word matches this rule. </returns>
		public virtual bool matches(char[] s, int len)
		{
		  return (len - suffix.Length >= min && StemmerUtil.EndsWith(s, len, suffix));
		}

		/// <returns> new valid length of the string after firing this rule. </returns>
		public virtual int replace(char[] s, int len)
		{
		  if (replacement.Length > 0)
		  {
			Array.Copy(replacement, 0, s, len - suffix.Length, replacement.Length);
		  }
		  return len - suffix.Length + replacement.Length;
		}
	  }

	  /// <summary>
	  /// A rule with a set of whole-word exceptions.
	  /// </summary>
	  protected internal class RuleWithSetExceptions : Rule
	  {
		protected internal readonly CharArraySet exceptions;

		public RuleWithSetExceptions(string suffix, int min, string replacement, string[] exceptions) : base(suffix, min, replacement)
		{
		  for (int i = 0; i < exceptions.Length; i++)
		  {
			if (!exceptions[i].EndsWith(suffix, StringComparison.Ordinal))
			{
			  throw new Exception("useless exception '" + exceptions[i] + "' does not end with '" + suffix + "'");
			}
		  }
		  this.exceptions = new CharArraySet(Version.LUCENE_CURRENT, Arrays.asList(exceptions), false);
		}

		public override bool matches(char[] s, int len)
		{
		  return base.matches(s, len) && !exceptions.contains(s, 0, len);
		}
	  }

	  /// <summary>
	  /// A rule with a set of exceptional suffixes.
	  /// </summary>
	  protected internal class RuleWithSuffixExceptions : Rule
	  {
		// TODO: use a more efficient datastructure: automaton?
		protected internal readonly char[][] exceptions;

		public RuleWithSuffixExceptions(string suffix, int min, string replacement, string[] exceptions) : base(suffix, min, replacement)
		{
		  for (int i = 0; i < exceptions.Length; i++)
		  {
			if (!exceptions[i].EndsWith(suffix, StringComparison.Ordinal))
			{
			  throw new Exception("warning: useless exception '" + exceptions[i] + "' does not end with '" + suffix + "'");
			}
		  }
		  this.exceptions = new char[exceptions.Length][];
		  for (int i = 0; i < exceptions.Length; i++)
		  {
			this.exceptions[i] = exceptions[i].ToCharArray();
		  }
		}

		public override bool matches(char[] s, int len)
		{
		  if (!base.matches(s, len))
		  {
			return false;
		  }

		  for (int i = 0; i < exceptions.Length; i++)
		  {
			if (StemmerUtil.EndsWith(s, len, exceptions[i]))
			{
			  return false;
			}
		  }

		  return true;
		}
	  }

	  /// <summary>
	  /// A step containing a list of rules.
	  /// </summary>
	  protected internal class Step
	  {
		protected internal readonly string name;
		protected internal readonly Rule[] rules;
		protected internal readonly int min;
		protected internal readonly char[][] suffixes;

		/// <summary>
		/// Create a new step </summary>
		/// <param name="name"> Step's name. </param>
		/// <param name="rules"> an ordered list of rules. </param>
		/// <param name="min"> minimum word size. if this is 0 it is automatically calculated. </param>
		/// <param name="suffixes"> optional list of conditional suffixes. may be null. </param>
		public Step(string name, Rule[] rules, int min, string[] suffixes)
		{
		  this.name = name;
		  this.rules = rules;
		  if (min == 0)
		  {
			min = int.MaxValue;
			foreach (Rule r in rules)
			{
			  min = Math.Min(min, r.min + r.suffix.Length);
			}
		  }
		  this.min = min;

		  if (suffixes == null || suffixes.Length == 0)
		  {
			this.suffixes = null;
		  }
		  else
		  {
			this.suffixes = new char[suffixes.Length][];
			for (int i = 0; i < suffixes.Length; i++)
			{
			  this.suffixes[i] = suffixes[i].ToCharArray();
			}
		  }
		}

		/// <returns> new valid length of the string after applying the entire step. </returns>
		public virtual int apply(char[] s, int len)
		{
		  if (len < min)
		  {
			return len;
		  }

		  if (suffixes != null)
		  {
			bool found = false;

			for (int i = 0; i < suffixes.Length; i++)
			{
			  if (StemmerUtil.EndsWith(s, len, suffixes[i]))
			  {
				found = true;
				break;
			  }
			}

			if (!found)
			{
				return len;
			}
		  }

		  for (int i = 0; i < rules.Length; i++)
		  {
			if (rules[i].matches(s, len))
			{
			  return rules[i].replace(s, len);
			}
		  }

		  return len;
		}
	  }

	  /// <summary>
	  /// Parse a resource file into an RSLP stemmer description. </summary>
	  /// <returns> a Map containing the named Steps in this description. </returns>
	  protected internal static IDictionary<string, Step> parse(Type clazz, string resource)
	  {
		// TODO: this parser is ugly, but works. use a jflex grammar instead.
		try
		{
		  InputStream @is = clazz.getResourceAsStream(resource);
		  LineNumberReader r = new LineNumberReader(new InputStreamReader(@is, StandardCharsets.UTF_8));
		  IDictionary<string, Step> steps = new Dictionary<string, Step>();
		  string step;
		  while ((step = readLine(r)) != null)
		  {
			Step s = parseStep(r, step);
			steps[s.name] = s;
		  }
		  r.close();
		  return steps;
		}
		catch (IOException e)
		{
		  throw new Exception(e);
		}
	  }

	  private static readonly Pattern headerPattern = Pattern.compile("^\\{\\s*\"([^\"]*)\",\\s*([0-9]+),\\s*(0|1),\\s*\\{(.*)\\},\\s*$");
	  private static readonly Pattern stripPattern = Pattern.compile("^\\{\\s*\"([^\"]*)\",\\s*([0-9]+)\\s*\\}\\s*(,|(\\}\\s*;))$");
	  private static readonly Pattern repPattern = Pattern.compile("^\\{\\s*\"([^\"]*)\",\\s*([0-9]+),\\s*\"([^\"]*)\"\\}\\s*(,|(\\}\\s*;))$");
	  private static readonly Pattern excPattern = Pattern.compile("^\\{\\s*\"([^\"]*)\",\\s*([0-9]+),\\s*\"([^\"]*)\",\\s*\\{(.*)\\}\\s*\\}\\s*(,|(\\}\\s*;))$");

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private static Step parseStep(java.io.LineNumberReader r, String header) throws java.io.IOException
	  private static Step parseStep(LineNumberReader r, string header)
	  {
		Matcher matcher = headerPattern.matcher(header);
		if (!matcher.find())
		{
		  throw new Exception("Illegal Step header specified at line " + r.LineNumber);
		}
		Debug.Assert(matcher.groupCount() == 4);
		string name = matcher.group(1);
		int min = int.Parse(matcher.group(2));
		int type = int.Parse(matcher.group(3));
		string[] suffixes = parseList(matcher.group(4));
		Rule[] rules = parseRules(r, type);
		return new Step(name, rules, min, suffixes);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private static Rule[] parseRules(java.io.LineNumberReader r, int type) throws java.io.IOException
	  private static Rule[] parseRules(LineNumberReader r, int type)
	  {
		IList<Rule> rules = new List<Rule>();
		string line;
		while ((line = readLine(r)) != null)
		{
		  Matcher matcher = stripPattern.matcher(line);
		  if (matcher.matches())
		  {
			rules.Add(new Rule(matcher.group(1), int.Parse(matcher.group(2)), ""));
		  }
		  else
		  {
			matcher = repPattern.matcher(line);
			if (matcher.matches())
			{
			  rules.Add(new Rule(matcher.group(1), int.Parse(matcher.group(2)), matcher.group(3)));
			}
			else
			{
			  matcher = excPattern.matcher(line);
			  if (matcher.matches())
			  {
				if (type == 0)
				{
				  rules.Add(new RuleWithSuffixExceptions(matcher.group(1), int.Parse(matcher.group(2)), matcher.group(3), parseList(matcher.group(4))));
				}
				else
				{
				  rules.Add(new RuleWithSetExceptions(matcher.group(1), int.Parse(matcher.group(2)), matcher.group(3), parseList(matcher.group(4))));
				}
			  }
			  else
			  {
				throw new Exception("Illegal Step rule specified at line " + r.LineNumber);
			  }
			}
		  }
		  if (line.EndsWith(";", StringComparison.Ordinal))
		  {
			return rules.ToArray();
		  }
		}
		return null;
	  }

	  private static string[] parseList(string s)
	  {
		if (s.Length == 0)
		{
		  return null;
		}
		string[] list = s.Split(",", true);
		for (int i = 0; i < list.Length; i++)
		{
		  list[i] = parseString(list[i].Trim());
		}
		return list;
	  }

	  private static string parseString(string s)
	  {
		return s.Substring(1, s.Length - 1 - 1);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private static String readLine(java.io.LineNumberReader r) throws java.io.IOException
	  private static string readLine(LineNumberReader r)
	  {
		string line = null;
		while ((line = r.readLine()) != null)
		{
		  line = line.Trim();
		  if (line.Length > 0 && line[0] != '#')
		  {
			return line;
		  }
		}
		return line;
	  }
	}

}