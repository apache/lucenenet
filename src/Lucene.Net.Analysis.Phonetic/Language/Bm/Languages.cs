// commons-codec version compatibility level: 1.9
using J2N;
using J2N.Collections.Generic.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Phonetic.Language.Bm
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Language codes.
    /// </summary>
    /// <remarks>
    /// Language codes are typically loaded from resource files. These are UTF-8 encoded text files. They are
    /// systematically named following the pattern:
    /// <c>Lucene.Net.Analysis.Phonetic.Language.Bm.<see cref="NameType"/>_languages.txt</c>
    /// <para/>
    /// The format of these resources is the following:
    /// <list type="bullet">
    ///     <item>
    ///         <term>Language:</term>
    ///         <description>A single string containing no whitespace.</description>
    ///     </item>
    ///     <item>
    ///         <term>End-of-line comments:</term>
    ///         <description>Any occurrence of '//' will cause all text following on that line to be discarded as a comment.</description>
    ///     </item>
    ///     <item>
    ///         <term>Multi-line comments:</term>
    ///         <description>Any line starting with '/*' will start multi-line commenting mode. This will skip all content until a line ending in '*' and '/' is found.</description>
    ///     </item>
    ///     <item>
    ///         <term>Blank lines:</term>
    ///         <description>All blank lines will be skipped.</description>
    ///     </item>
    /// </list>
    /// <para/>
    /// Ported from language.php
    /// <para/>
    /// This class is immutable and thread-safe.
    /// <para/>
    /// since 1.6
    /// </remarks>
    public class Languages
    {
        // Implementation note: This class is divided into two sections. The first part is a static factory interface that
        // exposes org/apache/commons/codec/language/bm/%s_languages.txt for %s in NameType.* as a list of supported
        // languages, and a second part that provides instance methods for accessing this set fo supported languages.

        public static readonly string ANY = "any";

        private static readonly IDictionary<NameType, Languages> LANGUAGES = LoadLanguages();

        private static IDictionary<NameType, Languages> LoadLanguages() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            IDictionary<NameType, Languages> LANGUAGES = new Dictionary<NameType, Languages>();
            foreach (NameType s in Enum.GetValues(typeof(NameType)))
            {
                LANGUAGES[s] = GetInstance(LangResourceName(s));
            }
            return LANGUAGES;
        }

        public static Languages GetInstance(NameType nameType)
        {
            LANGUAGES.TryGetValue(nameType, out Languages result);
            return result;
        }

        public static Languages GetInstance(string languagesResourceName)
        {
            // read languages list
            ISet<string> ls = new JCG.HashSet<string>();
            Stream langIS = typeof(Languages).FindAndGetManifestResourceStream(languagesResourceName);

            if (langIS is null)
            {
                throw new ArgumentException("Unable to resolve required resource: " + languagesResourceName);
            }

            using (TextReader reader = new StreamReader(langIS, ResourceConstants.ENCODING))
            {
                bool inExtendedComment = false;
                string rawLine;
                while ((rawLine = reader.ReadLine()) != null)
                {
                    string line = rawLine.Trim();
                    if (inExtendedComment)
                    {
                        if (line.EndsWith(ResourceConstants.EXT_CMT_END, StringComparison.Ordinal))
                        {
                            inExtendedComment = false;
                        }
                    }
                    else
                    {
                        if (line.StartsWith(ResourceConstants.EXT_CMT_START, StringComparison.Ordinal))
                        {
                            inExtendedComment = true;
                        }
                        else if (line.Length > 0)
                        {
                            ls.Add(line);
                        }
                    }
                }
            }

            return new Languages(ls.AsReadOnly());
        }

        private static string LangResourceName(NameType nameType)
        {
            return string.Format("{0}_languages.txt", nameType.GetName()); 
        }

        private readonly ISet<string> languages;

        private class NoLanguagesLanguageSet : LanguageSet
        {
            public override bool Contains(string language)
            {
                return false;
            }

            public override string GetAny()
            {
                throw new InvalidOperationException("Can't fetch any language from the empty language set.");
            }

            public override bool IsEmpty => true;

            public override bool IsSingleton => false;

            public override LanguageSet RestrictTo(LanguageSet other)
            {
                return this;
            }

            public override string ToString()
            {
                return "NO_LANGUAGES";
            }
        }

        /// <summary>
        /// No languages at all.
        /// </summary>
        public static readonly LanguageSet NO_LANGUAGES = new NoLanguagesLanguageSet();

        private class AnyLanguageLanguageSet : LanguageSet
        {
            public override bool Contains(string language)
            {
                return true;
            }

            public override string GetAny()
            {
                throw new InvalidOperationException("Can't fetch any language from the any language set.");
            }

            public override bool IsEmpty => false;

            public override bool IsSingleton => false;

            public override LanguageSet RestrictTo(LanguageSet other)
            {
                return other;
            }

            public override string ToString()
            {
                return "ANY_LANGUAGE";
            }
        }

        /// <summary>
        /// Any/all languages.
        /// </summary>
        public static readonly LanguageSet ANY_LANGUAGE = new AnyLanguageLanguageSet();

        private Languages(ISet<string> languages)
        {
            this.languages = languages;
        }

        public virtual ISet<string> GetLanguages() // LUCENENET NOTE: Kept as GetLanguages() because of naming conflict
        {
            return this.languages;
        }
    }

    /// <summary>
    /// A set of languages.
    /// </summary>
    public abstract class LanguageSet
    {

        public static LanguageSet From(ISet<string> langs)
        {
            return langs.Count == 0 ? Languages.NO_LANGUAGES : new SomeLanguages(langs);
        }

        public abstract bool Contains(string language);

        public abstract string GetAny();

        public abstract bool IsEmpty { get; }

        public abstract bool IsSingleton { get; }

        public abstract LanguageSet RestrictTo(LanguageSet other);
    }

    /// <summary>
    /// Some languages, explicitly enumerated.
    /// </summary>
    public sealed class SomeLanguages : LanguageSet
    {
        private readonly ISet<string> languages;

        internal SomeLanguages(ISet<string> languages)
        {
            this.languages = languages.AsReadOnly();
        }

        public override bool Contains(string language)
        {
            return this.languages.Contains(language);
        }

        public override string GetAny()
        {
            return this.languages.FirstOrDefault();
        }

        public ISet<string> GetLanguages()
        {
            return this.languages;
        }

        public override bool IsEmpty => this.languages.Count == 0;

        public override bool IsSingleton => this.languages.Count == 1;

        public override LanguageSet RestrictTo(LanguageSet other)
        {
            if (other == Languages.NO_LANGUAGES)
            {
                return other;
            }
            else if (other == Languages.ANY_LANGUAGE)
            {
                return this;
            }
            else
            {
                SomeLanguages sl = (SomeLanguages)other;
                ISet<string> ls = new JCG.HashSet<string>(Math.Min(languages.Count, sl.languages.Count));
                foreach (string lang in languages)
                {
                    if (sl.languages.Contains(lang))
                    {
                        ls.Add(lang);
                    }
                }
                return From(ls);
            }
        }

        public override string ToString()
        {
            return "Languages(" + languages.ToString() + ")";
        }
    }
}
