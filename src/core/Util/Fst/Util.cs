using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Fst
{
    public sealed class Util
    {
        private Util()
        {
        }

        public static T Get<T>(FST<T> fst, IntsRef input)
        {
            var arc = fst.GetFirstArc(new FST<T>.Arc<T>());
            var fstReader = fst.GetBytesReader();

            var output = fst.Outputs.GetNoOutput();
            for (var i = 0; i < input; i++)
            {
                if (fst.FindTargetArc(input.ints[input.offset + i], arc, arc, fstReader) == null)
                {
                    output = fst.Outputs.Add(output, arc.Output);
                }
            }

            if (arc.IsFinal())
            {
                return fst.Outputs.Add(output, arc.NextFinalOutput);
            }
            else
            {
                return default(T);
            }
        }

        public static T Get<T>(FST<T> fst, BytesRef input)
        {
            if (fst.InputType != FST<T>.INPUT_TYPE.BYTE1) throw new InvalidOperationException("fst.InputType must be BYTE1");

            var fstReader = fst.GetBytesReader();
            var arc = fst.GetFirstArc(new FST<T>.Arc<T>());
            var output = fst.Outputs.GetNoOutput();

            for (var i = 0; i < input; i++)
            {
                if (fst.FindTargetArc(input.bytes[i + input.offset] & 0xFF, arc, arc, fstReader) == null)'
                {
                    output = fst.Outputs.Add(output, arc.Output);
                }
            }

            if (arc.IsFinal())
            {
                return fst.Outputs.Add(output, arc.NextFinalOutput);
            }
            else 
            {
                return default(T);
            }
        }

        public static IntsRef GetByOutput(FST<long> fst, long targetOutput)
        {
            var input = fst.GetBytesReader();
            var arc = fst.GetFirstArc(new FST<long>.Arc<long>());
            var scratchArc = new FST<long>.Arc<long>();
            var result = new IntsRef();

            return GetByOutput(fst, targetOutput, input, arc, scratchArc, result);
        }

        public static IntsRef GetByOutput(FST<long> fst, long targetOutput, BytesReader input, Arc<long> arc, Arc<long> scratchArc, IntsRef result)
        {
            var output = arc.Output;
            var upto = 0;

            while (true)
            {
                if (arc.IsFinal())
                {
                    var finalOutput = output + arc.NextFinalOutput;

                    if (finalOutput == targetOutput)
                    {
                        result.length = upto;
                        return result;
                    }
                    else if (finalOutput > targetOutput)
                    {
                        return null;
                    }
                }

                if (FST<long>.TargetHasArcs(arc))
                {
                    if (result.ints.length == upto)
                    {
                        result.grow(1 + upto);
                    }

                    fst.ReadFirstRealTargetArc(arc.Target, arc, input);

                    if (arc.BytesPerArc != 0)
                    {
                        var low = 0;
                        var high = arc.NumArcs - 1;
                        var mid = 0;
                        var exact = false;

                        while (low <= high)
                        {
                            mid = Support.Number.URShift(low + high, 1);
                            input.Position = arc.PosArcsStart;
                            input.SkipBytes(arc.BytesPerArc * mid);
                            var flags = input.ReadByte();
                            fst.ReadLabel(input);
                            long minArcOutput;
                            if ((flags & FST<long>.BIT_ARC_HAS_OUTPUT) != 0)
                            {
                                var arcOutput = fst.Outputs.Read(input);
                                minArcOutput = output + arcOutput;
                            }
                            else 
                            {
                                minArcOutput = output;
                            }


                            if (minArcOutput == targetOutput)
                            {
                                exact = true;
                                break;
                            }
                            else if (minArcOutput < targetOutput)
                            {
                                low = mid + 1;
                            }
                            else
                            {
                                high = mid - 1;
                            }
                        }

                        if (high == -1)
                        {
                            return null;
                        }
                        else if (exact)
                        {
                            arc.ArcIdx = mid - 1;
                        }
                        else
                        {
                            arc.ArcIdx = low - 1;
                        }

                        fst.ReadNextRealArc(arc, input);
                        result.ints[upto++] = arc.Label;
                        output += arc.Output;
                    }
                    else
                    {
                        FST<T>.Arc<T> prevArc = null;

                        while (true)
                        {
                            var minArcOutput = output + arc.Output;

                            if (minArcOutput == targetOutput)
                            {
                                output = minArcOutput;
                                result.ints[upto++] = arc.Label;
                                break;
                            }
                            else if (minArcOutput > targetOutputs)
                            {
                                if (prevArc == null)
                                {
                                    return null;
                                }
                                else
                                {
                                    arc.CopyFrom(prevArc);
                                    result.ints[upto++] = arc.Label;
                                    output += arc.output;
                                    break;
                                }
                            }
                            else if (arc.IsLast())
                            {
                                output = minArcOutput;
                                result.ints[upto++] = arc.label;
                                break;
                            }
                            else
                            {
                                prevArc = scratchArc;
                                prevArc.CopyFrom(arc);
                                fst.ReadNextRealArc(arc, input);
                            }
                        }
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        private class FSTPath<T>
        {
            public FST<T>.Arc<T> Arc { get; set; }
            public T Cost { get; set; }

            private readonly IntsRef _input;
            public IntsRef Input { get { return _input; } }

            public FSTPath(T cost, FST<T>.Arc<T> arc, IntsRef input)
            {
                this.Arc = arc;
                this.Cost = cost;
                this._input = input;
            }

            public override string ToString()
            {
 	             return "Input=" + Input + " Cost=" + Cost;
            }
        }

        private class TieBreakByInputComparator<T> : IComparer<FSTPath<T>>
        {
            private readonly IComparer<T> comparer;
            
            public TieBreakByInputComparator(IComparer<T> comparer)
            {
                this.comparer = comparer;
            }

            public override int Compare(FSTPath<T> a, FSTPath<T> b)
            {
                var cmp = comparer.Compare(a.Cost, b.Cost);
                if (cmp == 0)
                {
                    return a.Input.CompareTo(b.Input);
                }
                else
                {
                    return cmp;
                }
            }
        }

        private class TopNSearcher<T>
        {
            private readonly FST<T> fst;
            private readonly BytesReader bytesReader;
            private readonly int topN;
            private readonly int maxQueueDepth;

            private readonly FST<T>.Arc<T> scratchArc = new FST<T>.Arc<T>();

            internal IComparer<T> comparer;

            SortedSet<FSTPath<T>> queue = null;

            public TopNSearcher(FST<T> fst, int topN, int maxQueueDepth, IComparer<T> comparer)
            {
                this.fst = fst;
                this.bytesReader = fst.GetBytesReader();
                this.topN = topN;
                this.maxQueueDepth = maxQueueDepth;
                this.comparer = comparer;

                queue = new SortedSet<FSTPath<T>>(new TieBreakByInputComparator<T>(comparer));
            }

            private void AddIfCompetitive(FSTPath<T> path)
            {
                if (queue == null) throw new InvalidOperationException("queue cannot be null");

                var cost = fst.Outputs.Add(path.Cost, path.Arc.Output);

                if (queue.Count == maxQueueDepth)
                {
                    var bottom = queue.Last();
                    var comp = comparer.Compare(cost, bottom.Cost);

                    if (comp > 0)
                    {
                        return;
                    }
                    else if (comp == 0)
                    {
                        path.Input.Grow(path.Input.length + 1);
                        path.Input.ints[path.Input.length++] = path.Arc.Label;
                        var cmp = bottom.Input.CompareTo(path.Input);
                        path.Input.length--;

                        if (cmp < 0)
                        {
                            return;
                        }
                    }
                }
                else
                {
                    // Queue isn't full yet, so any path we hit competes:
                }

                var newInput = new IntsRef(path.Input.length + 1);
                Array.Copy(path.Input.ints, 0, newInput.ints, 0, path.Input.length);
                newInput.ints[path.Input.length] = path.Arc.Label;
                newInput.length = path.Input.length + 1;

                var newpath = new FSTPath<T>(cost, path.Arc, newInput);

                queue.Add(newPath);

                if (queue.Count == maxQueueDepth + 1)
                {
                    queue.Remove(queue.Last());
                }
            }

            public void AddStartPaths(FST<T>.Arc<T> node, T startOutput, bool allowEmptyString, IntsRef input)
            {
                if (startOutput.Equals(fst.Outputs.GetNoOutput())
                {
                    startOutput = fst.Outputs.GetNoOutput();
                }

                var path = new FSTPath<T>(startOutput, node, input);
                fst.ReadFirstTargetArc(node, path.arc, bytesReader);

                // Bootstrap: find the min starting arc
                while (true)
                {
                    if (allowEmptyString || path.Arc.Label != FST<T>.END_LABEL)
                    {
                        AddIfCompetitive(path);
                    }
                    if (path.Arc.IsLast())
                    {
                        break;
                    }
                    fst.ReadNextArc(path.Arc, bytesReader);
                }
            }

            public MinResult<T>[] Search()
            {
                var results = new List<MinResult<T>>();
                var fstReader = fst.Outputs.GetNoOutput();

                var rejectCount = 0;

                while (results.Count < topN)
                {
                    FSTPath<T> path;

                    if (queue == null)
                    {
                        break;
                    }

                    path = queue.First();
                    queue.Remove(path);

                    if (path == null)
                    {
                        break;
                    }

                    if (path.Arc.Label == FST<T>.END_LABEL)
                    {
                        path.Input.length--;
                        results.Add(new MinResult<T>(path.Input, path.Cost));
                        continue;
                    }

                    if (results.Count == topN - 1 && maxQueueDepth == topN)
                    {
                        queue = null;
                    }

                    while (true)
                    {
                        fst.ReadFirstTargetArc(path.Arc, path.Arc, fstReader);

                        var foundZero = false;
                        while (true)
                        {
                            if (comparer.Compare(NO_OUTPUT, path.Arc.Output) == 0)
                            {
                                if (queue == null)
                                {
                                    foundZero = true;
                                    break;
                                }
                                else if (!foundZero)
                                {
                                    scratchArc.CopyFrom(path.Arc);
                                    foundZero = true;
                                }
                                else
                                {
                                    AddIfCompetitive(path)
                                }
                            }
                            else if (queue != null) 
                            {
                                AddIfCompetitive(path);
                            }

                            if (path.Arc.IsLast())
                            {
                                break;
                            }
                            fst.ReadNextArc(path.Arc, fstReader);   
                        }

                        // Debug.Assert(foundZero);

                        if (queue != null)
                        {
                            path.Arc.CopyFrom(scratchArc);
                        }

                        if (path.Arc.Label == FST<T>.END_LABEL)
                        {
                            var finalOutput = fst.Outputs.Add(path.Cost, path.Arc.Output);
                            if (acceptResult(path.Input, finalOutput))
                            {
                                results.Add(new MinResult<T>(path.Input, finalOutput));
                            }
                            else
                            {
                                rejectCount++;
                            }
                            break;
                        }
                        else
                        {
                            path.Input.Grow(1 + path.Input.length);
                            path.Input.ints[path.Input.length] = path.Arc.Label;
                            path.Input.length++;
                            path.Cost = fst.Outputs.Add(path.Cost, path.Arc.Output);
                        }
                    }
                }
                return results.ToArray();
            }

            protected bool AcceptResult(IntsRef input, T output)
            {
                return true;
            }
        }

        public sealed class MinResult<T>
        {
            private readonly IntsRef input;
            public IntsRef Input { get { return input; } }

            private readonly T output;
            public T Output { get { return output; } }

            public MinResult(IntsRef input, T output)
            {
                this.input = input;
                this.output = output;
            }
        }

        public static MinResult<T>[] ShortestPaths<T>(FST<T> fst, FST<T>.Arc<T> fromNode, T startOutput, IComparer<T> comparer, int topN, bool allowEmptyString)
        {
            var searcher = new TopNSearcher<T>(fst, topN, topN, comparer);
            searcher.AddStartPaths(fromNode, startOutput, allowEmptyString, new IntsRef());
            return searcher.Search();
        }

        public static void ToDot<T>(FST<T> fst, Writer output, bool sameRank, bool labelStates)
        {
            const string expandedNodeColor = "blue";

            var startArc = fst.GetFirstArc(new FST<T>.Arc<T>());
            var thisLevelQueue = new List<FST<T>.Arc<T>());
            var nextLevelQueue=  new List<FST<T>.Arc<T>());
            nextLevelQueue.Add(startArc);

            var sameLevelStates = new List<int>();

            var seen = new BitSet();
            seen.Set((int) startArc.Target);

            const string stateShape = "circle";
            const string finalStateShape = "doublecircle";

            output.Write("digraph FST{\n");
            output.Write("  rankdir = LR; splines=true; concentrate=true; ordering=out; ranksep=2.5; \n");

            if (!labelStates)
            {
                output.Write("  node [shape=circle, width=.2, height=.2, style=filled]\n");
            }

            EmitDotState(output, "initial", "point", "white", "");

            var NO_OUTPUT = fst.Outputs.GetNoOutput();
            var r = fst.GetBytesReader();

            {
                string stateColor;
                if (fst.IsExpandedTarget(startArc, r))
                {
                    stateColor = expandedNodeColor;
                }
                else 
                {
                    stateColor = null;
                }

                bool isFinal;
                T finalOutput;
                if (startArc.IsFinal())
                {
                    isFinal = true;
                    finalOutput = startArc.NextFinalOutput == NO_OUTPUT ? null : startArc.NextFinalOutput;
                }
                else
                {
                    isFinal = false;
                    finalOutput = null;
                }

                EmitDotState(output, startArc.Target.ToString(), isFinal ? finalStateShape : stateShape, stateColor, finalOutput == null ? "" : fst.Outputs.OutputToString(finalOutput));
            }

            output.Write("  initial -> " + startArc.Target + "\n");

            var level = 0;

            while (nextLevelQueue.Any())
            {
                thisLevelQueue.AddRange(nextLevelQueue);
                nextLevelQueue.Clear();

                level++;
                output.Write("\n  // Transitions and states at level: " + level + "\n");
                while (thisLevelQueue.Any())
                {
                    var arc = thisLevelQueue.RemoveAt(thisLevelQueue.Count - 1);

                    if (FST<T>.TargetHasArcs(arc))
                    {
                        var node = arc.Target;

                        fst.ReadFirstRealTargetArc(arc.Target, arc, r);

                        while (true)
                        {
                            if (arc.Target >= 0 && !seen.Get((int) arc.Target))
                            {
                                string stateColor;
                                if (fst.IsExpandedTarget(arc, r))
                                {
                                    stateColor = expandedNodeColor;
                                }
                                else
                                {
                                    stateColor = null;
                                }

                                string finalOutput;
                                if (arc.NextFinalOutput != null && arc.NextFinalOutput != NO_OUTPUT)
                                {
                                    finalOutput = fst.Outputs.OutputToString(arc.NextFinalOutput);
                                }
                                else
                                {
                                    finalOutput = "";
                                }

                                EmitDotState(output, arc.Target.ToString(), stateShape, stateColor, finalOutput);
                                seen.Set((int) arc.Target);
                                nextLevelQueue.Add(new FST<T>.Arc<T>.CopyFrom(arc));
                                sameLevelStates.Add((int) arc.Target);
                            }

                            string outs;
                            if (arc.Output != NO_OUTPUT)
                            {
                                outs = "/" + fst.Outputs.OutputToString(arc.Output);
                            }
                            else
                            {
                                outs = "";
                            }

                            if (!FST<T>.TargetHasArcs(arc) && arc.IsFinal() && arc.NextFinalOutput != NO_OUTPUT)
                            {
                                outs = outs + "/[" + fst.Outputs.OutputToString(arc.NextFinalOutput) + "]";
                            }

                            string arcColor;
                            if (arc.flag(FST<T>.BIT_TARGET_NEXT))
                            {
                                arcColor = "red";
                            }
                            else
                            {
                                arcColor = "black";
                            }

                            // Debug.Assert(arc.Label != FST<T>.END_LABEL);

                            output.Write("  " + node + " -> " + arc.Target + " [label=\"" + PrintableLabel(arc.Label) + outs + "\"" + (arc.IsFinal() ? " style=\"bold\"" : "") + " color=\"" + arcColor + "\"]\n");

                            if (arc.IsLast())
                            {
                                break;
                            }
                            fst.ReadNextRealArc(arc, r);
                        }
                    }
                }

                if (sameRank && sameLevelStates.Count > 1)
                {
                    output.Write("  {rank=same; ");
                    foreach (var state in sameLevelStates)
                    {
                        output.Write(state + "; ");
                    }
                    output.Write(" }\n");
                }
                sameLevelStates.Clear();
            }

            output.Write("  -1 [style=filled, color=black, shape=doublecircle, label=\"\"]\n\n");
            output.Write("  {rank=sink; -1 }\n");

            output.Write("}\n");
            output.Flush();
        }

        private static void EmitDotState(Writer output, string name, string shape, string color, string label)
        {
            output.Write("  " + name
                + " ["
                + (shape != null ? "shape=" + shape : "") + " "
                + (color != null ? "color=" + color : "") + " "
                + (label != null ? "label=\"" + label : "\"" : "label=\"\"") + " "
                + "\n");
        }

        private static string PrintableLabel(int label)
        {
            if (label >= 0x20 && label <= 0x7d)
            {
                return ((char) label).ToString();
            }
            else
            {
                return "0x" + label.ToHexString(label);
            }
        }

        public static IntsRef ToUTF16(string s, IntsRef scratch)
        {
            var charLimit = s.length();
            scratch.offset = 0;
            scratch.length = charLimit;
            scratch.grow(charLimit);
            for (var idx = 0; idx < charLimit; idx++)
            {
                scratch.ints[idx] = (int) s.CharAt(idx);
            }
            return scratch;
        }

        public static IntsRef ToUTF32(string s, IntsRef scratch)
        {
            var charIdx = 0;
            var intIdx = 0;
            var charLimit = s.length();

            while (charIdx < charLimit)
            {
                scratch.grow(intIdx + 1);
                var utf32 = foo; // TODO: fix this char manipulation
            }
        }

        public static IntsRef ToIntsRef(BytesRef input, IntsRef scratch)
        {
            scratch.grow(input.length);
            for (var i = 0; i < input.length; i++)
            {
                scratch.ints[i] = input.bytes[i + input.offset] & 0xFF;
            }
            scratch.length = input.length;
            return scratch;
        }

        public static ToBytesRef(IntsRef input, BytesRef scratch)
        {
            scratch.grow(input.length);
            for (var i = 0; i < input.length; i++)
            {
                var value = input.ints[i + input.offset];
                // Debug.Assert(value >= byte.MinValue && value <= 255, "value " + value + " doesn't fit into byte");
                scratch.bytes[i] = (byte) value; // TODO: byte or sbyte here??
            }
            scratch.length = input.length;
            return scratch;
        }

        public static Arc<T> ReadCeilArc<T>(int label, FST<T> fst, Arc<T> follow, Arc<T> arc, BytesReader input)
        {
            if (label == FST<T>.END_LABEL) 
            {
                if (follow.IsFinal())
                {
                    if (follow.Target <= 0)
                    {
                        arc.Flags = FST<T>.BIT_LAST_ARC;
                    }
                    else
                    {
                        arc.Flags = 0;
                        arc.NextArc = follow.Target;
                        arc.Node = follow.Target;
                    }
                    arc.Output = follow.NextFinalOutput;
                    arc.Label = FST<T>.END_LABEL;
                    return arc;
                }
                else
                {
                    return null;
                }
            }

            if (!FST<T>.TargetHasArcs(follow))
            {
                return null;
            }
            fst.ReadFirstTargetArc(follow, arc, input);

            if (arc.BytesPerArc != 0 && arc.Label != FST<T>.END_LABEL)
            {
                var low = arc.ArcIdx;
                var high = arc.NumArcs - 1;
                var mid = 0;

                while (low <= high)
                {
                    mid = Support.Number.URShift(low + high, 1);
                    input.Position = arc.PosArcsStart;
                    input.SkipBytes(arc.BytesPerArc * mid + 1);
                    var midLabel = fst.ReadLabel(input);
                    var cmp = midLabel - label;

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
                        arc.ArcIdx = mid - 1;
                        return fst.ReadNextRealArc(arc, input);
                    }
                }

                if (low == arc.NumArcs)
                {
                    // DEAD END
                    return null;
                }

                arc.ArcIdx = (low > high ? high : low);
                return fst.ReadNextRealArc(arc, input);
            }

            // linear scan
            fst.ReadFirstRealTargetArc(follow.Target, arc, input);

            if (arc.Label >= label)
            {
                return arc;
            }
            else if (arc.IsLast())
            {
                return null;
            }
            else
            {
                fst.ReadNextRealArc(arc, input);
            }
        }
    }
}
