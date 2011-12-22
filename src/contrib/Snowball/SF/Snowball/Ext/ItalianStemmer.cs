/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// This file was generated automatically by the Snowball to Java compiler
using System;
using Among = SF.Snowball.Among;
using SnowballProgram = SF.Snowball.SnowballProgram;
namespace SF.Snowball.Ext
{
	
	/// <summary> Generated class implementing code defined by a snowball script.</summary>
    public class ItalianStemmer : SnowballProgram
    {

        public ItalianStemmer()
        {
            a_0 = new Among[] {
				new Among("", -1, 7, "", null),
				new Among("qu", 0, 6, "", null),
				new Among("\u00E1", 0, 1, "", null),
				new Among("\u00E9", 0, 2, "", null),
				new Among("\u00ED", 0, 3, "", null),
				new Among("\u00F3", 0, 4, "", null),
				new Among("\u00FA", 0, 5, "", null)
			};

            a_1 = new Among[] {
				new Among("", -1, 3, "", null),
				new Among("I", 0, 1, "", null),
				new Among("U", 0, 2, "", null)
			};

            a_2 = new Among[] {
				new Among("la", -1, -1, "", null),
				new Among("cela", 0, -1, "", null),
				new Among("gliela", 0, -1, "", null),
				new Among("mela", 0, -1, "", null),
				new Among("tela", 0, -1, "", null),
				new Among("vela", 0, -1, "", null),
				new Among("le", -1, -1, "", null),
				new Among("cele", 6, -1, "", null),
				new Among("gliele", 6, -1, "", null),
				new Among("mele", 6, -1, "", null),
				new Among("tele", 6, -1, "", null),
				new Among("vele", 6, -1, "", null),
				new Among("ne", -1, -1, "", null),
				new Among("cene", 12, -1, "", null),
				new Among("gliene", 12, -1, "", null),
				new Among("mene", 12, -1, "", null),
				new Among("sene", 12, -1, "", null),
				new Among("tene", 12, -1, "", null),
				new Among("vene", 12, -1, "", null),
				new Among("ci", -1, -1, "", null),
				new Among("li", -1, -1, "", null),
				new Among("celi", 20, -1, "", null),
				new Among("glieli", 20, -1, "", null),
				new Among("meli", 20, -1, "", null),
				new Among("teli", 20, -1, "", null),
				new Among("veli", 20, -1, "", null),
				new Among("gli", 20, -1, "", null),
				new Among("mi", -1, -1, "", null),
				new Among("si", -1, -1, "", null),
				new Among("ti", -1, -1, "", null),
				new Among("vi", -1, -1, "", null),
				new Among("lo", -1, -1, "", null),
				new Among("celo", 31, -1, "", null),
				new Among("glielo", 31, -1, "", null),
				new Among("melo", 31, -1, "", null),
				new Among("telo", 31, -1, "", null),
				new Among("velo", 31, -1, "", null)
			};

            a_3 = new Among[] {
				new Among("ando", -1, 1, "", null),
				new Among("endo", -1, 1, "", null),
				new Among("ar", -1, 2, "", null),
				new Among("er", -1, 2, "", null),
				new Among("ir", -1, 2, "", null)
			};

            a_4 = new Among[] {
				new Among("ic", -1, -1, "", null),
				new Among("abil", -1, -1, "", null),
				new Among("os", -1, -1, "", null),
				new Among("iv", -1, 1, "", null)
			};

            a_5 = new Among[] {
				new Among("ic", -1, 1, "", null),
				new Among("abil", -1, 1, "", null),
				new Among("iv", -1, 1, "", null)
			};

            a_6 = new Among[] {
				new Among("ica", -1, 1, "", null),
				new Among("logia", -1, 3, "", null),
				new Among("osa", -1, 1, "", null),
				new Among("ista", -1, 1, "", null),
				new Among("iva", -1, 9, "", null),
				new Among("anza", -1, 1, "", null),
				new Among("enza", -1, 5, "", null),
				new Among("ice", -1, 1, "", null),
				new Among("atrice", 7, 1, "", null),
				new Among("iche", -1, 1, "", null),
				new Among("logie", -1, 3, "", null),
				new Among("abile", -1, 1, "", null),
				new Among("ibile", -1, 1, "", null),
				new Among("usione", -1, 4, "", null),
				new Among("azione", -1, 2, "", null),
				new Among("uzione", -1, 4, "", null),
				new Among("atore", -1, 2, "", null),
				new Among("ose", -1, 1, "", null),
				new Among("ante", -1, 1, "", null),
				new Among("mente", -1, 1, "", null),
				new Among("amente", 19, 7, "", null),
				new Among("iste", -1, 1, "", null),
				new Among("ive", -1, 9, "", null),
				new Among("anze", -1, 1, "", null),
				new Among("enze", -1, 5, "", null),
				new Among("ici", -1, 1, "", null),
				new Among("atrici", 25, 1, "", null),
				new Among("ichi", -1, 1, "", null),
				new Among("abili", -1, 1, "", null),
				new Among("ibili", -1, 1, "", null),
				new Among("ismi", -1, 1, "", null),
				new Among("usioni", -1, 4, "", null),
				new Among("azioni", -1, 2, "", null),
				new Among("uzioni", -1, 4, "", null),
				new Among("atori", -1, 2, "", null),
				new Among("osi", -1, 1, "", null),
				new Among("anti", -1, 1, "", null),
				new Among("amenti", -1, 6, "", null),
				new Among("imenti", -1, 6, "", null),
				new Among("isti", -1, 1, "", null),
				new Among("ivi", -1, 9, "", null),
				new Among("ico", -1, 1, "", null),
				new Among("ismo", -1, 1, "", null),
				new Among("oso", -1, 1, "", null),
				new Among("amento", -1, 6, "", null),
				new Among("imento", -1, 6, "", null),
				new Among("ivo", -1, 9, "", null),
				new Among("it\u00E0", -1, 8, "", null),
				new Among("ist\u00E0", -1, 1, "", null),
				new Among("ist\u00E8", -1, 1, "", null),
				new Among("ist\u00EC", -1, 1, "", null)
			};

            a_7 = new Among[] {
				new Among("isca", -1, 1, "", null),
				new Among("enda", -1, 1, "", null),
				new Among("ata", -1, 1, "", null),
				new Among("ita", -1, 1, "", null),
				new Among("uta", -1, 1, "", null),
				new Among("ava", -1, 1, "", null),
				new Among("eva", -1, 1, "", null),
				new Among("iva", -1, 1, "", null),
				new Among("erebbe", -1, 1, "", null),
				new Among("irebbe", -1, 1, "", null),
				new Among("isce", -1, 1, "", null),
				new Among("ende", -1, 1, "", null),
				new Among("are", -1, 1, "", null),
				new Among("ere", -1, 1, "", null),
				new Among("ire", -1, 1, "", null),
				new Among("asse", -1, 1, "", null),
				new Among("ate", -1, 1, "", null),
				new Among("avate", 16, 1, "", null),
				new Among("evate", 16, 1, "", null),
				new Among("ivate", 16, 1, "", null),
				new Among("ete", -1, 1, "", null),
				new Among("erete", 20, 1, "", null),
				new Among("irete", 20, 1, "", null),
				new Among("ite", -1, 1, "", null),
				new Among("ereste", -1, 1, "", null),
				new Among("ireste", -1, 1, "", null),
				new Among("ute", -1, 1, "", null),
				new Among("erai", -1, 1, "", null),
				new Among("irai", -1, 1, "", null),
				new Among("isci", -1, 1, "", null),
				new Among("endi", -1, 1, "", null),
				new Among("erei", -1, 1, "", null),
				new Among("irei", -1, 1, "", null),
				new Among("assi", -1, 1, "", null),
				new Among("ati", -1, 1, "", null),
				new Among("iti", -1, 1, "", null),
				new Among("eresti", -1, 1, "", null),
				new Among("iresti", -1, 1, "", null),
				new Among("uti", -1, 1, "", null),
				new Among("avi", -1, 1, "", null),
				new Among("evi", -1, 1, "", null),
				new Among("ivi", -1, 1, "", null),
				new Among("isco", -1, 1, "", null),
				new Among("ando", -1, 1, "", null),
				new Among("endo", -1, 1, "", null),
				new Among("Yamo", -1, 1, "", null),
				new Among("iamo", -1, 1, "", null),
				new Among("avamo", -1, 1, "", null),
				new Among("evamo", -1, 1, "", null),
				new Among("ivamo", -1, 1, "", null),
				new Among("eremo", -1, 1, "", null),
				new Among("iremo", -1, 1, "", null),
				new Among("assimo", -1, 1, "", null),
				new Among("ammo", -1, 1, "", null),
				new Among("emmo", -1, 1, "", null),
				new Among("eremmo", 54, 1, "", null),
				new Among("iremmo", 54, 1, "", null),
				new Among("immo", -1, 1, "", null),
				new Among("ano", -1, 1, "", null),
				new Among("iscano", 58, 1, "", null),
				new Among("avano", 58, 1, "", null),
				new Among("evano", 58, 1, "", null),
				new Among("ivano", 58, 1, "", null),
				new Among("eranno", -1, 1, "", null),
				new Among("iranno", -1, 1, "", null),
				new Among("ono", -1, 1, "", null),
				new Among("iscono", 65, 1, "", null),
				new Among("arono", 65, 1, "", null),
				new Among("erono", 65, 1, "", null),
				new Among("irono", 65, 1, "", null),
				new Among("erebbero", -1, 1, "", null),
				new Among("irebbero", -1, 1, "", null),
				new Among("assero", -1, 1, "", null),
				new Among("essero", -1, 1, "", null),
				new Among("issero", -1, 1, "", null),
				new Among("ato", -1, 1, "", null),
				new Among("ito", -1, 1, "", null),
				new Among("uto", -1, 1, "", null),
				new Among("avo", -1, 1, "", null),
				new Among("evo", -1, 1, "", null),
				new Among("ivo", -1, 1, "", null),
				new Among("ar", -1, 1, "", null),
				new Among("ir", -1, 1, "", null),
				new Among("er\u00E0", -1, 1, "", null),
				new Among("ir\u00E0", -1, 1, "", null),
				new Among("er\u00F2", -1, 1, "", null),
				new Among("ir\u00F2", -1, 1, "", null)
			};

        }

        private Among[] a_0;
        private Among[] a_1;
        private Among[] a_2;
        private Among[] a_3;
        private Among[] a_4;
        private Among[] a_5;
        private Among[] a_6;
        private Among[] a_7;
        private static readonly char[] g_v = new char[] { (char)17, (char)65, (char)16, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)128, (char)128, (char)8, (char)2, (char)1 };

        private static readonly char[] g_AEIO = new char[] { (char)17, (char)65, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)0, (char)128, (char)128, (char)8, (char)2 };

        private static readonly char[] g_CG = new char[] { (char)17 };

        private int I_p2;
        private int I_p1;
        private int I_pV;

        private void copy_from(ItalianStemmer other)
        {
            I_p2 = other.I_p2;
            I_p1 = other.I_p1;
            I_pV = other.I_pV;
            base.copy_from(other);
        }

        private bool r_prelude()
        {
            int among_var;
            int v_1;
            int v_2;
            int v_3;
            int v_4;
            int v_5;
            // (, line 34
            // test, line 35
            v_1 = cursor;
        // repeat, line 35
        replab0:
            v_2 = cursor;
            // (, line 35
            // [, line 36
            bra = cursor;
            // substring, line 36
            among_var = find_among(a_0, 7);
            if (among_var == 0)
            {
                goto lab1;
            }
            // ], line 36
            ket = cursor;
            switch (among_var)
            {
                case 0:
                    goto lab1;
                case 1:
                    // (, line 37
                    // <-, line 37
                    slice_from("\u00E0");
                    break;
                case 2:
                    // (, line 38
                    // <-, line 38
                    slice_from("\u00E8");
                    break;
                case 3:
                    // (, line 39
                    // <-, line 39
                    slice_from("\u00EC");
                    break;
                case 4:
                    // (, line 40
                    // <-, line 40
                    slice_from("\u00F2");
                    break;
                case 5:
                    // (, line 41
                    // <-, line 41
                    slice_from("\u00F9");
                    break;
                case 6:
                    // (, line 42
                    // <-, line 42
                    slice_from("qU");
                    break;
                case 7:
                    // (, line 43
                    // next, line 43
                    if (cursor >= limit)
                    {
                        goto lab1;
                    }
                    cursor++;
                    break;
            }
            goto replab0;
        lab1:
            cursor = v_2;
            cursor = v_1;
        // repeat, line 46
        replab2:
            v_3 = cursor;
            // goto, line 46
            while (true)
            {
                v_4 = cursor;
                // (, line 46
                if (!(in_grouping(g_v, 97, 249)))
                {
                    goto lab5;
                }
                // [, line 47
                bra = cursor;
                // or, line 47
                v_5 = cursor;
                // (, line 47
                // literal, line 47
                if (!(eq_s(1, "u")))
                {
                    goto lab7;
                }
                // ], line 47
                ket = cursor;
                if (!(in_grouping(g_v, 97, 249)))
                {
                    goto lab7;
                }
                // <-, line 47
                slice_from("U");
                goto lab6;
            lab7:
                cursor = v_5;
                // (, line 48
                // literal, line 48
                if (!(eq_s(1, "i")))
                {
                    goto lab5;
                }
                // ], line 48
                ket = cursor;
                if (!(in_grouping(g_v, 97, 249)))
                {
                    goto lab5;
                }
                // <-, line 48
                slice_from("I");
            lab6:
                cursor = v_4;
                goto golab4;
            lab5:
                cursor = v_4;
                if (cursor >= limit)
                {
                    goto lab3;
                }
                cursor++;
            }
        golab4:
            goto replab2;
        lab3:
            cursor = v_3;
            return true;
        }

        private bool r_mark_regions()
        {
            int v_1;
            int v_2;
            int v_3;
            int v_6;
            int v_8;
            // (, line 52
            I_pV = limit;
            I_p1 = limit;
            I_p2 = limit;
            // do, line 58
            v_1 = cursor;
            // (, line 58
            // or, line 60
            v_2 = cursor;
            // (, line 59
            if (!(in_grouping(g_v, 97, 249)))
            {
                goto lab2;
            }
            // or, line 59
            v_3 = cursor;
            // (, line 59
            if (!(out_grouping(g_v, 97, 249)))
            {
                goto lab4;
            }
            // gopast, line 59
            while (true)
            {
                if (!(in_grouping(g_v, 97, 249)))
                {
                    goto lab6;
                }
                goto golab5;
            lab6:
                if (cursor >= limit)
                {
                    goto lab4;
                }
                cursor++;
            }
        golab5:
            goto lab3;
        lab4:
            cursor = v_3;
            // (, line 59
            if (!(in_grouping(g_v, 97, 249)))
            {
                goto lab2;
            }
            // gopast, line 59
            while (true)
            {
                if (!(out_grouping(g_v, 97, 249)))
                {
                    goto lab8;
                }
                goto golab7;
            lab8:
                if (cursor >= limit)
                {
                    goto lab2;
                }
                cursor++;
            }
        golab7:
        lab3:
            goto lab1;
        lab2:
            cursor = v_2;
            // (, line 61
            if (!(out_grouping(g_v, 97, 249)))
            {
                goto lab0;
            }
            // or, line 61
            v_6 = cursor;
            // (, line 61
            if (!(out_grouping(g_v, 97, 249)))
            {
                goto lab10;
            }
            // gopast, line 61
            while (true)
            {
                if (!(in_grouping(g_v, 97, 249)))
                {
                    goto lab12;
                }
                goto golab11;
            lab12:
                if (cursor >= limit)
                {
                    goto lab10;
                }
                cursor++;
            }
        golab11:
            goto lab9;
        lab10:
            cursor = v_6;
            // (, line 61
            if (!(in_grouping(g_v, 97, 249)))
            {
                goto lab0;
            }
            // next, line 61
            if (cursor >= limit)
            {
                goto lab0;
            }
            cursor++;
        lab9:
        lab1:
            // setmark pV, line 62
            I_pV = cursor;
        lab0:
            cursor = v_1;
            // do, line 64
            v_8 = cursor;
            // (, line 64
            // gopast, line 65
            while (true)
            {
                if (!(in_grouping(g_v, 97, 249)))
                {
                    goto lab15;
                }
                goto golab14;
            lab15:
                if (cursor >= limit)
                {
                    goto lab13;
                }
                cursor++;
            }
        golab14:
            // gopast, line 65
            while (true)
            {
                if (!(out_grouping(g_v, 97, 249)))
                {
                    goto lab17;
                }
                goto golab16;
            lab17:
                if (cursor >= limit)
                {
                    goto lab13;
                }
                cursor++;
            }
        golab16:
            // setmark p1, line 65
            I_p1 = cursor;
            // gopast, line 66
            while (true)
            {
                if (!(in_grouping(g_v, 97, 249)))
                {
                    goto lab19;
                }
                goto golab18;
            lab19:
                if (cursor >= limit)
                {
                    goto lab13;
                }
                cursor++;
            }
        golab18:
            // gopast, line 66
            while (true)
            {
                if (!(out_grouping(g_v, 97, 249)))
                {
                    goto lab21;
                }
                goto golab20;
            lab21:
                if (cursor >= limit)
                {
                    goto lab13;
                }
                cursor++;
            }
        golab20:
            // setmark p2, line 66
            I_p2 = cursor;
        lab13:
            cursor = v_8;
            return true;
        }

        private bool r_postlude()
        {
            int among_var;
            int v_1;
        // repeat, line 70
        replab0:
            v_1 = cursor;
            // (, line 70
            // [, line 72
            bra = cursor;
            // substring, line 72
            among_var = find_among(a_1, 3);
            if (among_var == 0)
            {
                goto lab1;
            }
            // ], line 72
            ket = cursor;
            switch (among_var)
            {
                case 0:
                    goto lab1;
                case 1:
                    // (, line 73
                    // <-, line 73
                    slice_from("i");
                    break;
                case 2:
                    // (, line 74
                    // <-, line 74
                    slice_from("u");
                    break;
                case 3:
                    // (, line 75
                    // next, line 75
                    if (cursor >= limit)
                    {
                        goto lab1;
                    }
                    cursor++;
                    break;
            }
            goto replab0;
        lab1:
            cursor = v_1;
            return true;
        }

        private bool r_RV()
        {
            if (!(I_pV <= cursor))
            {
                return false;
            }
            return true;
        }

        private bool r_R1()
        {
            if (!(I_p1 <= cursor))
            {
                return false;
            }
            return true;
        }

        private bool r_R2()
        {
            if (!(I_p2 <= cursor))
            {
                return false;
            }
            return true;
        }

        private bool r_attached_pronoun()
        {
            int among_var;
            // (, line 86
            // [, line 87
            ket = cursor;
            // substring, line 87
            if (find_among_b(a_2, 37) == 0)
            {
                return false;
            }
            // ], line 87
            bra = cursor;
            // among, line 97
            among_var = find_among_b(a_3, 5);
            if (among_var == 0)
            {
                return false;
            }
            // (, line 97
            // call RV, line 97
            if (!r_RV())
            {
                return false;
            }
            switch (among_var)
            {
                case 0:
                    return false;
                case 1:
                    // (, line 98
                    // delete, line 98
                    slice_del();
                    break;
                case 2:
                    // (, line 99
                    // <-, line 99
                    slice_from("e");
                    break;
            }
            return true;
        }

        private bool r_standard_suffix()
        {
            int among_var;
            int v_1;
            int v_2;
            int v_3;
            int v_4;
            // (, line 103
            // [, line 104
            ket = cursor;
            // substring, line 104
            among_var = find_among_b(a_6, 51);
            if (among_var == 0)
            {
                return false;
            }
            // ], line 104
            bra = cursor;
            switch (among_var)
            {
                case 0:
                    return false;
                case 1:
                    // (, line 111
                    // call R2, line 111
                    if (!r_R2())
                    {
                        return false;
                    }
                    // delete, line 111
                    slice_del();
                    break;
                case 2:
                    // (, line 113
                    // call R2, line 113
                    if (!r_R2())
                    {
                        return false;
                    }
                    // delete, line 113
                    slice_del();
                    // try, line 114
                    v_1 = limit - cursor;
                    // (, line 114
                    // [, line 114
                    ket = cursor;
                    // literal, line 114
                    if (!(eq_s_b(2, "ic")))
                    {
                        cursor = limit - v_1;
                        goto lab0;
                    }
                    // ], line 114
                    bra = cursor;
                    // call R2, line 114
                    if (!r_R2())
                    {
                        cursor = limit - v_1;
                        goto lab0;
                    }
                    // delete, line 114
                    slice_del();
                lab0:
                    break;
                case 3:
                    // (, line 117
                    // call R2, line 117
                    if (!r_R2())
                    {
                        return false;
                    }
                    // <-, line 117
                    slice_from("log");
                    break;
                case 4:
                    // (, line 119
                    // call R2, line 119
                    if (!r_R2())
                    {
                        return false;
                    }
                    // <-, line 119
                    slice_from("u");
                    break;
                case 5:
                    // (, line 121
                    // call R2, line 121
                    if (!r_R2())
                    {
                        return false;
                    }
                    // <-, line 121
                    slice_from("ente");
                    break;
                case 6:
                    // (, line 123
                    // call RV, line 123
                    if (!r_RV())
                    {
                        return false;
                    }
                    // delete, line 123
                    slice_del();
                    break;
                case 7:
                    // (, line 124
                    // call R1, line 125
                    if (!r_R1())
                    {
                        return false;
                    }
                    // delete, line 125
                    slice_del();
                    // try, line 126
                    v_2 = limit - cursor;
                    // (, line 126
                    // [, line 127
                    ket = cursor;
                    // substring, line 127
                    among_var = find_among_b(a_4, 4);
                    if (among_var == 0)
                    {
                        cursor = limit - v_2;
                        goto lab1;
                    }
                    // ], line 127
                    bra = cursor;
                    // call R2, line 127
                    if (!r_R2())
                    {
                        cursor = limit - v_2;
                        goto lab1;
                    }
                    // delete, line 127
                    slice_del();
                    switch (among_var)
                    {
                        case 0:
                            cursor = limit - v_2;
                            goto lab1;
                        case 1:
                            // (, line 128
                            // [, line 128
                            ket = cursor;
                            // literal, line 128
                            if (!(eq_s_b(2, "at")))
                            {
                                cursor = limit - v_2;
                                goto lab1;
                            }
                            // ], line 128
                            bra = cursor;
                            // call R2, line 128
                            if (!r_R2())
                            {
                                cursor = limit - v_2;
                                goto lab1;
                            }
                            // delete, line 128
                            slice_del();
                            break;
                    }
                lab1:
                    break;
                case 8:
                    // (, line 133
                    // call R2, line 134
                    if (!r_R2())
                    {
                        return false;
                    }
                    // delete, line 134
                    slice_del();
                    // try, line 135
                    v_3 = limit - cursor;
                    // (, line 135
                    // [, line 136
                    ket = cursor;
                    // substring, line 136
                    among_var = find_among_b(a_5, 3);
                    if (among_var == 0)
                    {
                        cursor = limit - v_3;
                        goto lab2;
                    }
                    // ], line 136
                    bra = cursor;
                    switch (among_var)
                    {
                        case 0:
                            cursor = limit - v_3;
                            goto lab2;
                        case 1:
                            // (, line 137
                            // call R2, line 137
                            if (!r_R2())
                            {
                                cursor = limit - v_3;
                                goto lab2;
                            }
                            // delete, line 137
                            slice_del();
                            break;
                    }
                lab2:
                    break;
                case 9:
                    // (, line 141
                    // call R2, line 142
                    if (!r_R2())
                    {
                        return false;
                    }
                    // delete, line 142
                    slice_del();
                    // try, line 143
                    v_4 = limit - cursor;
                    // (, line 143
                    // [, line 143
                    ket = cursor;
                    // literal, line 143
                    if (!(eq_s_b(2, "at")))
                    {
                        cursor = limit - v_4;
                        goto lab3;
                    }
                    // ], line 143
                    bra = cursor;
                    // call R2, line 143
                    if (!r_R2())
                    {
                        cursor = limit - v_4;
                        goto lab3;
                    }
                    // delete, line 143
                    slice_del();
                    // [, line 143
                    ket = cursor;
                    // literal, line 143
                    if (!(eq_s_b(2, "ic")))
                    {
                        cursor = limit - v_4;
                        goto lab3;
                    }
                    // ], line 143
                    bra = cursor;
                    // call R2, line 143
                    if (!r_R2())
                    {
                        cursor = limit - v_4;
                        goto lab3;
                    }
                    // delete, line 143
                    slice_del();
                lab3:
                    break;
            }
            return true;
        }

        private bool r_verb_suffix()
        {
            int among_var;
            int v_1;
            int v_2;
            // setlimit, line 148
            v_1 = limit - cursor;
            // tomark, line 148
            if (cursor < I_pV)
            {
                return false;
            }
            cursor = I_pV;
            v_2 = limit_backward;
            limit_backward = cursor;
            cursor = limit - v_1;
            // (, line 148
            // [, line 149
            ket = cursor;
            // substring, line 149
            among_var = find_among_b(a_7, 87);
            if (among_var == 0)
            {
                limit_backward = v_2;
                return false;
            }
            // ], line 149
            bra = cursor;
            switch (among_var)
            {
                case 0:
                    limit_backward = v_2;
                    return false;
                case 1:
                    // (, line 163
                    // delete, line 163
                    slice_del();
                    break;
            }
            limit_backward = v_2;
            return true;
        }

        private bool r_vowel_suffix()
        {
            int v_1;
            int v_2;
            // (, line 170
            // try, line 171
            v_1 = limit - cursor;
            // (, line 171
            // [, line 172
            ket = cursor;
            if (!(in_grouping_b(g_AEIO, 97, 242)))
            {
                cursor = limit - v_1;
                goto lab0;
            }
            // ], line 172
            bra = cursor;
            // call RV, line 172
            if (!r_RV())
            {
                cursor = limit - v_1;
                goto lab0;
            }
            // delete, line 172
            slice_del();
            // [, line 173
            ket = cursor;
            // literal, line 173
            if (!(eq_s_b(1, "i")))
            {
                cursor = limit - v_1;
                goto lab0;
            }
            // ], line 173
            bra = cursor;
            // call RV, line 173
            if (!r_RV())
            {
                cursor = limit - v_1;
                goto lab0;
            }
            // delete, line 173
            slice_del();
        lab0:
            // try, line 175
            v_2 = limit - cursor;
            // (, line 175
            // [, line 176
            ket = cursor;
            // literal, line 176
            if (!(eq_s_b(1, "h")))
            {
                cursor = limit - v_2;
                goto lab1;
            }
            // ], line 176
            bra = cursor;
            if (!(in_grouping_b(g_CG, 99, 103)))
            {
                cursor = limit - v_2;
                goto lab1;
            }
            // call RV, line 176
            if (!r_RV())
            {
                cursor = limit - v_2;
                goto lab1;
            }
            // delete, line 176
            slice_del();
        lab1:
            return true;
        }

        public bool Stem()
        {
            int v_1;
            int v_2;
            int v_3;
            int v_4;
            int v_5;
            int v_6;
            int v_7;
            // (, line 181
            // do, line 182
            v_1 = cursor;
            // call prelude, line 182
            if (!r_prelude())
            {
                goto lab0;
            }
        lab0:
            cursor = v_1;
            // do, line 183
            v_2 = cursor;
            // call mark_regions, line 183
            if (!r_mark_regions())
            {
                goto lab1;
            }
        lab1:
            cursor = v_2;
            // backwards, line 184
            limit_backward = cursor; cursor = limit;
            // (, line 184
            // do, line 185
            v_3 = limit - cursor;
            // call attached_pronoun, line 185
            if (!r_attached_pronoun())
            {
                goto lab2;
            }
        lab2:
            cursor = limit - v_3;
            // do, line 186
            v_4 = limit - cursor;
            // (, line 186
            // or, line 186
            v_5 = limit - cursor;
            // call standard_suffix, line 186
            if (!r_standard_suffix())
            {
                goto lab5;
            }
            goto lab4;
        lab5:
            cursor = limit - v_5;
            // call verb_suffix, line 186
            if (!r_verb_suffix())
            {
                goto lab3;
            }
        lab4:
        lab3:
            cursor = limit - v_4;
            // do, line 187
            v_6 = limit - cursor;
            // call vowel_suffix, line 187
            if (!r_vowel_suffix())
            {
                goto lab6;
            }
        lab6:
            cursor = limit - v_6;
            cursor = limit_backward;
            // do, line 189
            v_7 = cursor;
            // call postlude, line 189
            if (!r_postlude())
            {
                goto lab7;
            }
        lab7:
            cursor = v_7;
            return true;
        }
    }
}
