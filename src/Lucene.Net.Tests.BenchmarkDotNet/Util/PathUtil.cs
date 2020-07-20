using System;
using System.IO;

namespace Lucene.Net.Tests.BenchmarkDotNet.Util
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

    public static class PathUtil
    {
        private const int TEMP_NAME_RETRY_THRESHOLD = 9999;

        public static DirectoryInfo CreateTempDir(string prefix)
        {
            //DirectoryInfo @base = BaseTempDirForTestClass();

            int attempt = 0;
            DirectoryInfo f;
            bool iterate = true;
            do
            {
                if (attempt++ >= TEMP_NAME_RETRY_THRESHOLD)
                {
                    throw new Exception("Failed to get a temporary name too many times, check your temp directory and consider manually cleaning it: " + System.IO.Path.GetTempPath());
                }
                // LUCENENET specific - need to use a random file name instead of a sequential one or two threads may attempt to do 
                // two operations on a file at the same time.
                //f = new DirectoryInfo(Path.Combine(System.IO.Path.GetTempPath(), "LuceneTemp", prefix + "-" + attempt));
                f = new DirectoryInfo(Path.Combine(System.IO.Path.GetTempPath(), "LuceneTemp", prefix + "-" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName())));

                try
                {
                    if (!System.IO.Directory.Exists(f.FullName))
                    {
                        f.Create();
                        iterate = false;
                    }
                }
#pragma warning disable 168
                catch (IOException exc)
#pragma warning restore 168
                {
                    iterate = true;
                }
            } while (iterate);

            return f;
        }
    }
}
