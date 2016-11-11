using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

#if !FEATURE_STACKTRACE
using System.Diagnostics;
#endif

namespace Lucene.Net.Util
{
    public static class StackTraceHelper
    {
        private static Regex s_methodNameRegex = new Regex(@"at\s+(?<fullyQualifiedMethod>.*\.(?<method>[\w`]+))\(");

        /// <summary>
        /// Matches the StackTrace for a method name.
        /// </summary>
        public static bool DoesStackTraceContainMethod(string methodName)
        {
#if FEATURE_STACKTRACE
            IEnumerable<string> allMethods = GetStackTrace(false);
            return allMethods.Contains(methodName);
#else
            StackTrace trace = new StackTrace();
            foreach (var frame in trace.GetFrames())
            {
                if (frame.GetMethod().Name.equals(methodName))
                {
                    return true;
                }
            }
            return false;
#endif

        }

        /// <summary>
        /// Matches the StackTrace for a particular class (not fully-qualified) and method name.
        /// </summary>
        public static bool DoesStackTraceContainMethod(string className, string methodName)
        {
#if FEATURE_STACKTRACE
            IEnumerable<string> allMethods = GetStackTrace(true);
            return allMethods.Any(x => x.Contains(className + '.' + methodName));
#else
            StackTrace trace = new StackTrace();
            foreach (var frame in trace.GetFrames())
            {
                var method = frame.GetMethod();
                if (method.DeclaringType.Name.equals(className) && method.Name.equals(methodName))
                {
                    return true;
                }
            }
            return false;
#endif
        }

        private static IEnumerable<string> GetStackTrace(bool includeFullyQualifiedName)
        {
            var matches =
                Environment.StackTrace
                .Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line =>
                {
                    var match = s_methodNameRegex.Match(line);

                    if (!match.Success)
                    {
                        return null;
                    }

                    return includeFullyQualifiedName
                        ? match.Groups["fullyQualifiedMethod"].Value
                        : match.Groups["method"].Value;
                })
                .Where(line => !string.IsNullOrEmpty(line));

            return matches;
        }
    }
}
