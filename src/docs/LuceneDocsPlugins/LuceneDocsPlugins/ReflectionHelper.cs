using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LuceneDocsPlugins
{
    internal static class ReflectionHelper
    {
        public static object CallMethod(this object obj, string methodName, params object[] parameters)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            Type type = obj.GetType();
            var methodInfo = GetMethodInfo(type, methodName);
            if (methodInfo == null)
                throw new ArgumentOutOfRangeException("methodName",
                    string.Format("Couldn't find method {0} in type {1}", methodName, type.FullName));
            return methodInfo.Invoke(obj, parameters);
        }

        private static MethodInfo GetMethodInfo(Type type, string methodName, Func<IEnumerable<MethodInfo>, MethodInfo> filter = null)
        {
            MethodInfo methodInfo = null;
            do
            {
                try
                {
                    methodInfo = type.GetMethod(methodName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }
                catch (AmbiguousMatchException)
                {
                    if (filter == null) throw;

                    methodInfo = filter(
                        type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                            .Where(x => x.Name == methodName));
                }
                type = type.BaseType;
            }
            while (methodInfo == null && type != null);
            return methodInfo;
        }
    }
}