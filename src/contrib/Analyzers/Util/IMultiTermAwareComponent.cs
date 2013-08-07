using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Util
{
    public interface IMultiTermAwareComponent
    {
        AbstractAnalysisFactory MultiTermComponent { get; }
    }
}
