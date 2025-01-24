using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lucene.Net.BenchmarkDotNet.Util
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

    public static class ContentGenerator
    {
        public static void GenerateFiles(Random random, string directory, int numberOfFiles, params string[] stringsToQuery)
        {
            var subdirectories = new HashSet<string>();
            for (int i = 0; i < numberOfFiles; i++)
            {
                bool root = random.Next(1, 100) > 50;

                if (root)
                {
                    GenerateFile(random, directory, stringsToQuery);
                }
                else
                {
                    string subdirectory;
                    if (subdirectories.Count > 0 && random.Next(1, 100) > 30)
                    {
                        subdirectory = RandomPicks.RandomFrom(random, subdirectories);
                    }
                    else
                    {
                        subdirectory = RandomSimpleString(random, 5, 20);
                        subdirectories.Add(subdirectory);
                    }
                    GenerateFile(random, Path.Combine(directory, subdirectory), stringsToQuery);
                }
            }
        }

        private static void GenerateFile(Random random, string directory, ICollection<string> stringsToQuery)
        {
            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);

            string fileName = RandomSimpleString(random, 5, 25) + ".txt";
            int paragraphs = random.Next(5, 25);

            using (var writer = new StreamWriter(Path.Combine(directory, fileName), append: false, encoding: Encoding.UTF8))
            {
                for (int i = 0; i < paragraphs; i++)
                {
                    WriteParagraph(random, writer, stringsToQuery);
                }
            }
        }

        private static void WriteParagraph(Random random, TextWriter writer, ICollection<string> stringsToQuery)
        {
            int words = random.Next(50, 100);
            bool addStringsToQuery = stringsToQuery != null && stringsToQuery.Count > 0;

            for (int i = 0; i < words; i++)
            {
                if (addStringsToQuery && random.Next(1, 1500) == 668)
                    writer.Write(RandomPicks.RandomFrom(random, stringsToQuery));
                else
                    writer.Write(RandomSimpleString(random, 1, 8));

                if (i + 1 < words)
                    writer.Write(" ");
            }
            writer.WriteLine(".");
            writer.WriteLine();
        }

        /// <summary>
        /// Returns a random string consisting only of lowercase characters 'a' through 'z'.
        /// </summary>
        public static string RandomSimpleString(Random r, int minLength, int maxLength)
        {
            int end = RandomNumbers.RandomInt32Between(r, minLength, maxLength);
            if (end == 0)
            {
                // allow 0 length
                return "";
            }
            char[] buffer = new char[end];
            for (int i = 0; i < end; i++)
            {
                buffer[i] = (char)RandomNumbers.RandomInt32Between(r, 'a', 'z');
            }
            return new string(buffer, 0, end);
        }
    }
}
