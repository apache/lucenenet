using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.IO;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Analysis
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
    /// Consumes a <see cref="TokenStream"/> and outputs the dot (graphviz) string (graph). </summary>
    public class TokenStreamToDot
    {
        private readonly TokenStream @in;
        private readonly ICharTermAttribute termAtt;
        private readonly IPositionIncrementAttribute posIncAtt;
        private readonly IPositionLengthAttribute posLengthAtt;
        private readonly IOffsetAttribute offsetAtt;
        private readonly string inputText;
        protected readonly TextWriter m_out;

        /// <summary>
        /// If inputText is non-null, and the <see cref="TokenStream"/> has
        /// offsets, we include the surface form in each arc's
        /// label.
        /// </summary>
        public TokenStreamToDot(string inputText, TokenStream @in, TextWriter @out)
        {
            this.@in = @in;
            this.m_out = @out;
            this.inputText = inputText;
            termAtt = @in.AddAttribute<ICharTermAttribute>();
            posIncAtt = @in.AddAttribute<IPositionIncrementAttribute>();
            posLengthAtt = @in.AddAttribute<IPositionLengthAttribute>();
            if (@in.HasAttribute<IOffsetAttribute>())
            {
                offsetAtt = @in.AddAttribute<IOffsetAttribute>();
            }
            else
            {
                offsetAtt = null;
            }
        }

        public virtual void ToDot()
        {
            @in.Reset();
            WriteHeader();

            // TODO: is there some way to tell dot that it should
            // make the "main path" a straight line and have the
            // non-sausage arcs not affect node placement...

            int pos = -1;
            int lastEndPos = -1;
            while (@in.IncrementToken())
            {
                bool isFirst = pos == -1;
                int posInc = posIncAtt.PositionIncrement;
                if (isFirst && posInc == 0)
                {
                    // TODO: hmm are TS's still allowed to do this...?
                    Console.Error.WriteLine("WARNING: first posInc was 0; correcting to 1");
                    posInc = 1;
                }

                if (posInc > 0)
                {
                    // New node:
                    pos += posInc;
                    WriteNode(pos, Convert.ToString(pos));
                }

                if (posInc > 1)
                {
                    // Gap!
                    WriteArc(lastEndPos, pos, null, "dotted");
                }

                if (isFirst)
                {
                    WriteNode(-1, null);
                    WriteArc(-1, pos, null, null);
                }

                string arcLabel = termAtt.ToString();
                if (offsetAtt != null)
                {
                    int startOffset = offsetAtt.StartOffset;
                    int endOffset = offsetAtt.EndOffset;
                    //System.out.println("start=" + startOffset + " end=" + endOffset + " len=" + inputText.length());
                    if (inputText != null)
                    {
                        arcLabel += " / " + inputText.Substring(startOffset, endOffset - startOffset);
                    }
                    else
                    {
                        arcLabel += " / " + startOffset + "-" + endOffset;
                    }
                }

                WriteArc(pos, pos + posLengthAtt.PositionLength, arcLabel, null);
                lastEndPos = pos + posLengthAtt.PositionLength;
            }

            @in.End();

            if (lastEndPos != -1)
            {
                // TODO: should we output any final text (from end
                // offsets) on this arc...?
                WriteNode(-2, null);
                WriteArc(lastEndPos, -2, null, null);
            }

            WriteTrailer();
        }

        protected virtual void WriteArc(int fromNode, int toNode, string label, string style)
        {
            m_out.Write("  " + fromNode + " -> " + toNode + " [");
            if (label != null)
            {
                m_out.Write(" label=\"" + label + "\"");
            }
            if (style != null)
            {
                m_out.Write(" style=\"" + style + "\"");
            }
            m_out.WriteLine("]");
        }

        protected virtual void WriteNode(int name, string label)
        {
            m_out.Write("  " + name);
            if (label != null)
            {
                m_out.Write(" [label=\"" + label + "\"]");
            }
            else
            {
                m_out.Write(" [shape=point color=white]");
            }
            m_out.WriteLine();
        }

        private const string FONT_NAME = "Helvetica";

        /// <summary>
        /// Override to customize. </summary>
        protected virtual void WriteHeader()
        {
            m_out.WriteLine("digraph tokens {");
            m_out.WriteLine("  graph [ fontsize=30 labelloc=\"t\" label=\"\" splines=true overlap=false rankdir = \"LR\" ];");
            m_out.WriteLine("  // A2 paper size");
            m_out.WriteLine("  size = \"34.4,16.5\";");
            //out.println("  // try to fill paper");
            //out.println("  ratio = fill;");
            m_out.WriteLine("  edge [ fontname=\"" + FONT_NAME + "\" fontcolor=\"red\" color=\"#606060\" ]");
            m_out.WriteLine("  node [ style=\"filled\" fillcolor=\"#e8e8f0\" shape=\"Mrecord\" fontname=\"" + FONT_NAME + "\" ]");
            m_out.WriteLine();
        }

        /// <summary>
        /// Override to customize. </summary>
        protected virtual void WriteTrailer()
        {
            m_out.WriteLine("}");
        }
    }
}