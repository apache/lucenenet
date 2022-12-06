using Lucene.Net.Support.Text;
using Lucene.Net.Util;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
#nullable enable

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
    internal static class FileSupport
    {
        private static readonly FileStreamOptions DefaultFileStreamOptionsCreateOnly = new FileStreamOptions { Access = FileAccess.Write, Share = FileShare.ReadWrite, BufferSize = 1 };
        public static readonly FileStreamOptions DefaultFileStreamOptions = new FileStreamOptions { Access = FileAccess.ReadWrite, BufferSize = 8192, Options = FileOptions.DeleteOnClose | FileOptions.RandomAccess };

        private static readonly char[] INVALID_FILENAME_CHARS = Path.GetInvalidFileNameChars();

        // LUCNENENET NOTE: Lookup the HResult value we are interested in for the current OS
        // by provoking the exception during initialization and caching its HResult value for later.
        // We optimize for Windows because those HResult values are known and documented, but for
        // other platforms, this is the only way we can reliably determine the HResult values
        // we are interested in.
        //
        // Reference: https://stackoverflow.com/q/46380483
        private const int WIN_HRESULT_FILE_ALREADY_EXISTS = unchecked((int)0x80070050);
        private static readonly int? HRESULT_FILE_ALREADY_EXISTS = LoadFileAlreadyExistsHResult();

        private static int? LoadFileAlreadyExistsHResult()
        {
            if (Constants.WINDOWS)
                return WIN_HRESULT_FILE_ALREADY_EXISTS;

            return GetFileIOExceptionHResult(provokeException: (fileName) =>
            {
                //Try to create the file again -this should throw an IOException with the correct HResult for the current platform
                using var stream = new FileStream(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            });
        }

        internal static int? GetFileIOExceptionHResult(Action<string> provokeException)
        {
            string fileName;
            try
            {
                // This could throw, but we don't care about this HResult value.
                fileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()); // LUCENENET NOTE: Path.GetTempFileName() is considered insecure because the filename can be guessed https://rules.sonarsource.com/csharp/RSPEC-5445
            }
            catch
            {
                return null; // We couldn't create a temp file
            }
            try
            {
                provokeException(fileName);
            }
            catch (IOException ex) when (ex.HResult != 0) // Assume 0 means the platform is not completely implemented, thus unknown
            {
                return ex.HResult;
            }
            catch
            {
                return null; // Unknown exception
            }
            finally
            {
                try
                {
                    File.Delete(fileName);
                }
                catch { /* ignored */ }
            }
            return null; // Should never get here
        }

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
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="prefix"/> length is less than 3 characters.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="prefix"/> or <paramref name="suffix"/> contains invalid characters according to <see cref="Path.GetInvalidFileNameChars()"/>.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FileInfo CreateTempFile(string prefix, string? suffix)
        {
            return CreateTempFile(prefix, suffix, (string)null!);
        }

        /// <summary>
        /// Creates a new empty file in the specified directory, using the given prefix and suffix strings to generate its name.
        /// </summary>
        /// <remarks>
        /// If this method returns successfully then it is guaranteed that:
        /// <list type="number">
        /// <item><description>The file denoted by the returned abstract pathname did not exist before this method was invoked, and</description></item>
        /// <item><description>Neither this method nor any of its variants will return the same abstract pathname again in the current invocation of the application.</description></item>
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
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="prefix"/> length is less than 3 characters.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="prefix"/> or <paramref name="suffix"/> contains invalid characters according to <see cref="Path.GetInvalidFileNameChars()"/>.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FileInfo CreateTempFile(string prefix, string suffix, DirectoryInfo? directory)
        {
            using var stream = CreateTempFileAsStream(prefix, suffix, directory?.FullName, DefaultFileStreamOptionsCreateOnly);
            return new FileInfo(stream.Name);
        }

        /// <summary>
        /// Creates a new empty file in the specified directory, using the given prefix and suffix strings to generate its name.
        /// </summary>
        /// <remarks>
        /// If this method returns successfully then it is guaranteed that:
        /// <list type="number">
        /// <item><description>The file denoted by the returned abstract pathname did not exist before this method was invoked, and</description></item>
        /// <item><description>Neither this method nor any of its variants will return the same abstract pathname again in the current invocation of the application.</description></item>
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
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="prefix"/> length is less than 3 characters.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="prefix"/> or <paramref name="suffix"/> contains invalid characters according to <see cref="Path.GetInvalidFileNameChars()"/>.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FileInfo CreateTempFile(string prefix, string? suffix, string? directory)
        {
            using var stream = CreateTempFileAsStream(prefix, suffix, directory, DefaultFileStreamOptionsCreateOnly);
            return new FileInfo(stream.Name);
        }

        /// <summary>
        /// Creates a new empty file in the specified directory, using the given prefix and suffix strings to generate its name and returns an open stream to it.
        /// </summary>
        /// <remarks>
        /// If this method returns successfully then it is guaranteed that:
        /// <list type="number">
        /// <item><description>The file denoted by the returned abstract pathname did not exist before this method was invoked, and</description></item>
        /// <item><description>Neither this method nor any of its variants will return the same abstract pathname again in the current invocation of the application.</description></item>
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
        /// <returns>A <see cref="FileStream"/> instance representing the temp file that was created.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="prefix"/> length is less than 3 characters.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="prefix"/> or <paramref name="suffix"/> contains invalid characters according to <see cref="Path.GetInvalidFileNameChars()"/>.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FileStream CreateTempFileAsStream(string prefix, string? suffix, DirectoryInfo? directory)
        {
            return CreateTempFileAsStream(prefix, suffix, directory?.FullName, DefaultFileStreamOptions);
        }

        /// <summary>
        /// Creates a new empty file in the specified directory, using the given prefix and suffix strings to generate its name and returns an open stream to it.
        /// </summary>
        /// <remarks>
        /// If this method returns successfully then it is guaranteed that:
        /// <list type="number">
        /// <item><description>The file denoted by the returned abstract pathname did not exist before this method was invoked, and</description></item>
        /// <item><description>Neither this method nor any of its variants will return the same abstract pathname again in the current invocation of the application.</description></item>
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
        /// <returns>A <see cref="FileStream"/> instance representing the temp file that was created.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="prefix"/> length is less than 3 characters.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="prefix"/> or <paramref name="suffix"/> contains invalid characters according to <see cref="Path.GetInvalidFileNameChars()"/>.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FileStream CreateTempFileAsStream(string prefix, string? suffix, string? directory)
        {
            return CreateTempFileAsStream(prefix, suffix, directory, DefaultFileStreamOptions);
        }

        /// <summary>
        /// Creates a new empty file in the specified directory, using the given prefix and suffix strings to generate its name and returns an open stream to it.
        /// </summary>
        /// <remarks>
        /// If this method returns successfully then it is guaranteed that:
        /// <list type="number">
        /// <item><description>The file denoted by the returned abstract pathname did not exist before this method was invoked, and</description></item>
        /// <item><description>Neither this method nor any of its variants will return the same abstract pathname again in the current invocation of the application.</description></item>
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
        /// <param name="options">The options to pass to the <see cref="FileStream"/>.</param>
        /// <returns>A <see cref="FileStream"/> instance representing the temp file that was created.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="prefix"/> length is less than 3 characters.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="prefix"/> or <paramref name="suffix"/> contains invalid characters according to <see cref="Path.GetInvalidFileNameChars()"/>.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="options"/>.<see cref="FileStreamOptions.Access"/> is set to <see cref="FileAccess.Read"/>.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FileStream CreateTempFileAsStream(string prefix, string? suffix, DirectoryInfo? directory, FileStreamOptions options)
        {
            return CreateTempFileAsStream(prefix, suffix, directory?.FullName, options);
        }

        /// <summary>
        /// Creates a new empty file in the specified directory, using the given prefix and suffix strings to generate its name and returns an open stream to it.
        /// </summary>
        /// <remarks>
        /// If this method returns successfully then it is guaranteed that:
        /// <list type="number">
        /// <item><description>The file denoted by the returned abstract pathname did not exist before this method was invoked, and</description></item>
        /// <item><description>Neither this method nor any of its variants will return the same abstract pathname again in the current invocation of the application.</description></item>
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
        /// <param name="options">The options to pass to the <see cref="FileStream"/>.</param>
        /// <returns>A <see cref="FileStream"/> instance representing the temp file that was created.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="prefix"/> length is less than 3 characters.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="prefix"/> or <paramref name="suffix"/> contains invalid characters according to <see cref="Path.GetInvalidFileNameChars()"/>.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="options"/>.<see cref="FileStreamOptions.Access"/> is set to <see cref="FileAccess.Read"/>.
        /// </exception>
        public static FileStream CreateTempFileAsStream(string prefix, string? suffix, string? directory, FileStreamOptions options)
        {
            if (string.IsNullOrEmpty(prefix))
                throw new ArgumentNullException(nameof(prefix));
            if (prefix.Length < 3)
                throw new ArgumentException("Prefix string too short");
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            // Ensure the strings passed don't contain invalid characters
            if (prefix.ContainsAny(INVALID_FILENAME_CHARS))
                throw new ArgumentException(string.Format("Prefix contains invalid characters. You may not use any of '{0}'", string.Join(", ", INVALID_FILENAME_CHARS)));
            if (suffix != null && suffix.ContainsAny(INVALID_FILENAME_CHARS))
                throw new ArgumentException(string.Format("Suffix contains invalid characters. You may not use any of '{0}'", string.Join(", ", INVALID_FILENAME_CHARS)));
            if (options.Access == FileAccess.Read)
                throw new ArgumentException("Read-only for options.FileAccess is not supported.");


            // If no directory supplied, create one.
            if (directory is null)
            {
                directory = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            }
            // Ensure the directory exists (this does nothing if it already exists, although may throw exceptions in cases where permissions are changed)
            Directory.CreateDirectory(directory);
            string fileName;

            while (true)
            {
                fileName = NewTempFileName(prefix, suffix, directory);

                if (File.Exists(fileName))
                {
                    continue;
                }

                try
                {
                    // Create the file, and return it only if successful
                    return new FileStream(fileName, FileMode.CreateNew, options.Access, options.Share, options.BufferSize, options.Options);
                }
                catch (IOException e) when (IsFileAlreadyExistsException(e, fileName))
                {
                    // If the error was because the file exists, try again.
                    // We might get here if another process or thread created the file since we checked above.
                }
            }
        }

        /// <summary>
        /// Tests whether the passed in <see cref="Exception"/> is an <see cref="IOException"/>
        /// corresponding to the underlying operating system's "File Already Exists" violation.
        /// This works by forcing the exception to occur during initialization and caching the
        /// <see cref="Exception.HResult"/> value for the current OS.
        /// </summary>
        /// <param name="ex">An exception, for comparison.</param>
        /// <param name="filePath">The path of the file to check. This is used as a fallback in case the
        /// current OS doesn't have an HResult (an edge case).</param>
        /// <returns><c>true</c> if the exception passed is an <see cref="IOException"/> with an 
        /// <see cref="Exception.HResult"/> corresponding to the operating system's "File Already Exists" violation, which
        /// occurs when an attempt is made to create a file that already exists.</returns>
        public static bool IsFileAlreadyExistsException(Exception ex, string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (!typeof(IOException).Equals(ex))
                return false;
            else if (HRESULT_FILE_ALREADY_EXISTS.HasValue)
                return ex.HResult == HRESULT_FILE_ALREADY_EXISTS;
            else
                return File.Exists(filePath);
        }

        /// <summary>
        /// Generates a new random file name with the provided <paramref name="directory"/>, 
        /// <paramref name="prefix"/> and optional <paramref name="suffix"/>.
        /// </summary>
        /// <param name="prefix">The prefix string to be used in generating the file's name</param>
        /// <param name="suffix">The suffix string to be used in generating the file's name; may be null, in which case a random suffix will be generated</param>
        /// <param name="directory">A <see cref="DirectoryInfo"/> object containing the temp directory path. Must not be null.</param>
        /// <returns>A random file name</returns>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c> or whitespace or <paramref name="directory"/> is <c>null</c>.</exception>
        internal static string NewTempFileName(string prefix, string? suffix, DirectoryInfo directory)
        {
            if (directory is null)
                throw new ArgumentNullException(nameof(directory));

            return NewTempFileName(prefix, suffix, directory.FullName);
        }

        /// <summary>
        /// Generates a new random file name with the provided <paramref name="directory"/>, 
        /// <paramref name="prefix"/> and optional <paramref name="suffix"/>.
        /// </summary>
        /// <param name="prefix">The prefix string to be used in generating the file's name</param>
        /// <param name="suffix">The suffix string to be used in generating the file's name; may be null, in which case a random suffix will be generated</param>
        /// <param name="directory">A <see cref="string"/> containing the temp directory path. Must not be null.</param>
        /// <returns>A random file name</returns>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> or <paramref name="directory"/> is <c>null</c> or whitespace.</exception>
        internal static string NewTempFileName(string prefix, string? suffix, string directory)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentNullException(nameof(prefix));
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentNullException(nameof(directory));

            string randomFileName = Path.GetRandomFileName();

            if (suffix != null)
            {
                randomFileName = string.Concat(
                    Path.GetFileNameWithoutExtension(randomFileName),
                    suffix.StartsWith(".", StringComparison.Ordinal) ? suffix : '.' + suffix
                );
            }

            return Path.Combine(directory, string.Concat(prefix, randomFileName));
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

            if (fileCanonPathCache.TryGetValue(absPath, out string? canonPath) && canonPath != null)
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
            // LUCENENET: There is a small chance that two threads could load the same string
            // simultaneously, but it shouldn't be too expensive.
            canonPath = fileCanonPathCache.GetOrAdd(
                absPath,
                k => Encoding.UTF8.GetString(newResult, 0, newLength).TrimEnd('\0')); // LUCENENET: Eliminate null terminator char
            return canonPath;
        }
    }
}