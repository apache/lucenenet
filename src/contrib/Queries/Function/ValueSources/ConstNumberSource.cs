using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public abstract class ConstNumberSource : ValueSource
    {
        public abstract int GetInt();
        public abstract long GetLong();
        public abstract float GetFloat();
        public abstract double GetDouble();
        public abstract object GetNumber();
        public abstract bool GetBool();
    }
}
