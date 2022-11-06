using System;
using System.IO;
using System.Text.RegularExpressions;

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
    /// A <see cref="TextReader"/> that conditionally includes or excludes lines 
    /// of source code based on commented sections in the original
    /// source.
    /// </summary>
    /// <remarks>
    /// Idenitifies sections of code based on tokens to transform 
    /// the output code to either contain extra code sections or
    /// remove unwanted code sections.
    /// <para/>
    /// There are 5 different types of tokens considered:
    /// <list type="table">
    ///     <item>
    ///         <term>// &lt;comment&gt;</term>
    ///         <description>
    ///             Beginning of commented block. This line and all lines 
    ///             until the end of a comment block are ignored.
    ///             </description>
    ///     </item>
    ///     <item>
    ///         <term>// &lt;\comment&gt;</term>
    ///         <description>
    ///             End of a commented block. This line is ignored, but any 
    ///             lines following will be included.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>// &lt;include&gt;</term>
    ///         <description>
    ///             Beginning of an include block. This line is ignored, but 
    ///             all lines following will have the //// comment marker 
    ///             removed from the beginning of the line. Effectively, 
    ///             it uncomments lines of code that were previously commented 
    ///             and ignored by the compiler. All normal C# comments (// and ///) 
    ///             are ignored and left in place.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>// &lt;\\include&gt;</term>
    ///         <description>
    ///             End of an include block. This line is ignored and following
    ///             lines will be treated normally again. In other words, the ////
    ///             comment will no longer be removed from following lines.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>////</term>
    ///         <description>
    ///             A double comment. This comment is removed in include blocks
    ///             to uncomment code that was previously commented and ignored
    ///             by the compiler. This allows adding using directives, alternate
    ///             type names, etc. to be output in the demo even if they don't exist
    ///             in the compiled application.
    ///         </description>
    ///     </item>
    /// </list>
    /// </remarks>
    public class SourceCodeSectionReader : TextReader
    {
        public static readonly Regex COMMENT_START = new Regex(@"//\s*?<comment>", RegexOptions.Compiled);
        public static readonly Regex COMMENT_END = new Regex(@"//\s*?</comment>", RegexOptions.Compiled);
        public static readonly Regex INCLUDE_START = new Regex(@"//\s*?<include>", RegexOptions.Compiled);
        public static readonly Regex INCLUDE_END = new Regex(@"//\s*?</include>", RegexOptions.Compiled);
        public static readonly Regex TO_UNCOMMENT = new Regex(@"////", RegexOptions.Compiled);

        private bool inComment = false;
        private bool inInclude = false;

        private readonly TextReader reader;

        public SourceCodeSectionReader(TextReader reader)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public override string ReadLine()
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (inComment)
                {
                    if (COMMENT_END.IsMatch(line))
                    {
                        inComment = false;
                        // continue; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
                    }
                }
                else
                {
                    if (COMMENT_START.IsMatch(line))
                    {
                        inComment = true;
                        continue; // Skip this line
                    }
                    else
                    {
                        // If not in a comment, consider source code includes.
                        // In this case, we will remove //// from the beginning of
                        // each line if it exists.
                        if (inInclude)
                        {
                            if (INCLUDE_END.IsMatch(line))
                            {
                                inInclude = false;
                                continue; // Skip this line
                            }
                            line = TO_UNCOMMENT.Replace(line, string.Empty, 1);
                        }
                        else if (INCLUDE_START.IsMatch(line))
                        {
                            inInclude = true;
                            continue; // Skip this line
                        }
                    }

                    // All other lines, include the line
                    return line;
                }
            }
            return line;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.reader?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
