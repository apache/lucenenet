using System;

namespace Lucene.Net.Analysis.Util
{
    internal static class TypeExtensions
    {
        // LUCENENET TODO: Try to eliminate this class by using Assembly.GetManifestResourceStream(Type, string), if possible.
        // If not possible, perhaps we should move this and BufferedCharFilter into a Support namespace here in Analysis.Common ?

        /// <summary>
        /// LUCENENET specific:
        /// In .NET Core, resources are embedded with the namespace based on
        /// the physical location they are located in preceded by the name of
        /// the assembly.
        /// For example, the file: Analysis/Bg/stopwords.txt, would have the
        /// resource name `Lucene.Net.Analysis.Common.Analysis.Bg.stopwords.txt`
        /// Taken from: Lucene.Net.TestFramework.SystemTypesHelpers.getResourceAsStream
        /// </summary>
        internal static string GetAnalysisResourceName(this Type type, string filename)
        {
#if FEATURE_EMBEDDED_RESOURCE
            Assembly assembly = type.GetTypeInfo().Assembly;
            string namespaceSegment = type.Namespace.Replace("Lucene.Net", string.Empty);
            string assemblyName = assembly.GetName().Name;
            return string.Concat(assemblyName, namespaceSegment, ".", filename);
#else
            return string.Format("{0}.{1}", type.Namespace, filename);
#endif
        }
    }
}
