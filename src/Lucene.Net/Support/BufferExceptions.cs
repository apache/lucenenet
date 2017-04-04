using System;
#if FEATURE_SERIALIZABLE
using System.Runtime.Serialization;
#endif

namespace Lucene.Net.Support
{
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal sealed class BufferUnderflowException : Exception
    {
        public BufferUnderflowException()
        {
        }

#if FEATURE_SERIALIZABLE
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        public BufferUnderflowException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal sealed class BufferOverflowException : Exception
    {
        public BufferOverflowException()
        {
        }

#if FEATURE_SERIALIZABLE
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        public BufferOverflowException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal sealed class ReadOnlyBufferException : Exception
    {
        public ReadOnlyBufferException()
        {
        }

#if FEATURE_SERIALIZABLE
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        public ReadOnlyBufferException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal sealed class InvalidMarkException : Exception
    {
        public InvalidMarkException()
        {
        }

#if FEATURE_SERIALIZABLE
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        public InvalidMarkException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}