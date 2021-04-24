// Lucene version compatibility level 4.8.1
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Lucene.Net.Util;
using Version = Lucene.Net.Util.LuceneVersion;

namespace Lucene.Net.Analysis.Util
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
    /// Base class for testing tokenstream factories. 
    /// <para>
    /// Example usage:
    /// <code><pre>
    ///   Reader reader = new StringReader("Some Text to Analyze");
    ///   reader = charFilterFactory("htmlstrip").create(reader);
    ///   TokenStream stream = tokenizerFactory("standard").create(reader);
    ///   stream = tokenFilterFactory("lowercase").create(stream);
    ///   stream = tokenFilterFactory("asciifolding").create(stream);
    ///   AssertTokenStreamContents(stream, new String[] { "some", "text", "to", "analyze" });
    /// </pre></code>
    /// </para>
    /// </summary>
    // TODO: this has to be here, since the abstract factories are not in lucene-core,
    // so test-framework doesnt know about them...
    // this also means we currently cannot use this in other analysis modules :(
    // TODO: maybe after we improve the abstract factory/SPI apis, they can sit in core and resolve this.
    public abstract class BaseTokenStreamFactoryTestCase : BaseTokenStreamTestCase
    {
        private AbstractAnalysisFactory AnalysisFactory(Type clazz, Version matchVersion, IResourceLoader loader, params string[] keysAndValues)
        {
            if (keysAndValues.Length % 2 == 1)
            {
                throw new ArgumentException("invalid keysAndValues map");
            }
            string previous;
            IDictionary<string, string> args = new Dictionary<string, string>();
            for (int i = 0; i < keysAndValues.Length; i += 2)
            {
                if (args.TryGetValue(keysAndValues[i], out previous))
                {
                    fail("duplicate values for key: " + keysAndValues[i]);
                }
                args[keysAndValues[i]] = keysAndValues[i + 1];
            }

            if (args.TryGetValue("luceneMatchVersion", out previous))
            {
                fail("duplicate values for key: luceneMatchVersion");
            }
            args["luceneMatchVersion"] = matchVersion.ToString();

            AbstractAnalysisFactory factory = null;
            try
            {
                factory = (AbstractAnalysisFactory)Activator.CreateInstance(clazz, args);
            }
            catch (Exception e) when (e.IsInvocationTargetException())
            {
                // to simplify tests that check for illegal parameters
                if (e.InnerException is ArgumentException argumentException)
                {
                    ExceptionDispatchInfo.Capture(argumentException).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
                }
                else
                {
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                }
            }
            if (factory is IResourceLoaderAware resourceLoaderAware)
            {
                resourceLoaderAware.Inform(loader);
            }
            return factory;
        }

        /// <summary>
        /// Returns a fully initialized TokenizerFactory with the specified name and key-value arguments.
        /// <seealso cref="ClasspathResourceLoader"/> is used for loading resources, so any required ones should
        /// be on the test classpath.
        /// </summary>
        protected internal virtual TokenizerFactory TokenizerFactory(string name, params string[] keysAndValues)
        {
            return TokenizerFactory(name, TEST_VERSION_CURRENT, keysAndValues);
        }

        /// <summary>
        /// Returns a fully initialized TokenizerFactory with the specified name and key-value arguments.
        /// <seealso cref="ClasspathResourceLoader"/> is used for loading resources, so any required ones should
        /// be on the test classpath.
        /// </summary>
        protected internal virtual TokenizerFactory TokenizerFactory(string name, Version version, params string[] keysAndValues)
        {
            return TokenizerFactory(name, version, new ClasspathResourceLoader(this.GetType()), keysAndValues);
        }

        /// <summary>
        /// Returns a fully initialized TokenizerFactory with the specified name, version, resource loader, 
        /// and key-value arguments.
        /// </summary>
        protected internal virtual TokenizerFactory TokenizerFactory(string name, Version matchVersion, IResourceLoader loader, params string[] keysAndValues)
        {
            return (TokenizerFactory)AnalysisFactory(Lucene.Net.Analysis.Util.TokenizerFactory.LookupClass(name), matchVersion, loader, keysAndValues);
        }

        /// <summary>
        /// Returns a fully initialized TokenFilterFactory with the specified name and key-value arguments.
        /// <seealso cref="ClasspathResourceLoader"/> is used for loading resources, so any required ones should
        /// be on the test classpath.
        /// </summary>
        protected internal virtual TokenFilterFactory TokenFilterFactory(string name, Version version, params string[] keysAndValues)
        {
            return TokenFilterFactory(name, version, new ClasspathResourceLoader(this.GetType()), keysAndValues);
        }

        /// <summary>
        /// Returns a fully initialized TokenFilterFactory with the specified name and key-value arguments.
        /// <seealso cref="ClasspathResourceLoader"/> is used for loading resources, so any required ones should
        /// be on the test classpath.
        /// </summary>
        protected internal virtual TokenFilterFactory TokenFilterFactory(string name, params string[] keysAndValues)
        {
            return TokenFilterFactory(name, TEST_VERSION_CURRENT, keysAndValues);
        }

        /// <summary>
        /// Returns a fully initialized TokenFilterFactory with the specified name, version, resource loader, 
        /// and key-value arguments.
        /// </summary>
        protected internal virtual TokenFilterFactory TokenFilterFactory(string name, Version matchVersion, IResourceLoader loader, params string[] keysAndValues)
        {
            return (TokenFilterFactory)AnalysisFactory(Lucene.Net.Analysis.Util.TokenFilterFactory.LookupClass(name), matchVersion, loader, keysAndValues);
        }

        /// <summary>
        /// Returns a fully initialized CharFilterFactory with the specified name and key-value arguments.
        /// <seealso cref="ClasspathResourceLoader"/> is used for loading resources, so any required ones should
        /// be on the test classpath.
        /// </summary>
        protected internal virtual CharFilterFactory CharFilterFactory(string name, params string[] keysAndValues)
        {
            return CharFilterFactory(name, TEST_VERSION_CURRENT, new ClasspathResourceLoader(this.GetType()), keysAndValues);
        }

        /// <summary>
        /// Returns a fully initialized CharFilterFactory with the specified name, version, resource loader, 
        /// and key-value arguments.
        /// </summary>
        protected internal virtual CharFilterFactory CharFilterFactory(string name, Version matchVersion, IResourceLoader loader, params string[] keysAndValues)
        {
            return (CharFilterFactory)AnalysisFactory(Lucene.Net.Analysis.Util.CharFilterFactory.LookupClass(name), matchVersion, loader, keysAndValues);
        }
    }
}