using System;

namespace Lucene.Net.Support
{
    [Serializable]
    internal sealed class BufferUnderflowException : Exception
    {
    }

    [Serializable]
    internal sealed class BufferOverflowException : Exception
    {
    }

    [Serializable]
    internal sealed class ReadOnlyBufferException : Exception
    {
    }

    [Serializable]
    internal sealed class InvalidMarkException : Exception
    {
    }
}