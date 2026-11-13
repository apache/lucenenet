// Lucene version compatibility level 4.8.1
using J2N;
using J2N.Text;
using Lucene.Net.Support.Buffers;
using Lucene.Net.Text;
using Lucene.Net.Util;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Compound.Hyphenation
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *      http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// A XMLReader document handler to read and parse hyphenation patterns from a XML
    /// file.
    /// <para/>
    /// LUCENENET: This class has been refactored from its Java counterpart to use XmlReader rather
    /// than a SAX parser.
    /// </summary>
    public class PatternParser
    {
        internal int currElement;

        internal IPatternConsumer consumer;

        internal readonly TextBuilder token; // LUCENENET: marked readonly

        internal IList<object> exception;

        internal char hyphenChar;

        internal string errMsg;

        internal const int ELEM_CLASSES = 1;

        internal const int ELEM_EXCEPTIONS = 2;

        internal const int ELEM_PATTERNS = 3;

        internal const int ELEM_HYPHEN = 4;

        public PatternParser()
        {
            token = new TextBuilder();
            hyphenChar = '-'; // default
        }

        public PatternParser(IPatternConsumer consumer)
            : this()
        {
            this.consumer = consumer;
        }

        public virtual IPatternConsumer Consumer
        {
            get => this.consumer; // LUCENENET NOTE: Added getter per MSDN guidelines
            set => this.consumer = value;
        }

        /// <summary>
        /// Parses a hyphenation pattern file.
        /// </summary>
        /// <param name="path">The complete file path to be read.</param>
        /// <exception cref="IOException"> In case of an exception while parsing </exception>
        public virtual void Parse(string path)
            => Parse(path, Encoding.UTF8);

        /// <summary>
        /// Parses a hyphenation pattern file.
        /// </summary>
        /// <param name="path">The complete file path to be read.</param>
        /// <param name="encoding">The character encoding to use</param>
        /// <exception cref="IOException"> In case of an exception while parsing </exception>
        public virtual void Parse(string path, Encoding encoding)
        {
            var xmlReaderSettings = GetXmlReaderSettings();
            using var src = XmlReader.Create(new StreamReader(new FileStream(path, FileMode.Open), encoding), xmlReaderSettings);
            Parse(src);
        }

        /// <summary>
        /// Parses a hyphenation pattern file.
        /// </summary>
        /// <param name="file">  a <see cref="FileInfo"/> object representing the file  </param>
        /// <exception cref="IOException"> In case of an exception while parsing </exception>
        public virtual void Parse(FileInfo file)
            => Parse(file.FullName, Encoding.UTF8);

        /// <summary>
        /// Parses a hyphenation pattern file.
        /// </summary>
        /// <param name="file">  a <see cref="FileInfo"/> object representing the file </param>
        /// <param name="encoding">The character encoding to use</param>
        /// <exception cref="IOException"> In case of an exception while parsing </exception>
        public virtual void Parse(FileInfo file, Encoding encoding)
            => Parse(file.FullName, encoding);

        /// <summary>
        /// Parses a hyphenation pattern file.
        /// </summary>
        /// <param name="xmlStream">
        /// The stream containing the XML data.
        /// <para/>
        /// The <see cref="PatternParser"/> scans the first bytes of the stream looking for a byte order mark
        /// or other sign of encoding. When encoding is determined, the encoding is used to continue reading
        /// the stream, and processing continues parsing the input as a stream of (Unicode) characters.
        /// </param>
        /// <exception cref="IOException"> In case of an exception while parsing </exception>
        public virtual void Parse(Stream xmlStream)
        {
            var xmlReaderSettings = GetXmlReaderSettings();

            using var src = XmlReader.Create(xmlStream, xmlReaderSettings);
            Parse(src);
        }

        /// <summary>
        /// Parses a hyphenation pattern file.
        /// </summary>
        /// <param name="source"> <see cref="XmlReader"/> input source for the file </param>
        /// <exception cref="IOException"> In case of an exception while parsing </exception>
        public virtual void Parse(XmlReader source)
        {
            source.MoveToContent();
            while (source.Read())
            {
                ParseNode(source);
            }
        }

        private void ParseNode(XmlReader node)
        {
            string uri, name, raw;
            switch (node.NodeType)
            {
                case XmlNodeType.Element:

                    // Element start
                    uri = node.NamespaceURI;
                    name = node.Name;
                    bool isEmptyElement = node.IsEmptyElement;
                    var attributes = GetAttributes(node);
                    raw = string.Empty; // node.ReadOuterXml(); - not used, but was messing with the node pointer

                    this.StartElement(uri, name, raw, attributes);
                    if (isEmptyElement)
                    {
                        this.EndElement(uri, name, raw);
                    }
                    break;

                case XmlNodeType.Text:

                    this.Characters(node.Value);
                    break;

                case XmlNodeType.EndElement:
                    uri = node.NamespaceURI;
                    name = node.Name;
                    raw = string.Empty; // node.ReadOuterXml(); - not used, but was messing with the node pointer

                    // Element end
                    this.EndElement(uri, name, raw);
                    break;
            }
        }

        private static XmlReaderSettings GetXmlReaderSettings() // LUCENENET: CA1822: Mark members as static
        {
            return

                new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Parse,
                    XmlResolver = new DtdResolver()
                };
        }

        private static IDictionary<string, string> GetAttributes(XmlReader node) // LUCENENET: CA1822: Mark members as static
        {
            var result = new JCG.Dictionary<string, string>();
            if (node.HasAttributes)
            {
                for (int i = 0; i < node.AttributeCount; i++)
                {
                    node.MoveToAttribute(i);
                    result.Add(node.Name, node.Value);
                }
            }

            return result;
        }

        // LUCENENET specific - eliminated the ReadToken() method because it was doing too many responsibilities.
        // It also exposed System.Text.StringBuilder on the public API and had a weird side-effect of deleting
        // chars from the input as it processed them.
        //
        // ProcessToken() was added to replace it, but is now called in both cases where token processing needs to happen:
        //
        // 1. A whitespace delimiter is encountered in Characters(). The token is complete even though the XML element has not ended.
        // 2. The XML element ends while a token is still buffered. The token is complete even though no trailing whitespace was encountered.
        //
        // So, ProcessToken() is an improved "event" that a subclass may be interested in that doesn't leave out any XML element endings
        // and provides a more sensible read-only token parameter that doesn't change when this ProcessToken() is called.

#nullable enable

        /// <summary>
        /// Processes a completed token.
        /// </summary>
        /// <param name="token">
        /// The completed token. The span is valid only for the duration of this call
        /// and must not be retained.
        /// </param>
        protected virtual void ProcessToken(ReadOnlySpan<char> token)
        {
            switch (currElement)
            {
                case ELEM_CLASSES:
                    consumer.AddClass(token);
                    break;

                case ELEM_EXCEPTIONS:
                    exception.Add(token.ToString());
                    exception = NormalizeException(exception);
                    // LUCENENET specific - removed allocations by using stack/pooled memory for maximum reuse
                    int exceptionBufferLength = GetExceptionWordCharCount(exception);
                    char[]? arrayToReturnToPool = null;
                    Span<char> exceptionWord = exceptionBufferLength * sizeof(char) <= Constants.MaxStackByteLimit
                        ? stackalloc char[exceptionBufferLength]
                        : (arrayToReturnToPool = ArrayPool<char>.Shared.Rent(exceptionBufferLength));
                    try
                    {
                        if (!TryGetExceptionWord(exception, exceptionWord, out int exceptionWordLength))
                            throw new InvalidOperationException("Exception Word buffer was not large enough for the result"); // Should never happen
                        consumer.AddException(exceptionWord.Slice(0, exceptionWordLength), new JCG.List<object>(exception));
                        exception.Clear();
                    }
                    finally
                    {
                        ArrayPool<char>.Shared.ReturnIfNotNull(arrayToReturnToPool);
                    }
                    break;

                case ELEM_PATTERNS:
                    // LUCENENET specific - removed allocations by using stack/pooled memory for maximum reuse
                    int patternBufferLength = GetMaxPatternCharCount(token);
                    int valuesBufferLength = GetMaxInterletterValuesCharCount(token);
                    char[]? patternArrayToReturnToPool = null;
                    char[]? valuesArrayToReturnToPool = null;
                    Span<char> pattern = patternBufferLength * sizeof(char) <= Constants.MaxStackByteLimit
                        ? stackalloc char[patternBufferLength]
                        : (patternArrayToReturnToPool = ArrayPool<char>.Shared.Rent(patternBufferLength));
                    Span<char> values = valuesBufferLength * sizeof(char) <= Constants.MaxStackByteLimit
                        ? stackalloc char[valuesBufferLength]
                        : (valuesArrayToReturnToPool = ArrayPool<char>.Shared.Rent(valuesBufferLength));
                    try
                    {
                        if (!TryGetPattern(token, pattern, out int patternLength))
                            throw new InvalidOperationException("Pattern buffer was not large enough for the result"); // Should never happen
                        if (!TryGetInterletterValues(token, values, out int valuesLength))
                            throw new InvalidOperationException("Interletter Values buffer was not large enough for the result"); // Should never happen
                        consumer.AddPattern(pattern.Slice(0, patternLength), values.Slice(0, valuesLength));
                    }
                    finally
                    {
                        ArrayPool<char>.Shared.ReturnIfNotNull(patternArrayToReturnToPool);
                        ArrayPool<char>.Shared.ReturnIfNotNull(valuesArrayToReturnToPool);
                    }
                    break;
            }
        }

        /// <summary>
        /// Gets the maximum length of the pattern produced by
        /// <see cref="TryGetPattern(ReadOnlySpan{char}, Span{char}, out int)"/>.
        /// </summary>
        /// <param name="word">The word to process.</param>
        /// <returns>The maximum number of characters required for the resulting pattern.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static int GetMaxPatternCharCount(ReadOnlySpan<char> word)
        {
            return word.Length;
        }

        /// <summary>
        /// Attempts to extract the pattern from a word, omitting all digit characters.
        /// </summary>
        /// <param name="word">The word to process.</param>
        /// <param name="destination">The buffer into which the pattern is written.</param>
        /// <param name="charsWritten">When this method returns, contains the number of characters
        /// written to <paramref name="destination"/>, or zero if the destination was too small.</param>
        /// <returns><c>true</c> if the pattern was successfully written; otherwise, <c>false</c>.</returns>
        protected static bool TryGetPattern(ReadOnlySpan<char> word, Span<char> destination, out int charsWritten)
        {
            charsWritten = 0;

            for (int i = 0; i < word.Length; i++)
            {
                char c = word[i];

                if (!char.IsDigit(c))
                {
                    if (charsWritten >= destination.Length)
                    {
                        charsWritten = 0;
                        return false;
                    }

                    destination[charsWritten++] = c;
                }
            }

            return true;
        }

#nullable restore

        protected virtual IList<object> NormalizeException<T>(IList<T> ex)
        {
            ValueStringBuilder buf = new(stackalloc char[128]);
            try
            {
                IList<object> res = new JCG.List<object>();
                for (int i = 0; i < ex.Count; i++)
                {
                    object item = ex[i];
                    if (item is string str)
                    {
                        // LUCENENET: Moved string builder outside of the loop so we can reuse it.
                        buf.Length = 0;
                        for (int j = 0; j < str.Length; j++)
                        {
                            char c = str[j];
                            if (c != hyphenChar)
                            {
                                buf.Append(c);
                            }
                            else
                            {
                                res.Add(buf.ToString());
                                buf.Length = 0;
                                // LUCENENET: commented out unnecessary array allocation
                                //char[] h = new char[1];
                                //h[0] = hyphenChar;
                                // we use here hyphenChar which is not necessarily
                                // the one to be printed
                                res.Add(new Hyphen(new string(hyphenChar, 1), null, null));
                            }
                        }
                        if (buf.Length > 0)
                        {
                            res.Add(buf.ToString());
                        }
                    }
                    else
                    {
                        res.Add(item);
                    }
                }
                return res;
            }
            finally
            {
                buf.Dispose();
            }
        }

        /// <summary>
        /// Gets the exact length of the exception word produced by <see cref="TryGetExceptionWord"/>.
        /// </summary>
        /// <param name="ex">The exception elements.</param>
        /// <returns>The number of characters required to contain the resulting exception word.</returns>
        protected virtual int GetExceptionWordCharCount<T>(IList<T> ex)
        {
            int length = 0;

            for (int i = 0; i < ex.Count; i++)
            {
                T item = ex[i];

                if (item is string text)
                {
                    length = checked(length + text.Length);
                }
                else if (item is Hyphen hyphen)
                {
                    string noBreak = hyphen.NoBreak;

                    if (noBreak != null)
                    {
                        length = checked(length + noBreak.Length);
                    }
                }
                else
                {
                    throw new InvalidCastException("An element of ex is not supported. Subclass GetExceptionWordCharCount if types other than string and Hyphen are required.");
                }
            }

            return length;
        }

        /// <summary>
        /// Attempts to construct the exception word from its elements.
        /// </summary>
        /// <param name="ex">The exception elements.</param>
        /// <param name="destination">The buffer into which the exception word is written.</param>
        /// <param name="charsWritten">When this method returns, contains the number of characters
        /// written to <paramref name="destination"/>, or zero if the destination was too small.</param>
        /// <returns><c>true</c> if the exception word was successfully written; otherwise, <c>false</c>.</returns>
        protected virtual bool TryGetExceptionWord<T>(IList<T> ex, Span<char> destination, out int charsWritten)
        {
            charsWritten = 0;

            for (int i = 0; i < ex.Count; i++)
            {
                T item = ex[i];
                string text;

                if (item is string str)
                {
                    text = str;
                }
                else if (item is Hyphen hyphen)
                {
                    text = hyphen.NoBreak;

                    if (text == null)
                    {
                        continue;
                    }
                }
                else
                {
                    throw new InvalidCastException("An element of ex is not supported. Subclass TryGetExceptionWord if types other than string and Hyphen are required.");
                }

                if (text.Length > destination.Length - charsWritten)
                {
                    charsWritten = 0;
                    return false;
                }

                text.AsSpan().CopyTo(destination.Slice(charsWritten));
                charsWritten += text.Length;
            }

            return true;
        }

        /// <summary>
        /// Gets the maximum length of the interletter values produced by
        /// <see cref="TryGetInterletterValues(ReadOnlySpan{char}, Span{char}, out int)"/>.
        /// </summary>
        /// <param name="pat">The pattern to process.</param>
        /// <returns>The maximum number of characters required for the resulting interletter values.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static int GetMaxInterletterValuesCharCount(ReadOnlySpan<char> pat)
        {
            return pat.Length == int.MaxValue ? int.MaxValue : pat.Length + 1;
        }

        /// <summary>
        /// Attempts to extract the interletter values from a pattern.
        /// </summary>
        /// <param name="pat">The pattern to process.</param>
        /// <param name="destination">The buffer into which the interletter values are written.</param>
        /// <param name="charsWritten">When this method returns, contains the number of characters
        /// written to <paramref name="destination"/>, or zero if the destination was too small.</param>
        /// <returns><c>true</c> if the interletter values were successfully written; otherwise, <c>false</c>.</returns>
        protected static bool TryGetInterletterValues(ReadOnlySpan<char> pat, Span<char> destination, out int charsWritten)
        {
            charsWritten = 0;

            int i = 0;

            while (i < pat.Length)
            {
                char c = pat[i++];

                if (char.IsDigit(c))
                {
                    if (charsWritten >= destination.Length)
                    {
                        charsWritten = 0;
                        return false;
                    }

                    destination[charsWritten++] = c;

                    // Skip the character following the digit.
                    i++;
                }
                else
                {
                    if (charsWritten >= destination.Length)
                    {
                        charsWritten = 0;
                        return false;
                    }

                    destination[charsWritten++] = '0';
                }
            }

            // Process the virtual sentinel ('a'), unless it was consumed by
            // a digit at the end of the input.
            if (i == pat.Length)
            {
                if (charsWritten >= destination.Length)
                {
                    charsWritten = 0;
                    return false;
                }

                destination[charsWritten++] = '0';
            }

            return true;
        }

        /// <summary>
        /// LUCENENET specific helper class to force the DTD file to be read from the embedded resource
        /// rather than from the file system. Any other external reference is rejected, so the parser
        /// only ever resolves the known, embedded <c>hyphenation.dtd</c>.
        /// </summary>
        internal class DtdResolver : XmlUrlResolver
        {
            internal const string DTD_FILENAME = "hyphenation.dtd";

            public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
            {
                if (DTD_FILENAME.Equals(absoluteUri?.Segments.LastOrDefault(), StringComparison.Ordinal))
                {
                    return typeof(PatternParser).FindAndGetManifestResourceStream(DTD_FILENAME);
                }

                // Only the embedded hyphenation.dtd is a valid external reference. Reject anything
                // else rather than resolving it from the file system or network.
                throw new XmlException($"Unexpected external reference in hyphenation data: '{absoluteUri}'. Only '{DTD_FILENAME}' may be referenced.");
            }
        }

        //
        // ContentHandler methods
        //

        /// <summary>
        /// Receive notification of the beginning of an element.
        /// <para/>
        /// The Parser will invoke this method at the beginning of every element in the XML document;
        /// there will be a corresponding <see cref="EndElement"/> event for every <see cref="StartElement"/> event
        /// (even when the element is empty). All of the element's content will be reported,
        /// in order, before the corresponding endElement event.
        /// </summary>
        /// <param name="uri">the Namespace URI, or the empty string if the element has no Namespace URI or if Namespace processing is not being performed</param>
        /// <param name="local">the local name (without prefix), or the empty string if Namespace processing is not being performed</param>
        /// <param name="raw"></param>
        /// <param name="attrs"> the attributes attached to the element. If there are no attributes, it shall be an empty Attributes object. The value of this object after startElement returns is undefined</param>
        public virtual void StartElement(string uri, string local, string raw, IDictionary<string, string> attrs)
        {
            if (local.Equals("hyphen-char", StringComparison.Ordinal))
            {
                if (attrs.TryGetValue("value", out string h) && h != null && h.Length == 1)
                {
                    hyphenChar = h[0];
                }
            }
            else if (local.Equals("classes", StringComparison.Ordinal))
            {
                currElement = ELEM_CLASSES;
            }
            else if (local.Equals("patterns", StringComparison.Ordinal))
            {
                currElement = ELEM_PATTERNS;
            }
            else if (local.Equals("exceptions", StringComparison.Ordinal))
            {
                currElement = ELEM_EXCEPTIONS;
                exception = new JCG.List<object>();
            }
            else if (local.Equals("hyphen", StringComparison.Ordinal))
            {
                if (token.Length > 0)
                {
                    exception.Add(token.ToString());
                }
                exception.Add(new Hyphen(attrs["pre"], attrs["no"], attrs["post"]));
                currElement = ELEM_HYPHEN;
            }
            token.Length = 0;
        }

        /// <summary>
        /// Receive notification of the end of an element.
        /// <para/>
        /// The parser will invoke this method at the end of every element in the XML document;
        /// there will be a corresponding <see cref="StartElement"/> event for every
        /// <see cref="EndElement"/> event (even when the element is empty).
        /// </summary>
        /// <param name="uri">the Namespace URI, or the empty string if the element has no Namespace URI or if Namespace processing is not being performed</param>
        /// <param name="local">the local name (without prefix), or the empty string if Namespace processing is not being performed</param>
        /// <param name="raw"></param>
        public virtual void EndElement(string uri, string local, string raw)
        {
            // LUCENENET specific - moved processing logic to a shared
            // ProcessToken() method that is caleld both here and by Characters().
            // This provides a more sensible hook for subclasses to tie into
            // than ReadToken().
            if (token.Length > 0)
            {
                if (currElement != ELEM_HYPHEN)
                {
                    try
                    {
                        ProcessToken(token);
                    }
                    finally
                    {
                        token.Length = 0;
                    }
                }
            }
            if (currElement == ELEM_HYPHEN)
            {
                currElement = ELEM_EXCEPTIONS;
            }
            else
            {
                currElement = 0;
            }
        }

        // LUCENENET design notes:
        // 1.   tokenLength can be zero when the input begins with whitespace.In that case, an already accumulated
        //      token is processed; otherwise, the whitespace is simply skipped.
        // 2.   Only one delimiter is consumed per iteration.Consecutive whitespace is handled by subsequent iterations.
        // 3.   If the input ends without whitespace, the partial token remains in token for the next callback.
        // 4.   The finally ensures the builder is cleared even if the consumer throws.This preserves the original
        //      behavior, where ReadToken() cleared the token before returning it to the caller.
        // 5.   The span passed to ProcessToken() is borrowed from token.It must not be retained after ProcessToken() returns.

        /// <summary>
        /// Receive notification of character data.
        /// <para/>
        /// The Parser will call this method to report each chunk of character data. Parsers may
        /// return all contiguous character data in a single chunk, or they may split it into
        /// several chunks; however, all of the characters in any single event must come from
        /// the same external entity so that the Locator provides useful information.
        /// </summary>
        /// <param name="chars">The character data to process.</param>
        public virtual void Characters(ReadOnlySpan<char> chars)
        {
            while (!chars.IsEmpty)
            {
                int tokenLength = 0;

                // Find the next whitespace character, which terminates the current token.
                while (tokenLength < chars.Length && !char.IsWhiteSpace(chars[tokenLength]))
                {
                    tokenLength++;
                }

                // Append the next portion of the token. This also handles tokens split
                // across multiple Characters() calls.
                token.Append(chars.Slice(0, tokenLength));

                if (tokenLength < chars.Length)
                {
                    // A delimiter was found, so the accumulated token is complete.
                    // Process it before clearing the builder because ProcessToken()
                    // consumes the span synchronously.
                    try
                    {
                        if (token.Length > 0)
                        {
                            ProcessToken(token);
                        }
                    }
                    finally
                    {
                        token.Length = 0;
                    }

                    // Consume the token and its terminating whitespace character.
                    chars = chars.Slice(tokenLength + 1);
                }
                else
                {
                    // No delimiter was found. Keep the accumulated token for the next
                    // Characters() call or the corresponding EndElement() call.
                    break;
                }
            }
        }
    }
}
