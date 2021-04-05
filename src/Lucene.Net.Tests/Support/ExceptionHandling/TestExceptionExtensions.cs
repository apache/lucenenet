using J2N.Text;
using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

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

#pragma warning disable IDE0001 // Name can be simplified
    [LuceneNetSpecific]
    public class TestExceptionExtensions : LuceneTestCase
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


        // Load exception types from all assemblies
        private static readonly Assembly[] LuceneAssemblies = new Assembly[]
        {
            typeof(Lucene.Net.Analysis.Analyzer).Assembly,                         // Lucene.Net
            typeof(Lucene.Net.Analysis.Standard.ClassicAnalyzer).Assembly,         // Lucene.Net.Analysis.Common
            typeof(Lucene.Net.Analysis.Ja.GraphvizFormatter).Assembly,             // Lucene.Net.Analysis.Kuromoji
            typeof(Lucene.Net.Analysis.Morfologik.MorfologikAnalyzer).Assembly,    // Lucene.Net.Analysis.Morfologik
#if FEATURE_OPENNLP
            typeof(Lucene.Net.Analysis.OpenNlp.OpenNLPTokenizer).Assembly,         // Lucene.Net.Analysis.OpenNlp
#endif
            typeof(Lucene.Net.Analysis.Phonetic.BeiderMorseFilter).Assembly,       // Lucene.Net.Analysis.Phonetic
            typeof(Lucene.Net.Analysis.Cn.Smart.AnalyzerProfile).Assembly,         // Lucene.Net.Analysis.SmartCn
            typeof(Lucene.Net.Analysis.Stempel.StempelFilter).Assembly,            // Lucene.Net.Analysis.Stempel
            typeof(Lucene.Net.Benchmarks.Constants).Assembly,                      // Lucene.Net.Benchmark
            typeof(Lucene.Net.Classification.KNearestNeighborClassifier).Assembly, // Lucene.Net.Classification
            typeof(Lucene.Net.Codecs.BlockTerms.BlockTermsReader).Assembly,        // Lucene.Net.Codecs
            typeof(Lucene.Net.Expressions.Bindings).Assembly,                      // Lucene.Net.Expressions
            typeof(Lucene.Net.Facet.Facets).Assembly,                              // Lucene.Net.Facet
            typeof(Lucene.Net.Search.Grouping.ICollectedSearchGroup).Assembly,     // Lucene.Net.Grouping
            typeof(Lucene.Net.Search.Highlight.DefaultEncoder).Assembly,           // Lucene.Net.Highlighter
            typeof(Lucene.Net.Join.JoinUtil).Assembly,                             // Lucene.Net.Join
            typeof(Lucene.Net.Index.Memory.MemoryIndex).Assembly,                  // Lucene.Net.Memory
            typeof(Lucene.Net.Misc.SweetSpotSimilarity).Assembly,                  // Lucene.Net.Misc
            typeof(Lucene.Net.Queries.BooleanFilter).Assembly,                     // Lucene.Net.Queries
            typeof(Lucene.Net.QueryParsers.Classic.QueryParser).Assembly,          // Lucene.Net.QueryParser
            typeof(Lucene.Net.Replicator.IReplicator).Assembly,                    // Lucene.Net.Replicator
            typeof(Lucene.Net.Sandbox.Queries.DuplicateFilter).Assembly,           // Lucene.Net.Sandbox
            typeof(Lucene.Net.Spatial.DisjointSpatialFilter).Assembly,             // Lucene.Net.Spatial
            typeof(Lucene.Net.Util.LuceneTestCase).Assembly,                       // Lucene.Net.TestFramework
        };


        private static readonly Assembly[] DotNetAssemblies = new Assembly[]
        {
            typeof(Exception).Assembly
        };

        private static readonly Assembly[] NUnitAssemblies = new Assembly[]
        {
            typeof(NUnit.Framework.AssertionException).Assembly
        };

        // Base class Exception
        private static readonly ICollection<Type> DotNetExceptionTypes = LoadTypesSubclassing(baseClass: typeof(Exception), DotNetAssemblies);
        private static readonly ICollection<Type> NUnitExceptionTypes = LoadTypesSubclassing(baseClass: typeof(Exception), NUnitAssemblies);
        private static readonly ICollection<Type> LuceneExceptionTypes = LoadTypesSubclassing(baseClass: typeof(Exception), LuceneAssemblies);

        private static readonly ICollection<Type> AllExceptionTypes = DotNetExceptionTypes.Union(NUnitExceptionTypes).Union(LuceneExceptionTypes).ToList();

        // Base class IOException
        private static readonly ICollection<Type> DotNetIOExceptionTypes = LoadTypesSubclassing(baseClass: typeof(IOException), DotNetAssemblies);
        private static readonly ICollection<Type> NUnitIOExceptionTypes = LoadTypesSubclassing(baseClass: typeof(IOException), NUnitAssemblies);
        private static readonly ICollection<Type> LuceneIOExceptionTypes = LoadTypesSubclassing(baseClass: typeof(IOException), LuceneAssemblies);

        private static readonly ICollection<Type> AllIOExceptionTypes = DotNetIOExceptionTypes.Union(NUnitIOExceptionTypes).Union(LuceneIOExceptionTypes).ToList();

        #region Known types of exception families

        private static readonly IEnumerable<Type> KnownAssertionErrorTypes = LoadKnownAssertionErrorTypes();

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

        private static readonly IEnumerable<Type> KnownErrorExceptionTypes = LoadKnownErrorExceptionTypes();

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

                typeof(Lucene.Net.QueryParsers.Classic.TokenMgrError),
                typeof(Lucene.Net.QueryParsers.Flexible.Core.QueryNodeError),
                typeof(Lucene.Net.QueryParsers.Flexible.Standard.Parser.TokenMgrError),
                typeof(Lucene.Net.QueryParsers.Surround.Parser.TokenMgrError),
            };
        }

        private static readonly IEnumerable<Type> KnownIOExceptionTypes = new Type[] {
            typeof(UnauthorizedAccessException),
            typeof(ObjectDisposedException),
        }.Union(AllIOExceptionTypes);

        private static readonly IEnumerable<Type> KnownIndexOutOfBoundsExceptionTypes = new Type[] {
            typeof(ArgumentOutOfRangeException),
            typeof(IndexOutOfRangeException),

            // Types for use as Java Aliases in .NET
            typeof(ArrayIndexOutOfBoundsException),
            typeof(StringIndexOutOfBoundsException),
            typeof(IndexOutOfBoundsException),
        };

        private static readonly IEnumerable<Type> KnownNullPointerExceptionTypes = new Type[] {
            typeof(ArgumentNullException),
            typeof(NullReferenceException),

            // Types for use as Java Aliases in .NET
            typeof(NullPointerException),
        };

        private static readonly IEnumerable<Type> KnownIllegalArgumentExceptionTypes = new Type[] {
            typeof(ArgumentException),
            typeof(ArgumentNullException),
            typeof(ArgumentOutOfRangeException),

            // Types for use as Java Aliases in .NET
            typeof(IllegalArgumentException),
        };

        private static readonly IEnumerable<Type> KnownIllegalArgumentExceptionTypes_TestEnvironment = new Type[] {
            typeof(ArgumentException),

            // Types for use as Java Aliases in .NET
            typeof(IllegalArgumentException),
        };

        #endregion Known types of exception families

        #region Special case constructors

        private static readonly IDictionary<Type, Func<Type, string, object>> NonStandardExceptionConstructors = LoadNonStandardExceptionConstructors();

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

        public class NUnitExceptionMessage : NUnit.Framework.Interfaces.ITestResult
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


        private static ICollection<Type> LoadTypesSubclassing(Type baseClass, params Assembly[] assemblies)
        {
            if (baseClass is null)
                throw new ArgumentNullException(nameof(baseClass));
            if (assemblies is null)
                throw new ArgumentNullException(nameof(assemblies));

            var result = new JCG.SortedSet<Type>(Comparer<Type>.Create((left, right) => left.Name.CompareToOrdinal(right.Name)));
            foreach (var assembly in assemblies)
            {
                result.UnionWith(assembly.GetTypes().Where(exceptionType => baseClass.IsAssignableFrom(exceptionType)));
            }
            return result;
        }

        public static IEnumerable<TestCaseData> ThrowableTypeExpressions
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                      // exception type (to make NUnit display them all)
                        true,                                               // expectedToThrow
                        new Action(() => ThrowException(exceptionType)));   // throw exception expression
                }
            }
        }


        public static IEnumerable<TestCaseData> ErrorTypeExpressions
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                      // exception type (to make NUnit display them all)
                        !KnownErrorExceptionTypes.Contains(exceptionType),  // expectedToThrow
                        new Action(() => ThrowException(exceptionType)));   // throw exception expression
                }
            }
        }

        public static IEnumerable<TestCaseData> ExceptionTypeExpressions
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                      // exception type (to make NUnit display them all)
                        KnownErrorExceptionTypes.Contains(exceptionType),   // expectedToThrow
                        new Action(() => ThrowException(exceptionType)));   // throw exception expression
                }
            } 
        }

        public static IEnumerable<TestCaseData> IOExceptionTypeExpressions
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                      // exception type (to make NUnit display them all)
                        !KnownIOExceptionTypes.Contains(exceptionType),     // expectedToThrow
                        new Action(() => ThrowException(exceptionType)));   // throw exception expression
                }
            }
        }

        public static IEnumerable<TestCaseData> AssertionErrorTypeExpressions
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                         // exception type (to make NUnit display them all)
                        !KnownAssertionErrorTypes.Contains(exceptionType),     // expectedToThrow
                        new Action(() => ThrowException(exceptionType)));      // throw exception expression
                }
            }
        }

        public static IEnumerable<TestCaseData> IndexOutOfBoundsExceptionTypeExpressions
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                                    // exception type (to make NUnit display them all)
                        !KnownIndexOutOfBoundsExceptionTypes.Contains(exceptionType),     // expectedToThrow
                        new Action(() => ThrowException(exceptionType)));                 // throw exception expression
                }
            }
        }

        public static IEnumerable<TestCaseData> NullPointerExceptionTypeExpressions
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                                    // exception type (to make NUnit display them all)
                        !KnownNullPointerExceptionTypes.Contains(exceptionType),          // expectedToThrow
                        new Action(() => ThrowException(exceptionType)));                 // throw exception expression
                }
            }
        }

        public static IEnumerable<TestCaseData> IllegalArgumentExceptionTypeExpressions
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                                    // exception type (to make NUnit display them all)
                        !KnownIllegalArgumentExceptionTypes.Contains(exceptionType),      // expectedToThrow
                        new Action(() => ThrowException(exceptionType)));                 // throw exception expression
                }
            }
        }

        public static IEnumerable<TestCaseData> IllegalArgumentExceptionTypeExpressions_TestEnvironment
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                                               // exception type (to make NUnit display them all)
                        !KnownIllegalArgumentExceptionTypes_TestEnvironment.Contains(exceptionType), // expectedToThrow
                        new Action(() => ThrowException(exceptionType)));                            // throw exception expression
                }
            }
        }

        private static void ThrowException(Type exceptionType)
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
            throw (Exception)exception;
        }

        [Test]
        [TestCaseSource("ThrowableTypeExpressions")]
        public void TestIsThrowable(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            static bool extensionMethod(Exception e) => e.IsThrowable();

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        [Test]
        [TestCaseSource("ErrorTypeExpressions")]
        public void TestIsError(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            static bool extensionMethod(Exception e) => e.IsError();

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        // This test ensures that all known Error types from Java are not caught by
        // our IsException() handler.
        [Test]
        [TestCaseSource("ExceptionTypeExpressions")]
        public void TestIsException(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            static bool extensionMethod(Exception e) => e.IsException();

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        [Test]
        [TestCaseSource("IOExceptionTypeExpressions")]
        public void TestIsIOException(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            static bool extensionMethod(Exception e) => e.IsIOException();

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        // This test ensures that Lucene.NET's AssertionException, the .NET platform's DebugAssertException, and
        // NUnit's AssertionException and MultipleAssertException types are all treated as if they were AssertionError
        // in Java.
        [Test]
        [TestCaseSource("AssertionErrorTypeExpressions")]
        public void TestIsAssertionError(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            static bool extensionMethod(Exception e) => e.IsAssertionError();

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        // This test ensures that ArgumentOutOfRangeException and IndexOutOfRangeException are both caught by our
        // IndexOutOfBoundsException handler, because they both correspond to IndexOutOfBoundsException in Java.
        // Java has 2 other types ArrayIndexOutOfBoundsException and StringIndexOutOfBoundsException, whose alias
        // exception types are also part of the test.
        [Test]
        [TestCaseSource("IndexOutOfBoundsExceptionTypeExpressions")]
        public void TestIsIndexOutOfBoundsException(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            static bool extensionMethod(Exception e) => e.IsIndexOutOfBoundsException();

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        // This test ensures that ArgumentNullException and NullReferenceException are both caught by our
        // NullPointerException handler, because they both correspond to NullPointerException in Java
        [Test]
        [TestCaseSource("NullPointerExceptionTypeExpressions")]
        public void TestIsNullPointerException(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            static bool extensionMethod(Exception e) => e.IsNullPointerException();

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        // This test ensures that any known ArgumentException will be caught.
        // We do it this way in production to ensure that if we "upgrade" to a .NET
        // ArgumentNullException or ArgumentOutOfRangeException it won't break the code.
        [Test]
        [TestCaseSource("IllegalArgumentExceptionTypeExpressions")]
        public void TestIsIllegalArgumentException(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            // Make sure we are testing the production code
            static bool extensionMethod(Exception e) => Lucene.ExceptionExtensions.IsIllegalArgumentException(e);

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        // This test ensures that ArgumentNullException and ArgumentOutOfRangeException are not caught by our
        // IllegalArgumentException handler in tests, because they wouldn't be in Java. We do this differently
        // in the test environment to ensure that if a test is specified wrong it will fail and should be updated
        // and commented to indicate we diverged from Lucene.
        [Test]
        [TestCaseSource("IllegalArgumentExceptionTypeExpressions_TestEnvironment")]
        public void TestIsIllegalArgumentException_TestEnvironment(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            // Make sure we are testing the test environment code
            static bool extensionMethod(Exception e) => Lucene.Net.ExceptionExtensions.IsIllegalArgumentException(e);

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        private void AssertCatches(Action action, Func<Exception, bool> extensionMethodExpression)
        {
            try
            {
                try
                {
                    action();
                }
                catch (Exception e) when (extensionMethodExpression(e))
                {
                    // expected
                    Assert.Pass($"Expected: Caught exception {e.GetType().FullName}");
                }
            }
            catch (Exception e) when (!(e is NUnit.Framework.SuccessException))
            {
                // not expected
                Assert.Fail($"Exception thrown when expected to be caught: {e.GetType().FullName}");
            }
        }

        private void AssertDoesNotCatch(Action action, Func<Exception, bool> extensionMethodExpression)
        {
            try
            {
                try
                {
                    action();
                }
                catch (Exception e) when (extensionMethodExpression(e))
                {
                    // not expected
                    Assert.Fail($"Exception caught when expected to be thrown: {e.GetType().FullName}");
                }
            }
            catch (Exception e)
            {
                // expected
                Assert.Pass($"Expected: Did not catch exception {e.GetType().FullName}");
            }
        }
    }
}
