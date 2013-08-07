using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Util
{
    public abstract class StopwordAnalyzerBase : Analyzer
    {
        protected readonly CharArraySet stopwords;

        protected readonly Version? matchVersion;

        public CharArraySet StopwordSet
        {
            get
            {
                return stopwords;
            }
        }

        protected StopwordAnalyzerBase(Version? version, CharArraySet stopwords)
        {
            matchVersion = version;
            // analyzers should use char array set for stopwords!
            this.stopwords = stopwords == null ? CharArraySet.EMPTY_SET : CharArraySet
                .UnmodifiableSet(CharArraySet.Copy(version, stopwords));
        }

        protected StopwordAnalyzerBase(Version? version)
            : this(version, null)
        {
        }

        protected static CharArraySet LoadStopwordSet(bool ignoreCase, Type aClass, string resource, string comment)
        {
            TextReader reader = null;
            try
            {
                reader = IOUtils.GetDecodingReader(aClass.Assembly.GetManifestResourceStream(resource), IOUtils.CHARSET_UTF_8);
                return WordlistLoader.GetWordSet(reader, comment, new CharArraySet(Version.LUCENE_31, 16, ignoreCase));
            }
            finally
            {
                IOUtils.Close(reader);
            }
        }

        protected static CharArraySet LoadStopwordSet(Stream stopwords, Version? matchVersion)
        {
            TextReader reader = null;
            try
            {
                reader = IOUtils.GetDecodingReader(stopwords, IOUtils.CHARSET_UTF_8);
                return WordlistLoader.GetWordSet(reader, matchVersion);
            }
            finally
            {
                IOUtils.Close(reader);
            }
        }

        protected static CharArraySet LoadStopwordSet(TextReader stopwords, Version? matchVersion)
        {
            try
            {
                return WordlistLoader.GetWordSet(stopwords, matchVersion);
            }
            finally
            {
                IOUtils.Close(stopwords);
            }
        }

        public abstract override Analyzer.TokenStreamComponents CreateComponents(string fieldName, System.IO.TextReader reader);
    }
}
