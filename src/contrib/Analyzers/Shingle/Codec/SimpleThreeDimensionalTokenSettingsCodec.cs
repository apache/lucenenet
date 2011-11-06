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

using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analyzers.Payloads;
using Lucene.Net.Index;

namespace Lucene.Net.Analyzers.Shingle.Codec
{
    /// <summary>
    /// A full featured codec not to be used for something serious.
    /// 
    /// It takes complete control of
    /// payload for weight
    /// and the bit flags for positioning in the matrix.
    /// 
    /// Mainly exist for demonstrational purposes.
    /// </summary>
    public class SimpleThreeDimensionalTokenSettingsCodec : TokenSettingsCodec
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns>the token flags int value as TokenPosition</returns>
        public override TokenPositioner GetTokenPositioner(Token token)
        {
            switch (token.GetFlags())
            {
                case 0:
                    return TokenPositioner.NewColumn;
                case 1:
                    return TokenPositioner.NewRow;
                case 2:
                    return TokenPositioner.SameRow;
            }
            throw new IOException("Unknown matrix positioning of token " + token);
        }

        /// <summary>
        /// Sets the TokenPositioner as token flags int value.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="tokenPositioner"></param>
        public override void SetTokenPositioner(Token token, TokenPositioner tokenPositioner)
        {
            token.SetFlags(tokenPositioner.Index);
        }

        /// <summary>
        /// Returns a 32 bit float from the payload, or 1f it null.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public override float GetWeight(Token token)
        {
            if (token.GetPayload() == null || token.GetPayload().GetData() == null)
                return 1f;

            return PayloadHelper.DecodeFloat(token.GetPayload().GetData());
        }

        /// <summary>
        /// Stores a 32 bit float in the payload, or set it to null if 1f;
        /// </summary>
        /// <param name="token"></param>
        /// <param name="weight"></param>
        public override void SetWeight(Token token, float weight)
        {
            token.SetPayload(
                weight == 1f
                    ? null
                    : new Payload(PayloadHelper.EncodeFloat(weight))
                );
        }
    }
}