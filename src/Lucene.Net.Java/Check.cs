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


namespace Java
{
    using System;
    using System.Collections.Generic;

    internal class Check
    {

        internal static void NotNull<T>(string argument, T value, string message = null, params  object[] args) where T : class 
        {
            if (value == null)
            {
                if (message == null)
                    message = string.Format("The parameter, {0}, must not be null.", argument);

                if (args != null && args.Length > 0)
                    message = string.Format(message, args);


                throw new ArgumentNullException(argument, message);
            }
        }

        internal static void Range<T>(string argument, IList<T> value, int start, int count)
        {
            if(start < 0)
                throw new ArgumentOutOfRangeException("start", "The parameter, start, must be 0 or greater.");

            if(start >= value.Count)
                throw new ArgumentOutOfRangeException("start", string.Format("The parameter, start, must not be equal or greater than {0}'s length.", argument));

            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "The parameter, count, must be 0 or greater.");
            
            if (count > value.Count)
                throw new ArgumentOutOfRangeException("count", string.Format("The parameter, count, must not be greater than {0}'s length.", argument));

            if(start + count > value.Count)
                throw new ArgumentOutOfRangeException("value,count", string.Format("The sum of start and count must not be greater than {0}'s length", argument));
        }
    }
}
