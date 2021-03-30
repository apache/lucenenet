---
uid: Lucene.Net.Facet
summary: *content
---

<!--
 Licensed to the Apache Software Foundation (ASF) under one or more
 contributor license agreements.  See the NOTICE file distributed with
 this work for additional information regarding copyright ownership.
 The ASF licenses this file to You under the Apache License, Version 2.0
 (the "License"); you may not use this file except in compliance with
 the License.  You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
-->

# Lucene.Net.Facet Faceted Search

 This module provides multiple methods for computing facet counts and value aggregations:

* Taxonomy-based methods rely on a separate taxonomy index to map hierarchical facet paths to global int ordinals for fast counting at search time; these methods can compute counts ((<xref:Lucene.Net.Facet.Taxonomy.FastTaxonomyFacetCounts>, <xref:Lucene.Net.Facet.Taxonomy.TaxonomyFacetCounts>) aggregate long or double values <xref:Lucene.Net.Facet.Taxonomy.TaxonomyFacetSumInt32Associations>, <xref:Lucene.Net.Facet.Taxonomy.TaxonomyFacetSumSingleAssociations>, <xref:Lucene.Net.Facet.Taxonomy.TaxonomyFacetSumValueSource>. Add <xref:Lucene.Net.Facet.FacetField> or <xref:Lucene.Net.Facet.Taxonomy.AssociationFacetField> to your documents at index time to use taxonomy-based methods.
* Sorted-set doc values method does not require a separate taxonomy index, and computes counts based on sorted set doc values fields (<xref:Lucene.Net.Facet.SortedSet.SortedSetDocValuesFacetCounts>). Add <xref:Lucene.Net.Facet.SortedSet.SortedSetDocValuesFacetField> to your documents at index time to use sorted set facet counts.
* Range faceting <xref:Lucene.Net.Facet.Range.Int64RangeFacetCounts>, <xref:Lucene.Net.Facet.Range.DoubleRangeFacetCounts> compute counts for a dynamic numeric range from a provided <xref:Lucene.Net.Queries.Function.ValueSource> (previously indexed numeric field, or a dynamic expression such as distance). 

 At search time you first run your search, but pass a <xref:Lucene.Net.Facet.FacetsCollector> to gather all hits (and optionally, scores for each hit). Then, instantiate whichever facet methods you'd like to use to compute aggregates. Finally, all methods implement a common <xref:Lucene.Net.Facet.Facets> base API that you use to obtain specific facet counts. 

 The various [FacetsCollector.Search()](xref:Lucene.Net.Facet.FacetsCollector#Lucene_Net_Facet_FacetsCollector_Search_Lucene_Net_Search_IndexSearcher_Lucene_Net_Search_Query_Lucene_Net_Search_Filter_System_Int32_Lucene_Net_Search_ICollector_) utility methods are useful for doing an "ordinary" search (sorting by score, or by a specified Sort) but also collecting into a <xref:Lucene.Net.Facet.FacetsCollector> for subsequent faceting. 