using Lucene.Net.Attributes;
using Lucene.Net.Cli.CommandLine;
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

    public class IndexFixCommandTest : CommandTestCase
    {
        protected override ConfigurationBase CreateConfiguration(MockConsoleApp app)
        {
            return new IndexFixCommand.Configuration(new CommandLineOptions()) { Main = (args) => app.Main(args) };
        }

        protected override IList<Arg[]> GetOptionalArgs()
        {
            // NOTE: We must order this in the sequence of the expected output.
            return new List<Arg[]>()
            {
                new Arg[] {
                    new Arg(inputPattern: "", output: new string[] { "-fix" }),
                    new Arg(inputPattern: "--dry-run", output: Arrays.Empty<string>()),
                },
                new Arg[] { new Arg(inputPattern: "-c|--cross-check-term-vectors", output: new string[] { "-crossCheckTermVectors" }) },
                new Arg[] { new Arg(inputPattern: "-v|--verbose", output: new string[] { "-verbose" }) },
                new Arg[] { new Arg(inputPattern: "-dir SimpleFSDirectory|--directory-type SimpleFSDirectory", output: new string[] { "-dir-impl", "SimpleFSDirectory" }) },
            };
        }

        protected override IList<Arg[]> GetRequiredArgs()
        {
            // NOTE: We must order this in the sequence of the expected output.
            return new List<Arg[]>()
            {
                new Arg[] { new Arg(inputPattern: @"C:\lucene-temp", output: new string[] { @"C:\lucene-temp" }) }
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
                    // Special case: the -fix option must be specified when --dry-run is not
                    .Concat(new string[] { "-fix" }).ToArray());
            }

            foreach (var requiredArg in requiredArgs)
            {
                foreach (var optionalArg in optionalArgs)
                {
                    string command = string.Join(" ", requiredArg.Select(x => x.InputPattern).Union(optionalArg.Select(x => x.InputPattern).ToArray()));
                    string[] expected = requiredArg.SelectMany(x => x.Output)
                        // Special case: the -fix option must be specified when --dry-run is not
                        .Concat(command.Contains("--dry-run") ? Arrays.Empty<string>() : new string[] { "-fix" })
                        .Union(optionalArg.SelectMany(x => x.Output)).ToArray();
                    AssertCommandTranslation(command, expected);
                }
            }
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestNoArguments()
        {
            System.IO.Directory.SetCurrentDirectory(RootDirectory);
            AssertCommandTranslation("", new string[] { RootDirectory, "-fix" });
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestTooManyArguments()
        {
            Assert.Throws<CommandParsingException>(() => AssertConsoleOutput("one two", ""));
        }
    }
}
