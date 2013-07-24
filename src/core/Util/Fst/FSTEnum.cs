using System;
using System.Diagnostics;

namespace Lucene.Net.Util.Fst
{
    public abstract class FSTEnum<T>
    {
        protected readonly FST<T> fst;

        protected FST<T>.Arc<T>[] arcs = new FST<T>.Arc<T>[10];

        protected T[] output = (T[])new Object[10];

        protected readonly T NO_OUTPUT;
        protected readonly FST.BytesReader fstReader;
        protected readonly FST<T>.Arc<T> scratchArc = new FST<T>.Arc<T>();

        protected int upto;
        protected int targetLength;

        protected FSTEnum(FST<T> fst)
        {
            this.fst = fst;
            fstReader = fst.GetBytesReader();
            NO_OUTPUT = fst.Outputs.GetNoOutput();
            fst.GetFirstArc(GetArc(0));
            output[0] = NO_OUTPUT;
        }

        protected abstract int GetTargetLabel();
        protected abstract int GetCurrentLabel();

        protected abstract void SetCurrentLabel(int label);
        protected abstract void Grow();

        protected void RewindPrefix()
        {
            if (upto == 0)
            {
                upto = 1;
                fst.ReadFirstTargetArc(GetArc(0), GetArc(1), fstReader);
                return;
            }

            var currentLimit = upto;
            upto = 1;
            while (upto < currentLimit && upto <= targetLength + 1)
            {
                var cmp = GetCurrentLabel() - GetTargetLabel();
                if (cmp < 0)
                {
                    break;
                }
                else if (cmp > 0)
                {
                    FST<T>.Arc<T> arc = GetArc(upto);
                    fst.ReadFirstTargetArc(GetArc(upto - 1), arc, fstReader);
                    break;
                }
                upto++;
            }
        }

        protected virtual void DoNext()
        {
            if (upto == 0)
            {
                upto = 1;
                fst.ReadFirstTargetArc(GetArc(0), GetArc(1), fstReader);
            }
            else
            {
                while (arcs[upto].IsLast())
                {
                    upto--;
                    if (upto == 0)
                    {
                        return;
                    }
                }
                fst.ReadNextArc(arcs[upto], fstReader);
            }
            PushFirst();
        }

        protected virtual void DoSeekCeil()
        {

            RewindPrefix();

            FST<T>.Arc<T> arc = GetArc(upto);
            var targetLabel = GetTargetLabel();

            while (true)
            {
                if (arc.BytesPerArc != 0 && arc.Label != -1)
                {
                    var input = fst.GetBytesReader();
                    var low = arc.ArcIdx;
                    var high = arc.NumArcs - 1;
                    var mid = 0;
                    var found = false;
                    while (low <= high)
                    {
                        mid = Support.Number.URShift((low + high), 1);
                        input.Position = arc.PosArcsStart;
                        input.SkipBytes(arc.BytesPerArc * mid + 1);
                        var midLabel = fst.ReadLabel(input);
                        var cmp = midLabel - targetLabel;
                        if (cmp < 0)
                            low = mid + 1;
                        else if (cmp > 0)
                            high = mid - 1;
                        else
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
                        arc.ArcIdx = mid - 1;
                        fst.ReadNextRealArc(arc, input);
                        // TODO: assert correct here?
                        Debug.Assert(arc.ArcIdx == mid);
                        Debug.Assert(arc.Label == targetLabel, "arc.label=" + arc.Label + " vs targetLabel=" + targetLabel + " mid=" + mid);
                        output[upto] = fst.Outputs.Add(output[upto - 1], arc.Output);
                        if (targetLabel == FST<T>.END_LABEL)
                        {
                            return;
                        }
                        SetCurrentLabel(arc.Label);
                        Incr();
                        arc = fst.ReadFirstTargetArc(arc, GetArc(upto), fstReader);
                        targetLabel = GetTargetLabel();
                        continue;
                    }
                    else if (low == arc.NumArcs)
                    {
                        // Dead end
                        arc.ArcIdx = arc.NumArcs - 2;
                        fst.ReadNextRealArc(arc, input);
                        // TODO: assert correct here?
                        Debug.Assert(arc.IsLast());
                        upto--;
                        while (true)
                        {
                            if (upto == 0)
                            {
                                return;
                            }
                            FST<T>.Arc<T> prevArc = GetArc(upto);
                            //System.out.println("  rollback upto=" + upto + " arc.label=" + prevArc.label + " isLast?=" + prevArc.isLast());
                            if (!prevArc.IsLast())
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
                        fst.ReadNextRealArc(arc, input);
                        // TODO: assert correct here?
                        Debug.Assert(arc.Label > targetLabel);
                        PushFirst();
                        return;
                    }
                }
                else
                {
                    if (arc.Label == targetLabel)
                    {
                        output[upto] = fst.Outputs.Add(output[upto - 1], arc.Output);
                        if (targetLabel == FST<T>.END_LABEL)
                        {
                            return;
                        }
                        SetCurrentLabel(arc.Label);
                        Incr();
                        arc = fst.ReadFirstTargetArc(arc, GetArc(upto), fstReader);
                        targetLabel = GetTargetLabel();
                    }
                    else if (arc.Label > targetLabel)
                    {
                        PushFirst();
                        return;
                    }
                    else if (arc.IsLast())
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
                            FST<T>.Arc<T> prevArc = GetArc(upto);
                            //System.out.println("  rollback upto=" + upto + " arc.label=" + prevArc.label + " isLast?=" + prevArc.isLast());
                            if (!prevArc.IsLast())
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

        protected virtual void DoSeekFloor()
        {

            RewindPrefix();

            FST<T>.Arc<T> arc = GetArc(upto);
            var targetLabel = GetTargetLabel();

            while (true)
            {
                if (arc.BytesPerArc != 0 && arc.Label != FST<T>.END_LABEL)
                {
                    FST.BytesReader input = fst.GetBytesReader();
                    var low = arc.ArcIdx;
                    int high = arc.NumArcs - 1;
                    int mid = 0;
                    bool found = false;
                    while (low <= high)
                    {
                        mid = Support.Number.URShift((low + high), 1);
                        input.Position = arc.PosArcsStart;
                        input.SkipBytes(arc.BytesPerArc * mid + 1);
                        int midLabel = fst.ReadLabel(input);
                        int cmp = midLabel - targetLabel;
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

                    if (found)
                    {
                        arc.ArcIdx = mid - 1;
                        fst.ReadNextRealArc(arc, input);
                        //Debug.Assert(arc.ArcIdx == mid);
                        //Debug.Assert(arc.Label == targetLabel, "arc.label=" + arc.Label + " vs targetLabel=" + targetLabel + " mid=" + mid);
                        output[upto] = fst.Outputs.Add(output[upto - 1], arc.Output);
                        if (targetLabel == FST<T>.END_LABEL)
                        {
                            return;
                        }
                        SetCurrentLabel(arc.Label);
                        Incr();
                        arc = fst.ReadFirstTargetArc(arc, GetArc(upto), fstReader);
                        targetLabel = GetTargetLabel();
                        continue;
                    }
                    else if (high == -1)
                    {
                        while (true)
                        {
                            fst.ReadFirstTargetArc(GetArc(upto - 1), arc, fstReader);
                            if (arc.Label < targetLabel)
                            {
                                // Then, scan forwards to the arc just before
                                // the targetLabel:
                                while (!arc.IsLast() && fst.ReadNextArcLabel(arc, input) < targetLabel)
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
                            targetLabel = GetTargetLabel();
                            arc = GetArc(upto);
                        }
                    }
                    else
                    {
                        // There is a floor arc:
                        arc.ArcIdx = (low > high ? high : low) - 1;
                        //System.out.println(" hasFloor arcIdx=" + (arc.arcIdx+1));
                        fst.ReadNextRealArc(arc, input);
                        Debug.Assert(arc.IsLast() || fst.ReadNextArcLabel(arc, input) > targetLabel);
                        Debug.Assert(arc.Label < targetLabel, "arc.label=" + arc.Label + " vs targetLabel=" + targetLabel);
                        PushLast();
                        return;
                    }
                }
                else
                {

                    if (arc.Label == targetLabel)
                    {
                        output[upto] = fst.Outputs.Add(output[upto - 1], arc.Output);
                        if (targetLabel == FST<T>.END_LABEL)
                        {
                            return;
                        }
                        SetCurrentLabel(arc.Label);
                        Incr();
                        arc = fst.ReadFirstTargetArc(arc, GetArc(upto), fstReader);
                        targetLabel = GetTargetLabel();
                    }
                    else if (arc.Label > targetLabel)
                    {
                        while (true)
                        {
                            fst.ReadFirstTargetArc(GetArc(upto - 1), arc, fstReader);
                            if (arc.Label < targetLabel)
                            {
                                while (!arc.IsLast() && fst.ReadNextArcLabel(arc, fstReader) < targetLabel)
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
                            targetLabel = GetTargetLabel();
                            arc = GetArc(upto);
                        }
                    }
                    else if (!arc.IsLast())
                    {
                        if (fst.ReadNextArcLabel(arc, fstReader) > targetLabel)
                        {
                            PushLast();
                            return;
                        }
                        else
                        {
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

        protected virtual bool DoSeekExact()
        {
            RewindPrefix();

            var arc = GetArc(upto - 1);
            var targetLabel = GetTargetLabel();

            var fstReader = fst.GetBytesReader();

            while (true)
            {
                var nextArc = fst.FindTargetArc(targetLabel, arc, GetArc(upto), fstReader);
                if (nextArc == null)
                {
                    fst.ReadFirstTargetArc(arc, GetArc(upto), fstReader);
                    return false;
                }
                output[upto] = fst.Outputs.Add(output[upto - 1], nextArc.Output);
                if (targetLabel == FST<T>.END_LABEL)
                {
                    return true;
                }
                SetCurrentLabel(targetLabel);
                Incr();
                targetLabel = GetTargetLabel();
                arc = nextArc;
            }
        }

        private void Incr()
        {
            upto++;
            Grow();
            if (arcs.Length <= upto)
            {
                var newArcs = new FST<T>.Arc<T>[ArrayUtil.Oversize(1 + upto, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                Array.Copy(arcs, 0, newArcs, 0, arcs.Length);
                arcs = newArcs;
            }
            if (output.Length <= upto)
            {
                var newOutput = new T[ArrayUtil.Oversize(1 + upto, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                Array.Copy(output, 0, newOutput, 0, output.Length);
                output = newOutput;
            }
        }

        private void PushFirst()
        {
            var arc = arcs[upto];
            // Debug.Assert(arc != null);

            while (true)
            {
                output[upto] = fst.Outputs.Add(output[upto - 1], arc.Output);
                if (arc.Label == FST<T>.END_LABEL)
                {
                    // Final node
                    break;
                }
                SetCurrentLabel(arc.Label);
                Incr();

                var nextArc = GetArc(upto);
                fst.ReadFirstTargetArc(arc, nextArc, fstReader);
                arc = nextArc;
            }
        }

        private void PushLast()
        {
            var arc = arcs[upto];
            // Debug.Assert(arc != null);

            while (true)
            {
                SetCurrentLabel(arc.Label);
                output[upto] = fst.Outputs.Add(output[upto - 1], arc.Output);
                if (arc.Label == FST<T>.END_LABEL)
                {
                    // Final node
                    break;
                }
                Incr();

                arc = fst.ReadLastTargetArc(arc, GetArc(upto), fstReader);
            }
        }

        private FST<T>.Arc<T> GetArc(int idx)
        {
            if (arcs[idx] == null)
            {
                arcs[idx] = new FST<T>.Arc<T>();
            }
            return arcs[idx];
        }
    }
}
