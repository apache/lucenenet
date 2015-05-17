using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Synonym
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
    /// <summary>
	/// Factory for <seealso cref="SlowSynonymFilter"/> (only used with luceneMatchVersion < 3.4)
	/// <pre class="prettyprint" >
	/// &lt;fieldType name="text_synonym" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
	///     &lt;filter class="solr.SynonymFilterFactory" synonyms="synonyms.txt" ignoreCase="false"
	///             expand="true" tokenizerFactory="solr.WhitespaceTokenizerFactory"/&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre> </summary>
	/// @deprecated (3.4) use <seealso cref="SynonymFilterFactory"/> instead. only for precise index backwards compatibility. this factory will be removed in Lucene 5.0 
	[Obsolete("(3.4) use <seealso cref=\"SynonymFilterFactory\"/> instead. only for precise index backwards compatibility. this factory will be removed in Lucene 5.0")]
	internal sealed class SlowSynonymFilterFactory : TokenFilterFactory, ResourceLoaderAware
	{
	  private readonly string synonyms;
	  private readonly bool ignoreCase;
	  private readonly bool expand;
	  private readonly string tf;
	  private readonly IDictionary<string, string> tokArgs = new Dictionary<string, string>();

	  public SlowSynonymFilterFactory(IDictionary<string, string> args) : base(args)
	  {
		synonyms = require(args, "synonyms");
		ignoreCase = getBoolean(args, "ignoreCase", false);
		expand = getBoolean(args, "expand", true);

		tf = get(args, "tokenizerFactory");
		if (tf != null)
		{
		  assureMatchVersion();
		  tokArgs["luceneMatchVersion"] = LuceneMatchVersion.ToString();
		  for (IEnumerator<string> itr = args.Keys.GetEnumerator(); itr.MoveNext();)
		  {
			string key = itr.Current;
			tokArgs[key.replaceAll("^tokenizerFactory\\.","")] = args[key];
			itr.remove();
		  }
		}
		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void inform(ResourceLoader loader) throws java.io.IOException
	  public void inform(ResourceLoader loader)
	  {
		TokenizerFactory tokFactory = null;
		if (tf != null)
		{
		  tokFactory = loadTokenizerFactory(loader, tf);
		}

		IEnumerable<string> wlist = loadRules(synonyms, loader);

		synMap = new SlowSynonymMap(ignoreCase);
		parseRules(wlist, synMap, "=>", ",", expand,tokFactory);
	  }

	  /// <returns> a list of all rules </returns>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: protected Iterable<String> loadRules(String synonyms, ResourceLoader loader) throws java.io.IOException
	  protected internal IEnumerable<string> loadRules(string synonyms, ResourceLoader loader)
	  {
		IList<string> wlist = null;
		File synonymFile = new File(synonyms);
		if (synonymFile.exists())
		{
		  wlist = getLines(loader, synonyms);
		}
		else
		{
		  IList<string> files = splitFileNames(synonyms);
		  wlist = new List<>();
		  foreach (string file in files)
		  {
			IList<string> lines = getLines(loader, file.Trim());
			wlist.AddRange(lines);
		  }
		}
		return wlist;
	  }

	  private SlowSynonymMap synMap;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: static void parseRules(Iterable<String> rules, SlowSynonymMap map, String mappingSep, String synSep, boolean expansion, TokenizerFactory tokFactory) throws java.io.IOException
	  internal static void parseRules(IEnumerable<string> rules, SlowSynonymMap map, string mappingSep, string synSep, bool expansion, TokenizerFactory tokFactory)
	  {
		int count = 0;
		foreach (string rule in rules)
		{
		  // To use regexes, we need an expression that specifies an odd number of chars.
		  // This can't really be done with string.split(), and since we need to
		  // do unescaping at some point anyway, we wouldn't be saving any effort
		  // by using regexes.

		  IList<string> mapping = splitSmart(rule, mappingSep, false);

		  IList<IList<string>> source;
		  IList<IList<string>> target;

		  if (mapping.Count > 2)
		  {
			throw new System.ArgumentException("Invalid Synonym Rule:" + rule);
		  }
		  else if (mapping.Count == 2)
		  {
			source = getSynList(mapping[0], synSep, tokFactory);
			target = getSynList(mapping[1], synSep, tokFactory);
		  }
		  else
		  {
			source = getSynList(mapping[0], synSep, tokFactory);
			if (expansion)
			{
			  // expand to all arguments
			  target = source;
			}
			else
			{
			  // reduce to first argument
			  target = new List<>(1);
			  target.Add(source[0]);
			}
		  }

		  bool includeOrig = false;
		  foreach (IList<string> fromToks in source)
		  {
			count++;
			foreach (IList<string> toToks in target)
			{
			  map.add(fromToks, SlowSynonymMap.makeTokens(toToks), includeOrig, true);
			}
		  }
		}
	  }

	  // a , b c , d e f => [[a],[b,c],[d,e,f]]
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private static java.util.List<java.util.List<String>> getSynList(String str, String separator, TokenizerFactory tokFactory) throws java.io.IOException
	  private static IList<IList<string>> getSynList(string str, string separator, TokenizerFactory tokFactory)
	  {
		IList<string> strList = splitSmart(str, separator, false);
		// now split on whitespace to get a list of token strings
		IList<IList<string>> synList = new List<IList<string>>();
		foreach (string toks in strList)
		{
		  IList<string> tokList = tokFactory == null ? splitWS(toks, true) : splitByTokenizer(toks, tokFactory);
		  synList.Add(tokList);
		}
		return synList;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private static java.util.List<String> splitByTokenizer(String source, TokenizerFactory tokFactory) throws java.io.IOException
	  private static IList<string> splitByTokenizer(string source, TokenizerFactory tokFactory)
	  {
		StringReader reader = new StringReader(source);
		TokenStream ts = loadTokenizer(tokFactory, reader);
		IList<string> tokList = new List<string>();
		try
		{
		  CharTermAttribute termAtt = ts.addAttribute(typeof(CharTermAttribute));
		  ts.reset();
		  while (ts.incrementToken())
		  {
			if (termAtt.length() > 0)
			{
			  tokList.Add(termAtt.ToString());
			}
		  }
		}
		finally
		{
		  reader.close();
		}
		return tokList;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private TokenizerFactory loadTokenizerFactory(ResourceLoader loader, String cname) throws java.io.IOException
	  private TokenizerFactory loadTokenizerFactory(ResourceLoader loader, string cname)
	  {
		Type clazz = loader.findClass(cname, typeof(TokenizerFactory));
		try
		{
		  TokenizerFactory tokFactory = clazz.getConstructor(typeof(IDictionary)).newInstance(tokArgs);
		  if (tokFactory is ResourceLoaderAware)
		  {
			((ResourceLoaderAware) tokFactory).inform(loader);
		  }
		  return tokFactory;
		}
		catch (Exception e)
		{
		  throw new Exception(e);
		}
	  }

	  private static TokenStream loadTokenizer(TokenizerFactory tokFactory, Reader reader)
	  {
		return tokFactory.create(reader);
	  }

	  public SlowSynonymMap SynonymMap
	  {
		  get
		  {
			return synMap;
		  }
	  }

	  public override SlowSynonymFilter create(TokenStream input)
	  {
		return new SlowSynonymFilter(input,synMap);
	  }

	  public static IList<string> splitWS(string s, bool decode)
	  {
		List<string> lst = new List<string>(2);
		StringBuilder sb = new StringBuilder();
		int pos = 0, end = s.Length;
		while (pos < end)
		{
		  char ch = s[pos++];
		  if (char.IsWhiteSpace(ch))
		  {
			if (sb.Length > 0)
			{
			  lst.Add(sb.ToString());
			  sb = new StringBuilder();
			}
			continue;
		  }

		  if (ch == '\\')
		  {
			if (!decode)
			{
				sb.Append(ch);
			}
			if (pos >= end) // ERROR, or let it go?
			{
				break;
			}
			ch = s[pos++];
			if (decode)
			{
			  switch (ch)
			  {
				case 'n' :
					ch = '\n';
					break;
				case 't' :
					ch = '\t';
					break;
				case 'r' :
					ch = '\r';
					break;
				case 'b' :
					ch = '\b';
					break;
				case 'f' :
					ch = '\f';
					break;
			  }
			}
		  }

		  sb.Append(ch);
		}

		if (sb.Length > 0)
		{
		  lst.Add(sb.ToString());
		}

		return lst;
	  }

	  /// <summary>
	  /// Splits a backslash escaped string on the separator.
	  /// <para>
	  /// Current backslash escaping supported:
	  /// <br> \n \t \r \b \f are escaped the same as a Java String
	  /// <br> Other characters following a backslash are produced verbatim (\c => c)
	  /// 
	  /// </para>
	  /// </summary>
	  /// <param name="s">  the string to split </param>
	  /// <param name="separator"> the separator to split on </param>
	  /// <param name="decode"> decode backslash escaping </param>
	  public static IList<string> splitSmart(string s, string separator, bool decode)
	  {
		List<string> lst = new List<string>(2);
		StringBuilder sb = new StringBuilder();
		int pos = 0, end = s.Length;
		while (pos < end)
		{
		  if (s.StartsWith(separator,pos))
		  {
			if (sb.Length > 0)
			{
			  lst.Add(sb.ToString());
			  sb = new StringBuilder();
			}
			pos += separator.Length;
			continue;
		  }

		  char ch = s[pos++];
		  if (ch == '\\')
		  {
			if (!decode)
			{
				sb.Append(ch);
			}
			if (pos >= end) // ERROR, or let it go?
			{
				break;
			}
			ch = s[pos++];
			if (decode)
			{
			  switch (ch)
			  {
				case 'n' :
					ch = '\n';
					break;
				case 't' :
					ch = '\t';
					break;
				case 'r' :
					ch = '\r';
					break;
				case 'b' :
					ch = '\b';
					break;
				case 'f' :
					ch = '\f';
					break;
			  }
			}
		  }

		  sb.Append(ch);
		}

		if (sb.Length > 0)
		{
		  lst.Add(sb.ToString());
		}

		return lst;
	  }
	}

}