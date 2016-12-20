using System;

namespace Lucene.Net.Index
{
    // LUCENENET TODO: This exception is being caught, but not thrown. 
    // It is LUCENENET specific. If we can make use of it, move to Support.
    // If not, it should be deleted.
    // LUCENENET: All exeption classes should be marked serializable
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    internal class NoSuchFileException : Exception
    {
    }
}