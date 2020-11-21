// Lucene version compatibility level 7.1.0
using ICU4N.Text;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Icu
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
    /// Factory for <see cref="ICUNormalizer2CharFilter"/>.
    /// </summary>
    /// <remarks>
    /// Supports the following attributes:
    /// <list type="table">
    ///     <item>
    ///         <term>name</term>
    ///         <description>
    ///             A <a href="http://unicode.org/reports/tr15/">Unicode Normalization Form</a>, 
    ///             one of 'nfc','nfkc', 'nfkc_cf'. Default is nfkc_cf.
    ///         </description></item>
    ///     <item>
    ///         <term>mode</term>
    ///         <description>
    ///             Either 'compose' or 'decompose'. Default is compose. Use "decompose" with nfc
    ///             or nfkc, to get nfd or nfkd, respectively.
    ///         </description></item>
    ///     <item>
    ///         <term>filter</term>
    ///         <description>
    ///             A <see cref="UnicodeSet"/> pattern. Codepoints outside the set are
    ///             always left unchanged. Default is [] (the null set, no filtering).
    ///         </description>
    ///     </item>
    /// </list>
    /// </remarks>
    /// <seealso cref="ICUNormalizer2CharFilter"/>
    /// <seealso cref="Normalizer2"/>
    /// <seealso cref="FilteredNormalizer2"/>
    [ExceptionToClassNameConvention]
    public class ICUNormalizer2CharFilterFactory : CharFilterFactory, IMultiTermAwareComponent
    {
        private readonly Normalizer2 normalizer;

        /// <summary>Creates a new <see cref="ICUNormalizer2CharFilterFactory"/>.</summary>
        public ICUNormalizer2CharFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            string name = Get(args, "name", "nfkc_cf");
            string mode = Get(args, "mode", new string[] { "compose", "decompose" }, "compose");
            Normalizer2 normalizer = Normalizer2.GetInstance
                (null, name, "compose".Equals(mode, StringComparison.Ordinal) ? Normalizer2Mode.Compose : Normalizer2Mode.Decompose);

            string filter = Get(args, "filter");
            if (filter != null)
            {
                UnicodeSet set = new UnicodeSet(filter);
                if (set.Any())
                {
                    set.Freeze();
                    normalizer = new FilteredNormalizer2(normalizer, set);
                }
            }
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
            this.normalizer = normalizer;
        }

        public override TextReader Create(TextReader input)
        {
            return new ICUNormalizer2CharFilter(input, normalizer);
        }

        public virtual AbstractAnalysisFactory GetMultiTermComponent()
        {
            return this;
        }
    }
}
