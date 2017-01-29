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

namespace Lucene.Net.Codecs.Sep
{
    using Store;
    using System;

    /// <summary>
    /// Defines basic API for writing ints to an IndexOutput.
    ///  IntBlockCodec interacts with this API. @see
    ///  IntBlockReader
    /// 
    /// @lucene.experimental 
    /// </summary>
    public abstract class IntIndexInput : IDisposable
    {
        public abstract IntIndexInputReader GetReader();
        public abstract void Dispose();
        public abstract IntIndexInputIndex GetIndex();

      
    }
    
    /// <summary>Reads int values</summary>
    public abstract class IntIndexInputReader // LUCENENET TODO: Rename AbstractReader and nest within IntIndexInput
    {
        /// <summary>Reads next single int</summary>
        public abstract int Next();
    }

    /// <summary>
    /// Records a single skip-point in the <seealso cref="IntIndexInput.GetReader"/>. </summary>
    public abstract class IntIndexInputIndex // LUCENENET TODO: Rename AbstractIndex and nest within IntIndexInput
    {
        public abstract void Read(DataInput indexIn, bool absolute);

        /// <summary>Seeks primary stream to the last read offset </summary>
        public abstract void Seek(IntIndexInputReader stream);

        public abstract void CopyFrom(IntIndexInputIndex other);

        public abstract IntIndexInputIndex Clone();
    }
}
