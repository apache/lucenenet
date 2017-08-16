using Lucene.Net.Benchmarks.Utils;
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

    public class BenchmarkExtractWikipediaCommand : ICommand
    {
        public class Configuration : ConfigurationBase
        {
            public Configuration(CommandLineOptions options)
            {
                this.Main = (args) => ExtractWikipedia.Main(args);

                this.Name = "extract-wikipedia";
                this.Description = FromResource("Description");
                this.ExtendedHelpText = FromResource("ExtendedHelpText");

                this.InputWikipediaFile = this.Argument("<INPUT_WIKIPEDIA_FILE>", FromResource("InputWikipediaFileDescription"));
                this.OutputDirectory = this.Argument("<OUTPUT_DIRECTORY>", FromResource("OutputDirectoryDescription"));
                this.DiscardImageOnlyDocs = this.Option("-d|--discard-image-only-docs", FromResource("DiscardImageOnlyDocsDescription"), CommandOptionType.NoValue);

                this.OnExecute(() => new BenchmarkExtractWikipediaCommand().Run(this));
            }

            public CommandArgument InputWikipediaFile { get; set; }
            public CommandArgument OutputDirectory { get; set; }
            public CommandOption DiscardImageOnlyDocs { get; set; }
        }

        public int Run(ConfigurationBase cmd)
        {
            if (!cmd.ValidateArguments(2))
            {
                return 1;
            }

            var args = new List<string>();
            var input = cmd as Configuration;

            args.Add("--input");
            args.Add(input.InputWikipediaFile.Value);
            args.Add("--output");
            args.Add(input.OutputDirectory.Value);

            if (input.DiscardImageOnlyDocs.HasValue())
            {
                args.Add("--discardImageOnlyDocs");
            }

            cmd.Main(args.ToArray());
            return 0;
        }
    }
}
