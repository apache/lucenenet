/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Text;

namespace Lucene.Net.Search.Highlight
{
    /// <summary> Low-level class used to record information about a section of a document 
    /// with a score.
    /// </summary>
    public class TextFragment
    {
        private StringBuilder markedUpText;
        private int fragNum;
        private int textStartPos;
        internal int textEndPos;
        private float score;

        public TextFragment(StringBuilder markedUpText, int textStartPos, int fragNum)
        {
            this.markedUpText = markedUpText;
            this.textStartPos = textStartPos;
            this.fragNum = fragNum;
        }

        public float Score
        {
            get { return score; }
            set { this.score = value; }
        }
        
        /// <summary></summary>
        /// <param name="frag2">Fragment to be merged into this one</param>
        public void Merge(TextFragment frag2)
        {
            textEndPos = frag2.textEndPos;
            score = Math.Max(score, frag2.score);
        }

        /// <summary>
        /// true if this fragment follows the one passed
        /// </summary>
        public bool Follows(TextFragment fragment)
        {
            return textStartPos == fragment.textEndPos;
        }

        /// <summary>
        /// the fragment sequence number
        /// </summary>
        public int FragNum
        {
            get { return fragNum; }
        }

        /// <summary>
        /// Returns the marked-up text for this text fragment 
        /// </summary>
        public override string ToString()
        {
            return markedUpText.ToString(textStartPos, textEndPos - textStartPos);
        }
    }
}