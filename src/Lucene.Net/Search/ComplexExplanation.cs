using System;

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

    /// <summary>
    /// Expert: Describes the score computation for document and query, and
    /// can distinguish a match independent of a positive value.
    /// </summary>
    public class ComplexExplanation : Explanation
    {
        private bool? match;

        public ComplexExplanation()
            : base()
        {
        }

        public ComplexExplanation(bool match, float value, string description)
            : base(value, description)
        {
            // NOTE: use of "boolean" instead of "Boolean" in params is conscious
            // choice to encourage clients to be specific.
            this.match = Convert.ToBoolean(match);
        }

        /// <summary>
        /// Gets or Sets the match status assigned to this explanation node. 
        /// May be <c>null</c> if match status is unknown.
        /// </summary>
        public virtual bool? Match
        {
            get => match;
            set => this.match = value;
        }

        /// <summary>
        /// Indicates whether or not this <see cref="Explanation"/> models a good match.
        ///
        /// <para>
        /// If the match status is explicitly set (i.e.: not null) this method
        /// uses it; otherwise it defers to the superclass.
        /// </para> </summary>
        /// <seealso cref="Match"/>
        public override bool IsMatch
        {
            get
            {
                bool? m = Match;
                return (null != m ? (bool)m : base.IsMatch);
            }
        }

        protected override string GetSummary()
        {
            if (null == Match)
            {
                return base.GetSummary();
            }

            return Value + " = " + (IsMatch ? "(MATCH) " : "(NON-MATCH) ") + Description;
        }
    }
}