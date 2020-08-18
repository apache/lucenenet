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
    /// Parametric description for generating a Levenshtein automaton of degree 2,
    ///    with transpositions as primitive edits
    /// </summary>
    internal class Lev2TParametricDescription : ParametricDescription
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
                if (state < 13)
                {
                    int loc = vector * 13 + state;
                    offset += Unpack(offsetIncrs2, loc, 2);
                    state = Unpack(toStates2, loc, 4) - 1;
                }
            }
            else if (position == m_w - 3)
            {
                if (state < 28)
                {
                    int loc = vector * 28 + state;
                    offset += Unpack(offsetIncrs3, loc, 2);
                    state = Unpack(toStates3, loc, 5) - 1;
                }
            }
            else if (position == m_w - 4)
            {
                if (state < 45)
                {
                    int loc = vector * 45 + state;
                    offset += Unpack(offsetIncrs4, loc, 3);
                    state = Unpack(toStates4, loc, 6) - 1;
                }
            }
            else
            {
                if (state < 45)
                {
                    int loc = vector * 45 + state;
                    offset += Unpack(offsetIncrs5, loc, 3);
                    state = Unpack(toStates5, loc, 6) - 1;
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

        // 4 vectors; 13 states per vector; array length = 52
        private static readonly long[] toStates2 = new long[] {
            0x60dbb0b05200b504L, 0x5233217627062227L, 0x2355543214323235L, 0x4354L
        }; //4 bits per value

        private static readonly long[] offsetIncrs2 = new long[] {
            0x555080a800002000L, 0x5555555555L
        }; //2 bits per value

        // 8 vectors; 28 states per vector; array length = 224
        private static readonly long[] toStates3 = new long[] {
            unchecked((long)0xe701c02940059404L), unchecked((long)0xa010162000a50000L), unchecked((long)0xb02c8c40a1416288L), 0xa821032310858c0L,
            0x314423980d28b201L, 0x5281e528847788e0L, unchecked((long)0xa23980d308c2280eL), 0x1e3294b1a962278cL,
            unchecked((long)0x8c41309e2288e528L), 0x11444409021aca21L, 0x11a4624886b1086bL, 0x2a6258941d6240c4L,
            0x5024a50b489074adL, 0x14821aca520c411aL, 0x5888b5890b594a44L, unchecked((long)0x941d6520c411a465L),
            unchecked((long)0x8b589075ad6a62d4L), 0x1a5055a4L
        }; //5 bits per value

        private static readonly long[] offsetIncrs3 = new long[] {
            0x30c30200002000L, 0x2a0030f3c3fc333cL, 0x233a00328282a820L, 0x5555555532b283a8L,
            0x5555555555555555L, 0x5555555555555555L, 0x5555555555555555L
        }; //2 bits per value

        // 16 vectors; 45 states per vector; array length = 720
        private static readonly long[] toStates4 = new long[] {
            0x3801450002c5004L, unchecked((long)0xc500014b00000e38L), 0x51451401402L, 0x0L,
            0x518000b14010000L, 0x9f1c20828e20230L, 0x219f0df0830a70c2L, unchecked((long)0x8200008208208200L),
            0x805050160800800L, 0x3082098602602643L, 0x4564014250508064L, 0x850051420000831L,
            0x4140582085002082L, 0x456180980990c201L, 0x8316d0c50a01051L, 0x21451420050df0e0L,
            0xd14214014508214L, 0x3c21c01850821c60L, 0x1cb1403cb142087L, unchecked((long)0x800821451851822cL),
            0x20020820800020L, unchecked((long)0xd006182087180345L), unchecked((long)0xcb0a81cb24976b09L), 0x8b1a60e624709d1L,
            0x249082082249089L, unchecked((long)0xc31421c600d2c024L), 0x3c31451515454423L, 0x31853c22c21cb140L,
            0x4514500b2c208214L, 0x8718034508b0051L, unchecked((long)0xb2cb45515108f0c5L), unchecked((long)0xe824715d1cb0a810L),
            0x1422cb14908b0e60L, 0x30812c22c02cb145L, unchecked((long)0x842022020cb1420cL), 0x5c20ce0820ce0850L,
            0x208208208b0d70c2L, 0x4208508214214208L, 0x920834050830c20L, unchecked((long)0xc6134dc613653592L),
            unchecked((long)0xd309341c6dc4db4dL), 0x6424d90854d34d34L, unchecked((long)0x92072c22030814c2L), 0x4220724b24a30930L,
            0x2470d72025c920e2L, unchecked((long)0x92c92d70975c9082L), unchecked((long)0xcb0880c204924e08L), 0x45739728c24c2481L,
            unchecked((long)0xc6da4db5da6174daL), 0x4b5d35d75d30971dL, 0x1030815c93825ce2L, 0x51442051020cb145L,
            unchecked((long)0xc538210e2c220e2cL), 0x851421452cb0d70L, 0x204b085085145142L, unchecked((long)0x921560834051440cL),
            0x4d660e4da60e6595L, unchecked((long)0x94d914e41c6dc658L), unchecked((long)0x826426591454d365L), 0x2892072c51030813L,
            unchecked((long)0xe2c22072cb2ca30bL), 0x452c70d720538910L, 0x8b2cb2d708e3891L, unchecked((long)0x81cb1440c204b24eL),
            unchecked((long)0xda44e38e28c2ca24L), 0x1dc6da6585d660e4L, unchecked((long)0xe2cb5d338e5d914eL), 0x38938238L
        }; //6 bits per value

        private static readonly long[] offsetIncrs4 = new long[] {
            0x3002000000080000L, 0x20c060L, unchecked((long)0x8149000004000000L), 0x4024924110824824L,
            unchecked((long)0xdb6030c360002082L), 0x6c36c06c301b0d80L, unchecked((long)0xb01861b0000db0dbL), 0x1b7036209188e06dL,
            0x800920006d86db7L, 0x4920c2402402490L, 0x49000208249009L, 0x4908128128124804L,
            0x34800104124a44a2L, unchecked((long)0xc30930900d24020cL), 0x40009a0924c24d24L, 0x4984a069201061aL,
            0x494d049271269262L, 0x2492492492492492L, unchecked((long)0x9249249249249249L), 0x4924924924924924L,
            0x2492492492492492L, unchecked((long)0x9249249249249249L), 0x4924924924924924L, 0x2492492492492492L,
            unchecked((long)0x9249249249249249L), 0x4924924924924924L, 0x2492492492492492L, unchecked((long)0x9249249249249249L),
            0x4924924924924924L, 0x2492492492492492L, unchecked((long)0x9249249249249249L), 0x4924924924924924L,
            0x2492492492492492L, 0x249249249249L
        }; //3 bits per value

        // 32 vectors; 45 states per vector; array length = 1440
        private static readonly long[] toStates5 = new long[] {
            0x3801450002c5004L, unchecked((long)0xc500014b00000e38L), 0x51451401402L, 0x0L,
            0x514000b14010000L, 0x550000038e00e0L, 0x264518500600b180L, unchecked((long)0x8208208208208208L),
            0x2c50040820820L, 0x70820a38808c0146L, unchecked((long)0xc37c20c29c30827cL), 0x20820820800867L,
            unchecked((long)0xb140102002002080L), unchecked((long)0x828e202300518000L), 0x830a70c209f1c20L, 0x51451450853df0dfL,
            0x1614214214508214L, 0x6026026430805050L, 0x2505080643082098L, 0x4200008314564014L,
            0x850020820850051L, unchecked((long)0x80990c2014140582L), unchecked((long)0x8201920208261809L), 0x892051990060941L,
            0x22492492c22cb242L, 0x430805050162492cL, unchecked((long)0x8041451586026026L), 0x37c38020c5b43142L,
            0x4208508514508014L, 0x141405850850051L, 0x51456180980990c2L, unchecked((long)0xe008316d0c50a010L),
            0x2c52cb2c508b21f0L, 0x600d2c92c22cb249L, unchecked((long)0x873c21c01850821cL), 0x2c01cb1403cb1420L,
            0x2080082145185182L, 0x4500200208208000L, 0x870061420871803L, 0x740500f5050821cfL,
            unchecked((long)0x934d964618609000L), 0x4c24d34d30824d30L, 0x1860821c600d642L, unchecked((long)0xc2a072c925dac274L),
            0x2c69839891c27472L, unchecked((long)0x9242082089242242L), unchecked((long)0x8208718034b00900L), 0x1cb24976b09d0061L,
            0x60e624709d1cb0a8L, unchecked((long)0xd31455d71574ce3eL), 0x1c600d3825c25d74L, 0x51515454423c3142L,
            unchecked((long)0xc22c21cb1403c314L), 0xb2c20821431853L, 0x34508b005145145L, 0x5515108f0c508718L,
            0x8740500f2051454L, unchecked((long)0xe2534d920618f090L), 0x493826596592c238L, 0x4423c31421c600d6L,
            0x72c2a042cb2d1545L, 0x422c3983a091c574L, 0xb2c514508b2c52L, unchecked((long)0xf0c508718034b08bL),
            unchecked((long)0xa810b2cb45515108L), 0x2260e824715d1cb0L, unchecked((long)0xe6592c538e2d74ceL), 0x420c308138938238L,
            0x850842022020cb1L, 0x70c25c20ce0820ceL, 0x4208208208208b0dL, 0xc20420850821421L,
            0x21080880832c5083L, unchecked((long)0xa50838820838c214L), unchecked((long)0xaaaaaaaaa9c39430L), 0x1aaa7eaa9fa9faaaL,
            unchecked((long)0x824820d01420c308L), 0x7184d37184d94d64L, 0x34c24d071b7136d3L, unchecked((long)0x990936421534d34dL),
            0x834050830c20530L, 0x34dc613653592092L, unchecked((long)0xa479c6dc4db4dc61L), unchecked((long)0x920a9f924924924aL),
            0x72c220308192a82aL, 0x724b24a30930920L, 0xd72025c920e2422L, unchecked((long)0x92d70975c9082247L),
            unchecked((long)0x880c204924e0892cL), 0x2c928c24c2481cb0L, unchecked((long)0x80a5248889088749L), 0x6a861b2aaac74394L,
            0x81b2ca6ab27b278L, unchecked((long)0xa3093092072c2203L), unchecked((long)0xd76985d36915ce5cL), 0x5d74c25c771b6936L,
            0x724e0973892d74d7L, 0x4c2481cb0880c205L, 0x6174da45739728c2L, 0x4aa175c6da4db5daL,
            0x6a869b2786486186L, 0xcb14510308186caL, 0x220e2c5144205102L, unchecked((long)0xcb0d70c538210e2cL),
            0x1451420851421452L, 0x51440c204b085085L, unchecked((long)0xcb1451081440832cL), unchecked((long)0x94316208488b0888L),
            unchecked((long)0xfaaa7dfa9f7e79c3L), 0x30819ea7ea7df7dL, 0x6564855820d01451L, unchecked((long)0x9613598393698399L),
            unchecked((long)0xd965364539071b71L), 0x4e0990996451534L, 0x21560834051440c2L, unchecked((long)0xd660e4da60e65959L),
            unchecked((long)0x9207e979c6dc6584L), unchecked((long)0xa82a8207df924820L), unchecked((long)0x892072c5103081a6L), 0x2c22072cb2ca30b2L,
            0x52c70d720538910eL, unchecked((long)0x8b2cb2d708e38914L), 0x1cb1440c204b24e0L, unchecked((long)0x874b2cb28c2ca248L),
            0x4394816224488b08L, unchecked((long)0x9e786aa69b1f7e77L), 0x51030819eca6a9e7L, unchecked((long)0x8e38a30b2892072cL),
            0x6996175983936913L, 0x74ce39764538771bL, unchecked((long)0xc204e24e08e38b2dL), 0x28c2ca2481cb1440L,
            unchecked((long)0x85d660e4da44e38eL), 0x698607e975c6da65L, unchecked((long)0xa6ca6aa699e7864aL)
        }; //6 bits per value

        private static readonly long[] offsetIncrs5 = new long[] {
            0x3002000000080000L, 0x20c060L, 0x100000004000000L, unchecked((long)0xdb6db6db50603018L),
            unchecked((long)0xa480000200002db6L), 0x1249208841241240L, 0x4000010000104120L, 0x2492c42092092052L,
            0xc30d800096592d9L, unchecked((long)0xb01b0c06c36036d8L), 0x186c00036c36db0dL, unchecked((long)0xad860361b01b6c06L),
            0x360001b75b6dd6ddL, unchecked((long)0xc412311c0db6030cL), 0xdb0db6e36e06L, 0x9188e06db01861bL,
            0x6dd6db71b72b62L, 0x4024024900800920L, 0x20824900904920c2L, 0x1201248040049000L,
            0x5524ad4aa4906120L, 0x4092402002480015L, unchecked((long)0x9252251248409409L), 0x4920100124000820L,
            0x29128924204a04a0L, unchecked((long)0x900830d200055549L), 0x934930c24c24034L, 0x418690002682493L,
            unchecked((long)0x9a49861261201a48L), 0xc348001355249d4L, 0x24c40930940d2402L, 0x1a40009a0924e24dL,
            0x6204984a06920106L, unchecked((long)0x92494d5492712692L), 0x4924924924924924L, 0x2492492492492492L,
            unchecked((long)0x9249249249249249L), 0x4924924924924924L, 0x2492492492492492L, unchecked((long)0x9249249249249249L),
            0x4924924924924924L, 0x2492492492492492L, unchecked((long)0x9249249249249249L), 0x4924924924924924L,
            0x2492492492492492L, unchecked((long)0x9249249249249249L), 0x4924924924924924L, 0x2492492492492492L,
            unchecked((long)0x9249249249249249L), 0x4924924924924924L, 0x2492492492492492L, unchecked((long)0x9249249249249249L),
            0x4924924924924924L, 0x2492492492492492L, unchecked((long)0x9249249249249249L), 0x4924924924924924L,
            0x2492492492492492L, unchecked((long)0x9249249249249249L), 0x4924924924924924L, 0x2492492492492492L,
            unchecked((long)0x9249249249249249L), 0x4924924924924924L, 0x2492492492492492L, unchecked((long)0x9249249249249249L),
            0x4924924924924924L, 0x2492492492492492L, unchecked((long)0x9249249249249249L), 0x24924924L
        }; //3 bits per value

        // state map
        //   0 -> [(0, 0)]
        //   1 -> [(0, 2)]
        //   2 -> [(0, 1)]
        //   3 -> [(0, 1), (1, 1)]
        //   4 -> [(0, 2), (1, 2)]
        //   5 -> [t(0, 2), (0, 2), (1, 2), (2, 2)]
        //   6 -> [(0, 2), (2, 1)]
        //   7 -> [(0, 1), (2, 2)]
        //   8 -> [(0, 2), (2, 2)]
        //   9 -> [(0, 1), (1, 1), (2, 1)]
        //   10 -> [(0, 2), (1, 2), (2, 2)]
        //   11 -> [(0, 1), (2, 1)]
        //   12 -> [t(0, 1), (0, 1), (1, 1), (2, 1)]
        //   13 -> [(0, 2), (1, 2), (2, 2), (3, 2)]
        //   14 -> [t(0, 2), (0, 2), (1, 2), (2, 2), (3, 2)]
        //   15 -> [(0, 2), t(1, 2), (1, 2), (2, 2), (3, 2)]
        //   16 -> [(0, 2), (2, 1), (3, 1)]
        //   17 -> [(0, 1), t(1, 2), (2, 2), (3, 2)]
        //   18 -> [(0, 2), (3, 2)]
        //   19 -> [(0, 2), (1, 2), t(1, 2), (2, 2), (3, 2)]
        //   20 -> [t(0, 2), (0, 2), (1, 2), (3, 1)]
        //   21 -> [(0, 1), (1, 1), (3, 2)]
        //   22 -> [(0, 2), (2, 2), (3, 2)]
        //   23 -> [(0, 2), (1, 2), (3, 1)]
        //   24 -> [(0, 2), (1, 2), (3, 2)]
        //   25 -> [(0, 1), (2, 2), (3, 2)]
        //   26 -> [(0, 2), (3, 1)]
        //   27 -> [(0, 1), (3, 2)]
        //   28 -> [(0, 2), (2, 1), (4, 2)]
        //   29 -> [(0, 2), t(1, 2), (1, 2), (2, 2), (3, 2), (4, 2)]
        //   30 -> [(0, 2), (1, 2), (4, 2)]
        //   31 -> [(0, 2), (1, 2), (3, 2), (4, 2)]
        //   32 -> [(0, 2), (2, 2), (3, 2), (4, 2)]
        //   33 -> [(0, 2), (1, 2), t(2, 2), (2, 2), (3, 2), (4, 2)]
        //   34 -> [(0, 2), (1, 2), (2, 2), t(2, 2), (3, 2), (4, 2)]
        //   35 -> [(0, 2), (3, 2), (4, 2)]
        //   36 -> [(0, 2), t(2, 2), (2, 2), (3, 2), (4, 2)]
        //   37 -> [t(0, 2), (0, 2), (1, 2), (2, 2), (4, 2)]
        //   38 -> [(0, 2), (1, 2), (2, 2), (4, 2)]
        //   39 -> [t(0, 2), (0, 2), (1, 2), (2, 2), (3, 2), (4, 2)]
        //   40 -> [(0, 2), (1, 2), (2, 2), (3, 2), (4, 2)]
        //   41 -> [(0, 2), (4, 2)]
        //   42 -> [t(0, 2), (0, 2), (1, 2), (2, 2), t(2, 2), (3, 2), (4, 2)]
        //   43 -> [(0, 2), (2, 2), (4, 2)]
        //   44 -> [(0, 2), (1, 2), t(1, 2), (2, 2), (3, 2), (4, 2)]
        public Lev2TParametricDescription(int w)
            : base(w, 2, new int[] { 0, 2, 1, 0, 1, 0, -1, 0, 0, -1, 0, -1, -1, -1, -1, -1, -2, -1, -1, -1, -2, -1, -1, -2, -1, -1, -2, -1, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2, -2 })
        {
        }
    }
}