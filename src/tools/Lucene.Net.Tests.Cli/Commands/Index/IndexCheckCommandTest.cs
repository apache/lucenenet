using NUnit.Framework;
using System.Collections.Generic;

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

    // LUCENENET TODO: Test to ensure all of the commands and arguments have a description (in all commands except for root)

    public class IndexCheckCommandTest : CommandTestCase
    {
        protected override ConfigurationBase CreateConfiguration(MockConsoleApp app)
        {
            return new IndexCheckCommand.Configuration(new CommandLineOptions()) { Main = (args) => app.Main(args) };
        }

        protected override IList<Arg[]> GetOptionalArgs()
        {
            // NOTE: We must order this in the sequence of the expected output.
            return new List<Arg[]>()
            {
                new Arg[] { new Arg(inputPattern: "-c|--cross-check-term-vectors", output: new string[] { "-crossCheckTermVectors" }) },
                new Arg[] { new Arg(inputPattern: "--verbose", output: new string[] { "-verbose" }) },
                new Arg[] {
                    new Arg(inputPattern: "-s _seg1|--segment _seg1", output: new string[] { "-segment", "_seg1" }),
                    new Arg(inputPattern: "-s _seg1 -s _seg2|--segment _seg1 --segment _seg2", output: new string[] { "-segment", "_seg1", "-segment", "_seg2" }),
                    new Arg(inputPattern: "-s _seg1 -s _seg2 -s _seg3|--segment _seg1 --segment _seg2 --segment _seg3", output: new string[] { "-segment", "_seg1", "-segment", "_seg2", "-segment", "_seg3" })
                },
                new Arg[] { new Arg(inputPattern: "-dir SimpleFSDirectory|--directory-type SimpleFSDirectory", output: new string[] { "-dir-impl", "SimpleFSDirectory" }) },
            };
        }
        protected override IList<Arg[]> GetRequiredArgs()
        {
            // NOTE: We must order this in the sequence of the expected output.
            return new List<Arg[]>()
            {
                new Arg[] { new Arg(inputPattern: @"C:\lucene-temp", output: new string[] { @"C:\lucene-temp" }) },
            };
        }

        



        //[Test]
        //public void TestAllOptionsShort()
        //{
        //    AssertCommandTranslation(
        //        @"C:\lucene-temp -v -c -dir SimpleFSDirectory -s _seg1 -s _seg2 -s _seg3",
        //        new string[] {
        //            @"C:\lucene-temp", "-crossCheckTermVectors", "-verbose",
        //            "-segment", "_seg1", "-segment", "_seg2", "-segment", "_seg3",
        //            "-dir-impl", "SimpleFSDirectory"
        //        });

        //    //var output = new MockConsoleApp();
        //    //var cmd = new IndexCheckCommand.Configuration(new CommandLineOptions()) { Main = (args) => output.Main(args) };

        //    //string input = @"C:\lucene-temp -v -c -dir SimpleFSDirectory -s _seg1 -s _seg2 -s _seg3";
        //    //cmd.Execute(input.ToArgs());

        //    //Assert.AreEqual(@"C:\lucene-temp", output.Args[0]);
        //    //Assert.True(output.Args.Contains("-crossCheckTermVectors"));
        //    //Assert.True(output.Args.Contains("-verbose"));
        //    //Assert.AreEqual("SimpleFSDirectory", output.Args.OptionValue("-dir-impl"));
        //    //Assert.True(new HashSet<string>(output.Args.OptionValues("-segment")).SetEquals(new HashSet<string>(new string[] { "_seg1", "_seg2", "_seg3" })));
        //    //Assert.False(output.Args.Contains("-fix"));
        //}

        //[Test]
        //public void TestAllOptionsLong()
        //{
        //    AssertCommandTranslation(
        //        @"C:\lucene-temp --verbose --cross-check-term-vectors --directory-type SimpleFSDirectory --segment _seg1 --segment _seg2 --segment _seg3",
        //        new string[] {
        //            @"C:\lucene-temp", "-crossCheckTermVectors", "-verbose",
        //            "-segment", "_seg1", "-segment", "_seg2", "-segment", "_seg3",
        //            "-dir-impl", "SimpleFSDirectory"
        //        });
        //}

        /// <summary>
        /// Ensures the current working directory is used when index directory is not supplied. 
        /// </summary>
        [Test]
        public void TestNoArguments()
        {
            System.IO.Directory.SetCurrentDirectory(@"C:\");
            AssertCommandTranslation("", new string[] { @"C:\" });
        }

        
    }
}
