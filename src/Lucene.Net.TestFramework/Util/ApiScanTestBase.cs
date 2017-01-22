using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Lucene.Net.Util
{
    /// <summary>
    /// LUCENENET specific - functionality for scanning the API to ensure 
    /// naming and .NET conventions are followed consistently.
    /// </summary>
    public abstract class ApiScanTestBase : LuceneTestCase
    {
        /// <summary>
        /// Private fields must be upper case separated with underscores, 
        /// must be camelCase (optionally may be prefixed with underscore, 
        /// but it is preferred not to use the underscore to match Lucene).
        /// </summary>
        private static Regex PrivateFieldName = new Regex("^_?[a-z][a-zA-Z0-9_]*$|^[A-Z0-9_]+$", RegexOptions.Compiled);

        /// <summary>
        /// Protected fields must either be upper case separated with underscores or
        /// must be prefixed with m_ (to avoid naming conflicts with properties).
        /// </summary>
        private static Regex ProtectedFieldName = new Regex("^m_[a-z][a-zA-Z0-9_]*$|^[A-Z0-9_]+$", RegexOptions.Compiled);

        /// <summary>
        /// Method parameters must be camelCase and not begin or end with underscore.
        /// </summary>
        private static Regex MethodParameterName = new Regex("^[a-z](?:[a-zA-Z0-9_]*[a-zA-Z0-9])?$", RegexOptions.Compiled);

        /// <summary>
        /// Public members should not contain the word "Comparer". In .NET, these should be named "Comparer".
        /// </summary>
        private static Regex ContainsComparer = new Regex("[Cc]omparator", RegexOptions.Compiled);

        /// <summary>
        /// Matches IL code pattern for a method body with only a return statement for a local variable.
        /// In this case, the array is writable by the consumer.
        /// </summary>
        private static Regex MethodBodyReturnValueOnly = new Regex("\\0\\u0002\\{(?:.|\\\\u\\d\\d\\d\\d|\\0|\\[a-z]){3}\\u0004\\n\\+\\0\\u0006\\*", RegexOptions.Compiled);


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

            Assert.IsFalse(names.Any(), names.Count() + " invalid protected field names detected.");
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestPrivateFieldNames(Type typeFromTargetAssembly)
        {
            var names = GetInvalidPrivateFields(typeFromTargetAssembly.Assembly);

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " invalid private field names detected.");
        }

        //[Test, LuceneNetSpecific]
        public virtual void TestPublicFields(Type typeFromTargetAssembly)
        {
            var names = GetInvalidPublicFields(typeFromTargetAssembly.Assembly);

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


        private static IEnumerable<string> GetInvalidPrivateFields(Assembly assembly)
        {
            var result = new List<string>();

            var classes = assembly.GetTypes().Where(t => t.IsClass);

            foreach (var c in classes)
            {
                var fields = c.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (field.Name.StartsWith("<")) // Ignore auto-implemented properties
                    {
                        continue;
                    }

                    if (field.DeclaringType.GetEvent(field.Name) != null) // Ignore events
                    {
                        continue;
                    }

                    if ((field.IsPrivate || field.IsAssembly) && !PrivateFieldName.IsMatch(field.Name) && field.DeclaringType.Equals(c.UnderlyingSystemType))
                    {
                        result.Add(string.Concat(c.FullName, ".", field.Name));
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
                if (!string.IsNullOrEmpty(c.Namespace) && c.Namespace.StartsWith("Lucene.Net.Support"))
                {
                    continue;
                }

                var fields = c.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (field.Name.StartsWith("<")) // Ignore auto-implemented properties
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
        private static IEnumerable<string> GetInvalidPublicFields(Assembly assembly)
        {
            var result = new List<string>();

            var classes = assembly.GetTypes().Where(t => t.IsClass);

            foreach (var c in classes)
            {
                if (c.Name.StartsWith("<")) // Ignore classes produced by anonymous methods 
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(c.Namespace) && c.Namespace.StartsWith("Lucene.Net.Support"))
                {
                    continue;
                }

                var fields = c.GetFields(BindingFlags.Public | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (field.Name.StartsWith("<")) // Ignore auto-implemented properties
                    {
                        continue;
                    }

                    if (field.DeclaringType.GetEvent(field.Name) != null) // Ignore events
                    {
                        continue;
                    }

                    if (field.IsPublic && field.DeclaringType.Equals(c.UnderlyingSystemType))
                    {
                        result.Add(string.Concat(c.FullName, ".", field.Name));
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
                    if (method.Name.StartsWith("<")) // Ignore auto-generated methods
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

        private static IEnumerable<string> GetPropertiesWithNoGetter(Assembly assembly)
        {
            var result = new List<string>();

            var classes = assembly.GetTypes().Where(t => t.IsClass);

            foreach (var c in classes)
            {
                var properties = c.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    if (property.GetSetMethod() != null && property.GetGetMethod() == null && property.DeclaringType.Equals(c.UnderlyingSystemType))
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
                    if (System.Attribute.IsDefined(property, typeof(WritableArrayAttribute)))
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
                    if (field.Name.StartsWith("<")) // Ignore auto-implemented properties
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
                
                var members = t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var member in members)
                {
                    if (ContainsComparer.IsMatch(member.Name) && member.DeclaringType.Equals(t.UnderlyingSystemType))
                    {
                        if (member.MemberType == MemberTypes.Method && !member.Name.StartsWith("get_"))
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

        private static IEnumerable<string> GetMethodsThatReturnWritableArray(Assembly assembly)
        {
            var result = new List<string>();

            var classes = assembly.GetTypes().Where(t => t.IsClass);

            foreach (var c in classes)
            {
                if (c.Name.StartsWith("<")) // Ignore classes produced by anonymous methods 
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
                    if (System.Attribute.IsDefined(method, typeof(WritableArrayAttribute)))
                    {
                        continue;
                    }

                    // Ignore property method definitions
                    if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
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
                                result.Add(string.Concat(c.FullName, ".", method.Name, " ---- ", il));
                            }
                        }
                    }
                }
            }

            return result.ToArray();
        }
    }
}
