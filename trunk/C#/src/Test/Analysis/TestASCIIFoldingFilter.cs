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

using System;

using NUnit.Framework;

using TermAttribute = Lucene.Net.Analysis.Tokenattributes.TermAttribute;

namespace Lucene.Net.Analysis
{
	
    [TestFixture]
	public class TestASCIIFoldingFilter:BaseTokenStreamTestCase
	{
		
		// testLain1Accents() is a copy of TestLatin1AccentFilter.testU().
        [Test]
		public virtual void  testLatin1Accents()
		{
			TokenStream stream = new WhitespaceTokenizer(new System.IO.StringReader("Des mot clÃ©s Ã€ LA CHAÃŽNE Ã€ ï¿½? Ã‚ Ãƒ Ã„ Ã… Ã† Ã‡ Ãˆ Ã‰ ÃŠ Ã‹ ÃŒ ï¿½? ÃŽ ï¿½? Ä² ï¿½? Ã‘" + " Ã’ Ã“ Ã” Ã• Ã– Ã˜ Å’ Ãž Ã™ Ãš Ã› Ãœ ï¿½? Å¸ Ã  Ã¡ Ã¢ Ã£ Ã¤ Ã¥ Ã¦ Ã§ Ã¨ Ã© Ãª Ã« Ã¬ Ã­ Ã® Ã¯ Ä³" + " Ã° Ã± Ã² Ã³ Ã´ Ãµ Ã¶ Ã¸ Å“ ÃŸ Ã¾ Ã¹ Ãº Ã» Ã¼ Ã½ Ã¿ ï¿½? ï¬‚"));
			ASCIIFoldingFilter filter = new ASCIIFoldingFilter(stream);
			
			TermAttribute termAtt = (TermAttribute) filter.GetAttribute(typeof(TermAttribute));
			
			AssertTermEquals("Des", filter, termAtt);
			AssertTermEquals("mot", filter, termAtt);
			AssertTermEquals("cles", filter, termAtt);
			AssertTermEquals("A", filter, termAtt);
			AssertTermEquals("LA", filter, termAtt);
			AssertTermEquals("CHAINE", filter, termAtt);
			AssertTermEquals("A", filter, termAtt);
			AssertTermEquals("A", filter, termAtt);
			AssertTermEquals("A", filter, termAtt);
			AssertTermEquals("A", filter, termAtt);
			AssertTermEquals("A", filter, termAtt);
			AssertTermEquals("A", filter, termAtt);
			AssertTermEquals("AE", filter, termAtt);
			AssertTermEquals("C", filter, termAtt);
			AssertTermEquals("E", filter, termAtt);
			AssertTermEquals("E", filter, termAtt);
			AssertTermEquals("E", filter, termAtt);
			AssertTermEquals("E", filter, termAtt);
			AssertTermEquals("I", filter, termAtt);
			AssertTermEquals("I", filter, termAtt);
			AssertTermEquals("I", filter, termAtt);
			AssertTermEquals("I", filter, termAtt);
			AssertTermEquals("IJ", filter, termAtt);
			AssertTermEquals("D", filter, termAtt);
			AssertTermEquals("N", filter, termAtt);
			AssertTermEquals("O", filter, termAtt);
			AssertTermEquals("O", filter, termAtt);
			AssertTermEquals("O", filter, termAtt);
			AssertTermEquals("O", filter, termAtt);
			AssertTermEquals("O", filter, termAtt);
			AssertTermEquals("O", filter, termAtt);
			AssertTermEquals("OE", filter, termAtt);
			AssertTermEquals("TH", filter, termAtt);
			AssertTermEquals("U", filter, termAtt);
			AssertTermEquals("U", filter, termAtt);
			AssertTermEquals("U", filter, termAtt);
			AssertTermEquals("U", filter, termAtt);
			AssertTermEquals("Y", filter, termAtt);
			AssertTermEquals("Y", filter, termAtt);
			AssertTermEquals("a", filter, termAtt);
			AssertTermEquals("a", filter, termAtt);
			AssertTermEquals("a", filter, termAtt);
			AssertTermEquals("a", filter, termAtt);
			AssertTermEquals("a", filter, termAtt);
			AssertTermEquals("a", filter, termAtt);
			AssertTermEquals("ae", filter, termAtt);
			AssertTermEquals("c", filter, termAtt);
			AssertTermEquals("e", filter, termAtt);
			AssertTermEquals("e", filter, termAtt);
			AssertTermEquals("e", filter, termAtt);
			AssertTermEquals("e", filter, termAtt);
			AssertTermEquals("i", filter, termAtt);
			AssertTermEquals("i", filter, termAtt);
			AssertTermEquals("i", filter, termAtt);
			AssertTermEquals("i", filter, termAtt);
			AssertTermEquals("ij", filter, termAtt);
			AssertTermEquals("d", filter, termAtt);
			AssertTermEquals("n", filter, termAtt);
			AssertTermEquals("o", filter, termAtt);
			AssertTermEquals("o", filter, termAtt);
			AssertTermEquals("o", filter, termAtt);
			AssertTermEquals("o", filter, termAtt);
			AssertTermEquals("o", filter, termAtt);
			AssertTermEquals("o", filter, termAtt);
			AssertTermEquals("oe", filter, termAtt);
			AssertTermEquals("ss", filter, termAtt);
			AssertTermEquals("th", filter, termAtt);
			AssertTermEquals("u", filter, termAtt);
			AssertTermEquals("u", filter, termAtt);
			AssertTermEquals("u", filter, termAtt);
			AssertTermEquals("u", filter, termAtt);
			AssertTermEquals("y", filter, termAtt);
			AssertTermEquals("y", filter, termAtt);
			AssertTermEquals("fi", filter, termAtt);
			AssertTermEquals("fl", filter, termAtt);
			Assert.IsFalse(filter.IncrementToken());
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
		public virtual void  testAllFoldings()
		{
			// Alternating strings of:
			//   1. All non-ASCII characters to be folded, concatenated together as a
			//      single string.
			//   2. The string of ASCII characters to which each of the above
			//      characters should be folded.
			System.String[] foldings = new System.String[]{"Ã€" + "ï¿½?" + "Ã‚" + "Ãƒ" + "Ã„" + "Ã…" + "Ä€" + "Ä‚" + "Ä„" + "ï¿½?" + "ï¿½?" + "Çž" + "Ç " + "Çº" + "È€" + "È‚" + "È¦" + "Èº" + "á´€" + "á¸€" + "áº " + "áº¢" + "áº¤" + "áº¦" + "áº¨" + "áºª" + "áº¬" + "áº®" + "áº°" + "áº²" + "áº´" + "áº¶" + "â’¶" + "ï¼¡", "A", "Ã " + "Ã¡" + "Ã¢" + "Ã£" + "Ã¤" + "Ã¥" + "ï¿½?" + "Äƒ" + "Ä…" + "ÇŽ" + "ÇŸ" + "Ç¡" + "Ç»" + "ï¿½?" + "Èƒ" + "È§" + "ï¿½?" + "É™" + "Éš" + "ï¿½?" + "ï¿½?" + "á¶•" + "áºš" + "áº¡" + "áº£" + "áº¥" + "áº§" + "áº©" + "áº«" + "áº­" + "áº¯" + "áº±" + "áº³" + "áºµ" + "áº·" + "ï¿½?" + "â‚”" + "ï¿½?" + "â±¥" + "â±¯" + "ï¿½?", "a", "êœ²", "AA", "Ã†" + "Ç¢" + "Ç¼" + "ï¿½?", "AE", "êœ´", "AO", "êœ¶", "AU", "êœ¸" + "êœº", "AV", "êœ¼", "AY", "â’œ", "(a)", "êœ³", "aa", "Ã¦" + "Ç£" + "Ç½" + "á´‚", "ae", "êœµ", "ao", "êœ·", "au", "êœ¹" + "êœ»", "av", "êœ½", "ay", "ï¿½?" + "Æ‚" + "Éƒ" + "Ê™" + "á´ƒ" + "á¸‚" + "á¸„" + "á¸†" + "â’·" + "ï¼¢", "B", "Æ€" + "Æƒ" + "É“" + "áµ¬" + "á¶€" + "á¸ƒ" + "á¸…" + "á¸‡" + "â“‘" + "ï½‚", "b", "ï¿½?", "(b)", "Ã‡" + "Ä†" + "Äˆ" + "ÄŠ" + "ÄŒ" + "Æ‡" + "È»" + "Ê—" + "á´„" + "á¸ˆ" + "â’¸" + "ï¼£", "C", "Ã§" + "Ä‡" + "Ä‰" + "Ä‹" + "ï¿½?" + "Æˆ" + "È¼" + "É•" + "á¸‰" + "â†„" + "â“’" + "êœ¾" + "êœ¿" + "ï½ƒ", "c", "â’ž", "(c)", "ï¿½?" + "ÄŽ" + "ï¿½?" + "Æ‰" + "ÆŠ" + "Æ‹" + "á´…" + "á´†" + "á¸Š" + "á¸Œ" + "á¸Ž" + "ï¿½?" + "á¸’" + "â’¹" + "ï¿½?ï¿½" + "ï¼¤", "D", "Ã°" + "ï¿½?" + "Ä‘" + "ÆŒ" + "È¡" + "É–" + "É—" + "áµ­" + "ï¿½?" + "á¶‘" + "á¸‹" + "ï¿½?" + "ï¿½?" + "á¸‘" + "á¸“" + "â““" + "ï¿½?ï¿½" + "ï½„", "d", "Ç„" + "Ç±", "DZ", "Ç…" + "Ç²", "Dz", "â’Ÿ", "(d)", "È¸", "db", "Ç†" + "Ç³" + "Ê£" + "Ê¥", "dz", "Ãˆ" + "Ã‰" + "ÃŠ" + "Ã‹" + "Ä’" + "Ä”" + "Ä–" + "Ä˜" + "Äš" + "ÆŽ" + "ï¿½?" + "È„" + "È†" + "È¨" + "É†" + "á´‡" + "á¸”" + "á¸–" + "á¸˜" + "á¸š" + "á¸œ" + "áº¸" + "áºº" + "áº¼" + "áº¾" + "á»€" + "á»‚" + "á»„" + "á»†" + "â’º" + "â±»" + "ï¼¥", "E", "Ã¨" + "Ã©" + "Ãª" + "Ã«" + "Ä“" + "Ä•" + "Ä—" + "Ä™" + "Ä›" + "ï¿½?" + "È…" + "È‡" + "È©" + "É‡" + "É˜" + "É›" + "Éœ" + "ï¿½?" + "Éž" + "Êš" + 
				"á´ˆ" + "á¶’" + "á¶“" + "á¶”" + "á¸•" + "á¸—" + "á¸™" + "á¸›" + "ï¿½?" + "áº¹" + "áº»" + "áº½" + "áº¿" + "ï¿½?" + "á»ƒ" + "á»…" + "á»‡" + "â‚‘" + "â“”" + "â±¸" + "ï½…", "e", "â’ ", "(e)", "Æ‘" + "á¸ž" + "â’»" + "êœ°" + "ï¿½?ï¿½" + "êŸ»" + "ï¼¦", "F", "Æ’" + "áµ®" + "á¶‚" + "á¸Ÿ" + "áº›" + "â“•" + "ï¿½?ï¿½" + "ï½†", "f", "â’¡", "(f)", "ï¬€", "ff", "ï¬ƒ", "ffi", "ï¬„", "ffl", "ï¿½?", "fi", "ï¬‚", "fl", "Äœ" + "Äž" + "Ä " + "Ä¢" + "Æ“" + "Ç¤" + "Ç¥" + "Ç¦" + "Ç§" + "Ç´" + "É¢" + "Ê›" + "á¸ " + "â’¼" + "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï¼§", "G", "ï¿½?" + "ÄŸ" + "Ä¡" + "Ä£" + "Çµ" + "É " + "É¡" + "áµ·" + "áµ¹" + "á¶ƒ" + "á¸¡" + "â“–" + "ï¿½?ï¿½" + "ï½‡", "g", "â’¢", "(g)", "Ä¤" + "Ä¦" + "Èž" + "Êœ" + "á¸¢" + "á¸¤" + "á¸¦" + "á¸¨" + "á¸ª" + "â’½" + "â±§" + "â±µ" + "ï¼¨", "H", "Ä¥" + "Ä§" + "ÈŸ" + "É¥" + "É¦" + "Ê®" + "Ê¯" + "á¸£" + "á¸¥" + "á¸§" + "á¸©" + "á¸«" + "áº–" + "â“—" + "â±¨" + "â±¶" + "ï½ˆ", "h", "Ç¶", "HV", "â’£", "(h)", "Æ•", "hv", "ÃŒ" + "ï¿½?" + "ÃŽ" + "ï¿½?" + "Ä¨" + "Äª" + "Ä¬" + "Ä®" + "Ä°" + "Æ–" + "Æ—" + "ï¿½?" + "Èˆ" + "ÈŠ" + "Éª" + "áµ»" + "á¸¬" + "á¸®" + "á»ˆ" + "á»Š" + "â’¾" + "êŸ¾" + "ï¼©", "I", "Ã¬" + "Ã­" + "Ã®" + "Ã¯" + "Ä©" + "Ä«" + "Ä­" + "Ä¯" + "Ä±" + "ï¿½?" + "È‰" + "È‹" + "É¨" + "á´‰" + "áµ¢" + "áµ¼" + "á¶–" + "á¸­" + "á¸¯" + "á»‰" + "á»‹" + "ï¿½?ï¿½" + "â“˜" + "ï½‰", "i", "Ä²", "IJ", "â’¤", "(i)", "Ä³", "ij", "Ä´" + "Éˆ" + "á´Š" + "â’¿" + "ï¼ª", "J", "Äµ" + "Ç°" + "È·" + "É‰" + "ÉŸ" + "Ê„" + "ï¿½?" + "â“™" + "â±¼" + "ï½Š", "j", "â’¥", "(j)", "Ä¶" + "Æ˜" + "Ç¨" + "á´‹" + "á¸°" + "á¸²" + "á¸´" + "â“€" + "â±©" + "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï¼«", "K", "Ä·" + "Æ™" + "Ç©" + "Êž" + "á¶„" + "á¸±" + "á¸³" + "á¸µ" + "â“š" + "â±ª" + "ï¿½??" + "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï½‹", "k", "â’¦", "(k)", "Ä¹" + "Ä»" + "Ä½" + "Ä¿" + "ï¿½?" + "È½" + "ÊŸ" + "á´Œ" + "á¸¶" + "á¸¸" + "á¸º" + "á¸¼" + "ï¿½?" + "â± " + "â±¢" + "ï¿½?ï¿½" + "ï¿½?ï¿½" + "êž€" + "ï¼¬", "L", "Äº" + "Ä¼" + "Ä¾" + "Å€" + "Å‚" + "Æš" + "È´" + "É«" + "É¬" + "É­" + "á¶…" + "á¸·" + "á¸¹" + "á¸»" + "á¸½" + "â“›" + "â±¡" + 
				"ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï¿½?" + "ï½Œ", "l", "Ç‡", "LJ", "á»º", "LL", "Çˆ", "Lj", "â’§", "(l)", "Ç‰", "lj", "á»»", "ll", "Êª", "ls", "Ê«", "lz", "Æœ" + "ï¿½?" + "á¸¾" + "á¹€" + "á¹‚" + "â“‚" + "â±®" + "êŸ½" + "êŸ¿" + "ï¼­", "M", "É¯" + "É°" + "É±" + "áµ¯" + "á¶†" + "á¸¿" + "ï¿½?" + "á¹ƒ" + "â“œ" + "ï¿½?", "m", "â’¨", "(m)", "Ã‘" + "Åƒ" + "Å…" + "Å‡" + "ÅŠ" + "ï¿½?" + "Ç¸" + "È " + "É´" + "á´Ž" + "á¹„" + "á¹†" + "á¹ˆ" + "á¹Š" + "â“ƒ" + "ï¼®", "N", "Ã±" + "Å„" + "Å†" + "Åˆ" + "Å‰" + "Å‹" + "Æž" + "Ç¹" + "Èµ" + "É²" + "É³" + "áµ°" + "á¶‡" + "á¹…" + "á¹‡" + "á¹‰" + "á¹‹" + "ï¿½?ï¿½" + "ï¿½?" + "ï½Ž", "n", "ÇŠ", "NJ", "Ç‹", "Nj", "â’©", "(n)", "ÇŒ", "nj", "Ã’" + "Ã“" + "Ã”" + "Ã•" + "Ã–" + "Ã˜" + "ÅŒ" + "ÅŽ" + "ï¿½?" + "Æ†" + "ÆŸ" + "Æ " + "Ç‘" + "Çª" + "Ç¬" + "Ç¾" + "ÈŒ" + "ÈŽ" + "Èª" + "È¬" + "È®" + "È°" + "ï¿½?" + "ï¿½?" + "á¹Œ" + "á¹Ž" + "ï¿½?" + "á¹’" + "á»Œ" + "á»Ž" + "ï¿½?" + "á»’" + "á»”" + "á»–" + "á»˜" + "á»š" + "á»œ" + "á»ž" + "á» " + "á»¢" + "â“„" + "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï¼¯", "O", "Ã²" + "Ã³" + "Ã´" + "Ãµ" + "Ã¶" + "Ã¸" + "ï¿½?" + "ï¿½?" + "Å‘" + "Æ¡" + "Ç’" + "Ç«" + "Ç­" + "Ç¿" + "ï¿½?" + "ï¿½?" + "È«" + "È­" + "È¯" + "È±" + "É”" + "Éµ" + "á´–" + "á´—" + "á¶—" + "ï¿½?" + "ï¿½?" + "á¹‘" + "á¹“" + "ï¿½?" + "ï¿½?" + "á»‘" + "á»“" + "á»•" + "á»—" + "á»™" + "á»›" + "ï¿½?" + "á»Ÿ" + "á»¡" + "á»£" + "â‚’" + "â“ž" + "â±º" + "ï¿½?ï¿½" + "ï¿½??" + "ï¿½?", "o", "Å’" + "É¶", "OE", "ï¿½?ï¿½", "OO", "È¢" + "á´•", "OU", "â’ª", "(o)", "Å“" + "á´”", "oe", "ï¿½??", "oo", "È£", "ou", "Æ¤" + "á´˜" + "á¹”" + "á¹–" + "â“…" + "â±£" + "ï¿½??" + "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï¼°", "P", "Æ¥" + "áµ±" + "áµ½" + "á¶ˆ" + "á¹•" + "á¹—" + "â“Ÿ" + "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï¿½?ï¿½" + "êŸ¼" + "ï¿½?", "p", "â’«", "(p)", "ÉŠ" + "â“†" + "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï¼±", "Q", "Ä¸" + "É‹" + "Ê " + "â“ " + "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï½‘", "q", "â’¬", "(q)", "È¹", "qp", "Å”" + "Å–" + "Å˜" + "ï¿½?" + "È’" + "ÉŒ" + "Ê€" + "ï¿½?" + "á´™" + "á´š" + "á¹˜" + "á¹š" + "á¹œ" + "á¹ž" + "â“‡" + "â±¤" + "ï¿½?ï¿½" + "êž‚" + "ï¼²", "R", "Å•" + 
				"Å—" + "Å™" + "È‘" + "È“" + "ï¿½?" + "É¼" + "É½" + "É¾" + "É¿" + "áµ£" + "áµ²" + "áµ³" + "á¶‰" + "á¹™" + "á¹›" + "ï¿½?" + "á¹Ÿ" + "â“¡" + "ï¿½?ï¿½" + "êžƒ" + "ï½’", "r", "â’­", "(r)", "Åš" + "Åœ" + "Åž" + "Å " + "È˜" + "á¹ " + "á¹¢" + "á¹¤" + "á¹¦" + "á¹¨" + "â“ˆ" + "êœ±" + "êž…" + "ï¼³", "S", "Å›" + "ï¿½?" + "ÅŸ" + "Å¡" + "Å¿" + "È™" + "È¿" + "Ê‚" + "áµ´" + "á¶Š" + "á¹¡" + "á¹£" + "á¹¥" + "á¹§" + "á¹©" + "áºœ" + "ï¿½?" + "â“¢" + "êž„" + "ï½“", "s", "áºž", "SS", "â’®", "(s)", "ÃŸ", "ss", "ï¬†", "st", "Å¢" + "Å¤" + "Å¦" + "Æ¬" + "Æ®" + "Èš" + "È¾" + "á´›" + "á¹ª" + "á¹¬" + "á¹®" + "á¹°" + "â“‰" + "êž†" + "ï¼´", "T", "Å£" + "Å¥" + "Å§" + "Æ«" + "Æ­" + "È›" + "È¶" + "Ê‡" + "Êˆ" + "áµµ" + "á¹«" + "á¹­" + "á¹¯" + "á¹±" + "áº—" + "â“£" + "â±¦" + "ï½”", "t", "Ãž" + "ï¿½?ï¿½", "TH", "êœ¨", "TZ", "â’¯", "(t)", "Ê¨", "tc", "Ã¾" + "áµº" + "ï¿½?ï¿½", "th", "Ê¦", "ts", "êœ©", "tz", "Ã™" + "Ãš" + "Ã›" + "Ãœ" + "Å¨" + "Åª" + "Å¬" + "Å®" + "Å°" + "Å²" + "Æ¯" + "Ç“" + "Ç•" + "Ç—" + "Ç™" + "Ç›" + "È”" + "È–" + "É„" + "á´œ" + "áµ¾" + "á¹²" + "á¹´" + "á¹¶" + "á¹¸" + "á¹º" + "á»¤" + "á»¦" + "á»¨" + "á»ª" + "á»¬" + "á»®" + "á»°" + "â“Š" + "ï¼µ", "U", "Ã¹" + "Ãº" + "Ã»" + "Ã¼" + "Å©" + "Å«" + "Å­" + "Å¯" + "Å±" + "Å³" + "Æ°" + "Ç”" + "Ç–" + "Ç˜" + "Çš" + "Çœ" + "È•" + "È—" + "Ê‰" + "áµ¤" + "á¶™" + "á¹³" + "á¹µ" + "á¹·" + "á¹¹" + "á¹»" + "á»¥" + "á»§" + "á»©" + "á»«" + "á»­" + "á»¯" + "á»±" + "â“¤" + "ï½•", "u", "â’°", "(u)", "áµ«", "ue", "Æ²" + "É…" + "á´ " + "á¹¼" + "á¹¾" + "á»¼" + "â“‹" + "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï¼¶", "V", "Ê‹" + "ÊŒ" + "áµ¥" + "á¶Œ" + "á¹½" + "á¹¿" + "â“¥" + "â±±" + "â±´" + "ï¿½?ï¿½" + "ï½–", "v", "ï¿½?ï¿½", "VY", "â’±", "(v)", "ï¿½?ï¿½", "vy", "Å´" + "Ç·" + "á´¡" + "áº€" + "áº‚" + "áº„" + "áº†" + "áºˆ" + "â“Œ" + "â±²" + "ï¼·", "W", "Åµ" + "Æ¿" + "ï¿½?" + "ï¿½?" + "áºƒ" + "áº…" + "áº‡" + "áº‰" + "áº˜" + "â“¦" + "â±³" + "ï½—", "w", "â’²", "(w)", "áºŠ" + "áºŒ" + "ï¿½?" + "ï¼¸", "X", "ï¿½?" + "áº‹" + "ï¿½?" + "â‚“" + "â“§" + "ï½˜", "x", "â’³", "(x)", "ï¿½?" + "Å¶" + "Å¸" + "Æ³" + "È²" + "ÉŽ" + 
				"ï¿½?" + "áºŽ" + "á»²" + "á»´" + "á»¶" + "á»¸" + "á»¾" + "â“Ž" + "ï¼¹", "Y", "Ã½" + "Ã¿" + "Å·" + "Æ´" + "È³" + "ï¿½?" + "ÊŽ" + "ï¿½?" + "áº™" + "á»³" + "á»µ" + "á»·" + "á»¹" + "á»¿" + "â“¨" + "ï½™", "y", "â’´", "(y)", "Å¹" + "Å»" + "Å½" + "Æµ" + "Èœ" + "È¤" + "á´¢" + "ï¿½?" + "áº’" + "áº”" + "ï¿½?" + "â±«" + "ï¿½?ï¿½" + "ï¼º", "Z", "Åº" + "Å¼" + "Å¾" + "Æ¶" + "ï¿½?" + "È¥" + "É€" + "ï¿½?" + "Ê‘" + "áµ¶" + "á¶Ž" + "áº‘" + "áº“" + "áº•" + "â“©" + "â±¬" + "ï¿½?ï¿½" + "ï½š", "z", "â’µ", "(z)", "ï¿½?ï¿½" + "â‚€" + "â“ª" + "â“¿" + "ï¿½?", "0", "Â¹" + "ï¿½?" + "â‘ " + "â“µ" + "ï¿½?ï¿½" + "âž€" + "âžŠ" + "ï¼‘", "1", "â’ˆ", "1.", "â‘´", "(1)", "Â²" + "â‚‚" + "â‘¡" + "â“¶" + "ï¿½?ï¿½" + "ï¿½?" + "âž‹" + "ï¼’", "2", "â’‰", "2.", "â‘µ", "(2)", "Â³" + "â‚ƒ" + "â‘¢" + "â“·" + "ï¿½?ï¿½" + "âž‚" + "âžŒ" + "ï¼“", "3", "â’Š", "3.", "â‘¶", "(3)", "ï¿½?ï¿½" + "â‚„" + "â‘£" + "â“¸" + "ï¿½?ï¿½" + "âžƒ" + "ï¿½?" + "ï¼”", "4", "â’‹", "4.", "â‘·", "(4)", "ï¿½?ï¿½" + "â‚…" + "â‘¤" + "â“¹" + "ï¿½?ï¿½" + "âž„" + "âžŽ" + "ï¼•", "5", "â’Œ", "5.", "â‘¸", "(5)", "ï¿½?ï¿½" + "â‚†" + "â‘¥" + "â“º" + "ï¿½?ï¿½" + "âž…" + "ï¿½?" + "ï¼–", "6", "ï¿½?", "6.", "â‘¹", "(6)", "ï¿½?ï¿½" + "â‚‡" + "â‘¦" + "â“»" + "ï¿½?ï¿½" + "âž†" + "ï¿½?" + "ï¼—", "7", "â’Ž", "7.", "â‘º", "(7)", "ï¿½?ï¿½" + "â‚ˆ" + "â‘§" + "â“¼" + "ï¿½?ï¿½" + "âž‡" + "âž‘" + "ï¼˜", "8", "ï¿½?", "8.", "â‘»", "(8)", "ï¿½?ï¿½" + "â‚‰" + "â‘¨" + "â“½" + "ï¿½?ï¿½" + "âžˆ" + "âž’" + "ï¼™", "9", "ï¿½?", "9.", "â‘¼", "(9)", "â‘©" + "â“¾" + "ï¿½?ï¿½" + "âž‰" + "âž“", "10", "â’‘", "10.", "â‘½", "(10)", "â‘ª" + "â“«", "11", "â’’", "11.", "â‘¾", "(11)", "â‘«" + "â“¬", "12", "â’“", "12.", "â‘¿", "(12)", "â‘¬" + "â“­", "13", "â’”", "13.", "â’€", "(13)", "â‘­" + "â“®", "14", "â’•", "14.", "ï¿½?", "(14)", "â‘®" + "â“¯", "15", "â’–", "15.", "â’‚", "(15)", "â‘¯" + "â“°", "16", "â’—", "16.", "â’ƒ", "(16)", "â‘°" + "â“±", "17", "â’˜", "17.", "â’„", "(17)", "â‘±" + "â“²", "18", "â’™", "18.", "â’…", "(18)", "â‘²" + "â“³", "19", "â’š", "19.", "â’†", "(19)", "â‘³" + "â“´", "20", "â’›", 
				"20.", "â’‡", "(20)", "Â«" + "Â»" + "â€œ" + "ï¿½?" + "â€ž" + "â€³" + "â€¶" + "ï¿½??" + "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï¼‚", "\"", "â€˜" + "â€™" + "â€š" + "â€›" + "â€²" + "â€µ" + "â€¹" + "â€º" + "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï¼‡", "'", "ï¿½?" + "â€‘" + "â€’" + "â€“" + "â€”" + "ï¿½?ï¿½" + "â‚‹" + "ï¿½?", "-", "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï¼»", "[", "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï¼½", "]", "ï¿½?ï¿½" + "ï¿½?" + "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï¼ˆ", "(", "â¸¨", "((", "ï¿½?ï¿½" + "â‚Ž" + "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï¼‰", ")", "â¸©", "))", "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï¼œ", "<", "ï¿½?ï¿½" + "ï¿½?ï¿½" + "ï¼ž", ">", "ï¿½?ï¿½" + "ï½›", "{", "ï¿½?ï¿½" + "ï¿½?", "}", "ï¿½?ï¿½" + "â‚Š" + "ï¼‹", "+", "ï¿½?ï¿½" + "â‚Œ" + "ï¿½?", "=", "ï¿½?", "!", "â€¼", "!!", "ï¿½?ï¿½", "!?", "ï¼ƒ", "#", "ï¼„", "$", "ï¿½?ï¿½" + "ï¼…", "%", "ï¼†", "&", "ï¿½?ï¿½" + "ï¼Š", "*", "ï¼Œ", ",", "ï¼Ž", ".", "ï¿½?ï¿½" + "ï¿½?", "/", "ï¼š", ":", "ï¿½??" + "ï¼›", ";", "ï¼Ÿ", "?", "ï¿½?ï¿½", "??", "ï¿½?ï¿½", "?!", "ï¼ ", "@", "ï¼¼", "\\", "â€¸" + "ï¼¾", "^", "ï¼¿", "_", "ï¿½?ï¿½" + "ï½ž", "~"};
			
			// Construct input text and expected output tokens
			System.Collections.IList expectedOutputTokens = new System.Collections.ArrayList();
			System.Text.StringBuilder inputText = new System.Text.StringBuilder();
			for (int n = 0; n < foldings.Length; n += 2)
			{
				if (n > 0)
				{
					inputText.Append(' '); // Space between tokens
				}
				inputText.Append(foldings[n]);
				
				// Construct the expected output token: the ASCII string to fold to,
				// duplicated as many times as the number of characters in the input text.
				System.Text.StringBuilder expected = new System.Text.StringBuilder();
				int numChars = foldings[n].Length;
				for (int m = 0; m < numChars; ++m)
				{
					expected.Append(foldings[n + 1]);
				}
				expectedOutputTokens.Add(expected.ToString());
			}
			
			TokenStream stream = new WhitespaceTokenizer(new System.IO.StringReader(inputText.ToString()));
			ASCIIFoldingFilter filter = new ASCIIFoldingFilter(stream);
			TermAttribute termAtt = (TermAttribute) filter.GetAttribute(typeof(TermAttribute));
			System.Collections.IEnumerator expectedIter = expectedOutputTokens.GetEnumerator();
			while (expectedIter.MoveNext())
			{
				;
				AssertTermEquals((System.String) expectedIter.Current, filter, termAtt);
			}
			Assert.IsFalse(filter.IncrementToken());
		}
		
		internal virtual void  AssertTermEquals(System.String expected, TokenStream stream, TermAttribute termAtt)
		{
			Assert.IsTrue(stream.IncrementToken());
			Assert.AreEqual(expected, termAtt.Term());
		}
	}
}