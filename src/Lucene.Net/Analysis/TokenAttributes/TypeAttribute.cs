using Lucene.Net.Util;

namespace Lucene.Net.Analysis.TokenAttributes
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
    /// A <see cref="Token"/>'s lexical type. The Default value is "word".
    /// </summary>
    public interface ITypeAttribute : IAttribute
    {
        /// <summary>
        /// Gets or Sets the lexical type. </summary>
        string Type { get; set; }
    }

    // LUCENENET specific - since we can't add a field to an interface,
    // the concrete type was made into a partial class so we can keep
    // this together in the same file as the interface.
    // 
    // The syntax is the same as it was in Lucene -
    // TypeAttribute.DEFAULT_TYPE.
    public partial class TypeAttribute
    {
        /// <summary>
        /// the default type
        /// </summary>
        public const string DEFAULT_TYPE = "word";
    }
}