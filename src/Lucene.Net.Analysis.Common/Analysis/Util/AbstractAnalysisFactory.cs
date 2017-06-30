using Lucene.Net.Analysis.Core;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
            originalArgs = Collections.UnmodifiableMap(args);
            string version = Get(args, LUCENE_MATCH_VERSION_PARAM);
            // LUCENENET TODO: What should we do if the version is null?
            //luceneMatchVersion = version == null ? (LuceneVersion?)null : LuceneVersionHelpers.ParseLeniently(version);
            m_luceneMatchVersion = version == null ?
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT :
#pragma warning restore 612, 618
                LuceneVersionExtensions.ParseLeniently(version);
            args.Remove(CLASS_NAME); // consume the class arg
        }

        public IDictionary<string, string> OriginalArgs
        {
            get { return originalArgs; }
        }

        /// <summary>
        /// this method can be called in the <see cref="TokenizerFactory.Create(TextReader)"/>
        /// or <see cref="TokenFilterFactory.Create(TokenStream)"/> methods,
        /// to inform user, that for this factory a <see cref="m_luceneMatchVersion"/> is required 
        /// </summary>
        protected void AssureMatchVersion() // LUCENENET TODO: Remove this method (not used anyway in .NET)
        {
            // LUCENENET NOTE: since luceneMatchVersion can never be null in .NET,
            // this method effectively does nothing. However, leaving it in place because
            // it is used throughout Lucene.
            //if (luceneMatchVersion == null)
            //{
            //    throw new System.ArgumentException("Configuration Error: Factory '" + this.GetType().FullName + "' needs a 'luceneMatchVersion' parameter");
            //}
        }

        public LuceneVersion LuceneMatchVersion
        {
            get { return this.m_luceneMatchVersion; }
        }

        public virtual string Require(IDictionary<string, string> args, string name)
        {
            string s;
            if (!args.TryGetValue(name, out s))
            {
                throw new System.ArgumentException("Configuration Error: missing parameter '" + name + "'");
            }
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
            string s;
            if (!args.TryGetValue(name, out s) || s == null)
            {
                throw new ArgumentException("Configuration Error: missing parameter '" + name + "'");
            }

            args.Remove(name);
            foreach (var allowedValue in allowedValues)
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
            throw new ArgumentException("Configuration Error: '" + name + "' value must be one of " +
                                               allowedValues);
        }

        public virtual string Get(IDictionary<string, string> args, string name, string defaultVal = null)
        {
            string s;
            if (args.TryGetValue(name, out s))
                args.Remove(name);
            return s ?? defaultVal;
        }

        public virtual string Get(IDictionary<string, string> args, string name, ICollection<string> allowedValues)
        {
            return Get(args, name, allowedValues, null); // defaultVal = null
        }

        public virtual string Get(IDictionary<string, string> args, string name, ICollection<string> allowedValues, string defaultVal)
        {
            return Get(args, name, allowedValues, defaultVal, true);
        }

        public virtual string Get(IDictionary<string, string> args, string name, ICollection<string> allowedValues, string defaultVal, bool caseSensitive)
        {
            string s = null;
            if (!args.TryGetValue(name, out s) || s == null)
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
                throw new System.ArgumentException("Configuration Error: '" + name + "' value must be one of " +
                                                   allowedValues);
            }
        }

        /// <summary>
        /// NOTE: This was requireInt() in Lucene
        /// </summary>
        protected int RequireInt32(IDictionary<string, string> args, string name)
        {
            return int.Parse(Require(args, name));
        }

        /// <summary>
        /// NOTE: This was getInt() in Lucene
        /// </summary>
        protected int GetInt32(IDictionary<string, string> args, string name, int defaultVal)
        {
            string s;
            if (args.TryGetValue(name, out s))
            {
                args.Remove(name);
                return int.Parse(s);
            }
            return defaultVal;
        }

        protected bool RequireBoolean(IDictionary<string, string> args, string name)
        {
            return bool.Parse(Require(args, name));
        }

        protected bool GetBoolean(IDictionary<string, string> args, string name, bool defaultVal)
        {
            string s;
            if (args.TryGetValue(name, out s))
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
            return float.Parse(Require(args, name));
        }

        /// <summary>
        /// NOTE: This was getFloat() in Lucene
        /// </summary>
        protected float GetSingle(IDictionary<string, string> args, string name, float defaultVal)
        {
            string s;
            if (args.TryGetValue(name, out s))
            {
                args.Remove(name);
                return float.Parse(s);
            }
            return defaultVal;
        }

        public virtual char RequireChar(IDictionary<string, string> args, string name)
        {
            return Require(args, name)[0];
        }

        public virtual char GetChar(IDictionary<string, string> args, string name, char defaultVal)
        {
            string s;
            if (args.TryGetValue(name, out s))
            {
                args.Remove(name);
                if (s.Length != 1)
                {
                    throw new System.ArgumentException(name + " should be a char. \"" + s + "\" is invalid");
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
            string s;
            if (args.TryGetValue(name, out s))
            {
                args.Remove(name);
                HashSet<string> set = null;
                Match matcher = ITEM_PATTERN.Match(s);
                if (matcher.Success)
                {
                    set = new HashSet<string>();
                    set.Add(matcher.Groups[0].Value);
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
                throw new System.ArgumentException("Configuration Error: '" + name + "' can not be parsed in " + this.GetType().Name, e);
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
            string culture;
            if (args.TryGetValue(name, out culture))
            {
                args.Remove(name);
                try
                {
                    if (culture.Equals("invariant"))
                    {
                        return CultureInfo.InvariantCulture;
                    }
                    return new CultureInfo(culture);
                }
                catch (Exception e)
                {
                    throw new System.ArgumentException("Configuration Error: '" + name + "' can not be parsed in " + this.GetType().Name, e);
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
            if (files.Count() > 0)
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
            if (files.Count() > 0)
            {
                // default stopwords list has 35 or so words, but maybe don't make it that
                // big to start
                words = new CharArraySet(m_luceneMatchVersion, files.Count() * 10, ignoreCase);
                foreach (string file in files)
                {
                    using (Stream stream = loader.OpenResource(file.Trim()))
                    {
                        using (TextReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            WordlistLoader.GetSnowballWordSet(reader, words);
                        }
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
        protected IList<string> SplitFileNames(string fileNames)
        {
            if (fileNames == null)
            {
                return Collections.EmptyList<string>();
            }

            IList<string> result = new List<string>();
            foreach (string file in Regex.Split(fileNames, "(?<!\\\\),"))
            {
                result.Add(Regex.Replace(file, "\\\\(?=,)", ""));
            }

            return result;
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