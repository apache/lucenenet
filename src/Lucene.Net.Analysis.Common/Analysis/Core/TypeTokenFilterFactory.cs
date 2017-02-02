using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using System.Collections.Generic;
using System.Linq;

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
    /// Factory class for <see cref="TypeTokenFilter"/>.
    /// <code>
    /// &lt;fieldType name="chars" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.StandardTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.TypeTokenFilterFactory" types="stoptypes.txt"
    ///                   useWhitelist="false"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </summary>
    public class TypeTokenFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        private readonly bool useWhitelist;
        private readonly bool enablePositionIncrements;
        private readonly string stopTypesFiles;
        private HashSet<string> stopTypes;

        /// <summary>
        /// Creates a new <see cref="TypeTokenFilterFactory"/> </summary>
        public TypeTokenFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            stopTypesFiles = Require(args, "types");
            enablePositionIncrements = GetBoolean(args, "enablePositionIncrements", true);
            useWhitelist = GetBoolean(args, "useWhitelist", false);
            if (args.Count > 0)
            {
                throw new System.ArgumentException("Unknown parameters: " + args);
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            IList<string> files = SplitFileNames(stopTypesFiles);
            if (files.Count() > 0)
            {
                stopTypes = new HashSet<string>();
                foreach (string file in files)
                {
                    IList<string> typesLines = GetLines(loader, file.Trim());
                    stopTypes.UnionWith(typesLines);
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

        public virtual ICollection<string> StopTypes
        {
            get
            {
                return stopTypes;
            }
        }

        public override TokenStream Create(TokenStream input)
        {
#pragma warning disable 612, 618
            TokenStream filter = new TypeTokenFilter(m_luceneMatchVersion, enablePositionIncrements, input, stopTypes, useWhitelist);
#pragma warning restore 612, 618
            return filter;
        }
    }
}