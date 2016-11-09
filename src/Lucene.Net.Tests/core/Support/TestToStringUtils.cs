using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Lucene.Net.Core.Support
{
    /// <summary>
    /// This test was added for .NET compatibility - LUCENENET specific
    /// 
    /// It tests the Lucene.Net.Util.ToStringUtils which was untested in the Java counterpart,
    /// but required some help to ensure .NET compatibility.
    /// </summary>
    public class TestToStringUtils : LuceneTestCase
    {
        CultureInfo originalCulture;
        public override void SetUp()
        {
            base.SetUp();
            originalCulture = CultureInfo.CurrentCulture;
        }

        public override void TearDown()
        {
#if NET451
            Thread.CurrentThread.CurrentCulture = originalCulture;
#else
            CultureInfo.CurrentCulture = originalCulture;
#endif
            base.TearDown();
        }

        /// <summary>
        /// Check to ensure that the Boost function is properly converted in every possible culture.
        /// </summary>
        [Test, LuceneNetSpecific]
        public void TestBoost()
        {
            float boostNormal = 1f;
            float boostFractional = 2.5f;
            float boostNonFractional = 5f;
            float boostLong = 1.111111111f;
            float boostZeroNonFractional = 0f;
            float boostZeroFractional = 0.123f;

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
                var cultureInfo = new CultureInfo(localeName);
                results.Add(cultureInfo);
                return true;
            };

            uint flags = (uint)(NativeMethods.Locale.LOCALE_NEUTRALDATA | NativeMethods.Locale.LOCALE_SPECIFICDATA);
            var successful = NativeMethods.EnumSystemLocalesEx(
                new NativeMethods.EnumLocalesProcEx(addLocaleFunc), 
                flags,
                IntPtr.Zero,
                IntPtr.Zero);

            var cultures = successful ? results.ToArray() : Enumerable.Empty<CultureInfo>();
#else
            var cultures =
                CultureInfo.GetCultures(CultureTypes.SpecificCultures | CultureTypes.NeutralCultures);
#endif
            foreach (CultureInfo culture in cultures)
            {
#if NET451
                Thread.CurrentThread.CurrentCulture = culture;
#else
                CultureInfo.CurrentCulture = culture;
#endif
                assertEquals("", ToStringUtils.Boost(boostNormal));
                assertEquals("^2.5", ToStringUtils.Boost(boostFractional));
                assertEquals("^5.0", ToStringUtils.Boost(boostNonFractional));
                assertEquals("^1.111111", ToStringUtils.Boost(boostLong));
                assertEquals("^0.0", ToStringUtils.Boost(boostZeroNonFractional));
                assertEquals("^0.123", ToStringUtils.Boost(boostZeroFractional));
            }
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
