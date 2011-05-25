SimpleFacetedSearch: Dynamic clustering of search results into categories according to values in given field(s).

Sample Usage:

    //Should be created only when IndexReader is opened/reopened. Creation with every search can be performance killer
    SimpleFacetedSearch sfs = new SimpleFacetedSearch(indexReader, new string[] { "source", "category" });

    Query query = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, field, analyzer).Parse(searchString);
    SimpleFacetedSearch.Hits hits = sfs.Search(query, 10);
       
    foreach (SimpleFacetedSearch.HitsPerGroup hpg in hits.HitsPerGroup)
    {
        SimpleFacetedSearch.GroupName name = hpg.Name;
        foreach (Document doc in hpg.Documents)
        {
             ........
        }
    }



PS: Hits.TotalHitCount & HitsPerGroup.HitCount properties are costly operations. Try to avoid using them if possible.
