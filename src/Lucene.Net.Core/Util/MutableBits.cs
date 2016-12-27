namespace Lucene.Net.Util
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
    /// Extension of Bits for live documents.
    /// </summary>
    public interface MutableBits : Bits // LUCENENET TODO: Make abstract class (since Bits should be an abstract class)
    {
        /// <summary>
        /// Sets the bit specified by <code>index</code> to false. </summary>
        /// <param name="index"> index, should be non-negative and &lt; <seealso cref="#length()"/>.
        ///        The result of passing negative or out of bounds values is undefined
        ///        by this interface, <b>just don't do it!</b> </param>
        void Clear(int index);
    }
}