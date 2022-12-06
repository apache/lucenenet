using System.IO;
using System.Reflection;

namespace Lucene.Net.Util
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
    /// LUCENENET specific rule for extracting the LineFileDocs file (or embedded resource) only once
    /// per assembly if the <see cref="UseTempLineDocsFileAttribute"/>.
    /// </summary>
    internal class UseTempLineDocsFileRule : AbstractBeforeAfterRule
    {
        private bool lineFileDocsExtracted;

        private static bool IsEnabled() // LUCENENET: CA1822: Mark members as static
        {
            bool enabled = false;
            Assembly assembly = RandomizedContext.CurrentContext.CurrentTestAssembly;

            if (assembly.HasAttribute<UseTempLineDocsFileAttribute>(inherit: true))
                enabled = true;
            else
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.IsClass || type.IsAbstract)
                        continue;
                    if (type.HasAttribute<UseTempLineDocsFileAttribute>(inherit: true))
                    {
                        enabled = true;
                        break;
                    }
                }
            }
            return enabled;
        }

        public override void Before()
        {
            if (IsEnabled())
            {
                // If we can cleanup, unzip the LineDocsFile 1 time for the entire test run,
                // which will dramatically improve performance.
                var temp = LineFileDocs.MaybeCreateTempFile(removeAfterClass: false);
                if (null != temp)
                {
                    lineFileDocsExtracted = true;
                    LuceneTestCase.TestLineDocsFile = temp;
                }
            }
        }

        public override void After()
        {
            if (lineFileDocsExtracted)
            {
                // Cleanup our LineDocsFile and reset LuceneTestCase back to its original state.
                try
                {
                    if (!string.IsNullOrEmpty(LuceneTestCase.TestLineDocsFile) && File.Exists(LuceneTestCase.TestLineDocsFile))
                        File.Delete(LuceneTestCase.TestLineDocsFile);
                }
                catch { }
                LuceneTestCase.TestLineDocsFile = SystemProperties.GetProperty("tests:linedocsfile", LuceneTestCase.DEFAULT_LINE_DOCS_FILE);
            }
        }
    }
}
