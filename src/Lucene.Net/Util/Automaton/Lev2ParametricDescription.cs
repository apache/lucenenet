using Lucene.Net.Diagnostics;

namespace Lucene.Net.Util.Automaton
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

    // The following code was generated with the moman/finenight pkg
    // this package is available under the MIT License, see NOTICE.txt
    // for more details.

    using ParametricDescription = Lucene.Net.Util.Automaton.LevenshteinAutomata.ParametricDescription;

    /// <summary>
    /// Parametric description for generating a Levenshtein automaton of degree 2 </summary>
    internal class Lev2ParametricDescription : ParametricDescription
    {
        internal override int Transition(int absState, int position, int vector)
        {
            // null absState should never be passed in
            if (Debugging.AssertsEnabled) Debugging.Assert(absState != -1);

            // decode absState -> state, offset
            int state = absState / (m_w + 1);
            int offset = absState % (m_w + 1);
            if (Debugging.AssertsEnabled) Debugging.Assert(offset >= 0);

            if (position == m_w)
            {
                if (state < 3)
                {
                    int loc = vector * 3 + state;
                    offset += Unpack(offsetIncrs0, loc, 1);
                    state = Unpack(toStates0, loc, 2) - 1;
                }
            }
            else if (position == m_w - 1)
            {
                if (state < 5)
                {
                    int loc = vector * 5 + state;
                    offset += Unpack(offsetIncrs1, loc, 1);
                    state = Unpack(toStates1, loc, 3) - 1;
                }
            }
            else if (position == m_w - 2)
            {
                if (state < 11)
                {
                    int loc = vector * 11 + state;
                    offset += Unpack(offsetIncrs2, loc, 2);
                    state = Unpack(toStates2, loc, 4) - 1;
                }
            }
            else if (position == m_w - 3)
            {
                if (state < 21)
                {
                    int loc = vector * 21 + state;
                    offset += Unpack(offsetIncrs3, loc, 2);
                    state = Unpack(toStates3, loc, 5) - 1;
                }
            }
            else if (position == m_w - 4)
            {
                if (state < 30)
                {
                    int loc = vector * 30 + state;
                    offset += Unpack(offsetIncrs4, loc, 3);
                    state = Unpack(toStates4, loc, 5) - 1;
                }
            }
            else
            {
                if (state < 30)
                {
                    int loc = vector * 30 + state;
                    offset += Unpack(offsetIncrs5, loc, 3);
                    state = Unpack(toStates5, loc, 5) - 1;
                }
            }

            if (state == -1)
            {
                // null state
                return -1;
            }
            else
            {
                // translate back to abs
                return state * (m_w + 1) + offset;
            }
        }

        // 1 vectors; 3 states per vector; array length = 3
        private static readonly long[] toStates0 = new long[] { 0x23L }; //2 bits per value

        private static readonly long[] offsetIncrs0 = new long[] { 0x0L }; //1 bits per value

        // 2 vectors; 5 states per vector; array length = 10
        private static readonly long[] toStates1 = new long[] { 0x13688b44L }; //3 bits per value

        private static readonly long[] offsetIncrs1 = new long[] { 0x3e0L }; //1 bits per value

        // 4 vectors; 11 states per vector; array length = 44
        private static readonly long[] toStates2 = new long[] {
            0x26a09a0a0520a504L, 0x2323523321a260a2L, 0x354235543213L
        }; //4 bits per value

        private static readonly long[] offsetIncrs2 = new long[] {
            0x5555520280000800L, 0x555555L
        }; //2 bits per value

        // 8 vectors; 21 states per vector; array length = 168
        private static readonly long[] toStates3 = new long[] {
            0x380e014a051404L, 0xe28245009451140L, unchecked((long)0x8a26880098a6268cL), 0x180a288ca0246213L,
            0x494053284a1080e1L, 0x510265a89c311940L, 0x4218c41188a6509cL, 0x6340c4211c4710dL,
            unchecked((long)0xa168398471882a12L), 0x104c841c683a0425L, 0x3294472904351483L, unchecked((long)0xe6290620a84a20d0L),
            0x1441a0ea2896a4a0L, 0x32L
        }; //5 bits per value

        private static readonly long[] offsetIncrs3 = new long[] {
            0x33300230c0000800L, 0x220ca080a00fc330L, 0x555555f832823380L, 0x5555555555555555L,
            0x5555555555555555L, 0x5555L
        }; //2 bits per value

        // 16 vectors; 30 states per vector; array length = 480
        private static readonly long[] toStates4 = new long[] {
            0x380e014a051404L, 0xaa015452940L, 0x55014501000000L, 0x1843ddc771085c07L,
            0x7141200040108405L, 0x52b44004c5313460L, 0x401080200063115cL, unchecked((long)0x85314c4d181c5048L),
            0x1440190a3e5c7828L, 0x28a232809100a21L, unchecked((long)0xa028ca2a84203846L), unchecked((long)0xca0240010800108aL),
            unchecked((long)0xc7b4205c1580a508L), 0x1021090251846b6L, 0x4cb513862328090L, 0x210863128ca2b8a2L,
            0x4e188ca024402940L, 0xa6b6c7c520532d4L, unchecked((long)0x8c41101451150219L), unchecked((long)0xa0c4211c4710d421L),
            0x2108421094e15063L, unchecked((long)0x8f13c43708631044L), 0x18274d908c611631L, 0x1cc238c411098263L,
            0x450e3a1d0212d0b4L, 0x31050242048108c6L, 0xfa318b42d07308eL, unchecked((long)0xa8865182356907c6L),
            0x1ca410d4520c4140L, 0x2954e13883a0ca51L, 0x3714831044229442L, unchecked((long)0x93946116b58f2c84L),
            unchecked((long)0xc41109a5631a574dL), 0x1d4512d4941cc520L, 0x52848294c643883aL, unchecked((long)0xb525073148310502L),
            unchecked((long)0xa5356939460f7358L), 0x409ca651L
        }; //5 bits per value

        private static readonly long[] offsetIncrs4 = new long[] {
            0x20c0600000010000L, 0x2000040000000001L, 0x209204a40209L, 0x301b6c0618018618L,
            0x207206186000186cL, 0x1200061b8e06dc0L, 0x480492080612010L, unchecked((long)0xa20204a040048000L),
            0x1061a0000129124L, 0x1848349b680612L, unchecked((long)0xd26da0204a041868L), 0x2492492492496128L,
            unchecked((long)0x9249249249249249L), 0x4924924924924924L, 0x2492492492492492L, unchecked((long)0x9249249249249249L),
            0x4924924924924924L, 0x2492492492492492L, unchecked((long)0x9249249249249249L), 0x4924924924924924L,
            0x2492492492492492L, unchecked((long)0x9249249249249249L), 0x24924924L
        }; //3 bits per value

        // 32 vectors; 30 states per vector; array length = 960
        private static readonly long[] toStates5 = new long[] {
            0x380e014a051404L, 0xaa015452940L, unchecked((long)0x8052814501000000L), unchecked((long)0xb80a515450000e03L),
            0x5140410842108426L, 0x71dc421701c01540L, 0x100421014610f7L, unchecked((long)0x85c0700550145010L),
            unchecked((long)0x94a271843ddc7710L), 0x1346071412108a22L, 0x3115c52b44004c53L, unchecked((long)0xc504840108020006L),
            0x54d1001314c4d181L, 0x9081204239c4a71L, 0x14c5313460714124L, 0x51006428f971e0a2L,
            0x4d181c5048402884L, 0xa3e5c782885314cL, 0x2809409482a8a239L, 0x2a84203846028a23L,
            0x10800108aa028caL, 0xe1180a288ca0240L, unchecked((long)0x98c6b80e3294a108L), 0x2942328091098c10L,
            0x11adb1ed08170560L, unchecked((long)0xa024004084240946L), 0x7b4205c1580a508cL, unchecked((long)0xa8c2968c71846b6cL),
            0x4cb5138623280910L, 0x10863128ca2b8a20L, unchecked((long)0xe188ca0244029402L), 0x4e3294e288132d44L,
            unchecked((long)0x809409ad1218c39cL), unchecked((long)0xf14814cb51386232L), 0x514454086429adb1L, 0x32d44e188ca02440L,
            unchecked((long)0x8c390a6b6c7c5205L), unchecked((long)0xd4218c41409cd2aaL), 0x5063a0c4211c4710L, 0x10442108421094e1L,
            0x31084711c4350863L, unchecked((long)0xbdef7bddf05918f2L), unchecked((long)0xc4f10dc218c41ef7L), 0x9d3642318458c63L,
            0x70863104426098c6L, 0x8c6116318f13c43L, 0x41ef75dd6b5de4d9L, unchecked((long)0xd0212d0b41cc238cL),
            0x2048108c6450e3a1L, 0x42d07308e3105024L, unchecked((long)0xdb591938f274084bL), unchecked((long)0xc238c41f77deefbbL),
            0x1f183e8c62d0b41cL, 0x502a2194608d5a4L, unchecked((long)0xa318b42d07308e31L), unchecked((long)0xed675db56907c60fL),
            unchecked((long)0xa410d4520c41f773L), 0x54e13883a0ca511cL, 0x1483104422944229L, 0x20f2329447290435L,
            0x1ef6f7ef6f7df05cL, unchecked((long)0xad63cb210dc520c4L), 0x58c695d364e51845L, unchecked((long)0xc843714831044269L),
            unchecked((long)0xe4d93946116b58f2L), 0x520c41ef717d6b17L, unchecked((long)0x83a1d4512d4941ccL), 0x50252848294c6438L,
            0x144b525073148310L, unchecked((long)0xefaf7b591c20f275L), unchecked((long)0x941cc520c41f777bL), unchecked((long)0xd5a4e5183dcd62d4L),
            0x4831050272994694L, 0x460f7358b5250731L, unchecked((long)0xf779bd6717b56939L)
        }; //5 bits per value

        private static readonly long[] offsetIncrs5 = new long[] {
            0x20c0600000010000L, 0x40000000001L, 0xb6db6d4830180L, 0x4812900824800010L,
            0x2092000040000082L, 0x618000b659254a40L, unchecked((long)0x86c301b6c0618018L), unchecked((long)0xdb01860061860001L),
            unchecked((long)0x81861800075baed6L), 0x186e381b70081cL, unchecked((long)0xe56dc02072061860L), 0x61201001200075b8L,
            0x480000480492080L, 0x52b5248201848040L, unchecked((long)0x880812810012000bL), 0x4004800004a4492L,
            0xb529124a20204aL, 0x49b68061201061a0L, unchecked((long)0x8480418680018483L), 0x1a000752ad26da01L,
            0x4a349b6808128106L, unchecked((long)0xa0204a0418680018L), 0x492492497528d26dL, 0x2492492492492492L,
            unchecked((long)0x9249249249249249L), 0x4924924924924924L, 0x2492492492492492L, unchecked((long)0x9249249249249249L),
            0x4924924924924924L, 0x2492492492492492L, unchecked((long)0x9249249249249249L), 0x4924924924924924L,
            0x2492492492492492L, unchecked((long)0x9249249249249249L), 0x4924924924924924L, 0x2492492492492492L,
            unchecked((long)0x9249249249249249L), 0x4924924924924924L, 0x2492492492492492L, unchecked((long)0x9249249249249249L),
            0x4924924924924924L, 0x2492492492492492L, unchecked((long)0x9249249249249249L), 0x4924924924924924L,
            0x2492492492492492L
        }; //3 bits per value

        // state map
        //   0 -> [(0, 0)]
        //   1 -> [(0, 2)]
        //   2 -> [(0, 1)]
        //   3 -> [(0, 1), (1, 1)]
        //   4 -> [(0, 2), (1, 2)]
        //   5 -> [(0, 2), (2, 1)]
        //   6 -> [(0, 1), (2, 2)]
        //   7 -> [(0, 2), (2, 2)]
        //   8 -> [(0, 1), (1, 1), (2, 1)]
        //   9 -> [(0, 2), (1, 2), (2, 2)]
        //   10 -> [(0, 1), (2, 1)]
        //   11 -> [(0, 2), (3, 2)]
        //   12 -> [(0, 2), (1, 2), (3, 2)]
        //   13 -> [(0, 2), (1, 2), (2, 2), (3, 2)]
        //   14 -> [(0, 1), (2, 2), (3, 2)]
        //   15 -> [(0, 2), (3, 1)]
        //   16 -> [(0, 1), (3, 2)]
        //   17 -> [(0, 1), (1, 1), (3, 2)]
        //   18 -> [(0, 2), (1, 2), (3, 1)]
        //   19 -> [(0, 2), (2, 2), (3, 2)]
        //   20 -> [(0, 2), (2, 1), (3, 1)]
        //   21 -> [(0, 2), (2, 1), (4, 2)]
        //   22 -> [(0, 2), (1, 2), (4, 2)]
        //   23 -> [(0, 2), (1, 2), (3, 2), (4, 2)]
        //   24 -> [(0, 2), (2, 2), (3, 2), (4, 2)]
        //   25 -> [(0, 2), (3, 2), (4, 2)]
        //   26 -> [(0, 2), (1, 2), (2, 2), (4, 2)]
        //   27 -> [(0, 2), (1, 2), (2, 2), (3, 2), (4, 2)]
        //   28 -> [(0, 2), (4, 2)]
        //   29 -> [(0, 2), (2, 2), (4, 2)]
        public Lev2ParametricDescription(int w)
            : base(w, 2, new int[] { 0, 2, 1, 0, 1, -1, 0, 0, -1, 0, -1, -1, -1, -1, -1, -2, -1, -1, -2, -1, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2 })
        {
        }
    }
}