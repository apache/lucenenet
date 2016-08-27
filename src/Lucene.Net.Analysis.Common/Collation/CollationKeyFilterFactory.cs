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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ICU4NET;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

namespace Lucene.Net.Collation
{
	/// <summary>
	/// Factory for <seealso cref="CollationKeyFilter"/>.
	/// <para>
	/// This factory can be created in two ways: 
	/// <ul>
	///  <li>Based upon a system collator associated with a Locale.</li>
	///  <li>Based upon a tailored ruleset.</li>
	/// </ul>
	/// </para>
	/// <para>
	/// Using a System collator:
	/// <ul>
	///  <li>language: ISO-639 language code (mandatory)</li>
	///  <li>country: ISO-3166 country code (optional)</li>
	///  <li>variant: vendor or browser-specific code (optional)</li>
	///  <li>strength: 'primary','secondary','tertiary', or 'identical' (optional)</li>
	///  <li>decomposition: 'no','canonical', or 'full' (optional)</li>
	/// </ul>
	/// </para>
	/// <para>
	/// Using a Tailored ruleset:
	/// <ul>
	///  <li>custom: UTF-8 text file containing rules supported by RuleBasedCollator (mandatory)</li>
	///  <li>strength: 'primary','secondary','tertiary', or 'identical' (optional)</li>
	///  <li>decomposition: 'no','canonical', or 'full' (optional)</li>
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
	/// <seealso cref="Collator"></seealso>
	/// <seealso cref="Locale"></seealso>
	/// <seealso cref="RuleBasedCollator">
	/// @since solr 3.1 </seealso>
	/// @deprecated use <seealso cref="CollationKeyAnalyzer"/> instead. 
	[Obsolete("use <seealso cref=\"CollationKeyAnalyzer\"/> instead.")]
	public class CollationKeyFilterFactory : TokenFilterFactory, IMultiTermAwareComponent, IResourceLoaderAware
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
			args.TryGetValue("custom", out this.custom);
			args.TryGetValue("language", out this.language);
			args.TryGetValue("country", out this.country);
			args.TryGetValue("variant", out this.variant);
			args.TryGetValue("strength", out this.strength);
			args.TryGetValue("decomposition", out this.decomposition);

			if (this.custom == null && this.language == null)
			{
				throw new ArgumentException("Either custom or language is required.");
			}

			if (this.custom != null && (this.language != null || this.country != null || this.variant != null))
			{
				throw new ArgumentException("Cannot specify both language and custom. " + "To tailor rules for a built-in language, see the javadocs for RuleBasedCollator. " + "Then save the entire customized ruleset to a file, and use with the custom parameter");
			}

			if (args.Count > 0)
			{
				throw new ArgumentException("Unknown parameters: " + args);
			}
		}

		public virtual void Inform(IResourceLoader loader)
		{
			if (this.language != null)
			{
				// create from a system collator, based on Locale.
				this.collator = this.CreateFromLocale(this.language, this.country, this.variant);
			}
			else
			{
				// create from a custom ruleset
				this.collator = this.CreateFromRules(this.custom, loader);
			}

			// set the strength flag, otherwise it will be the default.
			if (this.strength != null)
			{
				if (this.strength.Equals("primary", StringComparison.CurrentCultureIgnoreCase))
				{
					this.collator.Strength = Collator.PRIMARY;
				}
				else if (this.strength.Equals("secondary", StringComparison.CurrentCultureIgnoreCase))
				{
					this.collator.Strength = Collator.SECONDARY;
				}
				else if (this.strength.Equals("tertiary", StringComparison.CurrentCultureIgnoreCase))
				{
					this.collator.Strength = Collator.TERTIARY;
				}
				else if (this.strength.Equals("identical", StringComparison.CurrentCultureIgnoreCase))
				{
					this.collator.Strength = Collator.IDENTICAL;
				}
				else
				{
					throw new ArgumentException("Invalid strength: " + this.strength);
				}
			}

			// set the decomposition flag, otherwise it will be the default.
			if (this.decomposition != null)
			{
				if (this.decomposition.Equals("no", StringComparison.CurrentCultureIgnoreCase))
				{
					this.collator.Decomposition = Collator.NO_DECOMPOSITION;
				}
				else if (this.decomposition.Equals("canonical", StringComparison.CurrentCultureIgnoreCase))
				{
					this.collator.Decomposition = Collator.CANONICAL_DECOMPOSITION;
				}
				else if (this.decomposition.Equals("full", StringComparison.CurrentCultureIgnoreCase))
				{
					this.collator.Decomposition = Collator.FULL_DECOMPOSITION;
				}
				else
				{
					throw new ArgumentException("Invalid decomposition: " + this.decomposition);
				}
			}
		}

		public override TokenStream Create(TokenStream input)
		{
			return new CollationKeyFilter(input, this.collator);
		}

		/// <summary>
		/// Create a locale from language, with optional country and variant.
		/// Then return the appropriate collator for the locale.
		/// </summary>
		private Collator CreateFromLocale(string language, string country, string variant)
		{
			Locale locale;

			if (language != null && country == null && variant != null)
			{
				throw new ArgumentException("To specify variant, country is required");
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

		/// <summary>
		/// Read custom rules from a file, and create a RuleBasedCollator
		/// The file cannot support comments, as # might be in the rules!
		/// </summary>
		private Collator CreateFromRules(string fileName, IResourceLoader loader)
		{
			Stream input = null;
			try
			{
				input = loader.OpenResource(fileName);
				var rules = ToUTF8String(input);
				return new RuleBasedCollator(rules);
			}
			catch (ParseException e)
			{
				// invalid rules
				throw new IOException("ParseException thrown while parsing rules", e);
			}
			finally
			{
				IOUtils.CloseWhileHandlingException(input);
			}
		}

		public virtual AbstractAnalysisFactory MultiTermComponent
		{
			get
			{
				return this;
			}
		}

		private static string ToUTF8String(Stream @in)
		{
			var sb = new StringBuilder();
			var buffer = new char[1024];
			var r = IOUtils.GetDecodingReader(@in, Encoding.UTF8);
			var len = 0;
			
			while ((len = r.Read(buffer)) > 0)
			{
				sb.Append(buffer, 0, len);
			}

			return sb.ToString();
		}
	}

}