using Lucene.Net.Analysis.Ja.Dict;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Ja
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
    /// Factory for <see cref="JapaneseTokenizer"/>.
    /// <code>
    /// &lt;fieldType name="text_ja" class="solr.TextField"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.JapaneseTokenizerFactory"
    ///       mode="NORMAL"
    ///       userDictionary="user.txt"
    ///       userDictionaryEncoding="UTF-8"
    ///       discardPunctuation="true"
    ///     /&gt;
    ///     &lt;filter class="solr.JapaneseBaseFormFilterFactory"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </summary>
    public class JapaneseTokenizerFactory : TokenizerFactory, IResourceLoaderAware
    {
        private const string MODE = "mode";

        private const string USER_DICT_PATH = "userDictionary";

        private const string USER_DICT_ENCODING = "userDictionaryEncoding";

        private const string DISCARD_PUNCTUATION = "discardPunctuation"; // Expert option

        private UserDictionary userDictionary;

        private readonly JapaneseTokenizerMode mode;
        private readonly bool discardPunctuation;
        private readonly string userDictionaryPath;
        private readonly string userDictionaryEncoding;

        /// <summary>Creates a new <see cref="JapaneseTokenizerFactory"/>.</summary>
        public JapaneseTokenizerFactory(IDictionary<string, string> args)
            : base(args)
        {
            Enum.TryParse(Get(args, MODE, JapaneseTokenizer.DEFAULT_MODE.ToString()), true, out mode);
            userDictionaryPath = Get(args, USER_DICT_PATH);
            userDictionaryEncoding = Get(args, USER_DICT_ENCODING);
            discardPunctuation = GetBoolean(args, DISCARD_PUNCTUATION, true);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        static JapaneseTokenizerFactory()
        {
#if FEATURE_ENCODINGPROVIDERS
            // Support for EUC-JP encoding. See: https://docs.microsoft.com/en-us/dotnet/api/system.text.codepagesencodingprovider?view=netcore-2.0
            var encodingProvider = System.Text.CodePagesEncodingProvider.Instance;
            System.Text.Encoding.RegisterProvider(encodingProvider);
#endif
        }

        public virtual void Inform(IResourceLoader loader)
        {
            if (userDictionaryPath != null)
            {
                Stream stream = loader.OpenResource(userDictionaryPath);
                string encoding = userDictionaryEncoding;
                if (encoding is null)
                {
                    encoding = Encoding.UTF8.WebName;
                }
                Encoding decoder = Encoding.GetEncoding(encoding);
                TextReader reader = new StreamReader(stream, decoder);
                userDictionary = new UserDictionary(reader);
            }
            else
            {
                userDictionary = null;
            }
        }

        public override Tokenizer Create(AttributeSource.AttributeFactory factory, TextReader input)
        {
            return new JapaneseTokenizer(factory, input, userDictionary, discardPunctuation, mode);
        }
    }
}
