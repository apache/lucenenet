using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Lucene.Net.Support
{
    public static class CultureInfoSupport
    {
        public static CultureInfo[] GetNeutralAndSpecificCultures()
        {
#if NETSTANDARD
            // Because any well-formed culture (xx-XXXX) will succeed in
            // Windows 10, even if the system does not support the culture,
            // it did not make sense to enumerate through the cultures.  We
            // workaround this by pinvoking into Win32 API to get the system
            // cultures.  When these tests are run on a non-Windows OS, the
            // Pinvoke will have to import a Linux DLL that supports the
            // same functionality.
            // See NativeMethods.EnumSystemLocalesEx for more information.
            var results = new ConcurrentBag<CultureInfo>();

            Func<string, uint, IntPtr, bool> addLocaleFunc = (localeName, dwFlags, lParam) => {
                var info = new CultureInfo(localeName);
                results.Add(info);
                return true;
            };

            uint flags = (uint)(NativeMethods.Locale.LOCALE_NEUTRALDATA | NativeMethods.Locale.LOCALE_SPECIFICDATA);
            var successful = NativeMethods.EnumSystemLocalesEx(
                new NativeMethods.EnumLocalesProcEx(addLocaleFunc),
                flags,
                IntPtr.Zero,
                IntPtr.Zero);

            var cultures = successful ? results.ToArray() : new CultureInfo[0];
#else
            var cultures =
                CultureInfo.GetCultures(CultureTypes.SpecificCultures | CultureTypes.NeutralCultures);
#endif
            return cultures;
        }
    }

    static class NativeMethods
    {
        /// <summary>
        /// Because of this issue: https://github.com/dotnet/corefx/issues/1669,
        /// it does not make sense to enumerate through the cultures.  We work
        /// around this by calling into Win32 API, EnumSystemLocalesEx to fetch
        /// the OS's cultures.
        /// This https://msdn.microsoft.com/en-us/library/windows/desktop/dd317829(v=vs.85).aspx
        /// </summary>
        [DllImport("Kernel32.dll")]
        public static extern bool EnumSystemLocalesEx(
            EnumLocalesProcEx lpLocaleEnumProcEx,
            uint dwFlags,
            IntPtr lParam,
            IntPtr lpReserved
            );

        public delegate bool EnumLocalesProcEx(
            [MarshalAs(UnmanagedType.LPWStr)]
            string lpLocaleString,
            uint dwFlags,
            IntPtr lParam);

        [Flags]
        public enum Locale
        {
            LOCALE_ALL = 0,                         // enumerate all named based locales
            LOCALE_WINDOWS = 0x00000001,            // shipped locales and/or replacements for them
            LOCALE_SUPPLEMENTAL = 0x00000002,       // supplemental locales only
            LOCALE_ALTERNATE_SORTS = 0x00000004,    // alternate sort locales
            LOCALE_REPLACEMENT = 0x00000008,        // locales that replace shipped locales (callback flag only)
            LOCALE_NEUTRALDATA = 0x00000010,        // Locales that are "neutral" (language only, region data is default)
            LOCALE_SPECIFICDATA = 0x00000020,       // Locales that contain language and region data
        }
    }
}
