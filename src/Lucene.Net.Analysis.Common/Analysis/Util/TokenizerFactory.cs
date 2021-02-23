// Lucene version compatibility level 4.8.1
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;

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
    /// Abstract parent class for analysis factories that create <see cref="Tokenizer"/>
    /// instances.
    /// </summary>
    public abstract class TokenizerFactory : AbstractAnalysisFactory
    {
        private static readonly AnalysisSPILoader<TokenizerFactory> loader = new AnalysisSPILoader<TokenizerFactory>();

        /// <summary>
        /// looks up a tokenizer by name from the host project's referenced assemblies </summary>
        public static TokenizerFactory ForName(string name, IDictionary<string, string> args)
        {
            return loader.NewInstance(name, args);
        }

        /// <summary>
        /// looks up a tokenizer class by name from the host project's referenced assemblies </summary>
        public static Type LookupClass(string name)
        {
            return loader.LookupClass(name);
        }

        /// <summary>
        /// returns a list of all available tokenizer names from the host project's referenced assemblies </summary>
        public static ICollection<string> AvailableTokenizers => loader.AvailableServices;

        /// <summary>
        /// Reloads the factory list.
        /// Changes to the factories are visible after the method ends, all
        /// iterators (<see cref="AvailableTokenizers"/>,...) stay consistent. 
        /// 
        /// <para><b>NOTE:</b> Only new factories are added, existing ones are
        /// never removed or replaced.
        /// 
        /// </para>
        /// <para><em>This method is expensive and should only be called for discovery
        /// of new factories on the given classpath/classloader!</em>
        /// </para>
        /// </summary>
        public static void ReloadTokenizers()
        {
            loader.Reload();
        }

        /// <summary>
        /// Initialize this factory via a set of key-value pairs.
        /// </summary>
        protected TokenizerFactory(IDictionary<string, string> args)
            : base(args)
        {
        }

        /// <summary>
        /// Creates a <see cref="TokenStream"/> of the specified input using the default attribute factory. </summary>
        public Tokenizer Create(TextReader input)
        {
            return Create(AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, input);
        }

        /// <summary>
        /// Creates a <see cref="TokenStream"/> of the specified input using the given <see cref="AttributeSource.AttributeFactory"/> </summary>
        public abstract Tokenizer Create(AttributeSource.AttributeFactory factory, TextReader input);
    }
}