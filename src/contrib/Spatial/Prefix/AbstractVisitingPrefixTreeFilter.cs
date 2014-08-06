using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Util;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Prefix
{
    public abstract class AbstractVisitingPrefixTreeFilter : AbstractPrefixTreeFilter
    {
        //Historical note: this code resulted from a refactoring of RecursivePrefixTreeFilter,
        // which in turn came out of SOLR-2155

        protected readonly int prefixGridScanLevel; //at least one less than grid.getMaxLevels()

        protected AbstractVisitingPrefixTreeFilter(Shape queryShape, String fieldName, SpatialPrefixTree grid, int detailLevel, int prefixGridScanLevel)
            : base(queryShape, fieldName, grid, detailLevel)
        {
            this.prefixGridScanLevel = Math.Max(0, Math.Min(prefixGridScanLevel, grid.GetMaxLevels() - 1));
            System.Diagnostics.Debug.Assert(detailLevel <= grid.GetMaxLevels());
        }

        public override bool Equals(object o)
        {
            if (!base.Equals(o)) return false;//checks getClass == o.getClass & instanceof
            var that = (AbstractVisitingPrefixTreeFilter)o;
            if (prefixGridScanLevel != that.prefixGridScanLevel) return false;
            return true;
        }

        public override int GetHashCode()
        {
            var result = base.GetHashCode();
            result = 31 * result + prefixGridScanLevel;
            return result;
        }

        /**
   * An abstract class designed to make it easy to implement predicates or
   * other operations on a {@link SpatialPrefixTree} indexed field. An instance
   * of this class is not designed to be re-used across AtomicReaderContext
   * instances so simply create a new one for each call to, say a {@link
   * org.apache.lucene.search.Filter#getDocIdSet(org.apache.lucene.index.AtomicReaderContext, org.apache.lucene.util.Bits)}.
   * The {@link #getDocIdSet()} method here starts the work. It first checks
   * that there are indexed terms; if not it quickly returns null. Then it calls
   * {@link #start()} so a subclass can set up a return value, like an
   * {@link org.apache.lucene.util.OpenBitSet}. Then it starts the traversal
   * process, calling {@link #findSubCellsToVisit(org.apache.lucene.spatial.prefix.tree.Cell)}
   * which by default finds the top cells that intersect {@code queryShape}. If
   * there isn't an indexed cell for a corresponding cell returned for this
   * method then it's short-circuited until it finds one, at which point
   * {@link #visit(org.apache.lucene.spatial.prefix.tree.Cell)} is called. At
   * some depths, of the tree, the algorithm switches to a scanning mode that
   * finds calls {@link #visitScanned(org.apache.lucene.spatial.prefix.tree.Cell)}
   * for each leaf cell found.
   *
   * @lucene.internal
   */

        public abstract class VisitorTemplate : BaseTermsEnumTraverser
        {
            /* Future potential optimizations:

        * Can a polygon query shape be optimized / made-simpler at recursive depths
        (e.g. intersection of shape + cell box)

        * RE "scan" vs divide & conquer performance decision:
        We should use termsEnum.docFreq() as an estimate on the number of places at
        this depth.  It would be nice if termsEnum knew how many terms
        start with the current term without having to repeatedly next() & test to find out.

      */

            protected readonly bool hasIndexedLeaves; //if false then we can skip looking for them

            private VNode curVNode; //current pointer, derived from query shape
            private BytesRef curVNodeTerm = new BytesRef(); //curVNode.cell's term.
            private Node scanCell;

            private BytesRef thisTerm;//the result of termsEnum.term()

            protected VisitorTemplate(AtomicReaderContext context, IBits acceptDocs, bool hasIndexedLeaves) : base(context, acceptDocs)
            {
                this.hasIndexedLeaves = hasIndexedLeaves;
            }

            public DocIdSet GetDocIdSet()  
            {
                //assert curVNode == null : "Called more than once?";
                if (termsEnum == null)
                {
                    return null;
                }
                //advance
                if ((thisTerm = termsEnum.Next()) == null)
                {
                    return null; // all done
                }

                curVNode = new VNode(null);
      curVNode.reset(grid.getWorldCell());

      start();

      addIntersectingChildren();

      main: while (thisTerm != null) {//terminates for other reasons too!

        //Advance curVNode pointer
        if (curVNode.children != null) {
          //-- HAVE CHILDREN: DESCEND
          assert curVNode.children.hasNext();//if we put it there then it has something
          preSiblings(curVNode);
          curVNode = curVNode.children.next();
        } else {
          //-- NO CHILDREN: ADVANCE TO NEXT SIBLING
          VNode parentVNode = curVNode.parent;
          while (true) {
            if (parentVNode == null)
              break main; // all done
            if (parentVNode.children.hasNext()) {
              //advance next sibling
              curVNode = parentVNode.children.next();
              break;
            } else {
              //reached end of siblings; pop up
              postSiblings(parentVNode);
              parentVNode.children = null;//GC
              parentVNode = parentVNode.parent;
            }
          }
        }

        //Seek to curVNode's cell (or skip if termsEnum has moved beyond)
        curVNodeTerm.bytes = curVNode.Node.getTokenBytes();
        curVNodeTerm.length = curVNodeTerm.bytes.length;
        int compare = termsEnum.getComparator().compare(thisTerm, curVNodeTerm);
        if (compare > 0) {
          // leap frog (termsEnum is beyond where we would otherwise seek)
          assert ! context.reader().terms(fieldName).iterator(null).seekExact(curVNodeTerm, false) : "should be absent";
        } else {
          if (compare < 0) {
            // Seek !
            TermsEnum.SeekStatus seekStatus = termsEnum.seekCeil(curVNodeTerm, true);
            if (seekStatus == TermsEnum.SeekStatus.END)
              break; // all done
            thisTerm = termsEnum.term();
            if (seekStatus == TermsEnum.SeekStatus.NOT_FOUND) {
              continue; // leap frog
            }
          }
          // Visit!
          boolean descend = visit(curVNode.cell);
          //advance
          if ((thisTerm = termsEnum.next()) == null)
            break; // all done
          if (descend)
            addIntersectingChildren();

        }

      }//main loop

      return finish();
    }

















        }
    }
