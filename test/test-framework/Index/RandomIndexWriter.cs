using System;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Randomized;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Version = System.Version;

namespace Lucene.Net
{
public class RandomIndexWriter : IDisposable {

  public IndexWriter w;
  private Random r;
  int docCount;
  int flushAt;
  private double flushAtFactor = 1.0;
  private bool getReaderCalled;
  private Codec codec; // sugar

  // Randomly calls Thread.yield so we mixup thread scheduling
  private class MockIndexWriter : IndexWriter {

    private Random r;

    public MockIndexWriter(Random r, Directory dir, IndexWriterConfig conf) : base(dir, conf) {
      // TODO: this should be solved in a different way; Random should not be shared (!).
      this.r = new Random(r.nextLong());
    }

    override bool testPoint(String name) {
      if (r.nextInt(4) == 2)
        Thread.yield();
      return true;
    }
  }

  /** create a RandomIndexWriter with a random config: Uses TEST_VERSION_CURRENT and MockAnalyzer */
  public RandomIndexWriter(Random r, Directory dir):
    this(r, dir, LuceneTestCase.newIndexWriterConfig(r, LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(r)))
  {
  }
  
  /** create a RandomIndexWriter with a random config: Uses TEST_VERSION_CURRENT */
  public RandomIndexWriter(Random r, Directory dir, Analyzer a) {
    this(r, dir, LuceneTestCase.newIndexWriterConfig(r, LuceneTestCase.TEST_VERSION_CURRENT, a));
  }
  
  /** create a RandomIndexWriter with a random config */
  public RandomIndexWriter(Random r, Directory dir, Version v, Analyzer a) {
    this(r, dir, LuceneTestCase.newIndexWriterConfig(r, v, a));
  }
  
  /** create a RandomIndexWriter with the provided config */
  public RandomIndexWriter(Random r, Directory dir, IndexWriterConfig c) {
    // TODO: this should be solved in a different way; Random should not be shared (!).
    this.r = new Random(r.nextLong());
    w = new MockIndexWriter(r, dir, c);
    flushAt = _TestUtil.nextInt(r, 10, 1000);
    codec = w.getConfig().getCodec();
    if (LuceneTestCase.VERBOSE) {
      Console.WriteLine("RIW dir=" + dir + " config=" + w.getConfig());
      Console.WriteLine("codec default=" + codec.getName());
    }

    // Make sure we sometimes test indices that don't get
    // any forced merges:
    doRandomForceMerge = r.nextBoolean();
  } 
  
  /**
   * Adds a Document.
   * @see IndexWriter#addDocument(Iterable)
   */
  public <T extends IndexableField> void addDocument(Iterable<T> doc) {
    addDocument(doc, w.getAnalyzer());
  }

  public <T extends IndexableField> void addDocument(final Iterable<T> doc, Analyzer a) {
    if (r.nextInt(5) == 3) {
      // TODO: maybe, we should simply buffer up added docs
      // (but we need to clone them), and only when
      // getReader, commit, etc. are called, we do an
      // addDocuments?  Would be better testing.
      w.AddDocuments(new Iterable<Iterable<T>>() {

        public Iterator<Iterable<T>> iterator() {
          return new Iterator<Iterable<T>>() {
            boolean done;
            
            @Override
            public boolean hasNext() {
              return !done;
            }

            @Override
            public void remove() {
              throw new UnsupportedOperationException();
            }

            @Override
            public Iterable<T> next() {
              if (done) {
                throw new IllegalStateException();
              }
              done = true;
              return doc;
            }
          };
        }
        }, a);
    } else {
      w.AddDocument(doc, a);
    }
    
    maybeCommit();
  }

  private void maybeCommit() {
    if (docCount++ == flushAt) {
      if (LuceneTestCase.VERBOSE) {
        Console.WriteLine("RIW.add/updateDocument: now doing a commit at docCount=" + docCount);
      }
      w.Commit();
      flushAt += _TestUtil.nextInt(r, (int) (flushAtFactor * 10), (int) (flushAtFactor * 1000));
      if (flushAtFactor < 2e6) {
        // gradually but exponentially increase time b/w flushes
        flushAtFactor *= 1.05;
      }
    }
    }
  
  public void addDocuments(Iterable<? extends Iterable<? extends IIndexableField>> docs) {
    w.AddDocuments(docs);
    maybeCommit();
  }

  public void updateDocuments(Term delTerm, Iterable<? extends Iterable<? extends IndexableField>> docs) {
    w.UpdateDocuments(delTerm, docs);
    maybeCommit();
  }

  /**
   * Updates a document.
   * @see IndexWriter#updateDocument(Term, Iterable)
   */
  public <T extends IndexableField> void updateDocument(Term t, final Iterable<T> doc) {
    if (r.nextInt(5) == 3) {
      w.updateDocuments(t, new Iterable<Iterable<T>>() {

        @Override
        public Iterator<Iterable<T>> iterator() {
          return new Iterator<Iterable<T>>() {
            boolean done;
            
            @Override
            public boolean hasNext() {
              return !done;
            }

            @Override
            public void remove() {
              throw new UnsupportedOperationException();
            }

            @Override
            public Iterable<T> next() {
              if (done) {
                throw new IllegalStateException();
              }
              done = true;
              return doc;
            }
          };
        }
        });
    } else {
      w.UpdateDocument(t, doc);
    }
    maybeCommit();
  }
  
  public void addIndexes(params[] Directory dirs) {
    w.AddIndexes(dirs);
  }

  public void addIndexes(IndexReader... readers) {
    w.AddIndexes(readers);
  }
  
  public void deleteDocuments(Term term) {
    w.DeleteDocuments(term);
  }

  public void deleteDocuments(Query q) {
    w.DeleteDocuments(q);
  }
  
  public void commit() {
    w.Commit();
  }
  
  public int numDocs() {
    return w.NumDocs;
  }

  public int maxDoc() {
    return w.MaxDoc;
  }

  public void deleteAll() {
    w.DeleteAll();
  }

  public DirectoryReader getReader() {
    return getReader(true);
  }

  private bool doRandomForceMerge = true;
  private bool doRandomForceMergeAssert = true;

  public void forceMergeDeletes(bool doWait) {
    w.ForceMergeDeletes(doWait);
  }

  public void forceMergeDeletes() {
    w.ForceMergeDeletes();
  }

  public void setDoRandomForceMerge(bool v) {
    doRandomForceMerge = v;
  }

  public void setDoRandomForceMergeAssert(bool v) {
    doRandomForceMergeAssert = v;
  }

  private void doRandomForceMerge() {
    if (doRandomForceMerge) {
      int segCount = w.SegmentCount;
      if (r.nextBoolean() || segCount == 0) {
        // full forceMerge
        if (LuceneTestCase.VERBOSE) {
          Console.WriteLine("RIW: doRandomForceMerge(1)");
        }
        w.ForceMerge(1);
      } else {
        // partial forceMerge
        int limit = _TestUtil.nextInt(r, 1, segCount);
        if (LuceneTestCase.VERBOSE) {
          Console.WriteLine("RIW: doRandomForceMerge(" + limit + ")");
        }
        w.ForceMerge(limit);
        //assert !doRandomForceMergeAssert || w.getSegmentCount() <= limit: "limit=" + limit + " actual=" + w.getSegmentCount();
      }
    }
  }

  public DirectoryReader getReader(boolean applyDeletions) {
    getReaderCalled = true;
    if (r.nextInt(20) == 2) {
      doRandomForceMerge();
    }
    // If we are writing with PreFlexRW, force a full
    // IndexReader.open so terms are sorted in codepoint
    // order during searching:
    if (!applyDeletions || !codec.getName().equals("Lucene3x") && r.nextBoolean()) {
      if (LuceneTestCase.VERBOSE) {
        System.out.println("RIW.getReader: use NRT reader");
      }
      if (r.nextInt(5) == 1) {
        w.Commit();
      }
      return w.getReader(applyDeletions);
    } else {
      if (LuceneTestCase.VERBOSE) {
        System.out.println("RIW.getReader: open new reader");
      }
      w.Commit();
      if (r.nextBoolean()) {
        return DirectoryReader.Open(w.Directory, _TestUtil.nextInt(r, 1, 10));
      } else {
        return w.getReader(applyDeletions);
      }
    }
  }

  /**
   * Close this writer.
   * @see IndexWriter#close()
   */
  public void close() {
    // if someone isn't using getReader() API, we want to be sure to
    // forceMerge since presumably they might open a reader on the dir.
    if (getReaderCalled == false && r.nextInt(8) == 2) {
      doRandomForceMerge();
    }
    w.Close();
  }

  /**
   * Forces a forceMerge.
   * <p>
   * NOTE: this should be avoided in tests unless absolutely necessary,
   * as it will result in less test coverage.
   * @see IndexWriter#forceMerge(int)
   */
  public void ForceMerge(int maxSegmentCount) {
    w.ForceMerge(maxSegmentCount);
  }
}

}
