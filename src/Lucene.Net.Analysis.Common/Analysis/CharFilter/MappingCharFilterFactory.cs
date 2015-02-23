using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using org.apache.lucene.analysis.charfilter;
using org.apache.lucene.analysis.util;

namespace Lucene.Net.Analysis.CharFilters
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
	/// Factory for <seealso cref="MappingCharFilter"/>. 
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_map" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;charFilter class="solr.MappingCharFilterFactory" mapping="mapping.txt"/&gt;
	///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// 
	/// @since Solr 1.4
	/// </summary>
	public class MappingCharFilterFactory : CharFilterFactory, ResourceLoaderAware, MultiTermAwareComponent
	{

	  protected internal NormalizeCharMap normMap;
	  private readonly string mapping;

	  /// <summary>
	  /// Creates a new MappingCharFilterFactory </summary>
	  public MappingCharFilterFactory(IDictionary<string, string> args) : base(args)
	  {
		mapping = get(args, "mapping");
		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
	  }

	  // TODO: this should use inputstreams from the loader, not File!
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void inform(org.apache.lucene.analysis.util.ResourceLoader loader) throws java.io.IOException
	  public virtual void inform(ResourceLoader loader)
	  {
		if (mapping != null)
		{
		  IList<string> wlist = null;
		  File mappingFile = new File(mapping);
		  if (mappingFile.exists())
		  {
			wlist = getLines(loader, mapping);
		  }
		  else
		  {
			IList<string> files = splitFileNames(mapping);
			wlist = new List<>();
			foreach (string file in files)
			{
			  IList<string> lines = getLines(loader, file.Trim());
			  wlist.AddRange(lines);
			}
		  }
		  NormalizeCharMap.Builder builder = new NormalizeCharMap.Builder();
		  parseRules(wlist, builder);
		  normMap = builder.build();
		  if (normMap.map == null)
		  {
			// if the inner FST is null, it means it accepts nothing (e.g. the file is empty)
			// so just set the whole map to null
			normMap = null;
		  }
		}
	  }

	  public override TextReader Create(TextReader input)
	  {
		// if the map is null, it means there's actually no mappings... just return the original stream
		// as there is nothing to do here.
		return normMap == null ? input : new MappingCharFilter(normMap,input);
	  }

	  // "source" => "target"
	  internal static Pattern p = Pattern.compile("\"(.*)\"\\s*=>\\s*\"(.*)\"\\s*$");

	  protected internal virtual void parseRules(IList<string> rules, NormalizeCharMap.Builder builder)
	  {
		foreach (string rule in rules)
		{
		  Matcher m = p.matcher(rule);
		  if (!m.find())
		  {
			throw new System.ArgumentException("Invalid Mapping Rule : [" + rule + "], file = " + mapping);
		  }
		  builder.add(parseString(m.group(1)), parseString(m.group(2)));
		}
	  }

	  internal char[] @out = new char[256];

	  protected internal virtual string parseString(string s)
	  {
		int readPos = 0;
		int len = s.Length;
		int writePos = 0;
		while (readPos < len)
		{
		  char c = s[readPos++];
		  if (c == '\\')
		  {
			if (readPos >= len)
			{
			  throw new System.ArgumentException("Invalid escaped char in [" + s + "]");
			}
			c = s[readPos++];
			switch (c)
			{
			  case '\\' :
				  c = '\\';
				  break;
			  case '"' :
				  c = '"';
				  break;
			  case 'n' :
				  c = '\n';
				  break;
			  case 't' :
				  c = '\t';
				  break;
			  case 'r' :
				  c = '\r';
				  break;
			  case 'b' :
				  c = '\b';
				  break;
			  case 'f' :
				  c = '\f';
				  break;
			  case 'u' :
				if (readPos + 3 >= len)
				{
				  throw new System.ArgumentException("Invalid escaped char in [" + s + "]");
				}
				c = (char)int.Parse(s.Substring(readPos, 4), 16);
				readPos += 4;
				break;
			}
		  }
		  @out[writePos++] = c;
		}
		return new string(@out, 0, writePos);
	  }

	  public virtual AbstractAnalysisFactory MultiTermComponent
	  {
		  get
		  {
			return this;
		  }
	  }
	}

}