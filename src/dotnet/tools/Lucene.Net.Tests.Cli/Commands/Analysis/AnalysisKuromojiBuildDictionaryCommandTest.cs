using Lucene.Net.Attributes;
using Lucene.Net.Cli.CommandLine;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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

    public class AnalysisKuromojiBuildDictionaryCommandTest : CommandTestCase
    {
        protected override ConfigurationBase CreateConfiguration(MockConsoleApp app)
        {
            return new AnalysisKuromojiBuildDictionaryCommand.Configuration(new CommandLineOptions()) { Main = (args) => app.Main(args) };
        }

        protected override IList<Arg[]> GetOptionalArgs()
        {
            // NOTE: We must order this in the sequence of the expected output.
            return new List<Arg[]>()
            {
                new Arg[] { new Arg(inputPattern: "-e UTF-16|--encoding UTF-16", output: new string[] { "--encoding", "UTF-16" }) },
                new Arg[] { new Arg(inputPattern: "-n|--normalize", output: new string[] { "true" }) },
            };
        }
        protected override IList<Arg[]> GetRequiredArgs()
        {
            // NOTE: We must order this in the sequence of the expected output.
            return new List<Arg[]>()
            {
                new Arg[] { new Arg(inputPattern: "epidic", output: new string[] { @"epidic" }) },
                new Arg[] { new Arg(inputPattern: @"C:\lucene-input", output: new string[] { @"C:\lucene-input" }) },
                new Arg[] { new Arg(inputPattern: @"C:\lucene-output", output: new string[] { @"C:\lucene-output" }) },
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
                    
                    .Concat(new string[] {
                        // Special case: the encoding must always be supplied
                        "euc-jp",
                        // Special case: normalize must always be supplied
                        "false"
                    }).ToArray());
            }

            foreach (var requiredArg in requiredArgs)
            {
                foreach (var optionalArg in optionalArgs)
                {
                    string command = string.Join(" ", requiredArg.Select(x => x.InputPattern).Union(optionalArg.Select(x => x.InputPattern).ToArray()));
                    string[] expected = requiredArg.SelectMany(x => x.Output)
                        // Special case: the encoding must always be supplied
                        .Concat(Regex.IsMatch(command, "-e|--encoding") ? new string[] { "UTF-16" } : new string[] { "euc-jp" })
                        // Special case: the encoding must always be supplied
                        .Concat(Regex.IsMatch(command, "-n|--normalize") ? new string[] { "true" } : new string[] { "false" }).ToArray();
                    AssertCommandTranslation(command, expected);
                }
            }
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestNotEnoughArguments()
        {
            AssertConsoleOutput("one two", FromResource("NotEnoughArguments", 3));
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestTooManyArguments()
        {
            Assert.Throws<CommandParsingException>(() => AssertConsoleOutput("one two three four", ""));
        }
    }
}
