using Lucene.Net.Store;

namespace Lucene.Net.Codecs.Sep
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
    /// Provides <see cref="int"/> reader and writer to specified files.
    /// <para/>
    /// NOTE: This was IntStreamFactory in Lucene
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    public abstract class Int32StreamFactory
    {
        /// <summary>
        /// Create an <see cref="Int32IndexInput"/> on the provided fileName. 
        /// </summary>
        public abstract Int32IndexInput OpenInput(Directory dir, string fileName, IOContext context);

        /// <summary>
        /// Create an <see cref="Int32IndexOutput"/> on the provided fileName. 
        /// </summary>
        public abstract Int32IndexOutput CreateOutput(Directory dir, string fileName, IOContext context);
    }
}