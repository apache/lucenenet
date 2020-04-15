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
using System.Linq;
using System.Text;

namespace JavaDocToMarkdownConverter.Formatters
{
    public class DemoCodeFormatter
    {
        public static void ProcessDemoFiles(ConvertedDocument convertedDoc)
        {
            //custom processor for processing the Demo *.cs files so that when the docs build the csharp is visible directly in the docs without requiring
            //to navigate to the GitHub source. This is done by extracting just the code part of the *.cs files and creating docfx override files for those
            //classes.

            var demoCsFolder = convertedDoc.OutputFile.Directory;
            var apiDocsFolder = Path.Combine(demoCsFolder.Parent.Parent.FullName, "websites\\apidocs");
            if (!Directory.Exists(apiDocsFolder))
                throw new InvalidOperationException($"The folder {apiDocsFolder} does not exist");
            var apiSpecFolder = Path.Combine(apiDocsFolder, "apiSpec");
            Directory.CreateDirectory(apiSpecFolder);
            foreach (var folder in demoCsFolder.EnumerateDirectories().Where(x => x.Name != "Properties" && x.Name != "obj" && x.Name != "bin"))
            {
                foreach (var file in folder.EnumerateFiles("*.cs"))
                {
                    var code = CodeExtractor.ExtractCode(file);
                    var ns = CodeExtractor.ExtractNamespace(code);
                    var c = CodeExtractor.ExtractClassName(code);
                    var type = $"{ns}.{c}";
                    //naming is based on a docfx convention - not a strict convention but we might as well be consistent
                    var overrideFileName = $"{ns.Replace(".", "_")}_{c}.md";
                    File.WriteAllText(Path.Combine(apiSpecFolder, overrideFileName), AppendYamlHeader(type, code));
                }
            }
            
        }

        private static string AppendYamlHeader(string typeName, string fileContent)
        {
            var sb = new StringBuilder();
            sb.AppendLine("---");
            sb.Append("uid: ");
            sb.AppendLine(typeName);
            // This is special syntax to denote an 'array' of strings since that is what the 'example' metadata requires, 
            // see:https://dotnet.github.io/docfx/tutorial/intro_overwrite_files.html#managed-reference-model
            // This syntax doesn't seem to be documented anywhere except in the issue tracker
            //  see https://github.com/dotnet/docfx/issues/375#issuecomment-225407949
            //  see https://github.com/dotnet/docfx/issues/1685#issuecomment-303644744
            sb.AppendLine("example: [*content]");
            //sb.Append("summary: ");
            //sb.AppendLine(summary);
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine(fileContent);
            sb.AppendLine("```");

            return sb.ToString();
        }
    }
}
