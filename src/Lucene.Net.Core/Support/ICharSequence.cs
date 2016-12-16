namespace Lucene.Net.Support
{
    public interface ICharSequence
    {
        int Length { get; }

        // LUCENENET specific - removed CharAt() and replaced with this[int] to .NETify
        //char CharAt(int index);

        char this[int index] { get; }

        ICharSequence SubSequence(int start, int end);
    }
}