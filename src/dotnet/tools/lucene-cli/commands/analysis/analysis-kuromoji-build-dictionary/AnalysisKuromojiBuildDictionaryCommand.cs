using Lucene.Net.Analysis.Ja.Util;
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

    public class AnalysisKuromojiBuildDictionaryCommand : ICommand
    {
        public class Configuration : ConfigurationBase
        {
            public Configuration(CommandLineOptions options)
            {
                this.Main = (args) => DictionaryBuilder.Main(args);

                this.Name = "kuromoji-build-dictionary";
                this.Description = FromResource("Description");
                this.ExtendedHelpText = FromResource("ExtendedHelpText");

                this.Format = this.Argument(
                    "<FORMAT>",
                    FromResource("FormatDescription"));
                this.InputDirectory = this.Argument(
                    "<INPUT_DIRECTORY>",
                    FromResource("InputDirectoryDescription"));
                this.OutputDirectory = this.Argument(
                    "<OUTPUT_DIRECTORY>",
                    FromResource("OutputDirectoryDescription"));
                this.InputDirectoryEncoding = this.Option(
                    "-e|--encoding <ENCODING>",
                    FromResource("InputDirectoryEncodingDescription"),
                    CommandOptionType.SingleValue);
                this.Normalize = this.Option(
                    "-n|--normalize",
                    FromResource("NormalizeDescription"),
                    CommandOptionType.NoValue);

                this.OnExecute(() => new AnalysisKuromojiBuildDictionaryCommand().Run(this));
            }

            public virtual CommandArgument Format { get; private set; }
            public virtual CommandArgument InputDirectory { get; private set; }
            public virtual CommandArgument OutputDirectory { get; private set; }
            public virtual CommandOption InputDirectoryEncoding { get; private set; }
            public virtual CommandOption Normalize { get; private set; }
        }

        public int Run(ConfigurationBase cmd)
        {
            if (!cmd.ValidateArguments(3))
            {
                return 1;
            }

            var input = cmd as Configuration;
            var args = new List<string>(input.GetNonNullArguments());

            if (input.InputDirectoryEncoding.HasValue())
            {
                args.Add(input.InputDirectoryEncoding.Value());
            }
            else
            {
                args.Add("euc-jp");
            }

            if (input.Normalize.HasValue())
            {
                args.Add("true");
            }
            else
            {
                args.Add("false");
            }

            cmd.Main(args.ToArray());
            return 0;
        }
    }
}
