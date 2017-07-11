using Lucene.Net.Attributes;
using Lucene.Net.Cli.CommandLine;
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

    public class IndexCommandTest : CommandTestCase
    {
        protected override ConfigurationBase CreateConfiguration(MockConsoleApp app)
        {
            return new IndexCommand.Configuration(new CommandLineOptions());
        }

        protected override IList<Arg[]> GetOptionalArgs()
        {
            return new List<Arg[]>();
        }

        protected override IList<Arg[]> GetRequiredArgs()
        {
            return new List<Arg[]>();
        }

        /// <summary>
        /// Ensures the current working directory is used when index directory is not supplied. 
        /// </summary>
        [Test]
        [LuceneNetSpecific]
        public virtual void TestNoArguments()
        {
            AssertConsoleOutput("", "Lucene.Net Command Line Utility, Version");
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestTooManyArguments()
        {
            Assert.Throws<CommandParsingException>(() => AssertConsoleOutput("one", ""));
        }
    }
}
