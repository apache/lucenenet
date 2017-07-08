using Lucene.Net.Cli.CommandLine;
using Lucene.Net.Index;

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

    public class IndexFixCommand : ICommand
    {
        public class Configuration : ConfigurationBase
        {
            public Configuration(CommandLineOptions options)
            {
                this.Main = (args) => CheckIndex.Main(args);

                this.Name = "fix";
                this.Description = FromResource("Description");
                this.ExtendedHelpText = FromResource("ExtendedHelpText");

                this.Arguments.Add(new IndexDirectoryArgument());
                this.Options.Add(new VerboseOption());
                this.Options.Add(new CrossCheckTermVectorsOption());
                this.Options.Add(new DirectoryTypeOption());
                // LUCENENET NOTE: This is effectively the same thing as running
                // the check command, but using fix doesn't allow the option of
                // specifying individual segments, so it is better to have an option here.
                DryRunOption = this.Option("--dry-run",
                    FromResource("DryRunDescription"),
                    CommandOptionType.NoValue);

                this.OnExecute(() => new IndexFixCommand().Run(this));
            }

            public CommandOption DryRunOption { get; private set; }
        }

        public int Run(ConfigurationBase cmd)
        {
            var input = cmd as Configuration;

            bool fix = true;
            if (input.DryRunOption.HasValue())
            {
                fix = false;
            }

            return new IndexCheckCommand(fix).Run(cmd);
        }
    }
}
