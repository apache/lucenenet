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

    public abstract class FSTEnum<T>
    {
        protected internal readonly FST<T> Fst;

        protected internal FST<T>.Arc<T>[] Arcs = new FST<T>.Arc<T>[10];

        // outputs are cumulative
        protected internal T[] Output = new T[10];

        protected internal readonly T NO_OUTPUT;
        protected internal readonly FST<T>.BytesReader FstReader;
        protected internal readonly FST<T>.Arc<T> ScratchArc = new FST<T>.Arc<T>();

        protected internal int Upto;
        protected internal int TargetLength;

        /// <summary>
        /// doFloor controls the behavior of advance: if it's true
        ///  doFloor is true, advance positions to the biggest
        ///  term before target.
        /// </summary>
        protected internal FSTEnum(FST<T> fst)
        {
            this.Fst = fst;
            FstReader = fst.GetBytesReader;
            NO_OUTPUT = fst.Outputs.NoOutput;
            fst.GetFirstArc(GetArc(0));
            Output[0] = NO_OUTPUT;
        }

        protected internal abstract int TargetLabel { get; }

        protected internal abstract int CurrentLabel { get; set; }

        protected internal abstract void Grow();

        /// <summary>
        /// Rewinds enum state to match the shared prefix between
        ///  current term and target term
        /// </summary>
        protected internal void RewindPrefix()
        {
            if (Upto == 0)
            {
                //System.out.println("  init");
                Upto = 1;
                Fst.ReadFirstTargetArc(GetArc(0), GetArc(1), FstReader);
                return;
            }
            //System.out.println("  rewind upto=" + upto + " vs targetLength=" + targetLength);

            int currentLimit = Upto;
            Upto = 1;
            while (Upto < currentLimit && Upto <= TargetLength + 1)
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
                    FST<T>.Arc<T> arc = GetArc(Upto);
                    Fst.ReadFirstTargetArc(GetArc(Upto - 1), arc, FstReader);
                    //System.out.println("    seek first arc");
                    break;
                }
                Upto++;
            }
            //System.out.println("  fall through upto=" + upto);
        }

        protected internal virtual void DoNext()
        {
            //System.out.println("FE: next upto=" + upto);
            if (Upto == 0)
            {
                //System.out.println("  init");
                Upto = 1;
                Fst.ReadFirstTargetArc(GetArc(0), GetArc(1), FstReader);
            }
            else
            {
                // pop
                //System.out.println("  check pop curArc target=" + arcs[upto].target + " label=" + arcs[upto].label + " isLast?=" + arcs[upto].isLast());
                while (Arcs[Upto].Last)
                {
                    Upto--;
                    if (Upto == 0)
                    {
                        //System.out.println("  eof");
                        return;
                    }
                }
                Fst.ReadNextArc(Arcs[Upto], FstReader);
            }

            PushFirst();
        }

        // TODO: should we return a status here (SEEK_FOUND / SEEK_NOT_FOUND /
        // SEEK_END)?  saves the eq check above?

        /// <summary>
        /// Seeks to smallest term that's >= target. </summary>
        protected internal virtual void DoSeekCeil()
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

            FST<T>.Arc<T> arc = GetArc(Upto);
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

                    FST<T>.BytesReader @in = Fst.GetBytesReader;
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
                        int midLabel = Fst.ReadLabel(@in);
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
                        Fst.ReadNextRealArc(arc, @in);
                        Debug.Assert(arc.ArcIdx == mid);
                        Debug.Assert(arc.Label == targetLabel, "arc.label=" + arc.Label + " vs targetLabel=" + targetLabel + " mid=" + mid);
                        Output[Upto] = Fst.Outputs.Add(Output[Upto - 1], arc.Output);
                        if (targetLabel == FST<T>.END_LABEL)
                        {
                            return;
                        }
                        CurrentLabel = arc.Label;
                        Incr();
                        arc = Fst.ReadFirstTargetArc(arc, GetArc(Upto), FstReader);
                        targetLabel = TargetLabel;
                        continue;
                    }
                    else if (low == arc.NumArcs)
                    {
                        // Dead end
                        arc.ArcIdx = arc.NumArcs - 2;
                        Fst.ReadNextRealArc(arc, @in);
                        Debug.Assert(arc.Last);
                        // Dead end (target is after the last arc);
                        // rollback to last fork then push
                        Upto--;
                        while (true)
                        {
                            if (Upto == 0)
                            {
                                return;
                            }
                            FST<T>.Arc<T> prevArc = GetArc(Upto);
                            //System.out.println("  rollback upto=" + upto + " arc.label=" + prevArc.label + " isLast?=" + prevArc.isLast());
                            if (!prevArc.Last)
                            {
                                Fst.ReadNextArc(prevArc, FstReader);
                                PushFirst();
                                return;
                            }
                            Upto--;
                        }
                    }
                    else
                    {
                        arc.ArcIdx = (low > high ? low : high) - 1;
                        Fst.ReadNextRealArc(arc, @in);
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
                        Output[Upto] = Fst.Outputs.Add(Output[Upto - 1], arc.Output);
                        if (targetLabel == FST<T>.END_LABEL)
                        {
                            return;
                        }
                        CurrentLabel = arc.Label;
                        Incr();
                        arc = Fst.ReadFirstTargetArc(arc, GetArc(Upto), FstReader);
                        targetLabel = TargetLabel;
                    }
                    else if (arc.Label > targetLabel)
                    {
                        PushFirst();
                        return;
                    }
                    else if (arc.Last)
                    {
                        // Dead end (target is after the last arc);
                        // rollback to last fork then push
                        Upto--;
                        while (true)
                        {
                            if (Upto == 0)
                            {
                                return;
                            }
                            FST<T>.Arc<T> prevArc = GetArc(Upto);
                            //System.out.println("  rollback upto=" + upto + " arc.label=" + prevArc.label + " isLast?=" + prevArc.isLast());
                            if (!prevArc.Last)
                            {
                                Fst.ReadNextArc(prevArc, FstReader);
                                PushFirst();
                                return;
                            }
                            Upto--;
                        }
                    }
                    else
                    {
                        // keep scanning
                        //System.out.println("    next scan");
                        Fst.ReadNextArc(arc, FstReader);
                    }
                }
            }
        }

        // TODO: should we return a status here (SEEK_FOUND / SEEK_NOT_FOUND /
        // SEEK_END)?  saves the eq check above?
        /// <summary>
        /// Seeks to largest term that's <= target. </summary>
        protected internal virtual void DoSeekFloor()
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

            FST<T>.Arc<T> arc = GetArc(Upto);
            int targetLabel = TargetLabel;

            //System.out.println("FE: init targetLabel=" + targetLabel);

            // Now scan forward, matching the new suffix of the target
            while (true)
            {
                //System.out.println("  cycle upto=" + upto + " arc.label=" + arc.label + " (" + (char) arc.label + ") targetLabel=" + targetLabel + " isLast?=" + arc.isLast() + " bba=" + arc.bytesPerArc);

                if (arc.BytesPerArc != 0 && arc.Label != FST<T>.END_LABEL)
                {
                    // Arcs are fixed array -- use binary search to find
                    // the target.

                    FST<T>.BytesReader @in = Fst.GetBytesReader;
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
                        int midLabel = Fst.ReadLabel(@in);
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
                        Fst.ReadNextRealArc(arc, @in);
                        Debug.Assert(arc.ArcIdx == mid);
                        Debug.Assert(arc.Label == targetLabel, "arc.label=" + arc.Label + " vs targetLabel=" + targetLabel + " mid=" + mid);
                        Output[Upto] = Fst.Outputs.Add(Output[Upto - 1], arc.Output);
                        if (targetLabel == FST<T>.END_LABEL)
                        {
                            return;
                        }
                        CurrentLabel = arc.Label;
                        Incr();
                        arc = Fst.ReadFirstTargetArc(arc, GetArc(Upto), FstReader);
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
                            Fst.ReadFirstTargetArc(GetArc(Upto - 1), arc, FstReader);
                            if (arc.Label < targetLabel)
                            {
                                // Then, scan forwards to the arc just before
                                // the targetLabel:
                                while (!arc.Last && Fst.ReadNextArcLabel(arc, @in) < targetLabel)
                                {
                                    Fst.ReadNextArc(arc, FstReader);
                                }
                                PushLast();
                                return;
                            }
                            Upto--;
                            if (Upto == 0)
                            {
                                return;
                            }
                            targetLabel = TargetLabel;
                            arc = GetArc(Upto);
                        }
                    }
                    else
                    {
                        // There is a floor arc:
                        arc.ArcIdx = (low > high ? high : low) - 1;
                        //System.out.println(" hasFloor arcIdx=" + (arc.arcIdx+1));
                        Fst.ReadNextRealArc(arc, @in);
                        Debug.Assert(arc.Last || Fst.ReadNextArcLabel(arc, @in) > targetLabel);
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
                        Output[Upto] = Fst.Outputs.Add(Output[Upto - 1], arc.Output);
                        if (targetLabel == FST<T>.END_LABEL)
                        {
                            return;
                        }
                        CurrentLabel = arc.Label;
                        Incr();
                        arc = Fst.ReadFirstTargetArc(arc, GetArc(Upto), FstReader);
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
                            Fst.ReadFirstTargetArc(GetArc(Upto - 1), arc, FstReader);
                            if (arc.Label < targetLabel)
                            {
                                // Then, scan forwards to the arc just before
                                // the targetLabel:
                                while (!arc.Last && Fst.ReadNextArcLabel(arc, FstReader) < targetLabel)
                                {
                                    Fst.ReadNextArc(arc, FstReader);
                                }
                                PushLast();
                                return;
                            }
                            Upto--;
                            if (Upto == 0)
                            {
                                return;
                            }
                            targetLabel = TargetLabel;
                            arc = GetArc(Upto);
                        }
                    }
                    else if (!arc.Last)
                    {
                        //System.out.println("  check next label=" + fst.readNextArcLabel(arc) + " (" + (char) fst.readNextArcLabel(arc) + ")");
                        if (Fst.ReadNextArcLabel(arc, FstReader) > targetLabel)
                        {
                            PushLast();
                            return;
                        }
                        else
                        {
                            // keep scanning
                            Fst.ReadNextArc(arc, FstReader);
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
        protected internal virtual bool DoSeekExact()
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
            FST<T>.Arc<T> arc = GetArc(Upto - 1);
            int targetLabel = TargetLabel;

            FST<T>.BytesReader fstReader = Fst.GetBytesReader;

            while (true)
            {
                //System.out.println("  cycle target=" + (targetLabel == -1 ? "-1" : (char) targetLabel));
                FST<T>.Arc<T> nextArc = Fst.FindTargetArc(targetLabel, arc, GetArc(Upto), fstReader);
                if (nextArc == null)
                {
                    // short circuit
                    //upto--;
                    //upto = 0;
                    Fst.ReadFirstTargetArc(arc, GetArc(Upto), fstReader);
                    //System.out.println("  no match upto=" + upto);
                    return false;
                }
                // Match -- recurse:
                Output[Upto] = Fst.Outputs.Add(Output[Upto - 1], nextArc.Output);
                if (targetLabel == FST<T>.END_LABEL)
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
            Upto++;
            Grow();
            if (Arcs.Length <= Upto)
            {
                FST<T>.Arc<T>[] newArcs = new FST<T>.Arc<T>[ArrayUtil.Oversize(1 + Upto, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                Array.Copy(Arcs, 0, newArcs, 0, Arcs.Length);
                Arcs = newArcs;
            }
            if (Output.Length <= Upto)
            {
                T[] newOutput = new T[ArrayUtil.Oversize(1 + Upto, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                Array.Copy(Output, 0, newOutput, 0, Output.Length);
                Output = newOutput;
            }
        }

        // Appends current arc, and then recurses from its target,
        // appending first arc all the way to the final node
        private void PushFirst()
        {
            FST<T>.Arc<T> arc = Arcs[Upto];
            Debug.Assert(arc != null);

            while (true)
            {
                Output[Upto] = Fst.Outputs.Add(Output[Upto - 1], arc.Output);
                if (arc.Label == FST<T>.END_LABEL)
                {
                    // Final node
                    break;
                }
                //System.out.println("  pushFirst label=" + (char) arc.label + " upto=" + upto + " output=" + fst.outputs.outputToString(output[upto]));
                CurrentLabel = arc.Label;
                Incr();

                FST<T>.Arc<T> nextArc = GetArc(Upto);
                Fst.ReadFirstTargetArc(arc, nextArc, FstReader);
                arc = nextArc;
            }
        }

        // Recurses from current arc, appending last arc all the
        // way to the first final node
        private void PushLast()
        {
            FST<T>.Arc<T> arc = Arcs[Upto];
            Debug.Assert(arc != null);

            while (true)
            {
                CurrentLabel = arc.Label;
                Output[Upto] = Fst.Outputs.Add(Output[Upto - 1], arc.Output);
                if (arc.Label == FST<T>.END_LABEL)
                {
                    // Final node
                    break;
                }
                Incr();

                arc = Fst.ReadLastTargetArc(arc, GetArc(Upto), FstReader);
            }
        }

        private FST<T>.Arc<T> GetArc(int idx)
        {
            if (Arcs[idx] == null)
            {
                Arcs[idx] = new FST<T>.Arc<T>();
            }
            return Arcs[idx];
        }
    }
}