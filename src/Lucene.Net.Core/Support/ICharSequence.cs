using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Support
{
    public interface ICharSequence
    {
        int Length { get; }

        char CharAt(int index);

        ICharSequence SubSequence(int start, int end);
    }
}
