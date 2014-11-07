using System.Collections.Generic;
using TokenFilterFactory = Lucene.Net.Analysis.Util.TokenFilterFactory;

namespace org.apache.lucene.analysis.payloads
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

	using ResourceLoader = org.apache.lucene.analysis.util.ResourceLoader;
	using ResourceLoaderAware = org.apache.lucene.analysis.util.ResourceLoaderAware;
	using TokenFilterFactory = TokenFilterFactory;

	/// <summary>
	/// Factory for <seealso cref="DelimitedPayloadTokenFilter"/>.
	/// <pre class="prettyprint">
	/// &lt;fieldType name="text_dlmtd" class="solr.TextField" positionIncrementGap="100"&gt;
	///   &lt;analyzer&gt;
	///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
	///     &lt;filter class="solr.DelimitedPayloadTokenFilterFactory" encoder="float" delimiter="|"/&gt;
	///   &lt;/analyzer&gt;
	/// &lt;/fieldType&gt;</pre>
	/// </summary>
	public class DelimitedPayloadTokenFilterFactory : TokenFilterFactory, ResourceLoaderAware
	{
	  public const string ENCODER_ATTR = "encoder";
	  public const string DELIMITER_ATTR = "delimiter";

	  private readonly string encoderClass;
	  private readonly char delimiter;

	  private PayloadEncoder encoder;

	  /// <summary>
	  /// Creates a new DelimitedPayloadTokenFilterFactory </summary>
	  public DelimitedPayloadTokenFilterFactory(IDictionary<string, string> args) : base(args)
	  {
		encoderClass = require(args, ENCODER_ATTR);
		delimiter = getChar(args, DELIMITER_ATTR, '|');
		if (args.Count > 0)
		{
		  throw new System.ArgumentException("Unknown parameters: " + args);
		}
	  }

	  public override DelimitedPayloadTokenFilter create(TokenStream input)
	  {
		return new DelimitedPayloadTokenFilter(input, delimiter, encoder);
	  }

	  public virtual void inform(ResourceLoader loader)
	  {
		if (encoderClass.Equals("float"))
		{
		  encoder = new FloatEncoder();
		}
		else if (encoderClass.Equals("integer"))
		{
		  encoder = new IntegerEncoder();
		}
		else if (encoderClass.Equals("identity"))
		{
		  encoder = new IdentityEncoder();
		}
		else
		{
		  encoder = loader.newInstance(encoderClass, typeof(PayloadEncoder));
		}
	  }
	}
}