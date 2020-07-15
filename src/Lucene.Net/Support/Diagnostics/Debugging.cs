using Lucene.Net.Util;

namespace Lucene.Net.Diagnostics
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

    // LUCENENET: This can only be named Debug if we merge it with the Debug
    // class from Lucene.Net.TestFramework because it is in the same namespace.
    // But that class is dependent upon AssertionException, which is only for testing.
    internal static class Debugging
    {
        /// <summary>
        /// Allows toggling "assertions" on/off even in release builds. The default is <c>false</c>.
        /// <para/>
        /// This allows loggers and testing frameworks to enable test point messages ("TP")
        /// from <see cref="Index.IndexWriter"/>, <see cref="Index.DocumentsWriterPerThread"/>,
        /// <see cref="Index.FreqProxTermsWriterPerField"/>, <see cref="Index.StoredFieldsProcessor"/>,
        /// <see cref="Index.TermVectorsConsumer"/>, and <see cref="Index.TermVectorsConsumerPerField"/>.
        /// </summary>
        public static bool AssertsEnabled { get; set; } = SystemProperties.GetPropertyAsBoolean("assert", false);
    }
}
