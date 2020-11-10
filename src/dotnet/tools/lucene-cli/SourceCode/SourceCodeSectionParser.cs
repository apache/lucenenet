using System.IO;
using System.Text;

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
    /// </summary>
    public class SourceCodeSectionParser
    {
        public static readonly Encoding ENCODING = Encoding.UTF8;


        /// <summary>
        /// Parses the source code from the <paramref name="input"/> and places the
        /// valid lines (the lines that are not commented with a token,
        /// those that are included with a token, and "normal" lines)
        /// into the <paramref name="output"/>.
        /// </summary>
        /// <param name="input">A stream with the input data. This stream will still be open when the call completes.</param>
        /// <param name="output">A stream where the output data will be sent. This stream will still be open when the call completes.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance method allows swapping implementation at some point")]
        public void ParseSourceCodeFiles(Stream input, Stream output)
        {
            using var reader = new SourceCodeSectionReader(new StreamReader(input, ENCODING, false, 1024, true));
            using TextWriter writer = new StreamWriter(output, ENCODING, 1024, true);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                writer.WriteLine(line);
            }
        }
    }
}
