// Lucene version compatibility level 4.8.1
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
    /// Abstract parent class for analysis factories that create <see cref="CharFilter"/>
    /// instances.
    /// </summary>
    public abstract class CharFilterFactory : AbstractAnalysisFactory
    {
        private static readonly AnalysisSPILoader<CharFilterFactory> loader = new AnalysisSPILoader<CharFilterFactory>();

        /// <summary>
        /// looks up a charfilter by name from the host project's dependent assemblies </summary>
        public static CharFilterFactory ForName(string name, IDictionary<string, string> args)
        {
            return loader.NewInstance(name, args);
        }

        /// <summary>
        /// looks up a charfilter class by name from the host project's dependent assemblies </summary>
        public static Type LookupClass(string name)
        {
            return loader.LookupClass(name);
        }

        /// <summary>
        /// returns a list of all available charfilter names </summary>
        public static ICollection<string> AvailableCharFilters => loader.AvailableServices;

        /// <summary>
        /// Reloads the factory list.
        /// Changes to the factories are visible after the method ends, all
        /// iterators (<see cref="AvailableCharFilters"/>,...) stay consistent. 
        /// 
        /// <para><b>NOTE:</b> Only new factories are added, existing ones are
        /// never removed or replaced.
        /// 
        /// </para>
        /// <para><em>This method is expensive and should only be called for discovery
        /// of new factories on the given classpath/classloader!</em>
        /// </para>
        /// </summary>
        public static void ReloadCharFilters()
        {
            loader.Reload();
        }

        /// <summary>
        /// Initialize this factory via a set of key-value pairs.
        /// </summary>
        protected CharFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
        }

        /// <summary>
        /// Wraps the given <see cref="TextReader"/> with a <see cref="CharFilter"/>. </summary>
        public abstract TextReader Create(TextReader input);
    }
}