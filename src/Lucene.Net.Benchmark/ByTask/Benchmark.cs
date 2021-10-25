using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Benchmarks.ByTask
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
    /// Run the benchmark algorithm.
    /// </summary>
    /// <remarks>
    /// <list type="number">
    ///     <item><description>Read algorithm.</description></item>
    ///     <item><description>Run the algorithm.</description></item>
    /// </list>
    /// <para/>
    /// Things to be added/fixed in "Benchmarking by tasks":
    /// <list type="number">
    ///     <item><description>TODO - report into Excel and/or graphed view.</description></item>
    ///     <item><description>TODO - perf comparison between Lucene releases over the years.</description></item>
    ///     <item><description>TODO - perf report adequate to include in Lucene nightly build site? (so we can easily track performance changes.)</description></item>
    ///     <item><description>TODO - add overall time control for repeated execution (vs. current by-count only).</description></item>
    ///     <item><description>TODO - query maker that is based on index statistics.</description></item>
    /// </list>
    /// </remarks>
    public class Benchmark
    {
        private readonly PerfRunData runData; // LUCENENET: marked readonly
        private readonly Algorithm algorithm; // LUCENENET: marked readonly
        private bool executed;

        public Benchmark(TextReader algReader)
        {
            // prepare run data
            try
            {
                runData = new PerfRunData(new Config(algReader));
            }
            catch (Exception e) when (e.IsException())
            {
                //e.printStackTrace();
                Console.Error.WriteLine(e.ToString());
                throw new Exception("Error: cannot init PerfRunData!", e);
            }

            // parse algorithm
            try
            {
                algorithm = new Algorithm(runData);
            }
            catch (Exception e) when (e.IsException())
            {
                throw new Exception("Error: cannot understand algorithm!", e);
            }
        }

        /// <summary>
        /// Execute this benchmark.
        /// </summary>
        public virtual void Execute()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                if (executed)
                {
                    throw IllegalStateException.Create("Benchmark was already executed");
                }
                executed = true;
                runData.SetStartTimeMillis();
                algorithm.Execute();
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Run the benchmark algorithm.
        /// </summary>
        /// <param name="args">Benchmark config and algorithm files.</param>
        public static void Main(string[] args)
        {
            Exec(args);
        }

        /// <summary>
        /// Utility: execute benchmark from command line.
        /// </summary>
        /// <param name="args">Single argument is expected: algorithm-file.</param>
        public static void Exec(string[] args)
        {
            // verify command line args
            if (args.Length < 1)
            {
                // LUCENENET specific - usage info printed by our wrapper console
                throw new ArgumentException();
                //Console.WriteLine("Usage: java Benchmark <algorithm file>");
                //Environment.Exit(1);
            }

            // verify input files 
            FileInfo algFile = new FileInfo(args[0]);
            if (!algFile.Exists /*|| !algFile.isFile() ||!algFile.canRead()*/ )
            {
                Console.WriteLine("cannot find/read algorithm file: " + algFile.FullName);
                Environment.Exit(1);
            }

            Console.WriteLine("Running algorithm from: " + algFile.FullName);

            Benchmark benchmark = null;
            try
            {
                benchmark = new Benchmark(IOUtils.GetDecodingReader(algFile, Encoding.UTF8));
            }
            catch (Exception e) when (e.IsException())
            {
                Console.Error.WriteLine(e.ToString());
                Environment.Exit(1);
            }

            Console.WriteLine("------------> algorithm:");
            Console.WriteLine(benchmark.Algorithm.ToString());

            // execute
            try
            {
                benchmark.Execute();
            }
            catch (Exception e) when (e.IsException())
            {
                Console.Error.WriteLine("Error: cannot execute the algorithm! " + e.Message);
                Console.Error.WriteLine(e.ToString());
            }

            Console.WriteLine("####################");
            Console.WriteLine("###  D O N E !!! ###");
            Console.WriteLine("####################");
        }

        /// <summary>
        /// Returns the algorithm.
        /// </summary>
        public virtual Algorithm Algorithm => algorithm;

        /// <summary>
        /// Returns the runData.
        /// </summary>
        public virtual PerfRunData RunData => runData;
    }
}
