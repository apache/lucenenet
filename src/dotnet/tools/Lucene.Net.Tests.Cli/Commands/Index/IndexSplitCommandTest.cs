using Lucene.Net.Attributes;
using Lucene.Net.Support;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Cli.Commands
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

    public class IndexSplitCommandTest : CommandTestCase
    {
        protected override ConfigurationBase CreateConfiguration(MockConsoleApp app)
        {
            return new IndexSplitCommand.Configuration(new CommandLineOptions()) { Main = (args) => app.Main(args) };
        }

        protected override IList<Arg[]> GetOptionalArgs()
        {
            // NOTE: We must order this in the sequence of the expected output.
            return new List<Arg[]>()
            {
                new Arg[] { new Arg(inputPattern: "-n 20|--number-of-parts 20", output: new string[] { "-num", "20" }) },
                new Arg[] { new Arg(inputPattern: "-s|--sequential", output: new string[] { "-seq" }) },
            };
        }

        protected override IList<Arg[]> GetRequiredArgs()
        {
            // NOTE: We must order this in the sequence of the expected output.
            return new List<Arg[]>()
            {
                new Arg[] { new Arg(inputPattern: @"C:\lucene-temp", output: Arrays.Empty<string>() /*"-out", @"C:\lucene-temp"*/) },
                new Arg[] {
                    new Arg(inputPattern: @"C:\lucene-temp2 C:\lucene-temp3", output: new string[] { @"C:\lucene-temp2", @"C:\lucene-temp3" }),
                    new Arg(inputPattern: @"C:\lucene-temp2 C:\lucene-temp3 C:\lucene-temp4", output: new string[] { @"C:\lucene-temp2", @"C:\lucene-temp3", @"C:\lucene-temp4" }),
                    new Arg(inputPattern: @"C:\lucene-temp2 C:\lucene-temp3 C:\lucene-temp4 C:\lucene-temp5", output: new string[] { @"C:\lucene-temp2", @"C:\lucene-temp3", @"C:\lucene-temp4", @"C:\lucene-temp5" }),
                },
                new Arg[] { new Arg(inputPattern: "", output: new string[] { "-out", @"C:\lucene-temp" }) },
            };
        }

        [Test]
        [LuceneNetSpecific]
        public override void TestAllValidCombinations()
        {
            var requiredArgs = GetRequiredArgs().ExpandArgs().RequiredParameters();
            var optionalArgs = GetOptionalArgs().ExpandArgs().OptionalParameters();

            foreach (var requiredArg in requiredArgs)
            {
                AssertCommandTranslation(
                    string.Join(" ", requiredArg.Select(x => x.InputPattern).ToArray()),
                    requiredArg.SelectMany(x => x.Output)
                    // Special case: the -num option must be specified when -n is not
                    // because in MultiPassIndexSplitter it is not optional, so we are patching
                    // it in our command to make 2 the default.
                    .Concat(new string[] { "-num", "2" }).ToArray());
            }

            foreach (var requiredArg in requiredArgs)
            {
                foreach (var optionalArg in optionalArgs)
                {
                    string command = string.Join(" ", requiredArg.Select(x => x.InputPattern).Union(optionalArg.Select(x => x.InputPattern).ToArray()));
                    string[] expected = requiredArg.SelectMany(x => x.Output)
                        // Special case: the -num option must be specified when -n is not
                        // because in MultiPassIndexSplitter it is not optional, so we are patching
                        // it in our command to make 2 the default.
                        .Concat(command.Contains("-n") ? Arrays.Empty<string>() : new string[] { "-num", "2" })
                        .Union(optionalArg.SelectMany(x => x.Output)).ToArray();
                    AssertCommandTranslation(command, expected);
                }
            }
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestNotEnoughArguments()
        {
            AssertConsoleOutput("", FromResource("NotEnoughArguments", 2));
        }
    }
}
