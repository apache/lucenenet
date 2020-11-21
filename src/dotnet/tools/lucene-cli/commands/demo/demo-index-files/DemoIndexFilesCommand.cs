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

    public class DemoIndexFilesCommand : ICommand
    {
        public class Configuration : DemoConfiguration
        {
            public Configuration(CommandLineOptions options)
            {
                this.Main = (args) => IndexFiles.Main(args);

                this.Name = "index-files";
                this.Description = FromResource("Description");
                this.ExtendedHelpText = FromResource("ExtendedHelpText");

                this.IndexDirectoryArgument = new IndexDirectoryArgument(required: true);
                this.Arguments.Add(IndexDirectoryArgument);
                this.SourceDirectoryArgument = this.Argument(
                    "<SOURCE_DIRECTORY>",
                    FromResource("SourceDirectoryDescription"));
                this.UpdateOption = this.Option(
                    "-u|--update",
                    FromResource("UpdateDescription"), 
                    CommandOptionType.NoValue);
                
                this.OnExecute(() => new DemoIndexFilesCommand().Run(this));
            }

            public override IEnumerable<string> SourceCodeFiles => new string[] { "IndexFiles.cs" };

            public CommandArgument IndexDirectoryArgument { get; private set; }
            public CommandArgument SourceDirectoryArgument { get; private set; }
            public CommandOption UpdateOption { get; private set; }
        }

        public int Run(ConfigurationBase cmd)
        {
            if (!cmd.ValidateArguments(2))
            {
                return 1;
            }

            var input = cmd as Configuration;
            var args = new List<string>
            {
                input.IndexDirectoryArgument.Value,
                input.SourceDirectoryArgument.Value
            };

            if (input.UpdateOption.HasValue())
            {
                args.Add("--update");
            }

            cmd.Main(args.ToArray());
            return 0;
        }
    }
}
