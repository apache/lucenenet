using Lucene.Net.Analysis.Ja.Dict;
using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Analysis.Ja
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

    // TODO: would be nice to show 2nd best path in a diff't
    // color...

    /// <summary>
    /// Outputs the dot (graphviz) string for the viterbi lattice.
    /// </summary>
    public class GraphvizFormatter
    {
        private const string BOS_LABEL = "BOS";

        private const string EOS_LABEL = "EOS";

        private const string FONT_NAME = "Helvetica";

        private readonly ConnectionCosts costs;

        private readonly IDictionary<string, string> bestPathMap;

        private readonly StringBuilder sb = new StringBuilder();

        public GraphvizFormatter(ConnectionCosts costs)
        {
            this.costs = costs;
            this.bestPathMap = new Dictionary<string, string>();
            sb.Append(FormatHeader());
            sb.Append("  init [style=invis]\n");
            sb.Append("  init -> 0.0 [label=\"" + BOS_LABEL + "\"]\n");
        }

        public virtual string Finish()
        {
            sb.Append(FormatTrailer());
            return sb.ToString();
        }

        // Backtraces another incremental fragment:
        internal void OnBacktrace(JapaneseTokenizer tok, WrappedPositionArray positions, int lastBackTracePos, Position endPosData, int fromIDX, char[] fragment, bool isEnd)
        {
            SetBestPathMap(positions, lastBackTracePos, endPosData, fromIDX);
            sb.Append(FormatNodes(tok, positions, lastBackTracePos, endPosData, fragment));
            if (isEnd)
            {
                sb.Append("  fini [style=invis]\n");
                sb.Append("  ");
                sb.Append(GetNodeID(endPosData.pos, fromIDX));
                sb.Append(" -> fini [label=\"" + EOS_LABEL + "\"]");
            }
        }

        // Records which arcs make up the best bath:
        private void SetBestPathMap(WrappedPositionArray positions, int startPos, Position endPosData, int fromIDX)
        {
            bestPathMap.Clear();

            int pos = endPosData.pos;
            int bestIDX = fromIDX;
            while (pos > startPos)
            {
                Position posData = positions.Get(pos);

                int backPos = posData.backPos[bestIDX];
                int backIDX = posData.backIndex[bestIDX];

                string toNodeID = GetNodeID(pos, bestIDX);
                string fromNodeID = GetNodeID(backPos, backIDX);

                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(!bestPathMap.ContainsKey(fromNodeID));
                    Debugging.Assert(!bestPathMap.Values.Contains(toNodeID));
                }
                bestPathMap[fromNodeID] = toNodeID;
                pos = backPos;
                bestIDX = backIDX;
            }
        }

        private string FormatNodes(JapaneseTokenizer tok, WrappedPositionArray positions, int startPos, Position endPosData, char[] fragment)
        {
            StringBuilder sb = new StringBuilder();
            // Output nodes
            for (int pos = startPos + 1; pos <= endPosData.pos; pos++)
            {
                Position posData = positions.Get(pos);
                for (int idx = 0; idx < posData.count; idx++)
                {
                    sb.Append("  ");
                    sb.Append(GetNodeID(pos, idx));
                    sb.Append(" [label=\"");
                    sb.Append(pos);
                    sb.Append(": ");
                    sb.Append(posData.lastRightID[idx]);
                    sb.Append("\"]\n");
                }
            }

            // Output arcs
            for (int pos = endPosData.pos; pos > startPos; pos--)
            {
                Position posData = positions.Get(pos);
                for (int idx = 0; idx < posData.count; idx++)
                {
                    Position backPosData = positions.Get(posData.backPos[idx]);
                    string toNodeID = GetNodeID(pos, idx);
                    string fromNodeID = GetNodeID(posData.backPos[idx], posData.backIndex[idx]);

                    sb.Append("  ");
                    sb.Append(fromNodeID);
                    sb.Append(" -> ");
                    sb.Append(toNodeID);

                    string attrs;
                    bestPathMap.TryGetValue(fromNodeID, out string path);
                    if (toNodeID.Equals(path, StringComparison.Ordinal))
                    {
                        // This arc is on best path
                        attrs = " color=\"#40e050\" fontcolor=\"#40a050\" penwidth=3 fontsize=20";
                    }
                    else
                    {
                        attrs = "";
                    }

                    IDictionary dict = tok.GetDict(posData.backType[idx]);
                    int wordCost = dict.GetWordCost(posData.backID[idx]);
                    int bgCost = costs.Get(backPosData.lastRightID[posData.backIndex[idx]],
                                                 dict.GetLeftId(posData.backID[idx]));

                    // LUCENENET: Removed unnecessary surfaceForm allocation and appended
                    // the chars directly to the StringBuilder below.

                    sb.Append(" [label=\"");
                    sb.Append(fragment, posData.backPos[idx] - startPos, pos - posData.backPos[idx]); 
                    sb.Append(' ');
                    sb.Append(wordCost);
                    if (bgCost >= 0)
                    {
                        sb.Append('+');
                    }
                    sb.Append(bgCost);
                    sb.Append('\"');
                    sb.Append(attrs);
                    sb.Append("]\n");
                }
            }
            return sb.ToString();
        }

        private static string FormatHeader() // LUCENENET: CA1822: Mark members as static
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("digraph viterbi {\n");
            sb.Append("  graph [ fontsize=30 labelloc=\"t\" label=\"\" splines=true overlap=false rankdir = \"LR\"];\n");
            //sb.Append("  // A2 paper size\n");
            //sb.Append("  size = \"34.4,16.5\";\n");
            //sb.Append("  // try to fill paper\n");
            //sb.Append("  ratio = fill;\n");
            sb.Append("  edge [ fontname=\"" + FONT_NAME + "\" fontcolor=\"red\" color=\"#606060\" ]\n");
            sb.Append("  node [ style=\"filled\" fillcolor=\"#e8e8f0\" shape=\"Mrecord\" fontname=\"" + FONT_NAME + "\" ]\n");

            return sb.ToString();
        }

        private static string FormatTrailer() // LUCENENET: CA1822: Mark members as static
        {
            return "}";
        }

        private static string GetNodeID(int pos, int idx) // LUCENENET: CA1822: Mark members as static
        {
            return pos + "." + idx;
        }
    }
}
