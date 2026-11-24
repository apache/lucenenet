using J2N.Text;
using Lucene.Net.Native;
using Lucene.Net.Support.Buffers;
using Lucene.Net.Support.Text;
using Lucene.Net.Util;
using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        // LUCENENET specific: PATH_MAX on Linux (MAXPATHLEN is 1024 on macOS). readlink() cannot return a longer
        // target, and fails with ENAMETOOLONG when asked about a longer path.
        private const int MaxSymbolicLinkTargetByteCount = 4096;

        // LUCENENET specific: Same as MAXSYMLINKS on Linux (macOS allows 32). Guards against cycles such as
        // a -> a/b, which never terminate because every hop produces a new path.
        private const int MaxSymbolicLinkHops = 40;

        // LUCENENET specific: Upper bound for a canonical path that symbolic link targets have made longer than
        // GetMaxCanonicalPathByteCount() allowed for. Beyond this we give up rather than keep renting bigger buffers.
        private const int MaxCanonicalPathByteCount = 32 * 1024;

        /// <summary>
        /// Returns the absolute path of this <see cref="FileSystemInfo"/> with all references resolved and
        /// any drive letters normalized to upper case on Windows. An
        /// <em>absolute</em> path is one that begins at the root of the file
        /// system. The canonical path is one in which all references have been
        /// resolved. For the cases of '..' and '.', where the file system supports
        /// parent and working directory respectively, these are removed and replaced
        /// with a direct directory reference. On Unix, symbolic links are resolved.
        /// <para/>
        /// LUCENENET NOTE: <see cref="FileSystemInfo.FullName"/> removes '..' segments lexically before this
        /// method sees the path, so <c>/a/link/../x</c> resolves to <c>/a/x</c> even when <c>link</c> is a
        /// symbolic link to another directory, where Java (and <c>realpath(3)</c>) would give the sibling of the
        /// link's target. <see cref="TryGetCanonicalPathBytes(ReadOnlySpan{char}, Span{byte}, out int)"/> and
        /// <see cref="EqualsCanonicalPath(ReadOnlySpan{char}, ReadOnlySpan{char})"/> resolve such a path like Java
        /// does when they are given it unnormalized.
        /// </summary>
        /// <param name="path">This <see cref="FileSystemInfo"/> instance.</param>
        /// <returns>The canonical path of this file.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c>.</exception>
        /// <exception cref="IOException">On Unix, a symbolic link in the path could not be resolved, for
        /// example because there are too many levels of symbolic links, or the resolved path is longer than
        /// 32 KiB (<see cref="PathTooLongException"/>).</exception>
        // LUCENENET NOTE: Implementation ported mostly from Apache Harmony
        public static string GetCanonicalPath(this FileSystemInfo path)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            string absPath = path.FullName; // LUCENENET NOTE: This internally calls GetFullPath(), which resolves relative paths

            // LUCENENET: Harmony cached the result to make up for the arrays it allocated. We use stack or pooled
            // memory instead, so there is nothing worth caching, and a symbolic link that is retargeted while the
            // process is running is seen the next time it is used (see #1409).
            int maxByteCount = GetMaxCanonicalPathByteCount(absPath.Length);
            byte[]? arrayToReturnToPool = null;
            Span<byte> buffer = maxByteCount <= Constants.MaxStackByteLimit
                ? stackalloc byte[maxByteCount]
                : (arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(maxByteCount));
            try
            {
                ReadOnlySpan<byte> canonicalPath = GetCanonicalPathBytes(absPath.AsSpan(), buffer, ref arrayToReturnToPool);
                return Encoding.UTF8.GetString(canonicalPath);
            }
            finally
            {
                ArrayPool<byte>.Shared.ReturnIfNotNull(arrayToReturnToPool);
            }
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="path"/> and <paramref name="other"/> have the same canonical path
        /// (see <see cref="GetCanonicalPath(FileSystemInfo)"/>), without allocating either path as a string.
        /// <para/>
        /// LUCENENET NOTE: Like <see cref="GetCanonicalPath(FileSystemInfo)"/>, this sees the paths after
        /// <see cref="FileSystemInfo.FullName"/> has removed '..' segments lexically.
        /// </summary>
        /// <param name="path">This <see cref="FileSystemInfo"/> instance.</param>
        /// <param name="other">The <see cref="FileSystemInfo"/> to compare with.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="other"/> is <c>null</c>.</exception>
        /// <exception cref="IOException">On Unix, a symbolic link in either path could not be resolved, or a
        /// resolved path is longer than 32 KiB (<see cref="PathTooLongException"/>).</exception>
        public static bool EqualsCanonicalPath(this FileSystemInfo path, FileSystemInfo other)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return EqualsCanonicalPath(path.FullName.AsSpan(), other.FullName.AsSpan());
        }

        /// <summary>
        /// Returns <c>true</c> if two absolute paths have the same canonical path (see
        /// <see cref="GetCanonicalPath(FileSystemInfo)"/>), without allocating either path as a string.
        /// </summary>
        /// <param name="absPath">An absolute path, such as <see cref="FileSystemInfo.FullName"/> or the result of
        /// <see cref="Path.GetFullPath(string)"/>.</param>
        /// <param name="otherAbsPath">The absolute path to compare with.</param>
        /// <exception cref="ArgumentException"><paramref name="absPath"/> or <paramref name="otherAbsPath"/> is not an absolute path.</exception>
        /// <exception cref="IOException">On Unix, a symbolic link in either path could not be resolved, or a
        /// resolved path is longer than 32 KiB (<see cref="PathTooLongException"/>).</exception>
        public static bool EqualsCanonicalPath(ReadOnlySpan<char> absPath, ReadOnlySpan<char> otherAbsPath)
        {
            if (!IsAbsolutePath(absPath))
            {
                throw new ArgumentException("An absolute path is required.", nameof(absPath));
            }
            if (!IsAbsolutePath(otherAbsPath))
            {
                throw new ArgumentException("An absolute path is required.", nameof(otherAbsPath));
            }
            if (absPath.SequenceEqual(otherAbsPath))
            {
                return true; // The same path canonicalizes the same way
            }

            int maxByteCount = GetMaxCanonicalPathByteCount(absPath.Length);
            int otherMaxByteCount = GetMaxCanonicalPathByteCount(otherAbsPath.Length);
            byte[]? arrayToReturnToPool = null, otherArrayToReturnToPool = null;
            Span<byte> buffer = maxByteCount <= Constants.MaxStackByteLimit
                ? stackalloc byte[maxByteCount]
                : (arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(maxByteCount));
            Span<byte> otherBuffer = otherMaxByteCount <= Constants.MaxStackByteLimit
                ? stackalloc byte[otherMaxByteCount]
                : (otherArrayToReturnToPool = ArrayPool<byte>.Shared.Rent(otherMaxByteCount));
            try
            {
                ReadOnlySpan<byte> canonicalPath = GetCanonicalPathBytes(absPath, buffer, ref arrayToReturnToPool);
                ReadOnlySpan<byte> otherCanonicalPath = GetCanonicalPathBytes(otherAbsPath, otherBuffer, ref otherArrayToReturnToPool);
                return canonicalPath.SequenceEqual(otherCanonicalPath);
            }
            finally
            {
                ArrayPool<byte>.Shared.ReturnIfNotNull(arrayToReturnToPool);
                ArrayPool<byte>.Shared.ReturnIfNotNull(otherArrayToReturnToPool);
            }
        }

        /// <summary>
        /// Returns a buffer size that is sufficient for
        /// <see cref="TryGetCanonicalPathBytes(ReadOnlySpan{char}, Span{byte}, out int)"/> to write the canonical form
        /// of any absolute path of <paramref name="charCount"/> characters, unless symbolic links on Unix resolve to a
        /// longer path. This is more than the canonical path itself needs: the buffer also holds the path before
        /// '.' and '..' segments are removed and, on Unix, a terminating null for <c>readlink()</c>.
        /// </summary>
        /// <param name="charCount">The number of characters in the path.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="charCount"/> is negative.</exception>
        public static int GetMaxCanonicalPathByteCount(int charCount)
        {
            if (charCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(charCount), charCount, "Non-negative number required.");
            }

            int maxByteCount = Encoding.UTF8.GetMaxByteCount(charCount);
            // On Unix, every prefix of the path is passed to readlink() as a null-terminated string
            return Path.DirectorySeparatorChar == '/' ? maxByteCount + 1 : maxByteCount;
        }

        /// <summary>
        /// Writes the canonical form of <paramref name="absPath"/> to <paramref name="destination"/> as UTF-8:
        /// '.' and '..' segments and any trailing separator are removed, the drive letter is normalized to upper
        /// case on Windows, and symbolic links are resolved on Unix.
        /// </summary>
        /// <param name="absPath">An absolute path, such as <see cref="FileSystemInfo.FullName"/> or the result of
        /// <see cref="Path.GetFullPath(string)"/>.</param>
        /// <param name="destination">The buffer to write to. <see cref="GetMaxCanonicalPathByteCount(int)"/> returns
        /// a size that is sufficient unless symbolic links on Unix resolve to a longer path.</param>
        /// <param name="bytesWritten">The number of bytes written to <paramref name="destination"/>, or 0 if it was too small.</param>
        /// <returns><c>true</c> if the canonical path was written; <c>false</c> if <paramref name="destination"/> is too small.</returns>
        /// <exception cref="ArgumentException"><paramref name="absPath"/> is not an absolute path.</exception>
        /// <exception cref="IOException">On Unix, a symbolic link in the path could not be resolved, for
        /// example because there are too many levels of symbolic links.</exception>
        public static bool TryGetCanonicalPathBytes(ReadOnlySpan<char> absPath, Span<byte> destination, out int bytesWritten)
        {
            if (!IsAbsolutePath(absPath))
            {
                throw new ArgumentException("An absolute path is required.", nameof(absPath));
            }

            int length;
            if (Path.DirectorySeparatorChar == '/')
            {
                if (!TryResolveSymbolicLinks(absPath, destination, out length))
                {
                    bytesWritten = 0;
                    return false;
                }
            }
            else if (!Encoding.UTF8.TryGetBytes(absPath, destination, out length))
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = RemoveDotSegments(destination, length);
            return true;
        }

        // Accepts what RemoveDotSegments() knows how to handle: a Unix root, or a Windows drive (c:...) or a path
        // starting with a separator (UNC or drive-relative). Cheaper than Path.IsPathRooted() and works with a span
        // on every target framework.
        private static bool IsAbsolutePath(ReadOnlySpan<char> path)
        {
            if (path.IsEmpty)
            {
                return false;
            }
            if (Path.DirectorySeparatorChar == '/')
            {
                return path[0] == '/';
            }
            return path[0] == '\\' || (path.Length > 2 && path[1] == ':' && path[2] == '\\'); // not drive-relative (c:foo)
        }

        // Retries TryGetCanonicalPathBytes() with a larger pooled buffer when symbolic links make the canonical path
        // longer than the buffer the caller sized from the input. The caller returns arrayToReturnToPool to the pool.
        private static ReadOnlySpan<byte> GetCanonicalPathBytes(ReadOnlySpan<char> absPath, Span<byte> buffer, ref byte[]? arrayToReturnToPool)
        {
            int bytesWritten;
            while (!TryGetCanonicalPathBytes(absPath, buffer, out bytesWritten))
            {
                int newSize = Math.Max(buffer.Length * 2, MaxSymbolicLinkTargetByteCount);
                if (newSize > MaxCanonicalPathByteCount)
                {
                    throw new PathTooLongException($"The canonical path of '{absPath.ToString()}' is longer than {MaxCanonicalPathByteCount} bytes.");
                }
                ArrayPool<byte>.Shared.ReturnIfNotNull(arrayToReturnToPool);
                arrayToReturnToPool = null; // so the caller doesn't return it again if Rent() throws
                buffer = arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(newSize);
            }
            return buffer.Slice(0, bytesWritten);
        }

        /// <summary>
        /// Encodes <paramref name="absPath"/> as UTF-8 into <paramref name="destination"/> with every symbolic link
        /// resolved, from the root down, so that paths reaching the same file through different links come out the
        /// same. Segments that do not exist (or cannot be examined) are copied unchanged, so the directories above a
        /// file that will be created later, such as a lock file, are still resolved. '.' and '..' segments, which may
        /// come from link targets, are left for <see cref="RemoveDotSegments(Span{byte}, int)"/>; that is correct
        /// because everything written before them is already free of links.
        /// </summary>
        /// <returns><c>false</c> if <paramref name="destination"/> is too small.</returns>
        /// <exception cref="IOException">A symbolic link could not be read, or there are too many levels of symbolic links.</exception>
        // LUCENENET: Harmony did this in resolveLink()/resolve() with a new byte[] per hop; this is the same walk
        // over the path with readlink(), without the allocations. Like Java's canonicalize(), ENOENT, ENOTDIR and
        // EACCES stop resolution and keep the rest of the path as it is; any other error is an IOException.
        private static unsafe bool TryResolveSymbolicLinks(ReadOnlySpan<char> absPath, Span<byte> destination, out int bytesWritten)
        {
            bytesWritten = 0;
            if (destination.Length < 2) // the root and a terminating null
            {
                return false;
            }

            int byteCount = Encoding.UTF8.GetByteCount(absPath);
            Span<byte> stackBuffer = byteCount <= Constants.MaxStackByteLimit ? stackalloc byte[byteCount] : Span<byte>.Empty;
            var pending = new PendingPath(stackBuffer, byteCount);
            byte[] linkTarget = ArrayPool<byte>.Shared.Rent(MaxSymbolicLinkTargetByteCount);
            try
            {
                pending.PushUtf8(absPath, byteCount);

                destination[0] = (byte)'/';
                int length = 1;
                int hops = 0;
                fixed (byte* destinationPtr = destination)
                fixed (byte* linkTargetPtr = linkTarget)
                {
                    while (!pending.IsEmpty)
                    {
                        ReadOnlySpan<byte> segment = pending.PopSegment();
                        if (segment.IsEmpty)
                        {
                            continue; // a doubled or trailing separator
                        }

                        // Append the segment after a separator (unless we are at the root), leaving room for a null terminator
                        bool needsSeparator = destination[length - 1] != '/';
                        if (length + (needsSeparator ? 1 : 0) + segment.Length + 1 > destination.Length)
                        {
                            return false;
                        }
                        if (needsSeparator)
                        {
                            destination[length++] = (byte)'/';
                        }
                        int segmentStart = length;
                        segment.CopyTo(destination.Slice(length));
                        length += segment.Length;
                        destination[length] = 0;

                        nint targetLength = Interop.Posix.readlink(destinationPtr, linkTargetPtr, (nuint)linkTarget.Length);
                        if (targetLength < 0)
                        {
                            int errno = Marshal.GetLastWin32Error();
                            if (errno == Interop.Posix.EINVAL)
                            {
                                continue; // not a symbolic link
                            }
                            if (errno != Interop.Posix.ENOENT && errno != Interop.Posix.ENOTDIR && errno != Interop.Posix.EACCES)
                            {
                                throw new IOException($"readlink {Encoding.UTF8.GetString(destination.Slice(0, length))} failed (errno {errno})", errno);
                            }
                            // Otherwise nothing below this point can be examined (Java's canonicalize() gives up on the
                            // same three errors), so we fall through and keep the rest of the path as it is.
                        }
                        else if (targetLength > 0) // a link with an empty target can't be followed either
                        {
                            if (targetLength >= linkTarget.Length)
                            {
                                // readlink() truncates silently, so a full buffer may mean a longer target
                                throw new IOException($"Symbolic link target is too long: {Encoding.UTF8.GetString(destination.Slice(0, length))}");
                            }
                            if (++hops > MaxSymbolicLinkHops)
                            {
                                throw new IOException($"Too many levels of symbolic links: {absPath.ToString()}");
                            }

                            // Replace the link with its target. Go back to the directory containing the link...
                            length = segmentStart > 1 ? segmentStart - 1 : 1;
                            ReadOnlySpan<byte> target = linkTarget.AsSpan(0, (int)targetLength);
                            if (target[0] == '/')
                            {
                                // ...or to the root for an absolute target, whose parent directories are resolved again
                                length = 1;
                                target = target.Slice(1);
                            }
                            // ...and examine the target's segments before the rest of the path
                            pending.PushSeparator();
                            pending.Push(target);
                            continue;
                        }

                        // Keep the rest of the path as it is
                        ReadOnlySpan<byte> rest = pending.Remaining;
                        if (length + 1 + rest.Length > destination.Length)
                        {
                            return false;
                        }
                        destination[length++] = (byte)'/';
                        foreach (byte b in rest)
                        {
                            if (b == '/' && destination[length - 1] == '/')
                            {
                                continue; // collapse doubled separators like Path.GetFullPath() does
                            }
                            destination[length++] = b;
                        }
                        break;
                    }
                }

                bytesWritten = length;
                return true;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(linkTarget);
                pending.Dispose();
            }
        }

        // Removes "." and ".." segments and a trailing separator from the UTF-8 path in place and normalizes the
        // drive letter to upper case on Windows. Returns the new length. It can work in place because the write
        // position never passes the read position.
        // LUCENENET: Ported from Apache Harmony's File.getCanonicalPath(), minus the separator position array.
        // Harmony counted dots anywhere in a segment, so "a." lost its dot and the separator after it, "a.." went
        // up a level and "..." went up two. Windows strips trailing dots from names itself, but on Unix they are
        // ordinary names, so (like Java's collapse()) only a segment that is exactly "." or ".." is special here.
        private static int RemoveDotSegments(Span<byte> path, int length)
        {
            byte separator = (byte)Path.DirectorySeparatorChar;
            int rootLoc = 0;
            if (separator == '\\')
            {
                if (path[0] == '\\')
                {
                    rootLoc = (length > 1 && path[1] == '\\') ? 1 : 0;
                }
                else
                {
                    rootLoc = 2; // skip drive i.e. c:
                }
            }
            int newLength = 0, foundDots = 0;
            bool foundText = false; // the current segment has a character other than '.'
            for (int i = 0; i <= length; i++)
            {
                if (i < rootLoc)
                {
                    // Normalize case of Windows drive letter to upper
                    path[newLength++] = (byte)char.ToUpperInvariant((char)path[i]);
                    continue;
                }
                if (i == length || path[i] == separator)
                {
                    if (!foundText && foundDots == 1)
                    {
                        /* Don't write anything, just reset and continue */
                        foundDots = 0;
                        continue;
                    }
                    if (!foundText && foundDots == 2)
                    {
                        /* Go back one level, but not above the root */
                        // The last byte written is the separator before this segment; find the one before that.
                        int lastSeparator = newLength - 1;
                        if (lastSeparator > rootLoc)
                        {
                            do
                            {
                                lastSeparator--;
                            } while (lastSeparator > rootLoc && path[lastSeparator] != separator);
                        }
                        newLength = lastSeparator + 1;
                        foundDots = 0;
                        continue;
                    }
                    /* Any other dots are part of a name */
                    for (int j = 0; j < foundDots; j++)
                    {
                        path[newLength++] = (byte)'.';
                    }
                    foundDots = 0;
                    foundText = false;
                    if (i == length)
                    {
                        break;
                    }
                    path[newLength++] = separator;
                    continue;
                }
                if (path[i] == '.')
                {
                    foundDots++;
                    continue;
                }
                /* Found some dots within text, write them out */
                for (int j = 0; j < foundDots; j++)
                {
                    path[newLength++] = (byte)'.';
                }
                foundDots = 0;
                path[newLength++] = path[i];
                foundText = true;
            }
            // remove trailing slash
            if (newLength > (rootLoc + 1) && path[newLength - 1] == separator)
            {
                newLength--;
            }
            // LUCENENET: Harmony appended a null terminator here and trimmed it from the string; we don't need one.
            return newLength;
        }

        // LUCENENET specific: The part of a path that TryResolveSymbolicLinks() has not examined yet. It sits at
        // the end of its buffer and is consumed from the front, so a symbolic link's target can be pushed in front
        // of the rest of the path without moving the rest:
        //
        //   buffer: [ headroom | link target | '/' | rest of the path ]
        //                      ^ start                                ^ buffer.Length
        //
        // The buffer grows (from the pool) only when a target doesn't fit in the headroom.
        private ref struct PendingPath
        {
            private Span<byte> buffer;
            private int start;
            private byte[]? arrayToReturnToPool;

            public PendingPath(Span<byte> stackBuffer, int capacity)
            {
                if (stackBuffer.Length >= capacity)
                {
                    buffer = stackBuffer;
                    arrayToReturnToPool = null;
                }
                else
                {
                    buffer = arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(capacity);
                }
                start = buffer.Length;
            }

            public bool IsEmpty => start == buffer.Length;

            public ReadOnlySpan<byte> Remaining => buffer.Slice(start);

            // Returns the next segment and moves past it and the separator that follows it.
            // The result is only valid until the next Push.
            public ReadOnlySpan<byte> PopSegment()
            {
                ReadOnlySpan<byte> remaining = Remaining;
                int end = remaining.IndexOf((byte)'/');
                if (end < 0)
                {
                    end = remaining.Length;
                }
                start += Math.Min(end + 1, remaining.Length);
                return remaining.Slice(0, end);
            }

            public void Push(ReadOnlySpan<byte> bytes)
            {
                EnsureHeadroom(bytes.Length);
                start -= bytes.Length;
                bytes.CopyTo(buffer.Slice(start));
            }

            public void PushSeparator()
            {
                EnsureHeadroom(1);
                buffer[--start] = (byte)'/';
            }

            public void PushUtf8(ReadOnlySpan<char> chars, int byteCount)
            {
                EnsureHeadroom(byteCount);
                start -= byteCount;
                Encoding.UTF8.GetBytes(chars, buffer.Slice(start, byteCount));
            }

            private void EnsureHeadroom(int count)
            {
                if (start >= count)
                {
                    return;
                }
                int remainingLength = buffer.Length - start;
                byte[] newArray = ArrayPool<byte>.Shared.Rent(Math.Max(buffer.Length * 2, remainingLength + count));
                int newStart = newArray.Length - remainingLength;
                buffer.Slice(start).CopyTo(newArray.AsSpan(newStart));
                ArrayPool<byte>.Shared.ReturnIfNotNull(arrayToReturnToPool);
                buffer = arrayToReturnToPool = newArray;
                start = newStart;
            }

            public void Dispose()
            {
                ArrayPool<byte>.Shared.ReturnIfNotNull(arrayToReturnToPool);
                this = default;
            }
        }
    }
}
