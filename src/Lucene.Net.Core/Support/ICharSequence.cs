namespace Lucene.Net.Support
{
    public interface ICharSequence
    {
        int Length { get; }

        // LUCENENET TODO: Remove CharAt and change all references to use the .NETified indexer this[index].
        // Make sure to update all code and all tests to reflect this change.
        char CharAt(int index);

        char this[int index] { get; }

        ICharSequence SubSequence(int start, int end);
    }
}