using Lucene.Net.Util.Automaton;
using System;
using System.Text;

namespace Lucene.Net.Search
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

    using Automaton = Lucene.Net.Util.Automaton.Automaton;
    using IAutomatonProvider = Lucene.Net.Util.Automaton.IAutomatonProvider;
    using RegExp = Lucene.Net.Util.Automaton.RegExp;
    using Term = Lucene.Net.Index.Term;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// A fast regular expression query based on the
    /// <see cref="Lucene.Net.Util.Automaton"/> package.
    /// <list type="bullet">
    ///     <item><description>Comparisons are <a
    ///         href="http://tusker.org/regex/regex_benchmark.html">fast</a></description></item>
    ///     <item><description>The term dictionary is enumerated in an intelligent way, to avoid
    ///         comparisons. See <see cref="AutomatonQuery"/> for more details.</description></item>
    /// </list>
    /// <para>
    /// The supported syntax is documented in the <see cref="RegExp"/> class.
    /// Note this might be different than other regular expression implementations.
    /// For some alternatives with different syntax, look under the sandbox.
    /// </para>
    /// <para>
    /// Note this query can be slow, as it needs to iterate over many terms. In order
    /// to prevent extremely slow <see cref="RegexpQuery"/>s, a <see cref="RegExp"/> term should not start with
    /// the expression <c>.*</c>
    /// </para>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="RegExp"/>
    public class RegexpQuery : AutomatonQuery
    {
        /// <summary>
        /// A provider that provides no named automata
        /// </summary>
        private static readonly IAutomatonProvider defaultProvider = new AutomatonProviderAnonymousClass();

        private sealed class AutomatonProviderAnonymousClass : IAutomatonProvider
        {
            public Automaton GetAutomaton(string name)
            {
                return null;
            }
        }

        /// <summary>
        /// Constructs a query for terms matching <paramref name="term"/>.
        /// <para>
        /// By default, all regular expression features are enabled.
        /// </para>
        /// </summary>
        /// <param name="term"> Regular expression. </param>
        public RegexpQuery(Term term)
            : this(term, RegExpSyntax.ALL)
        {
        }

        /// <summary>
        /// Constructs a query for terms matching <paramref name="term"/>.
        /// </summary>
        /// <param name="term"> Regular expression. </param>
        /// <param name="flags"> Optional <see cref="RegExp"/> features from <see cref="RegExpSyntax"/> </param>
        public RegexpQuery(Term term, RegExpSyntax flags)
            : this(term, flags, defaultProvider)
        {
        }

        /// <summary>
        /// Constructs a query for terms matching <paramref name="term"/>.
        /// </summary>
        /// <param name="term"> Regular expression. </param>
        /// <param name="flags"> Optional <see cref="RegExp"/> features from <see cref="RegExpSyntax"/> </param>
        /// <param name="provider"> Custom <see cref="IAutomatonProvider"/> for named automata </param>
        public RegexpQuery(Term term, RegExpSyntax flags, IAutomatonProvider provider)
            : base(term, (new RegExp(term.Text, flags)).ToAutomaton(provider))
        {
        }

        /// <summary>
        /// Prints a user-readable version of this query. </summary>
        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            if (!m_term.Field.Equals(field, StringComparison.Ordinal))
            {
                buffer.Append(m_term.Field);
                buffer.Append(':');
            }
            buffer.Append('/');
            buffer.Append(m_term.Text);
            buffer.Append('/');
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }
    }
}