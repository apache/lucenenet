// lucene version compatibility level: 4.8.1
using Lucene.Net.Analysis.Phonetic.Language;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Lucene.Net.Analysis.Phonetic
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
    /// Factory for <see cref="PhoneticFilter"/>.
    /// <para/>
    /// Create tokens based on phonetic encoders from the Language namespace.
    /// <para/>
    /// This takes one required argument, "encoder", and the rest are optional:
    /// <list type="bullet">
    ///     <item>
    ///         <term>encoder</term>
    ///         <description>
    ///         required, one of "DoubleMetaphone", "Metaphone", "Soundex", "RefinedSoundex", "Caverphone" (v2.0),
    ///         or "ColognePhonetic" (case insensitive). If encoder isn't one of these, it'll be resolved as a class name either by
    ///         itself if it already contains a '.' or otherwise as in the same package as these others.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>inject</term>
    ///         <description>
    ///         (default=true) add tokens to the stream with the offset=0
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>maxCodeLength</term>
    ///         <description>
    ///         The maximum length of the phonetic codes, as defined by the encoder. If an encoder doesn't
    ///         support this then specifying this is an error.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// <code>
    /// &lt;fieldType name="text_phonetic" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.WhitespaceTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.PhoneticFilterFactory" encoder="DoubleMetaphone" inject="true"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </summary>
    /// <seealso cref="PhoneticFilter"/>
    public class PhoneticFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        /// <summary>parameter name: either a short name or a full class name</summary>
        public const string ENCODER = "encoder";
        /// <summary>parameter name: true if encoded tokens should be added as synonyms</summary>
        public const string INJECT = "inject"; // boolean
                                                         /** parameter name: restricts the length of the phonetic code */
        public const string MAX_CODE_LENGTH = "maxCodeLength";
        private const string PACKAGE_CONTAINING_ENCODERS = "Lucene.Net.Analysis.Phonetic.Language.";

        //Effectively constants; uppercase keys
        private static readonly IDictionary<string, Type> registry = new Dictionary<string, Type> // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            { "DoubleMetaphone".ToUpperInvariant(), typeof(DoubleMetaphone) },
            { "Metaphone".ToUpperInvariant(), typeof(Metaphone) },
            { "Soundex".ToUpperInvariant(), typeof(Soundex) },
            { "RefinedSoundex".ToUpperInvariant(), typeof(RefinedSoundex) },
            { "Caverphone".ToUpperInvariant(), typeof(Caverphone2) },
            { "ColognePhonetic".ToUpperInvariant(), typeof(ColognePhonetic) },
        };

        internal bool inject; //accessed by the test
        private readonly string name;
        private readonly int? maxCodeLength;
        private Type clazz = null;
        private MethodInfo setMaxCodeLenMethod = null;

        /// <summary>Creates a new <see cref="PhoneticFilterFactory"/>.</summary>
        public PhoneticFilterFactory(IDictionary<string, string> args)
                : base(args)
        {
            inject = GetBoolean(args, INJECT, true);
            name = Require(args, ENCODER);
            string v = Get(args, MAX_CODE_LENGTH);
            if (v != null)
            {
                maxCodeLength = int.Parse(v, CultureInfo.InvariantCulture);
            }
            else
            {
                maxCodeLength = null;
            }
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }


        public virtual void Inform(IResourceLoader loader)
        {
            registry.TryGetValue(name.ToUpperInvariant(), out clazz);
            if (clazz is null)
            {
                clazz = ResolveEncoder(name, loader);
            }

            if (maxCodeLength != null)
            {
                try
                {
                    setMaxCodeLenMethod = clazz.GetMethod("set_MaxCodeLen");
                }
                catch (Exception e) when (e.IsException())
                {
                    throw new ArgumentException("Encoder " + name + " / " + clazz + " does not support " + MAX_CODE_LENGTH, e);
                }
            }

            GetEncoder();//trigger initialization for potential problems to be thrown now
        }

        private static Type ResolveEncoder(string name, IResourceLoader loader) // LUCENENET: CA1822: Mark members as static
        {
            string lookupName = name;
            if (name.IndexOf('.') == -1)
            {
                lookupName = PACKAGE_CONTAINING_ENCODERS + name;
            }
            try
            {
                return loader.NewInstance<IStringEncoder>(lookupName).GetType();
            }
            catch (Exception e) when (e.IsRuntimeException())
            {
                throw new ArgumentException("Error loading encoder '" + name + "': must be full class name or one of " + Collections.ToString(registry.Keys), e);
            }
        }

        /// <summary>Must be thread-safe.</summary>
        protected internal virtual IStringEncoder GetEncoder()
        {
            // Unfortunately, Commons-Codec doesn't offer any thread-safe guarantees so we must play it safe and instantiate
            // every time.  A simple benchmark showed this as negligible.
            try
            {
                IStringEncoder encoder = (IStringEncoder)Activator.CreateInstance(clazz);
                // Try to set the maxCodeLength
                if (maxCodeLength != null && setMaxCodeLenMethod != null)
                {
                    setMaxCodeLenMethod.Invoke(encoder, new object[] { maxCodeLength });
                }
                return encoder;
            }
            catch (Exception e) when (e.IsException())
            {
                Exception t = (e is TargetInvocationException) ? e.InnerException : e;
                throw new ArgumentException("Error initializing encoder: " + name + " / " + clazz, t);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new PhoneticFilter(input, GetEncoder(), inject);
        }
    }
}
