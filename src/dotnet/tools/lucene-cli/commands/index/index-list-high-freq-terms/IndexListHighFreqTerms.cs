using Lucene.Net.Cli.CommandLine;
using Lucene.Net.Misc;
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

    public class IndexListHighFreqTermsCommand : ICommand
    {
        public class Configuration : ConfigurationBase
        {
            public Configuration(CommandLineOptions options)
            {
                this.Main = (args) => HighFreqTerms.Main(args);

                this.Name = "list-high-freq-terms";
                this.Description = FromResource("Description");

                this.Arguments.Add(new IndexDirectoryArgument());
                this.TotalTermFreqOption = this.Option(
                    "-t|--total-term-frequency",
                    FromResource("TotalTermFrequencyDescription"),
                    CommandOptionType.NoValue);
                this.NumberOfTermsOption = this.Option(
                    "-n|--number-of-terms <NUMBER>",
                    FromResource("NumberOfTermsDescription"),
                    CommandOptionType.SingleValue);
                this.FieldOption = this.Option(
                    "-f|--field <FIELD>",
                    FromResource("FieldDescription"),
                    CommandOptionType.SingleValue);

                this.OnExecute(() => new IndexListHighFreqTermsCommand().Run(this));
            }

            public virtual CommandOption TotalTermFreqOption { get; private set; }
            public virtual CommandOption NumberOfTermsOption { get; private set; }
            public virtual CommandOption FieldOption { get; private set; }
        }

        public int Run(ConfigurationBase cmd)
        {
            if (!cmd.ValidateArguments(1))
            {
                return 1;
            }

            var args = new List<string>() { cmd.GetArgument<IndexDirectoryArgument>().Value };
            var input = cmd as Configuration;

            if (input.TotalTermFreqOption != null && input.TotalTermFreqOption.HasValue())
            {
                args.Add("-t");
            }

            if (input.NumberOfTermsOption != null && input.NumberOfTermsOption.HasValue())
            {
                args.Add(input.NumberOfTermsOption.Value());
            }

            if (input.FieldOption != null && input.FieldOption.HasValue())
            {
                args.Add(input.FieldOption.Value());
            }

            cmd.Main(args.ToArray());
            return 0;
        }
    }
}
