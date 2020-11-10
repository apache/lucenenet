using J2N;
using System.Collections.Generic;
using System.IO;

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

    /// <summary>
    /// Exports source code files from embedded resources and includes or
    /// excludes any sections that are marked by comment tokens. See
    /// <see cref="SourceCodeSectionReader"/> for for the supported tokens.
    /// </summary>
    public class SourceCodeExporter
    {
        protected SourceCodeSectionParser sectionParser = new SourceCodeSectionParser();

        /// <summary>
        /// Reads the provided source code <paramref name="files"/> from the 
        /// embeded resources within this assembly
        /// and writes them out to the <paramref name="outputPath"/>.
        /// </summary>
        /// <param name="files">An <see cref="IEnumerable{T}"/> of files to export.</param>
        /// <param name="outputPath">A directory where the files will be exported.</param>
        public void ExportSourceCodeFiles(IEnumerable<string> files, string outputPath)
        {
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            foreach (var file in files)
            {
                using var input = typeof(Program).FindAndGetManifestResourceStream(file);
                using var output = new FileStream(Path.Combine(outputPath, file), FileMode.Create, FileAccess.Write);
                sectionParser.ParseSourceCodeFiles(input, output);
            }
        }
    }
}
