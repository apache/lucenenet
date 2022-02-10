using J2N;
using System;
using System.Collections;
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
    /// Interactively pages or scrolls through the list of files provided in
    /// the constructor.
    /// <para/>
    /// <b>Commands</b>:
    /// <list type="table">
    ///     <listheader>
    ///         <term>Keys</term>
    ///         <description>Description</description>
    ///     </listheader>
    ///     <item>
    ///         <term><c>n</c> or Space</term>
    ///         <description>Pages to the next full screen of text.</description>
    ///     </item>
    ///     <item>
    ///         <term><c>q</c> or <c>x</c></term>
    ///         <description>Exits the application.</description>
    ///     </item>
    ///     <item>
    ///         <term>Enter</term>
    ///         <description>
    ///             Moves to the next line of text. Hold down
    ///             the Enter key to scroll.
    ///         </description>
    ///     </item>
    /// </list>
    /// </summary>
    public sealed class ConsolePager : IDisposable
    {
        private readonly MultipleFileLineEnumerator enumerator;

        public ConsolePager(IEnumerable<string> files)
        {
            if (files is null)
                throw new ArgumentNullException(nameof(files));
            this.enumerator = new MultipleFileLineEnumerator(files);
        }

        public TextWriter Out { get; set; } = Console.Out;
        public TextReader In { get; set; } = Console.In;
        public Func<int> GetWindowHeight { get; set; } = () => Console.WindowHeight;

        public void Run()
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                int take = GetWindowHeight();
                int count = 0;
                bool done = false;
                do
                {
                    while (count++ < take)
                    {
                        done = !enumerator.MoveNext();
                        if (done) break;
                        Out.WriteLine(enumerator.Current);
                    }
                    count = 0; // Reset
                    bool valid = false;
                    while (!valid)
                    {
                        var keyInfo = Console.ReadKey(true);

                        switch (keyInfo.KeyChar)
                        {
                            case 'q': // quit
                            case 'x':
                                done = valid = true;
                                break;
                            case 'n':
                            case ' ':
                                take = GetWindowHeight(); // Get next page
                                valid = true;
                                break;
                            case (char)13: // ENTER
                                take = 1; // Get a single line
                                valid = true;
                                break;
                        }
                    }
                } while (!done);
            }
            finally
            {
                Console.ResetColor();
            }
        }

        public void Dispose()
        {
            this.enumerator?.Dispose();
        }

        /// <summary>
        /// Enumerates through a list of files (embedded resources)
        /// as if they were one contiguous set of text.
        /// </summary>
        internal sealed class MultipleFileLineEnumerator : IEnumerator<string>
        {
            private readonly IEnumerator<string> fileEnumerator;
            private TextReader currentFile;
            private string line = null;

            public MultipleFileLineEnumerator(IEnumerable<string> files)
            {
                if (files is null)
                    throw new ArgumentNullException(nameof(files));
                this.fileEnumerator = files.GetEnumerator();
                NextFile();
            }

            private bool NextFile()
            {

                if (this.fileEnumerator.MoveNext())
                {
                    currentFile = new SourceCodeSectionReader(new StreamReader(
                        typeof(Program).FindAndGetManifestResourceStream(this.fileEnumerator.Current), 
                        SourceCodeSectionParser.ENCODING));
                    return true;
                }
                return false;
            }

            public string Current => line;

            object IEnumerator.Current => line;

            public void Dispose()
            {
                this.fileEnumerator?.Dispose();
                this.currentFile?.Dispose();
            }

            public bool MoveNext()
            {
                line = this.currentFile.ReadLine();
                if (line is null)
                {
                    if (!NextFile())
                    {
                        return false;
                    }

                    line = this.currentFile.ReadLine();
                }
                return line != null;
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }
        }
    }
}
