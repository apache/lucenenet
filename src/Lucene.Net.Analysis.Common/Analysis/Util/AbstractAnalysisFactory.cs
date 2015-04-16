using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Support;
using Lucene.Net.Util;
using org.apache.lucene.analysis.util;
using Reader = System.IO.TextReader;
using Version = Lucene.Net.Util.LuceneVersion;

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
    /// Abstract parent class for analysis factories <seealso cref="TokenizerFactory"/>,
    /// <seealso cref="TokenFilterFactory"/> and <seealso cref="CharFilterFactory"/>.
    /// <para>
    /// The typical lifecycle for a factory consumer is:
    /// <ol>
    ///   <li>Create factory via its constructor (or via XXXFactory.forName)
    ///   <li>(Optional) If the factory uses resources such as files, <seealso cref="ResourceLoaderAware#inform(ResourceLoader)"/> is called to initialize those resources.
    ///   <li>Consumer calls create() to obtain instances.
    /// </ol>
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
        protected internal readonly LuceneVersion? luceneMatchVersion;

        /// <summary>
        /// Initialize this factory via a set of key-value pairs.
        /// </summary>
        protected internal AbstractAnalysisFactory(IDictionary<string, string> args)
        {
            ExplicitLuceneMatchVersion = false;
            originalArgs = Collections.UnmodifiableMap(args);
            string version = get(args, LUCENE_MATCH_VERSION_PARAM);
            luceneMatchVersion = version == null ? (LuceneVersion?)null : LuceneVersionHelpers.ParseLeniently(version);
            args.Remove(CLASS_NAME); // consume the class arg
        }

        public IDictionary<string, string> OriginalArgs
        {
            get
            {
                return originalArgs;
            }
        }

        /// <summary>
        /// this method can be called in the <seealso cref="TokenizerFactory#create(java.io.Reader)"/>
        /// or <seealso cref="TokenFilterFactory#create(org.apache.lucene.analysis.TokenStream)"/> methods,
        /// to inform user, that for this factory a <seealso cref="#luceneMatchVersion"/> is required 
        /// </summary>
        protected internal void assureMatchVersion()
        {
            if (luceneMatchVersion == null)
            {
                throw new System.ArgumentException("Configuration Error: Factory '" + this.GetType().FullName + "' needs a 'luceneMatchVersion' parameter");
            }
        }

        public LuceneVersion? LuceneMatchVersion
        {
            get
            {
                return this.luceneMatchVersion;
            }
        }

        public virtual string require(IDictionary<string, string> args, string name)
        {
            string s = args.Remove(name);
            if (s == null)
            {
                throw new System.ArgumentException("Configuration Error: missing parameter '" + name + "'");
            }
            return s;
        }
        public virtual string require(IDictionary<string, string> args, string name, ICollection<string> allowedValues)
        {
            return require(args, name, allowedValues, true);
        }
        public virtual string require(IDictionary<string, string> args, string name, ICollection<string> allowedValues, bool caseSensitive)
        {
            string s = args.Remove(name);
            if (s == null)
            {
                throw new System.ArgumentException("Configuration Error: missing parameter '" + name + "'");
            }
            else
            {
                foreach (string allowedValue in allowedValues)
                {
                    if (caseSensitive)
                    {
                        if (s.Equals(allowedValue))
                        {
                            return s;
                        }
                    }
                    else
                    {
                        if (s.Equals(allowedValue, StringComparison.CurrentCultureIgnoreCase))
                        {
                            return s;
                        }
                    }
                }
                throw new System.ArgumentException("Configuration Error: '" + name + "' value must be one of " + allowedValues);
            }
        }
        public virtual string get(IDictionary<string, string> args, string name)
        {
            return args.Remove(name); // defaultVal = null
        }
        public virtual string get(IDictionary<string, string> args, string name, string defaultVal)
        {
            string s = args.Remove(name);
            return s == null ? defaultVal : s;
        }
        public virtual string get(IDictionary<string, string> args, string name, ICollection<string> allowedValues)
        {
            return get(args, name, allowedValues, null); // defaultVal = null
        }
        public virtual string get(IDictionary<string, string> args, string name, ICollection<string> allowedValues, string defaultVal)
        {
            return get(args, name, allowedValues, defaultVal, true);
        }
        public virtual string get(IDictionary<string, string> args, string name, ICollection<string> allowedValues, string defaultVal, bool caseSensitive)
        {
            string s = args.Remove(name);
            if (s == null)
            {
                return defaultVal;
            }
            else
            {
                foreach (string allowedValue in allowedValues)
                {
                    if (caseSensitive)
                    {
                        if (s.Equals(allowedValue))
                        {
                            return s;
                        }
                    }
                    else
                    {
                        if (s.Equals(allowedValue, StringComparison.CurrentCultureIgnoreCase))
                        {
                            return s;
                        }
                    }
                }
                throw new System.ArgumentException("Configuration Error: '" + name + "' value must be one of " + allowedValues);
            }
        }

        protected internal int requireInt(IDictionary<string, string> args, string name)
        {
            return int.Parse(require(args, name));
        }
        protected internal int getInt(IDictionary<string, string> args, string name, int defaultVal)
        {
            string s = args.Remove(name);
            return s == null ? defaultVal : int.Parse(s);
        }

        protected internal bool requireBoolean(IDictionary<string, string> args, string name)
        {
            return bool.Parse(require(args, name));
        }
        protected internal bool getBoolean(IDictionary<string, string> args, string name, bool defaultVal)
        {
            string s = args.Remove(name);
            return s == null ? defaultVal : bool.Parse(s);
        }

        protected internal float requireFloat(IDictionary<string, string> args, string name)
        {
            return float.Parse(require(args, name));
        }
        protected internal float getFloat(IDictionary<string, string> args, string name, float defaultVal)
        {
            string s = args.Remove(name);
            return s == null ? defaultVal : float.Parse(s);
        }

        public virtual char requireChar(IDictionary<string, string> args, string name)
        {
            return require(args, name)[0];
        }
        public virtual char getChar(IDictionary<string, string> args, string name, char defaultValue)
        {
            string s = args.Remove(name);
            if (s == null)
            {
                return defaultValue;
            }
            else
            {
                if (s.Length != 1)
                {
                    throw new System.ArgumentException(name + " should be a char. \"" + s + "\" is invalid");
                }
                else
                {
                    return s[0];
                }
            }
        }

        private static readonly Pattern ITEM_PATTERN = Pattern.compile("[^,\\s]+");

        /// <summary>
        /// Returns whitespace- and/or comma-separated set of values, or null if none are found </summary>
        public virtual HashSet<string> getSet(IDictionary<string, string> args, string name)
	  {
		string s = args.Remove(name);
		if (s == null)
		{
		 return null;
		}
		else
		{
		  HashSet<string> set = null;
		  Matcher matcher = ITEM_PATTERN.matcher(s);
		  if (matcher.find())
		  {
			set = new HashSet<>();
			set.Add(matcher.group(0));
			while (matcher.find())
			{
			  set.Add(matcher.group(0));
			}
		  }
		  return set;
		}
	  }

        /// <summary>
        /// Compiles a pattern for the value of the specified argument key <code>name</code> 
        /// </summary>
        protected internal Pattern GetPattern(IDictionary<string, string> args, string name)
        {
            try
            {
                return Pattern.compile(require(args, name));
            }
            catch (PatternSyntaxException e)
            {
                throw new System.ArgumentException("Configuration Error: '" + name + "' can not be parsed in " + this.GetType().Name, e);
            }
        }

        /// <summary>
        /// Returns as <seealso cref="CharArraySet"/> from wordFiles, which
        /// can be a comma-separated list of filenames
        /// </summary>
        protected internal CharArraySet GetWordSet(ResourceLoader loader, string wordFiles, bool ignoreCase)
        {
            assureMatchVersion();
            IList<string> files = splitFileNames(wordFiles);
            CharArraySet words = null;
            if (files.Count > 0)
            {
                // default stopwords list has 35 or so words, but maybe don't make it that
                // big to start
                words = new CharArraySet(luceneMatchVersion, files.Count * 10, ignoreCase);
                foreach (string file in files)
                {
                    var wlist = getLines(loader, file.Trim());
                    words.AddAll(StopFilter.makeStopSet(luceneMatchVersion, wlist, ignoreCase));
                }
            }
            return words;
        }

        /// <summary>
        /// Returns the resource's lines (with content treated as UTF-8)
        /// </summary>
        protected internal IList<string> getLines(ResourceLoader loader, string resource)
        {
            return WordlistLoader.getLines(loader.openResource(resource), StandardCharsets.UTF_8);
        }

        /// <summary>
        /// same as <seealso cref="#getWordSet(ResourceLoader, String, boolean)"/>,
        /// except the input is in snowball format. 
        /// </summary>
        protected internal CharArraySet getSnowballWordSet(ResourceLoader loader, string wordFiles, bool ignoreCase)
        {
            assureMatchVersion();
            IList<string> files = splitFileNames(wordFiles);
            CharArraySet words = null;
            if (files.Count > 0)
            {
                // default stopwords list has 35 or so words, but maybe don't make it that
                // big to start
                words = new CharArraySet(luceneMatchVersion, files.Count * 10, ignoreCase);
                foreach (string file in files)
                {
                    InputStream stream = null;
                    TextReader reader = null;
                    try
                    {
                        stream = loader.openResource(file.Trim());
                        CharsetDecoder decoder = StandardCharsets.UTF_8.newDecoder().onMalformedInput(CodingErrorAction.REPORT).onUnmappableCharacter(CodingErrorAction.REPORT);
                        reader = new InputStreamReader(stream, decoder);
                        WordlistLoader.getSnowballWordSet(reader, words);
                    }
                    finally
                    {
                        IOUtils.closeWhileHandlingException(reader, stream);
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
        protected internal IList<string> splitFileNames(string fileNames)
        {
            if (fileNames == null)
            {
                return System.Linq.Enumerable.Empty<string>();
            }

            IList<string> result = new List<string>();
            foreach (string file in fileNames.Split("(?<!\\\\),", true))
            {
                result.Add(file.replaceAll("\\\\(?=,)", ""));
            }

            return result;
        }

        private const string CLASS_NAME = "class";

        /// <returns> the string used to specify the concrete class name in a serialized representation: the class arg.  
        ///         If the concrete class name was not specified via a class arg, returns {@code getClass().getName()}. </returns>
        public virtual string ClassArg
        {
            get
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
        }

        public virtual bool ExplicitLuceneMatchVersion { get; set; }
    }
}