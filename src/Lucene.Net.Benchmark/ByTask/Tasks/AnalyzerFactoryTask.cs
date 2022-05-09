using J2N.IO;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Benchmarks.ByTask.Tasks
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
    /// Analyzer factory construction task.  The name given to the constructed factory may
    /// be given to <see cref="NewAnalyzerTask"/>, which will call <see cref="AnalyzerFactory.Create()"/>.
    /// </summary>
    /// <remarks>
    /// Params are in the form argname:argvalue or argname:"argvalue" or argname:'argvalue';
    /// use backslashes to escape '"' or "'" inside a quoted value when it's used as the enclosing
    /// quotation mark,
    /// <para/>
    /// Specify params in a comma separated list of the following, in order:
    /// <list type="number">
    ///     <item><description>
    ///         <list type="bullet">
    ///             <item><description><b>Required</b>: <c>name:<i>analyzer-factory-name</i></c></description></item>
    ///             <item><description>Optional: <c>positionIncrementGap:<i>int value</i></c> (default: 0)</description></item>
    ///             <item><description>Optional: <c>offsetGap:<i>int value</i></c> (default: 1)</description></item>
    ///         </list>
    ///     </description></item>
    ///     <item><description>zero or more CharFilterFactory's, followed by</description></item>
    ///     <item><description>exactly one TokenizerFactory, followed by</description></item>
    ///     <item><description>zero or more TokenFilterFactory's</description></item>
    /// </list>
    /// <para/>
    /// Each component analysis factory map specify <tt>luceneMatchVersion</tt> (defaults to
    /// <see cref="LuceneVersion.LUCENE_CURRENT"/>) and any of the args understood by the specified
    /// *Factory class, in the above-describe param format.
    /// <para/>
    /// Example:
    /// <code>
    ///     -AnalyzerFactory(name:'strip html, fold to ascii, whitespace tokenize, max 10k tokens',
    ///                      positionIncrementGap:100,
    ///                      HTMLStripCharFilter,
    ///                      MappingCharFilter(mapping:'mapping-FoldToASCII.txt'),
    ///                      WhitespaceTokenizer(luceneMatchVersion:LUCENE_43),
    ///                      TokenLimitFilter(maxTokenCount:10000, consumeAllTokens:false))
    ///     [...]
    ///     -NewAnalyzer('strip html, fold to ascii, whitespace tokenize, max 10k tokens')
    /// </code>
    /// <para/>
    /// <see cref="AnalyzerFactory"/> will direct analysis component factories to look for resources
    /// under the directory specified in the "work.dir" property.
    /// </remarks>
    public class AnalyzerFactoryTask : PerfTask
    {
        private const string LUCENE_ANALYSIS_PACKAGE_PREFIX = "Lucene.Net.Analysis.";
        private static readonly Regex ANALYSIS_COMPONENT_SUFFIX_PATTERN
            = new Regex("(?s:(?:(?:Token|Char)?Filter|Tokenizer)(?:Factory)?)$", RegexOptions.Compiled);
        private static readonly Regex TRAILING_DOT_ZERO_PATTERN = new Regex(@"\.0$", RegexOptions.Compiled);

        private enum ArgType { ANALYZER_ARG, ANALYZER_ARG_OR_CHARFILTER_OR_TOKENIZER, TOKENFILTER }

        private string factoryName = null;
        private int? positionIncrementGap = null;
        private int? offsetGap = null;
        private readonly IList<CharFilterFactory> charFilterFactories = new JCG.List<CharFilterFactory>();
        private TokenizerFactory tokenizerFactory = null;
        private readonly IList<TokenFilterFactory> tokenFilterFactories = new JCG.List<TokenFilterFactory>();

        public AnalyzerFactoryTask(PerfRunData runData)
            : base(runData)
        {
        }

        public override int DoLogic()
        {
            return 1;
        }

        /// <summary>
        /// Sets the params.
        /// Analysis component factory names may optionally include the "Factory" suffix.
        /// </summary>
        /// <param name="params">
        /// analysis pipeline specification: name, (optional) positionIncrementGap,
        /// (optional) offsetGap, 0+ CharFilterFactory's, 1 TokenizerFactory,
        /// and 0+ TokenFilterFactory's
        /// </param>
        public override void SetParams(string @params)
        {
            base.SetParams(@params);
            ArgType expectedArgType = ArgType.ANALYZER_ARG;

            StreamTokenizer stok = new StreamTokenizer(new StringReader(@params));
            stok.CommentChar('#');
            stok.QuoteChar('"');
            stok.QuoteChar('\'');
            stok.EndOfLineIsSignificant = false;
            stok.OrdinaryChar('(');
            stok.OrdinaryChar(')');
            stok.OrdinaryChar(':');
            stok.OrdinaryChar(',');
            try
            {
                while (stok.NextToken() != StreamTokenizer.TokenType_EndOfStream)
                {
                    switch (stok.TokenType)
                    {
                        case ',':
                            {
                                // Do nothing
                                break;
                            }
                        case StreamTokenizer.TokenType_Word:
                            {
                                if (expectedArgType.Equals(ArgType.ANALYZER_ARG))
                                {
                                    string argName = stok.StringValue;
                                    if (!argName.Equals("name", StringComparison.OrdinalIgnoreCase)
                                        && !argName.Equals("positionIncrementGap", StringComparison.OrdinalIgnoreCase)
                                        && !argName.Equals("offsetGap", StringComparison.OrdinalIgnoreCase))
                                    {
                                        throw RuntimeException.Create
                                            ("Line #" + GetLineNumber(stok) + ": Missing 'name' param to AnalyzerFactory: '" + @params + "'");
                                    }
                                    stok.NextToken();
                                    if (stok.TokenType != ':')
                                    {
                                        throw RuntimeException.Create
                                            ("Line #" + GetLineNumber(stok) + ": Missing ':' after '" + argName + "' param to AnalyzerFactory");
                                    }

                                    stok.NextToken();
                                    string argValue = stok.StringValue;
                                    switch (stok.TokenType)
                                    {
                                        case StreamTokenizer.TokenType_Number:
                                            {
                                                argValue = stok.NumberValue.ToString(CultureInfo.InvariantCulture);
                                                // Drop the ".0" from numbers, for integer arguments
                                                argValue = TRAILING_DOT_ZERO_PATTERN.Replace(argValue, "", 1);
                                                // Intentional fallthrough

                                                if (argName.Equals("name", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    factoryName = argValue;
                                                    expectedArgType = ArgType.ANALYZER_ARG_OR_CHARFILTER_OR_TOKENIZER;
                                                }
                                                else
                                                {
                                                    int intArgValue = 0;
                                                    try
                                                    {
                                                        intArgValue = int.Parse(argValue, CultureInfo.InvariantCulture);
                                                    }
                                                    catch (Exception e) when (e.IsNumberFormatException())
                                                    {
                                                        throw RuntimeException.Create
                                                            ("Line #" + GetLineNumber(stok) + ": Exception parsing " + argName + " value '" + argValue + "'", e);
                                                    }
                                                    if (argName.Equals("positionIncrementGap", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        positionIncrementGap = intArgValue;
                                                    }
                                                    else if (argName.Equals("offsetGap", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        offsetGap = intArgValue;
                                                    }
                                                }
                                                break;
                                            }
                                        case '"':
                                        case '\'':
                                        case StreamTokenizer.TokenType_Word:
                                            {
                                                if (argName.Equals("name", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    factoryName = argValue;
                                                    expectedArgType = ArgType.ANALYZER_ARG_OR_CHARFILTER_OR_TOKENIZER;
                                                }
                                                else
                                                {
                                                    int intArgValue = 0;
                                                    try
                                                    {
                                                        intArgValue = int.Parse(argValue, CultureInfo.InvariantCulture);
                                                    }
                                                    catch (Exception e) when (e.IsNumberFormatException())
                                                    {
                                                        throw RuntimeException.Create
                                                            ("Line #" + GetLineNumber(stok) + ": Exception parsing " + argName + " value '" + argValue + "'", e);
                                                    }
                                                    if (argName.Equals("positionIncrementGap", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        positionIncrementGap = intArgValue;
                                                    }
                                                    else if (argName.Equals("offsetGap", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        offsetGap = intArgValue;
                                                    }
                                                }
                                                break;
                                            }
                                        case StreamTokenizer.TokenType_EndOfStream:
                                            {
                                                throw RuntimeException.Create("Unexpected EOF: " + stok.ToString());
                                            }
                                        default:
                                            {
                                                throw RuntimeException.Create
                                                    ("Line #" + GetLineNumber(stok) + ": Unexpected token: " + stok.ToString());
                                            }
                                    }
                                }
                                else if (expectedArgType.Equals(ArgType.ANALYZER_ARG_OR_CHARFILTER_OR_TOKENIZER))
                                {
                                    string argName = stok.StringValue;

                                    if (argName.Equals("positionIncrementGap", StringComparison.OrdinalIgnoreCase)
                                        || argName.Equals("offsetGap", StringComparison.OrdinalIgnoreCase))
                                    {
                                        stok.NextToken();
                                        if (stok.TokenType != ':')
                                        {
                                            throw RuntimeException.Create
                                                ("Line #" + GetLineNumber(stok) + ": Missing ':' after '" + argName + "' param to AnalyzerFactory");
                                        }
                                        stok.NextToken();
                                        int intArgValue = (int)stok.NumberValue;
                                        switch (stok.TokenType)
                                        {
                                            case '"':
                                            case '\'':
                                            case StreamTokenizer.TokenType_Word:
                                                {
                                                    intArgValue = 0;
                                                    try
                                                    {
                                                        intArgValue = int.Parse(stok.StringValue.Trim(), CultureInfo.InvariantCulture);
                                                    }
                                                    catch (Exception e) when (e.IsNumberFormatException())
                                                    {
                                                        throw RuntimeException.Create
                                                            ("Line #" + GetLineNumber(stok) + ": Exception parsing " + argName + " value '" + stok.StringValue + "'", e);
                                                    }
                                                    // Intentional fall-through

                                                    if (argName.Equals("positionIncrementGap", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        positionIncrementGap = intArgValue;
                                                    }
                                                    else if (argName.Equals("offsetGap", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        offsetGap = intArgValue;
                                                    }
                                                    break;
                                                }
                                            case StreamTokenizer.TokenType_Number:
                                                {
                                                    if (argName.Equals("positionIncrementGap", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        positionIncrementGap = intArgValue;
                                                    }
                                                    else if (argName.Equals("offsetGap", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        offsetGap = intArgValue;
                                                    }
                                                    break;
                                                }
                                            case StreamTokenizer.TokenType_EndOfStream:
                                                {
                                                    throw RuntimeException.Create("Unexpected EOF: " + stok.ToString());
                                                }
                                            default:
                                                {
                                                    throw RuntimeException.Create
                                                        ("Line #" + GetLineNumber(stok) + ": Unexpected token: " + stok.ToString());
                                                }
                                        }
                                        break;
                                    }
                                    try
                                    {
                                        Type clazz;
                                        clazz = LookupAnalysisClass(argName, typeof(CharFilterFactory));
                                        CreateAnalysisPipelineComponent(stok, clazz);
                                    }
                                    catch (Exception e) when (e.IsIllegalArgumentException())
                                    {
                                        try
                                        {
                                            Type clazz;
                                            clazz = LookupAnalysisClass(argName, typeof(TokenizerFactory));
                                            CreateAnalysisPipelineComponent(stok, clazz);
                                            expectedArgType = ArgType.TOKENFILTER;
                                        }
                                        catch (Exception e2) when (e2.IsIllegalArgumentException())
                                        {
                                            throw RuntimeException.Create("Line #" + GetLineNumber(stok) + ": Can't find class '"
                                                                       + argName + "' as CharFilterFactory or TokenizerFactory", e2);
                                        }
                                    }
                                }
                                else
                                { // expectedArgType = ArgType.TOKENFILTER
                                    string className = stok.StringValue;
                                    Type clazz;
                                    try
                                    {
                                        clazz = LookupAnalysisClass(className, typeof(TokenFilterFactory));
                                    }
                                    catch (Exception e) when (e.IsIllegalArgumentException())
                                    {
                                        throw RuntimeException.Create
                                            ("Line #" + GetLineNumber(stok) + ": Can't find class '" + className + "' as TokenFilterFactory", e);
                                    }
                                    CreateAnalysisPipelineComponent(stok, clazz);
                                }
                                break;
                            }
                        default:
                            {
                                throw RuntimeException.Create("Line #" + GetLineNumber(stok) + ": Unexpected token: " + stok.ToString());
                            }
                    }
                }
            }
            catch (Exception e) when (e.IsRuntimeException())
            {
                if (e.Message.StartsWith("Line #", StringComparison.Ordinal))
                {
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                }
                else
                {
                    throw RuntimeException.Create("Line #" + GetLineNumber(stok) + ": ", e);
                }
            }
            catch (Exception t) when (t.IsThrowable())
            {
                throw RuntimeException.Create("Line #" + GetLineNumber(stok) + ": ", t);
            }

            AnalyzerFactory analyzerFactory = new AnalyzerFactory(charFilterFactories, tokenizerFactory, tokenFilterFactories)
            {
                PositionIncrementGap = positionIncrementGap,
                OffsetGap = offsetGap
            };
            RunData.AnalyzerFactories[factoryName] = analyzerFactory;
        }

        /// <summary>
        /// Instantiates the given analysis factory class after pulling params from
        /// the given stream tokenizer, then stores the result in the appropriate
        /// pipeline component list.        
        /// </summary>
        /// <param name="stok">Stream tokenizer from which to draw analysis factory params.</param>
        /// <param name="clazz">Analysis factory class to instantiate.</param>
        private void CreateAnalysisPipelineComponent(StreamTokenizer stok, Type clazz)
        {
            IDictionary<string, string> argMap = new Dictionary<string, string>();
            bool parenthetical = false;
            try
            {
                while (stok.NextToken() != StreamTokenizer.TokenType_EndOfStream)
                {
                    switch (stok.TokenType)
                    {
                        case ',':
                            {
                                if (parenthetical)
                                {
                                    // Do nothing
                                    break;
                                }
                                else
                                {
                                    // Finished reading this analysis factory configuration
                                    goto WHILE_LOOP_BREAK;
                                }
                            }
                        case '(':
                            {
                                if (parenthetical)
                                {
                                    throw RuntimeException.Create
                                        ("Line #" + GetLineNumber(stok) + ": Unexpected opening parenthesis.");
                                }
                                parenthetical = true;
                                break;
                            }
                        case ')':
                            {
                                if (parenthetical)
                                {
                                    parenthetical = false;
                                }
                                else
                                {
                                    throw RuntimeException.Create
                                        ("Line #" + GetLineNumber(stok) + ": Unexpected closing parenthesis.");
                                }
                                break;
                            }
                        case StreamTokenizer.TokenType_Word:
                            {
                                if (!parenthetical)
                                {
                                    throw RuntimeException.Create("Line #" + GetLineNumber(stok) + ": Unexpected token '" + stok.StringValue + "'");
                                }
                                string argName = stok.StringValue;
                                stok.NextToken();
                                if (stok.TokenType != ':')
                                {
                                    throw RuntimeException.Create
                                        ("Line #" + GetLineNumber(stok) + ": Missing ':' after '" + argName + "' param to " + clazz.Name);
                                }
                                stok.NextToken();
                                string argValue = stok.StringValue;
                                switch (stok.TokenType)
                                {
                                    case StreamTokenizer.TokenType_Number:
                                        {
                                            argValue = stok.NumberValue.ToString(CultureInfo.InvariantCulture);
                                            // Drop the ".0" from numbers, for integer arguments
                                            argValue = TRAILING_DOT_ZERO_PATTERN.Replace(argValue, "", 1);
                                            // Intentional fall-through
                                            argMap[argName] = argValue;
                                            break;
                                        }
                                    case '"':
                                    case '\'':
                                    case StreamTokenizer.TokenType_Word:
                                        {
                                            argMap[argName] = argValue;
                                            break;
                                        }
                                    case StreamTokenizer.TokenType_EndOfStream:
                                        {
                                            throw RuntimeException.Create("Unexpected EOF: " + stok.ToString());
                                        }
                                    default:
                                        {
                                            throw RuntimeException.Create
                                                ("Line #" + GetLineNumber(stok) + ": Unexpected token: " + stok.ToString());
                                        }
                                }
                                break;
                            }
                    }
                }
            WHILE_LOOP_BREAK: { }

                if (!argMap.ContainsKey("luceneMatchVersion"))
                {
#pragma warning disable 612, 618
                    argMap["luceneMatchVersion"] = LuceneVersion.LUCENE_CURRENT.ToString();
#pragma warning restore 612, 618
                }
                AbstractAnalysisFactory instance;
                try
                {
                    instance = (AbstractAnalysisFactory)Activator.CreateInstance(clazz, argMap);
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create("Line #" + GetLineNumber(stok) + ": ", e);
                }
                if (instance is IResourceLoaderAware resourceLoaderAware)
                {
                    DirectoryInfo baseDir = new DirectoryInfo(RunData.Config.Get("work.dir", "work"));
                    resourceLoaderAware.Inform(new FilesystemResourceLoader(baseDir));
                }
                if (typeof(CharFilterFactory).IsAssignableFrom(clazz))
                {
                    charFilterFactories.Add((CharFilterFactory)instance);
                }
                else if (typeof(TokenizerFactory).IsAssignableFrom(clazz))
                {
                    tokenizerFactory = (TokenizerFactory)instance;
                }
                else if (typeof(TokenFilterFactory).IsAssignableFrom(clazz))
                {
                    tokenFilterFactories.Add((TokenFilterFactory)instance);
                }
            }
            catch (Exception e) when (e.IsRuntimeException())
            {
                if (e.Message.StartsWith("Line #", StringComparison.Ordinal))
                {
                    throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                }
                else
                {
                    throw RuntimeException.Create("Line #" + GetLineNumber(stok) + ": ", e);
                }
            }
            catch (Exception t) when (t.IsThrowable())
            {
                throw RuntimeException.Create("Line #" + GetLineNumber(stok) + ": ", t);
            }
        }

        /// <summary>
        /// This method looks up a class with its fully qualified name (FQN), or a short-name
        /// class-simplename, or with a package suffix, assuming "Lucene.Net.Analysis."
        /// as the namespace prefix (e.g. "standard.ClassicTokenizerFactory" ->
        /// "Lucene.Net.Analysis.Standard.ClassicTokenizerFactory").
        /// </summary>
        /// <remarks>
        /// If <paramref name="className"/> contains a period, the class is first looked up as-is, assuming that it
        /// is an FQN.  If this fails, lookup is retried after prepending the Lucene analysis
        /// package prefix to the class name.
        /// <para/>
        /// If <paramref name="className"/> does not contain a period, the analysis SPI *Factory.LookupClass()
        /// methods are used to find the class.
        /// </remarks>
        /// <param name="className">The namespace qualified name or the short name of the class.</param>
        /// <param name="expectedType">The superclass <paramref name="className"/> is expected to extend. </param>
        /// <returns>The loaded type.</returns>
        /// <exception cref="TypeLoadException">If lookup fails.</exception>
        public virtual Type LookupAnalysisClass(string className, Type expectedType)
        {
            if (className.Contains("."))
            {
                // First, try className == FQN
                Type result = Type.GetType(className);
                if (result is null)
                {
                    // Second, retry lookup after prepending the Lucene analysis package prefix
                    result = Type.GetType(LUCENE_ANALYSIS_PACKAGE_PREFIX + className);

                    if (result is null)
                    {
                        throw ClassNotFoundException.Create("Can't find class '" + className
                                                 + "' or '" + LUCENE_ANALYSIS_PACKAGE_PREFIX + className + "'");
                    }
                }
                return result;
            }
            // No dot - use analysis SPI lookup
            string analysisComponentName = ANALYSIS_COMPONENT_SUFFIX_PATTERN.Replace(className, "", 1);
            if (typeof(CharFilterFactory).IsAssignableFrom(expectedType))
            {
                return CharFilterFactory.LookupClass(analysisComponentName);
            }
            else if (typeof(TokenizerFactory).IsAssignableFrom(expectedType))
            {
                return TokenizerFactory.LookupClass(analysisComponentName);
            }
            else if (typeof(TokenFilterFactory).IsAssignableFrom(expectedType))
            {
                return TokenFilterFactory.LookupClass(analysisComponentName);
            }

            throw ClassNotFoundException.Create("Can't find class '" + className + "'");
        }

        /// <seealso cref="PerfTask.SupportsParams"/>
        public override bool SupportsParams => true;

        /// <summary>Returns the current line in the algorithm file</summary>
        public virtual int GetLineNumber(StreamTokenizer stok)
        {
            return AlgLineNum + stok.LineNumber;
        }
    }
}
