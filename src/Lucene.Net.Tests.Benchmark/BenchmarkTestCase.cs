using Lucene.Net.Benchmarks.ByTask;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Benchmarks
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
    /// Base class for all Benchmark unit tests.
    /// </summary>
    public abstract class BenchmarkTestCase : LuceneTestCase
    {
        private static DirectoryInfo WORKDIR;

        public override void BeforeClass()
        {
            base.BeforeClass();
            WORKDIR = CreateTempDir("benchmark");
            // LUCENENET: Our directory numbers are sequential. Doing a delete
            // here will make threads collide.
            //WORKDIR.Delete();
            //WORKDIR.Create();

            propLines = new string[] {
                "work.dir=" + getWorkDirPath(),
                "directory=RAMDirectory",
                "print.props=false",
            };
        }

        public override void AfterClass()
        {
            WORKDIR = null;
            base.AfterClass();
        }


        public static DirectoryInfo getWorkDir() // LUCENENET: CA1822: Mark members as static
        {
            return WORKDIR;
        }

        /** Copy a resource into the workdir */
        public void copyToWorkDir(string resourceName)
        {
            Stream resource = GetType().getResourceAsStream(resourceName);
            Stream dest = new FileStream(System.IO.Path.Combine(getWorkDir().FullName, resourceName), FileMode.Create, FileAccess.Write);
            byte[] buffer = new byte[8192];
            int len;

            while ((len = resource.Read(buffer, 0, buffer.Length)) > 0)
            {
                dest.Write(buffer, 0, len);
            }

            resource.Dispose();
            dest.Dispose();
        }

        /** Return a path, suitable for a .alg config file, for a resource in the workdir */
        public String getWorkDirResourcePath(String resourceName)
        {
            return System.IO.Path.Combine(getWorkDir().FullName, resourceName).Replace("\\", "/");
        }

        /** Return a path, suitable for a .alg config file, for the workdir */
        public String getWorkDirPath()
        {
            return getWorkDir().FullName.Replace("\\", "/");
        }

        // create the benchmark and execute it. 
        public Benchmark execBenchmark(String[] algLines)
        {
            String algText = algLinesToText(algLines);
            logTstLogic(algText);
            Benchmark benchmark = new Benchmark(new StringReader(algText));
            benchmark.Execute();
            return benchmark;
        }

        // properties in effect in all tests here
        String[] propLines;

        static readonly String NEW_LINE = Environment.NewLine;

        // catenate alg lines to make the alg text
        private String algLinesToText(String[] algLines)
        {
            String indent = "  ";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < propLines.Length; i++)
            {
                sb.append(indent).append(propLines[i]).append(NEW_LINE);
            }
            for (int i = 0; i < algLines.Length; i++)
            {
                sb.append(indent).append(algLines[i]).append(NEW_LINE);
            }
            return sb.toString();
        }

        private static void logTstLogic(String txt)
        {
            if (!Verbose)
                return;
            Console.WriteLine("Test logic of:");
            Console.WriteLine(txt);
        }
    }
}
