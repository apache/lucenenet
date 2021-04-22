using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Benchmarks.Utils
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
    /// Split the Reuters SGML documents into Simple Text files containing: Title, Date, Dateline, Body
    /// </summary>
    public class ExtractReuters
    {
        private readonly DirectoryInfo reutersDir; // LUCENENET: marked readonly
        private readonly DirectoryInfo outputDir; // LUCENENET: marked readonly
        private static readonly string LINE_SEPARATOR = Environment.NewLine;

        public ExtractReuters(DirectoryInfo reutersDir, DirectoryInfo outputDir)
        {
            this.reutersDir = reutersDir;
            this.outputDir = outputDir;
            Console.WriteLine("Deleting all files in " + outputDir);
            foreach (FileInfo f in outputDir.EnumerateFiles())
            {
                f.Delete();
            }
        }

        public virtual void Extract()
        {
            FileInfo[] sgmFiles = reutersDir.GetFiles("*.sgm");
            if (sgmFiles != null && sgmFiles.Length > 0)
            {
                foreach (FileInfo sgmFile in sgmFiles)
                {
                    ExtractFile(sgmFile);
                }
            }
            else
            {
                Console.Error.WriteLine("No .sgm files in " + reutersDir);
            }
        }

        internal static readonly Regex EXTRACTION_PATTERN = new Regex("<TITLE>(.*?)</TITLE>|<DATE>(.*?)</DATE>|<BODY>(.*?)</BODY>", RegexOptions.Compiled);

        private static readonly string[] META_CHARS = { "&", "<", ">", "\"", "'" };

        private static readonly string[] META_CHARS_SERIALIZATIONS = { "&amp;", "&lt;",
            "&gt;", "&quot;", "&apos;" };

        /// <summary>
        /// Override if you wish to change what is extracted
        /// </summary>
        protected virtual void ExtractFile(FileInfo sgmFile)
        {
            try
            {
                using TextReader reader = new StreamReader(new FileStream(sgmFile.FullName, FileMode.Open, FileAccess.Read), Encoding.UTF8);
                StringBuilder buffer = new StringBuilder(1024);
                StringBuilder outBuffer = new StringBuilder(1024);

                string line = null;
                int docNumber = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    // when we see a closing reuters tag, flush the file

                    if (line.IndexOf("</REUTERS", StringComparison.Ordinal) == -1)
                    {
                        // Replace the SGM escape sequences

                        buffer.Append(line).Append(' ');// accumulate the strings for now,
                                                        // then apply regular expression to
                                                        // get the pieces,
                    }
                    else
                    {
                        // Extract the relevant pieces and write to a file in the output dir
                        Match matcher = EXTRACTION_PATTERN.Match(buffer.ToString());
                        if (matcher.Success)
                        {
                            do
                            {
                                for (int i = 1; i <= matcher.Groups.Count; i++)
                                {
                                    if (matcher.Groups[i] != null)
                                    {
                                        outBuffer.Append(matcher.Groups[i].Value);
                                    }
                                }
                                outBuffer.Append(LINE_SEPARATOR).Append(LINE_SEPARATOR);
                            } while ((matcher = matcher.NextMatch()).Success);
                        }

                        string @out = outBuffer.ToString();
                        for (int i = 0; i < META_CHARS_SERIALIZATIONS.Length; i++)
                        {
                            @out = @out.Replace(META_CHARS_SERIALIZATIONS[i], META_CHARS[i]);
                        }
                        string outFile = System.IO.Path.Combine(outputDir.FullName, sgmFile.Name + "-"
                            + (docNumber++) + ".txt");
                        // System.out.println("Writing " + outFile);
                        StreamWriter writer = new StreamWriter(new FileStream(outFile, FileMode.Create, FileAccess.Write), Encoding.UTF8);
                        writer.Write(@out);
                        writer.Dispose();
                        outBuffer.Length = 0;
                        buffer.Length = 0;
                    }
                }
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw RuntimeException.Create(e);
            }
        }

        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Usage("Wrong number of arguments (" + args.Length + ")");
                return;
            }
            DirectoryInfo reutersDir = new DirectoryInfo(args[0]);
            if (!reutersDir.Exists)
            {
                Usage("Cannot find Path to Reuters SGM files (" + reutersDir + ")");
                return;
            }

            // First, extract to a tmp directory and only if everything succeeds, rename
            // to output directory.
            DirectoryInfo outputDir = new DirectoryInfo(args[1]);
            outputDir = new DirectoryInfo(outputDir.FullName + "-tmp");
            outputDir.Create();
            ExtractReuters extractor = new ExtractReuters(reutersDir, outputDir);
            extractor.Extract();
            // Now rename to requested output dir
            outputDir.MoveTo(args[1]);
        }

        private static void Usage(string msg)
        {
            // LUCENENET specific - our wrapper console shows correct usage
            throw new ArgumentException(msg);
            //Console.Error.WriteLine("Usage: " + msg + " :: java -cp <...> org.apache.lucene.benchmark.utils.ExtractReuters <Path to Reuters SGM files> <Output Path>");
        }
    }
}
