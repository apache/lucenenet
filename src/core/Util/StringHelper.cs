/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Support;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Util
{
    /// <summary> Methods for manipulating strings.</summary>
    public static class StringHelper
    {
        public static int BytesDifference(BytesRef left, BytesRef right)
        {
            int len = left.length < right.length ? left.length : right.length;
            sbyte[] bytesLeft = left.bytes;
            int offLeft = left.offset;
            sbyte[] bytesRight = right.bytes;
            int offRight = right.offset;
            for (int i = 0; i < len; i++)
                if (bytesLeft[i + offLeft] != bytesRight[i + offRight])
                    return i;
            return len;
        }

        public static IComparer<string> VersionComparator
        {
            get { return versionComparator; }
        }

        /// <summary>
        /// This is to replace the anonymous class usage in the java version.
        /// </summary>
        private class AnonymousVersionComparator : IComparer<string>
        {
            public int Compare(string a, string b)
            {
                StringTokenizer aTokens = new StringTokenizer(a, ".");
                StringTokenizer bTokens = new StringTokenizer(b, ".");

                while (aTokens.HasMoreTokens())
                {
                    int aToken = int.Parse(aTokens.NextToken());
                    if (bTokens.HasMoreTokens())
                    {
                        int bToken = int.Parse(bTokens.NextToken());
                        if (aToken != bToken)
                        {
                            return aToken < bToken ? -1 : 1;
                        }
                    }
                    else
                    {
                        // a has some extra trailing tokens. if these are all zeroes, thats ok.
                        if (aToken != 0)
                        {
                            return 1;
                        }
                    }
                }

                // b has some extra trailing tokens. if these are all zeroes, thats ok.
                while (bTokens.HasMoreTokens())
                {
                    if (int.Parse(bTokens.NextToken()) != 0)
                        return -1;
                }

                return 0;
            }
        }

        private static IComparer<string> versionComparator = new AnonymousVersionComparator();

        public static bool Equals(String s1, String s2)
        {
            return string.Equals(s1, s2);
        }

        public static bool StartsWith(BytesRef @ref, BytesRef prefix)
        {
            return SliceEquals(@ref, prefix, 0);
        }

        public static bool EndsWith(BytesRef @ref, BytesRef suffix)
        {
            return SliceEquals(@ref, suffix, @ref.length - suffix.length);
        }

        private static bool SliceEquals(BytesRef sliceToTest, BytesRef other, int pos)
        {
            if (pos < 0 || sliceToTest.length - pos < other.length)
            {
                return false;
            }
            int i = sliceToTest.offset + pos;
            int j = other.offset;
            int k = other.offset + other.length;

            while (j < k)
            {
                if (sliceToTest.bytes[i++] != other.bytes[j++])
                {
                    return false;
                }
            }

            return true;
        }
    }
}