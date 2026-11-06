using Lucene.Net.Analysis.Ko.Dict;

namespace Lucene.Net.Analysis.Ko
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
    /// A token that was generated from a compound.
    /// </summary>
    public class DecompoundToken : Token
    {
        private readonly POS.Tag posTag;

        /// <summary>
        /// Creates a new DecompoundToken
        /// </summary>
        /// <param name="posTag"> The part of speech of the token. </param>
        /// <param name="surfaceForm"> The surface form of the token. </param>
        /// <param name="startOffset"> The start offset of the token in the analyzed text. </param>
        /// <param name="endOffset"> The end offset of the token in the analyzed text. </param>
        public DecompoundToken(POS.Tag posTag, char[] surfaceForm, int startOffset, int endOffset)
            : base(surfaceForm, 0, surfaceForm.Length, startOffset, endOffset)
        {
            this.posTag = posTag;
        }

        public override string ToString() {
            return "DecompoundToken(\"" + GetSurfaceForm + "\" pos=" + GetStartOffset() + " length=" + GetLength +
                   " startOffset=" + GetStartOffset() + " endOffset=" + GetEndOffset() + ")";
        }

        public override POS.Type GetPOSType()
        {
            return POS.Type.MORPHEME;
        }

        public override POS.Tag GetLeftPOS()
        {
            return posTag;
        }

        public override POS.Tag GetRightPOS()
        {
            return posTag;
        }

        public override string GetReading()
        {
            return null;
        }

        public override IDictionary.Morpheme[] GetMorphemes()
        {
            return null;
        }
    }
}