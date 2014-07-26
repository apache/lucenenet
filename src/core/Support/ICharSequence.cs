namespace Lucene.Net.Support
{
    public interface ICharSequence
    {
        int Length { get; }

        char CharAt(int index);

        ICharSequence SubSequence(int start, int end);
    }
}