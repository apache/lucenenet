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

    /// <summary>
    /// An interface for implementations that support 2-phase commit. You can use
    /// <seealso cref="TwoPhaseCommitTool"/> to execute a 2-phase commit algorithm over several
    /// <seealso cref="TwoPhaseCommit"/>s.
    ///
    /// @lucene.experimental
    /// </summary>
    public interface TwoPhaseCommit // LUCENENET TODO: Rename with "I"
    {
        /// <summary>
        /// The first stage of a 2-phase commit. Implementations should do as much work
        /// as possible in this method, but avoid actual committing changes. If the
        /// 2-phase commit fails, <seealso cref="#rollback()"/> is called to discard all changes
        /// since last successful commit.
        /// </summary>
        void PrepareCommit();

        /// <summary>
        /// The second phase of a 2-phase commit. Implementations should ideally do
        /// very little work in this method (following <seealso cref="#prepareCommit()"/>, and
        /// after it returns, the caller can assume that the changes were successfully
        /// committed to the underlying storage.
        /// </summary>
        void Commit();

        /// <summary>
        /// Discards any changes that have occurred since the last commit. In a 2-phase
        /// commit algorithm, where one of the objects failed to <seealso cref="#commit()"/> or
        /// <seealso cref="#prepareCommit()"/>, this method is used to roll all other objects
        /// back to their previous state.
        /// </summary>
        void Rollback();
    }
}