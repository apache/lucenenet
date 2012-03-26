using System;
using Lucene.Net.Search;

namespace Lucene.Net.Index.Memory
{
    private static final Term MATCH_ALL_TERM = new Term("");
    
    /**
    * Search support for Lucene framework integration; implements all methods
    * required by the Lucene IndexReader contracts.
    */
    internal class MemoryIndexReader : IndexReader {
    
        private Searcher searcher; // needed to find searcher.getSimilarity() 
    
        private MemoryIndexReader() {}
    
        private Info getInfo(String fieldName) 
        {
            return fields.get(fieldName);
        }
        
        private Info getInfo(int pos) 
        {
            return sortedFields[pos].getValue();
        }
    
        public override int docFreq(Term term) 
        {
            Info info = getInfo(term.field());
            int freq = 0;
            if (info != null) freq = info.getPositions(term.text()) != null ? 1 : 0;
            if (DEBUG) System.err.println("MemoryIndexReader.docFreq: " + term + ", freq:" + freq);
            return freq;
        }
  
        public TermEnum terms() 
        {
            if (DEBUG) System.err.println("MemoryIndexReader.terms()");
            return terms(MATCH_ALL_TERM);
        }
    
        public TermEnum terms(Term term) 
        {
            if (DEBUG) System.err.println("MemoryIndexReader.terms: " + term);
  
            int i; // index into info.sortedTerms
            int j; // index into sortedFields
      
            sortFields();
            if (sortedFields.length == 1 && sortedFields[0].getKey() == term.field()) {
                j = 0; // fast path
            } else {
                j = Arrays.binarySearch(sortedFields, term.field(), termComparator);
            }
      
            if (j < 0) { // not found; choose successor
                j = -j -1; 
                i = 0;
                if (j < sortedFields.length) 
                    getInfo(j).sortTerms();
            } else { // found
                Info info = getInfo(j);
                info.sortTerms();
                i = Arrays.binarySearch(info.sortedTerms, term.text(), termComparator);
                if (i < 0) { // not found; choose successor
                    i = -i -1;
                
                    if (i >= info.sortedTerms.length) { // move to next successor
                        j++;
                        i = 0;
                        if (j < sortedFields.length) getInfo(j).sortTerms();
                    }
                }
            }
    
            final int ix = i;
            final int jx = j;
  
    return new TermEnum() {
  
    }
  
    @Override
    public TermPositions termPositions() {
    if (DEBUG) System.err.println("MemoryIndexReader.termPositions");
      
    return new TermPositions() {
  
    private boolean hasNext;
    private int cursor = 0;
    private List<Int32> current;
    private Term term;
        
    public void seek(Term term) {
    this.term = term;
    if (DEBUG) System.err.println(".seek: " + term);
    if (term == null) {
    hasNext = true;  // term==null means match all docs
    } else {
    Info info = getInfo(term.field());
    current = info == null ? null : info.getPositions(term.text());
    hasNext = (current != null);
    cursor = 0;
    }
    }
  
    public void seek(TermEnum termEnum) {
    if (DEBUG) System.err.println(".seekEnum");
    seek(termEnum.term());
    }
  
    public int doc() {
    if (DEBUG) System.err.println(".doc");
    return 0;
    }
  
    public int freq() {
    int freq = current != null ? numPositions(current) : (term == null ? 1 : 0);
    if (DEBUG) System.err.println(".freq: " + freq);
    return freq;
    }
  
    public boolean next() {
    if (DEBUG) System.err.println(".next: " + current + ", oldHasNext=" + hasNext);
    boolean next = hasNext;
    hasNext = false;
    return next;
    }
  
    public int read(int[] docs, int[] freqs) {
    if (DEBUG) System.err.println(".read: " + docs.length);
    if (!hasNext) return 0;
    hasNext = false;
    docs[0] = 0;
    freqs[0] = freq();
    return 1;
    }
  
    public boolean skipTo(int target) {
    if (DEBUG) System.err.println(".skipTo: " + target);
    return next();
    }
  
    public void close() {
    if (DEBUG) System.err.println(".close");
    }
        
    public int nextPosition() { // implements TermPositions
    int pos = current.get(cursor);
    cursor += stride;
    if (DEBUG) System.err.println(".nextPosition: " + pos);
    return pos;
    }
        
    /**
         * Not implemented.
         * @throws UnsupportedOperationException
         */
    public int getPayloadLength() {
    throw new UnsupportedOperationException();
    }
         
    /**
         * Not implemented.
         * @throws UnsupportedOperationException
         */
    public byte[] getPayload(byte[] data, int offset) throws IOException {
    throw new UnsupportedOperationException();
    }

    public boolean isPayloadAvailable() {
    // unsuported
    return false;
    }

    };
    }
  
    @Override
    public TermDocs termDocs() {
    if (DEBUG) System.err.println("MemoryIndexReader.termDocs");
    return termPositions();
    }
  
    @Override
    public TermFreqVector[] getTermFreqVectors(int docNumber) {
    if (DEBUG) System.err.println("MemoryIndexReader.getTermFreqVectors");
    TermFreqVector[] vectors = new TermFreqVector[fields.size()];
    //      if (vectors.length == 0) return null;
    Iterator<String> iter = fields.keySet().iterator();
    for (int i=0; i < vectors.length; i++) {
    vectors[i] = getTermFreqVector(docNumber, iter.next());
    }
    return vectors;
    }

    @Override
    public void getTermFreqVector(int docNumber, TermVectorMapper mapper) throws IOException
    {
    if (DEBUG) System.err.println("MemoryIndexReader.getTermFreqVectors");

    //      if (vectors.length == 0) return null;
    for (final String fieldName : fields.keySet())
    {
    getTermFreqVector(docNumber, fieldName, mapper);
    }
    }

    @Override
    public void getTermFreqVector(int docNumber, String field, TermVectorMapper mapper) throws IOException
    {
    if (DEBUG) System.err.println("MemoryIndexReader.getTermFreqVector");
    final Info info = getInfo(field);
    if (info == null){
    return;
    }
    info.sortTerms();
    mapper.setExpectations(field, info.sortedTerms.length, stride != 1, true);
    for (int i = info.sortedTerms.length; --i >=0;){

    List<Int32> positions = info.sortedTerms[i].getValue();
    int size = positions.size();
    org.apache.lucene.index.TermVectorOffsetInfo[] offsets =
    new org.apache.lucene.index.TermVectorOffsetInfo[size / stride];

    for (int k=0, j=1; j < size; k++, j += stride) {
    int start = positions.get(j);
    int end = positions.get(j+1);
    offsets[k] = new org.apache.lucene.index.TermVectorOffsetInfo(start, end);
    }
    mapper.map(info.sortedTerms[i].getKey(),
    numPositions(info.sortedTerms[i].getValue()),
    offsets, (info.sortedTerms[i].getValue()).toArray(stride));
    }
    }

    @Override
    public TermFreqVector getTermFreqVector(int docNumber, final String fieldName) {
    if (DEBUG) System.err.println("MemoryIndexReader.getTermFreqVector");
    final Info info = getInfo(fieldName);
    if (info == null) return null; // TODO: or return empty vector impl???
    info.sortTerms();
      
    return new TermPositionVector() { 
  
    private final Map.Entry<String,List<Int32>>[] sortedTerms = info.sortedTerms;
        
    public String getField() {
    return fieldName;
    }
  
    public int size() {
    return sortedTerms.length;
    }
  
    public String[] getTerms() {
    String[] terms = new String[sortedTerms.length];
    for (int i=sortedTerms.length; --i >= 0; ) {
    terms[i] = sortedTerms[i].getKey();
    }
    return terms;
    }
  
    public int[] getTermFrequencies() {
    int[] freqs = new int[sortedTerms.length];
    for (int i=sortedTerms.length; --i >= 0; ) {
    freqs[i] = numPositions(sortedTerms[i].getValue());
    }
    return freqs;
    }
  
    public int indexOf(String term) {
    int i = Arrays.binarySearch(sortedTerms, term, termComparator);
    return i >= 0 ? i : -1;
    }
  
    public int[] indexesOf(String[] terms, int start, int len) {
    int[] indexes = new int[len];
    for (int i=0; i < len; i++) {
    indexes[i] = indexOf(terms[start++]);
    }
    return indexes;
    }
        
    // lucene >= 1.4.3
    public int[] getTermPositions(int index) {
    return sortedTerms[index].getValue().toArray(stride);
    } 
        
    // lucene >= 1.9 (remove this method for lucene-1.4.3)
    public org.apache.lucene.index.TermVectorOffsetInfo[] getOffsets(int index) {
    if (stride == 1) return null; // no offsets stored
          
    List<Int32> positions = sortedTerms[index].getValue();
    int size = positions.size();
    org.apache.lucene.index.TermVectorOffsetInfo[] offsets = 
    new org.apache.lucene.index.TermVectorOffsetInfo[size / stride];
          
    for (int i=0, j=1; j < size; i++, j += stride) {
    int start = positions.get(j);
    int end = positions.get(j+1);
    offsets[i] = new org.apache.lucene.index.TermVectorOffsetInfo(start, end);
    }
    return offsets;
    }

    };
    }

    private Similarity getSimilarity() {
    if (searcher != null) return searcher.getSimilarity();
    return Similarity.getDefault();
    }
    
    private void setSearcher(Searcher searcher) {
    this.searcher = searcher;
    }
    
    /** performance hack: cache norms to avoid repeated expensive calculations */
    private byte[] cachedNorms;
    private String cachedFieldName;
    private Similarity cachedSimilarity;
    
    @Override
    public byte[] norms(String fieldName) {
    byte[] norms = cachedNorms;
    Similarity sim = getSimilarity();
    if (fieldName != cachedFieldName || sim != cachedSimilarity) { // not cached?
    Info info = getInfo(fieldName);
    int numTokens = info != null ? info.numTokens : 0;
    int numOverlapTokens = info != null ? info.numOverlapTokens : 0;
    float boost = info != null ? info.getBoost() : 1.0f; 
    FieldInvertState invertState = new FieldInvertState(0, numTokens, numOverlapTokens, 0, boost);
    float n = sim.computeNorm(fieldName, invertState);
    byte norm = Similarity.encodeNorm(n);
    norms = new byte[] {norm};
        
    // cache it for future reuse
    cachedNorms = norms;
    cachedFieldName = fieldName;
    cachedSimilarity = sim;
    if (DEBUG) System.err.println("MemoryIndexReader.norms: " + fieldName + ":" + n + ":" + norm + ":" + numTokens);
    }
    return norms;
    }
  
    @Override
    public void norms(String fieldName, byte[] bytes, int offset) {
    if (DEBUG) System.err.println("MemoryIndexReader.norms*: " + fieldName);
    byte[] norms = norms(fieldName);
    System.arraycopy(norms, 0, bytes, offset, norms.length);
    }
  
    @Override
    protected void doSetNorm(int doc, String fieldName, byte value) {
    throw new UnsupportedOperationException();
    }
  
    @Override
    public int numDocs() {
    if (DEBUG) System.err.println("MemoryIndexReader.numDocs");
    return fields.size() > 0 ? 1 : 0;
    }
  
    @Override
    public int maxDoc() {
    if (DEBUG) System.err.println("MemoryIndexReader.maxDoc");
    return 1;
    }
  
    @Override
    public Document document(int n) {
    if (DEBUG) System.err.println("MemoryIndexReader.document");
    return new Document(); // there are no stored fields
    }

    //When we convert to JDK 1.5 make this Set<String>
    @Override
    public Document document(int n, FieldSelector fieldSelector) throws IOException {
    if (DEBUG) System.err.println("MemoryIndexReader.document");
    return new Document(); // there are no stored fields
    }

    @Override
    public boolean isDeleted(int n) {
    if (DEBUG) System.err.println("MemoryIndexReader.isDeleted");
    return false;
    }
  
    @Override
    public boolean hasDeletions() {
    if (DEBUG) System.err.println("MemoryIndexReader.hasDeletions");
    return false;
    }
  
    @Override
    protected void doDelete(int docNum) {
    throw new UnsupportedOperationException();
    }
  
    @Override
    protected void doUndeleteAll() {
    throw new UnsupportedOperationException();
    }
  
    @Override
    protected void doCommit(Map<String,String> commitUserData) {
    if (DEBUG) System.err.println("MemoryIndexReader.doCommit");
    }
  
    @Override
    protected void doClose() {
    if (DEBUG) System.err.println("MemoryIndexReader.doClose");
    }
    
    // lucene >= 1.9 (remove this method for lucene-1.4.3)
    public override Collection<String> getFieldNames(FieldOption fieldOption) {
        if (DEBUG) 
            System.err.println("MemoryIndexReader.getFieldNamesOption");
        
        if (fieldOption == FieldOption.UNINDEXED) 
            return Collections.<String>emptySet();
        
        if (fieldOption == FieldOption.INDEXED_NO_TERMVECTOR) 
            return Collections.<String>emptySet();
    
        if (fieldOption == FieldOption.TERMVECTOR_WITH_OFFSET && stride == 1) 
            return Collections.<String>emptySet();
    
        if (fieldOption == FieldOption.TERMVECTOR_WITH_POSITION_OFFSET && stride == 1) 
            return Collections.<String>emptySet();
      
        return Collections.unmodifiableSet(fields.keySet());
    }
    }

 private class AnonTermEnum : TermEnum
 {
    private int i = ix; // index into info.sortedTerms
    private int j = jx; // index into sortedFields
          
    public override bool Next() {
        if (DEBUG) System.err.println("TermEnum.next");
        if (j >= sortedFields.length) return false;
         Info info = getInfo(j);
            if (++i < info.sortedTerms.length) return true;
  
        // move to successor
        j++;
        i = 0;
        if (j >= sortedFields.length) return false;
        getInfo(j).sortTerms();
        return true;
    }
  
    public override Term Term() {
        if (DEBUG) System.err.println("TermEnum.term: " + i);
        if (j >= sortedFields.length) return null;
        Info info = getInfo(j);
        if (i >= info.sortedTerms.length) return null;
        //          if (DEBUG) System.err.println("TermEnum.term: " + i + ", " + info.sortedTerms[i].getKey());
        return createTerm(info, j, info.sortedTerms[i].getKey());
    }
        
    public override int DocFreq() {
        if (DEBUG) System.err.println("TermEnum.docFreq");
        if (j >= sortedFields.length) return 0;
        Info info = getInfo(j);
        if (i >= info.sortedTerms.length) return 0;
        return numPositions(info.getPositions(i));
    }
  
    /** Returns a new Term object, minimizing String.intern() overheads. */
    private Term CreateTerm(Info info, int pos, String text) { 
        // Assertion: sortFields has already been called before
        Term template = info.template;
        if (template == null) { // not yet cached?
        String fieldName = sortedFields[pos].getKey();
        template = new Term(fieldName);
        info.template = template;
    }
          
    return template.createTerm(text);
       
 }
}