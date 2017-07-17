using System.Collections.Generic;
using System.Composition;
using Microsoft.DocAsCode.Dfm;

namespace LuceneDocsPlugins
{
    /// <summary>
    /// Exports our custom renderer via MEF to DocFx
    /// </summary>
    [Export(typeof(IDfmCustomizedRendererPartProvider))]
    public class LuceneRendererPartProvider : IDfmCustomizedRendererPartProvider
    {
        public IEnumerable<IDfmCustomizedRendererPart> CreateParts(IReadOnlyDictionary<string, object> parameters)
        {
            yield return new LuceneTokenRendererPart();
        }
    }
}