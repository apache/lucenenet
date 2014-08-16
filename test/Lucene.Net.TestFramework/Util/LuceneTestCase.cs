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

using System.Runtime.Serialization;

namespace Lucene.Net.Util
{
#if PORTABLE || K10
    using Lucene.Net.Support;
#endif
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using Xunit;

    public class LuceneTestCase
    {
        /// <summary>
        ///  A random multiplier which you should use when writing random tests:
        ///  multiply it by the number of iterations to scale your tests (for nightly builds).
        /// </summary>
        public static int RANDOM_MULTIPLIER = SystemProps.Get<int>("tests:multiplier", 1);

        /// <summary>
        /// Whether or not <see cref="NightlyAttribute" /> tests should run.
        /// </summary>
        public static bool TEST_NIGHTLY = SystemProps.Get<Boolean>("tests:nightly", false);

        private static ThreadLocal<System.Random> random;

        static LuceneTestCase()
        {
            random = new ThreadLocal<System.Random>(() => {

              
                return new System.Random((int) DateTime.Now.Ticks & 0x0000FFFF);
            });
        }

        public LuceneTestCase()
        {
           
        }

        /// <summary>
        /// Placeholder for random values.
        /// </summary>
        public System.Random Random
        {
            get { return random.Value; }  
        }

#if XUNIT

        [Serializable]
        public class LuceneAssertionException : Exception
        {
            //
            // For guidelines regarding the creation of new exception types, see
            //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
            // and
            //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
            //

            public LuceneAssertionException()
            {
            }

            public LuceneAssertionException(string message) : base(message)
            {
            }

            public LuceneAssertionException(string message, Exception inner) : base(message, inner)
            {
            }

#if NET45
            protected LuceneAssertionException(
                System.Runtime.Serialization.SerializationInfo info,
                StreamingContext context) : base(info, context)
            {
            }
#endif
        }

        [DebuggerHidden]
        public static void Null(object value, string message = null, params  object[] args)
        {
            try
            {
                Assert.Null(value);
            }
            catch (Exception ex)
            {
                var msg = message ?? "The value must be null.";
                if (args != null && args.Length > 0)
                    msg = string.Format(msg, args);

                throw new LuceneAssertionException(msg, ex);
            }
        }

        public static void NotNull(object value, string message = null, params object[] args)
        {
            try
            {
                Assert.NotNull(value);
            }
            catch (Exception ex)
            {
                var msg = message ?? "The value must not be null.";
                if (args != null && args.Length > 0)
                    msg = string.Format(msg, args);

                throw new LuceneAssertionException(msg, ex);
            }
        }

        /// <summary>
        /// Asserts that two object are the same.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        [DebuggerHidden]
        public static void Same(object expected, object actual)
        {
            Assert.Same(expected, actual);
        }

        /// <summary>
        /// Assert that two objects are not the same.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        [DebuggerHidden]
        public static void NotSame(object expected, object actual)
        {
            Assert.NotSame(expected, actual);
        }

        [DebuggerHidden]
        public static void Equal(string expected, string actual, string message = null, params object[] args)
        {
            try
            {
                Assert.Equal(expected, actual);
            }
            catch (Exception ex)
            {
                if (message == null)
                    throw;

                var msg = message;
                if (args != null && args.Length > 0)
                    msg = string.Format(msg, args);

                throw new LuceneAssertionException(msg, ex);
            }
        }

        [DebuggerHidden]
        public static void Equal<T>(T expected, T actual, string message = null, params object[] args)
        {
            try
            {
                Assert.Equal(expected, actual);
            }
            catch (Exception ex)
            {
                if (message == null)
                    throw;

                var msg = message;
                if (args != null && args.Length > 0)
                    msg = string.Format(msg, args);

                throw new LuceneAssertionException(msg, ex);
            }
        }

        [DebuggerHidden]
        public static void Equal<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message= null, params object[] args)
        {
            try
            {
                Assert.Equal(expected, actual);
            }
            catch (Exception ex)
            {
                if (message == null)
                    throw;

                var msg = message;
                if (args != null && args.Length > 0)
                    msg = string.Format(msg, args);

                throw new LuceneAssertionException(msg, ex);
            }
        }

        [DebuggerHidden]
        public static void NotEqual<T>(T expected, T actual, string message = null, params object[] args)
        {
            try
            {
                Assert.NotEqual(expected, actual);
            }
            catch (Exception ex)
            {
                if (message == null)
                    throw;

                var msg = message;
                if (args != null && args.Length > 0)
                    msg = string.Format(msg, args);

                throw new LuceneAssertionException(msg, ex);
            }
           
        }


        [DebuggerHidden]
        public static void Ok(bool condition, string message = null, params object[] values)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                var exceptionMessage = message;

                if(values != null && values.Length > 0)
                {
                    exceptionMessage = String.Format(exceptionMessage, values);
                }

                Assert.True(condition, exceptionMessage);
            }
            else 
            {
                Assert.True(condition);    
            }
        }

        [DebuggerHidden]
        public static T Throws<T>(Action code) where T : Exception
        {
            return Assert.Throws<T>(code);
        }
        
        #endif
    }
}