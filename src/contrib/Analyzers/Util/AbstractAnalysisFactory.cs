using Lucene.Net.Analysis.Core;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Util
{
    public abstract class AbstractAnalysisFactory
    {
        public const string LUCENE_MATCH_VERSION_PARAM = "luceneMatchVersion";

        private readonly IDictionary<string, string> originalArgs;

        protected readonly Lucene.Net.Util.Version? luceneMatchVersion;

        private bool isExplicitLuceneMatchVersion = false;

        protected AbstractAnalysisFactory(IDictionary<string, string> args)
        {
            originalArgs = new HashMap<String, String>(args);
            String version = Get(args, LUCENE_MATCH_VERSION_PARAM);
            luceneMatchVersion = version == null ? (Lucene.Net.Util.Version?)null : version.ParseLeniently();
            args.Remove(CLASS_NAME);  // consume the class arg
        }

        public IDictionary<string, string> OriginalArgs
        {
            get
            {
                return originalArgs;
            }
        }

        protected void AssureMatchVersion()
        {
            if (luceneMatchVersion == null)
            {
                throw new ArgumentException("Configuration Error: Factory '" + this.GetType().FullName +
                  "' needs a 'luceneMatchVersion' parameter");
            }
        }

        public Lucene.Net.Util.Version? LuceneMatchVersion
        {
            get
            {
                return this.luceneMatchVersion;
            }
        }

        public virtual string Require(IDictionary<string, string> args, string name)
        {
            string s = args[name];

            if (s == null)
            {
                throw new ArgumentException("Configuration Error: missing parameter '" + name + "'");
            }

            args.Remove(name);

            return s;
        }

        public virtual string Require(IDictionary<string, string> args, string name, ICollection<string> allowedValues)
        {
            return Require(args, name, allowedValues, true);
        }

        public virtual string Require(IDictionary<string, string> args, string name, ICollection<string> allowedValues, bool caseSensitive)
        {
            string s = args[name];
            if (s == null)
            {
                throw new ArgumentException("Configuration Error: missing parameter '" + name + "'");
            }
            else
            {
                args.Remove(name);
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
                        if (s.EqualsIgnoreCase(allowedValue))
                        {
                            return s;
                        }
                    }
                }
                throw new ArgumentException("Configuration Error: '" + name + "' value must be one of " + allowedValues);
            }
        }

        public virtual string Get(IDictionary<string, string> args, string name)
        {
            string temp = args[name];
            args.Remove(name);
            return temp; // defaultVal = null
        }

        public virtual string Get(IDictionary<string, string> args, string name, string defaultVal)
        {
            string s = args[name];
            args.Remove(name);
            return s == null ? defaultVal : s;
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
            string s = args[name];
            args.Remove(name);
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
                        if (s.EqualsIgnoreCase(allowedValue))
                        {
                            return s;
                        }
                    }
                }
                throw new ArgumentException("Configuration Error: '" + name + "' value must be one of " + allowedValues);
            }
        }

        protected int RequireInt(IDictionary<string, string> args, string name)
        {
            return int.Parse(Require(args, name));
        }

        protected int GetInt(IDictionary<string, string> args, string name, int defaultVal)
        {
            string s = args[name];
            args.Remove(name);
            return s == null ? defaultVal : int.Parse(s);
        }

        protected bool RequireBoolean(IDictionary<string, string> args, string name)
        {
            return bool.Parse(Require(args, name));
        }

        protected bool GetBoolean(IDictionary<string, string> args, string name, bool defaultVal)
        {
            string s = args[name];
            args.Remove(name);
            return s == null ? defaultVal : bool.Parse(s);
        }

        protected float RequireFloat(IDictionary<string, string> args, string name)
        {
            return float.Parse(Require(args, name));
        }

        protected float GetFloat(IDictionary<string, string> args, string name, float defaultVal)
        {
            string s = args[name];
            args.Remove(name);
            return s == null ? defaultVal : float.Parse(s);
        }

        public virtual char RequireChar(IDictionary<string, string> args, string name)
        {
            return Require(args, name)[0];
        }

        public virtual char GetChar(IDictionary<string, string> args, string name, char defaultValue)
        {
            string s = args[name];
            args.Remove(name);
            if (s == null)
            {
                return defaultValue;
            }
            else
            {
                if (s.Length != 1)
                {
                    throw new ArgumentException(name + " should be a char. \"" + s + "\" is invalid");
                }
                else
                {
                    return s[0];
                }
            }
        }

        private static readonly Regex ITEM_PATTERN = new Regex("[^,\\s]+", RegexOptions.Compiled);

        public virtual ISet<string> GetSet(IDictionary<string, string> args, string name)
        {
            string s = args[name];
            args.Remove(name);

            if (s == null)
            {
                return null;
            }
            else
            {
                ISet<String> set = null;
                MatchCollection matcher = ITEM_PATTERN.Matches(s);

                // .NET Port: this code is a bit different than the java version due to Regex vs Pattern APIs
                foreach (Match match in matcher)
                {
                    if (set == null) set = new HashSet<string>();

                    set.Add(match.Groups[0].Value);
                }
                return set;
            }
        }

        protected Regex GetPattern(IDictionary<string, string> args, string name)
        {
            try
            {
                return new Regex(Require(args, name), RegexOptions.Compiled);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException
                  ("Configuration Error: '" + name + "' can not be parsed in " +
                   this.GetType().Name, e);
            }
        }

        protected CharArraySet GetWordSet(IResourceLoader loader, string wordFiles, bool ignoreCase)
        {
            AssureMatchVersion();
            IList<String> files = SplitFileNames(wordFiles);
            CharArraySet words = null;
            if (files.Count > 0)
            {
                // default stopwords list has 35 or so words, but maybe don't make it that
                // big to start
                words = new CharArraySet(luceneMatchVersion,
                    files.Count * 10, ignoreCase);
                foreach (String file in files)
                {
                    IList<String> wlist = GetLines(loader, file.Trim());
                    words.UnionWith(StopFilter.MakeStopSet(luceneMatchVersion, wlist.Cast<object>().ToList(),
                        ignoreCase));
                }
            }
            return words;
        }

        protected IList<string> GetLines(IResourceLoader loader, string resource)
        {
            return WordlistLoader.GetLines(loader.OpenResource(resource), IOUtils.CHARSET_UTF_8);
        }

        protected CharArraySet GetSnowballWordSet(IResourceLoader loader, string wordFiles, bool ignoreCase)
        {
            AssureMatchVersion();
            IList<String> files = SplitFileNames(wordFiles);
            CharArraySet words = null;
            if (files.Count > 0)
            {
                // default stopwords list has 35 or so words, but maybe don't make it that
                // big to start
                words = new CharArraySet(luceneMatchVersion,
                    files.Count * 10, ignoreCase);
                foreach (String file in files)
                {
                    Stream stream = null;
                    TextReader reader = null;
                    try
                    {
                        stream = loader.OpenResource(file.Trim());
                        var decoder = IOUtils.CHARSET_UTF_8;
                        reader = new StreamReader(stream, decoder);
                        WordlistLoader.GetSnowballWordSet(reader, words);
                    }
                    finally
                    {
                        IOUtils.CloseWhileHandlingException((IDisposable)reader, stream);
                    }
                }
            }
            return words;
        }

        protected IList<String> SplitFileNames(string fileNames)
        {
            if (fileNames == null)
                return new string[0];

            List<String> result = new List<String>();
            foreach (String file in fileNames.Split(new[] { "(?<!\\\\)," }, StringSplitOptions.RemoveEmptyEntries))
            {
                result.Add(file.Replace("\\\\(?=,)", ""));
            }

            return result;
        }

        private const string CLASS_NAME = "class";

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
            return GetType().FullName;
        }

        public virtual bool IsExplicitLuceneMatchVersion
        {
            get { return isExplicitLuceneMatchVersion; }
            set { isExplicitLuceneMatchVersion = value; }
        }   
    }
}
