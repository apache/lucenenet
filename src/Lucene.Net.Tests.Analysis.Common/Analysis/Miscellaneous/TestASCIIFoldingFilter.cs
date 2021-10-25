// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Miscellaneous
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

    public class TestASCIIFoldingFilter : BaseTokenStreamTestCase
    {
        /// <summary>
        /// Pop one input token's worth of tokens off the filter and verify that they are as expected.
        /// </summary>
        internal virtual void assertNextTerms(string expectedUnfolded, string expectedFolded, ASCIIFoldingFilter filter, ICharTermAttribute termAtt)
        {
            assertTrue(filter.IncrementToken());
            assertEquals(expectedFolded, termAtt.ToString());
            if (filter.PreserveOriginal && !expectedUnfolded.Equals(expectedFolded, StringComparison.Ordinal))
            {
                assertTrue(filter.IncrementToken());
                assertEquals(expectedUnfolded, termAtt.ToString());
            }
        }

        // testLain1Accents() is a copy of TestLatin1AccentFilter.testU().
        [Test]
        public virtual void TestLatin1Accents()
        {
            TokenStream stream = new MockTokenizer(new StringReader("Des mot clés À LA CHAÎNE À Á Â Ã Ä Å Æ Ç È É Ê Ë Ì Í Î Ï Ĳ Ð Ñ" + " Ò Ó Ô Õ Ö Ø Œ Þ Ù Ú Û Ü Ý Ÿ à á â ã ä å æ ç è é ê ë ì í î ï ĳ" + " ð ñ ò ó ô õ ö ø œ ß þ ù ú û ü ý ÿ ﬁ ﬂ"), MockTokenizer.WHITESPACE, false);
            ASCIIFoldingFilter filter = new ASCIIFoldingFilter(stream, Random.nextBoolean());

            ICharTermAttribute termAtt = filter.GetAttribute<ICharTermAttribute>();
            filter.Reset();
            assertNextTerms("Des", "Des", filter, termAtt);
            assertNextTerms("mot", "mot", filter, termAtt);
            assertNextTerms("clés", "cles", filter, termAtt);
            assertNextTerms("À", "A", filter, termAtt);
            assertNextTerms("LA", "LA", filter, termAtt);
            assertNextTerms("CHAÎNE", "CHAINE", filter, termAtt);
            assertNextTerms("À", "A", filter, termAtt);
            assertNextTerms("Á", "A", filter, termAtt);
            assertNextTerms("Â", "A", filter, termAtt);
            assertNextTerms("Ã", "A", filter, termAtt);
            assertNextTerms("Ä", "A", filter, termAtt);
            assertNextTerms("Å", "A", filter, termAtt);
            assertNextTerms("Æ", "AE", filter, termAtt);
            assertNextTerms("Ç", "C", filter, termAtt);
            assertNextTerms("È", "E", filter, termAtt);
            assertNextTerms("É", "E", filter, termAtt);
            assertNextTerms("Ê", "E", filter, termAtt);
            assertNextTerms("Ë", "E", filter, termAtt);
            assertNextTerms("Ì", "I", filter, termAtt);
            assertNextTerms("Í", "I", filter, termAtt);
            assertNextTerms("Î", "I", filter, termAtt);
            assertNextTerms("Ï", "I", filter, termAtt);
            assertNextTerms("Ĳ", "IJ", filter, termAtt);
            assertNextTerms("Ð", "D", filter, termAtt);
            assertNextTerms("Ñ", "N", filter, termAtt);
            assertNextTerms("Ò", "O", filter, termAtt);
            assertNextTerms("Ó", "O", filter, termAtt);
            assertNextTerms("Ô", "O", filter, termAtt);
            assertNextTerms("Õ", "O", filter, termAtt);
            assertNextTerms("Ö", "O", filter, termAtt);
            assertNextTerms("Ø", "O", filter, termAtt);
            assertNextTerms("Œ", "OE", filter, termAtt);
            assertNextTerms("Þ", "TH", filter, termAtt);
            assertNextTerms("Ù", "U", filter, termAtt);
            assertNextTerms("Ú", "U", filter, termAtt);
            assertNextTerms("Û", "U", filter, termAtt);
            assertNextTerms("Ü", "U", filter, termAtt);
            assertNextTerms("Ý", "Y", filter, termAtt);
            assertNextTerms("Ÿ", "Y", filter, termAtt);
            assertNextTerms("à", "a", filter, termAtt);
            assertNextTerms("á", "a", filter, termAtt);
            assertNextTerms("â", "a", filter, termAtt);
            assertNextTerms("ã", "a", filter, termAtt);
            assertNextTerms("ä", "a", filter, termAtt);
            assertNextTerms("å", "a", filter, termAtt);
            assertNextTerms("æ", "ae", filter, termAtt);
            assertNextTerms("ç", "c", filter, termAtt);
            assertNextTerms("è", "e", filter, termAtt);
            assertNextTerms("é", "e", filter, termAtt);
            assertNextTerms("ê", "e", filter, termAtt);
            assertNextTerms("ë", "e", filter, termAtt);
            assertNextTerms("ì", "i", filter, termAtt);
            assertNextTerms("í", "i", filter, termAtt);
            assertNextTerms("î", "i", filter, termAtt);
            assertNextTerms("ï", "i", filter, termAtt);
            assertNextTerms("ĳ", "ij", filter, termAtt);
            assertNextTerms("ð", "d", filter, termAtt);
            assertNextTerms("ñ", "n", filter, termAtt);
            assertNextTerms("ò", "o", filter, termAtt);
            assertNextTerms("ó", "o", filter, termAtt);
            assertNextTerms("ô", "o", filter, termAtt);
            assertNextTerms("õ", "o", filter, termAtt);
            assertNextTerms("ö", "o", filter, termAtt);
            assertNextTerms("ø", "o", filter, termAtt);
            assertNextTerms("œ", "oe", filter, termAtt);
            assertNextTerms("ß", "ss", filter, termAtt);
            assertNextTerms("þ", "th", filter, termAtt);
            assertNextTerms("ù", "u", filter, termAtt);
            assertNextTerms("ú", "u", filter, termAtt);
            assertNextTerms("û", "u", filter, termAtt);
            assertNextTerms("ü", "u", filter, termAtt);
            assertNextTerms("ý", "y", filter, termAtt);
            assertNextTerms("ÿ", "y", filter, termAtt);
            assertNextTerms("ﬁ", "fi", filter, termAtt);
            assertNextTerms("ﬂ", "fl", filter, termAtt);
            assertFalse(filter.IncrementToken());
        }


        // The following Perl script generated the foldings[] array automatically
        // from ASCIIFoldingFilter.java:
        //
        //    ============== begin get.test.cases.pl ==============
        //
        //    use strict;
        //    use warnings;
        //
        //    my $file = "ASCIIFoldingFilter.java";
        //    my $output = "testcases.txt";
        //    my %codes = ();
        //    my $folded = '';
        //
        //    open IN, "<:utf8", $file || die "Error opening input file '$file': $!";
        //    open OUT, ">:utf8", $output || die "Error opening output file '$output': $!";
        //
        //    while (my $line = <IN>) {
        //      chomp($line);
        //      # case '\u0133': // <char> <maybe URL> [ description ]
        //      if ($line =~ /case\s+'\\u(....)':.*\[([^\]]+)\]/) {
        //        my $code = $1;
        //        my $desc = $2;
        //        $codes{$code} = $desc;
        //      }
        //      # output[outputPos++] = 'A';
        //      elsif ($line =~ /output\[outputPos\+\+\] = '(.+)';/) {
        //        my $output_char = $1;
        //        $folded .= $output_char;
        //      }
        //      elsif ($line =~ /break;/ && length($folded) > 0) {
        //        my $first = 1;
        //        for my $code (sort { hex($a) <=> hex($b) } keys %codes) {
        //          my $desc = $codes{$code};
        //          print OUT '      ';
        //          print OUT '+ ' if (not $first);
        //          $first = 0;
        //          print OUT '"', chr(hex($code)), qq!"  // U+$code: $desc\n!;
        //        }
        //        print OUT qq!      ,"$folded", // Folded result\n\n!;
        //        %codes = ();
        //        $folded = '';
        //      }
        //    }
        //    close OUT;
        //
        //    ============== end get.test.cases.pl ==============
        //
        [Test]
        public virtual void TestAllFoldings()
        {
            // Alternating strings of:
            //   1. All non-ASCII characters to be folded, concatenated together as a
            //      single string.
            //   2. The string of ASCII characters to which each of the above
            //      characters should be folded.
            string[] foldings = new string[] { "À" + "Á" + "Â" + "Ã" + "Ä" + "Å" + "Ā" + "Ă" + "Ą" + "Ə" + "Ǎ" + "Ǟ" + "Ǡ" + "Ǻ" + "Ȁ" + "Ȃ" + "Ȧ" + "Ⱥ" + "ᴀ" + "Ḁ" + "Ạ" + "Ả" + "Ấ" + "Ầ" + "Ẩ" + "Ẫ" + "Ậ" + "Ắ" + "Ằ" + "Ẳ" + "Ẵ" + "Ặ" + "Ⓐ" + "Ａ", "A", "à" + "á" + "â" + "ã" + "ä" + "å" + "ā" + "ă" + "ą" + "ǎ" + "ǟ" + "ǡ" + "ǻ" + "ȁ" + "ȃ" + "ȧ" + "ɐ" + "ə" + "ɚ" + "ᶏ" + "ḁ" + "ᶕ" + "ẚ" + "ạ" + "ả" + "ấ" + "ầ" + "ẩ" + "ẫ" + "ậ" + "ắ" + "ằ" + "ẳ" + "ẵ" + "ặ" + "ₐ" + "ₔ" + "ⓐ" + "ⱥ" + "Ɐ" + "ａ", "a", "Ꜳ", "AA", "Æ" + "Ǣ" + "Ǽ" + "ᴁ", "AE", "Ꜵ", "AO", "Ꜷ", "AU", "Ꜹ" + "Ꜻ", "AV", "Ꜽ", "AY", "⒜", "(a)", "ꜳ", "aa", "æ" + "ǣ" + "ǽ" + "ᴂ", "ae", "ꜵ", "ao", "ꜷ", "au", "ꜹ" + "ꜻ", "av", "ꜽ", "ay", "Ɓ" + "Ƃ" + "Ƀ" + "ʙ" + "ᴃ" + "Ḃ" + "Ḅ" + "Ḇ" + "Ⓑ" + "Ｂ", "B", "ƀ" + "ƃ" + "ɓ" + "ᵬ" + "ᶀ" + "ḃ" + "ḅ" + "ḇ" + "ⓑ" + "ｂ", "b", "⒝", "(b)", "Ç" + "Ć" + "Ĉ" + "Ċ" + "Č" + "Ƈ" + "Ȼ" + "ʗ" + "ᴄ" + "Ḉ" + "Ⓒ" + "Ｃ", "C", "ç" + "ć" + "ĉ" + "ċ" + "č" + "ƈ" + "ȼ" + "ɕ" + "ḉ" + "ↄ" + "ⓒ" + "Ꜿ" + "ꜿ" + "ｃ", "c", "⒞", "(c)", "Ð" + "Ď" + "Đ" + "Ɖ" + "Ɗ" + "Ƌ" + "ᴅ" + "ᴆ" + "Ḋ" + "Ḍ" + "Ḏ" + "Ḑ" + "Ḓ" + "Ⓓ" + "Ꝺ" + "Ｄ", "D", "ð" + "ď" + "đ" + "ƌ" + "ȡ" + "ɖ" + "ɗ" + "ᵭ" + "ᶁ" + "ᶑ" + "ḋ" + "ḍ" + "ḏ" + "ḑ" + "ḓ" + "ⓓ" + "ꝺ" + "ｄ", "d", "Ǆ" + "Ǳ", "DZ", "ǅ" + "ǲ", "Dz", "⒟", "(d)", "ȸ", "db", "ǆ" + "ǳ" + "ʣ" + "ʥ", "dz", "È" + "É" + "Ê" + "Ë" + "Ē" + "Ĕ" + "Ė" + "Ę" + "Ě" + "Ǝ" + "Ɛ" + "Ȅ" + "Ȇ" + "Ȩ" + "Ɇ" + "ᴇ" + "Ḕ" + "Ḗ" + "Ḙ" + "Ḛ" + "Ḝ" + "Ẹ" + "Ẻ" + "Ẽ" + "Ế" + "Ề" + "Ể" + "Ễ" + "Ệ" + "Ⓔ" + "ⱻ" + "Ｅ", "E", "è" + "é" + "ê" + "ë" + "ē" + "ĕ" + "ė" + "ę" + "ě" + "ǝ" + "ȅ" + "ȇ" + "ȩ" + "ɇ" + "ɘ" + "ɛ" + "ɜ" + "ɝ" + "ɞ" + "ʚ" + "ᴈ" + "ᶒ" + "ᶓ" + "ᶔ" + "ḕ" + "ḗ" + "ḙ" + "ḛ" + "ḝ" + "ẹ" + "ẻ" + "ẽ" + "ế" + "ề" + "ể" + "ễ" + "ệ" + "ₑ" + "ⓔ" + "ⱸ" + "ｅ", "e", "⒠", "(e)", "Ƒ" + "Ḟ" + "Ⓕ" + "ꜰ" + "Ꝼ" + "ꟻ" + "Ｆ", "F", "ƒ" + "ᵮ" + "ᶂ" + "ḟ" + "ẛ" + "ⓕ" + "ꝼ" + "ｆ", "f", "⒡", "(f)", "ﬀ", "ff", "ﬃ", "ffi", "ﬄ", "ffl", "ﬁ", "fi", "ﬂ", "fl", "Ĝ" + "Ğ" + "Ġ" + "Ģ" + "Ɠ" + "Ǥ" + "ǥ" + "Ǧ" + "ǧ" + "Ǵ" + "ɢ" + "ʛ" + "Ḡ" + "Ⓖ" + "Ᵹ" + "Ꝿ" + "Ｇ", "G", "ĝ" + "ğ" + "ġ" + "ģ" + "ǵ" + "ɠ" + "ɡ" + "ᵷ" + "ᵹ" + "ᶃ" + "ḡ" + "ⓖ" + "ꝿ" + "ｇ", "g", "⒢", "(g)", "Ĥ" + "Ħ" + "Ȟ" + "ʜ" + "Ḣ" + "Ḥ" + "Ḧ" + "Ḩ" + "Ḫ" + "Ⓗ" + "Ⱨ" + "Ⱶ" + "Ｈ", "H", "ĥ" + "ħ" + "ȟ" + "ɥ" + "ɦ" + "ʮ" + "ʯ" + "ḣ" + "ḥ" + "ḧ" + "ḩ" + "ḫ" + "ẖ" + "ⓗ" + "ⱨ" + "ⱶ" + "ｈ", "h", "Ƕ", "HV", "⒣", "(h)", "ƕ", "hv", "Ì" + "Í" + "Î" + "Ï" + "Ĩ" + "Ī" + "Ĭ" + "Į" + "İ" + "Ɩ" + "Ɨ" + "Ǐ" + "Ȉ" + "Ȋ" + "ɪ" + "ᵻ" + "Ḭ" + "Ḯ" + "Ỉ" + "Ị" + "Ⓘ" + "ꟾ" + "Ｉ", "I", "ì" + "í" + "î" + "ï" + "ĩ" + "ī" + "ĭ" + "į" + "ı" + "ǐ" + "ȉ" + "ȋ" + "ɨ" + "ᴉ" + "ᵢ" + "ᵼ" + "ᶖ" + "ḭ" + "ḯ" + "ỉ" + "ị" + "ⁱ" + "ⓘ" + "ｉ", "i", "Ĳ", "IJ", "⒤", "(i)", "ĳ", "ij", "Ĵ" + "Ɉ" + "ᴊ" + "Ⓙ" + "Ｊ", "J", "ĵ" + "ǰ" + "ȷ" + "ɉ" + "ɟ" + "ʄ" + "ʝ" + "ⓙ" + "ⱼ" + "ｊ", "j", "⒥", "(j)", "Ķ" + "Ƙ" + "Ǩ" + "ᴋ" + "Ḱ" + "Ḳ" + "Ḵ" + "Ⓚ" + "Ⱪ" + "Ꝁ" + "Ꝃ" + "Ꝅ" + "Ｋ", "K", "ķ" + "ƙ" + "ǩ" + "ʞ" + "ᶄ" + "ḱ" + "ḳ" + "ḵ" + "ⓚ" + "ⱪ" + "ꝁ" + "ꝃ" + "ꝅ" + "ｋ", "k", "⒦", "(k)", "Ĺ" + "Ļ" + "Ľ" + "Ŀ" + "Ł" + "Ƚ" + "ʟ" + "ᴌ" + "Ḷ" + "Ḹ" + "Ḻ" + "Ḽ" + "Ⓛ" + "Ⱡ" + "Ɫ" + "Ꝇ" + "Ꝉ" + "Ꞁ" + "Ｌ", "L", "ĺ" + "ļ" + "ľ" + "ŀ" + "ł" + "ƚ" + "ȴ" + "ɫ" + "ɬ" + "ɭ" + "ᶅ" + "ḷ" + "ḹ" + "ḻ" + "ḽ" + "ⓛ" + "ⱡ" + "ꝇ" + "ꝉ" + "ꞁ" + "ｌ", "l", "Ǉ", "LJ", "Ỻ", "LL", "ǈ", "Lj", "⒧", "(l)", "ǉ", "lj", "ỻ", "ll", "ʪ", "ls", "ʫ", "lz", "Ɯ" + "ᴍ" + "Ḿ" + "Ṁ" + "Ṃ" + "Ⓜ" + "Ɱ" + "ꟽ" + "ꟿ" + "Ｍ", "M", "ɯ" + "ɰ" + "ɱ" + "ᵯ" + "ᶆ" + "ḿ" + "ṁ" + "ṃ" + "ⓜ" + "ｍ", "m", "⒨", "(m)", "Ñ" + "Ń" + "Ņ" + "Ň" + "Ŋ" + "Ɲ" + "Ǹ" + "Ƞ" + "ɴ" + "ᴎ" + "Ṅ" + "Ṇ" + "Ṉ" + "Ṋ" + "Ⓝ" + "Ｎ", "N", "ñ" + "ń" + "ņ" + "ň" + "ŉ" + "ŋ" + "ƞ" + "ǹ" + "ȵ" + "ɲ" + "ɳ" + "ᵰ" + "ᶇ" + "ṅ" + "ṇ" + "ṉ" + "ṋ" + "ⁿ" + "ⓝ" + "ｎ", "n", "Ǌ", "NJ", "ǋ", "Nj", "⒩", "(n)", "ǌ", "nj", "Ò" + "Ó" + "Ô" + "Õ" + "Ö" + "Ø" + "Ō" + "Ŏ" + "Ő" + "Ɔ" + "Ɵ" + "Ơ" + "Ǒ" + "Ǫ" + "Ǭ" + "Ǿ" + "Ȍ" + "Ȏ" + "Ȫ" + "Ȭ" + "Ȯ" + "Ȱ" + "ᴏ" + "ᴐ" + "Ṍ" + "Ṏ" + "Ṑ" + "Ṓ" + "Ọ" + "Ỏ" + "Ố" + "Ồ" + "Ổ" + "Ỗ" + "Ộ" + "Ớ" + "Ờ" + "Ở" + "Ỡ" + "Ợ" + "Ⓞ" + "Ꝋ" + "Ꝍ" + "Ｏ", "O", "ò" + "ó" + "ô" + "õ" + "ö" + "ø" + "ō" + "ŏ" + "ő" + "ơ" + "ǒ" + "ǫ" + "ǭ" + "ǿ" + "ȍ" + "ȏ" + "ȫ" + "ȭ" + "ȯ" + "ȱ" + "ɔ" + "ɵ" + "ᴖ" + "ᴗ" + "ᶗ" + "ṍ" + "ṏ" + "ṑ" + "ṓ" + "ọ" + "ỏ" + "ố" + "ồ" + "ổ" + "ỗ" + "ộ" + "ớ" + "ờ" + "ở" + "ỡ" + "ợ" + "ₒ" + "ⓞ" + "ⱺ" + "ꝋ" + "ꝍ" + "ｏ", "o", "Œ" + "ɶ", "OE", "Ꝏ", "OO", "Ȣ" + "ᴕ", "OU", "⒪", "(o)", "œ" + "ᴔ", "oe", "ꝏ", "oo", "ȣ", "ou", "Ƥ" + "ᴘ" + "Ṕ" + "Ṗ" + "Ⓟ" + "Ᵽ" + "Ꝑ" + "Ꝓ" + "Ꝕ" + "Ｐ", "P", "ƥ" + "ᵱ" + "ᵽ" + "ᶈ" + "ṕ" + "ṗ" + "ⓟ" + "ꝑ" + "ꝓ" + "ꝕ" + "ꟼ" + "ｐ", "p", "⒫", "(p)", "Ɋ" + "Ⓠ" + "Ꝗ" + "Ꝙ" + "Ｑ", "Q", "ĸ" + "ɋ" + "ʠ" + "ⓠ" + "ꝗ" + "ꝙ" + "ｑ", "q", "⒬", "(q)", "ȹ", "qp", "Ŕ" + "Ŗ" + "Ř" + "Ȑ" + "Ȓ" + "Ɍ" + "ʀ" + "ʁ" + "ᴙ" + "ᴚ" + "Ṙ" + "Ṛ" + "Ṝ" + "Ṟ" + "Ⓡ" + "Ɽ" + "Ꝛ" + "Ꞃ" + "Ｒ", "R", "ŕ" + "ŗ" + "ř" + "ȑ" + "ȓ" + "ɍ" + "ɼ" + "ɽ" + "ɾ" + "ɿ" + "ᵣ" + "ᵲ" + "ᵳ" + "ᶉ" + "ṙ" + "ṛ" + "ṝ" + "ṟ" + "ⓡ" + "ꝛ" + "ꞃ" + "ｒ", "r", "⒭", "(r)", "Ś" + "Ŝ" + "Ş" + "Š" + "Ș" + "Ṡ" + "Ṣ" + "Ṥ" + "Ṧ" + "Ṩ" + "Ⓢ" + "ꜱ" + "ꞅ" + "Ｓ", "S", "ś" + "ŝ" + "ş" + "š" + "ſ" + "ș" + "ȿ" + "ʂ" + "ᵴ" + "ᶊ" + "ṡ" + "ṣ" + "ṥ" + "ṧ" + "ṩ" + "ẜ" + "ẝ" + "ⓢ" + "Ꞅ" + "ｓ", "s", "ẞ", "SS", "⒮", "(s)", "ß", "ss", "ﬆ", "st", "Ţ" + "Ť" + "Ŧ" + "Ƭ" + "Ʈ" + "Ț" + "Ⱦ" + "ᴛ" + "Ṫ" + "Ṭ" + "Ṯ" + "Ṱ" + "Ⓣ" + "Ꞇ" + "Ｔ", "T", "ţ" + "ť" + "ŧ" + "ƫ" + "ƭ" + "ț" + "ȶ" + "ʇ" + "ʈ" + "ᵵ" + "ṫ" + "ṭ" + "ṯ" + "ṱ" + "ẗ" + "ⓣ" + "ⱦ" + "ｔ", "t", "Þ" + "Ꝧ", "TH", "Ꜩ", "TZ", "⒯", "(t)", "ʨ", "tc", "þ" + "ᵺ" + "ꝧ", "th", "ʦ", "ts", "ꜩ", "tz", "Ù" + "Ú" + "Û" + "Ü" + "Ũ" + "Ū" + "Ŭ" + "Ů" + "Ű" + "Ų" + "Ư" + "Ǔ" + "Ǖ" + "Ǘ" + "Ǚ" + "Ǜ" + "Ȕ" + "Ȗ" + "Ʉ" + "ᴜ" + "ᵾ" + "Ṳ" + "Ṵ" + "Ṷ" + "Ṹ" + "Ṻ" + "Ụ" + "Ủ" + "Ứ" + "Ừ" + "Ử" + "Ữ" + "Ự" + "Ⓤ" + "Ｕ", "U", "ù" + "ú" + "û" + "ü" + "ũ" + "ū" + "ŭ" + "ů" + "ű" + "ų" + "ư" + "ǔ" + "ǖ" + "ǘ" + "ǚ" + "ǜ" + "ȕ" + "ȗ" + "ʉ" + "ᵤ" + "ᶙ" + "ṳ" + "ṵ" + "ṷ" + "ṹ" + "ṻ" + "ụ" + "ủ" + "ứ" + "ừ" + "ử" + "ữ" + "ự" + "ⓤ" + "ｕ", "u", "⒰", "(u)", "ᵫ", "ue", "Ʋ" + "Ʌ" + "ᴠ" + "Ṽ" + "Ṿ" + "Ỽ" + "Ⓥ" + "Ꝟ" + "Ꝩ" + "Ｖ", "V", "ʋ" + "ʌ" + "ᵥ" + "ᶌ" + "ṽ" + "ṿ" + "ⓥ" + "ⱱ" + "ⱴ" + "ꝟ" + "ｖ", "v", "Ꝡ", "VY", "⒱", "(v)", "ꝡ", "vy", "Ŵ" + "Ƿ" + "ᴡ" + "Ẁ" + "Ẃ" + "Ẅ" + "Ẇ" + "Ẉ" + "Ⓦ" + "Ⱳ" + "Ｗ", "W", "ŵ" + "ƿ" + "ʍ" + "ẁ" + "ẃ" + "ẅ" + "ẇ" + "ẉ" + "ẘ" + "ⓦ" + "ⱳ" + "ｗ", "w", "⒲", "(w)", "Ẋ" + "Ẍ" + "Ⓧ" + "Ｘ", "X", "ᶍ" + "ẋ" + "ẍ" + "ₓ" + "ⓧ" + "ｘ", "x", "⒳", "(x)", "Ý" + "Ŷ" + "Ÿ" + "Ƴ" + "Ȳ" + "Ɏ" + "ʏ" + "Ẏ" + "Ỳ" + "Ỵ" + "Ỷ" + "Ỹ" + "Ỿ" + "Ⓨ" + "Ｙ", "Y", "ý" + "ÿ" + "ŷ" + "ƴ" + "ȳ" + "ɏ" + "ʎ" + "ẏ" + "ẙ" + "ỳ" + "ỵ" + "ỷ" + "ỹ" + "ỿ" + "ⓨ" + "ｙ", "y", "⒴", "(y)", "Ź" + "Ż" + "Ž" + "Ƶ" + "Ȝ" + "Ȥ" + "ᴢ" + "Ẑ" + "Ẓ" + "Ẕ" + "Ⓩ" + "Ⱬ" + "Ꝣ" + "Ｚ", "Z", "ź" + "ż" + "ž" + "ƶ" + "ȝ" + "ȥ" + "ɀ" + "ʐ" + "ʑ" + "ᵶ" + "ᶎ" + "ẑ" + "ẓ" + "ẕ" + "ⓩ" + "ⱬ" + "ꝣ" + "ｚ", "z", "⒵", "(z)", "⁰" + "₀" + "⓪" + "⓿" + "０", "0", "¹" + "₁" + "①" + "⓵" + "❶" + "➀" + "➊" + "１", "1", "⒈", "1.", "⑴", "(1)", "²" + "₂" + "②" + "⓶" + "❷" + "➁" + "➋" + "２", "2", "⒉", "2.", "⑵", "(2)", "³" + "₃" + "③" + "⓷" + "❸" + "➂" + "➌" + "３", "3", "⒊", "3.", "⑶", "(3)", "⁴" + "₄" + "④" + "⓸" + "❹" + "➃" + "➍" + "４", "4", "⒋", "4.", "⑷", "(4)", "⁵" + "₅" + "⑤" + "⓹" + "❺" + "➄" + "➎" + "５", "5", "⒌", "5.", "⑸", "(5)", "⁶" + "₆" + "⑥" + "⓺" + "❻" + "➅" + "➏" + "６", "6", "⒍", "6.", "⑹", "(6)", "⁷" + "₇" + "⑦" + "⓻" + "❼" + "➆" + "➐" + "７", "7", "⒎", "7.", "⑺", "(7)", "⁸" + "₈" + "⑧" + "⓼" + "❽" + "➇" + "➑" + "８", "8", "⒏", "8.", "⑻", "(8)", "⁹" + "₉" + "⑨" + "⓽" + "❾" + "➈" + "➒" + "９", "9", "⒐", "9.", "⑼", "(9)", "⑩" + "⓾" + "❿" + "➉" + "➓", "10", "⒑", "10.", "⑽", "(10)", "⑪" + "⓫", "11", "⒒", "11.", "⑾", "(11)", "⑫" + "⓬", "12", "⒓", "12.", "⑿", "(12)", "⑬" + "⓭", "13", "⒔", "13.", "⒀", "(13)", "⑭" + "⓮", "14", "⒕", "14.", "⒁", "(14)", "⑮" + "⓯", "15", "⒖", "15.", "⒂", "(15)", "⑯" + "⓰", "16", "⒗", "16.", "⒃", "(16)", "⑰" + "⓱", "17", "⒘", "17.", "⒄", "(17)", "⑱" + "⓲", "18", "⒙", "18.", "⒅", "(18)", "⑲" + "⓳", "19", "⒚", "19.", "⒆", "(19)", "⑳" + "⓴", "20", "⒛", "20.", "⒇", "(20)", "«" + "»" + "“" + "”" + "„" + "″" + "‶" + "❝" + "❞" + "❮" + "❯" + "＂", "\"", "‘" + "’" + "‚" + "‛" + "′" + "‵" + "‹" + "›" + "❛" + "❜" + "＇", "'", "‐" + "‑" + "‒" + "–" + "—" + "⁻" + "₋" + "－", "-", "⁅" + "❲" + "［", "[", "⁆" + "❳" + "］", "]", "⁽" + "₍" + "❨" + "❪" + "（", "(", "⸨", "((", "⁾" + "₎" + "❩" + "❫" + "）", ")", "⸩", "))", "❬" + "❰" + "＜", "<", "❭" + "❱" + "＞", ">", "❴" + "｛", "{", "❵" + "｝", "}", "⁺" + "₊" + "＋", "+", "⁼" + "₌" + "＝", "=", "！", "!", "‼", "!!", "⁉", "!?", "＃", "#", "＄", "$", "⁒" + "％", "%", "＆", "&", "⁎" + "＊", "*", "，", ",", "．", ".", "⁄" + "／", "/", "：", ":", "⁏" + "；", ";", "？", "?", "⁇", "??", "⁈", "?!", "＠", "@", "＼", "\\", "‸" + "＾", "^", "＿", "_", "⁓" + "～", "~" };

            // Construct input text and expected output tokens
            IList<string> expectedUnfoldedTokens = new JCG.List<string>();
            IList<string> expectedFoldedTokens = new JCG.List<string>();
            StringBuilder inputText = new StringBuilder();
            for (int n = 0; n < foldings.Length; n += 2)
            {
                if (n > 0)
                {
                    inputText.Append(' '); // Space between tokens
                }
                inputText.Append(foldings[n]);

                // Construct the expected output tokens: both the unfolded and folded string,
                // with the folded duplicated as many times as the number of characters in
                // the input text.
                StringBuilder expected = new StringBuilder();
                int numChars = foldings[n].Length;
                for (int m = 0; m < numChars; ++m)
                {
                    expected.Append(foldings[n + 1]);
                }
                expectedUnfoldedTokens.Add(foldings[n]);
                expectedFoldedTokens.Add(expected.ToString());
            }

            TokenStream stream = new MockTokenizer(new StringReader(inputText.ToString()), MockTokenizer.WHITESPACE, false);
            ASCIIFoldingFilter filter = new ASCIIFoldingFilter(stream, Random.NextBoolean());
            ICharTermAttribute termAtt = filter.GetAttribute<ICharTermAttribute>();
            IEnumerator<string> unfoldedIter = expectedUnfoldedTokens.GetEnumerator();
            IEnumerator<string> foldedIter = expectedFoldedTokens.GetEnumerator();
            filter.Reset();
            while (foldedIter.MoveNext())
            {
                unfoldedIter.MoveNext();
                assertNextTerms(unfoldedIter.Current, foldedIter.Current, filter, termAtt);
            }
            assertFalse(filter.IncrementToken());
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new ASCIIFoldingFilter(tokenizer, Random.NextBoolean()));
            });
            CheckRandomData(Random, a, 1000 * RandomMultiplier);
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new ASCIIFoldingFilter(tokenizer, Random.NextBoolean()));
            });
            CheckOneTerm(a, "", "");
        }
    }
}