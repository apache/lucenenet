using Lucene.Net.Index;
using System.Collections.Generic;
using System.Linq;

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

    public class IndexListCfsCommand : ICommand
    {
        private readonly bool extract;
        public IndexListCfsCommand(bool extract)
        {
            this.extract = extract;
        }

        public class Configuration : ConfigurationBase
        {
            public Configuration(CommandLineOptions options)
            {
                this.Main = (args) => CompoundFileExtractor.Main(args);

                this.Name = "list-cfs";
                this.Description = FromResource("Description");
                this.ExtendedHelpText = FromResource("ExtendedHelpText");

                this.Argument("<CFS_FILE_NAME>", FromResource("CFSFileNameDescription"));
                this.Options.Add(new DirectoryTypeOption());

                this.OnExecute(() => new IndexListCfsCommand(extract: false).Run(this));
            }
        }

        public int Run(ConfigurationBase cmd)
        {
            if (!cmd.ValidateArguments(1))
            {
                return 1;
            }

            var args = new List<string>();
            if (extract)
            {
                args.Add("-extract");
            }

            var directoryTypeOption = cmd.GetOption<DirectoryTypeOption>();
            if (directoryTypeOption != null && directoryTypeOption.HasValue())
            {
                args.Add("-dir-impl");
                args.Add(directoryTypeOption.Value());
            }

            cmd.Main(cmd.GetNonNullArguments().Union(args).ToArray());
            return 0;
        }
    }
}
