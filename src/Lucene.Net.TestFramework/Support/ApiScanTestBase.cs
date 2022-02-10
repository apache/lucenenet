using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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

    /// <summary>
    /// LUCENENET specific - functionality for scanning the API to ensure
    /// naming and .NET conventions are followed consistently. Not for use
    /// by end users.
    /// </summary>
    public abstract class ApiScanTestBase : LuceneTestCase
    {
        internal ApiScanTestBase() { } // LUCENENET: Not for use by end users

        /// <summary>
        /// Private fields must be upper case separated with underscores, 
        /// must be camelCase (optionally may be prefixed with underscore, 
        /// but it is preferred not to use the underscore to match Lucene).
        /// </summary>
        private static readonly Regex PrivateFieldName = new Regex("^_?[a-z][a-zA-Z0-9_]*$|^[A-Z0-9_]+$", RegexOptions.Compiled);

        /// <summary>
        /// Protected fields must either be upper case separated with underscores or
        /// must be prefixed with m_ (to avoid naming conflicts with properties).
        /// </summary>
        private static readonly Regex ProtectedFieldName = new Regex("^m_[a-z][a-zA-Z0-9_]*$|^[A-Z0-9_]+$", RegexOptions.Compiled);

        /// <summary>
        /// Method parameters must be camelCase and not begin or end with underscore.
        /// </summary>
        private static readonly Regex MethodParameterName = new Regex("^[a-z](?:[a-zA-Z0-9_]*[a-zA-Z0-9])?$", RegexOptions.Compiled);

        /// <summary>
        /// Interfaces must begin with "I" followed by another captial letter. Note this includes a
        /// fix for generic interface names, that end with `{number}.
        /// </summary>
        private static readonly Regex InterfaceName = new Regex("^I[A-Z][a-zA-Z0-9_]*(?:`\\d+)?$", RegexOptions.Compiled);

        /// <summary>
        /// Class names must be pascal case and not use the interface naming convention.
        /// </summary>
        private static readonly Regex ClassName = new Regex("^[A-Z][a-zA-Z0-9_]*(?:`\\d+)?$", RegexOptions.Compiled);

        /// <summary>
        /// Public members should not contain the word "Comparer". In .NET, these should be named "Comparer".
        /// </summary>
        private static readonly Regex ContainsComparer = new Regex("[Cc]omparator", RegexOptions.Compiled);

        /// <summary>
        /// Public methods and properties should not contain the word "Int" that is not followed by 16, 32, or 64,
        /// "Long", "Short", or "Float". These should be converted to their .NET names "Int32", "Int64", "Int16", and "Short".
        /// Note we need to ignore common words such as "point", "intern", and "intersect".
        /// </summary>
        private static readonly Regex ContainsNonNetNumeric = new Regex("(?<![Pp]o|[Pp]r|[Jj]o)[Ii]nt(?!16|32|64|er|eg|ro)|[Ll]ong(?!est|er)|[Ss]hort(?!est|er)|[Ff]loat", RegexOptions.Compiled);

        ///// <summary>
        ///// Constants should not contain the word INT that is not followed by 16, 32, or 64, LONG, SHORT, or FLOAT
        ///// </summary>
        //private static readonly Regex ConstContainsNonNetNumeric = new Regex("(?<!PO|PR|JO)INT(?!16|32|64|ER|EG|RO)|LONG(?!EST|ER)|SHORT(?!EST|ER)|FLOAT", RegexOptions.Compiled);

        /// <summary>
        /// Matches IL code pattern for a method body with only a return statement for a local variable.
        /// In this case, the array is writable by the consumer.
        /// </summary>
        private static readonly Regex MethodBodyReturnValueOnly = new Regex("\\0\\u0002\\{(?:.|\\\\u\\d\\d\\d\\d|\\0|\\[a-z]){3}\\u0004\\n\\+\\0\\u0006\\*", RegexOptions.Compiled);


        //[Test, LuceneNetSpecific]
        public virtual void TestProtectedFieldNames(Type typeFromTargetAssembly)
        {
            var names = GetInvalidProtectedFields(typeFromTargetAssembly.Assembly);

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " invalid protected field names detected. " +
                "Protected fields must be camelCase and prefixed with 'm_' to prevent naming conflicts with properties.");
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestPrivateFieldNames(Type typeFromTargetAssembly)
        {
            TestPrivateFieldNames(typeFromTargetAssembly, null);
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestPrivateFieldNames(Type typeFromTargetAssembly, string exceptionRegex)
        {
            var names = GetInvalidPrivateFields(typeFromTargetAssembly.Assembly, exceptionRegex);

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " invalid private field names detected. " +
                "Private field names should be camelCase.");
        }

        public virtual void TestPublicFields(Type typeFromTargetAssembly)
        {
            TestPublicFields(typeFromTargetAssembly, null);
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestPublicFields(Type typeFromTargetAssembly, string exceptionRegex)
        {
            var names = GetInvalidPublicFields(typeFromTargetAssembly.Assembly, exceptionRegex);

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " public fields detected. Consider using public properties instead." +
                "Public properties that return arrays should be decorated with the WritableArray attribute.");
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestMethodParameterNames(Type typeFromTargetAssembly)
        {
            var names = GetInvalidMethodParameterNames(typeFromTargetAssembly.Assembly);

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " invalid method parameter names detected. " +
                "Parameter names must be camelCase and may not start or end with '_'.");
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestInterfaceNames(Type typeFromTargetAssembly)
        {
            var names = GetInvalidInterfaceNames(typeFromTargetAssembly.Assembly);

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " invalid interface names detected. " +
                "Interface names must begin with a capital 'I' followed by another capital letter.");
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestClassNames(Type typeFromTargetAssembly)
        {
            var names = GetInvalidClassNames(typeFromTargetAssembly.Assembly);

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " invalid class names detected. " +
                "Class names must be Pascal case, but may not follow the interface naming " + 
                "convention of captial 'I' followed by another capital letter.");
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestForPropertiesWithNoGetter(Type typeFromTargetAssembly)
        {
            var names = GetPropertiesWithNoGetter(typeFromTargetAssembly.Assembly);

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " properties with a setter but no getter detected. " +
                "Getters are required for properties. If the main functionality is to set a value, " +
                "consider using a method instead (prefixed with Set).");
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestForPropertiesThatReturnArray(Type typeFromTargetAssembly)
        {
            var names = GetPropertiesThatReturnArray(typeFromTargetAssembly.Assembly);

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " properties that return Array detected. " +
                "Properties should generally not return Array. Change to a method (prefixed with Get) " + 
                "or if returning an array that can be written to was intended, decorate with the WritableArray attribute. " +
                "Note that returning an array field from either a property or method means the array can be written to by " + 
                "the consumer if the array is not cloned using arr.ToArray().");
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestForMethodsThatReturnWritableArray(Type typeFromTargetAssembly)
        {
            var names = GetMethodsThatReturnWritableArray(typeFromTargetAssembly.Assembly);

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " methods that return a writable Array detected. " +
                "An array should be cloned before returning using arr.ToArray() or if it is intended to be writable, " +
                "decorate with the WritableArray attribute and consider making it a property for clarity.");
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestForPublicMembersContainingComparer(Type typeFromTargetAssembly)
        {
            var names = new List<string>();

            names.AddRange(GetProtectedFieldsContainingComparer(typeFromTargetAssembly.Assembly));
            names.AddRange(GetMembersContainingComparer(typeFromTargetAssembly.Assembly));

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " member names containing the word 'comparer' detected. " +
                "In .NET, we need to change the word 'comparer' to 'comparer'.");
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestForPublicMembersNamedSize(Type typeFromTargetAssembly)
        {
            var names = GetMembersNamedSize(typeFromTargetAssembly.Assembly);

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " member names named 'Size'. " +
                "In .NET, we need to change the name 'Size' to either 'Count' or 'Length', " + 
                "and it should generally be made a property.");
        }

        

        //[Test, LuceneNetSpecific]
        public virtual void TestForPublicMembersContainingNonNetNumeric(Type typeFromTargetAssembly)
        {
            var names = GetMembersContainingNonNetNumeric(typeFromTargetAssembly.Assembly);

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " member names containing the word 'Int' not followed " + 
                "by 16, 32, or 64, 'Long', 'Short', or 'Float' detected. " +
                "In .NET, we need to change to 'Short' to 'Int16', 'Int' to 'Int32', 'Long' to 'Int64', and 'Float' to 'Single'.");
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestForTypesContainingNonNetNumeric(Type typeFromTargetAssembly)
        {
            var names = GetTypesContainingNonNetNumeric(typeFromTargetAssembly.Assembly);

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " member names containing the word 'Int' not followed " +
                "by 16, 32, or 64, 'Long', 'Short', or 'Float' detected. " +
                "In .NET, we need to change to 'Short' to 'Int16', 'Int' to 'Int32', 'Long' to 'Int64', and 'Float' to 'Single'." +
                "\n\nIMPORTANT: Before making changes, make sure to rename any types with ambiguous use of the word `Single` (meaning 'singular' rather than `System.Single`) to avoid confusion.");
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestForPublicMembersWithNullableEnum(Type typeFromTargetAssembly)
        {
            var names = GetPublicNullableEnumMembers(typeFromTargetAssembly.Assembly);

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " members that are type nullable enum detected. " +
                "Nullable enum parameters, fields, methods, and properties should be eliminated (where possible), either by " +
                "eliminating the logic that depends on 'null'. Sometimes, it makes sense to keep a nullable enum parameter. " +
                "In those cases, mark the member with the [ExceptionToNullableEnumConvention] attribute.");
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestForMembersAcceptingOrReturningIEnumerable(Type typeFromTargetAssembly)
        {
            TestForMembersAcceptingOrReturningIEnumerable(typeFromTargetAssembly, null);
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestForMembersAcceptingOrReturningIEnumerable(Type typeFromTargetAssembly, string exceptionRegex)
        {
            var names = GetMembersAcceptingOrReturningType(typeof(IEnumerable<>), typeFromTargetAssembly.Assembly, false, exceptionRegex);

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " members that accept or return IEnumerable<T> detected.");
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestForMembersAcceptingOrReturningListOrDictionary(Type typeFromTargetAssembly)
        {
            TestForMembersAcceptingOrReturningListOrDictionary(typeFromTargetAssembly, null);
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestForMembersAcceptingOrReturningListOrDictionary(Type typeFromTargetAssembly, string exceptionRegex)
        {
            var names = new List<string>();
            names.AddRange(GetMembersAcceptingOrReturningType(typeof(List<>), typeFromTargetAssembly.Assembly, true, exceptionRegex));
            names.AddRange(GetMembersAcceptingOrReturningType(typeof(Dictionary<,>), typeFromTargetAssembly.Assembly, true, exceptionRegex));

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " members that accept or return List<T> or Dictionary<K, V> detected. " +
                "These should be changed to IList<T> and IDictionary<K, V>, respectively.");
        }

        private static IEnumerable<string> GetInvalidPrivateFields(Assembly assembly, string exceptionRegex)
        {
            var result = new List<string>();

            var classes = assembly.GetTypes().Where(t => t.IsClass);

            foreach (var c in classes)
            {
                var fields = c.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                foreach (var field in fields)
                {
                    if (field.Name.StartsWith("<", StringComparison.Ordinal)) // Ignore auto-implemented properties
                    {
                        continue;
                    }

                    if (field.DeclaringType.GetEvent(field.Name) != null) // Ignore events
                    {
                        continue;
                    }

                    if ((field.IsPrivate || field.IsAssembly) && !PrivateFieldName.IsMatch(field.Name) && field.DeclaringType.Equals(c.UnderlyingSystemType))
                    {
                        var name = string.Concat(c.FullName, ".", field.Name);
                        if (!IsException(name, exceptionRegex))
                        {
                            result.Add(name);
                        }
                    }
                }
            }

            return result.ToArray();
        }

        private static IEnumerable<string> GetInvalidProtectedFields(Assembly assembly)
        {
            var result = new List<string>();

            var classes = assembly.GetTypes().Where(t => t.IsClass);

            foreach (var c in classes)
            {
                if (!string.IsNullOrEmpty(c.Namespace) && c.Namespace.StartsWith("Lucene.Net.Support", StringComparison.Ordinal))
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(c.Name) && c.Name.Equals("AssemblyKeys", StringComparison.Ordinal))
                {
                    continue;
                }

                var fields = c.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (field.Name.StartsWith("<", StringComparison.Ordinal)) // Ignore auto-implemented properties
                    {
                        continue;
                    }

                    if (field.DeclaringType.GetEvent(field.Name) != null) // Ignore events
                    {
                        continue;
                    }

                    if ((field.IsFamily || field.IsFamilyOrAssembly) && !ProtectedFieldName.IsMatch(field.Name) && field.DeclaringType.Equals(c.UnderlyingSystemType))
                    {
                        result.Add(string.Concat(c.FullName, ".", field.Name));
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// All public fields are invalid
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetInvalidPublicFields(Assembly assembly, string exceptionRegex)
        {
            var result = new List<string>();

            var classes = assembly.GetTypes().Where(t => t.IsClass);

            foreach (var c in classes)
            {
                if (c.Name.StartsWith("<", StringComparison.Ordinal)) // Ignore classes produced by anonymous methods 
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(c.Namespace) && c.Namespace.StartsWith("Lucene.Net.Support", StringComparison.Ordinal))
                {
                    continue;
                }

                var fields = c.GetFields(BindingFlags.Public | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (field.Name.StartsWith("<", StringComparison.Ordinal)) // Ignore auto-implemented properties
                    {
                        continue;
                    }

                    if (field.DeclaringType.GetEvent(field.Name) != null) // Ignore events
                    {
                        continue;
                    }

                    if (field.IsPublic && field.DeclaringType.Equals(c.UnderlyingSystemType))
                    {
                        var name = string.Concat(c.FullName, ".", field.Name);
                        if (!IsException(name, exceptionRegex))
                        {
                            result.Add(name);
                        }
                    }
                }
            }

            return result.ToArray();
        }

        private static IEnumerable<string> GetInvalidMethodParameterNames(Assembly assembly)
        {
            var result = new List<string>();

            var classes = assembly.GetTypes().Where(t => t.IsClass);

            foreach (var c in classes)
            {
                var methods = c.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                foreach (var method in methods)
                {
                    if (method.Name.StartsWith("<", StringComparison.Ordinal)) // Ignore auto-generated methods
                    {
                        continue;
                    }

                    var parameters = method.GetParameters();

                    foreach (var parameter in parameters)
                    {
                        if (!MethodParameterName.IsMatch(parameter.Name) && method.DeclaringType.Equals(c.UnderlyingSystemType))
                        {
                            result.Add(string.Concat(c.FullName, ".", method.Name, " -parameter- ", parameter.Name));
                        }
                    }
                }
            }

            return result.ToArray();
        }

        private static IEnumerable<string> GetInvalidInterfaceNames(Assembly assembly)
        {
            var result = new List<string>();

            var interfaces = assembly.GetTypes().Where(t => t.IsInterface);

            foreach (var i in interfaces)
            {
                if (!InterfaceName.IsMatch(i.Name))
                {
                    result.Add(i.FullName);
                }
            }

            return result.ToArray();
        }

        private static IEnumerable<string> GetInvalidClassNames(Assembly assembly)
        {
            var result = new List<string>();

            var classes = assembly.GetTypes().Where(t => t.IsClass);

            foreach (var c in classes)
            {
                if (c.Name.StartsWith("<", StringComparison.Ordinal)) // Ignore classes produced by anonymous methods 
                {
                    continue;
                }

                if (c.IsDefined(typeof(ExceptionToClassNameConventionAttribute)))
                {
                    continue;
                }

                if (!ClassName.IsMatch(c.Name) || InterfaceName.IsMatch(c.Name))
                {
                    result.Add(c.FullName);
                }
            }

            return result.ToArray();
        }

        private static IEnumerable<string> GetPropertiesWithNoGetter(Assembly assembly)
        {
            var result = new List<string>();

            var classes = assembly.GetTypes().Where(t => t.IsClass);

            foreach (var c in classes)
            {
                var properties = c.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    if (property.GetSetMethod(true) != null && property.GetGetMethod(true) is null && property.DeclaringType.Equals(c.UnderlyingSystemType))
                    {
                        result.Add(string.Concat(c.FullName, ".", property.Name));
                    }
                }
            }

            return result.ToArray();
        }

        private static IEnumerable<string> GetPropertiesThatReturnArray(Assembly assembly)
        {
            var result = new List<string>();

            var classes = assembly.GetTypes().Where(t => t.IsClass);

            foreach (var c in classes)
            {
                var properties = c.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    // Skip attributes with WritableArrayAttribute defined. These are
                    // properties that were intended to expose arrays, as per MSDN this
                    // is not a .NET best practice. However, Lucene's design requires that
                    // this be done.
                    if (property.IsDefined(typeof(WritableArrayAttribute)))
                    {
                        continue;
                    }

                    var getMethod = property.GetGetMethod();
                    
                    if (getMethod != null && getMethod.ReturnParameter != null && getMethod.ReturnParameter.ParameterType.IsArray && property.DeclaringType.Equals(c.UnderlyingSystemType))
                    {
                        result.Add(string.Concat(c.FullName, ".", property.Name));
                    }
                }
            }

            return result.ToArray();
        }


        private static IEnumerable<string> GetProtectedFieldsContainingComparer(Assembly assembly)
        {
            var result = new List<string>();

            var classes = assembly.GetTypes().Where(t => t.IsClass);

            foreach (var c in classes)
            {
                var fields = c.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (field.Name.StartsWith("<", StringComparison.Ordinal)) // Ignore auto-implemented properties
                    {
                        continue;
                    }

                    if (field.DeclaringType.GetEvent(field.Name) != null) // Ignore events
                    {
                        continue;
                    }

                    if ((field.IsFamily || field.IsFamilyOrAssembly) && ContainsComparer.IsMatch(field.Name) && field.DeclaringType.Equals(c.UnderlyingSystemType))
                    {
                        result.Add(string.Concat(c.FullName, ".", field.Name));
                    }
                }
            }

            return result.ToArray();
        }

        private static IEnumerable<string> GetMembersContainingComparer(Assembly assembly)
        {
            var result = new List<string>();

            var types = assembly.GetTypes();

            foreach (var t in types)
            {
                if (ContainsComparer.IsMatch(t.Name) && t.IsVisible)
                {
                    result.Add(t.FullName);
                }
                
                var members = t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                foreach (var member in members)
                {
                    if (ContainsComparer.IsMatch(member.Name) && member.DeclaringType.Equals(t.UnderlyingSystemType))
                    {
                        if (member.MemberType == MemberTypes.Method && !(member.Name.StartsWith("get_", StringComparison.Ordinal) || member.Name.StartsWith("set_", StringComparison.Ordinal)))
                        {
                            result.Add(string.Concat(t.FullName, ".", member.Name, "()"));
                        }
                        else if (member.MemberType == MemberTypes.Property)
                        {
                            result.Add(string.Concat(t.FullName, ".", member.Name));
                        }
                        else if (member.MemberType == MemberTypes.Event)
                        {
                            result.Add(string.Concat(t.FullName, ".", member.Name, " (event)"));
                        }
                    }
                }
            }

            return result.ToArray();
        }

        private static IEnumerable<string> GetMembersNamedSize(Assembly assembly)
        {
            var result = new List<string>();

            var types = assembly.GetTypes();

            foreach (var t in types)
            {
                var members = t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                foreach (var member in members)
                {
                    if ("Size".Equals(member.Name, StringComparison.OrdinalIgnoreCase) && member.DeclaringType.Equals(t.UnderlyingSystemType))
                    {
                        if (member.MemberType == MemberTypes.Method && !(member.Name.StartsWith("get_", StringComparison.Ordinal) || member.Name.StartsWith("set_", StringComparison.Ordinal)))
                        {
                            var method = (MethodInfo)member;
                            // Ignore methods with parameters
                            if (!method.GetParameters().Any())
                            {
                                result.Add(string.Concat(t.FullName, ".", member.Name, "()"));
                            }
                        }
                        else if (member.MemberType == MemberTypes.Property)
                        {
                            result.Add(string.Concat(t.FullName, ".", member.Name));
                        }
                        
                    }
                }
            }

            return result.ToArray();
        }

        private static IEnumerable<string> GetMembersContainingNonNetNumeric(Assembly assembly)
        {
            var result = new List<string>();

            var types = assembly.GetTypes();

            foreach (var t in types)
            {
                //if (ContainsComparer.IsMatch(t.Name) && t.IsVisible)
                //{
                //    result.Add(t.FullName);
                //}

                var members = t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                foreach (var member in members)
                {
                    // Ignore properties, methods, and events with IgnoreNetNumericConventionAttribute
                    if (member.IsDefined(typeof(ExceptionToNetNumericConventionAttribute)))
                    {
                        continue;
                    }

                    if (ContainsNonNetNumeric.IsMatch(member.Name) && member.DeclaringType.Equals(t.UnderlyingSystemType))
                    {
                        if (member.MemberType == MemberTypes.Method && !(member.Name.StartsWith("get_", StringComparison.Ordinal) || member.Name.StartsWith("set_", StringComparison.Ordinal)))
                        {
                            result.Add(string.Concat(t.FullName, ".", member.Name, "()"));
                        }
                        else if (member.MemberType == MemberTypes.Property)
                        {
                            result.Add(string.Concat(t.FullName, ".", member.Name));
                        }
                        else if (member.MemberType == MemberTypes.Event)
                        {
                            result.Add(string.Concat(t.FullName, ".", member.Name, " (event)"));
                        }
                    }
                }
            }

            return result.ToArray();
        }

        private static IEnumerable<string> GetTypesContainingNonNetNumeric(Assembly assembly)
        {
            var result = new List<string>();

            var types = assembly.GetTypes();

            foreach (var t in types)
            {
                if (t.IsDefined(typeof(ExceptionToNetNumericConventionAttribute)))
                {
                    continue;
                }

                if (ContainsNonNetNumeric.IsMatch(t.Name))
                {
                    result.Add(t.FullName);
                }
            }

            return result.ToArray();
        }

        private static IEnumerable<string> GetMethodsThatReturnWritableArray(Assembly assembly)
        {
            var result = new List<string>();

            var classes = assembly.GetTypes().Where(t => t.IsClass);

            foreach (var c in classes)
            {
                if (c.Name.StartsWith("<", StringComparison.Ordinal)) // Ignore classes produced by anonymous methods 
                {
                    continue;
                }

                var methods = c.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var method in methods)
                {
                    // Skip attributes with WritableArrayAttribute defined. These are
                    // properties that were intended to expose arrays, as per MSDN this
                    // is not a .NET best practice. However, Lucene's design requires that
                    // this be done.
                    if (method.IsDefined(typeof(WritableArrayAttribute)))
                    {
                        continue;
                    }

                    // Ignore property method definitions
                    if (method.Name.StartsWith("get_", StringComparison.Ordinal) || method.Name.StartsWith("set_", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (method != null && method.ReturnParameter != null 
                        && method.ReturnParameter.ParameterType.IsArray 
                        && method.DeclaringType.Equals(c.UnderlyingSystemType))
                    {

                        var methodBody = method.GetMethodBody();
                        if (methodBody != null)
                        {
                            var il = Encoding.UTF8.GetString(methodBody.GetILAsByteArray());

                            if (MethodBodyReturnValueOnly.IsMatch(il))
                            {
                                result.Add(string.Concat(c.FullName, ".", method.Name));
                            }
                        }
                    }
                }
            }

            return result.ToArray();
        }

        private static IEnumerable<string> GetPublicNullableEnumMembers(Assembly assembly)
        {
            var result = new List<string>();

            var types = assembly.GetTypes();

            foreach (var t in types)
            {
                var members = t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                foreach (var member in members)
                {
                    if (member.Name.StartsWith("<", StringComparison.Ordinal)) // Ignore auto-generated methods
                    {
                        continue;
                    }

                    // Ignore properties, methods, and events with IgnoreNetNumericConventionAttribute
                    if (member.IsDefined(typeof(ExceptionToNullableEnumConventionAttribute)))
                    {
                        continue;
                    }

                    if (member.DeclaringType.Equals(t.UnderlyingSystemType))
                    {
                        if (member.MemberType == MemberTypes.Method && !(member.Name.StartsWith("get_", StringComparison.Ordinal) || member.Name.StartsWith("set_", StringComparison.Ordinal)))
                        {
                            var method = (MethodInfo)member;

                            if (!method.IsPrivate)
                            {
                                if (method.ReturnParameter != null
                                    && Nullable.GetUnderlyingType(method.ReturnParameter.ParameterType) != null
                                    && method.ReturnParameter.ParameterType.GetGenericArguments()[0].IsEnum)
                                {
                                    result.Add(string.Concat(t.FullName, ".", member.Name, "()"));
                                }

                                var parameters = method.GetParameters();

                                foreach (var parameter in parameters)
                                {
                                    if (Nullable.GetUnderlyingType(parameter.ParameterType) != null
                                        && parameter.ParameterType.GetGenericArguments()[0].IsEnum
                                        && member.DeclaringType.Equals(t.UnderlyingSystemType))
                                    {
                                        result.Add(string.Concat(t.FullName, ".", member.Name, "()", " -parameter- ", parameter.Name));
                                    }
                                }
                            }
                        }
                        else if (member.MemberType == MemberTypes.Constructor)
                        {
                            var constructor = (ConstructorInfo)member;

                            if (!constructor.IsPrivate)
                            {
                                var parameters = constructor.GetParameters();

                                foreach (var parameter in parameters)
                                {
                                    if (Nullable.GetUnderlyingType(parameter.ParameterType) != null
                                        && parameter.ParameterType.GetGenericArguments()[0].IsEnum
                                        && member.DeclaringType.Equals(t.UnderlyingSystemType))
                                    {
                                        result.Add(string.Concat(t.FullName, ".", member.Name, "()", " -parameter- ", parameter.Name));
                                    }
                                }
                            }
                        }
                        else if (member.MemberType == MemberTypes.Property 
                            && Nullable.GetUnderlyingType(((PropertyInfo)member).PropertyType) != null 
                            && ((PropertyInfo)member).PropertyType.GetGenericArguments()[0].IsEnum 
                            && IsNonPrivateProperty((PropertyInfo)member))
                        {
                            result.Add(string.Concat(t.FullName, ".", member.Name));
                        }
                        else if (member.MemberType == MemberTypes.Field 
                            && Nullable.GetUnderlyingType(((FieldInfo)member).FieldType) != null 
                            && ((FieldInfo)member).FieldType.GetGenericArguments()[0].IsEnum 
                            && (((FieldInfo)member).IsFamily || ((FieldInfo)member).IsFamilyOrAssembly))
                        {
                            result.Add(string.Concat(t.FullName, ".", member.Name, " (field)"));
                        }
                    }
                }
            }

            return result.ToArray();
        }

        private static bool IsNonPrivateProperty(PropertyInfo property)
        {
            var getMethod = property.GetGetMethod();
            var setMethod = property.GetSetMethod();
            return ((getMethod != null && !getMethod.IsPrivate) ||
                (setMethod != null && !setMethod.IsPrivate));
        }

        private static bool IsException(string name, string exceptionRegex)
        {
            bool hasExceptions = !string.IsNullOrWhiteSpace(exceptionRegex);
            return (hasExceptions && Regex.IsMatch(name, exceptionRegex));
        }

        /// <summary>
        /// Some parameters were incorrectly changed from List to IEnumerable during the port. This is
        /// to track down constructor and method parameters and property and method return types
        /// containing IEnumerable
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetMembersAcceptingOrReturningType(Type lookFor, Assembly assembly, bool publiclyVisibleOnly, string exceptionRegex)
        {
            var result = new List<string>();

            var types = assembly.GetTypes();

            foreach (var t in types)
            {
                var members = t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                foreach (var member in members)
                {
                    if (member.Name.StartsWith("<", StringComparison.Ordinal)) // Ignore auto-generated methods
                    {
                        continue;
                    }

                    if (member.DeclaringType.Equals(t.UnderlyingSystemType))
                    {
                        if (member.MemberType == MemberTypes.Method && !(member.Name.StartsWith("get_", StringComparison.Ordinal) || member.Name.StartsWith("set_", StringComparison.Ordinal)))
                        {
                            var method = (MethodInfo)member;

                            if (!publiclyVisibleOnly || !method.IsPrivate)
                            {

                                if (method.ReturnParameter != null
                                    && method.ReturnParameter.ParameterType.IsGenericType
                                    && method.ReturnParameter.ParameterType.GetGenericTypeDefinition().IsAssignableFrom(lookFor))
                                {
                                    var name = string.Concat(t.FullName, ".", member.Name, "()");

                                    if (!IsException(name, exceptionRegex))
                                    {
                                        result.Add(name);
                                    }
                                }

                                var parameters = method.GetParameters();

                                foreach (var parameter in parameters)
                                {
                                    if (parameter.ParameterType.IsGenericType
                                        && parameter.ParameterType.GetGenericTypeDefinition().IsAssignableFrom(lookFor)
                                        && member.DeclaringType.Equals(t.UnderlyingSystemType))
                                    {
                                        var name = string.Concat(t.FullName, ".", member.Name, "()", " -parameter- ", parameter.Name);

                                        if (!IsException(name, exceptionRegex))
                                        {
                                            result.Add(name);
                                        }
                                    }
                                }
                            }
                        }
                        else if (member.MemberType == MemberTypes.Constructor)
                        {
                            var constructor = (ConstructorInfo)member;

                            if (!publiclyVisibleOnly || !constructor.IsPrivate)
                            {
                                var parameters = constructor.GetParameters();

                                foreach (var parameter in parameters)
                                {
                                    if (parameter.ParameterType.IsGenericType
                                        && parameter.ParameterType.GetGenericTypeDefinition().IsAssignableFrom(lookFor)
                                        && member.DeclaringType.Equals(t.UnderlyingSystemType))
                                    {
                                        var name = string.Concat(t.FullName, ".", member.Name, "()", " -parameter- ", parameter.Name);

                                        if (!IsException(name, exceptionRegex))
                                        {
                                            result.Add(name);
                                        }
                                    }
                                }
                            }
                        }
                        else if (member.MemberType == MemberTypes.Property
                            && ((PropertyInfo)member).PropertyType.IsGenericType
                            && ((PropertyInfo)member).PropertyType.GetGenericTypeDefinition().IsAssignableFrom(lookFor)
                            && (!publiclyVisibleOnly || IsNonPrivateProperty((PropertyInfo)member)))
                        {
                            var name = string.Concat(string.Concat(t.FullName, ".", member.Name));

                            if (!IsException(name, exceptionRegex))
                            {
                                result.Add(name);
                            }
                        }
                        //else if (member.MemberType == MemberTypes.Field
                        //    && ((FieldInfo)member).IndexableFieldType.IsGenericType
                        //    && ((FieldInfo)member).IndexableFieldType.GetGenericTypeDefinition().IsAssignableFrom(lookFor)
                        //    && (!publiclyVisibleOnly || (((FieldInfo)member).IsFamily || ((FieldInfo)member).IsFamilyOrAssembly)))
                        //{
                        //    result.Add(string.Concat(t.FullName, ".", member.Name, " (field)"));
                        //}
                    }
                }
            }

            return result.ToArray();
        }
    }
}
