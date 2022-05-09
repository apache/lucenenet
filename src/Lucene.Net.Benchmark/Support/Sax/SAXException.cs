// SAX exception class.
// http://www.saxproject.org
// No warranty; no copyright -- use this as you will.
// $Id: SAXException.java,v 1.7 2002/01/30 21:13:48 dbrownell Exp $

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
    /// Encapsulate a general SAX error or warning.
    /// </summary>
    /// <remarks>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with<strong> NO WARRANTY</strong>.</em>
    /// See<a href='http://www.saxproject.org'>http://www.saxproject.org</a>
    /// for further information.
    /// <para/>
    /// This class can contain basic error or warning information from
    /// either the XML parser or the application: a parser writer or
    /// application writer can subclass it to provide additional
    /// functionality. SAX handlers may throw this exception or
    /// any exception subclassed from it.
    /// <para/>
    /// If the application needs to pass through other types of
    /// exceptions, it must wrap those exceptions in a <see cref="SAXException"/>
    /// or an exception derived from a <see cref="SAXException"/>.
    /// <para/>
    /// If the parser or application needs to include information about a
    /// specific location in an XML document, it should use the
    /// <see cref="SAXParseException"/> subclass.
    /// </remarks>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    public class SAXException : Exception
    {
        /// <summary>
        /// Create a new <see cref="SAXException"/>.
        /// </summary>
        public SAXException()
            : base()
        {
            this.exception = null;
        }

        /// <summary>
        /// Create a new <see cref="SAXException"/>.
        /// </summary>
        /// <param name="message">The error or warning message.</param>
        public SAXException(string message)
            : base(message)
        {
            this.exception = null;
        }

        /// <summary>
        /// Create a new <see cref="SAXException"/> wrapping an existing exception.
        /// </summary>
        /// <remarks>
        /// The existing exception will be embedded in the new
        /// one, and its message will become the default message for
        /// the <see cref="SAXException"/>.
        /// </remarks>
        /// <param name="e">The exception to be wrapped in a <see cref="SAXException"/>.</param>
        public SAXException(Exception e)
            : base()
        {
            this.exception = e;
        }

        /// <summary>
        /// Create a new <see cref="SAXException"/> from an existing exception.
        /// </summary>
        /// <remarks>
        /// The existing exception will be embedded in the new
        /// one, but the new exception will have its own message.
        /// </remarks>
        /// <param name="message">The detail message.</param>
        /// <param name="e">The exception to be wrapped in a SAXException.</param>
        public SAXException(string message, Exception e)
            : base(message)
        {
            this.exception = e;
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected SAXException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            exception = (Exception)info.GetValue("Exception", typeof(Exception));
        }

#if FEATURE_CODE_ACCESS_SECURITY
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Exception", exception, typeof(Exception));
        }
#endif

        /// <summary>
        /// Return a detail message for this exception.
        /// </summary>
        /// <remarks>
        /// If there is an embedded exception, and if the SAXException
        /// has no detail message of its own, this method will return
        /// the detail message from the embedded exception.
        /// </remarks>
        public override string Message
        {
            get
            {
                string message = base.Message;

                if (message is null && exception != null)
                {
                    return exception.Message;
                }
                else
                {
                    return message;
                }
            }
        }

        /// <summary>
        /// Gets the embedded exception, if any, or <c>null</c> if there is none.
        /// </summary>
        public virtual Exception Exception => exception;

        /// <summary>
        /// Override ToString to pick up any embedded exception.
        /// </summary>
        /// <returns>A string representation of this exception.</returns>
        public override string ToString()
        {
            if (exception != null)
            {
                return exception.ToString();
            }
            else
            {
                return base.ToString();
            }
        }



        //////////////////////////////////////////////////////////////////////
        // Internal state.
        //////////////////////////////////////////////////////////////////////


        /// <summary>
        /// The embedded exception if tunnelling, or null.
        /// </summary>
        [NonSerialized]
        private readonly Exception exception; // LUCENENET: marked readonly

    }
}
