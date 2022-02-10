using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lucene.Net.Util
{
    #region Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License.

    // Copyright (c) 2021 Charlie Poole, Rob Prouse
    // 
    // Permission is hereby granted, free of charge, to any person obtaining a copy
    // of this software and associated documentation files (the "Software"), to deal
    // in the Software without restriction, including without limitation the rights
    // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    // copies of the Software, and to permit persons to whom the Software is
    // furnished to do so, subject to the following conditions:
    // 
    // The above copyright notice and this permission notice shall be included in
    // all copies or substantial portions of the Software.
    // 
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    // THE SOFTWARE.

    #endregion

    /// <summary>
    /// NUnitTestFixtureBuilder is able to build a fixture given
    /// a class marked with a TestFixtureAttribute or an unmarked
    /// class containing test methods. In the first case, it is
    /// called by the attribute and in the second directly by
    /// NUnitSuiteBuilder.
    /// </summary>
    internal class NUnitTestFixtureBuilder
    {
        const int SETUP_FIXTURE_SEED_OFFSET = 7;
        const int TEST_FIXTURE_SEED_OFFSET = 3;

        private static RandomizedContext setUpFixtureRandomizedContext = null;

        #region Messages

        const string NO_TYPE_ARGS_MSG =
            "Fixture type contains generic parameters. You must either provide Type arguments or specify constructor arguments that allow NUnit to deduce the Type arguments.";

        const string PARALLEL_NOT_ALLOWED_MSG =
            "ParallelizableAttribute is only allowed on test methods and fixtures";

        #endregion

        #region Instance Fields

        private readonly ITestCaseBuilder _testBuilder = new DefaultTestCaseBuilder();
        private readonly LuceneSetUpFixtureBuilder _setUpFixtureBuilder = new LuceneSetUpFixtureBuilder();
        private readonly LuceneRandomSeedInitializer _randomSeedInitializer = new LuceneRandomSeedInitializer();

        #endregion

        #region Public Methods

        /// <summary>
        /// Build a TestFixture from type provided. A non-null TestSuite
        /// must always be returned, since the method is generally called
        /// because the user has marked the target class as a fixture.
        /// If something prevents the fixture from being used, it should
        /// be returned nonetheless, labelled as non-runnable.
        /// </summary>
        /// <param name="typeInfo">An ITypeInfo for the fixture to be used.</param>
        /// <param name="filter">Filter used to select methods as tests.</param>
        /// <returns>A TestSuite object or one derived from TestSuite.</returns>
        // TODO: This should really return a TestFixture, but that requires changes to the Test hierarchy.
        public TestSuite BuildFrom(ITypeInfo typeInfo, IPreFilter filter)
        {
            // Build our custom SetUpFixture to get the NUnit runner to initialize us
            // even though we don't own the test assembly.
            var setUpFixture = _setUpFixtureBuilder.BuildFrom(typeInfo);
            var fixture = new TestFixture(typeInfo);

            SetUpRandomizedContext(setUpFixture, fixture);

            if (fixture.RunState != RunState.NotRunnable)
                CheckTestFixtureIsValid(fixture);

            fixture.ApplyAttributesToTest(typeInfo.Type.GetTypeInfo());

            AddTestCasesToFixture(fixture, filter);

            setUpFixture.Add(fixture);
            return setUpFixture;
        }

        /// <summary>
        /// Overload of BuildFrom called by tests that have arguments.
        /// Builds a fixture using the provided type and information
        /// in the ITestFixtureData object.
        /// </summary>
        /// <param name="typeInfo">The TypeInfo for which to construct a fixture.</param>
        /// <param name="filter">Filter used to select methods as tests.</param>
        /// <param name="testFixtureData">An object implementing ITestFixtureData or null.</param>
        /// <returns></returns>
        public TestSuite BuildFrom(ITypeInfo typeInfo, IPreFilter filter, ITestFixtureData testFixtureData)
        {
            //Guard.ArgumentNotNull(testFixtureData, nameof(testFixtureData));
            if (testFixtureData is null)
                throw new ArgumentNullException(nameof(testFixtureData));

            object[] arguments = testFixtureData.Arguments;

            if (typeInfo.ContainsGenericParameters)
            {
                Type[] typeArgs = testFixtureData.TypeArgs;
                if (typeArgs is null || typeArgs.Length == 0)
                {
                    int cnt = 0;
                    foreach (object o in arguments)
                        if (o is Type) cnt++;
                        else break;

                    typeArgs = new Type[cnt];
                    for (int i = 0; i < cnt; i++)
                        typeArgs[i] = (Type)arguments[i];

                    if (cnt > 0)
                    {
                        object[] args = new object[arguments.Length - cnt];
                        for (int i = 0; i < args.Length; i++)
                            args[i] = arguments[cnt + i];

                        arguments = args;
                    }
                }

                if (typeArgs.Length > 0 ||
                    TypeHelper.CanDeduceTypeArgsFromArgs(typeInfo.Type, arguments, ref typeArgs))
                {
                    typeInfo = typeInfo.MakeGenericType(typeArgs);
                }
            }

            // Build our custom SetUpFixture to get the NUnit runner to initialize us
            // even though we don't own the test assembly.
            var setUpFixture = _setUpFixtureBuilder.BuildFrom(typeInfo);
            var fixture = new TestFixture(typeInfo, arguments);

            SetUpRandomizedContext(setUpFixture, fixture);

            string name = fixture.Name;

            if (testFixtureData.TestName != null)
            {
                fixture.Name = testFixtureData.TestName;
            }
            else
            {
                //var argDisplayNames = (testFixtureData as NUnit.Framework.Internal.TestParameters)?.ArgDisplayNames;
                var testParameters = testFixtureData as NUnit.Framework.Internal.TestParameters;
                string[] argDisplayNames = null;
                if (testParameters != null)
                {
                    // Hack so we can call the same internal field that NUnit does
                    BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                    FieldInfo field = typeof(NUnit.Framework.Internal.TestParameters).GetField("_argDisplayNames", bindFlags);
                    argDisplayNames = (string[])field.GetValue(testFixtureData);
                }
                if (argDisplayNames != null)
                {
                    fixture.Name = typeInfo.GetDisplayName();
                    if (argDisplayNames.Length != 0)
                        fixture.Name += '(' + string.Join(", ", argDisplayNames) + ')';
                }
                else if (arguments != null && arguments.Length > 0)
                {
                    fixture.Name = typeInfo.GetDisplayName(arguments);
                }
            }

            if (fixture.Name != name) // name was changed 
            {
                string nspace = typeInfo.Namespace;
                fixture.FullName = nspace != null && nspace != ""
                    ? nspace + "." + fixture.Name
                    : fixture.Name;
            }

            if (fixture.RunState != RunState.NotRunnable)
                fixture.RunState = testFixtureData.RunState;

            foreach (string key in testFixtureData.Properties.Keys)
                foreach (object val in testFixtureData.Properties[key])
                    fixture.Properties.Add(key, val);

            if (fixture.RunState != RunState.NotRunnable)
                CheckTestFixtureIsValid(fixture);

            fixture.ApplyAttributesToTest(typeInfo.Type.GetTypeInfo());

            AddTestCasesToFixture(fixture, filter);

            setUpFixture.Add(fixture);
            return setUpFixture;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Sets up the randomized context for both the set up fixture and the test fixture.
        /// We use the same instance for every set up fixture in the assembly, but each test
        /// fixture has its own distinct randomized context instance.
        /// </summary>
        /// <param name="setUpFixture">The setup fixture.</param>
        /// <param name="testFixture">The test fixture.</param>
        private void SetUpRandomizedContext(Test setUpFixture, Test testFixture)
        {
            // Setup the factories so we can read the random seed from the system properties
            LuceneTestCase.SetUpFixture.EnsureInitialized(setUpFixture, testFixture);

            // Reuse the same randomized context for each setup fixture instance, since these all need to report
            // the same seed. Note that setUpFixtureRandomizedContext is static, so we do this once per assembly.
            if (setUpFixtureRandomizedContext is null)
                setUpFixtureRandomizedContext = _randomSeedInitializer.InitializeTestFixture(setUpFixture, testFixture.TypeInfo.Assembly, SETUP_FIXTURE_SEED_OFFSET);
            else
                _randomSeedInitializer.InitializeTestFixture(setUpFixture, setUpFixtureRandomizedContext);

            _randomSeedInitializer.InitializeTestFixture(testFixture, testFixture.TypeInfo.Assembly, TEST_FIXTURE_SEED_OFFSET);
        }

        /// <summary>
        /// Method to add test cases to the newly constructed fixture.
        /// </summary>
        private void AddTestCasesToFixture(TestFixture fixture, IPreFilter filter)
        {
            // TODO: Check this logic added from Neil's build.
            if (fixture.TypeInfo.ContainsGenericParameters)
            {
                fixture.MakeInvalid(NO_TYPE_ARGS_MSG);
                return;
            }

            // We sort the methods in a deterministic order, since BuildTestCase() will invoke the
            // Randomizer to create seeds for each test. We want those seeds to be deterministically repeatable
            // so we can re-run the same conditions by manually fixing the Randomizer.InitialSeed.
            var methods = new SortedSet<IMethodInfo>(fixture.TypeInfo.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static),
                MethodInfoComparer.Default);

            foreach (IMethodInfo method in methods)
            {
                // Generate the seed whether or not we use a filter to ensure we invoke the Randomizer to
                // move to the next random test seed (a test should always get the same seed in the sequence).
                Test test = BuildTestCase(method, fixture);

                _randomSeedInitializer.GenerateRandomSeeds(test);

                if (filter.IsMatch(fixture.TypeInfo.Type, method.MethodInfo))
                {
                    if (test != null)
                        fixture.Add(test);
                    else // it's not a test, check for disallowed attributes
                        if (method.MethodInfo.HasAttribute<ParallelizableAttribute>(false))
                        fixture.MakeInvalid(PARALLEL_NOT_ALLOWED_MSG);
                }
            }
        }

        /// <summary>
        /// Method to create a test case from a MethodInfo and add
        /// it to the fixture being built. It first checks to see if
        /// any global TestCaseBuilder addin wants to build the
        /// test case. If not, it uses the internal builder
        /// collection maintained by this fixture builder.
        ///
        /// The default implementation has no test case builders.
        /// Derived classes should add builders to the collection
        /// in their constructor.
        /// </summary>
        /// <param name="method">The method for which a test is to be created</param>
        /// <param name="suite">The test suite being built.</param>
        /// <returns>A newly constructed Test</returns>
        private Test BuildTestCase(IMethodInfo method, TestSuite suite)
        {
            return _testBuilder.CanBuildFrom(method, suite)
                ? _testBuilder.BuildFrom(method, suite)
                : null;
        }

        private static void CheckTestFixtureIsValid(TestFixture fixture)
        {
            if (fixture.TypeInfo.ContainsGenericParameters)
            {
                fixture.MakeInvalid(NO_TYPE_ARGS_MSG);
            }
            else if (!fixture.TypeInfo.IsStaticClass)
            {
                Type[] argTypes = /*Reflect.*/GetTypeArray(fixture.Arguments);

                if (!/*Reflect.*/GetConstructors(fixture.TypeInfo.Type, argTypes).Any())
                {
                    fixture.MakeInvalid("No suitable constructor was found");
                }
            }
        }

        /// <summary>
        /// Returns an array of types from an array of objects.
        /// Differs from <see cref="M:System.Type.GetTypeArray(System.Object[])"/> by returning <see langword="null"/>
        /// for null elements rather than throwing <see cref="ArgumentNullException"/>.
        /// </summary>
        internal static Type[] GetTypeArray(object[] objects)
        {
            Type[] types = new Type[objects.Length];
            int index = 0;
            foreach (object o in objects)
            {
                types[index++] = o?.GetType();
            }
            return types;
        }

        /// <summary>
        /// Gets the constructors to which the specified argument types can be coerced.
        /// </summary>
        internal static IEnumerable<ConstructorInfo> GetConstructors(Type type, Type[] matchingTypes)
        {
            return type
                .GetConstructors()
                .Where(c => c.GetParameters().ParametersMatch(matchingTypes));
        }

        #endregion
    }

    internal class MethodInfoComparer : IComparer<IMethodInfo>
    {
        private MethodInfoComparer() { } // LUCENENT: Made into singleton

        public static IComparer<IMethodInfo> Default { get; } = new MethodInfoComparer();

        public int Compare(IMethodInfo x, IMethodInfo y)
        {
            StringComparer stringComparer = StringComparer.Ordinal;

            int nameCompare = stringComparer.Compare(x.Name, y.Name);
            if (nameCompare != 0)
                return nameCompare;

            var xParameters = x.GetParameters();
            var yParameters = y.GetParameters();

            if (xParameters.Length > yParameters.Length)
                return 1;
            if (xParameters.Length < yParameters.Length)
                return -1;

            for (int i = 0; i < xParameters.Length; i++)
            {
                var px = xParameters[i];
                var py = xParameters[i];

                int parameterTypeCompare = stringComparer.Compare(px.ParameterType.FullName, py.ParameterType.FullName);
                if (parameterTypeCompare != 0)
                    return parameterTypeCompare;
            }

            return 0;
        }
    }

    internal static class Extensions
    {
        // From NUnit's Reflect class

        /// <summary>
        /// Determines if the given types can be coerced to match the given parameters.
        /// </summary>
        internal static bool ParametersMatch(this ParameterInfo[] pinfos, Type[] ptypes)
        {
            if (pinfos.Length != ptypes.Length)
                return false;

            for (int i = 0; i < pinfos.Length; i++)
            {
                if (!ptypes[i].CanImplicitlyConvertTo(pinfos[i].ParameterType))
                    return false;
            }
            return true;
        }

        // §6.1.2 (Implicit numeric conversions) of the specification
        private static readonly Dictionary<Type, List<Type>> convertibleValueTypes = new Dictionary<Type, List<Type>>() {
            { typeof(decimal), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char) } },
            { typeof(double), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char), typeof(float) } },
            { typeof(float), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char), typeof(float) } },
            { typeof(ulong), new List<Type> { typeof(byte), typeof(ushort), typeof(uint), typeof(char) } },
            { typeof(long), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(char) } },
            { typeof(uint), new List<Type> { typeof(byte), typeof(ushort), typeof(char) } },
            { typeof(int), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(char) } },
            { typeof(ushort), new List<Type> { typeof(byte), typeof(char) } },
            { typeof(short), new List<Type> { typeof(byte) } }
        };

        /// <summary>
        /// Determines whether the current type can be implicitly converted to the specified type.
        /// </summary>
        internal static bool CanImplicitlyConvertTo(this Type from, Type to)
        {
            if (to.IsAssignableFrom(from))
                return true;

            // Look for the marker that indicates from was null
            if (from is null && (to.GetTypeInfo().IsClass || to.FullName.StartsWith("System.Nullable")))
                return true;

            if (convertibleValueTypes.ContainsKey(to) && convertibleValueTypes[to].Contains(from))
                return true;

            return from
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.ReturnType == to && m.Name == "op_Implicit");
        }

        internal static IEnumerable<Type> TypeAndBaseTypes(this Type type)
        {
            for (; type != null; type = type.GetTypeInfo().BaseType)
            {
                yield return type;
            }
        }


        // From NUnit's Extensions class

        public static bool IsStatic(this Type type)
        {
            return type.GetTypeInfo().IsAbstract && type.GetTypeInfo().IsSealed;
        }

        public static bool HasAttribute<T>(this ICustomAttributeProvider attributeProvider, bool inherit)
        {
            return attributeProvider.IsDefined(typeof(T), inherit);
        }

        public static bool HasAttribute<T>(this Type type, bool inherit)
        {
            return ((ICustomAttributeProvider)type.GetTypeInfo()).HasAttribute<T>(inherit);
        }

        public static T[] GetAttributes<T>(this ICustomAttributeProvider attributeProvider, bool inherit) where T : class
        {
            return (T[])attributeProvider.GetCustomAttributes(typeof(T), inherit);
        }

        public static T[] GetAttributes<T>(this Assembly assembly) where T : class
        {
            return assembly.GetAttributes<T>(inherit: false);
        }

        public static T[] GetAttributes<T>(this Type type, bool inherit) where T : class
        {
            return ((ICustomAttributeProvider)type.GetTypeInfo()).GetAttributes<T>(inherit);
        }
    }

}
