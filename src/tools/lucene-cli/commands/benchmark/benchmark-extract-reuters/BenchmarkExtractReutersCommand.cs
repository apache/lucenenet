using Lucene.Net.Benchmarks.Utils;

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

    public class BenchmarkExtractReutersCommand : ICommand
    {
        public class Configuration : ConfigurationBase
        {
            public Configuration(CommandLineOptions options)
            {
                this.Main = (args) => ExtractReuters.Main(args);

                this.Name = "extract-reuters";
                this.Description = FromResource("Description");
                this.ExtendedHelpText = FromResource("ExtendedHelpText");

                this.Argument("<INPUT_DIRECTORY>", FromResource("InputDirectoryDescription"));
                this.Argument("<OUTPUT_DIRECTORY>", FromResource("OutputDirectoryDescription"));

                this.OnExecute(() => new BenchmarkExtractReutersCommand().Run(this));
            }
        }

        public int Run(ConfigurationBase cmd)
        {
            if (!cmd.ValidateArguments(2))
            {
                return 1;
            }

            cmd.Main(cmd.GetNonNullArguments());
            return 0;
        }
    }
}
