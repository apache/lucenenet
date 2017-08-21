using Lucene.Net.Benchmarks.Quality.Trec;
using Lucene.Net.Cli.CommandLine;
using System.Collections.Generic;

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

    public class BenchmarkRunTrecEvalCommand : ICommand
    {
        public class Configuration : ConfigurationBase
        {
            public Configuration(CommandLineOptions options)
            {
                this.Main = (args) => QueryDriver.Main(args);

                this.Name = "run-trec-eval";
                this.Description = FromResource("Description");
                this.ExtendedHelpText = FromResource("ExtendedHelpText");

                this.Argument("<INPUT_TOPICS_FILE>", FromResource("TopicsFileDescription"));
                this.Argument("<INPUT_QUERY_RELEVANCE_FILE>", FromResource("QueryRelevanceFileDescription"));
                this.Argument("<OUTPUT_SUBMISSION_FILE>", FromResource("OutputSubmissionFileDescription"));
                this.Arguments.Add(new IndexDirectoryArgument(required: true));
                this.QueryOnTitle = this.Option("-t|--query-on-title", FromResource("QueryOnTitleDescription"), CommandOptionType.NoValue);
                this.QueryOnDescription = this.Option("-d|--query-on-description", FromResource("QueryOnDescriptionDescription"), CommandOptionType.NoValue);
                this.QueryOnNarrative = this.Option("-n|--query-on-narrative", FromResource("QueryOnNarrativeDescription"), CommandOptionType.NoValue);

                this.OnExecute(() => new BenchmarkRunTrecEvalCommand().Run(this));
            }

            public CommandOption QueryOnTitle { get; set; }
            public CommandOption QueryOnDescription { get; set; }
            public CommandOption QueryOnNarrative { get; set; }
        }

        public int Run(ConfigurationBase cmd)
        {
            if (!cmd.ValidateArguments(4))
            {
                return 1;
            }

            var input = cmd as Configuration;
            var args = new List<string>(cmd.GetNonNullArguments());

            string querySpec = string.Empty;

            if (input.QueryOnTitle.HasValue())
                querySpec += "T";
            if (input.QueryOnDescription.HasValue())
                querySpec += "D";
            if (input.QueryOnNarrative.HasValue())
                querySpec += "N";

            if (!string.IsNullOrEmpty(querySpec))
                args.Add(querySpec);

            cmd.Main(args.ToArray());
            return 0;
        }
    }
}
