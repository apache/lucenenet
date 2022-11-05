using Lucene.Net.Queries.Function;
using System.Diagnostics.CodeAnalysis;

namespace Lucene.Net.Expressions
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

    /// <summary>Binds variable names in expressions to actual data.</summary>
    /// <remarks>
    /// Binds variable names in expressions to actual data.
    /// <para/>
    /// These are typically DocValues fields/FieldCache, the document's
    /// relevance score, or other <see cref="ValueSource"/>s.
    /// @lucene.experimental
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1012:Abstract types should not have constructors", Justification = "Required for Reflection")]
    public abstract class Bindings
    {
        /// <summary>Sole constructor.</summary>
        /// <remarks>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </remarks>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("CodeQuality", "S3442:\"abstract\" classes should not have \"public\" constructors", Justification = "Public is required for Relection")]
        public Bindings() // LUCENENET NOTE: This must be public for the Reflection code to work right.
        {
        }

        /// <summary>
        /// Returns a <see cref="ValueSource"/> bound to the variable name.
        /// </summary>
        public abstract ValueSource GetValueSource(string name);

        /// <summary>
        /// Returns a <see cref="ValueSource"/> over relevance scores
        /// </summary>
        protected static ValueSource GetScoreValueSource() // LUCENENET: CA1822: Mark members as static
        {
            return new ScoreValueSource();
        }
    }
}
