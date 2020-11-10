using Lucene.Net.Cli.CommandLine;
using Lucene.Net.Demo;
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

    public class DemoSearchFilesCommand : ICommand
    {
        public class Configuration : DemoConfiguration
        {
            public Configuration(CommandLineOptions options)
            {
                this.Main = (args) => SearchFiles.Main(args);

                this.Name = "search-files";
                this.Description = FromResource("Description");
                this.ExtendedHelpText = FromResource("ExtendedHelpText");

                this.IndexDirectoryArgument = new IndexDirectoryArgument(required: true);
                this.Arguments.Add(IndexDirectoryArgument);
                this.FieldOption = this.Option(
                    "-f|--field <FIELD>",
                    FromResource("FieldDescription"), 
                    CommandOptionType.SingleValue);
                this.RepeatOption = this.Option(
                    "-r|--repeat <NUMBER>",
                    FromResource("RepeatDescription"),
                    CommandOptionType.SingleValue);
                this.QueriesFileOption = this.Option(
                    "-qf|--queries-file <PATH>",
                    FromResource("QueriesFileDescription"),
                    CommandOptionType.SingleValue);
                this.QueryOption = this.Option(
                    "-q|--query <QUERY>",
                    FromResource("QueryDescription"),
                    CommandOptionType.SingleValue);
                this.RawOption = this.Option(
                    "--raw",
                    FromResource("RawDescription"),
                    CommandOptionType.NoValue);
                this.PageSizeOption = this.Option(
                    "-p|--page-size <NUMBER>",
                    FromResource("PageSizeDescription"),
                    CommandOptionType.SingleValue);


                this.OnExecute(() => new DemoSearchFilesCommand().Run(this));
            }

            public override IEnumerable<string> SourceCodeFiles => new string[] { "SearchFiles.cs" };

            public CommandArgument IndexDirectoryArgument { get; private set; }
            public CommandOption FieldOption { get; private set; }
            public CommandOption RepeatOption { get; private set; }
            public CommandOption QueriesFileOption { get; private set; }
            public CommandOption QueryOption { get; private set; }
            public CommandOption RawOption { get; private set; }
            public CommandOption PageSizeOption { get; private set; }
        }

        public int Run(ConfigurationBase cmd)
        {
            if (!cmd.ValidateArguments(1))
            {
                return 1;
            }

            var input = cmd as Configuration;
            var args = new List<string> { input.IndexDirectoryArgument.Value };

            if (input.FieldOption.HasValue())
            {
                args.Add("--field");
                args.Add(input.FieldOption.Value());
            }

            if (input.RepeatOption.HasValue())
            {
                args.Add("--repeat");
                args.Add(input.RepeatOption.Value());
            }

            if (input.QueriesFileOption.HasValue())
            {
                args.Add("--queries-file");
                args.Add(input.QueriesFileOption.Value());
            }

            if (input.QueryOption.HasValue())
            {
                args.Add("--query");
                args.Add(input.QueryOption.Value());
            }

            if (input.RawOption.HasValue())
            {
                args.Add("--raw");
            }

            if (input.PageSizeOption.HasValue())
            {
                args.Add("--page-size");
                args.Add(input.PageSizeOption.Value());
            }

            cmd.Main(args.ToArray());
            return 0;
        }
    }
}
