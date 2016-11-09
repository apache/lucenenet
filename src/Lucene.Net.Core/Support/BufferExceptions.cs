using System;

namespace Lucene.Net.Support
{
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal sealed class BufferUnderflowException : Exception
    {
    }

#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal sealed class BufferOverflowException : Exception
    {
    }

#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal sealed class ReadOnlyBufferException : Exception
    {
    }

#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal sealed class InvalidMarkException : Exception
    {
    }
}