using Lucene.Net.Cli.CommandLine;
using Lucene.Net.Index;
using System.Collections.Generic;
using System.Linq;

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

    public class IndexSplitCommand : ICommand
    {
        public class Configuration : ConfigurationBase
        {
            public Configuration(CommandLineOptions options)
            {
                this.Main = (args) => MultiPassIndexSplitter.Main(args);

                this.Name = "split";
                this.Description = FromResource("Description");

                this.Argument("<OUTPUT_DIRECTORY>", FromResource("OutputDirectoryDescription"));
                this.Argument("<INPUT_DIRECTORY>[ <INPUT_DIRECTORY_2>...]", FromResource("InputDirectoryDescription"), true);
                this.NumberOfParts = this.Option("-n |--number-of-parts <NUMBER>", FromResource("NumberOfPartsDescription"), CommandOptionType.SingleValue);
                this.Sequential = this.Option("-s|--sequential", FromResource("SequentialDescription"), CommandOptionType.NoValue);

                this.OnExecute(() => new IndexSplitCommand().Run(this));
            }

            public virtual CommandOption NumberOfParts { get; private set; }
            public virtual CommandOption Sequential { get; private set; }
        }

        public int Run(ConfigurationBase cmd)
        {
            if (!cmd.ValidateArguments(2))
            {
                return 1;
            }

            // The first argument is the output - we need to use the -out switch
            var args = new List<string>(cmd.GetNonNullArguments().Skip(1)) {
                "-out",
                cmd.GetNonNullArguments().First()
            };

            var input = cmd as Configuration;

            args.Add("-num");

            if (input.NumberOfParts != null && input.NumberOfParts.HasValue())
            {
                args.Add(input.NumberOfParts.Value());
            }
            else
            {
                // Default to 2 parts
                args.Add("2");
            }

            if (input.Sequential != null && input.Sequential.HasValue())
            {
                args.Add("-seq");
            }

            cmd.Main(args.ToArray());
            return 0;
        }
    }
}
