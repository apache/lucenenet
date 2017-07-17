using Egothor.Stemmer;
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

    public class AnalysisStempelPatchStemsCommand : ICommand
    {
        public class Configuration : ConfigurationBase
        {
            public Configuration(CommandLineOptions options)
            {
                this.Main = (args) => DiffIt.Main(args);

                this.Name = "stempel-patch-stems";
                this.Description = FromResource("Description");

                this.StemmerTableFiles = this.Argument(
                    "<STEMMER_TABLE_FILE>[ <STEMMER_TABLE_FILE_2>...]",
                    FromResource("StemmerTableFilesDescription"),
                    multipleValues: true);
                this.StemmerTableFilesEncoding = this.Option(
                    "-e|--encoding <ENCODING>",
                    FromResource("StemmerTableFilesEncodingDescription"),
                    CommandOptionType.SingleValue);

                this.OnExecute(() => new AnalysisStempelPatchStemsCommand().Run(this));
            }

            public virtual CommandArgument StemmerTableFiles { get; private set; }
            public virtual CommandOption StemmerTableFilesEncoding { get; private set; }
        }

        public int Run(ConfigurationBase cmd)
        {
            if (!cmd.ValidateArguments(1))
            {
                return 1;
            }

            var input = cmd as Configuration;
            var args = new List<string>(input.StemmerTableFiles.Values);

            if (input.StemmerTableFilesEncoding.HasValue())
            {
                args.Add("--encoding");
                args.Add(input.StemmerTableFilesEncoding.Value());
            }

            cmd.Main(args.ToArray());
            return 0;
        }
    }
}
