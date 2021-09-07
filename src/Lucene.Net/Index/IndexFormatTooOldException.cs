using Lucene.Net.Diagnostics;
using System;
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
#endif

namespace Lucene.Net.Index
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

    using DataInput = Lucene.Net.Store.DataInput;

    /// <summary>
    /// This exception is thrown when Lucene detects
    /// an index that is too old for this Lucene version
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    public class IndexFormatTooOldException : CorruptIndexException
    {
        /// <summary>
        /// Creates an <see cref="IndexFormatTooOldException"/>.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <param name="resourceDesc"> describes the file that was too old </param>
        /// <param name="version"> the version of the file that was too old </param>
        public IndexFormatTooOldException(string resourceDesc, string version)
            : base("Format version is not supported (resource: " + resourceDesc + "): " + version + ". this version of Lucene only supports indexes created with release 3.0 and later.")
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(resourceDesc != null);
        }

        /// <summary>
        /// Creates an <see cref="IndexFormatTooOldException"/>.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <param name="input"> the open file that's too old </param>
        /// <param name="version"> the version of the file that was too old </param>
        public IndexFormatTooOldException(DataInput input, string version)
            : this(input.ToString(), version)
        {
        }

        /// <summary>
        /// Creates an <see cref="IndexFormatTooOldException"/>.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <param name="resourceDesc"> describes the file that was too old </param>
        /// <param name="version"> the version of the file that was too old </param>
        /// <param name="minVersion"> the minimum version accepted </param>
        /// <param name="maxVersion"> the maxium version accepted </param>
        public IndexFormatTooOldException(string resourceDesc, int version, int minVersion, int maxVersion)
            : base("Format version is not supported (resource: " + resourceDesc + "): " + version + " (needs to be between " + minVersion + " and " + maxVersion + "). this version of Lucene only supports indexes created with release 3.0 and later.")
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(resourceDesc != null);
        }

        /// <summary>
        /// Creates an <see cref="IndexFormatTooOldException"/>.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        /// <param name="input"> the open file that's too old </param>
        /// <param name="version"> the version of the file that was too old </param>
        /// <param name="minVersion"> the minimum version accepted </param>
        /// <param name="maxVersion"> the maxium version accepted </param>
        public IndexFormatTooOldException(DataInput input, int version, int minVersion, int maxVersion)
            : this(input.ToString(), version, minVersion, maxVersion)
        {
        }

        // LUCENENET: For testing purposes
        internal IndexFormatTooOldException(string message)
            : base(message)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected IndexFormatTooOldException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}