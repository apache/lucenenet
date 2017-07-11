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

    public class IndexCheckCommand : ICommand
    {
        private readonly bool fix;

        public IndexCheckCommand(bool fix)
        {
            this.fix = fix;
        }

        public class Configuration : ConfigurationBase
        {
            public Configuration(CommandLineOptions options)
            {
                this.Main = (args) => CheckIndex.Main(args);

                this.Name = "check";
                this.Description = FromResource("Description");
                this.ExtendedHelpText = FromResource("ExtendedHelpText");

                this.Arguments.Add(new IndexDirectoryArgument());
                this.Options.Add(new VerboseOption());
                this.Options.Add(new CrossCheckTermVectorsOption());
                this.Options.Add(new DirectoryTypeOption());
                this.Options.Add(new SegmentOption(allowMultiple: true) { Description = FromResource("SegmentsDescription") });

                // NOTE: We are intentionally calling fix here because it is exactly
                // the same operation minus the -fix argument
                OnExecute(() => new IndexCheckCommand(fix: false).Run(this));
            }
        }

        public int Run(ConfigurationBase cmd)
        {
            if (!cmd.ValidateArguments(1))
            {
                return 1;
            }

            var args = new List<string>() { cmd.GetArgument<IndexDirectoryArgument>().Value };

            if (fix)
            {
                args.Add("-fix");
            }

            // get cross check option
            var crossCheckOption = cmd.GetOption<CrossCheckTermVectorsOption>();
            if (crossCheckOption != null && crossCheckOption.HasValue())
            {
                args.Add("-crossCheckTermVectors");
            }

            // get vebose option
            var verboseOption = cmd.GetOption<VerboseOption>();
            if (verboseOption != null && verboseOption.HasValue())
            {
                args.Add("-verbose");
            }

            // get segment option
            var segmentOption = cmd.GetOption<SegmentOption>();
            if (segmentOption != null && segmentOption.HasValue())
            {
                foreach (var value in segmentOption.Values)
                {
                    args.Add("-segment");
                    args.Add(value);
                }
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
