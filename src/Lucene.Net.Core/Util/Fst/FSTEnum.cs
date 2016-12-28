using System;
using System.Diagnostics;

namespace Lucene.Net.Util.Fst
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Can next() and advance() through the terms in an FST
    ///
    /// @lucene.experimental
    /// </summary>
    public abstract class FSTEnum<T> // LUCENENET NOTE: changed from internal to public because has public subclasses
    {
        protected readonly FST<T> fst;

        protected FST.Arc<T>[] arcs = new FST.Arc<T>[10];

        // outputs are cumulative
        protected T[] output = new T[10];

        protected readonly T NO_OUTPUT;
        protected readonly FST.BytesReader fstReader;
        protected readonly FST.Arc<T> scratchArc = new FST.Arc<T>();

        protected int upto;
        protected int targetLength;

        /// <summary>
        /// doFloor controls the behavior of advance: if it's true
        ///  doFloor is true, advance positions to the biggest
        ///  term before target.
        /// </summary>
        protected FSTEnum(FST<T> fst)
        {
            this.fst = fst;
            fstReader = fst.GetBytesReader();
            NO_OUTPUT = fst.Outputs.NoOutput;
            fst.GetFirstArc(GetArc(0));
            output[0] = NO_OUTPUT;
        }

        protected abstract int TargetLabel { get; }

        protected abstract int CurrentLabel { get; set; }

        protected abstract void Grow();

        /// <summary>
        /// Rewinds enum state to match the shared prefix between
        ///  current term and target term
        /// </summary>
        protected void RewindPrefix()
        {
            if (upto == 0)
            {
                //System.out.println("  init");
                upto = 1;
                fst.ReadFirstTargetArc(GetArc(0), GetArc(1), fstReader);
                return;
            }
            //System.out.println("  rewind upto=" + upto + " vs targetLength=" + targetLength);

            int currentLimit = upto;
            upto = 1;
            while (upto < currentLimit && upto <= targetLength + 1)
            {
                int cmp = CurrentLabel - TargetLabel;
                if (cmp < 0)
                {
                    // seek forward
                    //System.out.println("    seek fwd");
                    break;
                }
                else if (cmp > 0)
                {
                    // seek backwards -- reset this arc to the first arc
                    FST.Arc<T> arc = GetArc(upto);
                    fst.ReadFirstTargetArc(GetArc(upto - 1), arc, fstReader);
                    //System.out.println("    seek first arc");
                    break;
                }
                upto++;
            }
            //System.out.println("  fall through upto=" + upto);
        }

        protected virtual void DoNext()
        {
            //System.out.println("FE: next upto=" + upto);
            if (upto == 0)
            {
                //System.out.println("  init");
                upto = 1;
                fst.ReadFirstTargetArc(GetArc(0), GetArc(1), fstReader);
            }
            else
            {
                // pop
                //System.out.println("  check pop curArc target=" + arcs[upto].target + " label=" + arcs[upto].label + " isLast?=" + arcs[upto].isLast());
                while (arcs[upto].IsLast)
                {
                    upto--;
                    if (upto == 0)
                    {
                        //System.out.println("  eof");
                        return;
                    }
                }
                fst.ReadNextArc(arcs[upto], fstReader);
            }

            PushFirst();
        }

        // TODO: should we return a status here (SEEK_FOUND / SEEK_NOT_FOUND /
        // SEEK_END)?  saves the eq check above?

        /// <summary>
        /// Seeks to smallest term that's >= target. </summary>
        protected virtual void DoSeekCeil()
        {
            //System.out.println("    advance len=" + target.length + " curlen=" + current.length);

            // TODO: possibly caller could/should provide common
            // prefix length?  ie this work may be redundant if
            // caller is in fact intersecting against its own
            // automaton

            //System.out.println("FE.seekCeil upto=" + upto);

            // Save time by starting at the end of the shared prefix
            // b/w our current term & the target:
            RewindPrefix();
            //System.out.println("  after rewind upto=" + upto);

            FST.Arc<T> arc = GetArc(upto);
            int targetLabel = TargetLabel;
            //System.out.println("  init targetLabel=" + targetLabel);

            // Now scan forward, matching the new suffix of the target
            while (true)
            {
                //System.out.println("  cycle upto=" + upto + " arc.label=" + arc.label + " (" + (char) arc.label + ") vs targetLabel=" + targetLabel);

                if (arc.BytesPerArc != 0 && arc.Label != -1)
                {
                    // Arcs are fixed array -- use binary search to find
                    // the target.

                    FST.BytesReader @in = fst.GetBytesReader();
                    int low = arc.ArcIdx;
                    int high = arc.NumArcs - 1;
                    int mid = 0;
                    //System.out.println("do arc array low=" + low + " high=" + high + " targetLabel=" + targetLabel);
                    bool found = false;
                    while (low <= high)
                    {
                        mid = (int)((uint)(low + high) >> 1);
                        @in.Position = arc.PosArcsStart;
                        @in.SkipBytes(arc.BytesPerArc * mid + 1);
                        int midLabel = fst.ReadLabel(@in);
                        int cmp = midLabel - targetLabel;
                        //System.out.println("  cycle low=" + low + " high=" + high + " mid=" + mid + " midLabel=" + midLabel + " cmp=" + cmp);
                        if (cmp < 0)
                        {
                            low = mid + 1;
                        }
                        else if (cmp > 0)
                        {
                            high = mid - 1;
                        }
                        else
                        {
                            found = true;
                            break;
                        }
                    }

                    // NOTE: this code is dup'd w/ the code below (in
                    // the outer else clause):
                    if (found)
                    {
                        // Match
                        arc.ArcIdx = mid - 1;
                        fst.ReadNextRealArc(arc, @in);
                        Debug.Assert(arc.ArcIdx == mid);
                        Debug.Assert(arc.Label == targetLabel, "arc.label=" + arc.Label + " vs targetLabel=" + targetLabel + " mid=" + mid);
                        output[upto] = fst.Outputs.Add(output[upto - 1], arc.Output);
                        if (targetLabel == FST.END_LABEL)
                        {
                            return;
                        }
                        CurrentLabel = arc.Label;
                        Incr();
                        arc = fst.ReadFirstTargetArc(arc, GetArc(upto), fstReader);
                        targetLabel = TargetLabel;
                        continue;
                    }
                    else if (low == arc.NumArcs)
                    {
                        // Dead end
                        arc.ArcIdx = arc.NumArcs - 2;
                        fst.ReadNextRealArc(arc, @in);
                        Debug.Assert(arc.IsLast);
                        // Dead end (target is after the last arc);
                        // rollback to last fork then push
                        upto--;
                        while (true)
                        {
                            if (upto == 0)
                            {
                                return;
                            }
                            FST.Arc<T> prevArc = GetArc(upto);
                            //System.out.println("  rollback upto=" + upto + " arc.label=" + prevArc.label + " isLast?=" + prevArc.isLast());
                            if (!prevArc.IsLast)
                            {
                                fst.ReadNextArc(prevArc, fstReader);
                                PushFirst();
                                return;
                            }
                            upto--;
                        }
                    }
                    else
                    {
                        arc.ArcIdx = (low > high ? low : high) - 1;
                        fst.ReadNextRealArc(arc, @in);
                        Debug.Assert(arc.Label > targetLabel);
                        PushFirst();
                        return;
                    }
                }
                else
                {
                    // Arcs are not array'd -- must do linear scan:
                    if (arc.Label == targetLabel)
                    {
                        // recurse
                        output[upto] = fst.Outputs.Add(output[upto - 1], arc.Output);
                        if (targetLabel == FST.END_LABEL)
                        {
                            return;
                        }
                        CurrentLabel = arc.Label;
                        Incr();
                        arc = fst.ReadFirstTargetArc(arc, GetArc(upto), fstReader);
                        targetLabel = TargetLabel;
                    }
                    else if (arc.Label > targetLabel)
                    {
                        PushFirst();
                        return;
                    }
                    else if (arc.IsLast)
                    {
                        // Dead end (target is after the last arc);
                        // rollback to last fork then push
                        upto--;
                        while (true)
                        {
                            if (upto == 0)
                            {
                                return;
                            }
                            FST.Arc<T> prevArc = GetArc(upto);
                            //System.out.println("  rollback upto=" + upto + " arc.label=" + prevArc.label + " isLast?=" + prevArc.isLast());
                            if (!prevArc.IsLast)
                            {
                                fst.ReadNextArc(prevArc, fstReader);
                                PushFirst();
                                return;
                            }
                            upto--;
                        }
                    }
                    else
                    {
                        // keep scanning
                        //System.out.println("    next scan");
                        fst.ReadNextArc(arc, fstReader);
                    }
                }
            }
        }

        // TODO: should we return a status here (SEEK_FOUND / SEEK_NOT_FOUND /
        // SEEK_END)?  saves the eq check above?
        /// <summary>
        /// Seeks to largest term that's <= target. </summary>
        protected virtual void DoSeekFloor()
        {
            // TODO: possibly caller could/should provide common
            // prefix length?  ie this work may be redundant if
            // caller is in fact intersecting against its own
            // automaton
            //System.out.println("FE: seek floor upto=" + upto);

            // Save CPU by starting at the end of the shared prefix
            // b/w our current term & the target:
            RewindPrefix();

            //System.out.println("FE: after rewind upto=" + upto);

            FST.Arc<T> arc = GetArc(upto);
            int targetLabel = TargetLabel;

            //System.out.println("FE: init targetLabel=" + targetLabel);

            // Now scan forward, matching the new suffix of the target
            while (true)
            {
                //System.out.println("  cycle upto=" + upto + " arc.label=" + arc.label + " (" + (char) arc.label + ") targetLabel=" + targetLabel + " isLast?=" + arc.isLast() + " bba=" + arc.bytesPerArc);

                if (arc.BytesPerArc != 0 && arc.Label != FST.END_LABEL)
                {
                    // Arcs are fixed array -- use binary search to find
                    // the target.

                    FST.BytesReader @in = fst.GetBytesReader();
                    int low = arc.ArcIdx;
                    int high = arc.NumArcs - 1;
                    int mid = 0;
                    //System.out.println("do arc array low=" + low + " high=" + high + " targetLabel=" + targetLabel);
                    bool found = false;
                    while (low <= high)
                    {
                        mid = (int)((uint)(low + high) >> 1);
                        @in.Position = arc.PosArcsStart;
                        @in.SkipBytes(arc.BytesPerArc * mid + 1);
                        int midLabel = fst.ReadLabel(@in);
                        int cmp = midLabel - targetLabel;
                        //System.out.println("  cycle low=" + low + " high=" + high + " mid=" + mid + " midLabel=" + midLabel + " cmp=" + cmp);
                        if (cmp < 0)
                        {
                            low = mid + 1;
                        }
                        else if (cmp > 0)
                        {
                            high = mid - 1;
                        }
                        else
                        {
                            found = true;
                            break;
                        }
                    }

                    // NOTE: this code is dup'd w/ the code below (in
                    // the outer else clause):
                    if (found)
                    {
                        // Match -- recurse
                        //System.out.println("  match!  arcIdx=" + mid);
                        arc.ArcIdx = mid - 1;
                        fst.ReadNextRealArc(arc, @in);
                        Debug.Assert(arc.ArcIdx == mid);
                        Debug.Assert(arc.Label == targetLabel, "arc.label=" + arc.Label + " vs targetLabel=" + targetLabel + " mid=" + mid);
                        output[upto] = fst.Outputs.Add(output[upto - 1], arc.Output);
                        if (targetLabel == FST.END_LABEL)
                        {
                            return;
                        }
                        CurrentLabel = arc.Label;
                        Incr();
                        arc = fst.ReadFirstTargetArc(arc, GetArc(upto), fstReader);
                        targetLabel = TargetLabel;
                        continue;
                    }
                    else if (high == -1)
                    {
                        //System.out.println("  before first");
                        // Very first arc is after our target
                        // TODO: if each arc could somehow read the arc just
                        // before, we can save this re-scan.  The ceil case
                        // doesn't need this because it reads the next arc
                        // instead:
                        while (true)
                        {
                            // First, walk backwards until we find a first arc
                            // that's before our target label:
                            fst.ReadFirstTargetArc(GetArc(upto - 1), arc, fstReader);
                            if (arc.Label < targetLabel)
                            {
                                // Then, scan forwards to the arc just before
                                // the targetLabel:
                                while (!arc.IsLast && fst.ReadNextArcLabel(arc, @in) < targetLabel)
                                {
                                    fst.ReadNextArc(arc, fstReader);
                                }
                                PushLast();
                                return;
                            }
                            upto--;
                            if (upto == 0)
                            {
                                return;
                            }
                            targetLabel = TargetLabel;
                            arc = GetArc(upto);
                        }
                    }
                    else
                    {
                        // There is a floor arc:
                        arc.ArcIdx = (low > high ? high : low) - 1;
                        //System.out.println(" hasFloor arcIdx=" + (arc.arcIdx+1));
                        fst.ReadNextRealArc(arc, @in);
                        Debug.Assert(arc.IsLast || fst.ReadNextArcLabel(arc, @in) > targetLabel);
                        Debug.Assert(arc.Label < targetLabel, "arc.label=" + arc.Label + " vs targetLabel=" + targetLabel);
                        PushLast();
                        return;
                    }
                }
                else
                {
                    if (arc.Label == targetLabel)
                    {
                        // Match -- recurse
                        output[upto] = fst.Outputs.Add(output[upto - 1], arc.Output);
                        if (targetLabel == FST.END_LABEL)
                        {
                            return;
                        }
                        CurrentLabel = arc.Label;
                        Incr();
                        arc = fst.ReadFirstTargetArc(arc, GetArc(upto), fstReader);
                        targetLabel = TargetLabel;
                    }
                    else if (arc.Label > targetLabel)
                    {
                        // TODO: if each arc could somehow read the arc just
                        // before, we can save this re-scan.  The ceil case
                        // doesn't need this because it reads the next arc
                        // instead:
                        while (true)
                        {
                            // First, walk backwards until we find a first arc
                            // that's before our target label:
                            fst.ReadFirstTargetArc(GetArc(upto - 1), arc, fstReader);
                            if (arc.Label < targetLabel)
                            {
                                // Then, scan forwards to the arc just before
                                // the targetLabel:
                                while (!arc.IsLast && fst.ReadNextArcLabel(arc, fstReader) < targetLabel)
                                {
                                    fst.ReadNextArc(arc, fstReader);
                                }
                                PushLast();
                                return;
                            }
                            upto--;
                            if (upto == 0)
                            {
                                return;
                            }
                            targetLabel = TargetLabel;
                            arc = GetArc(upto);
                        }
                    }
                    else if (!arc.IsLast)
                    {
                        //System.out.println("  check next label=" + fst.readNextArcLabel(arc) + " (" + (char) fst.readNextArcLabel(arc) + ")");
                        if (fst.ReadNextArcLabel(arc, fstReader) > targetLabel)
                        {
                            PushLast();
                            return;
                        }
                        else
                        {
                            // keep scanning
                            fst.ReadNextArc(arc, fstReader);
                        }
                    }
                    else
                    {
                        PushLast();
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Seeks to exactly target term. </summary>
        protected virtual bool DoSeekExact()
        {
            // TODO: possibly caller could/should provide common
            // prefix length?  ie this work may be redundant if
            // caller is in fact intersecting against its own
            // automaton

            //System.out.println("FE: seek exact upto=" + upto);

            // Save time by starting at the end of the shared prefix
            // b/w our current term & the target:
            RewindPrefix();

            //System.out.println("FE: after rewind upto=" + upto);
            FST.Arc<T> arc = GetArc(upto - 1);
            int targetLabel = TargetLabel;

            FST.BytesReader fstReader = fst.GetBytesReader();

            while (true)
            {
                //System.out.println("  cycle target=" + (targetLabel == -1 ? "-1" : (char) targetLabel));
                FST.Arc<T> nextArc = fst.FindTargetArc(targetLabel, arc, GetArc(upto), fstReader);
                if (nextArc == null)
                {
                    // short circuit
                    //upto--;
                    //upto = 0;
                    fst.ReadFirstTargetArc(arc, GetArc(upto), fstReader);
                    //System.out.println("  no match upto=" + upto);
                    return false;
                }
                // Match -- recurse:
                output[upto] = fst.Outputs.Add(output[upto - 1], nextArc.Output);
                if (targetLabel == FST.END_LABEL)
                {
                    //System.out.println("  return found; upto=" + upto + " output=" + output[upto] + " nextArc=" + nextArc.isLast());
                    return true;
                }
                CurrentLabel = targetLabel;
                Incr();
                targetLabel = TargetLabel;
                arc = nextArc;
            }
        }

        private void Incr()
        {
            upto++;
            Grow();
            if (arcs.Length <= upto)
            {
                FST.Arc<T>[] newArcs = new FST.Arc<T>[ArrayUtil.Oversize(1 + upto, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                Array.Copy(arcs, 0, newArcs, 0, arcs.Length);
                arcs = newArcs;
            }
            if (output.Length <= upto)
            {
                T[] newOutput = new T[ArrayUtil.Oversize(1 + upto, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                Array.Copy(output, 0, newOutput, 0, output.Length);
                output = newOutput;
            }
        }

        // Appends current arc, and then recurses from its target,
        // appending first arc all the way to the final node
        private void PushFirst()
        {
            FST.Arc<T> arc = arcs[upto];
            Debug.Assert(arc != null);

            while (true)
            {
                output[upto] = fst.Outputs.Add(output[upto - 1], arc.Output);
                if (arc.Label == FST.END_LABEL)
                {
                    // Final node
                    break;
                }
                //System.out.println("  pushFirst label=" + (char) arc.label + " upto=" + upto + " output=" + fst.outputs.outputToString(output[upto]));
                CurrentLabel = arc.Label;
                Incr();

                FST.Arc<T> nextArc = GetArc(upto);
                fst.ReadFirstTargetArc(arc, nextArc, fstReader);
                arc = nextArc;
            }
        }

        // Recurses from current arc, appending last arc all the
        // way to the first final node
        private void PushLast()
        {
            FST.Arc<T> arc = arcs[upto];
            Debug.Assert(arc != null);

            while (true)
            {
                CurrentLabel = arc.Label;
                output[upto] = fst.Outputs.Add(output[upto - 1], arc.Output);
                if (arc.Label == FST.END_LABEL)
                {
                    // Final node
                    break;
                }
                Incr();

                arc = fst.ReadLastTargetArc(arc, GetArc(upto), fstReader);
            }
        }

        private FST.Arc<T> GetArc(int idx)
        {
            if (arcs[idx] == null)
            {
                arcs[idx] = new FST.Arc<T>();
            }
            return arcs[idx];
        }
    }
}