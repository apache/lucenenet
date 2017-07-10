using Lucene.Net.Cli.CommandLine;
using Lucene.Net.Facet.Taxonomy;
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

    public class IndexListTaxonomyStatsCommand : ICommand
    {
        public class Configuration : ConfigurationBase
        {
            public Configuration(CommandLineOptions options)
            {
                this.Main = (args) => PrintTaxonomyStats.Main(args);

                this.Name = "list-taxonomy-stats";
                this.Description = FromResource("Description");
                this.ExtendedHelpText = FromResource("ExtendedHelpText");

                this.Arguments.Add(new IndexDirectoryArgument());
                this.ShowTreeOption = this.Option("-tree|--show-tree", FromResource("ShowTreeDescription"), CommandOptionType.NoValue);

                this.OnExecute(() => new IndexListTaxonomyStatsCommand().Run(this));
            }

            public virtual CommandOption ShowTreeOption { get; private set; }
        }

        public int Run(ConfigurationBase cmd)
        {
            if (!cmd.ValidateArguments(1))
            {
                return 1;
            }
            var input = cmd as Configuration;
            var args = new List<string>() { cmd.GetArgument<IndexDirectoryArgument>().Value };
            
            if (input.ShowTreeOption != null && input.ShowTreeOption.HasValue())
            {
                args.Add("-printTree");
            }

            cmd.Main(args.ToArray());
            return 0;
        }
    }
}
