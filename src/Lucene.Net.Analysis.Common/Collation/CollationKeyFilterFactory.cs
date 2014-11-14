using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Analysis.Util;

namespace org.apache.lucene.collation
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


	using TokenStream = org.apache.lucene.analysis.TokenStream;
	using org.apache.lucene.analysis.util;
	using IOUtils = org.apache.lucene.util.IOUtils;

	/// <summary>
	/// Factory for <seealso cref="CollationKeyFilter"/>.
	/// <para>
	/// This factory can be created in two ways: 
	/// <ul>
	///  <li>Based upon a system collator associated with a Locale.
	///  <li>Based upon a tailored ruleset.
	/// </ul>
	/// </para>
	/// <para>
	/// Using a System collator:
	/// <ul>
	///  <li>language: ISO-639 language code (mandatory)
	///  <li>country: ISO-3166 country code (optional)
	///  <li>variant: vendor or browser-specific code (optional)
	///  <li>strength: 'primary','secondary','tertiary', or 'identical' (optional)
	///  <li>decomposition: 'no','canonical', or 'full' (optional)
	/// </ul>
	/// </para>
	/// <para>
	/// Using a Tailored ruleset:
	/// <ul>
	///  <li>custom: UTF-8 text file containing rules supported by RuleBasedCollator (mandatory)
	///  <li>strength: 'primary','secondary','tertiary', or 'identical' (optional)
	///  <li>decomposition: 'no','canonical', or 'full' (optional)
	/// </ul>
	/// 
	/// <pre class="prettyprint" >
	/// &lt;fieldType name="text_clltnky" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.KeywordTokenizerFactory"/&gt;
	///     &lt;filter class="solr.CollationKeyFilterFactory" language="ja" country="JP"/&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// 
	/// </para>
	/// </summary>
	/// <seealso cref= Collator </seealso>
	/// <seealso cref= Locale </seealso>
	/// <seealso cref= RuleBasedCollator
	/// @since solr 3.1 </seealso>
	/// @deprecated use <seealso cref="CollationKeyAnalyzer"/> instead. 
	[Obsolete("use <seealso cref="CollationKeyAnalyzer"/> instead.")]
	public class CollationKeyFilterFactory : TokenFilterFactory, MultiTermAwareComponent, ResourceLoaderAware
	{
	  private Collator collator;
	  private readonly string custom;
	  private readonly string language;
	  private readonly string country;
	  private readonly string variant;
	  private readonly string strength;
	  private readonly string decomposition;

	  public CollationKeyFilterFactory(IDictionary<string, string> args) : base(args)
	  {
		custom = args.Remove("custom");
		language = args.Remove("language");
		country = args.Remove("country");
		variant = args.Remove("variant");
		strength = args.Remove("strength");
		decomposition = args.Remove("decomposition");

		if (custom == null && language == null)
		{
		  throw new System.ArgumentException("Either custom or language is required.");
		}

		if (custom != null && (language != null || country != null || variant != null))
		{
		  throw new System.ArgumentException("Cannot specify both language and custom. " + "To tailor rules for a built-in language, see the javadocs for RuleBasedCollator. " + "Then save the entire customized ruleset to a file, and use with the custom parameter");
		}

		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void inform(ResourceLoader loader) throws java.io.IOException
	  public virtual void inform(ResourceLoader loader)
	  {
		if (language != null)
		{
		  // create from a system collator, based on Locale.
		  collator = createFromLocale(language, country, variant);
		}
		else
		{
		  // create from a custom ruleset
		  collator = createFromRules(custom, loader);
		}

		// set the strength flag, otherwise it will be the default.
		if (strength != null)
		{
		  if (strength.Equals("primary", StringComparison.CurrentCultureIgnoreCase))
		  {
			collator.Strength = Collator.PRIMARY;
		  }
		  else if (strength.Equals("secondary", StringComparison.CurrentCultureIgnoreCase))
		  {
			collator.Strength = Collator.SECONDARY;
		  }
		  else if (strength.Equals("tertiary", StringComparison.CurrentCultureIgnoreCase))
		  {
			collator.Strength = Collator.TERTIARY;
		  }
		  else if (strength.Equals("identical", StringComparison.CurrentCultureIgnoreCase))
		  {
			collator.Strength = Collator.IDENTICAL;
		  }
		  else
		  {
			throw new System.ArgumentException("Invalid strength: " + strength);
		  }
		}

		// set the decomposition flag, otherwise it will be the default.
		if (decomposition != null)
		{
		  if (decomposition.Equals("no", StringComparison.CurrentCultureIgnoreCase))
		  {
			collator.Decomposition = Collator.NO_DECOMPOSITION;
		  }
		  else if (decomposition.Equals("canonical", StringComparison.CurrentCultureIgnoreCase))
		  {
			collator.Decomposition = Collator.CANONICAL_DECOMPOSITION;
		  }
		  else if (decomposition.Equals("full", StringComparison.CurrentCultureIgnoreCase))
		  {
			collator.Decomposition = Collator.FULL_DECOMPOSITION;
		  }
		  else
		  {
			throw new System.ArgumentException("Invalid decomposition: " + decomposition);
		  }
		}
	  }

	  public override TokenStream create(TokenStream input)
	  {
		return new CollationKeyFilter(input, collator);
	  }

	  /*
	   * Create a locale from language, with optional country and variant.
	   * Then return the appropriate collator for the locale.
	   */
	  private Collator createFromLocale(string language, string country, string variant)
	  {
		Locale locale;

		if (language != null && country == null && variant != null)
		{
		  throw new System.ArgumentException("To specify variant, country is required");
		}
		else if (language != null && country != null && variant != null)
		{
		  locale = new Locale(language, country, variant);
		}
		else if (language != null && country != null)
		{
		  locale = new Locale(language, country);
		}
		else
		{
		  locale = new Locale(language);
		}

		return Collator.getInstance(locale);
	  }

	  /*
	   * Read custom rules from a file, and create a RuleBasedCollator
	   * The file cannot support comments, as # might be in the rules!
	   */
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private java.text.Collator createFromRules(String fileName, ResourceLoader loader) throws java.io.IOException
	  private Collator createFromRules(string fileName, ResourceLoader loader)
	  {
		InputStream input = null;
		try
		{
		 input = loader.openResource(fileName);
		 string rules = toUTF8String(input);
		 return new RuleBasedCollator(rules);
		}
		catch (ParseException e)
		{
		  // invalid rules
		  throw new IOException("ParseException thrown while parsing rules", e);
		}
		finally
		{
		  IOUtils.closeWhileHandlingException(input);
		}
	  }

	  public virtual AbstractAnalysisFactory MultiTermComponent
	  {
		  get
		  {
			return this;
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private String toUTF8String(java.io.InputStream in) throws java.io.IOException
	  private string toUTF8String(InputStream @in)
	  {
		StringBuilder sb = new StringBuilder();
		char[] buffer = new char[1024];
		Reader r = IOUtils.getDecodingReader(@in, StandardCharsets.UTF_8);
		int len = 0;
		while ((len = r.read(buffer)) > 0)
		{
		  sb.Append(buffer, 0, len);
		}
		return sb.ToString();
	  }
	}

}