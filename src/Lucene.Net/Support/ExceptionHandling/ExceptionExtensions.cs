using Lucene.Net.Diagnostics;
using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;

namespace Lucene
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
    /// Extension methods to close gaps when catching exceptions in .NET.
    /// <para/>
    /// These methods make it possible to catch only the types for a general exception
    /// type in Java even though the exception inheritance structure is different in .NET
    /// and does not map 1-to-1 with Java exceptions.
    /// </summary>
    internal static class ExceptionExtensions
    {
        internal static Type NUnitResultStateExceptionType = null; // All NUnit exceptions derive from this base class
        internal static Type NUnitAssertionExceptionType = null;
        internal static Type NUnitMultipleAssertExceptionType = null;
        internal static Type NUnitInconclusiveExceptionType = null;
        internal static Type DebugAssertExceptionType = null;

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a Throwable
        /// in Java. Throwable is the base class for all errors in Java.
        /// </summary>
        /// <param name="e">Unused, all errors in Java are throwble.</param>
        /// <returns>Always returns <c>true</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsThrowable(this Exception e)
        {
            return !(e is null);
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to an AssertionError
        /// in Java. Error indicates serious problems that a reasonable application
        /// should not try to catch.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an AssertionError type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAssertionError(this Exception e)
        {
            if (e is null) return false;

            return e is AssertionException ||
                (!(DebugAssertExceptionType is null) && DebugAssertExceptionType.IsAssignableFrom(e.GetType())) ||
                // Ignore NUnit exceptions (in tests)
                (!(NUnitAssertionExceptionType is null) && NUnitAssertionExceptionType.IsAssignableFrom(e.GetType())) ||
                (!(NUnitMultipleAssertExceptionType is null) && NUnitMultipleAssertExceptionType.IsAssignableFrom(e.GetType()));
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to an Error
        /// in Java. Error indicates serious problems that a reasonable application
        /// should not try to catch.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an Error type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsError(this Exception e)
        {
            if (e is null) return false;

            return
                e is IError ||
                e is OutOfMemoryException ||
                e is AssertionException ||
                // e.IsNoClassDefFoundError() || // NOTE: These are technically errors, but they overlap other exception types in .NET. Since Lucene always catches this at the source, we can ignore here.
                e is StackOverflowException || // Not catchable in .NET unless we throw it, but mainly here just for documentation purposes
                // Ignore .NET debug assert statements (only valid when test framework is attached)
                (!(DebugAssertExceptionType is null) && DebugAssertExceptionType.IsAssignableFrom(e.GetType())) ||
                // Ignore NUnit exceptions (in tests)
                (!(NUnitResultStateExceptionType is null) && NUnitResultStateExceptionType.IsAssignableFrom(e.GetType()) &&
                    // Exclude InconclusiveException - AssumptionViolatedException derives from RuntimeException in Java so it is not an Error type
                    (!(NUnitInconclusiveExceptionType is null) && NUnitInconclusiveExceptionType.IsAssignableFrom(e.GetType())) == false);
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to an Exception
        /// in Java. RuntimeException in Java indicates conditions that a reasonable application
        /// might want to catch.
        /// <para/>
        /// WARNING: Error in Java doesn't inherit from Exception, so it is important to use
        /// this method in a catch block. Instead of <c>catch (Exception e)</c>, use
        /// <c>catch (Exception e) when (e.IsException())</c>.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an Exception type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsException(this Exception e)
        {
            return e is Exception && !IsError(e); // IMPORTANT: Error types should not be identified here.
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a RuntimeException
        /// in Java. RuntimeException in Java indicates an unchecked exception. Unchecked
        /// exceptions don't force the developer to make a decision whether to handle or re-throw
        /// the excption, it can safely be ignored and allowed to propagate.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to a RuntimeException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRuntimeException(this Exception e)
        {
            // LUCENENET TODO: After creating a RuntimeException replacement (that at least implments IRuntimeException,
            // we can use the below code instead and do the proper catch in TestIndexWriterExceptions.IndexerThread using IsRuntimeException().
            // For now, this is close enough but we need a separate exception type for our tests to do checks.
            return IsException(e);

            //if (e is null) return false;

            //var typeofE = e.GetType();

            //return e is IRuntimeException ||

            //    e is IndexOutOfRangeException ||
            //    e is ArgumentOutOfRangeException ||

            //    e is ArgumentException ||

            //    e is NullReferenceException ||
            //    e is ArgumentNullException ||

            //    e is NotSupportedException ||

            //    e is ThreadInterruptedException ||

            //    e is SecurityException ||

            //    e is ObjectDisposedException ||

            //    // Known implemetnations of IRuntimeException
            //    //e is BytesRefHash.MaxBytesLengthExceededException ||
            //    //e is CollectionTerminatedException ||
            //    //e is TimeLimitingCollector.TimeExceededException ||
            //    //e is BooleanQuery.TooManyClausesException ||

            //    //e is Lucene.Net.Util.AlreadySetException || // Subclasses InvalidOperationException

            //    e is J2N.IO.BufferUnderflowException ||
            //    e is J2N.IO.BufferOverflowException ||
            //    e is J2N.IO.InvalidMarkException ||

            //    e is InvalidCastException ||

            //    e is InvalidOperationException ||

            //    e is MissingManifestResourceException ||

            //    typeof(FormatException).Equals(typeofE) || // Thrown only on datetime and number format problems, ignore all other FormatException errors (including ParseException)

            //    NUnitInconclusiveExceptionType is null ? false : NUnitInconclusiveExceptionType.IsAssignableFrom(typeofE);
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to an IOException
        /// in Java.
        /// <para/>
        /// WARNING: java.nio.file.AccessDeniedException inherits from IOException,
        /// its .NET counterpart <see cref="UnauthorizedAccessException"/> does not. Therefore, is important to use
        /// this method in a catch block to ensure there are no gaps. Instead of <c>catch (IOException e)</c>, use
        /// <c>catch (Exception e) when (e.IsIOException())</c>.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an IOException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIOException(this Exception e)
        {
            return e is IOException ||
                e.IsAlreadyClosedException() || // In Lucene, AlreadyClosedException subclass IOException instead of InvalidOperationException, so we need a special case here
                e is UnauthorizedAccessException; // In Java, java.nio.file.AccessDeniedException subclasses IOException
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to an ArrayIndexOutOfBoundsException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an ArrayIndexOutOfBoundsException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsArrayIndexOutOfBoundsException(this Exception e)
        {
            return e is ArgumentOutOfRangeException ||
                e is IndexOutOfRangeException; // LUCENENET TODO: These could be real problems where excptions can be prevevented that our catch blocks are hiding
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a StringIndexOutOfBoundsException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to a StringIndexOutOfBoundsException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsStringIndexOutOfBoundsException(this Exception e)
        {
            return e is ArgumentOutOfRangeException ||
                e is IndexOutOfRangeException; // LUCENENET TODO: These could be real problems where excptions can be prevevented that our catch blocks are hiding
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to an IndexOutOfBoundsException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an IndexOutOfBoundsException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIndexOutOfBoundsException(this Exception e)
        {
            return e is ArgumentOutOfRangeException ||
                e is IndexOutOfRangeException; // LUCENENET TODO: These could be real problems where excptions can be prevevented that our catch blocks are hiding
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a NoSuchFileException
        /// or a FileNotFoundExcpetion in Java.
        /// <para/>
        /// NOTE: In Java, there is no distinction between file and directory, and FileNotFoundException is thrown
        /// in either case. Therefore, this handler also catches <see cref="System.IO.DirectoryNotFoundException"/>.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to a NoSuchFileException or a FileNotFoundException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNoSuchFileExceptionOrFileNotFoundException(this Exception e)
        {
            return e is FileNotFoundException ||
                // Java doesn't have an equivalent to DirectoryNotFoundExcption, but
                // Lucene added one that subclassed java.io.FileNotFoundException
                // that we didn't add to the .NET port.
                e is DirectoryNotFoundException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a ParseException
        /// in Java.
        /// <para/>
        /// IMPORTANT: QueryParser has its own ParseException types (there are multiple),
        /// so be sure not to use this exception instead of the ones in QueryParser.
        /// For QueryParser exceptions, there are no extension methods to use for identification
        /// in catch blocks, you should instead use the fully-qualified name of the exception.
        /// <code>
        /// catch (Lucene.Net.QueryParsers.Surround.Parser.ParseException e)
        /// </code>
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to a ParseException type
        /// in Java; otherwise <c>false</c>.</returns>
        /// <seealso cref="IsNumberFormatException(Exception)"/>
        /// <seealso cref="ParseException"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsParseException(this Exception e)
        {
            return e is FormatException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a NumberFormatException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an NumberFormatException type
        /// in Java; otherwise <c>false</c>.</returns>
        /// <seealso cref="IsParseException(Exception)"/>
        /// <seealso cref="ParseException"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumberFormatException(this Exception e)
        {
            return e is FormatException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to an InvocationTargetException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an InvocationTargetException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInvocationTargetException(this Exception e)
        {
            return e is TargetInvocationException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to an IllegalAccessException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an IllegalAccessException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIllegalAccessException(this Exception e)
        {
            return e is MemberAccessException ||
                e is TypeAccessException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to an IllegalArgumentException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an IllegalArgumentException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIllegalArgumentException(this Exception e)
        {
            // LUCENENET: In production, there is a chance that we will upgrade to ArgumentNullExcpetion or ArgumentOutOfRangeException
            // and it is still important that those are caught. However, we have a copy of this method in the test environment
            // where this is done more strictly to catch ArgumentException without its known subclasses so we can be more explicit in tests.
            return e is ArgumentException;
                //!(e is ArgumentNullException) &&     // Corresponds to NullPointerException, so we don't catch it here.
                //!(e is ArgumentOutOfRangeException); // Corresponds to IndexOutOfBoundsException (and subclasses), so we don't catch it here.
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a NullPointerException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to a NullPointerException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullPointerException(this Exception e)
        {
            return e is ArgumentNullException ||
                e is NullReferenceException; // LUCENENET TODO: These could be real problems where excptions can be prevevented that our catch blocks are hiding
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to an InstantiationException
        /// (Reflection) in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an InstantiationException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInstantiationException(this Exception e)
        {
            // LUCENENET: In Java, the exception comes from Reflection. In .NET, it may
            // happen due to a class initializer that throws an uncaught exception.
            // These 2 are definitely not the same thing, but I am not sure it makes a difference.
            return e is TypeInitializationException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to an UnsupportedOperationException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an UnsupportedOperationException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUnsupportedOperationException(this Exception e)
        {
            return e is NotSupportedException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to an UnsupportedEncodingException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an UnsupportedEncodingException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUnsupportedEncodingException(this Exception e)
        {
            // According to the docs, this maps to 2 potential exceptions:
            // https://docs.microsoft.com/en-us/dotnet/api/system.text.encoding.getencoding?view=net-5.0
            return e is ArgumentException ||
                e is PlatformNotSupportedException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to an InterruptedException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an InterruptedException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInterruptedException(this Exception e)
        {
            // LUCENENET: Special case - we only catch under certain scenarios and do not rethrow explicitly.
            // This exception is the shutdown signal for a thread and it is used in Lucene for control flow.
            // However, in .NET it is thrown in more cases than in Java. So, rather than wrapping it in a new
            // exception type, we don't catch it unless there is a specific reason to do something other than re-throw.
            return e is ThreadInterruptedException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a CompressorException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to a CompressorException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompressorException(this Exception e)
        {
            return e is InvalidDataException; // LUCENENET TODO: Not sure if this is the right exception
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a DataFormatException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to a DataFormatException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDataFormatException(this Exception e)
        {
            return e is InvalidDataException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a SecurityException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to a SecurityException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSecurityException(this Exception e)
        {
            return e is SecurityException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a NoSuchDirectoryException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to a NoSuchDirectoryException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNoSuchDirectoryException(this Exception e)
        {
            return e is DirectoryNotFoundException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to an OutOfMemoryError
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an OutOfMemoryError type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOutOfMemoryError(this Exception e)
        {
            return e is OutOfMemoryException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to an AlreadyClosedException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an AlreadyClosedException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAlreadyClosedException(this Exception e)
        {
            return e is ObjectDisposedException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a ClassCastException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to a ClassCastException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsClassCastException(this Exception e)
        {
            return e is InvalidCastException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to an EOFException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an EOFException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEOFException(this Exception e)
        {
            return e is EndOfStreamException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to an IllegalStateException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to an IllegalStateException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIllegalStateException(this Exception e)
        {
            return e is InvalidOperationException &&
                !(e is ObjectDisposedException); // In .NET, ObjectDisposedException subclases InvalidOperationException, but Lucene decided to use IOException for AlreadyClosedException
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a StackOverflowError
        /// in Java.
        /// <para/>
        /// IMPORTANT: When catching this exception in .NET, put the try catch logic inside of
        /// <c>#if FEATURE_STACKOVERFLOWEXCEPTION__ISCATCHABLE</c> blocks because this exception
        /// is not catchable on newer flavors of .NET.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to a StackOverflowError type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsStackOverflowError(this Exception e)
        {
            return e is StackOverflowException; // Uncatchable in .NET core, be sure to use with 
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a MissingResourceException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to a MissingResourceException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMissingResourceException(this Exception e)
        {
            return e is MissingManifestResourceException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a NoClassDefFoundError
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to a NoClassDefFoundError type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNoClassDefFoundError(this Exception e)
        {
            return e is TypeLoadException; // LUCENENET NOTE: Not an exact match for Java behavior, but will be thrown if the type or any of its dependencies can't load, which is similar.
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a ClassNotFoundException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to a ClassNotFoundException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsClassNotFoundException(this Exception e)
        {
            return e is TypeLoadException; // LUCENENET NOTE: In the case of calling Activator.CreateInstance when the type string doesn't exist, this is the expected exception (there may be other cases to cover here)
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a NoSuchMethodException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to a NoSuchMethodException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNoSuchMethodException(this Exception e)
        {
            return e is MissingMethodException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a ArithmeticException
        /// in Java.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to a ArithmeticException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsArithmeticException(this Exception e)
        {
            return e is ArithmeticException;
        }

        /// <summary>
        /// Used to check whether <paramref name="e"/> corresponds to a AccessDeniedException
        /// in Java.
        /// <para/>
        /// This is an a low level IO exception from the underlying operating system when
        /// there are insufficient permissions to access a file or folder.
        /// </summary>
        /// <param name="e">This exception.</param>
        /// <returns><c>true</c> if <paramref name="e"/> corresponds to a AccessDeniedException type
        /// in Java; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAccessDeniedException(this Exception e)
        {
            return e is UnauthorizedAccessException;
        }
    }
}
