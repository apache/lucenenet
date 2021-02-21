using J2N.Numerics;
using System;

namespace Lucene.Net.Replicator
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
    /// Describes a file in a <see cref="IRevision"/>. A file has a source, which allows a
    /// single revision to contain files from multiple sources (e.g. multiple indexes).
    /// </summary>
    /// <remarks>
    /// @lucene.experimental
    /// </remarks>
    public class RevisionFile : IEquatable<RevisionFile>
    {
        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        public string FileName { get; private set; }
        
        /// <summary>
        /// Gets or sets the length of the file denoted by <see cref="FileName"/>.
        /// </summary>
        public long Length { get; set; }

        /// <summary>
        /// Constructor with the given file name and optionally length. 
        /// </summary>
        /// <param name="fileName">The name of the file</param>
        /// <param name="length">Optional, the length of the file.</param>
        public RevisionFile(string fileName, long length = -1)
        {
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentException("fileName must not be null or empty", nameof(fileName));

            FileName = fileName;
            Length = length;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RevisionFile)obj);
        }

        // LUCENENET specific Equals overload
        public virtual bool Equals(RevisionFile other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(FileName, other.FileName, StringComparison.Ordinal) && Length == other.Length;
        }

        public override int GetHashCode()
        {
            return FileName.GetHashCode() ^ (int)(Length ^ Length.TripleShift(32));
        }

        public override string ToString()
        {
            return string.Format("fileName={0} length={1}", FileName, Length);
        }
    }
}