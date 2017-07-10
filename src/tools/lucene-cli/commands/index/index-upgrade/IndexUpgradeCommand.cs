using Lucene.Net.Cli.CommandLine;
using Lucene.Net.Index;
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

    public class IndexUpgradeCommand : ICommand
    {
        public class Configuration : ConfigurationBase
        {
            public Configuration(CommandLineOptions options)
            {
                this.Main = (args) => IndexUpgrader.Main(args);

                this.Name = "upgrade";
                this.Description = FromResource("Description");
                this.ExtendedHelpText = FromResource("ExtendedHelpText");

                this.Arguments.Add(new IndexDirectoryArgument());
                this.DeletePriorCommitsOption = this.Option("-d|--delete-prior-commits",
                    FromResource("DeleteDescription"), 
                    CommandOptionType.NoValue);
                this.Options.Add(new VerboseOption());
                this.Options.Add(new DirectoryTypeOption());
                
                this.OnExecute(() => new IndexUpgradeCommand().Run(this));
            }

            public virtual CommandOption DeletePriorCommitsOption { get; private set; }
        }

        public int Run(ConfigurationBase cmd)
        {
            if (!cmd.ValidateArguments(1))
            {
                return 1;
            }

            var args = new List<string>() { cmd.GetArgument<IndexDirectoryArgument>().Value };
            var input = cmd as Configuration;
            
            if (input.DeletePriorCommitsOption != null && input.DeletePriorCommitsOption.HasValue())
            {
                args.Add("-delete-prior-commits");
            }

            // get vebose option
            var verboseOption = cmd.GetOption<VerboseOption>();
            if (verboseOption != null && verboseOption.HasValue())
            {
                args.Add("-verbose");
            }

            var directoryTypeOption = cmd.GetOption<DirectoryTypeOption>();
            if (directoryTypeOption != null && directoryTypeOption.HasValue())
            {
                args.Add("-dir-impl");
                args.Add(directoryTypeOption.Value());
            }

            cmd.Main(args.ToArray());
            return 0;
        }
    }
}
