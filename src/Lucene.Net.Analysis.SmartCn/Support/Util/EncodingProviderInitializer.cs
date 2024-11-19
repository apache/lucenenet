using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Lucene.Net.Util
{
    /// <summary>
    /// Loads the <see cref="System.Text.EncodingProvider"/> for the current runtime for support of
    /// GB2312 encoding.
    /// </summary>
    internal static class EncodingProviderInitializer
    {
        private static int initialized;

        private static bool IsNetFramework =>
#if NETSTANDARD2_0
            RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);
#elif NET40_OR_GREATER
            true;
#else
            false;
#endif

        [Conditional("FEATURE_ENCODINGPROVIDERS")]
        public static void EnsureInitialized()
        {
            // Only allow a single thread to call this
            if (0 != Interlocked.CompareExchange(ref initialized, 1, 0)) return;

#if FEATURE_ENCODINGPROVIDERS
            if (!IsNetFramework)
            {
                Initialize();
            }
#endif
        }

#if FEATURE_ENCODINGPROVIDERS
        // NOTE: CodePagesEncodingProvider.Instance loads early, so we need this in a separate method to ensure
        // that it isn't executed until after we know which runtime we are on.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Initialize()
        {
            // Support for GB2312 encoding. See: https://docs.microsoft.com/en-us/dotnet/api/system.text.codepagesencodingprovider?view=netcore-2.0
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
#endif
    }
}
