using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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

    public abstract class CommandTestCase : LuceneTestCase
    {
        protected readonly string RootDirectory = Util.Constants.WINDOWS ? @"C:\" : (Constants.LINUX ? "/home" : /*macOS*/ "/");

        protected abstract ConfigurationBase CreateConfiguration(MockConsoleApp app);

        protected abstract IList<Arg[]> GetRequiredArgs();
        protected abstract IList<Arg[]> GetOptionalArgs();

        protected virtual void AssertCommandTranslation(string command, string[] expectedResult)
        {
            var output = new MockConsoleApp();
            var cmd = CreateConfiguration(output);
            cmd.Execute(command.ToArgs());

            Assert.False(output.CallCount < 1, "Main() method not called");
            Assert.False(output.CallCount > 1, "Main() method called more than once");

            Assert.AreEqual(expectedResult.Length, output.Args.Length);
            for (int i = 0; i < output.Args.Length; i++)
            {
                Assert.AreEqual(expectedResult[i], output.Args[i], "Command: {0}, Expected: {1}, Actual {2}", command, string.Join(",", expectedResult), string.Join(",", output.Args[i]));
            }
        }

        protected virtual void AssertConsoleOutput(string command, string expectedConsoleText)
        {
            var output = new MockConsoleApp();
            var cmd = CreateConfiguration(output);

            var console = new StringWriter();
            cmd.Out = console;
            cmd.Execute(command.ToArgs());

            string consoleText = console.ToString();
            Assert.True(consoleText.Contains(expectedConsoleText), "Expected output was {0}, actual console output was {1}", expectedConsoleText, consoleText);
        }

        public static string FromResource(string resourceName)
        {
            return Resources.Strings.ResourceManager.GetString(resourceName);
        }

        public static string FromResource(string resourceName, params object[] args)
        {
            return string.Format(Resources.Strings.ResourceManager.GetString(resourceName), args);
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestAllValidCombinations()
        {
            var requiredArgs = GetRequiredArgs().ExpandArgs().RequiredParameters();
            var optionalArgs = GetOptionalArgs().ExpandArgs().OptionalParameters();

            foreach (var requiredArg in requiredArgs)
            {
                AssertCommandTranslation(
                    string.Join(" ", requiredArg.Select(x => x.InputPattern).ToArray()),
                    requiredArg.SelectMany(x => x.Output).ToArray());
            }

            foreach (var requiredArg in requiredArgs)
            {
                foreach (var optionalArg in optionalArgs)
                {
                    string command = string.Join(" ", requiredArg.Select(x => x.InputPattern).Union(optionalArg.Select(x => x.InputPattern).ToArray()));
                    string[] expected = requiredArg.SelectMany(x => x.Output).Concat(optionalArg.SelectMany(x => x.Output)).ToArray();
                    AssertCommandTranslation(command, expected);
                }
            }
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestHelp()
        {
            AssertConsoleOutput("?", "Version");
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestCommandHasDescription()
        {
            var output = new MockConsoleApp();
            var cmd = CreateConfiguration(output);
            Assert.IsNotNull(cmd.Description);
            NUnit.Framework.Assert.IsNotEmpty(cmd.Description);
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestAllArgumentsHaveDescription()
        {
            var output = new MockConsoleApp();
            var cmd = CreateConfiguration(output);
            foreach (var arg in cmd.Arguments)
            {
                Assert.IsNotNull(arg.Description);
                Assert.IsNotEmpty(arg.Description);
            }
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestAllOptionsHaveDescription()
        {
            var output = new MockConsoleApp();
            var cmd = CreateConfiguration(output);
            foreach (var option in cmd.Options)
            {
                Assert.IsNotNull(option.Description);
                Assert.IsNotEmpty(option.Description);
            }
        }

        /// <summary>
        /// Runs a command against the current command configuration
        /// </summary>
        /// <param name="command">A command as a string that will be parsed.</param>
        /// <returns>A MockConsoleApp that can be used to inspect the number of calls and arguments that will be passed to the Lucene CLI tool.</returns>
        public virtual MockConsoleApp RunCommand(string command)
        {
            var output = new MockConsoleApp();
            CreateConfiguration(output).Execute(command.ToArgs());
            return output;
        }

        public class MockConsoleApp
        {
            public void Main(string[] args)
            {
                this.Args = args;
                this.CallCount++;
            }

            [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Used for testing arguments")]
            public string[] Args { get; private set; }
            public int CallCount { get; private set; }
        }

        public class Arg
        {
            public Arg(string inputPattern, string[] output)
            {
                InputPattern = inputPattern;
                Output = output;
            }

            public string InputPattern { get; private set; }

            [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Used for testing output")]
            public string[] Output { get; private set; }
        }
    }
}
