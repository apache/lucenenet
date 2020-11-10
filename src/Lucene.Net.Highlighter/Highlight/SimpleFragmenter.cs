using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;

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
    /// <see cref="IFragmenter"/> implementation which breaks text up into same-size
    /// fragments with no concerns over spotting sentence boundaries.
    /// </summary>
    public class SimpleFragmenter : IFragmenter
    {
        private const int DEFAULT_FRAGMENT_SIZE = 100;
        private int currentNumFrags;
        private int fragmentSize;
        private IOffsetAttribute offsetAtt;

        public SimpleFragmenter() : this(DEFAULT_FRAGMENT_SIZE) { }

        /// <param name="fragmentSize">size in number of characters of each fragment</param>
        public SimpleFragmenter(int fragmentSize)
        {
            // LUCENENET NOTE: Must not set the property directly
            // in case the user decides to override it and produce an excepetion.
            // Therefore, an auto-implemented property is unacceptable.
            this.fragmentSize = fragmentSize;
        }

        /// <summary>
        /// <seealso cref="IFragmenter.Start(string, TokenStream)"/>
        /// </summary>
        public virtual void Start(string originalText, TokenStream stream)
        {
            offsetAtt = stream.AddAttribute<IOffsetAttribute>();
            currentNumFrags = 1;
        }

        /// <summary>
        /// <seealso cref="IFragmenter.IsNewFragment()"/>
        /// </summary>
        public virtual bool IsNewFragment()
        {
            bool isNewFrag = offsetAtt.EndOffset >= (FragmentSize*currentNumFrags);
            if (isNewFrag)
            {
                currentNumFrags++;
            }
            return isNewFrag;
        }

        /// <summary>
        /// Gets or Sets size in number of characters of each fragment
        /// </summary>
        public virtual int FragmentSize
        {
            get => fragmentSize;
            set => fragmentSize = value;
        }
    }
}
