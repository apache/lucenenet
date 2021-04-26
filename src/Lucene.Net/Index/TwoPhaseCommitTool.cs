using System;
using System.IO;
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

    /// <summary>
    /// A utility for executing 2-phase commit on several objects.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="ITwoPhaseCommit"/>
    public sealed class TwoPhaseCommitTool
    {
        /// <summary>
        /// No instance </summary>
        private TwoPhaseCommitTool()
        {
        }

        /// <summary>
        /// Thrown by <see cref="TwoPhaseCommitTool.Execute(ITwoPhaseCommit[])"/> when an
        /// object fails to <see cref="ITwoPhaseCommit.PrepareCommit()"/>.
        /// </summary>
        // LUCENENET: It is no longer good practice to use binary serialization. 
        // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
        [Serializable]
#endif
        public class PrepareCommitFailException
            : IOException
        {
            /// <summary>
            /// Sole constructor. </summary>
            public PrepareCommitFailException(Exception cause, ITwoPhaseCommit obj)
                : base("prepareCommit() failed on " + obj, cause)
            {
            }

            // LUCENENET: For testing purposes
            internal PrepareCommitFailException(string message)
                : base(message)
            {
            }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
            /// <summary>
            /// Initializes a new instance of this class with serialized data.
            /// </summary>
            /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
            /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
            protected PrepareCommitFailException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
#endif
        }

        /// <summary>
        /// Thrown by <see cref="TwoPhaseCommitTool.Execute(ITwoPhaseCommit[])"/> when an
        /// object fails to <see cref="ITwoPhaseCommit.Commit()"/>.
        /// </summary>
        // LUCENENET: It is no longer good practice to use binary serialization. 
        // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
        [Serializable]
#endif
        public class CommitFailException : IOException
        {
            /// <summary>
            /// Sole constructor. </summary>
            public CommitFailException(Exception cause, ITwoPhaseCommit obj)
                : base("commit() failed on " + obj, cause)
            {
            }

            // LUCENENET: For testing purposes
            internal CommitFailException(string message)
                : base(message)
            {
            }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
            /// <summary>
            /// Initializes a new instance of this class with serialized data.
            /// </summary>
            /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
            /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
            protected CommitFailException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
#endif
        }

        /// <summary>
        /// Rollback all objects, discarding any exceptions that occur. </summary>
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
                    catch (Exception t) when (t.IsThrowable())
                    {
                        // ignore
                    }
                }
            }
        }

        /// <summary>
        /// Executes a 2-phase commit algorithm by first
        /// <see cref="ITwoPhaseCommit.PrepareCommit()"/> all objects and only if all succeed,
        /// it proceeds with <see cref="ITwoPhaseCommit.Commit()"/>. If any of the objects
        /// fail on either the preparation or actual commit, it terminates and
        /// <see cref="ITwoPhaseCommit.Rollback()"/> all of them.
        /// <para/>
        /// <b>NOTE:</b> It may happen that an object fails to commit, after few have
        /// already successfully committed. This tool will still issue a rollback
        /// instruction on them as well, but depending on the implementation, it may
        /// not have any effect.
        /// <para/>
        /// <b>NOTE:</b> if any of the objects are <c>null</c>, this method simply
        /// skips over them.
        /// </summary>
        /// <exception cref="PrepareCommitFailException">
        ///           if any of the objects fail to
        ///           <see cref="ITwoPhaseCommit.PrepareCommit()"/> </exception>
        /// <exception cref="CommitFailException">
        ///           if any of the objects fail to <see cref="ITwoPhaseCommit.Commit()"/> </exception>
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
            catch (Exception t) when (t.IsThrowable())
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
            catch (Exception t) when (t.IsThrowable())
            {
                // first object that fails results in rollback all of them and
                // throwing an exception.
                Rollback(objects);
                throw new CommitFailException(t, tpc);
            }
        }
    }
}