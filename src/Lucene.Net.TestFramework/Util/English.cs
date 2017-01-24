using System.Text;

namespace Lucene.Net.Util
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     * <p/>
     * http://www.apache.org/licenses/LICENSE-2.0
     * <p/>
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Converts numbers to english strings for testing.
    /// @lucene.internal
    /// </summary>
    public sealed class English
    {
        private English() // no instance
        {
        }

        public static string LongToEnglish(long i)
        {
            StringBuilder result = new StringBuilder();
            LongToEnglish(i, result);
            return result.ToString();
        }

        public static void LongToEnglish(long i, StringBuilder result)
        {
            if (i == 0)
            {
                result.Append("zero");
                return;
            }
            if (i < 0)
            {
                result.Append("minus ");
                i = -i;
            }
            if (i >= 1000000000000000000L) // quadrillion
            {
                LongToEnglish(i / 1000000000000000000L, result);
                result.Append("quintillion, ");
                i = i % 1000000000000000000L;
            }
            if (i >= 1000000000000000L) // quadrillion
            {
                LongToEnglish(i / 1000000000000000L, result);
                result.Append("quadrillion, ");
                i = i % 1000000000000000L;
            }
            if (i >= 1000000000000L) // trillions
            {
                LongToEnglish(i / 1000000000000L, result);
                result.Append("trillion, ");
                i = i % 1000000000000L;
            }
            if (i >= 1000000000) // billions
            {
                LongToEnglish(i / 1000000000, result);
                result.Append("billion, ");
                i = i % 1000000000;
            }
            if (i >= 1000000) // millions
            {
                LongToEnglish(i / 1000000, result);
                result.Append("million, ");
                i = i % 1000000;
            }
            if (i >= 1000) // thousands
            {
                LongToEnglish(i / 1000, result);
                result.Append("thousand, ");
                i = i % 1000;
            }
            if (i >= 100) // hundreds
            {
                LongToEnglish(i / 100, result);
                result.Append("hundred ");
                i = i % 100;
            }
            //we know we are smaller here so we can cast
            if (i >= 20)
            {
                switch (((int)i) / 10)
                {
                    case 9:
                        result.Append("ninety");
                        break;

                    case 8:
                        result.Append("eighty");
                        break;

                    case 7:
                        result.Append("seventy");
                        break;

                    case 6:
                        result.Append("sixty");
                        break;

                    case 5:
                        result.Append("fifty");
                        break;

                    case 4:
                        result.Append("forty");
                        break;

                    case 3:
                        result.Append("thirty");
                        break;

                    case 2:
                        result.Append("twenty");
                        break;
                }
                i = i % 10;
                if (i == 0)
                {
                    result.Append(" ");
                }
                else
                {
                    result.Append("-");
                }
            }
            switch ((int)i)
            {
                case 19:
                    result.Append("nineteen ");
                    break;

                case 18:
                    result.Append("eighteen ");
                    break;

                case 17:
                    result.Append("seventeen ");
                    break;

                case 16:
                    result.Append("sixteen ");
                    break;

                case 15:
                    result.Append("fifteen ");
                    break;

                case 14:
                    result.Append("fourteen ");
                    break;

                case 13:
                    result.Append("thirteen ");
                    break;

                case 12:
                    result.Append("twelve ");
                    break;

                case 11:
                    result.Append("eleven ");
                    break;

                case 10:
                    result.Append("ten ");
                    break;

                case 9:
                    result.Append("nine ");
                    break;

                case 8:
                    result.Append("eight ");
                    break;

                case 7:
                    result.Append("seven ");
                    break;

                case 6:
                    result.Append("six ");
                    break;

                case 5:
                    result.Append("five ");
                    break;

                case 4:
                    result.Append("four ");
                    break;

                case 3:
                    result.Append("three ");
                    break;

                case 2:
                    result.Append("two ");
                    break;

                case 1:
                    result.Append("one ");
                    break;

                case 0:
                    result.Append("");
                    break;
            }
        }

        public static string IntToEnglish(int i)
        {
            StringBuilder result = new StringBuilder();
            LongToEnglish(i, result);
            return result.ToString();
        }

        public static void IntToEnglish(int i, StringBuilder result)
        {
            LongToEnglish(i, result);
        }
    }
}