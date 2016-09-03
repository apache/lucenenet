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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Collation;
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
	/// <seealso cref="CultureInfo"></seealso>
	/// <seealso cref="RuleBasedCollator">
	/// @since solr 3.1 </seealso>
	/// @deprecated use <seealso cref="CollationKeyAnalyzer"/> instead. 
	[Obsolete("use <seealso cref=\"CollationKeyAnalyzer\"/> instead.")]
	public class CollationKeyFilterFactory : TokenFilterFactory, IMultiTermAwareComponent, IResourceLoaderAware
	{
		private Collator collator;
		private readonly String custom;
		private readonly String language;
		private readonly String country;
		private readonly String variant;
		private readonly String strength;
		private readonly String decomposition;

		public CollationKeyFilterFactory(IDictionary<string, string> args) : base(args)
		{
			this.custom = this.RemoveFromDictionary(args, "custom");
			this.language = this.RemoveFromDictionary(args, "language");
			this.country = this.RemoveFromDictionary(args, "country");
			this.variant = this.RemoveFromDictionary(args, "variant");
			this.strength = this.RemoveFromDictionary(args, "strength");
			this.decomposition = this.RemoveFromDictionary(args, "decomposition");
			
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
				//this.collator = this.CreateFromRules(this.custom, loader);
			}

			// set the strength flag, otherwise it will be the default.
			if (this.strength != null)
			{
				if (this.strength.Equals("primary", StringComparison.CurrentCultureIgnoreCase))
				{
					this.collator.Strength = Collator.Primary;
				}
				else if (this.strength.Equals("secondary", StringComparison.CurrentCultureIgnoreCase))
				{
					this.collator.Strength = Collator.Secondary;
				}
				else if (this.strength.Equals("tertiary", StringComparison.CurrentCultureIgnoreCase))
				{
					this.collator.Strength = Collator.Tertiary;
				}
				else if (this.strength.Equals("identical", StringComparison.CurrentCultureIgnoreCase))
				{
					this.collator.Strength = Collator.Identical;
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
					this.collator.Decomposition = Collator.NoDecomposition;
				}
				else if (this.decomposition.Equals("canonical", StringComparison.CurrentCultureIgnoreCase))
				{
					this.collator.Decomposition = Collator.CannonicalDecomposition;
				}
				else if (this.decomposition.Equals("full", StringComparison.CurrentCultureIgnoreCase))
				{
					this.collator.Decomposition = Collator.FullDecomposition;
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
			CultureInfo cultureInfo;

			if (language == null)
			{
				throw new System.ArgumentException("Language is required");
			}

			if (language != null && country == null && variant != null)
			{
				throw new System.ArgumentException("To specify variant, country is required");
			}

			if (country != null && variant != null)
			{
				cultureInfo = CultureInfo.GetCultures(CultureTypes.SpecificCultures).Single(x =>
				{
					if (!x.TwoLetterISOLanguageName.Equals(language, StringComparison.OrdinalIgnoreCase) &&
						!x.ThreeLetterISOLanguageName.Equals(language, StringComparison.OrdinalIgnoreCase) &&
						!x.ThreeLetterWindowsLanguageName.Equals(language, StringComparison.OrdinalIgnoreCase))
					{
						return false;
					}

					var region = new RegionInfo(x.Name);

					if (!region.TwoLetterISORegionName.Equals(country, StringComparison.OrdinalIgnoreCase) &&
						!region.ThreeLetterISORegionName.Equals(country, StringComparison.OrdinalIgnoreCase) &&
						!region.ThreeLetterWindowsRegionName.Equals(country, StringComparison.OrdinalIgnoreCase))
					{
						return false;
					}

					return x.Name
						.Replace(x.TwoLetterISOLanguageName, String.Empty)
						.Replace(region.TwoLetterISORegionName, String.Empty)
						.Replace("-", String.Empty)
						.Equals(variant, StringComparison.OrdinalIgnoreCase);
				});
			}
			else if (country != null)
			{
				cultureInfo = CultureInfo.GetCultureInfo(String.Concat(language, "-", country));
			}
			else
			{
				cultureInfo = CultureInfo.GetCultureInfo(language);
			}

			return Collator.GetInstance(cultureInfo);
		}

		/// <summary>
		/// Read custom rules from a file, and create a RuleBasedCollator
		/// The file cannot support comments, as # might be in the rules!
		/// </summary>
		//private Collator CreateFromRules(string fileName, IResourceLoader loader)
		//{
		//	Stream input = null;
		//	try
		//	{
		//		input = loader.OpenResource(fileName);
		//		var rules = ToUTF8String(input);
		//		return new RuleBasedCollator(rules);
		//	}
		//	catch (ParseException e)
		//	{
		//		// invalid rules
		//		throw new IOException("ParseException thrown while parsing rules", e);
		//	}
		//	finally
		//	{
		//		IOUtils.CloseWhileHandlingException(input);
		//	}
		//}

		public virtual AbstractAnalysisFactory MultiTermComponent
		{
			get
			{
				return this;
			}
		}

		private static string ToUTF8String(Stream @in)
		{
			var builder = new StringBuilder();
			var buffer = new char[1024];
			var reader = IOUtils.GetDecodingReader(@in, Encoding.UTF8);

			var index = 0;
			while ((index = reader.Read(buffer, index, 1)) > 0)
			{
				builder.Append(buffer, 0, index);
			}

			return builder.ToString();
		}

		/// <summary>
		/// Trys to gets the value of a key from a dictionary and removes the value after.
		/// This is to mimic java's Dictionary.Remove method.
		/// </summary>
		/// <returns>The value for the given key; otherwise null.</returns>
		private string RemoveFromDictionary(IDictionary<string, string> args, string key)
		{
			string value = null;
			if (args.TryGetValue(key, out value))
			{
				args.Remove(key);
			}

			return value;
		}
	}
}