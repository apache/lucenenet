using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private static Regex PrivateFieldName = new Regex("^_?[a-z][a-zA-Z0-9_]*$|^[A-Z0-9_]+$");

        /// <summary>
        /// Protected fields must either be upper case separated with underscores or
        /// must be prefixed with m_ (to avoid naming conflicts with properties).
        /// </summary>
        private static Regex ProtectedFieldName = new Regex("^m_[a-z][a-zA-Z0-9_]*$|^[A-Z0-9_]+$");

        /// <summary>
        /// Method parameters must be camelCase and not begin or end with underscore.
        /// </summary>
        private static Regex MethodParameterName = new Regex("^[a-z](?:[a-zA-Z0-9_]*[a-zA-Z0-9])?$");

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

            Assert.IsFalse(names.Any(), names.Count() + " public fields detected. Consider using public properties instead.");
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
                "Properties should not return Array. Change to a method (prefixed with Get).");
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
                    var getMethod = property.GetGetMethod();
                    
                    if (getMethod != null && getMethod.ReturnParameter != null && getMethod.ReturnParameter.ParameterType.IsArray && property.DeclaringType.Equals(c.UnderlyingSystemType))
                    {
                        result.Add(string.Concat(c.FullName, ".", property.Name));
                    }
                }
            }

            return result.ToArray();
        }

        // LUCENENET TODO: Make methods for finding types, fields, properties, methods, and method parameters containing
        // the word "comparator" (case insensitive), which should be changed to "comparer" in .NET.
    }
}
