/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace JavaDocToMarkdownConverter.Formatters
{
    /// <summary>
    /// Used to extract the code from a *.cs file while ignoring license headers, etc...
    /// </summary>
    public static class CodeExtractor
    {
        private const string Start = "using ";
        private const string End = "}";
        private static readonly Regex Ns = new Regex(@"^namespace\s+?([\w\.\s]+?)\s", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex Class = new Regex(@"public class\s+?([\w\.\s]+?)\s", RegexOptions.Compiled | RegexOptions.Multiline);

        public static string ExtractCode(FileInfo inputDoc)
        {
            var content = File.ReadAllText(inputDoc.FullName);
            var start = content.IndexOf(Start);
            var end = content.LastIndexOf(End);
            var code = content.Substring(start, (end - start) + 1);

            return code;
        }

        public static string ExtractNamespace(string code)
        {
            var ns = Ns.Match(code).Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(ns)) throw new InvalidOperationException("Could not detect namespace");
            return ns;
        }

        public static string ExtractClassName(string code)
        {
            var c = Class.Match(code).Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(c)) throw new InvalidOperationException("Could not detect class");
            return c;
        }
    }
}
