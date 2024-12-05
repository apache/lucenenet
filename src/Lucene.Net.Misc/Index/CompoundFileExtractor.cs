using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Index
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
    /// Command-line tool for extracting sub-files out of a compound file.
    /// <para />
    /// LUCENENET specific: In the Java implementation, this class' Main method
    /// was intended to be called from the command line. However, in .NET a
    /// method within a DLL can't be directly called from the command line so we
    /// provide a <see href="https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools">.NET tool</see>,
    /// <see href="https://www.nuget.org/packages/lucene-cli">lucene-cli</see>,
    /// with two commands that map to that method:
    /// index extract-cfs and index list-cfs
    /// </summary>
    public static class CompoundFileExtractor // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        /// <summary>
        /// Prints the filename and size of each file within a given compound file.
        /// Add the -extract flag to extract files to the current working directory.
        /// In order to make the extracted version of the index work, you have to copy
        /// the segments file from the compound index into the directory where the extracted files are stored.
        /// <para />
        /// LUCENENET specific: In the Java implementation, this Main method
        /// was intended to be called from the command line. However, in .NET a
        /// method within a DLL can't be directly called from the command line so we
        /// provide a <see href="https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools">.NET tool</see>,
        /// <see href="https://www.nuget.org/packages/lucene-cli">lucene-cli</see>,
        /// with two commands that map to this method:
        /// index extract-cfs and index list-cfs
        /// </summary>
        /// <param name="args">The command line arguments</param>
        public static void Main(string[] args)
        {
            string filename = null;
            bool extract = false;
            string dirImpl = null;

            int j = 0;
            while (j < args.Length)
            {
                string arg = args[j];
                if ("-extract".Equals(arg, StringComparison.Ordinal))
                {
                    extract = true;
                }
                else if ("-dir-impl".Equals(arg, StringComparison.Ordinal))
                {
                    if (j == args.Length - 1)
                    {
                        // LUCENENET specific - our wrapper console shows the correct usage
                        throw new ArgumentException("ERROR: missing value for --directory-type option");
                        //Console.WriteLine("ERROR: missing value for -dir-impl option");
                        //Environment.Exit(1);
                    }
                    j++;
                    dirImpl = args[j];
                }
                else if (filename is null)
                {
                    filename = arg;
                }
                j++;
            }

            if (filename is null)
            {
                // LUCENENET specific - our wrapper console shows the correct usage
                throw new ArgumentException("ERROR: CFS-FILE is required");
                //Console.WriteLine("Usage: org.apache.lucene.index.CompoundFileExtractor [-extract] [-dir-impl X] <cfsfile>");
                //return;
            }

            Store.Directory dir = null;
            CompoundFileDirectory cfr = null;
            IOContext context = IOContext.READ;

            try
            {
                // LUCENENET specific: changed to use string filename instead of allocating a FileInfo (#832)
                string dirname = Path.GetDirectoryName(filename)
                                 ?? throw new InvalidOperationException($"Could not determine directory name from filename: {filename}");

                if (dirImpl is null)
                {
                    dir = FSDirectory.Open(dirname);
                }
                else
                {
                    // LUCENENET NOTE: We need the DirectoryInfo instance here, as there's no benefit to using a string.
                    // See comments on CommandLineUtil.NewFSDirectory(string, DirectoryInfo) for more information. (#832)
                    dir = CommandLineUtil.NewFSDirectory(dirImpl, new DirectoryInfo(dirname));
                }

                cfr = new CompoundFileDirectory(dir, filename, IOContext.DEFAULT, false);

                string[] files = cfr.ListAll();
                ArrayUtil.TimSort(files); // sort the array of filename so that the output is more readable

                for (int i = 0; i < files.Length; ++i)
                {
                    long len = cfr.FileLength(files[i]);

                    if (extract)
                    {
                        Console.WriteLine("extract " + files[i] + " with " + len + " bytes to local directory...");
                        using IndexInput ii = cfr.OpenInput(files[i], context);
                        using FileStream f = new FileStream(files[i], FileMode.Open, FileAccess.ReadWrite);

                        // read and write with a small buffer, which is more effective than reading byte by byte
                        byte[] buffer = new byte[1024];
                        int chunk = buffer.Length;
                        while (len > 0)
                        {
                            int bufLen = (int)Math.Min(chunk, len);
                            ii.ReadBytes(buffer, 0, bufLen);
                            f.Write(buffer, 0, bufLen);
                            len -= bufLen;
                        }
                    }
                    else
                    {
                        Console.WriteLine(files[i] + ": " + len + " bytes");
                    }
                }
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                ioe.PrintStackTrace();
            }
            finally
            {
                try
                {
                    if (dir != null)
                    {
                        dir.Dispose();
                    }
                    if (cfr != null)
                    {
                        cfr.Dispose();
                    }
                }
                catch (Exception ioe) when (ioe.IsIOException())
                {
                    ioe.PrintStackTrace();
                }
            }
        }
    }
}
