using Lucene.Net.Util;
using System;
using System.Collections.Concurrent;
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

        private static readonly ConcurrentDictionary<string, string> fileCanonPathCache = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Returns the absolute path of this <see cref="FileSystemInfo"/> with all references resolved and
        /// any drive letters normalized to upper case on Windows. An
        /// <em>absolute</em> path is one that begins at the root of the file
        /// system. The canonical path is one in which all references have been
        /// resolved. For the cases of '..' and '.', where the file system supports
        /// parent and working directory respectively, these are removed and replaced
        /// with a direct directory reference. 
        /// </summary>
        /// <param name="path">This <see cref="FileSystemInfo"/> instance.</param>
        /// <returns>The canonical path of this file.</returns>
        // LUCENENET NOTE: Implementation ported mostly from Apache Harmony
        public static string GetCanonicalPath(this FileSystemInfo path)
        {
            string absPath = path.FullName; // LUCENENET NOTE: This internally calls GetFullPath(), which resolves relative paths
            byte[] result = Encoding.UTF8.GetBytes(absPath);

            string canonPath;
            if (fileCanonPathCache.TryGetValue(absPath, out canonPath) && canonPath != null)
            {
                return canonPath;
            }

            // LUCENENET TODO: On Unix, this resolves symbolic links. Not sure
            // if it is safe to assume Path.GetFullPath() does that for us.
            //if (Path.DirectorySeparatorChar == '/')
            //{
            //    //// resolve the full path first
            //    //result = resolveLink(result, result.Length, false);
            //    //// resolve the parent directories
            //    //result = resolve(result);
            //}
            int numSeparators = 1;
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] == Path.DirectorySeparatorChar)
                {
                    numSeparators++;
                }
            }
            int[] sepLocations = new int[numSeparators];
            int rootLoc = 0;
            if (Path.DirectorySeparatorChar == '\\')
            {
                if (result[0] == '\\')
                {
                    rootLoc = (result.Length > 1 && result[1] == '\\') ? 1 : 0;
                }
                else
                {
                    rootLoc = 2; // skip drive i.e. c:
                }
            }
            byte[] newResult = new byte[result.Length + 1];
            int newLength = 0, lastSlash = 0, foundDots = 0;
            sepLocations[lastSlash] = rootLoc;
            for (int i = 0; i <= result.Length; i++)
            {
                if (i < rootLoc)
                {
                    // Normalize case of Windows drive letter to upper
                    newResult[newLength++] = (byte)char.ToUpperInvariant((char)result[i]);
                }
                else
                {
                    if (i == result.Length || result[i] == Path.DirectorySeparatorChar)
                    {
                        if (i == result.Length && foundDots == 0)
                        {
                            break;
                        }
                        if (foundDots == 1)
                        {
                            /* Don't write anything, just reset and continue */
                            foundDots = 0;
                            continue;
                        }
                        if (foundDots > 1)
                        {
                            /* Go back N levels */
                            lastSlash = lastSlash > (foundDots - 1) ? lastSlash
                                    - (foundDots - 1) : 0;
                            newLength = sepLocations[lastSlash] + 1;
                            foundDots = 0;
                            continue;
                        }
                        sepLocations[++lastSlash] = newLength;
                        newResult[newLength++] = (byte)Path.DirectorySeparatorChar;
                        continue;
                    }
                    if (result[i] == '.')
                    {
                        foundDots++;
                        continue;
                    }
                    /* Found some dots within text, write them out */
                    if (foundDots > 0)
                    {
                        for (int j = 0; j < foundDots; j++)
                        {
                            newResult[newLength++] = (byte)'.';
                        }
                    }
                    newResult[newLength++] = result[i];
                    foundDots = 0;
                }
            }
            // remove trailing slash
            if (newLength > (rootLoc + 1)
                    && newResult[newLength - 1] == Path.DirectorySeparatorChar)
            {
                newLength--;
            }
            newResult[newLength] = 0;
            //newResult = getCanonImpl(newResult);
            newLength = newResult.Length;
            canonPath = fileCanonPathCache.GetOrAdd(
                absPath,
                k => Encoding.UTF8.GetString(newResult, 0, newLength).TrimEnd('\0')); // LUCENENET: Eliminate null terminator char
            return canonPath;
        }
    }
}