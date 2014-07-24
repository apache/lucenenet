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

namespace Lucene.Net.Util
{
    using System;
    using Lucene.Net.Support;

    public class TestArrayUtil : LuceneTestCase
    {

        [Test(JavaMethodName = "TestParseInt")]
        public void ParseInt()
        {
            // int result;

            // chars is null
            Throws<ArgumentNullException>(() =>
            {
                ArrayUtil.ParseInt(null);
            });

            // chars is empty
            Throws<ArgumentException>(() =>
            {
                ArrayUtil.ParseInt("".ToCharArray());
            });

            // throws when radix is greater than 36 
            Throws<ArgumentOutOfRangeException>(() =>
            {
                var data = "-123".ToCharArray();
                Ok(data.Length > 0, "data.length should be greater than 0");
                ArrayUtil.ParseInt(data, radix: 37);
            });

            // throws when radix is less than 2
            Throws<ArgumentOutOfRangeException>(() =>
            {
                ArrayUtil.ParseInt("-123".ToCharArray(), radix: 1);
            });

            // throws when limit > chars.Length
            Throws<ArgumentException>(() =>
            {
                ArrayUtil.ParseInt("12".ToCharArray(), limit: 20);
            });

            // throws when offset > chars.length;
            Throws<ArgumentException>(() =>
            {
                ArrayUtil.ParseInt("12".ToCharArray(), offset: 20);
            });

            // throws when offset < 0.
            Throws<ArgumentException>(() =>
            {
                ArrayUtil.ParseInt("12".ToCharArray(), offset: -1);
            });

            // throws when only the negative sign is present "-" 
            Throws<FormatException>(() =>
            {
               ArrayUtil.ParseInt("-".ToCharArray());
            });

            int test;

            test = ArrayUtil.ParseInt("1".ToCharArray());
            Ok(test == 1, test + " does not equal: " + 1);

            test = ArrayUtil.ParseInt("-10000".ToCharArray());
            Ok(test == -10000, test + " does not equal: " + -10000);

            test = ArrayUtil.ParseInt("1923".ToCharArray());
            Ok(test == 1923, test + " does not equal: " + 1923);

            test = ArrayUtil.ParseInt("-1".ToCharArray());
            Ok(test == -1, test + " does not equal: " + -1);

            test = ArrayUtil.ParseInt("foo 1923 bar".ToCharArray(), 4, 4);
            Ok(test == 1923, test + " does not equal: " + 1923);
        }
    }
}