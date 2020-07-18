using Lucene.Net.Attributes;
using Lucene.Net.Cli.CommandLine;
using Lucene.Net.Index;
using Lucene.Net.Store;
using NUnit.Framework;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

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

    public class IndexUpgradeCommandTest : CommandTestCase
    {
        protected override ConfigurationBase CreateConfiguration(MockConsoleApp app)
        {
            return new IndexUpgradeCommand.Configuration(new CommandLineOptions()) { Main = (args) => app.Main(args) };
        }

        protected override IList<Arg[]> GetOptionalArgs()
        {
            // NOTE: We must order this in the sequence of the expected output.
            return new List<Arg[]>()
            {
                new Arg[] { new Arg(inputPattern: "-d|--delete-prior-commits", output: new string[] { "-delete-prior-commits" }) },
                new Arg[] { new Arg(inputPattern: "-v|--verbose", output: new string[] { "-verbose" }) },
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

        /// <summary>
        /// Ensures the current working directory is used when index directory is not supplied. 
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public virtual void TestNoArguments()
        {
            System.IO.Directory.SetCurrentDirectory(RootDirectory);
            AssertCommandTranslation("", new string[] { RootDirectory });
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestTooManyArguments()
        {
            Assert.Throws<CommandParsingException>(() => AssertConsoleOutput("one two", ""));
        }

        /// <summary>
        /// Integration test to ensure --verbose argument is passed through and parsed correctly by IndexUpgrader
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public virtual void TestPassingVerboseArgument()
        {
            MockConsoleApp output;
            IndexUpgrader upgrader;

            output = RunCommand(@"C:\test-index");
            upgrader = IndexUpgrader.ParseArgs(output.Args);
            Assert.AreSame(Util.InfoStream.Default, upgrader.iwc.InfoStream);

            output = RunCommand(@"C:\test-index -v");
            upgrader = IndexUpgrader.ParseArgs(output.Args);
            Assert.AreNotSame(Util.InfoStream.Default, upgrader.iwc.InfoStream);

            output = RunCommand(@"C:\test-index --verbose");
            upgrader = IndexUpgrader.ParseArgs(output.Args);
            Assert.AreNotSame(Util.InfoStream.Default, upgrader.iwc.InfoStream);
        }

        /// <summary>
        /// Integration test to ensure --delete-prior-commits argument is passed through and parsed correctly by IndexUpgrader
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public virtual void TestPassingDeletePriorCommitsArgument()
        {
            MockConsoleApp output;
            IndexUpgrader upgrader;

            output = RunCommand(@"C:\test-index");
            upgrader = IndexUpgrader.ParseArgs(output.Args);
            Assert.IsFalse(upgrader.deletePriorCommits);

            output = RunCommand(@"C:\test-index -d");
            upgrader = IndexUpgrader.ParseArgs(output.Args);
            Assert.IsTrue(upgrader.deletePriorCommits);

            output = RunCommand(@"C:\test-index --delete-prior-commits");
            upgrader = IndexUpgrader.ParseArgs(output.Args);
            Assert.IsTrue(upgrader.deletePriorCommits);
        }

        /// <summary>
        /// Integration test to ensure --directory-type argument is passed through and parsed correctly by IndexUpgrader
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public virtual void TestPassingDirectoryTypeArgument()
        {
            MockConsoleApp output;
            IndexUpgrader upgrader;
            var tempDir = CreateTempDir("index-upgrader");

            output = RunCommand(@"C:\test-index");
            upgrader = IndexUpgrader.ParseArgs(output.Args);
            Assert.AreEqual(FSDirectory.Open(tempDir).GetType(), upgrader.dir.GetType());

            output = RunCommand(@"C:\test-index -dir SimpleFSDirectory");
            upgrader = IndexUpgrader.ParseArgs(output.Args);
            Assert.AreEqual(typeof(SimpleFSDirectory), upgrader.dir.GetType());

            output = RunCommand(@"C:\test-index --directory-type SimpleFSDirectory");
            upgrader = IndexUpgrader.ParseArgs(output.Args);
            Assert.AreEqual(typeof(SimpleFSDirectory), upgrader.dir.GetType());

            output = RunCommand(@"C:\test-index -dir MMapDirectory");
            upgrader = IndexUpgrader.ParseArgs(output.Args);
            Assert.AreEqual(typeof(MMapDirectory), upgrader.dir.GetType());

            output = RunCommand(@"C:\test-index --directory-type MMapDirectory");
            upgrader = IndexUpgrader.ParseArgs(output.Args);
            Assert.AreEqual(typeof(MMapDirectory), upgrader.dir.GetType());
        }
    }
}
