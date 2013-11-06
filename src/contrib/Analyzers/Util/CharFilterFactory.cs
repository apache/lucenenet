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
using System.IO;

namespace Lucene.Net.Analysis.Util
{
    /// <summary>
    /// Abstract parent class for analysis factories that create
    /// {@link CharFilter} instances.
    /// </summary>
    public abstract class CharFilterFactory : AbstractAnalysisFactory
    {
        private static readonly AnalysisSPILoader<CharFilterFactory> Loader =
            new AnalysisSPILoader<CharFilterFactory>(typeof (CharFilterFactory));

        /// <summary>
        /// Looks up a CharFilter by name from context classpath.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns>Returns an instance of the looked up CharFilter.</returns>
        public static CharFilterFactory ForName(string name, IDictionary<string, string> args)
        {
            return Loader.NewInstance(name, args);
        }

        /// <summary>
        /// Looks up a CharFilter class by name from context classpath.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Returns the type of the looked up CharFilter.</returns>
        public static Type LookupType(string name)
        {
            return Loader.LookupClass(name);
        }

        /// <summary>
        /// Returns a list of all available CharFilter names.
        /// </summary>
        /// <returns>Returns a list of all available CharFilter names.</returns>
        public static ICollection<string> AvailableCharFilters()
        {
            return Loader.AvailableServices;
        }

        /// <summary>
        /// Reloads the factory list from the given {@link ClassLoader}.
        /// Changes to the factories are visible after the method ends, all
        /// iterators ({@link #availableCharFilters()},...) stay consistent. 
        /// 
        /// <p><b>NOTE:</b> Only new factories are added, existing ones are
        /// never removed or replaced.
        /// 
        /// <p><em>This method is expensive and should only be called for discovery
        /// of new factories on the given classpath/classloader!</em></p></p>
        /// </summary>
        public static void ReloadCharFilters()
        {
            Loader.Reload();
        }

        /// <summary>
        /// Initialize this factory via a set of key-value pairs.
        /// </summary>
        /// <param name="args"></param>
        protected CharFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            
        }

        /// <summary>
        /// Wraps the given TextReader with a CharFilter.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public abstract StreamReader Create(StreamReader input);
    }
}
