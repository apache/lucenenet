// SAX exception class.
// http://www.saxproject.org
// No warranty; no copyright -- use this as you will.
// $Id: SAXParseException.java,v 1.11 2004/04/21 13:05:02 dmegginson Exp $

using System;
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
#endif
#if FEATURE_CODE_ACCESS_SECURITY
using System.Security.Permissions;
#endif

namespace Sax
{
    /// <summary>
    /// Encapsulate an XML parse error or warning.
    /// </summary>
    /// <remarks>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with<strong> NO WARRANTY</strong>.</em>
    /// See<a href='http://www.saxproject.org'>http://www.saxproject.org</a>
    /// for further information.
    /// <para/>
    /// This exception may include information for locating the error
    /// in the original XML document, as if it came from a <see cref="ILocator"/>
    /// object.  Note that although the application
    /// will receive a SAXParseException as the argument to the handlers
    /// in the <see cref="IErrorHandler"/> interface, 
    /// the application is not actually required to throw the exception;
    /// instead, it can simply read the information in it and take a
    /// different action.
    /// <para/>
    /// Since this exception is a subclass of <see cref="SAXException"/>, 
    /// it inherits the ability to wrap another exception.
    /// </remarks>
    /// <since>SAX 1.0</since>
    /// <author>David Megginson</author>
    /// <version>2.0.1 (sax2r2)</version>
    /// <seealso cref="SAXException"/>
    /// <seealso cref="ILocator"/>
    /// <seealso cref="IErrorHandler"/>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    public class SAXParseException : SAXException
    {
        //////////////////////////////////////////////////////////////////////
        // Constructors.
        //////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Create a new <see cref="SAXParseException"/> from a message and a <see cref="ILocator"/>.
        /// </summary>
        /// <remarks>
        /// This constructor is especially useful when an application is
        /// creating its own exception from within a <see cref="IContentHandler"/>
        /// callback.
        /// </remarks>
        /// <param name="message">The error or warning message.</param>
        /// <param name="locator">The locator object for the error or warning (may be null).</param>
        /// <seealso cref="ILocator"/>
        public SAXParseException(string message, ILocator locator)
            : base(message)
        {
            if (locator != null)
            {
                Init(locator.PublicId, locator.SystemId,
                 locator.LineNumber, locator.ColumnNumber);
            }
            else
            {
                Init(null, null, -1, -1);
            }
        }

        /// <summary>
        /// Wrap an existing exception in a SAXParseException.
        /// </summary>
        /// <remarks>
        /// This constructor is especially useful when an application is
        /// creating its own exception from within a <see cref="IContentHandler"/>
        /// callback, and needs to wrap an existing exception that is not a
        /// subclass of <see cref="SAXException"/>.
        /// </remarks>
        /// <param name="message">The error or warning message, or null to
        /// use the message from the embedded exception.</param>
        /// <param name="locator">The locator object for the error or warning (may be
        /// null).</param>
        /// <param name="e">Any exception.</param>
        /// <seealso cref="ILocator"/>
        public SAXParseException(string message, ILocator locator,
                      Exception e)
            : base(message, e)
        {
            if (locator != null)
            {
                Init(locator.PublicId, locator.SystemId,
                 locator.LineNumber, locator.ColumnNumber);
            }
            else
            {
                Init(null, null, -1, -1);
            }
        }

        /// <summary>
        /// Create a new SAXParseException.
        /// </summary>
        /// <remarks>
        /// This constructor is most useful for parser writers.
        /// <para/>
        /// All parameters except the message are as if
        /// they were provided by a <see cref="ILocator"/>.  For example, if the
        /// system identifier is a URL (including relative filename), the
        /// caller must resolve it fully before creating the exception.
        /// </remarks>
        /// <param name="message">The error or warning message.</param>
        /// <param name="publicId">The public identifier of the entity that generated the error or warning.</param>
        /// <param name="systemId">The system identifier of the entity that generated the error or warning.</param>
        /// <param name="lineNumber">The line number of the end of the text that caused the error or warning.</param>
        /// <param name="columnNumber">The column number of the end of the text that cause the error or warning.</param>
        public SAXParseException(string message, string publicId, string systemId,
                      int lineNumber, int columnNumber)
            : base(message)
        {
            Init(publicId, systemId, lineNumber, columnNumber);
        }

        /// <summary>
        /// Create a new <see cref="SAXParseException"/> with an embedded exception.
        /// </summary>
        /// <remarks>
        /// This constructor is most useful for parser writers who
        /// need to wrap an exception that is not a subclass of
        /// <see cref="SAXException"/>.
        /// <para/>
        /// All parameters except the message and exception are as if
        /// they were provided by a <see cref="ILocator"/>.  For example, if the
        /// system identifier is a URL (including relative filename), the
        /// caller must resolve it fully before creating the exception.
        /// </remarks>
        /// <param name="message">The error or warning message, or null to use the message from the embedded exception.</param>
        /// <param name="publicId">The public identifier of the entity that generated the error or warning.</param>
        /// <param name="systemId">The system identifier of the entity that generated the error or warning.</param>
        /// <param name="lineNumber">The line number of the end of the text that caused the error or warning.</param>
        /// <param name="columnNumber">The column number of the end of the text that cause the error or warning.</param>
        /// <param name="e">Another exception to embed in this one.</param>
        public SAXParseException(string message, string publicId, string systemId,
                      int lineNumber, int columnNumber, Exception e)
            : base(message, e)
        {
            Init(publicId, systemId, lineNumber, columnNumber);
        }

        /// <summary>
        /// Construct a new exception with no message.
        /// </summary>
        // LUCENENET: For testing purposes
        internal SAXParseException(string message)
            : base(message)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected SAXParseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            publicId = info.GetString("publicId");
            systemId = info.GetString("systemId");
            lineNumber = info.GetInt32("lineNumber");
            columnNumber = info.GetInt32("columnNumber");
        }

#if FEATURE_CODE_ACCESS_SECURITY
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("publicId", publicId, typeof(string));
            info.AddValue("systemId", systemId, typeof(string));
            info.AddValue("lineNumber", lineNumber, typeof(int));
            info.AddValue("columnNumber", columnNumber, typeof(int));
        }
#endif

        /// <summary>
        /// Internal initialization method.
        /// </summary>
        /// <param name="publicId">The public identifier of the entity which generated the exception, or null.</param>
        /// <param name="systemId">The system identifier of the entity which generated the exception, or null.</param>
        /// <param name="lineNumber">The line number of the error, or -1.</param>
        /// <param name="columnNumber">The column number of the error, or -1.</param>
        private void Init(string publicId, string systemId,
                   int lineNumber, int columnNumber)
        {
            this.publicId = publicId;
            this.systemId = systemId;
            this.lineNumber = lineNumber;
            this.columnNumber = columnNumber;
        }

        /// <summary>
        /// Get the public identifier of the entity where the exception occurred.
        /// Returns a string containing the public identifier, or null if none is available.
        /// </summary>
        /// <seealso cref="ILocator.PublicId"/>
        public string PublicId => this.publicId;

        /// <summary>
        /// Get the system identifier of the entity where the exception occurred.
        /// <para/>
        /// If the system identifier is a URL, it will have been resolved fully.
        /// <para/>
        /// A string containing the system identifier, or null if none is available.
        /// </summary>
        /// <seealso cref="ILocator.SystemId"/>
        public string SystemId => this.systemId;

        /// <summary>
        /// The line number of the end of the text where the exception occurred.
        /// <para/>
        /// The first line is line 1.
        /// <para/>
        /// An integer representing the line number, or -1 if none is available.
        /// </summary>
        /// <seealso cref="ILocator.LineNumber"/>
        public int LineNumber => this.lineNumber;

        /// <summary>
        /// The column number of the end of the text where the exception occurred.
        /// <para/>
        /// The first column in a line is position 1.
        /// <para/>
        /// An integer representing the column number, or -1
        /// if none is available.
        /// </summary>
        /// <seealso cref="ILocator.ColumnNumber"/>
        public int ColumnNumber => this.columnNumber;


        //////////////////////////////////////////////////////////////////////
        // Internal state.
        //////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The public identifier, or null.
        /// </summary>
        /// <seealso cref="PublicId"/>
        private string publicId;

        /// <summary>
        /// The system identifier, or null.
        /// </summary>
        /// <seealso cref="SystemId"/>
        private string systemId;

        /// <summary>
        /// The line number, or -1.
        /// </summary>
        /// <seealso cref="LineNumber"/>
        private int lineNumber;

        /// <summary>
        /// The column number, or -1.
        /// </summary>
        /// <seealso cref="ColumnNumber"/>
        private int columnNumber;
    }
}
