using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Lucene
{
    /// <summary>
    /// The Java description is:
    /// Defines a general exception a servlet can throw when it encounters difficulty.
    /// <para/>
    /// This is a Java compatibility exception, and should be thrown in
    /// Lucene.NET everywhere Lucene throws it.
    /// </summary>

    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    internal class ServletException : Exception
    {
        [Obsolete("Use ServletException.Create() instead.", error: true)]
        public ServletException()
        {
        }

        [Obsolete("Use ServletException.Create() instead.", error: true)]
        public ServletException(string message) : base(message)
        {
        }

        [Obsolete("Use ServletException.Create() instead.", error: true)]
        public ServletException(string message, Exception innerException) : base(message, innerException)
        {
        }

        [Obsolete("Use ServletException.Create() instead.", error: true)]
        public ServletException(Exception cause)
            : base(cause?.ToString(), cause)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected ServletException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif

        // Static factory methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create() => new HttpRequestException();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message) => new HttpRequestException(message);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message, Exception innerException) => new HttpRequestException(message, innerException);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(Exception cause) => new HttpRequestException(cause.Message, cause);
    }
}
