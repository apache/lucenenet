using Lucene.Net.Analysis;
using System.IO;

namespace Lucene.Net.Search.Highlight
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
    /// A <see cref="IScorer"/> is responsible for scoring a stream of tokens. These token scores
    /// can then be used to compute <see cref="TextFragment"/> scores.
    /// </summary>
    public interface IScorer
    {
        /// <summary>
        /// Called to init the Scorer with a <see cref="TokenStream"/>. You can grab references to
        /// the attributes you are interested in here and access them from <see cref="GetTokenScore()"/>.
        /// </summary>
        /// <param name="tokenStream">the <see cref="TokenStream"/> that will be scored.</param>
        /// <returns>
        /// either a <see cref="TokenStream"/> that the <see cref="Highlighter"/> should continue using (eg
        /// if you read the tokenSream in this method) or null to continue
        /// using the same <see cref="TokenStream"/> that was passed in.
        /// </returns> 
        /// <exception cref="IOException">If there is a low-level I/O error</exception>
        TokenStream Init(TokenStream tokenStream);

        /// <summary>
        /// Called when a new fragment is started for consideration.
        /// </summary>
        /// <param name="newFragment">the fragment that will be scored next</param>
        void StartFragment(TextFragment newFragment);

        /// <summary>
        /// Called for each token in the current fragment. The <see cref="Highlighter"/> will
        /// increment the <see cref="TokenStream"/> passed to init on every call.
        /// </summary>
        /// <returns>a score which is passed to the <see cref="Highlighter"/> class to influence the
        /// mark-up of the text (this return value is NOT used to score the
        /// fragment)</returns> 
        float GetTokenScore();

        ///<summary>
        /// Called when the <see cref="Highlighter"/> has no more tokens for the current fragment -
        /// the <see cref="IScorer"/> returns the weighting it has derived for the most recent
        /// fragment, typically based on the results of <see cref="GetTokenScore()"/>.
        /// </summary>
        float FragmentScore { get; }
    }
}