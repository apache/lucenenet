using System;

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
    /// A utility for executing 2-phase commit on several objects.
    /// </summary>
    /// <seealso cref= ITwoPhaseCommit
    /// @lucene.experimental </seealso>
    public sealed class TwoPhaseCommitTool
    {
        /// <summary>
        /// No instance </summary>
        private TwoPhaseCommitTool()
        {
        }

        /// <summary>
        /// Thrown by <seealso cref="TwoPhaseCommitTool#execute(TwoPhaseCommit...)"/> when an
        /// object fails to prepareCommit().
        /// </summary>
        // LUCENENET: All exeption classes should be marked serializable
#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        public class PrepareCommitFailException
            : System.IO.IOException
        {
            /// <summary>
            /// Sole constructor. </summary>
            public PrepareCommitFailException(Exception cause, ITwoPhaseCommit obj)
                : base("prepareCommit() failed on " + obj, cause)
            {
            }
        }

        /// <summary>
        /// Thrown by <seealso cref="TwoPhaseCommitTool#execute(TwoPhaseCommit...)"/> when an
        /// object fails to commit().
        /// </summary>
        // LUCENENET: All exeption classes should be marked serializable
#if FEATURE_SERIALIZABLE
        [Serializable]
#endif
        public class CommitFailException : System.IO.IOException
        {
            /// <summary>
            /// Sole constructor. </summary>
            public CommitFailException(Exception cause, ITwoPhaseCommit obj)
                : base("commit() failed on " + obj, cause)
            {
            }
        }

        /// <summary>
        /// rollback all objects, discarding any exceptions that occur. </summary>
        private static void Rollback(params ITwoPhaseCommit[] objects)
        {
            foreach (ITwoPhaseCommit tpc in objects)
            {
                // ignore any exception that occurs during rollback - we want to ensure
                // all objects are rolled-back.
                if (tpc != null)
                {
                    try
                    {
                        tpc.Rollback();
                    }
                    catch (Exception t)
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Executes a 2-phase commit algorithm by first
        /// <seealso cref="ITwoPhaseCommit#prepareCommit()"/> all objects and only if all succeed,
        /// it proceeds with <seealso cref="ITwoPhaseCommit#commit()"/>. If any of the objects
        /// fail on either the preparation or actual commit, it terminates and
        /// <seealso cref="ITwoPhaseCommit#rollback()"/> all of them.
        /// <p>
        /// <b>NOTE:</b> it may happen that an object fails to commit, after few have
        /// already successfully committed. this tool will still issue a rollback
        /// instruction on them as well, but depending on the implementation, it may
        /// not have any effect.
        /// <p>
        /// <b>NOTE:</b> if any of the objects are {@code null}, this method simply
        /// skips over them.
        /// </summary>
        /// <exception cref="PrepareCommitFailException">
        ///           if any of the objects fail to
        ///           <seealso cref="ITwoPhaseCommit#prepareCommit()"/> </exception>
        /// <exception cref="CommitFailException">
        ///           if any of the objects fail to <seealso cref="ITwoPhaseCommit#commit()"/> </exception>
        public static void Execute(params ITwoPhaseCommit[] objects)
        {
            ITwoPhaseCommit tpc = null;
            try
            {
                // first, all should successfully prepareCommit()
                for (int i = 0; i < objects.Length; i++)
                {
                    tpc = objects[i];
                    if (tpc != null)
                    {
                        tpc.PrepareCommit();
                    }
                }
            }
            catch (Exception t)
            {
                // first object that fails results in rollback all of them and
                // throwing an exception.
                Rollback(objects);
                throw new PrepareCommitFailException(t, tpc);
            }

            // If all successfully prepareCommit(), attempt the actual commit()
            try
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    tpc = objects[i];
                    if (tpc != null)
                    {
                        tpc.Commit();
                    }
                }
            }
            catch (Exception t)
            {
                // first object that fails results in rollback all of them and
                // throwing an exception.
                Rollback(objects);
                throw new CommitFailException(t, tpc);
            }
        }
    }
}