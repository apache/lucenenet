using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Util
{
    public static class WordlistLoader
    {
        private const int INITIAL_CAPACITY = 16;

        public static CharArraySet GetWordSet(TextReader reader, CharArraySet result)
        {
            //BufferedReader br = null;
            try
            {
                //br = getBufferedReader(reader);
                String word = null;
                while ((word = reader.ReadLine()) != null)
                {
                    result.Add(word.Trim());
                }
            }
            finally
            {
                //IOUtils.Close(reader);
            }
            return result;
        }

        public static CharArraySet GetWordSet(TextReader reader, Lucene.Net.Util.Version matchVersion)
        {
            return GetWordSet(reader, new CharArraySet(matchVersion, INITIAL_CAPACITY, false));
        }

        public static CharArraySet GetWordSet(TextReader reader, String comment, Lucene.Net.Util.Version matchVersion)
        {
            return GetWordSet(reader, comment, new CharArraySet(matchVersion, INITIAL_CAPACITY, false));
        }

        public static CharArraySet GetWordSet(TextReader reader, String comment, CharArraySet result)
        {
            //BufferedReader br = null;
            try
            {
                //br = getBufferedReader(reader);
                String word = null;
                while ((word = reader.ReadLine()) != null)
                {
                    if (word.StartsWith(comment) == false)
                    {
                        result.Add(word.Trim());
                    }
                }
            }
            finally
            {
                //IOUtils.Close(reader);
            }
            return result;
        }

        public static CharArraySet GetSnowballWordSet(TextReader reader, CharArraySet result)
        {
            //BufferedReader br = null;
            try
            {
                //br = getBufferedReader(reader);
                String line = null;
                var rx = new Regex("\\s+");
                while ((line = reader.ReadLine()) != null)
                {
                    int comment = line.IndexOf('|');
                    if (comment >= 0) line = line.Substring(0, comment);
                    String[] words = rx.Split(line);
                    for (int i = 0; i < words.Length; i++)
                        if (words[i].Length > 0) result.Add(words[i]);
                }
            }
            finally
            {
                //IOUtils.Close(reader);
            }
            return result;
        }

        public static CharArraySet GetSnowballWordSet(TextReader reader, Lucene.Net.Util.Version matchVersion)
        {
            return GetSnowballWordSet(reader, new CharArraySet(matchVersion, INITIAL_CAPACITY, false));
        }

        public static CharArrayMap<String> GetStemDict(TextReader reader, CharArrayMap<String> result)
        {
            //BufferedReader br = null;
            try
            {
                //br = getBufferedReader(reader);
                String line;
                var rx = new Regex("\t");
                while ((line = reader.ReadLine()) != null)
                {
                    String[] wordstem = rx.Split(line, 2);
                    result.Put(wordstem[0], wordstem[1]);
                }
            }
            finally
            {
                //IOUtils.Close(reader);
            }
            return result;
        }

        public static IList<String> GetLines(Stream stream, Encoding charset)
        {
            TextReader input = null;
            List<String> lines;
            bool success = false;
            try
            {
                input = IOUtils.GetDecodingReader(stream, charset);

                lines = new List<String>();
                for (String word = null; (word = input.ReadLine()) != null; )
                {
                    // skip initial bom marker
                    if (lines.Count == 0 && word.Length > 0 && word[0] == '\uFEFF')
                        word = word.Substring(1);
                    // skip comments
                    if (word.StartsWith("#")) continue;
                    word = word.Trim();
                    // skip blank lines
                    if (word.Length == 0) continue;
                    lines.Add(word);
                }
                success = true;
                return lines;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Close(input);
                }
                else
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)input);
                }
            }
        }

    }
}
