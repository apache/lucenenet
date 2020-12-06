using J2N.Collections.Generic.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Util
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
    /// Extensions to the <see cref="Exception"/> class to allow for
    /// adding and retrieving suppressed exceptions, like you can do in Java.
    /// </summary>
    public static class ExceptionExtensions
    {
        public static readonly string SUPPRESSED_EXCEPTIONS_KEY = "Lucene_SuppressedExceptions";

        public static Exception[] GetSuppressed(this Exception e)
        {
            return e.GetSuppressedAsList().ToArray();
        }

        public static IList<Exception> GetSuppressedAsList(this Exception e)
        {
            IList<Exception> suppressed;
            if (!e.Data.Contains(SUPPRESSED_EXCEPTIONS_KEY))
            {
                suppressed = new JCG.List<Exception>();
                e.Data.Add(SUPPRESSED_EXCEPTIONS_KEY, suppressed);
            }
            else
            {
                suppressed = e.Data[SUPPRESSED_EXCEPTIONS_KEY] as IList<Exception>;
            }

            return suppressed;
        }

        public static void AddSuppressed(this Exception e, Exception exception)
        {
            e.GetSuppressedAsList().Add(exception);
        }
    }
}
