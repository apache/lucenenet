using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Util
{
    public static class TaxonomyMergeUtils
    {
        public static void Merge(Directory srcIndexDir, Directory srcTaxDir, DirectoryTaxonomyWriter.IOrdinalMap map, 
            IndexWriter destIndexWriter, DirectoryTaxonomyWriter destTaxWriter, FacetIndexingParams params_renamed)
        {
            destTaxWriter.AddTaxonomy(srcTaxDir, map);
            int[] ordinalMap = map.GetMap();
            DirectoryReader reader = DirectoryReader.Open(srcIndexDir, -1);
            IList<AtomicReaderContext> leaves = reader.Leaves;
            int numReaders = leaves.Count;
            AtomicReader[] wrappedLeaves = new AtomicReader[numReaders];
            for (int i = 0; i < numReaders; i++)
            {
                wrappedLeaves[i] = new OrdinalMappingAtomicReader(leaves[i].AtomicReader, ordinalMap, params_renamed);
            }

            try
            {
                destIndexWriter.AddIndexes(new MultiReader(wrappedLeaves));
                destTaxWriter.Commit();
                destIndexWriter.Commit();
            }
            finally
            {
                reader.Dispose();
            }
        }
    }
}
