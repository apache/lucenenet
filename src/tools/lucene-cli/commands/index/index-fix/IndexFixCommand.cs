using Lucene.Net.Index;
using System;

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

                this.Arguments.Add(new IndexDirectoryArgument());
                this.Options.Add(new VerboseOption());
                this.Options.Add(new CrossCheckTermVectorsOption());
                this.Options.Add(new DirectoryTypeOption());
                this.Options.Add(new SegmentOption(allowMultiple: true) { Description = FromResource("SegmentsDescpription") });

                this.OnExecute(() => new IndexCheckCommand(fix: true).Run(this));
            }
        }

        public int Run(ConfigurationBase cmd)
        {
            // We call IndexCheckCommand - nothing to do here.
            throw new NotSupportedException();
        }
    }
}
