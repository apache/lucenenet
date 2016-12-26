using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    /// Sanity check some things about all factories,
    /// we do our best to see if we can sanely initialize it with
    /// no parameters and smoke test it, etc.
    /// </summary>
    // LUCENETODO: move this, TestRandomChains, and TestAllAnalyzersHaveFactories
    // to an integration test module that sucks in all analysis modules.
    // currently the only way to do this is via eclipse etc (LUCENE-3974)
    [TestFixture]
    public class TestFactories : BaseTokenStreamTestCase
    {
        [Test]
        public virtual void Test()
        {
            foreach (string tokenizer in TokenizerFactory.AvailableTokenizers)
            {
                DoTestTokenizer(tokenizer);
            }

            foreach (string tokenFilter in TokenFilterFactory.AvailableTokenFilters)
            {
                DoTestTokenFilter(tokenFilter);
            }

            foreach (string charFilter in CharFilterFactory.AvailableCharFilters)
            {
                DoTestCharFilter(charFilter);
            }
        }

        private void DoTestTokenizer(string tokenizer)
        {
            var factoryClazz = TokenizerFactory.LookupClass(tokenizer);
            TokenizerFactory factory = (TokenizerFactory)Initialize(factoryClazz);
            if (factory != null)
            {
                // we managed to fully create an instance. check a few more things:

                // if it implements MultiTermAware, sanity check its impl
                if (factory is IMultiTermAwareComponent)
                {
                    AbstractAnalysisFactory mtc = ((IMultiTermAwareComponent)factory).MultiTermComponent;
                    assertNotNull(mtc);
                    // its not ok to return e.g. a charfilter here: but a tokenizer could wrap a filter around it
                    assertFalse(mtc is CharFilterFactory);
                }

                // beast it just a little, it shouldnt throw exceptions:
                // (it should have thrown them in initialize)
                CheckRandomData(Random(), new FactoryAnalyzer(factory, null, null), 100, 20, false, false);
            }
        }

        private void DoTestTokenFilter(string tokenfilter)
        {
            var factoryClazz = TokenFilterFactory.LookupClass(tokenfilter);
            TokenFilterFactory factory = (TokenFilterFactory)Initialize(factoryClazz);
            if (factory != null)
            {
                // we managed to fully create an instance. check a few more things:

                // if it implements MultiTermAware, sanity check its impl
                if (factory is IMultiTermAwareComponent)
                {
                    AbstractAnalysisFactory mtc = ((IMultiTermAwareComponent)factory).MultiTermComponent;
                    assertNotNull(mtc);
                    // its not ok to return a charfilter or tokenizer here, this makes no sense
                    assertTrue(mtc is TokenFilterFactory);
                }

                // beast it just a little, it shouldnt throw exceptions:
                // (it should have thrown them in initialize)
                CheckRandomData(Random(), new FactoryAnalyzer(assertingTokenizer, factory, null), 100, 20, false, false);
            }
        }

        private void DoTestCharFilter(string charfilter)
        {
            var factoryClazz = CharFilterFactory.LookupClass(charfilter);
            CharFilterFactory factory = (CharFilterFactory)Initialize(factoryClazz);
            if (factory != null)
            {
                // we managed to fully create an instance. check a few more things:

                // if it implements MultiTermAware, sanity check its impl
                if (factory is IMultiTermAwareComponent)
                {
                    AbstractAnalysisFactory mtc = ((IMultiTermAwareComponent)factory).MultiTermComponent;
                    assertNotNull(mtc);
                    // its not ok to return a tokenizer or tokenfilter here, this makes no sense
                    assertTrue(mtc is CharFilterFactory);
                }

                // beast it just a little, it shouldnt throw exceptions:
                // (it should have thrown them in initialize)
                CheckRandomData(Random(), new FactoryAnalyzer(assertingTokenizer, null, factory), 100, 20, false, false);
            }
        }

        /// <summary>
        /// tries to initialize a factory with no arguments </summary>
        private AbstractAnalysisFactory Initialize(Type factoryClazz)
        {
            IDictionary<string, string> args = new Dictionary<string, string>();
            args["luceneMatchVersion"] = TEST_VERSION_CURRENT.ToString();

            ConstructorInfo ctor;
            try
            {
                ctor = factoryClazz.GetConstructor(new Type[] { typeof(IDictionary<string, string>) });
            }
            catch (Exception)
            {
                throw new Exception("factory '" + factoryClazz + "' does not have a proper ctor!");
            }

            AbstractAnalysisFactory factory = null;
            try
            {
                factory = (AbstractAnalysisFactory)ctor.Invoke(new object[] { args });
            }
            catch (TypeInitializationException e)
            {
                throw new Exception(e.Message, e);
            }
            catch (MethodAccessException e)
            {
                throw new Exception(e.Message, e);
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException is System.ArgumentException)
                {
                    // its ok if we dont provide the right parameters to throw this
                    return null;
                }
            }

            if (factory is IResourceLoaderAware)
            {
                try
                {
                    ((IResourceLoaderAware)factory).Inform(new StringMockResourceLoader(""));
                }
                catch (IOException)
                {
                    // its ok if the right files arent available or whatever to throw this
                }
                catch (System.ArgumentException)
                {
                    // is this ok? I guess so
                }
            }
            return factory;
        }

        // some silly classes just so we can use checkRandomData
        private TokenizerFactory assertingTokenizer = new AnonymousInnerClassHelperTokenizerFactory(new Dictionary<string, string>());

        private class AnonymousInnerClassHelperTokenizerFactory : TokenizerFactory
        {
            public AnonymousInnerClassHelperTokenizerFactory(IDictionary<string, string> java) : base(java)
            {
            }

            public override Tokenizer Create(AttributeSource.AttributeFactory factory, TextReader input)
            {
                return new MockTokenizer(factory, input);
            }
        }

        private class FactoryAnalyzer : Analyzer
        {
            internal readonly TokenizerFactory tokenizer;
            internal readonly CharFilterFactory charFilter;
            internal readonly TokenFilterFactory tokenfilter;

            internal FactoryAnalyzer(TokenizerFactory tokenizer, TokenFilterFactory tokenfilter, CharFilterFactory charFilter)
            {
                Debug.Assert(tokenizer != null);
                this.tokenizer = tokenizer;
                this.charFilter = charFilter;
                this.tokenfilter = tokenfilter;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tf = tokenizer.Create(reader);
                if (tokenfilter != null)
                {
                    return new TokenStreamComponents(tf, tokenfilter.Create(tf));
                }
                else
                {
                    return new TokenStreamComponents(tf);
                }
            }


            protected internal override TextReader InitReader(string fieldName, TextReader reader)
            {
                if (charFilter != null)
                {
                    return charFilter.Create(reader);
                }
                else
                {
                    return reader;
                }
            }
        }
    }
}