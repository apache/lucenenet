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

    public class IndexDeleteSegmentsCommand : ICommand
    {
        public class Configuration : ConfigurationBase
        {
            public Configuration(CommandLineOptions options)
            {
                this.Main = (args) => IndexSplitter.Main(args);

                this.Name = "delete-segments";
                this.Description = FromResource("Description");

                this.Arguments.Add(new IndexDirectoryArgument(required: true));
                this.Arguments.Add(new SegmentsArgument() { Description = FromResource("SegmentsDescription") });

                this.ExtendedHelpText = FromResource("ExtendedHelpText");

                this.OnExecute(() => new IndexDeleteSegmentsCommand().Run(this));
            }
        }

        public int Run(ConfigurationBase cmd)
        {
            if (!cmd.ValidateArguments(2))
            {
                return 1;
            }

            var args = new List<string>() { cmd.GetNonNullArguments()[0] };
            var segmentsArgument = cmd.GetArgument<SegmentsArgument>();
            if (segmentsArgument != null)
            {
                foreach(var segment in segmentsArgument.Values)
                {
                    args.Add("-d");
                    args.Add(segment);
                }
            }

            cmd.Main(cmd.GetNonNullArguments().Union(new string[] { "-d" }).ToArray());
            return 0;
        }
    }
}