using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Runtime.CompilerServices;

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
    /// Can Next() and Advance() through the terms in an FST
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class FSTEnum<T> // LUCENENET NOTE: changed from internal to public because has public subclasses
        where T : class // LUCENENET specific - added class constraint, since we compare reference equality
    {
        protected readonly FST<T> m_fst;

        protected FST.Arc<T>[] m_arcs = new FST.Arc<T>[10];

        // outputs are cumulative
        protected T[] m_output = new T[10];

        protected readonly T NO_OUTPUT;
        protected readonly FST.BytesReader m_fstReader;
        protected readonly FST.Arc<T> m_scratchArc = new FST.Arc<T>();

        protected int m_upto;
        protected int m_targetLength;

        /// <summary>
        /// doFloor controls the behavior of advance: if it's true
        /// doFloor is true, advance positions to the biggest
        /// term before target.
        /// </summary>
        protected FSTEnum(FST<T> fst)
        {
            this.m_fst = fst;
            m_fstReader = fst.GetBytesReader();
            NO_OUTPUT = fst.Outputs.NoOutput;
            fst.GetFirstArc(GetArc(0));
            m_output[0] = NO_OUTPUT;
        }

        protected abstract int TargetLabel { get; }

        protected abstract int CurrentLabel { get; set; }

        protected abstract void Grow();

        /// <summary>
        /// Rewinds enum state to match the shared prefix between
        /// current term and target term
        /// </summary>
        protected void RewindPrefix()
        {
            if (m_upto == 0)
            {
                //System.out.println("  init");
                m_upto = 1;
                m_fst.ReadFirstTargetArc(GetArc(0), GetArc(1), m_fstReader);
                return;
            }
            //System.out.println("  rewind upto=" + upto + " vs targetLength=" + targetLength);

            int currentLimit = m_upto;
            m_upto = 1;
            while (m_upto < currentLimit && m_upto <= m_targetLength + 1)
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
                    FST.Arc<T> arc = GetArc(m_upto);
                    m_fst.ReadFirstTargetArc(GetArc(m_upto - 1), arc, m_fstReader);
                    //System.out.println("    seek first arc");
                    break;
                }
                m_upto++;
            }
            //System.out.println("  fall through upto=" + upto);
        }

        protected virtual void DoNext()
        {
            //System.out.println("FE: next upto=" + upto);
            if (m_upto == 0)
            {
                //System.out.println("  init");
                m_upto = 1;
                m_fst.ReadFirstTargetArc(GetArc(0), GetArc(1), m_fstReader);
            }
            else
            {
                // pop
                //System.out.println("  check pop curArc target=" + arcs[upto].target + " label=" + arcs[upto].label + " isLast?=" + arcs[upto].isLast());
                while (m_arcs[m_upto].IsLast)
                {
                    m_upto--;
                    if (m_upto == 0)
                    {
                        //System.out.println("  eof");
                        return;
                    }
                }
                m_fst.ReadNextArc(m_arcs[m_upto], m_fstReader);
            }

            PushFirst();
        }

        // TODO: should we return a status here (SEEK_FOUND / SEEK_NOT_FOUND /
        // SEEK_END)?  saves the eq check above?

        /// <summary>
        /// Seeks to smallest term that's &gt;= target. </summary>
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

            FST.Arc<T> arc = GetArc(m_upto);
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

                    FST.BytesReader @in = m_fst.GetBytesReader();
                    int low = arc.ArcIdx;
                    int high = arc.NumArcs - 1;
                    int mid = 0;
                    //System.out.println("do arc array low=" + low + " high=" + high + " targetLabel=" + targetLabel);
                    bool found = false;
                    while (low <= high)
                    {
                        mid = (low + high).TripleShift(1);
                        @in.Position = arc.PosArcsStart;
                        @in.SkipBytes(arc.BytesPerArc * mid + 1);
                        int midLabel = m_fst.ReadLabel(@in);
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
                        m_fst.ReadNextRealArc(arc, @in);
                        if (Debugging.AssertsEnabled)
                        {
                            Debugging.Assert(arc.ArcIdx == mid);
                            Debugging.Assert(arc.Label == targetLabel, "arc.label={0} vs targetLabel={1} mid={2}", arc.Label, targetLabel, mid);
                        }
                        m_output[m_upto] = m_fst.Outputs.Add(m_output[m_upto - 1], arc.Output);
                        if (targetLabel == FST.END_LABEL)
                        {
                            return;
                        }
                        CurrentLabel = arc.Label;
                        Incr();
                        arc = m_fst.ReadFirstTargetArc(arc, GetArc(m_upto), m_fstReader);
                        targetLabel = TargetLabel;
                        // continue; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
                    }
                    else if (low == arc.NumArcs)
                    {
                        // Dead end
                        arc.ArcIdx = arc.NumArcs - 2;
                        m_fst.ReadNextRealArc(arc, @in);
                        if (Debugging.AssertsEnabled) Debugging.Assert(arc.IsLast);
                        // Dead end (target is after the last arc);
                        // rollback to last fork then push
                        m_upto--;
                        while (true)
                        {
                            if (m_upto == 0)
                            {
                                return;
                            }
                            FST.Arc<T> prevArc = GetArc(m_upto);
                            //System.out.println("  rollback upto=" + upto + " arc.label=" + prevArc.label + " isLast?=" + prevArc.isLast());
                            if (!prevArc.IsLast)
                            {
                                m_fst.ReadNextArc(prevArc, m_fstReader);
                                PushFirst();
                                return;
                            }
                            m_upto--;
                        }
                    }
                    else
                    {
                        arc.ArcIdx = (low > high ? low : high) - 1;
                        m_fst.ReadNextRealArc(arc, @in);
                        if (Debugging.AssertsEnabled) Debugging.Assert(arc.Label > targetLabel);
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
                        m_output[m_upto] = m_fst.Outputs.Add(m_output[m_upto - 1], arc.Output);
                        if (targetLabel == FST.END_LABEL)
                        {
                            return;
                        }
                        CurrentLabel = arc.Label;
                        Incr();
                        arc = m_fst.ReadFirstTargetArc(arc, GetArc(m_upto), m_fstReader);
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
                        m_upto--;
                        while (true)
                        {
                            if (m_upto == 0)
                            {
                                return;
                            }
                            FST.Arc<T> prevArc = GetArc(m_upto);
                            //System.out.println("  rollback upto=" + upto + " arc.label=" + prevArc.label + " isLast?=" + prevArc.isLast());
                            if (!prevArc.IsLast)
                            {
                                m_fst.ReadNextArc(prevArc, m_fstReader);
                                PushFirst();
                                return;
                            }
                            m_upto--;
                        }
                    }
                    else
                    {
                        // keep scanning
                        //System.out.println("    next scan");
                        m_fst.ReadNextArc(arc, m_fstReader);
                    }
                }
            }
        }

        // TODO: should we return a status here (SEEK_FOUND / SEEK_NOT_FOUND /
        // SEEK_END)?  saves the eq check above?
        /// <summary>
        /// Seeks to largest term that's &lt;= target. </summary>
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

            FST.Arc<T> arc = GetArc(m_upto);
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

                    FST.BytesReader @in = m_fst.GetBytesReader();
                    int low = arc.ArcIdx;
                    int high = arc.NumArcs - 1;
                    int mid = 0;
                    //System.out.println("do arc array low=" + low + " high=" + high + " targetLabel=" + targetLabel);
                    bool found = false;
                    while (low <= high)
                    {
                        mid = (low + high).TripleShift(1);
                        @in.Position = arc.PosArcsStart;
                        @in.SkipBytes(arc.BytesPerArc * mid + 1);
                        int midLabel = m_fst.ReadLabel(@in);
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
                        m_fst.ReadNextRealArc(arc, @in);
                        if (Debugging.AssertsEnabled)
                        {
                            Debugging.Assert(arc.ArcIdx == mid);
                            Debugging.Assert(arc.Label == targetLabel, "arc.label={0} vs targetLabel={1} mid={2}", arc.Label, targetLabel, mid);
                        }
                        m_output[m_upto] = m_fst.Outputs.Add(m_output[m_upto - 1], arc.Output);
                        if (targetLabel == FST.END_LABEL)
                        {
                            return;
                        }
                        CurrentLabel = arc.Label;
                        Incr();
                        arc = m_fst.ReadFirstTargetArc(arc, GetArc(m_upto), m_fstReader);
                        targetLabel = TargetLabel;
                        // continue; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
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
                            m_fst.ReadFirstTargetArc(GetArc(m_upto - 1), arc, m_fstReader);
                            if (arc.Label < targetLabel)
                            {
                                // Then, scan forwards to the arc just before
                                // the targetLabel:
                                while (!arc.IsLast && m_fst.ReadNextArcLabel(arc, @in) < targetLabel)
                                {
                                    m_fst.ReadNextArc(arc, m_fstReader);
                                }
                                PushLast();
                                return;
                            }
                            m_upto--;
                            if (m_upto == 0)
                            {
                                return;
                            }
                            targetLabel = TargetLabel;
                            arc = GetArc(m_upto);
                        }
                    }
                    else
                    {
                        // There is a floor arc:
                        arc.ArcIdx = (low > high ? high : low) - 1;
                        //System.out.println(" hasFloor arcIdx=" + (arc.arcIdx+1));
                        m_fst.ReadNextRealArc(arc, @in);

                        // LUCNENET specific: We don't want the ReadNextArcLabel call to be
                        // excluded when Debug.Assert is stripped out by the compiler.
                        bool check = arc.IsLast || m_fst.ReadNextArcLabel(arc, @in) > targetLabel;
                        if (Debugging.AssertsEnabled)
                        {
                            Debugging.Assert(check);
                            Debugging.Assert(arc.Label < targetLabel,"arc.label={0} vs targetLabel={1}", arc.Label, targetLabel);
                        }
                        PushLast();
                        return;
                    }
                }
                else
                {
                    if (arc.Label == targetLabel)
                    {
                        // Match -- recurse
                        m_output[m_upto] = m_fst.Outputs.Add(m_output[m_upto - 1], arc.Output);
                        if (targetLabel == FST.END_LABEL)
                        {
                            return;
                        }
                        CurrentLabel = arc.Label;
                        Incr();
                        arc = m_fst.ReadFirstTargetArc(arc, GetArc(m_upto), m_fstReader);
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
                            m_fst.ReadFirstTargetArc(GetArc(m_upto - 1), arc, m_fstReader);
                            if (arc.Label < targetLabel)
                            {
                                // Then, scan forwards to the arc just before
                                // the targetLabel:
                                while (!arc.IsLast && m_fst.ReadNextArcLabel(arc, m_fstReader) < targetLabel)
                                {
                                    m_fst.ReadNextArc(arc, m_fstReader);
                                }
                                PushLast();
                                return;
                            }
                            m_upto--;
                            if (m_upto == 0)
                            {
                                return;
                            }
                            targetLabel = TargetLabel;
                            arc = GetArc(m_upto);
                        }
                    }
                    else if (!arc.IsLast)
                    {
                        //System.out.println("  check next label=" + fst.readNextArcLabel(arc) + " (" + (char) fst.readNextArcLabel(arc) + ")");
                        if (m_fst.ReadNextArcLabel(arc, m_fstReader) > targetLabel)
                        {
                            PushLast();
                            return;
                        }
                        else
                        {
                            // keep scanning
                            m_fst.ReadNextArc(arc, m_fstReader);
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
            FST.Arc<T> arc = GetArc(m_upto - 1);
            int targetLabel = TargetLabel;

            FST.BytesReader fstReader = m_fst.GetBytesReader();

            while (true)
            {
                //System.out.println("  cycle target=" + (targetLabel == -1 ? "-1" : (char) targetLabel));
                FST.Arc<T> nextArc = m_fst.FindTargetArc(targetLabel, arc, GetArc(m_upto), fstReader);
                if (nextArc is null)
                {
                    // short circuit
                    //upto--;
                    //upto = 0;
                    m_fst.ReadFirstTargetArc(arc, GetArc(m_upto), fstReader);
                    //System.out.println("  no match upto=" + upto);
                    return false;
                }
                // Match -- recurse:
                m_output[m_upto] = m_fst.Outputs.Add(m_output[m_upto - 1], nextArc.Output);
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
            m_upto++;
            Grow();
            if (m_arcs.Length <= m_upto)
            {
                FST.Arc<T>[] newArcs = new FST.Arc<T>[ArrayUtil.Oversize(1 + m_upto, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                Arrays.Copy(m_arcs, 0, newArcs, 0, m_arcs.Length);
                m_arcs = newArcs;
            }
            if (m_output.Length <= m_upto)
            {
                T[] newOutput = new T[ArrayUtil.Oversize(1 + m_upto, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                Arrays.Copy(m_output, 0, newOutput, 0, m_output.Length);
                m_output = newOutput;
            }
        }

        /// <summary>
        /// Appends current arc, and then recurses from its target,
        /// appending first arc all the way to the final node
        /// </summary>
        private void PushFirst()
        {
            FST.Arc<T> arc = m_arcs[m_upto];
            if (Debugging.AssertsEnabled) Debugging.Assert(arc != null);

            while (true)
            {
                m_output[m_upto] = m_fst.Outputs.Add(m_output[m_upto - 1], arc.Output);
                if (arc.Label == FST.END_LABEL)
                {
                    // Final node
                    break;
                }
                //System.out.println("  pushFirst label=" + (char) arc.label + " upto=" + upto + " output=" + fst.outputs.outputToString(output[upto]));
                CurrentLabel = arc.Label;
                Incr();

                FST.Arc<T> nextArc = GetArc(m_upto);
                m_fst.ReadFirstTargetArc(arc, nextArc, m_fstReader);
                arc = nextArc;
            }
        }

        /// <summary>
        /// Recurses from current arc, appending last arc all the
        /// way to the first final node
        /// </summary>
        private void PushLast()
        {
            FST.Arc<T> arc = m_arcs[m_upto];
            if (Debugging.AssertsEnabled) Debugging.Assert(arc != null);

            while (true)
            {
                CurrentLabel = arc.Label;
                m_output[m_upto] = m_fst.Outputs.Add(m_output[m_upto - 1], arc.Output);
                if (arc.Label == FST.END_LABEL)
                {
                    // Final node
                    break;
                }
                Incr();

                arc = m_fst.ReadLastTargetArc(arc, GetArc(m_upto), m_fstReader);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FST.Arc<T> GetArc(int idx)
        {
            if (m_arcs[idx] is null)
            {
                m_arcs[idx] = new FST.Arc<T>();
            }
            return m_arcs[idx];
        }
    }
}