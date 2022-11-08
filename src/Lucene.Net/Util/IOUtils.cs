using J2N;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;

namespace Lucene.Net.Util
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

    using Directory = Lucene.Net.Store.Directory;

    /// <summary>
    /// This class emulates the new Java 7 "Try-With-Resources" statement.
    /// Remove once Lucene is on Java 7.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    [ExceptionToClassNameConvention]
    public static class IOUtils // LUCENENET specific - made static
    {
        /// <summary>
        /// UTF-8 <see cref="Encoding"/> instance to prevent repeated
        /// <see cref="Encoding.UTF8"/> lookups </summary>
        [Obsolete("Use Encoding.UTF8 instead.")]
        public static readonly Encoding CHARSET_UTF_8 = Encoding.UTF8;

        /// <summary>
        /// UTF-8 charset string.
        /// <para/>Where possible, use <see cref="Encoding.UTF8"/> instead,
        /// as using the <see cref="string"/> constant may slow things down. </summary>
        /// <seealso cref="Encoding.UTF8"/>
        public static readonly string UTF_8 = "UTF-8";

        /// <summary>
        /// <para>Disposes all given <c>IDisposable</c>s, suppressing all thrown exceptions. Some of the <c>IDisposable</c>s
        /// may be <c>null</c>, they are ignored. After everything is disposed, method either throws <paramref name="priorException"/>,
        /// if one is supplied, or the first of suppressed exceptions, or completes normally.</para>
        /// <para>Sample usage:
        /// <code>
        /// IDisposable resource1 = null, resource2 = null, resource3 = null;
        /// ExpectedException priorE = null;
        /// try 
        /// {
        ///     resource1 = ...; resource2 = ...; resource3 = ...; // Acquisition may throw ExpectedException
        ///     ..do..stuff.. // May throw ExpectedException
        /// } 
        /// catch (ExpectedException e) 
        /// {
        ///     priorE = e;
        /// } 
        /// finally 
        /// {
        ///     IOUtils.CloseWhileHandlingException(priorE, resource1, resource2, resource3);
        /// }
        /// </code>
        /// </para> 
        /// </summary>
        /// <param name="priorException">  <c>null</c> or an exception that will be rethrown after method completion. </param>
        /// <param name="objects">         Objects to call <see cref="IDisposable.Dispose()"/> on. </param>
        [Obsolete("Use DisposeWhileHandlingException(Exception, params IDisposable[]) instead.")]
        public static void CloseWhileHandlingException(Exception priorException, params IDisposable[] objects)
        {
            DisposeWhileHandlingException(priorException, objects);
        }

        /// <summary>
        /// Disposes all given <see cref="IDisposable"/>s, suppressing all thrown exceptions. </summary>
        /// <seealso cref="DisposeWhileHandlingException(Exception, IDisposable[])"/>
        [Obsolete("Use DisposeWhileHandlingException(Exception, IEnumerable<IDisposable>) instead.")]
        public static void CloseWhileHandlingException(Exception priorException, IEnumerable<IDisposable> objects)
        {
            DisposeWhileHandlingException(priorException, objects);
        }

        /// <summary>
        /// Disposes all given <see cref="IDisposable"/>s.  Some of the
        /// <see cref="IDisposable"/>s may be <c>null</c>; they are
        /// ignored.  After everything is closed, the method either
        /// throws the first exception it hit while closing, or
        /// completes normally if there were no exceptions.
        /// </summary>
        /// <param name="objects">
        ///          Objects to call <see cref="IDisposable.Dispose()"/> on </param>
        [Obsolete("Use Dispose(params IDisposable[]) instead.")]
        public static void Close(params IDisposable[] objects)
        {
            Dispose(objects);
        }

        /// <summary>
        /// Disposes all given <see cref="IDisposable"/>s. </summary>
        /// <seealso cref="Dispose(IDisposable[])"/>
        [Obsolete("Use Dispose(IEnumerable<IDisposable>) instead.")]
        public static void Close(IEnumerable<IDisposable> objects)
        {
            Dispose(objects);
        }

        /// <summary>
        /// Disposes all given <see cref="IDisposable"/>s, suppressing all thrown exceptions.
        /// Some of the <see cref="IDisposable"/>s may be <c>null</c>, they are ignored.
        /// </summary>
        /// <param name="objects">
        ///          Objects to call <see cref="IDisposable.Dispose()"/> on </param>
        [Obsolete("Use DisposeWhileHandlingException(params IDisposable[]) instead.")]
        public static void CloseWhileHandlingException(params IDisposable[] objects)
        {
            DisposeWhileHandlingException(objects);
        }

        /// <summary>
        /// Disposes all given <see cref="IDisposable"/>s, suppressing all thrown exceptions. </summary>
        /// <seealso cref="DisposeWhileHandlingException(IEnumerable{IDisposable})"/>
        /// <seealso cref="DisposeWhileHandlingException(IDisposable[])"/>
        [Obsolete("Use DisposeWhileHandlingException(IEnumerable<IDisposable>) instead.")]
        public static void CloseWhileHandlingException(IEnumerable<IDisposable> objects)
        {
            DisposeWhileHandlingException(objects);
        }


        // LUCENENET specific - added overloads starting with Dispose... instead of Close...

        /// <summary>
        /// <para>Disposes all given <c>IDisposable</c>s, suppressing all thrown exceptions. Some of the <c>IDisposable</c>s
        /// may be <c>null</c>, they are ignored. After everything is disposed, method either throws <paramref name="priorException"/>,
        /// if one is supplied, or the first of suppressed exceptions, or completes normally.</para>
        /// <para>Sample usage:
        /// <code>
        /// IDisposable resource1 = null, resource2 = null, resource3 = null;
        /// ExpectedException priorE = null;
        /// try 
        /// {
        ///     resource1 = ...; resource2 = ...; resource3 = ...; // Acquisition may throw ExpectedException
        ///     ..do..stuff.. // May throw ExpectedException
        /// } 
        /// catch (ExpectedException e) 
        /// {
        ///     priorE = e;
        /// } 
        /// finally 
        /// {
        ///     IOUtils.DisposeWhileHandlingException(priorE, resource1, resource2, resource3);
        /// }
        /// </code>
        /// </para> 
        /// </summary>
        /// <param name="priorException">  <c>null</c> or an exception that will be rethrown after method completion. </param>
        /// <param name="objects">         Objects to call <see cref="IDisposable.Dispose()"/> on. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeWhileHandlingException(Exception priorException, params IDisposable[] objects)
        {
            Exception th = null;

            foreach (IDisposable @object in objects)
            {
                try
                {
                    @object?.Dispose();
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    AddSuppressed(priorException ?? th, t);
                    if (th is null)
                    {
                        th = t;
                    }
                }
            }

            if (priorException != null)
            {
                ExceptionDispatchInfo.Capture(priorException).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
            }
            else
            {
                ReThrow(th);
            }
        }

        /// <summary>
        /// Disposes all given <see cref="IDisposable"/>s, suppressing all thrown exceptions. </summary>
        /// <seealso cref="DisposeWhileHandlingException(Exception, IDisposable[])"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeWhileHandlingException(Exception priorException, IEnumerable<IDisposable> objects) 
        {
            Exception th = null;

            foreach (IDisposable @object in objects)
            {
                try
                {
                    @object?.Dispose();
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    AddSuppressed(priorException ?? th, t);
                    if (th is null)
                    {
                        th = t;
                    }
                }
            }

            if (priorException != null)
            {
                ExceptionDispatchInfo.Capture(priorException).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
            }
            else
            {
                ReThrow(th);
            }
        }

        /// <summary>
        /// Disposes all given <see cref="IDisposable"/>s.  Some of the
        /// <see cref="IDisposable"/>s may be <c>null</c>; they are
        /// ignored.  After everything is closed, the method either
        /// throws the first exception it hit while closing, or
        /// completes normally if there were no exceptions.
        /// </summary>
        /// <param name="objects">
        ///          Objects to call <see cref="IDisposable.Dispose()"/> on </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Dispose(params IDisposable[] objects) 
        {
            Exception th = null;

            foreach (IDisposable @object in objects)
            {
                try
                {
                    @object?.Dispose();
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    AddSuppressed(th, t);
                    if (th is null)
                    {
                        th = t;
                    }
                }
            }

            ReThrow(th);
        }

        /// <summary>
        /// Disposes all given <see cref="IDisposable"/>s. </summary>
        /// <seealso cref="Dispose(IDisposable[])"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Dispose(IEnumerable<IDisposable> objects)
        {
            Exception th = null;

            foreach (IDisposable @object in objects)
            {
                try
                {
                    @object?.Dispose();
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    AddSuppressed(th, t);
                    if (th is null)
                    {
                        th = t;
                    }
                }
            }

            ReThrow(th);
        }

        /// <summary>
        /// Disposes all given <see cref="IDisposable"/>s, suppressing all thrown exceptions.
        /// Some of the <see cref="IDisposable"/>s may be <c>null</c>, they are ignored.
        /// </summary>
        /// <param name="objects">
        ///          Objects to call <see cref="IDisposable.Dispose()"/> on </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeWhileHandlingException(params IDisposable[] objects) 
        {
            foreach (var o in objects)
            {
                try
                {
                    o?.Dispose();
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    //eat it
                }
            }
        }

        /// <summary>
        /// Disposes all given <see cref="IDisposable"/>s, suppressing all thrown exceptions. </summary>
        /// <seealso cref="DisposeWhileHandlingException(IDisposable[])"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeWhileHandlingException(IEnumerable<IDisposable> objects)
        {
            foreach (IDisposable @object in objects)
            {
                try
                {
                    @object?.Dispose();
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    //eat it
                }
            }
        }

        /// <summary>
        /// Since there's no C# equivalent of Java's Exception.AddSuppressed, we add the
        /// suppressed exceptions to a data field via the 
        /// <see cref="ExceptionExtensions.AddSuppressed(Exception, Exception)"/> method.
        /// <para/>
        /// The exceptions can be retrieved by calling <see cref="ExceptionExtensions.GetSuppressed(Exception)"/>
        /// or <see cref="ExceptionExtensions.GetSuppressedAsList(Exception)"/>.
        /// </summary>
        /// <param name="exception"> this exception should get the suppressed one added </param>
        /// <param name="suppressed"> the suppressed exception </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddSuppressed(Exception exception, Exception suppressed)
        {
            if (exception != null && suppressed != null)
            {
                exception.AddSuppressed(suppressed);
            }
        }

        /// <summary>
        /// Wrapping the given <see cref="Stream"/> in a reader using a <see cref="Encoding"/>.
        /// Unlike Java's defaults this reader will throw an exception if your it detects
        /// the read charset doesn't match the expected <see cref="Encoding"/>.
        /// <para/>
        /// Decoding readers are useful to load configuration files, stopword lists or synonym files
        /// to detect character set problems. However, its not recommended to use as a common purpose
        /// reader.
        /// </summary>
        /// <param name="stream"> The stream to wrap in a reader </param>
        /// <param name="charSet"> The expected charset </param>
        /// <returns> A wrapping reader </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TextReader GetDecodingReader(Stream stream, Encoding charSet)
        {
            return new StreamReader(stream, charSet);
        }

        /// <summary>
        /// Opens a <see cref="TextReader"/> for the given <see cref="FileInfo"/> using a <see cref="Encoding"/>.
        /// Unlike Java's defaults this reader will throw an exception if your it detects
        /// the read charset doesn't match the expected <see cref="Encoding"/>.
        /// <para/>
        /// Decoding readers are useful to load configuration files, stopword lists or synonym files
        /// to detect character set problems. However, its not recommended to use as a common purpose
        /// reader. </summary>
        /// <param name="file"> The file to open a reader on </param>
        /// <param name="charSet"> The expected charset </param>
        /// <returns> A reader to read the given file </returns>
        public static TextReader GetDecodingReader(FileInfo file, Encoding charSet)
        {
            FileStream stream = null;
            bool success = false;
            try
            {
                stream = file.OpenRead();
                TextReader reader = GetDecodingReader(stream, charSet);
                success = true;
                return reader;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.Dispose(stream);
                }
            }
        }

        /// <summary>
        /// Opens a <see cref="TextReader"/> for the given resource using a <see cref="Encoding"/>.
        /// Unlike Java's defaults this reader will throw an exception if your it detects
        /// the read charset doesn't match the expected <see cref="Encoding"/>.
        /// <para/>
        /// Decoding readers are useful to load configuration files, stopword lists or synonym files
        /// to detect character set problems. However, its not recommended to use as a common purpose
        /// reader. </summary>
        /// <param name="clazz"> The class used to locate the resource </param>
        /// <param name="resource"> The resource name to load </param>
        /// <param name="charSet"> The expected charset </param>
        /// <returns> A reader to read the given file </returns>
        public static TextReader GetDecodingReader(Type clazz, string resource, Encoding charSet)
        {
            Stream stream = null;
            bool success = false;
            try
            {
                stream = clazz.FindAndGetManifestResourceStream(resource);
                TextReader reader = GetDecodingReader(stream, charSet);
                success = true;
                return reader;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.Dispose(stream);
                }
            }
        }

        /// <summary>
        /// Deletes all given files, suppressing all thrown <see cref="Exception"/>s.
        /// <para/>
        /// Note that the files should not be <c>null</c>.
        /// </summary>
        public static void DeleteFilesIgnoringExceptions(Directory dir, params string[] files)
        {
            foreach (string name in files)
            {
                try
                {
                    dir.DeleteFile(name);
                }
                catch (Exception ignored) when (ignored.IsThrowable())
                {
                    // ignore
                }
            }
        }

        /// <summary>
        /// Copy one file's contents to another file. The target will be overwritten
        /// if it exists. The source must exist.
        /// </summary>
        public static void Copy(FileInfo source, FileInfo target)
        {
            FileStream fis = null;
            FileStream fos = null;
            try
            {
                fis = source.OpenRead();
                fos = target.OpenWrite();

                byte[] buffer = new byte[1024 * 8];
                int len;
                while ((len = fis.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fos.Write(buffer, 0, len);
                }
            }
            finally
            {
                Dispose(fis, fos);
            }
        }

        /// <summary>
        /// Simple utilty method that takes a previously caught
        /// <see cref="Exception"/> and rethrows either 
        /// <see cref="IOException"/> or an unchecked exception.  If the
        /// argument is <c>null</c> then this method does nothing.
        /// </summary>
        public static void ReThrow(Exception th)
        {
            if (th != null)
            {
                if (th.IsIOException())
                {
                    ExceptionDispatchInfo.Capture(th).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
                }
                ReThrowUnchecked(th);
            }
        }

        /// <summary>
        /// Simple utilty method that takes a previously caught
        /// <see cref="Exception"/> and rethrows it as an unchecked exception.
        /// If the argument is <c>null</c> then this method does nothing.
        /// </summary>
        public static void ReThrowUnchecked(Exception th)
        {
            if (th != null)
            {
                if (th.IsRuntimeException())
                    ExceptionDispatchInfo.Capture(th).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
                if (th.IsError())
                    ExceptionDispatchInfo.Capture(th).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
                throw RuntimeException.Create(th);
            }
        }

        // LUCENENET specific: Fsync is pointless in .NET, since we are 
        // calling FileStream.Flush(true) before the stream is disposed
        // which means we never need it at the point in Java where it is called.
    }
}