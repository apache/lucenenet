using Lucene.Net.Analysis.Fr;
using Lucene.Net.Analysis.In;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Nl;
using Lucene.Net.Analysis.Path;
using Lucene.Net.Analysis.Sinks;
using Lucene.Net.Analysis.Snowball;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Lucene.Net.Analysis.Core
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
    /// Tests that any newly added Tokenizers/TokenFilters/CharFilters have a
    /// corresponding factory (and that the SPI configuration is correct)
    /// </summary>
    public class TestAllAnalyzersHaveFactories : LuceneTestCase
    {

        // these are test-only components (e.g. test-framework)
        private static readonly ISet<Type> testComponents = new HashSet<Type>();
        static TestAllAnalyzersHaveFactories()
        {
            testComponents.addAll(new Type[] { typeof(MockTokenizer), typeof(MockCharFilter), typeof(MockFixedLengthPayloadFilter), typeof(MockGraphTokenFilter), typeof(MockHoleInjectingTokenFilter), typeof(MockRandomLookaheadTokenFilter), typeof(MockTokenFilter), typeof(MockVariableLengthPayloadFilter), typeof(ValidatingTokenFilter) });
            crazyComponents.addAll(new Type[] { typeof(CachingTokenFilter), typeof(TeeSinkTokenFilter),
                // LUCENENET: Added this specialized BufferedCharFilter which doesn't need a factory
                typeof(BufferedCharFilter)
            });
#pragma warning disable 612, 618
            deprecatedDuplicatedComponents.addAll(new Type[] { typeof(DutchStemFilter), typeof(FrenchStemFilter), typeof(IndicTokenizer) });
#pragma warning restore 612, 618
            oddlyNamedComponents.addAll(new Type[] { typeof(ReversePathHierarchyTokenizer), typeof(SnowballFilter), typeof(PatternKeywordMarkerFilter), typeof(SetKeywordMarkerFilter) }); // this is called SnowballPorterFilterFactory -  this is supported via an option to PathHierarchyTokenizer's factory
        }

        // these are 'crazy' components like cachingtokenfilter. does it make sense to add factories for these?
        private static readonly ISet<Type> crazyComponents = new HashSet<Type>();

        // these are deprecated components that are just exact dups of other functionality: they dont need factories
        // (they never had them)
        private static readonly ISet<Type> deprecatedDuplicatedComponents = new HashSet<Type>();

        // these are oddly-named (either the actual analyzer, or its factory)
        // they do actually have factories.
        // TODO: clean this up!
        private static readonly ISet<Type> oddlyNamedComponents = new HashSet<Type>();

        private static readonly IResourceLoader loader = new StringMockResourceLoader("");

        [Test]
        public virtual void Test()
        {
            IList<Type> analysisClasses = typeof(StandardAnalyzer).GetTypeInfo().Assembly.GetTypes()
                    .Where(c =>
                    {
                        var typeInfo = c.GetTypeInfo();

                        return !typeInfo.IsAbstract && typeInfo.IsPublic && !typeInfo.IsInterface && typeInfo.IsClass && (typeInfo.GetCustomAttribute<ObsoleteAttribute>() == null)
                            && !testComponents.Contains(c) && !crazyComponents.Contains(c) && !oddlyNamedComponents.Contains(c) && !deprecatedDuplicatedComponents.Contains(c)
                            && (typeInfo.IsSubclassOf(typeof(Tokenizer)) || typeInfo.IsSubclassOf(typeof(TokenFilter)) || typeInfo.IsSubclassOf(typeof(CharFilter)));
                    })
                    .ToList();


            foreach (Type c in analysisClasses)
            {

                IDictionary<string, string> args = new Dictionary<string, string>();
                args["luceneMatchVersion"] = TEST_VERSION_CURRENT.ToString();

                if (c.GetTypeInfo().IsSubclassOf(typeof(Tokenizer)))
                {
                    string clazzName = c.Name;
                    assertTrue(clazzName.EndsWith("Tokenizer", StringComparison.Ordinal));
                    string simpleName = clazzName.Substring(0, clazzName.Length - 9);
                    assertNotNull(TokenizerFactory.LookupClass(simpleName));
                    TokenizerFactory instance = null;
                    try
                    {
                        instance = TokenizerFactory.ForName(simpleName, args);
                        assertNotNull(instance);
                        if (instance is IResourceLoaderAware)
                        {
                            ((IResourceLoaderAware)instance).Inform(loader);
                        }
                        assertSame(c, instance.Create(new StringReader("")).GetType());
                    }
                    catch (System.ArgumentException e)
                    {
                        if (e.InnerException is MissingMethodException)
                        {
                            // there is no corresponding ctor available
                            throw e;
                        }
                        // TODO: For now pass because some factories have not yet a default config that always works
                    }
                }
                else if (c.GetTypeInfo().IsSubclassOf(typeof(TokenFilter)))
                {
                    string clazzName = c.Name;
                    assertTrue(clazzName.EndsWith("Filter", StringComparison.Ordinal));
                    string simpleName = clazzName.Substring(0, clazzName.Length - (clazzName.EndsWith("TokenFilter", StringComparison.Ordinal) ? 11 : 6));
                    assertNotNull(TokenFilterFactory.LookupClass(simpleName));
                    TokenFilterFactory instance = null;
                    try
                    {
                        instance = TokenFilterFactory.ForName(simpleName, args);
                        assertNotNull(instance);
                        if (instance is IResourceLoaderAware)
                        {
                            ((IResourceLoaderAware)instance).Inform(loader);
                        }
                        Type createdClazz = instance.Create(new KeywordTokenizer(new StringReader(""))).GetType();
                        // only check instance if factory have wrapped at all!
                        if (typeof(KeywordTokenizer) != createdClazz)
                        {
                            assertSame(c, createdClazz);
                        }
                    }
                    catch (System.ArgumentException e)
                    {
                        if (e.InnerException is MissingMethodException)
                        {
                            // there is no corresponding ctor available
                            throw e;
                        }
                        // TODO: For now pass because some factories have not yet a default config that always works
                    }
                }
                else if (c.GetTypeInfo().IsSubclassOf(typeof(CharFilter)))
                {
                    string clazzName = c.Name;
                    assertTrue(clazzName.EndsWith("CharFilter", StringComparison.Ordinal));
                    string simpleName = clazzName.Substring(0, clazzName.Length - 10);
                    assertNotNull(CharFilterFactory.LookupClass(simpleName));
                    CharFilterFactory instance = null;
                    try
                    {
                        instance = CharFilterFactory.ForName(simpleName, args);
                        assertNotNull(instance);
                        if (instance is IResourceLoaderAware)
                        {
                            ((IResourceLoaderAware)instance).Inform(loader);
                        }
                        Type createdClazz = instance.Create(new StringReader("")).GetType();
                        // only check instance if factory have wrapped at all!
                        if (typeof(StringReader) != createdClazz)
                        {
                            assertSame(c, createdClazz);
                        }
                    }
                    catch (System.ArgumentException e)
                    {
                        if (e.InnerException is MissingMethodException)
                        {
                            // there is no corresponding ctor available
                            throw e;
                        }
                        // TODO: For now pass because some factories have not yet a default config that always works
                    }
                }
            }
        }
    }
}