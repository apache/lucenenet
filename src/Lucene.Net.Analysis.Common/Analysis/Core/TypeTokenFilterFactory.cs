using System.Collections.Generic;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;

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
    /// Factory class for <seealso cref="TypeTokenFilter"/>.
    /// <pre class="prettyprint">
    /// &lt;fieldType name="chars" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.StandardTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.TypeTokenFilterFactory" types="stoptypes.txt"
    ///                   useWhitelist="false"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</pre>
    /// </summary>
    public class TypeTokenFilterFactory : TokenFilterFactory, ResourceLoaderAware
    {
        private readonly bool useWhitelist;
        private readonly bool enablePositionIncrements;
        private readonly string stopTypesFiles;
        private HashSet<string> stopTypes;

        /// <summary>
        /// Creates a new TypeTokenFilterFactory </summary>
        public TypeTokenFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            stopTypesFiles = require(args, "types");
            enablePositionIncrements = getBoolean(args, "enablePositionIncrements", true);
            useWhitelist = getBoolean(args, "useWhitelist", false);
            if (args.Count > 0)
            {
                throw new System.ArgumentException("Unknown parameters: " + args);
            }
        }

        public virtual void Inform(ResourceLoader loader)
        {
            IList<string> files = splitFileNames(stopTypesFiles);
            if (files.Count > 0)
            {
                stopTypes = new HashSet<string>();
                foreach (string file in files)
                {
                    IList<string> typesLines = getLines(loader, file.Trim());
                    stopTypes.AddAll(typesLines);
                }
            }
        }

        public virtual bool EnablePositionIncrements
        {
            get
            {
                return enablePositionIncrements;
            }
        }

        public virtual HashSet<string> StopTypes
        {
            get
            {
                return stopTypes;
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            TokenStream filter = new TypeTokenFilter(luceneMatchVersion, enablePositionIncrements, input, stopTypes, useWhitelist);
            return filter;
        }
    }

}