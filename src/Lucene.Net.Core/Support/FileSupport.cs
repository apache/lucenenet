/*
 *
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
 *
*/

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Represents the methods to support some operations over files.
    /// </summary>
    public class FileSupport
    {
        private static readonly object _lock = new object();

        /// <summary>
        /// Returns an array of abstract pathnames representing the files and directories of the specified path.
        /// </summary>
        /// <param name="path">The abstract pathname to list it childs.</param>
        /// <returns>An array of abstract pathnames childs of the path specified or null if the path is not a directory</returns>
        public static System.IO.FileInfo[] GetFiles(System.IO.FileInfo path)
        {
            if ((path.Attributes & FileAttributes.Directory) > 0)
            {
                String[] fullpathnames = Directory.GetFileSystemEntries(path.FullName);
                System.IO.FileInfo[] result = new System.IO.FileInfo[fullpathnames.Length];
                for (int i = 0; i < result.Length; i++)
                    result[i] = new System.IO.FileInfo(fullpathnames[i]);
                return result;
            }
            else
                return null;
        }

        // TODO: This filesupport thing is silly.  Same goes with _TestUtil's RMDir.
        //       If we're removing a directory
        public static System.IO.FileInfo[] GetFiles(System.IO.DirectoryInfo path)
        {
            return GetFiles(new FileInfo(path.FullName));
        }

        ///// <summary>
        ///// Returns a list of files in a give directory.
        ///// </summary>
        ///// <param name="fullName">The full path name to the directory.</param>
        ///// <param name="indexFileNameFilter"></param>
        ///// <returns>An array containing the files.</returns>
        //public static System.String[] GetLuceneIndexFiles(System.String fullName,
        //                                                  Index.IndexFileNameFilter indexFileNameFilter)
        //{
        //    System.IO.DirectoryInfo dInfo = new System.IO.DirectoryInfo(fullName);
        //    System.Collections.ArrayList list = new System.Collections.ArrayList();
        //    foreach (System.IO.FileInfo fInfo in dInfo.GetFiles())
        //    {
        //        if (indexFileNameFilter.Accept(fInfo, fInfo.Name) == true)
        //        {
        //            list.Add(fInfo.Name);
        //        }
        //    }
        //    System.String[] retFiles = new System.String[list.Count];
        //    list.CopyTo(retFiles);
        //    return retFiles;
        //}

        // Disable the obsolete warning since we must use FileStream.Handle
        // because Mono does not support FileSystem.SafeFileHandle at present.
#pragma warning disable 618

        /// <summary>
        /// Flushes the specified file stream. Ensures that all buffered
        /// data is actually written to the file system.
        /// </summary>
        /// <param name="fileStream">The file stream.</param>
        public static void Sync(System.IO.FileStream fileStream)
        {
            if (fileStream == null)
                throw new ArgumentNullException("fileStream");

            fileStream.Flush(true);

            if (OS.IsWindows)
            {
#if NETSTANDARD
                // Getting the SafeFileHandle property automatically flushes the
                // stream: https://msdn.microsoft.com/en-us/library/system.io.filestream.safefilehandle(v=vs.110).aspx
                var handle = fileStream.SafeFileHandle;
#else
                if (!FlushFileBuffers(fileStream.Handle))
                    throw new IOException();
#endif
            }
            //else if (OS.IsUnix)
            //{
            //    if (fsync(fileStream.Handle) != IntPtr.Zero)
            //    throw new System.IO.IOException();
            //}
            //else
            //{
            //    throw new NotImplementedException();
            //}
        }

#pragma warning restore 618

        //[System.Runtime.InteropServices.DllImport("libc")]
        //extern static IntPtr fsync(IntPtr fd);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        extern static bool FlushFileBuffers(IntPtr hFile);


        /// <summary>
        /// Creates a new empty file in the specified directory, using the given prefix and suffix strings to generate its name. 
        /// If this method returns successfully then it is guaranteed that:
        /// <list type="number">
        /// <item>The file denoted by the returned abstract pathname did not exist before this method was invoked, and</item>
        /// <item>Neither this method nor any of its variants will return the same abstract pathname again in the current invocation of the virtual machine.</item>
        /// </list>
        /// This method provides only part of a temporary-file facility.To arrange for a file created by this method to be deleted automatically, use the deleteOnExit() method.
        /// The prefix argument must be at least three characters long. It is recommended that the prefix be a short, meaningful string such as "hjb" or "mail". The suffix argument may be null, in which case the suffix ".tmp" will be used.
        /// To create the new file, the prefix and the suffix may first be adjusted to fit the limitations of the underlying platform.If the prefix is too long then it will be truncated, but its first three characters will always be preserved.If the suffix is too long then it too will be truncated, but if it begins with a period character ('.') then the period and the first three characters following it will always be preserved.Once these adjustments have been made the name of the new file will be generated by concatenating the prefix, five or more internally-generated characters, and the suffix.
        /// If the directory argument is null then the system-dependent default temporary-file directory will be used.The default temporary-file directory is specified by the system property java.io.tmpdir.On UNIX systems the default value of this property is typically "/tmp" or "/var/tmp"; on Microsoft Windows systems it is typically "C:\\WINNT\\TEMP". A different value may be given to this system property when the Java virtual machine is invoked, but programmatic changes to this property are not guaranteed to have any effect upon the temporary directory used by this method.
        /// 
        /// Ported over from the java.io.File class. Used by the Analysis.Hunspell.Directory
        /// class, but this can probably be removed when that class is upgraded to a more recent
        /// version of lucene, where it uses the lucene Store.Directory class to create a temporary
        /// file.
        /// </summary>
        /// <param name="prefix">The prefix string to be used in generating the file's name; must be at least three characters long</param>
        /// <param name="suffix">The suffix string to be used in generating the file's name; may be null, in which case the suffix ".tmp" will be used</param>
        /// <param name="directory">The directory in which the file is to be created, or null if the default temporary-file directory is to be used</param>
        /// <returns></returns>
        public static FileInfo CreateTempFile(string prefix, string suffix, DirectoryInfo directory)
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(prefix))
                    throw new ArgumentNullException("prefix");
                if (prefix.Length < 3)
                    throw new ArgumentException("Prefix string too short");
                string s = (suffix == null) ? ".tmp" : suffix;
                if (directory == null)
                {
                    string tmpDir = Path.GetTempPath();
                    directory = new DirectoryInfo(tmpDir);
                }
                int attempt = 0;
                string extension = suffix.StartsWith(".") ? suffix : '.' + suffix;
                string fileName = Path.Combine(directory.FullName, string.Concat(prefix, extension));
                while (true)
                {
                    try
                    {
                        if (attempt > 0)
                        {
                            fileName = Path.Combine(directory.FullName, string.Concat(prefix, attempt.ToString(), extension));
                        }
                        if (File.Exists(fileName))
                        {
                            attempt++;
                            continue;
                        }
                        // Create the file
                        File.WriteAllText(fileName, string.Empty, new UTF8Encoding(false) /* No BOM */);
                        break;
                    }
                    catch (IOException e)
                    {
                        if (!e.Message.Contains("already exists"))
                        {
                            throw e;
                        }

                        attempt++;
                    }
                }
                return new FileInfo(fileName);
            }
        }
    }
}