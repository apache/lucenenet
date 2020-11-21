// Lucene version compatibility level 7.1.0
using ICU4N.Text;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;

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
    /// Factory for <see cref="ICUTransformFilter"/>.
    /// </summary>
    /// <remarks>
    /// Supports the following attributes:
    /// <list type="table">
    ///     <item><item>id (mandatory)</item><description>A Transliterator ID, one from <see cref="Transliterator.GetAvailableIDs()"/></description>.</item>
    ///     <item><item>direction (optional)</item><description>Either 'forward' or 'reverse'. Default is forward.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Transliterator"/>
    [ExceptionToClassNameConvention]
    public class ICUTransformFilterFactory : TokenFilterFactory, IMultiTermAwareComponent
    {
        private readonly Transliterator transliterator;

        // TODO: add support for custom rules
        /// <summary>Creates a new <see cref="ICUTransformFilterFactory"/>.</summary>
        public ICUTransformFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            string id = Require(args, "id");
            string direction = Get(args, "direction", new string[] { "forward", "reverse" }, "forward", false);
            TransliterationDirection dir = "forward".Equals(direction, StringComparison.Ordinal) ? Transliterator.Forward : Transliterator.Reverse;
            transliterator = Transliterator.GetInstance(id, dir);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new ICUTransformFilter(input, transliterator);
        }

        public virtual AbstractAnalysisFactory GetMultiTermComponent()
        {
            return this;
        }
    }
}
