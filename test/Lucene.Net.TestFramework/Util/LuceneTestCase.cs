/**
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
 */


namespace Lucene.Net.TestFramework
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Xunit;

    public class LuceneTestCase
    {
	    public LuceneTestCase()
	    {
	    }



        [DebuggerHidden]
        public static void Ok(bool condition, string message = null, params object[] values)
        {
#if XUNIT
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
#endif
        }

        [DebuggerHidden]
        public static void Equal(string expected, string actual)
        {
#if XUNIT
            Assert.Equal(expected, actual);
#endif 
        }

        [DebuggerHidden]
        public static void Equal<T>(T expected, T actual)
        {
#if XUNIT
            Assert.Equal(expected, actual);
#endif
        }

        [DebuggerHidden]
        public static void Equal<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
#if XUNIT
            Assert.Equal(expected, actual);
#endif
        }

        [DebuggerHidden]
        public static void Throws<T>(Action code) where T : Exception
        {
#if XUNIT
            Assert.Throws<T>(code);
#endif
        }
    }
}