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

    public class IndexCommand : ICommand
    {
        public class Configuration : ConfigurationBase
        {
            public Configuration(CommandLineOptions options)
            {
                this.Name = "index";
                this.Description = FromResource("Description");

                this.Commands.Add(new IndexCheckCommand.Configuration(options));
                this.Commands.Add(new IndexCopySegmentsCommand.Configuration(options));
                this.Commands.Add(new IndexDeleteSegmentsCommand.Configuration(options));
                this.Commands.Add(new IndexExtractCfsCommand.Configuration(options));
                this.Commands.Add(new IndexFixCommand.Configuration(options));
                this.Commands.Add(new IndexListCfsCommand.Configuration(options));
                this.Commands.Add(new IndexListHighFreqTermsCommand.Configuration(options));
                this.Commands.Add(new IndexListSegmentsCommand.Configuration(options));
                this.Commands.Add(new IndexListTaxonomyStatsCommand.Configuration(options));
                this.Commands.Add(new IndexListTermInfoCommand.Configuration(options));
                this.Commands.Add(new IndexMergeCommand.Configuration(options));
                this.Commands.Add(new IndexSplitCommand.Configuration(options));
                this.Commands.Add(new IndexUpgradeCommand.Configuration(options));

                this.OnExecute(() => new IndexCommand().Run(this));
            }
        }

        public int Run(ConfigurationBase cmd)
        {
            cmd.ShowHelp();
            return ExitCode.NoCommandProvided;
        }
    }
}
