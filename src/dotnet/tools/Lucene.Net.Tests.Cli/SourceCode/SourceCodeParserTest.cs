using J2N;
using Lucene.Net.Attributes;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Cli.SourceCode
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

    public class SourceCodeParserTest
    {
        [Test]
        [LuceneNetSpecific]
        public void TestSourceCodeSectionParser()
        {
            var parser = new SourceCodeSectionParser();

            using var output = new MemoryStream();
            using (var input = this.GetType().FindAndGetManifestResourceStream("TestInputForParser.cs"))
            {
                parser.ParseSourceCodeFiles(input, output);
            }

            output.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(output, SourceCodeSectionParser.ENCODING);
            Assert.AreEqual("using System;", reader.ReadLine());
            Assert.AreEqual("using System.Collections.Generic;", reader.ReadLine());
            Assert.AreEqual("using System.Linq;", reader.ReadLine());
            Assert.AreEqual("using System.Threading.Tasks;", reader.ReadLine());
            Assert.AreEqual("using System.Reflection;", reader.ReadLine());
            Assert.AreEqual("using System.Xml;", reader.ReadLine());
            Assert.AreEqual("", reader.ReadLine());
            Assert.AreEqual("namespace Lucene.Net.Cli.SourceCode", reader.ReadLine());
            Assert.AreEqual("{", reader.ReadLine());
            Assert.AreEqual("    public class TestInputForParser", reader.ReadLine());
            Assert.AreEqual("    {", reader.ReadLine());
            Assert.AreEqual("        public void Foo()", reader.ReadLine());
            Assert.AreEqual("        {", reader.ReadLine());
            Assert.AreEqual("            Console.WriteLine(\"Foo\");", reader.ReadLine());
            Assert.AreEqual("        }", reader.ReadLine());
            Assert.AreEqual("", reader.ReadLine());
            Assert.AreEqual("        public void Bar()", reader.ReadLine());
            Assert.AreEqual("        {", reader.ReadLine());
            Assert.AreEqual("            Console.WriteLine(\"Bar2\");", reader.ReadLine());
            Assert.AreEqual("        }", reader.ReadLine());
            Assert.AreEqual("    }", reader.ReadLine());
            Assert.AreEqual("}", reader.ReadLine());
            Assert.AreEqual(null, reader.ReadLine());
            Assert.AreEqual(null, reader.ReadLine());
        }
    }
}
