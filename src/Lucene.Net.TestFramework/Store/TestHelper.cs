using SimpleFSIndexInput = Lucene.Net.Store.SimpleFSDirectory.SimpleFSIndexInput;

namespace Lucene.Net.Store
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
    /// This class provides access to namespace-level features defined in the
    /// Store namespace. It is used for testing only.
    /// </summary>
    public static class TestHelper // LUCENENET specific - made static and remoed private constructor since all members are static
    {
        /// <summary>
        /// Returns true if the instance of the provided input stream is actually
        /// an <see cref="SimpleFSIndexInput"/>.
        /// </summary>
        public static bool IsSimpleFSIndexInput(IndexInput @is)
        {
            return @is is SimpleFSIndexInput;
        }

        /// <summary>
        /// Returns true if the provided input stream is an <see cref="SimpleFSIndexInput"/> and
        /// is a clone, that is it does not own its underlying file descriptor.
        /// </summary>
        public static bool IsSimpleFSIndexInputClone(IndexInput @is)
        {
            if (IsSimpleFSIndexInput(@is))
            {
                return ((SimpleFSIndexInput)@is).IsClone;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Given an instance of <see cref="SimpleFSIndexInput"/>, this method returns
        /// true if the underlying file descriptor is valid, and false otherwise.
        /// this can be used to determine if the OS file has been closed.
        /// The descriptor becomes invalid when the non-clone instance of the
        /// <see cref="SimpleFSIndexInput"/> that owns this descriptor is disposed. However, the
        /// descriptor may possibly become invalid in other ways as well.
        /// </summary>
        public static bool IsSimpleFSIndexInputOpen(IndexInput @is)
        {
            if (IsSimpleFSIndexInput(@is))
            {
                SimpleFSIndexInput fis = (SimpleFSIndexInput)@is;
                return fis.IsFDValid;
            }
            else
            {
                return false;
            }
        }
    }
}