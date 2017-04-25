using System;
using System.Collections.Generic;

namespace Lucene.Net.Support
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
    /// LUCENENET specific comparer to handle the special case
    /// of comparing negative zero with positive zero.
    /// <para/>
    /// For IEEE floating-point numbers, there is a distinction of negative and positive zero.
    /// Reference: http://stackoverflow.com/a/3139636
    /// </summary>
    public class SignedZeroComparer : IComparer<double>
    {
        public int Compare(double v1, double v2)
        {
            long a = BitConverter.DoubleToInt64Bits(v1);
            long b = BitConverter.DoubleToInt64Bits(v2);
            if (a > b)
            {
                return 1;
            }
            else if (a < b)
            {
                return -1;
            }

            return 0;
        }
    }
}
