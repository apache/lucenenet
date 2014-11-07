using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

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

namespace org.apache.lucene.analysis.compound.hyphenation
{

	// SAX
	using XMLReader = org.xml.sax.XMLReader;
	using InputSource = org.xml.sax.InputSource;
	using SAXException = org.xml.sax.SAXException;
	using SAXParseException = org.xml.sax.SAXParseException;
	using DefaultHandler = org.xml.sax.helpers.DefaultHandler;
	using Attributes = org.xml.sax.Attributes;

	// Java

	/// <summary>
	/// A SAX document handler to read and parse hyphenation patterns from a XML
	/// file.
	/// 
	/// This class has been taken from the Apache FOP project (http://xmlgraphics.apache.org/fop/). They have been slightly modified. 
	/// </summary>
	public class PatternParser : DefaultHandler
	{

	  internal XMLReader parser;

	  internal int currElement;

	  internal PatternConsumer consumer;

	  internal StringBuilder token;

	  internal List<object> exception;

	  internal char hyphenChar;

	  internal string errMsg;

	  internal const int ELEM_CLASSES = 1;

	  internal const int ELEM_EXCEPTIONS = 2;

	  internal const int ELEM_PATTERNS = 3;

	  internal const int ELEM_HYPHEN = 4;

	  public PatternParser()
	  {
		token = new StringBuilder();
		parser = createParser();
		parser.ContentHandler = this;
		parser.ErrorHandler = this;
		parser.EntityResolver = this;
		hyphenChar = '-'; // default

	  }

	  public PatternParser(PatternConsumer consumer) : this()
	  {
		this.consumer = consumer;
	  }

	  public virtual PatternConsumer Consumer
	  {
		  set
		  {
			this.consumer = value;
		  }
	  }

	  /// <summary>
	  /// Parses a hyphenation pattern file.
	  /// </summary>
	  /// <param name="filename"> the filename </param>
	  /// <exception cref="IOException"> In case of an exception while parsing </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void parse(String filename) throws java.io.IOException
	  public virtual void parse(string filename)
	  {
		parse(new InputSource(filename));
	  }

	  /// <summary>
	  /// Parses a hyphenation pattern file.
	  /// </summary>
	  /// <param name="file"> the pattern file </param>
	  /// <exception cref="IOException"> In case of an exception while parsing </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void parse(java.io.File file) throws java.io.IOException
	  public virtual void parse(File file)
	  {
		InputSource src = new InputSource(file.toURI().toASCIIString());
		parse(src);
	  }

	  /// <summary>
	  /// Parses a hyphenation pattern file.
	  /// </summary>
	  /// <param name="source"> the InputSource for the file </param>
	  /// <exception cref="IOException"> In case of an exception while parsing </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void parse(org.xml.sax.InputSource source) throws java.io.IOException
	  public virtual void parse(InputSource source)
	  {
		try
		{
		  parser.parse(source);
		}
		catch (SAXException e)
		{
		  throw new IOException(e);
		}
	  }

	  /// <summary>
	  /// Creates a SAX parser using JAXP
	  /// </summary>
	  /// <returns> the created SAX parser </returns>
	  internal static XMLReader createParser()
	  {
		try
		{
		  SAXParserFactory factory = SAXParserFactory.newInstance();
		  factory.NamespaceAware = true;
		  return factory.newSAXParser().XMLReader;
		}
		catch (Exception e)
		{
		  throw new Exception("Couldn't create XMLReader: " + e.Message);
		}
	  }

	  protected internal virtual string readToken(StringBuilder chars)
	  {
		string word;
		bool space = false;
		int i;
		for (i = 0; i < chars.Length; i++)
		{
		  if (char.IsWhiteSpace(chars[i]))
		  {
			space = true;
		  }
		  else
		  {
			break;
		  }
		}
		if (space)
		{
		  // chars.delete(0,i);
		  for (int countr = i; countr < chars.Length; countr++)
		  {
			chars[countr - i] = chars[countr];
		  }
		  chars.Length = chars.Length - i;
		  if (token.Length > 0)
		  {
			word = token.ToString();
			token.Length = 0;
			return word;
		  }
		}
		space = false;
		for (i = 0; i < chars.Length; i++)
		{
		  if (char.IsWhiteSpace(chars[i]))
		  {
			space = true;
			break;
		  }
		}
		token.Append(chars.ToString().Substring(0, i));
		// chars.delete(0,i);
		for (int countr = i; countr < chars.Length; countr++)
		{
		  chars[countr - i] = chars[countr];
		}
		chars.Length = chars.Length - i;
		if (space)
		{
		  word = token.ToString();
		  token.Length = 0;
		  return word;
		}
		token.Append(chars);
		return null;
	  }

	  protected internal static string getPattern(string word)
	  {
		StringBuilder pat = new StringBuilder();
		int len = word.Length;
		for (int i = 0; i < len; i++)
		{
		  if (!char.IsDigit(word[i]))
		  {
			pat.Append(word[i]);
		  }
		}
		return pat.ToString();
	  }

	  protected internal virtual List<object> normalizeException(List<T1> ex)
	  {
		List<object> res = new List<object>();
		for (int i = 0; i < ex.Count; i++)
		{
		  object item = ex[i];
		  if (item is string)
		  {
			string str = (string) item;
			StringBuilder buf = new StringBuilder();
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
				char[] h = new char[1];
				h[0] = hyphenChar;
				// we use here hyphenChar which is not necessarily
				// the one to be printed
				res.Add(new Hyphen(new string(h), null, null));
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

	  protected internal virtual string getExceptionWord<T1>(List<T1> ex)
	  {
		StringBuilder res = new StringBuilder();
		for (int i = 0; i < ex.Count; i++)
		{
		  object item = ex[i];
		  if (item is string)
		  {
			res.Append((string) item);
		  }
		  else
		  {
			if (((Hyphen) item).noBreak != null)
			{
			  res.Append(((Hyphen) item).noBreak);
			}
		  }
		}
		return res.ToString();
	  }

	  protected internal static string getInterletterValues(string pat)
	  {
		StringBuilder il = new StringBuilder();
		string word = pat + "a"; // add dummy letter to serve as sentinel
		int len = word.Length;
		for (int i = 0; i < len; i++)
		{
		  char c = word[i];
		  if (char.IsDigit(c))
		  {
			il.Append(c);
			i++;
		  }
		  else
		  {
			il.Append('0');
		  }
		}
		return il.ToString();
	  }

	  //
	  // EntityResolver methods
	  //
	  public override InputSource resolveEntity(string publicId, string systemId)
	  {
		// supply the internal hyphenation.dtd if possible
		if ((systemId != null && systemId.matches("(?i).*\\bhyphenation.dtd\\b.*")) || ("hyphenation-info".Equals(publicId)))
		{
		  // System.out.println(this.getClass().getResource("hyphenation.dtd").toExternalForm());
		  return new InputSource(this.GetType().getResource("hyphenation.dtd").toExternalForm());
		}
		return null;
	  }

	  //
	  // ContentHandler methods
	  //

	  /// <seealso cref= org.xml.sax.ContentHandler#startElement(java.lang.String,
	  ///      java.lang.String, java.lang.String, org.xml.sax.Attributes) </seealso>
	  public override void startElement(string uri, string local, string raw, Attributes attrs)
	  {
		if (local.Equals("hyphen-char"))
		{
		  string h = attrs.getValue("value");
		  if (h != null && h.Length == 1)
		  {
			hyphenChar = h[0];
		  }
		}
		else if (local.Equals("classes"))
		{
		  currElement = ELEM_CLASSES;
		}
		else if (local.Equals("patterns"))
		{
		  currElement = ELEM_PATTERNS;
		}
		else if (local.Equals("exceptions"))
		{
		  currElement = ELEM_EXCEPTIONS;
		  exception = new List<>();
		}
		else if (local.Equals("hyphen"))
		{
		  if (token.Length > 0)
		  {
			exception.Add(token.ToString());
		  }
		  exception.Add(new Hyphen(attrs.getValue("pre"), attrs.getValue("no"), attrs.getValue("post")));
		  currElement = ELEM_HYPHEN;
		}
		token.Length = 0;
	  }

	  /// <seealso cref= org.xml.sax.ContentHandler#endElement(java.lang.String,
	  ///      java.lang.String, java.lang.String) </seealso>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Override @SuppressWarnings("unchecked") public void endElement(String uri, String local, String raw)
	  public override void endElement(string uri, string local, string raw)
	  {

		if (token.Length > 0)
		{
		  string word = token.ToString();
		  switch (currElement)
		  {
			case ELEM_CLASSES:
			  consumer.addClass(word);
			  break;
			case ELEM_EXCEPTIONS:
			  exception.Add(word);
			  exception = normalizeException(exception);
			  consumer.addException(getExceptionWord(exception), (ArrayList) exception.clone());
			  break;
			case ELEM_PATTERNS:
			  consumer.addPattern(getPattern(word), getInterletterValues(word));
			  break;
			case ELEM_HYPHEN:
			  // nothing to do
			  break;
		  }
		  if (currElement != ELEM_HYPHEN)
		  {
			token.Length = 0;
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

	  /// <seealso cref= org.xml.sax.ContentHandler#characters(char[], int, int) </seealso>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings("unchecked") @Override public void characters(char ch[] , int start, int length)
	  public override void characters(char[] ch, int start, int length)
	  {
		StringBuilder chars = new StringBuilder(length);
		chars.Append(ch, start, length);
		string word = readToken(chars);
		while (word != null)
		{
		  // System.out.println("\"" + word + "\"");
		  switch (currElement)
		  {
			case ELEM_CLASSES:
			  consumer.addClass(word);
			  break;
			case ELEM_EXCEPTIONS:
			  exception.Add(word);
			  exception = normalizeException(exception);
			  consumer.addException(getExceptionWord(exception), (ArrayList) exception.clone());
			  exception.Clear();
			  break;
			case ELEM_PATTERNS:
			  consumer.addPattern(getPattern(word), getInterletterValues(word));
			  break;
		  }
		  word = readToken(chars);
		}

	  }

	  /// <summary>
	  /// Returns a string of the location.
	  /// </summary>
	  private string getLocationString(SAXParseException ex)
	  {
		StringBuilder str = new StringBuilder();

		string systemId = ex.SystemId;
		if (systemId != null)
		{
		  int index = systemId.LastIndexOf('/');
		  if (index != -1)
		  {
			systemId = systemId.Substring(index + 1);
		  }
		  str.Append(systemId);
		}
		str.Append(':');
		str.Append(ex.LineNumber);
		str.Append(':');
		str.Append(ex.ColumnNumber);

		return str.ToString();

	  } // getLocationString(SAXParseException):String
	}

}