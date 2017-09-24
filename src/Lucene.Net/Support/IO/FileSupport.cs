using Lucene.Net.Util;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support.IO
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
    /// Represents the methods to support some operations over files.
    /// </summary>
    public static class FileSupport
    {
        private static int ERROR_FILE_EXISTS = 0x0050;

        /// <summary>
        /// Creates a new empty file in a random subdirectory of <see cref="Path.GetTempPath()"/>, using the given prefix and 
        /// suffix strings to generate its name.
        /// </summary>
        /// <remarks>
        /// If this method returns successfully then it is guaranteed that:
        /// <list type="number">
        /// <item><description>The file denoted by the returned abstract pathname did not exist before this method was invoked, and</description></item>
        /// <item><description>Neither this method nor any of its variants will return the same abstract pathname again in the current invocation of the virtual machine.</description></item>
        /// </list>
        /// This method provides only part of a temporary-file facility. However, the file will not be deleted automatically, 
        /// it must be deleted by the caller.
        /// <para/>
        /// The prefix argument must be at least three characters long. It is recommended that the prefix be a short, meaningful 
        /// string such as "hjb" or "mail". 
        /// <para/>
        /// The suffix argument may be null, in which case a random suffix will be used.
        /// <para/>
        /// Both prefix and suffix must be provided with valid characters for the underlying system, as specified by 
        /// <see cref="Path.GetInvalidFileNameChars()"/>.
        /// <para/>
        /// If the directory argument is null then the system-dependent default temporary-file directory will be used, 
        /// with a random subdirectory name. The default temporary-file directory is specified by the 
        /// <see cref="Path.GetTempPath()"/> method. On UNIX systems the default value of this property is typically 
        /// "/tmp" or "/var/tmp"; on Microsoft Windows systems it is typically "C:\\Users\\[UserName]\\AppData\Local\Temp".
        /// </remarks>
        /// <param name="prefix">The prefix string to be used in generating the file's name; must be at least three characters long</param>
        /// <param name="suffix">The suffix string to be used in generating the file's name; may be null, in which case a random suffix will be generated</param>
        /// <returns>A <see cref="FileInfo"/> instance representing the temp file that was created.</returns>
        public static FileInfo CreateTempFile(string prefix, string suffix)
        {
            return CreateTempFile(prefix, suffix, null);
        }

        /// <summary>
        /// Creates a new empty file in the specified directory, using the given prefix and suffix strings to generate its name.
        /// </summary>
        /// <remarks>
        /// If this method returns successfully then it is guaranteed that:
        /// <list type="number">
        /// <item><description>The file denoted by the returned abstract pathname did not exist before this method was invoked, and</description></item>
        /// <item><description>Neither this method nor any of its variants will return the same abstract pathname again in the current invocation of the virtual machine.</description></item>
        /// </list>
        /// This method provides only part of a temporary-file facility. However, the file will not be deleted automatically, 
        /// it must be deleted by the caller.
        /// <para/>
        /// The prefix argument must be at least three characters long. It is recommended that the prefix be a short, meaningful 
        /// string such as "hjb" or "mail". 
        /// <para/>
        /// The suffix argument may be null, in which case a random suffix will be used.
        /// <para/>
        /// Both prefix and suffix must be provided with valid characters for the underlying system, as specified by 
        /// <see cref="Path.GetInvalidFileNameChars()"/>.
        /// <para/>
        /// If the directory argument is null then the system-dependent default temporary-file directory will be used, 
        /// with a random subdirectory name. The default temporary-file directory is specified by the 
        /// <see cref="Path.GetTempPath()"/> method. On UNIX systems the default value of this property is typically 
        /// "/tmp" or "/var/tmp"; on Microsoft Windows systems it is typically "C:\\Users\\[UserName]\\AppData\Local\Temp".
        /// </remarks>
        /// <param name="prefix">The prefix string to be used in generating the file's name; must be at least three characters long</param>
        /// <param name="suffix">The suffix string to be used in generating the file's name; may be null, in which case a random suffix will be generated</param>
        /// <param name="directory">The directory in which the file is to be created, or null if the default temporary-file directory is to be used</param>
        /// <returns>A <see cref="FileInfo"/> instance representing the temp file that was created.</returns>
        public static FileInfo CreateTempFile(string prefix, string suffix, DirectoryInfo directory)
        {
            if (string.IsNullOrEmpty(prefix))
                throw new ArgumentNullException("prefix");
            if (prefix.Length < 3)
                throw new ArgumentException("Prefix string too short");

            // Ensure the strings passed don't contain invalid characters
            char[] invalid = Path.GetInvalidFileNameChars();

            if (prefix.ToCharArray().Intersect(invalid).Any())
                throw new ArgumentException(string.Format("Prefix contains invalid characters. You may not use any of '{0}'", string.Join(", ", invalid)));
            if (suffix != null && suffix.ToCharArray().Intersect(invalid).Any())
                throw new ArgumentException(string.Format("Suffix contains invalid characters. You may not use any of '{0}'", string.Join(", ", invalid)));

            // If no directory supplied, create one.
            if (directory == null)
            {
                directory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName())));
            }
            // Ensure the directory exists (this does nothing if it already exists, although may throw exceptions in cases where permissions are changed)
            directory.Create();
            string fileName = string.Empty;

            while (true)
            {
                fileName = NewTempFileName(prefix, suffix, directory);

                if (File.Exists(fileName))
                {
                    continue;
                }

                try
                {
                    // Create the file, and close it immediately
                    using (var stream = new FileStream(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                    {
                        break;
                    }
                }
                catch (IOException e)
                {
                    // If the error was because the file exists, try again.
                    // On Windows, we can rely on the constant, but we need to fallback
                    // to doing a physical file check to be portable across platforms.
                    if (Constants.WINDOWS && (e.HResult & 0xFFFF) == ERROR_FILE_EXISTS)
                    {
                        continue;
                    }
                    else if (!Constants.WINDOWS && File.Exists(fileName))
                    {
                        continue;
                    }

                    // else rethrow it
                    throw;
                }
            }
            return new FileInfo(fileName);
        }

        /// <summary>
        /// Generates a new random file name with the provided <paramref name="directory"/>, 
        /// <paramref name="prefix"/> and optional <paramref name="suffix"/>.
        /// </summary>
        /// <param name="prefix">The prefix string to be used in generating the file's name</param>
        /// <param name="suffix">The suffix string to be used in generating the file's name; may be null, in which case a random suffix will be generated</param>
        /// <param name="directory">A <see cref="DirectoryInfo"/> object containing the temp directory path. Must not be null.</param>
        /// <returns>A random file name</returns>
        internal static string NewTempFileName(string prefix, string suffix, DirectoryInfo directory)
        {
            string randomFileName = Path.GetRandomFileName();

            if (suffix != null)
            {
                randomFileName = string.Concat(
                    Path.GetFileNameWithoutExtension(randomFileName),
                    suffix.StartsWith(".", StringComparison.Ordinal) ? suffix : '.' + suffix
                );
            }

            return Path.Combine(directory.FullName, string.Concat(prefix, randomFileName));
        }
    }
}