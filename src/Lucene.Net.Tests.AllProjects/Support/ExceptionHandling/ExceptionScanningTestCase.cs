using Lucene.Net.Attributes;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Security;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Support.ExceptionHandling
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

    [LuceneNetSpecific]
    public abstract class ExceptionScanningTestCase : AssemblyScanningTestCase
    {
        // Internal types references
        private static readonly Type DebugAssertExceptionType =
            // .NET 5/.NET Core 3.x
            Type.GetType("System.Diagnostics.DebugProvider+DebugAssertException, System.Private.CoreLib")
            // .NET Core 2.x
            ?? Type.GetType("System.Diagnostics.Debug+DebugAssertException, System.Private.CoreLib");
        // .NET Framework doesn't throw in this case

        private static readonly Type MetadataExceptionType =
            // .NET Core/5.0
            Type.GetType("System.Reflection.MetadataException, System.Private.CoreLib") ??
            // .NET Framework
            Type.GetType("System.Reflection.MetadataException, mscorlib");

        private static readonly Type CrossAppDomainMarshaledExceptionType =
             // .NET Core 2.1 Only
             Type.GetType("System.CrossAppDomainMarshaledException, System.Private.CoreLib");

        private static readonly Type SwitchExpressionExceptionType =
            // No .NET Framework or .NET Core 2.x support
            // .NET Core 3.1
            Type.GetType("System.Runtime.CompilerServices.SwitchExpressionException, System.Runtime.Extensions") ??
            // .NET 5
            Type.GetType("System.Runtime.CompilerServices.SwitchExpressionException, System.Private.CoreLib");

        private static readonly Type RemotingExceptionType =
            // .NET Framework only
            Type.GetType("System.Runtime.Remoting.RemotingException, mscorlib");

        private static readonly Type RemotingTimeoutExceptionType =
            // .NET Framework only
            Type.GetType("System.Runtime.Remoting.RemotingTimeoutException, mscorlib");

        private static readonly Type RemotingServerExceptionType =
            // .NET Framework only
            Type.GetType("System.Runtime.Remoting.ServerException, mscorlib");

        private static readonly Type CryptographicUnexpectedOperationExceptionType =
            // .NET Framework only
            Type.GetType("System.Security.Cryptography.CryptographicUnexpectedOperationException, mscorlib");

        private static readonly Type HostProtectionExceptionType =
            // .NET Framework only
            Type.GetType("System.Security.HostProtectionException, mscorlib");

        private static readonly Type PolicyExceptionType =
            // .NET Framework only
            Type.GetType("System.Security.Policy.PolicyException, mscorlib");

        private static readonly Type IdentityNotMappedExceptionType =
            // .NET Framework only
            Type.GetType("System.Security.Principal.IdentityNotMappedException, mscorlib");

        private static readonly Type XmlSyntaxExceptionType =
            // .NET Framework only
            Type.GetType("System.Security.XmlSyntaxException, mscorlib");

        private static readonly Type PrivilegeNotHeldExceptionType =
            // .NET Framework only
            Type.GetType("System.Security.AccessControl.PrivilegeNotHeldException, mscorlib");

        private static readonly Type NUnitFrameworkInternalInvalidPlatformExceptionType =
            Type.GetType("NUnit.Framework.Internal.InvalidPlatformException, NUnit.Framework");


        // Base class Exception
        public static readonly ICollection<Type> DotNetExceptionTypes = LoadTypesSubclassing(baseClass: typeof(Exception), DotNetAssemblies);
        public static readonly ICollection<Type> NUnitExceptionTypes = LoadTypesSubclassing(baseClass: typeof(Exception), NUnitAssemblies);
        public static readonly ICollection<Type> LuceneExceptionTypes = LoadTypesSubclassing(baseClass: typeof(Exception), LuceneAssemblies);

        public static readonly ICollection<Type> AllExceptionTypes = DotNetExceptionTypes.Union(NUnitExceptionTypes).Union(LuceneExceptionTypes).ToList();

        // Base class IOException
        public static readonly ICollection<Type> DotNetIOExceptionTypes = LoadTypesSubclassing(baseClass: typeof(IOException), DotNetAssemblies);
        public static readonly ICollection<Type> NUnitIOExceptionTypes = LoadTypesSubclassing(baseClass: typeof(IOException), NUnitAssemblies);
        public static readonly ICollection<Type> LuceneIOExceptionTypes = LoadTypesSubclassing(baseClass: typeof(IOException), LuceneAssemblies);

        public static readonly ICollection<Type> AllIOExceptionTypes = DotNetIOExceptionTypes.Union(NUnitIOExceptionTypes).Union(LuceneIOExceptionTypes).ToList();

        #region Known types of exception families

        public static readonly IEnumerable<Type> KnownAssertionErrorTypes = LoadKnownAssertionErrorTypes();

        private static IEnumerable<Type> LoadKnownAssertionErrorTypes()
        {
            var result = new HashSet<Type>
            {
                typeof(NUnit.Framework.AssertionException),          // Corresponds to Java's AssertionError
                typeof(NUnit.Framework.MultipleAssertException),     // Corresponds to Java's AssertionError
                typeof(Lucene.Net.Diagnostics.AssertionException),   // Corresponds to Java's AssertionError

                // Types for use as Java Aliases in .NET
                typeof(Lucene.AssertionError),
            };

            // Special case - this doesn't exist on .NET Framework, so we only add it if not null
            if (!(DebugAssertExceptionType is null))
            {
                result.Add(DebugAssertExceptionType);                 // Corresponds to Java's AssertionError
            }
            return result;
        }

        public static readonly IEnumerable<Type> KnownErrorExceptionTypes = LoadKnownErrorExceptionTypes();

        private static IEnumerable<Type> LoadKnownErrorExceptionTypes()
        {
            return new HashSet<Type>(KnownAssertionErrorTypes)       // Include all known types that correspond to Java's AssertionError
            {
                typeof(NUnit.Framework.IgnoreException),             // Don't care - only used for testing and we shouldn't catch it in general
                typeof(OutOfMemoryException),                        // Corresponds to Java's OutOfMemoryError
                typeof(StackOverflowException),                      // Corresponds to Java's StackOverflowError
                typeof(InsufficientMemoryException),                 // OutOfMemoryException is the base class

                // Types for use as Java Aliases in .NET
                typeof(Lucene.Error),
#pragma warning disable CS0618 // Type or member is obsolete
                typeof(Lucene.StackOverflowError),
#pragma warning restore CS0618 // Type or member is obsolete
                typeof(Lucene.OutOfMemoryError),
                typeof(Lucene.NoClassDefFoundError),
                typeof(Lucene.ServiceConfigurationError),

                typeof(Lucene.Net.QueryParsers.Classic.TokenMgrError),
                typeof(Lucene.Net.QueryParsers.Flexible.Core.QueryNodeError),
                typeof(Lucene.Net.QueryParsers.Flexible.Standard.Parser.TokenMgrError),
                typeof(Lucene.Net.QueryParsers.Surround.Parser.TokenMgrError),

                typeof(NUnit.Framework.SuccessException), // Not sure about this, but it seems reasonable to ignore it in most cases because it is NUnit result state
            };
        }

        public static readonly IEnumerable<Type> KnownExceptionTypes = AllExceptionTypes
            // Exceptions in Java exclude Errors
            .Except(KnownErrorExceptionTypes)
            // Special Case: We never want to catch this NUnit exception
            .Where(t => !Type.Equals(t, NUnitFrameworkInternalInvalidPlatformExceptionType));

        public static readonly IEnumerable<Type> KnownThrowableExceptionTypes = AllExceptionTypes
            // Special Case: We never want to catch this NUnit exception
            .Where(t => !Type.Equals(t, NUnitFrameworkInternalInvalidPlatformExceptionType));


        public static readonly IEnumerable<Type> KnownIOExceptionTypes = new Type[] {
                typeof(UnauthorizedAccessException),
                typeof(ObjectDisposedException),
                typeof(Lucene.AlreadyClosedException),
            }.Union(AllIOExceptionTypes)
            // .NET Framework only - Subclasses UnauthorizedAccessException
            .Union(new[] { PrivilegeNotHeldExceptionType });

        public static readonly IEnumerable<Type> KnownIndexOutOfBoundsExceptionTypes = new Type[] {
            typeof(ArgumentOutOfRangeException),
            typeof(IndexOutOfRangeException),

            // Types for use as Java Aliases in .NET
            typeof(ArrayIndexOutOfBoundsException),
            typeof(StringIndexOutOfBoundsException),
            typeof(IndexOutOfBoundsException),
        };

        public static readonly IEnumerable<Type> KnownNullPointerExceptionTypes = new Type[] {
            typeof(ArgumentNullException),
            typeof(NullReferenceException),

            // Types for use as Java Aliases in .NET
            typeof(NullPointerException),
        };

        public static readonly IEnumerable<Type> KnownIllegalArgumentExceptionTypes = new Type[] {
            typeof(ArgumentException),
            typeof(ArgumentNullException),
            typeof(ArgumentOutOfRangeException),

            // Types for use as Java Aliases in .NET
            typeof(Lucene.IllegalArgumentException),
            typeof(Lucene.ArrayIndexOutOfBoundsException),
            typeof(Lucene.IndexOutOfBoundsException),
            typeof(Lucene.NullPointerException), // ArgumentNullException subclass
            typeof(Lucene.StringIndexOutOfBoundsException),

            // Subclasses
            typeof(System.DuplicateWaitObjectException),
            typeof(System.Globalization.CultureNotFoundException),
            typeof(System.Text.DecoderFallbackException),
            typeof(System.Text.EncoderFallbackException),
        };

        public static readonly IEnumerable<Type> KnownIllegalArgumentExceptionTypes_TestEnvironment = new Type[] {
            typeof(ArgumentException),

            // Types for use as Java Aliases in .NET
            typeof(IllegalArgumentException),

            // Subclasses
            typeof(System.DuplicateWaitObjectException),
            typeof(System.Globalization.CultureNotFoundException),
            typeof(System.Text.DecoderFallbackException),
            typeof(System.Text.EncoderFallbackException),
        };

        public static readonly IEnumerable<Type> KnownRuntimeExceptionTypes = LoadKnownRuntimeExceptionTypes();

        private static IEnumerable<Type> LoadKnownRuntimeExceptionTypes()
        {
            var result = new HashSet<Type>
            {

                // ******************************************************************************************
                // CONFIRMED TYPES - these are for sure mapping to a type in Java that we want to catch
                // ******************************************************************************************

                typeof(SystemException), // Roughly corresponds to RuntimeException

                // Corresponds to IndexOutOfBoundsException, StringIndexOutOfBoundsException, and ArrayIndexOutOfBoundsException
                typeof(IndexOutOfRangeException),
                typeof(ArgumentOutOfRangeException),
                typeof(Lucene.ArrayIndexOutOfBoundsException),
                typeof(Lucene.IndexOutOfBoundsException),
                typeof(Lucene.StringIndexOutOfBoundsException),

                // Corresponds to NullPointerException
                typeof(NullReferenceException),
                typeof(ArgumentNullException),
                typeof(Lucene.NullPointerException),

                // Corresponds to IllegalArgumentException
                typeof(ArgumentException),
                typeof(Lucene.IllegalArgumentException),

                // Corresponds to UnsupportedOperationException
                typeof(NotSupportedException),
                typeof(Lucene.UnsupportedOperationException),

                // Corresponds to Lucene's ThreadInterruptedException
                typeof(Lucene.Net.Util.ThreadInterruptedException),

                // Corresponds to SecurityException
                typeof(SecurityException),

                // Corresponds to ClassCastException
                typeof(InvalidCastException),

                // Corresponds to IllegalStateException
                typeof(InvalidOperationException),
                typeof(Lucene.IllegalStateException),

                // Corresponds to MissingResourceException
                typeof(MissingManifestResourceException),

                // Corresponds to NumberFormatException
                typeof(FormatException),
                typeof(Lucene.NumberFormatException),

                // Corresponds to ArithmeticException
                typeof(ArithmeticException),

                // Corresponds to IllformedLocaleException
                typeof(CultureNotFoundException),

                // Corresponds to JUnit's AssumptionViolatedException
                typeof(NUnit.Framework.InconclusiveException),

                // Known implementations of IRuntimeException

                typeof(RuntimeException),
                typeof(LuceneSystemException),

                typeof(BytesRefHash.MaxBytesLengthExceededException),
                typeof(CollectionTerminatedException),
                typeof(DocTermsIndexDocValues.DocTermsIndexException),
                typeof(MergePolicy.MergeException),
                typeof(SearcherExpiredException),
                typeof(TimeLimitingCollector.TimeExceededException),
                typeof(BooleanQuery.TooManyClausesException),

                // Other known runtime exceptions
                typeof(AlreadySetException), // Subclasses InvalidOperationException
                typeof(J2N.IO.BufferUnderflowException),
                typeof(J2N.IO.BufferOverflowException),
                typeof(J2N.IO.InvalidMarkException),
                typeof(Lucene.Net.Spatial.Queries.UnsupportedSpatialOperationException), // Subclasses NotSupportedException

                //typeof(NUnit.Framework.Internal.InvalidPlatformException),

                // ******************************************************************************************
                // UNCONFIRMED TYPES - these are SystemException types that are included, but require more
                // research to determine whether they actually are something we don't want to catch as a RuntimeException.
                // ******************************************************************************************

                typeof(AccessViolationException),
                typeof(AppDomainUnloadedException),
                typeof(ArrayTypeMismatchException),
                typeof(BadImageFormatException),
                typeof(CannotUnloadAppDomainException),
                typeof(KeyNotFoundException),
                typeof(ContextMarshalException),
                typeof(DataMisalignedException),
                typeof(DivideByZeroException), // Subclasses ArithmeticException, so probably okay
                typeof(DllNotFoundException),
                typeof(DuplicateWaitObjectException),
                typeof(EntryPointNotFoundException),
#pragma warning disable CS0618 // Type or member is obsolete
                typeof(ExecutionEngineException),
#pragma warning restore CS0618 // Type or member is obsolete
                typeof(InsufficientExecutionStackException),
                typeof(InvalidProgramException),
                typeof(InvalidDataException),
                typeof(MulticastNotSupportedException),
                typeof(NotFiniteNumberException), // Subclasses ArithmeticException, so probably okay
                typeof(NotImplementedException),
                typeof(OperationCanceledException),
                typeof(OverflowException), // Subclasses ArithmeticException, so probably okay
                typeof(PlatformNotSupportedException),
                typeof(RankException),
                typeof(System.ComponentModel.Win32Exception), // Added for .NET 7 (not sure why, this is an old exception)
                typeof(System.Reflection.CustomAttributeFormatException), // Maybe like AnnotationTypeMismatchException in Java...?
                typeof(System.Resources.MissingSatelliteAssemblyException),
                //typeof(System.Runtime.CompilerServices.SwitchExpressionException), // .NET Standard 2.1+ only (conditionally added below)
                typeof(System.Runtime.InteropServices.COMException),
                typeof(System.Runtime.InteropServices.ExternalException),
                typeof(System.Runtime.InteropServices.InvalidComObjectException),
                typeof(System.Runtime.InteropServices.InvalidOleVariantTypeException),
                typeof(System.Runtime.InteropServices.MarshalDirectiveException),
                typeof(System.Runtime.InteropServices.SafeArrayRankMismatchException),
                typeof(System.Runtime.InteropServices.SafeArrayTypeMismatchException),
                typeof(System.Runtime.InteropServices.SEHException),
                typeof(System.Runtime.Serialization.SerializationException),
                typeof(System.Security.Cryptography.CryptographicException),
                typeof(System.Security.VerificationException),
                typeof(System.Text.DecoderFallbackException), // LUCENENET TODO: Need to be sure about this one
                typeof(System.Text.EncoderFallbackException), // LUCENENET TODO: Need to be sure about this one
                typeof(System.Threading.AbandonedMutexException),
                typeof(System.Threading.SemaphoreFullException),
                typeof(System.Threading.SynchronizationLockException),
                typeof(System.Threading.Tasks.TaskCanceledException),
                typeof(System.Threading.ThreadAbortException),
                typeof(System.Threading.ThreadStartException),
                typeof(System.Threading.ThreadStateException),
                typeof(System.TimeoutException),
                typeof(System.TypeAccessException),
                typeof(System.TypeInitializationException),
                typeof(System.TypeLoadException),
                typeof(System.TypeUnloadedException),
            };

            // .NET Core 3.0 + only
            if (!(SwitchExpressionExceptionType is null))
            {
                result.Add(SwitchExpressionExceptionType);
            }

            // .NET Framework only
            if (!(RemotingExceptionType is null))
            {
                result.Add(RemotingExceptionType);
            }
            if (!(RemotingTimeoutExceptionType is null))
            {
                result.Add(RemotingTimeoutExceptionType);
            }
            if (!(RemotingServerExceptionType is null))
            {
                result.Add(RemotingServerExceptionType);
            }
            if (!(CryptographicUnexpectedOperationExceptionType is null))
            {
                result.Add(CryptographicUnexpectedOperationExceptionType);
            }
            if (!(HostProtectionExceptionType is null))
            {
                result.Add(HostProtectionExceptionType);
            }
            if (!(PolicyExceptionType is null))
            {
                result.Add(PolicyExceptionType);
            }
            if (!(IdentityNotMappedExceptionType is null))
            {
                result.Add(IdentityNotMappedExceptionType);
            }
            if (!(XmlSyntaxExceptionType is null))
            {
                result.Add(XmlSyntaxExceptionType);
            }

            return result;
        }

        #endregion Known types of exception families

        #region Special case constructors

        public static readonly IDictionary<Type, Func<Type, string, object>> NonStandardExceptionConstructors = LoadNonStandardExceptionConstructors();

        private static IDictionary<Type, Func<Type, string, object>> LoadNonStandardExceptionConstructors()
        {
            var result = new Dictionary<Type, Func<Type, string, object>>
            {
                [typeof(NUnit.Framework.MultipleAssertException)] = (exceptionType, message) =>
                {
                    //public class MultipleAssertException : ResultStateException
                    //{
                    //    public MultipleAssertException(ITestResult testResult)
                    return Activator.CreateInstance(
                            typeof(NUnit.Framework.MultipleAssertException),
                            new object[] { new NUnitExceptionMessage(message) }); // NUnitExcpetionMessage implements NUnit.Framework.Interfaces.ITestResult
                },
                [typeof(ReflectionTypeLoadException)] = (exceptionType, message) =>
                {
                    //public sealed class ReflectionTypeLoadException : SystemException
                    //{
                    //    public ReflectionTypeLoadException (Type?[]? classes, Exception?[]? exceptions)
                    Type[] types = new Type[] { typeof(Exception) };
                    Exception[] exceptions = new Exception[] { new Exception() };
                    return Activator.CreateInstance(
                        typeof(ReflectionTypeLoadException),
                        new object[] { types, exceptions });
                },
                [typeof(TargetInvocationException)] = (exceptionType, message) =>
                {
                    //public sealed class TargetInvocationException : ApplicationException
                    //{
                    //    public TargetInvocationException (string? message, Exception? inner)
                    return Activator.CreateInstance(
                        typeof(TargetInvocationException),
                        new object[] { message, new Exception() });
                },
            };

            // Special case - this doesn't exist on .NET Framework, so we only add it if not null
            if (!(DebugAssertExceptionType is null))
            {
                result[DebugAssertExceptionType] = (exceptionType, message) =>
                {
                    //private sealed class DebugAssertException : Exception
                    //{
                    //    internal DebugAssertException(string? message, string? detailMessage, string? stackTrace)
                    BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                    return Activator.CreateInstance(DebugAssertExceptionType, flags, null, new object[] { message, null, null }, CultureInfo.InvariantCulture);
                };
            }

            if (!(MetadataExceptionType is null))
            {
                result[MetadataExceptionType] = (exceptionType, message) =>
                {
                    //private sealed class MetadataException : Exception // NOTE: Couldn't find in the source, but this part doesn't matter so much
                    //{
                    //    internal MetadataException(int value)
                    BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                    return Activator.CreateInstance(MetadataExceptionType, flags, null, new object[] { (int)0 }, CultureInfo.InvariantCulture);
                };
            }

            if (!(CrossAppDomainMarshaledExceptionType is null))
            {
                result[CrossAppDomainMarshaledExceptionType] = (exceptionType, message) =>
                {
                    //private sealed class CrossAppDomainMarshaledException : Exception // NOTE: Couldn't find in the source, but this part doesn't matter so much
                    //{
                    //    internal CrossAppDomainMarshaledException(string message, int value)
                    BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
                    return Activator.CreateInstance(CrossAppDomainMarshaledExceptionType, flags, null, new object[] { message, (int)0 }, CultureInfo.InvariantCulture);
                };
            }

            return result;
        }

        private class NUnitExceptionMessage : ITestResult
        {
            private readonly string message;

            public NUnitExceptionMessage(string message)
            {
                this.message = message ?? throw new ArgumentException(nameof(message));
            }

            public string Message => message;

            #region Unneeded Members
            public ResultState ResultState => throw new NotImplementedException();

            public string Name => throw new NotImplementedException();

            public string FullName => throw new NotImplementedException();

            public double Duration => throw new NotImplementedException();

            public DateTime StartTime => throw new NotImplementedException();

            public DateTime EndTime => throw new NotImplementedException();

            public string StackTrace => throw new NotImplementedException();

            public int TotalCount => throw new NotImplementedException();

            public int AssertCount => throw new NotImplementedException();

            public int FailCount => throw new NotImplementedException();

            public int WarningCount => throw new NotImplementedException();

            public int PassCount => throw new NotImplementedException();

            public int SkipCount => throw new NotImplementedException();

            public int InconclusiveCount => throw new NotImplementedException();

            public bool HasChildren => throw new NotImplementedException();

            public IEnumerable<ITestResult> Children => throw new NotImplementedException();

            public ITest Test => throw new NotImplementedException();

            public string Output => throw new NotImplementedException();

            public IList<AssertionResult> AssertionResults => throw new NotImplementedException();

            public ICollection<TestAttachment> TestAttachments => throw new NotImplementedException();

            public TNode AddToXml(TNode parentNode, bool recursive) => throw new NotImplementedException();
            public TNode ToXml(bool recursive) => throw new NotImplementedException();

            #endregion Unneeded Members
        }

        #endregion Special case constructors


        protected static Exception TryInstantiate(Type exceptionType)
        {
            object exception = null;
            if (NonStandardExceptionConstructors.TryGetValue(exceptionType, out Func<Type, string, object> constructionFactory))
            {
                try
                {
                    exception = constructionFactory(exceptionType, $"Throwing a {exceptionType.Name}.");
                }
                catch (MissingMethodException ex)
                {
                    Assert.Fail($"Can't instantiate type {exceptionType.Name}, it's missing the necessary constructors.:\n\n{ex}");
                }
            }
            else
            {
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
                try
                {
                    exception = Activator.CreateInstance(exceptionType, flags, null, new object[] { $"Throwing a {exceptionType.Name}." }, CultureInfo.InvariantCulture);
                }
                catch (MissingMethodException)
                {
                    try
                    {
                        exception = Activator.CreateInstance(exceptionType, flags, null, null, CultureInfo.InvariantCulture);
                    }
                    catch (MissingMethodException ex)
                    {
                        Assert.Fail($"Can't instantiate type {exceptionType.Name}, it's missing the necessary constructors.:\n\n{ex}");
                    }
                }
            }
            return (Exception)exception;
        }
    }
}