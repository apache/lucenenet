// Lucene version compatibility level 4.8.1
using J2N.Collections.Generic.Extensions;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Util
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
    /// Abstract parent class for analysis factories <see cref="TokenizerFactory"/>,
    /// <see cref="TokenFilterFactory"/> and <see cref="CharFilterFactory"/>.
    /// <para>
    /// The typical lifecycle for a factory consumer is:
    /// <list type="bullet">
    ///     <item><description>Create factory via its constructor (or via XXXFactory.ForName)</description></item>
    ///     <item><description>(Optional) If the factory uses resources such as files, 
    ///         <see cref="IResourceLoaderAware.Inform(IResourceLoader)"/> is called to initialize those resources.</description></item>
    ///     <item><description>Consumer calls create() to obtain instances.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public abstract class AbstractAnalysisFactory
    {
        public const string LUCENE_MATCH_VERSION_PARAM = "luceneMatchVersion";

        /// <summary>
        /// The original args, before any processing </summary>
        private readonly IDictionary<string, string> originalArgs;

        /// <summary>
        /// the luceneVersion arg </summary>
        protected readonly LuceneVersion m_luceneMatchVersion;

        /// <summary>
        /// Initialize this factory via a set of key-value pairs.
        /// </summary>
        protected AbstractAnalysisFactory(IDictionary<string, string> args)
        {
            IsExplicitLuceneMatchVersion = false;
            originalArgs = args.AsReadOnly();
            string version = Get(args, LUCENE_MATCH_VERSION_PARAM);
            // LUCENENET TODO: What should we do if the version is null?
            //luceneMatchVersion = version is null ? (LuceneVersion?)null : LuceneVersionHelpers.ParseLeniently(version);
            m_luceneMatchVersion = version is null ?
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT :
#pragma warning restore 612, 618
                LuceneVersionExtensions.ParseLeniently(version);
            args.Remove(CLASS_NAME); // consume the class arg
        }

        public IDictionary<string, string> OriginalArgs => originalArgs;

        /// <summary>
        /// this method can be called in the <see cref="TokenizerFactory.Create(TextReader)"/>
        /// or <see cref="TokenFilterFactory.Create(TokenStream)"/> methods,
        /// to inform user, that for this factory a <see cref="m_luceneMatchVersion"/> is required 
        /// </summary>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design")]
        protected void AssureMatchVersion() // LUCENENET TODO: Remove this method (not used anyway in .NET)
        {
            // LUCENENET NOTE: since luceneMatchVersion can never be null in .NET,
            // this method effectively does nothing. However, leaving it in place because
            // it is used throughout Lucene.
            //if (luceneMatchVersion is null)
            //{
            //    throw new ArgumentException("Configuration Error: Factory '" + this.GetType().FullName + "' needs a 'luceneMatchVersion' parameter");
            //}
        }

        public LuceneVersion LuceneMatchVersion => this.m_luceneMatchVersion;

        public virtual string Require(IDictionary<string, string> args, string name)
        {
            if (!args.TryGetValue(name, out string s))
                throw new ArgumentException($"Configuration Error: missing parameter '{name}'");
            args.Remove(name);
            return s;
        }
        public virtual string Require(IDictionary<string, string> args, string name, ICollection<string> allowedValues)
        {
            return Require(args, name, allowedValues, true);
        }

        public virtual string Require(IDictionary<string, string> args, string name, ICollection<string> allowedValues,
            bool caseSensitive)
        {
            if (!args.TryGetValue(name, out string s))
                throw new ArgumentException($"Configuration Error: missing parameter '{name}'");
            args.Remove(name);
            foreach (var allowedValue in allowedValues)
            {
                if (caseSensitive)
                {
                    if (s.Equals(allowedValue, StringComparison.Ordinal))
                        return s;
                }
                else
                {
                    if (s.Equals(allowedValue, StringComparison.OrdinalIgnoreCase))
                        return s;
                }
            }
            throw new ArgumentException($"Configuration Error: '{name}' value must be one of {Collections.ToString(allowedValues)}");
        }

        // LUCENENET specific - S1699 - marked non-virtual because calling
        // virtual members from the constructor is not a safe operation in .NET
        public string Get(IDictionary<string, string> args, string name, string defaultVal = null)
        {
            if (args.TryGetValue(name, out string s))
                args.Remove(name);
            return s ?? defaultVal;
        }

        // LUCENENET specific - S1699 - marked non-virtual because calling
        // virtual members from the constructor is not a safe operation in .NET
        public string Get(IDictionary<string, string> args, string name, ICollection<string> allowedValues)
        {
            return Get(args, name, allowedValues, defaultVal: null);
        }

        // LUCENENET specific - S1699 - marked non-virtual because calling
        // virtual members from the constructor is not a safe operation in .NET
        public string Get(IDictionary<string, string> args, string name, ICollection<string> allowedValues, string defaultVal)
        {
            return Get(args, name, allowedValues, defaultVal, caseSensitive: true);
        }

        // LUCENENET specific - S1699 - marked non-virtual because calling
        // virtual members from the constructor is not a safe operation in .NET
        public string Get(IDictionary<string, string> args, string name, ICollection<string> allowedValues, string defaultVal, bool caseSensitive)
        {
            if (!args.TryGetValue(name, out string s) || s is null)
            {
                return defaultVal;
            }
            else
            {
                args.Remove(name);
                foreach (string allowedValue in allowedValues)
                {
                    if (caseSensitive)
                    {
                        if (s.Equals(allowedValue, StringComparison.Ordinal))
                        {
                            return s;
                        }
                    }
                    else
                    {
                        if (s.Equals(allowedValue, StringComparison.OrdinalIgnoreCase))
                        {
                            return s;
                        }
                    }
                }
                throw new ArgumentException($"Configuration Error: '{name}' value must be one of {Collections.ToString(allowedValues)}");
            }
        }

        /// <summary>
        /// NOTE: This was requireInt() in Lucene
        /// </summary>
        protected int RequireInt32(IDictionary<string, string> args, string name)
        {
            return int.Parse(Require(args, name), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// NOTE: This was getInt() in Lucene
        /// </summary>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design")]
        protected int GetInt32(IDictionary<string, string> args, string name, int defaultVal)
        {
            if (args.TryGetValue(name, out string s))
            {
                args.Remove(name);
                return int.Parse(s, CultureInfo.InvariantCulture);
            }
            return defaultVal;
        }

        protected bool RequireBoolean(IDictionary<string, string> args, string name)
        {
            return bool.Parse(Require(args, name));
        }

        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design")]
        protected bool GetBoolean(IDictionary<string, string> args, string name, bool defaultVal)
        {
            if (args.TryGetValue(name, out string s))
            {
                args.Remove(name);
                return bool.Parse(s);
            }
            return defaultVal;
        }

        /// <summary>
        /// NOTE: This was requireFloat() in Lucene
        /// </summary>
        protected float RequireSingle(IDictionary<string, string> args, string name)
        {
            return float.Parse(Require(args, name), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// NOTE: This was getFloat() in Lucene
        /// </summary>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design")]
        protected float GetSingle(IDictionary<string, string> args, string name, float defaultVal)
        {
            if (args.TryGetValue(name, out string s))
            {
                args.Remove(name);
                return float.Parse(s, CultureInfo.InvariantCulture);
            }
            return defaultVal;
        }

        public virtual char RequireChar(IDictionary<string, string> args, string name)
        {
            return Require(args, name)[0];
        }

        public virtual char GetChar(IDictionary<string, string> args, string name, char defaultVal)
        {
            if (args.TryGetValue(name, out string s))
            {
                args.Remove(name);
                if (s.Length != 1)
                {
                    throw new ArgumentException($"{name} should be a char. \"{s}\" is invalid");
                }
                else
                {
                    return s[0];
                }
            }
            return defaultVal;
        }

        private static readonly Regex ITEM_PATTERN = new Regex("[^,\\s]+", RegexOptions.Compiled);

        /// <summary>
        /// Returns whitespace- and/or comma-separated set of values, or null if none are found </summary>
        public virtual ISet<string> GetSet(IDictionary<string, string> args, string name)
        {
            if (args.TryGetValue(name, out string s))
            {
                args.Remove(name);
                ISet<string> set = null;
                Match matcher = ITEM_PATTERN.Match(s);
                if (matcher.Success)
                {
                    set = new JCG.HashSet<string>
                    {
                        matcher.Groups[0].Value
                    };
                    matcher = matcher.NextMatch();
                    while (matcher.Success)
                    {
                        set.Add(matcher.Groups[0].Value);
                        matcher = matcher.NextMatch();
                    }
                }
                return set;
            }
            return null;
        }

        /// <summary>
        /// Compiles a pattern for the value of the specified argument key <paramref name="name"/> 
        /// </summary>
        protected Regex GetPattern(IDictionary<string, string> args, string name)
        {
            try
            {
                return new Regex(Require(args, name), RegexOptions.Compiled);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Configuration Error: '" + name + "' can not be parsed in " + this.GetType().Name, e);
            }
        }

        /// <summary>
        /// Gets a <see cref="CultureInfo"/> value of the specified argument key <paramref name="name"/>.
        /// <para/>
        /// To specify the invariant culture, pass the string <c>"invariant"</c>.
        /// <para/>
        /// LUCENENET specific
        /// </summary>
        protected CultureInfo GetCulture(IDictionary<string, string> args, string name, CultureInfo defaultVal)
        {
            if (args.TryGetValue(name, out string culture))
            {
                args.Remove(name);
                try
                {
                    if (culture.Equals("invariant", StringComparison.Ordinal))
                    {
                        return CultureInfo.InvariantCulture;
                    }
                    return new CultureInfo(culture);
                }
                catch (Exception e)
                {
                    throw new ArgumentException("Configuration Error: '" + name + "' can not be parsed in " + this.GetType().Name, e);
                }
            }
            return defaultVal;
        }

        /// <summary>
        /// Returns as <see cref="CharArraySet"/> from wordFiles, which
        /// can be a comma-separated list of filenames
        /// </summary>
        protected CharArraySet GetWordSet(IResourceLoader loader, string wordFiles, bool ignoreCase)
        {
            AssureMatchVersion();
            IList<string> files = SplitFileNames(wordFiles);
            CharArraySet words = null;
            if (files.Count > 0)
            {
                // default stopwords list has 35 or so words, but maybe don't make it that
                // big to start
                words = new CharArraySet(m_luceneMatchVersion, files.Count * 10, ignoreCase);
                foreach (string file in files)
                {
                    var wlist = GetLines(loader, file.Trim());
                    words.UnionWith(StopFilter.MakeStopSet(m_luceneMatchVersion, wlist, ignoreCase));
                }
            }
            return words;
        }

        /// <summary>
        /// Returns the resource's lines (with content treated as UTF-8)
        /// </summary>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design")]
        protected IList<string> GetLines(IResourceLoader loader, string resource)
        {
            return WordlistLoader.GetLines(loader.OpenResource(resource), Encoding.UTF8);
        }

        /// <summary>
        /// Same as <see cref="GetWordSet(IResourceLoader, string, bool)"/>,
        /// except the input is in snowball format. 
        /// </summary>
        protected CharArraySet GetSnowballWordSet(IResourceLoader loader, string wordFiles, bool ignoreCase)
        {
            AssureMatchVersion();
            IList<string> files = SplitFileNames(wordFiles);
            CharArraySet words = null;
            if (files.Count > 0)
            {
                // default stopwords list has 35 or so words, but maybe don't make it that
                // big to start
                words = new CharArraySet(m_luceneMatchVersion, files.Count * 10, ignoreCase);
                foreach (string file in files)
                {
                    using (Stream stream = loader.OpenResource(file.Trim()))
                    using (TextReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        WordlistLoader.GetSnowballWordSet(reader, words);
                    }
                }
            }
            return words;
        }

        /// <summary>
        /// Splits file names separated by comma character.
        /// File names can contain comma characters escaped by backslash '\'
        /// </summary>
        /// <param name="fileNames"> the string containing file names </param>
        /// <returns> a list of file names with the escaping backslashed removed </returns>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design")]
        protected IList<string> SplitFileNames(string fileNames)
        {
            if (fileNames is null)
            {
                return Collections.EmptyList<string>();
            }

            IList<string> result = new JCG.List<string>();
            foreach (string file in SplitFileNameHolder.FILE_SPLIT_PATTERN.Split(fileNames))
            {
                result.Add(SplitFileNameHolder.FILE_REPLACE_PATTERN.Replace(file, string.Empty));
            }

            return result;
        }

        // LUCENENET specific - optimize compilation and lazy-load the regular expressions
        private static class SplitFileNameHolder
        {
            public static readonly Regex FILE_SPLIT_PATTERN = new Regex("(?<!\\\\),", RegexOptions.Compiled | RegexOptions.CultureInvariant);
            public static readonly Regex FILE_REPLACE_PATTERN = new Regex("\\\\(?=,)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        private const string CLASS_NAME = "class";

        /// <returns> the string used to specify the concrete class name in a serialized representation: the class arg.  
        ///         If the concrete class name was not specified via a class arg, returns <c>GetType().Name</c>. </returns>
        public virtual string GetClassArg()
        {
            if (null != originalArgs)
            {
                string className = originalArgs[CLASS_NAME];
                if (null != className)
                {
                    return className;
                }
            }
            return this.GetType().Name;
        }

        public virtual bool IsExplicitLuceneMatchVersion { get; set; }
    }
}