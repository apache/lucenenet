using Lucene.Net.Cli.CommandLine;
using Lucene.Net.Cli.SourceCode;
using System;
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

    public abstract class DemoConfiguration : ConfigurationBase
    {
        private static readonly SourceCodeExporter sourceCodeExporter = new SourceCodeExporter();
        protected readonly CommandOption viewSourceOption;
        protected readonly CommandOption outputSourceOption;

        protected DemoConfiguration()
        {

            this.viewSourceOption = this.Option(
                "-src|--view-source-code",
                Resources.Strings.ViewSourceCodeDescription,
                CommandOptionType.NoValue);
            this.outputSourceOption = this.Option(
                "-out|--output-source-code <DIRECTORY>",
                Resources.Strings.OutputSourceCodeDescription,
                CommandOptionType.SingleValue);

            this.viewSourceOption.ShowInHelpText = false;
            this.outputSourceOption.ShowInHelpText = false;
        }

        public abstract IEnumerable<string> SourceCodeFiles { get; }

        public override void OnExecute(Func<int> invoke)
        {
            base.OnExecute(() =>
            {
                bool viewSource = viewSourceOption.HasValue();
                bool outputSource = outputSourceOption.HasValue();

                if (viewSource || outputSource)
                {
                    if (outputSource)
                    {
                        Out.WriteLine(Resources.Strings.ExportingSourceCodeMessage);

                        string outputPath = outputSourceOption.Value();
                        sourceCodeExporter.ExportSourceCodeFiles(this.SourceCodeFiles, outputPath);

                        Out.WriteLine(string.Format(Resources.Strings.ExportingSourceCodeCompleteMessage, outputPath));
                    }
                    if (viewSource)
                    {
                        using var console = new ConsolePager(this.SourceCodeFiles);
                        console.Run();
                    }

                    return 0;
                }

                var result = invoke();
                ShowOutputSourceCodeMessage();
                return result;
            });
        }

        public virtual void ShowOutputSourceCodeMessage()
        {
            this.Out.WriteLine();
            this.Out.WriteLine("-------------------------");
            this.Out.WriteLine(Resources.Strings.OutputSourceCodeMessage, this.Name);
        }
    }
}
