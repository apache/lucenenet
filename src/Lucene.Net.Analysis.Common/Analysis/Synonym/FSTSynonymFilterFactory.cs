using System;
using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using org.apache.lucene.analysis.core;
using org.apache.lucene.analysis.synonym;
using org.apache.lucene.analysis.util;

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
    /// @deprecated (3.4) use <seealso cref="SynonymFilterFactory"/> instead. this is only a backwards compatibility
	///                   mechanism that will be removed in Lucene 5.0 
	// NOTE: rename this to "SynonymFilterFactory" and nuke that delegator in Lucene 5.0!
	[Obsolete("(3.4) use <seealso cref="SynonymFilterFactory"/> instead. this is only a backwards compatibility")]
	internal sealed class FSTSynonymFilterFactory : TokenFilterFactory, ResourceLoaderAware
	{
	  private readonly bool ignoreCase;
	  private readonly string tokenizerFactory;
	  private readonly string synonyms;
	  private readonly string format;
	  private readonly bool expand;
	  private readonly IDictionary<string, string> tokArgs = new Dictionary<string, string>();

	  private SynonymMap map;

	  public FSTSynonymFilterFactory(IDictionary<string, string> args) : base(args)
	  {
		ignoreCase = getBoolean(args, "ignoreCase", false);
		synonyms = require(args, "synonyms");
		format = get(args, "format");
		expand = getBoolean(args, "expand", true);

		tokenizerFactory = get(args, "tokenizerFactory");
		if (tokenizerFactory != null)
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

	  public override TokenStream create(TokenStream input)
	  {
		// if the fst is null, it means there's actually no synonyms... just return the original stream
		// as there is nothing to do here.
		return map.fst == null ? input : new SynonymFilter(input, map, ignoreCase);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void inform(ResourceLoader loader) throws java.io.IOException
	  public void inform(ResourceLoader loader)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final TokenizerFactory factory = tokenizerFactory == null ? null : loadTokenizerFactory(loader, tokenizerFactory);
		TokenizerFactory factory = tokenizerFactory == null ? null : loadTokenizerFactory(loader, tokenizerFactory);

		Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper(this, factory);

		try
		{
		  string formatClass = format;
		  if (format == null || format.Equals("solr"))
		  {
			formatClass = typeof(SolrSynonymParser).Name;
		  }
		  else if (format.Equals("wordnet"))
		  {
			formatClass = typeof(WordnetSynonymParser).Name;
		  }
		  // TODO: expose dedup as a parameter?
		  map = loadSynonyms(loader, formatClass, true, analyzer);
		}
		catch (ParseException e)
		{
		  throw new IOException("Error parsing synonyms file:", e);
		}
	  }

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  private readonly FSTSynonymFilterFactory outerInstance;

		  private TokenizerFactory factory;

		  public AnalyzerAnonymousInnerClassHelper(FSTSynonymFilterFactory outerInstance, TokenizerFactory factory)
		  {
			  this.outerInstance = outerInstance;
			  this.factory = factory;
		  }

		  protected internal override Analyzer.TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = factory == null ? new WhitespaceTokenizer(Version.LUCENE_CURRENT, reader) : factory.create(reader);
			TokenStream stream = outerInstance.ignoreCase ? new LowerCaseFilter(Version.LUCENE_CURRENT, tokenizer) : tokenizer;
			return new Analyzer.TokenStreamComponents(tokenizer, stream);
		  }
	  }

	  /// <summary>
	  /// Load synonyms with the given <seealso cref="SynonymMap.Parser"/> class.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private org.apache.lucene.analysis.synonym.SynonymMap loadSynonyms(ResourceLoader loader, String cname, boolean dedup, org.apache.lucene.analysis.Analyzer analyzer) throws java.io.IOException, java.text.ParseException
	  private SynonymMap loadSynonyms(ResourceLoader loader, string cname, bool dedup, Analyzer analyzer)
	  {
		CharsetDecoder decoder = Charset.forName("UTF-8").newDecoder().onMalformedInput(CodingErrorAction.REPORT).onUnmappableCharacter(CodingErrorAction.REPORT);

		SynonymMap.Parser parser;
		Type clazz = loader.findClass(cname, typeof(SynonymMap.Parser));
		try
		{
		  parser = clazz.getConstructor(typeof(bool), typeof(bool), typeof(Analyzer)).newInstance(dedup, expand, analyzer);
		}
		catch (Exception e)
		{
		  throw new Exception(e);
		}

		File synonymFile = new File(synonyms);
		if (synonymFile.exists())
		{
		  decoder.reset();
		  parser.parse(new InputStreamReader(loader.openResource(synonyms), decoder));
		}
		else
		{
		  IList<string> files = splitFileNames(synonyms);
		  foreach (string file in files)
		  {
			decoder.reset();
			parser.parse(new InputStreamReader(loader.openResource(file), decoder));
		  }
		}
		return parser.build();
	  }

	  // (there are no tests for this functionality)
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
	}

}