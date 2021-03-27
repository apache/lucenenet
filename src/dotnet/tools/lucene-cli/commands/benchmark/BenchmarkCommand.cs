using Lucene.Net.Benchmarks.ByTask;

namespace Lucene.Net.Cli
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

    public class BenchmarkCommand : ICommand
    {
        public class Configuration : ConfigurationBase
        {
            public Configuration(CommandLineOptions options)
            {
                this.Main = (args) => Benchmark.Main(args);

                this.Name = "benchmark";
                this.Description = FromResource("Description");
                this.ExtendedHelpText = FromResource("ExtendedHelpText");

                this.Commands.Add(new BenchmarkExtractReutersCommand.Configuration(options));
                this.Commands.Add(new BenchmarkExtractWikipediaCommand.Configuration(options));
                this.Commands.Add(new BenchmarkFindQualityQueriesCommand.Configuration(options));
                this.Commands.Add(new BenchmarkRunCommand.Configuration(options));
                this.Commands.Add(new BenchmarkRunTrecEvalCommand.Configuration(options));
                this.Commands.Add(new BenchmarkSampleCommand.Configuration(options));

                this.OnExecute(() => new BenchmarkCommand().Run(this));
            }
        }

        public int Run(ConfigurationBase cmd)
        {
            cmd.ShowHelp();
            return ExitCode.NoCommandProvided;
        }
    }
}
