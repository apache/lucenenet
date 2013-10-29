using Lucene.Net.Support.BreakIterators;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support
{
    /// <summary>
    /// A backfill for BreakIterator support, since .NET doesn't have it. This implementation
    /// certainly will need improving.
    /// </summary>
    public abstract class BreakIterator : ICloneable
    {
        public const int DONE = -1;

        private static ISet<CultureInfo> _allLocales = new HashSet<CultureInfo>();
        private static IDictionary<CultureInfo, Type> _lineTypes = new Dictionary<CultureInfo, Type>();
        private static IDictionary<CultureInfo, Type> _characterTypes = new Dictionary<CultureInfo, Type>();
        private static IDictionary<CultureInfo, Type> _sentenceTypes = new Dictionary<CultureInfo, Type>();
        private static IDictionary<CultureInfo, Type> _wordTypes = new Dictionary<CultureInfo, Type>();

        static BreakIterator()
        {
            // HACK HACK HACK HACK HACK
            CultureInfo invariant = CultureInfo.InvariantCulture;
            CultureInfo english = CultureInfo.GetCultureInfoByIetfLanguageTag("en");
            CultureInfo englishUs = CultureInfo.GetCultureInfoByIetfLanguageTag("en-US");
            
            RegisterCharacterBreakIterator<EnglishCharacterBreakIterator>(invariant);
            RegisterCharacterBreakIterator<EnglishCharacterBreakIterator>(english);
            RegisterCharacterBreakIterator<EnglishCharacterBreakIterator>(englishUs);
            RegisterLineBreakIterator<EnglishLineBreakIterator>(invariant);
            RegisterLineBreakIterator<EnglishLineBreakIterator>(english);
            RegisterLineBreakIterator<EnglishLineBreakIterator>(englishUs);
            RegisterSentenceBreakIterator<EnglishSentenceBreakIterator>(invariant);
            RegisterSentenceBreakIterator<EnglishSentenceBreakIterator>(english);
            RegisterSentenceBreakIterator<EnglishSentenceBreakIterator>(englishUs);
            RegisterWordBreakIterator<EnglishWordBreakIterator>(invariant);
            RegisterWordBreakIterator<EnglishWordBreakIterator>(english);
            RegisterWordBreakIterator<EnglishWordBreakIterator>(englishUs);
        }

        protected BreakIterator()
        {
        }
        
        public virtual object Clone()
        {
            return this.MemberwiseClone();
        }

        public abstract int Current();

        public abstract int First();

        public abstract int Following(int offset);

        public abstract string Text { get; set;  }

        public virtual bool IsBoundary(int offset)
        {
            return false;
        }

        public abstract int Last();

        public abstract int Next();

        public abstract int Next(int n);

        public virtual int Preceding(int offset)
        {
            // goal here is to get the last boundary before the offset. 
            // so we start at the boundary Following() the offset,
            // then go Previous() to find the next one before the offset.
            
            var indexAfter = Following(offset);

            // check for no more boundaries
            if (indexAfter == DONE)
                return DONE;

            return Previous(); // doesn't matter if DONE or not DONE, caller will decide what to do
        }

        public abstract int Previous();

        public static CultureInfo[] GetAvailableLocales()
        {
            return _allLocales.ToArray();
        }

        public static BreakIterator GetCharacterInstance()
        {
            return GetCharacterInstance(CultureInfo.InvariantCulture);
        }

        public static BreakIterator GetCharacterInstance(CultureInfo locale)
        {
            Type it;

            if (_characterTypes.TryGetValue(locale, out it))
                return (BreakIterator)Activator.CreateInstance(it);

            it = _characterTypes[CultureInfo.InvariantCulture];

            return (BreakIterator)Activator.CreateInstance(it);
        }

        public static BreakIterator GetLineInstance()
        {
            return GetLineInstance(CultureInfo.InvariantCulture);
        }

        public static BreakIterator GetLineInstance(CultureInfo locale)
        {
            Type it;

            if (_lineTypes.TryGetValue(locale, out it))
                return (BreakIterator)Activator.CreateInstance(it);

            it = _lineTypes[CultureInfo.InvariantCulture];

            return (BreakIterator)Activator.CreateInstance(it);
        }

        public static BreakIterator GetSentenceInstance()
        {
            return GetSentenceInstance(CultureInfo.InvariantCulture);
        }

        public static BreakIterator GetSentenceInstance(CultureInfo locale)
        {
            Type it;

            if (_sentenceTypes.TryGetValue(locale, out it))
                return (BreakIterator)Activator.CreateInstance(it);

            it = _sentenceTypes[CultureInfo.InvariantCulture];

            return (BreakIterator)Activator.CreateInstance(it);
        }

        public static BreakIterator GetWordInstance()
        {
            return GetWordInstance(CultureInfo.InvariantCulture);
        }

        public static BreakIterator GetWordInstance(CultureInfo locale)
        {
            Type it;

            if (_wordTypes.TryGetValue(locale, out it))
                return (BreakIterator)Activator.CreateInstance(it);

            it = _wordTypes[CultureInfo.InvariantCulture];

            return (BreakIterator)Activator.CreateInstance(it);
        }

        public static void RegisterCharacterBreakIterator<T>(CultureInfo locale)
            where T : BreakIterator, new()
        {
            _allLocales.Add(locale);
            _characterTypes[locale] = typeof(T);
        }

        public static void RegisterLineBreakIterator<T>(CultureInfo locale)
            where T : BreakIterator, new()
        {
            _allLocales.Add(locale);
            _lineTypes[locale] = typeof(T);
        }

        public static void RegisterSentenceBreakIterator<T>(CultureInfo locale)
            where T : BreakIterator, new()
        {
            _allLocales.Add(locale);
            _sentenceTypes[locale] = typeof(T);
        }

        public static void RegisterWordBreakIterator<T>(CultureInfo locale)
            where T : BreakIterator, new()
        {
            _allLocales.Add(locale);
            _wordTypes[locale] = typeof(T);
        }
    }
}
