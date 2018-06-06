
<!--
 Licensed to the Apache Software Foundation (ASF) under one or more
 contributor license agreements.  See the NOTICE file distributed with
 this work for additional information regarding copyright ownership.
 The ASF licenses this file to You under the Apache License, Version 2.0
 (the "License"); you may not use this file except in compliance with
 the License.  You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
-->

A filter that decomposes compound words you find in many Germanic
languages into the word parts. This example shows what it does:
<table border="1">
	<tr>
		<th>Input token stream</th>
	</tr>
	<tr>
		<td>Rindfleischüberwachungsgesetz Drahtschere abba</td>
	</tr>
</table>

<table border="1">
	<tr>
		<th>Output token stream</th>
	</tr>
	<tr>
		<td>(Rindfleischüberwachungsgesetz,0,29)</td>
	</tr>
	<tr>
		<td>(Rind,0,4,posIncr=0)</td>
	</tr>
	<tr>
		<td>(fleisch,4,11,posIncr=0)</td>
	</tr>
	<tr>
		<td>(überwachung,11,22,posIncr=0)</td>
	</tr>
	<tr>
		<td>(gesetz,23,29,posIncr=0)</td>
	</tr>
	<tr>
		<td>(Drahtschere,30,41)</td>
	</tr>
	<tr>
		<td>(Draht,30,35,posIncr=0)</td>
	</tr>
	<tr>
		<td>(schere,35,41,posIncr=0)</td>
	</tr>
	<tr>
		<td>(abba,42,46)</td>
	</tr>
</table>

The input token is always preserved and the filters do not alter the case of word parts. There are two variants of the
filter available:

*   *HyphenationCompoundWordTokenFilter*: it uses a
	hyphenation grammar based approach to find potential word parts of a
	given word.
*   *DictionaryCompoundWordTokenFilter*: it uses a
	brute-force dictionary-only based approach to find the word parts of a given
	word.

### Compound word token filters

#### HyphenationCompoundWordTokenFilter

The [](xref:Lucene.Net.Analysis.Compound.HyphenationCompoundWordTokenFilter
HyphenationCompoundWordTokenFilter) uses hyphenation grammars to find
potential subwords that a worth to check against the dictionary. It can be used
without a dictionary as well but then produces a lot of "nonword" tokens.
The quality of the output tokens is directly connected to the quality of the
grammar file you use. For languages like German they are quite good.

##### Grammar file

Unfortunately we cannot bundle the hyphenation grammar files with Lucene
because they do not use an ASF compatible license (they use the LaTeX
Project Public License instead). You can find the XML based grammar
files at the
[Objects
For Formatting Objects](http://offo.sourceforge.net/hyphenation/index.html)
(OFFO) Sourceforge project (direct link to download the pattern files:
[http://downloads.sourceforge.net/offo/offo-hyphenation.zip](http://downloads.sourceforge.net/offo/offo-hyphenation.zip)
). The files you need are in the subfolder
*offo-hyphenation/hyph/*
.

Credits for the hyphenation code go to the
[Apache FOP project](http://xmlgraphics.apache.org/fop/)
.

#### DictionaryCompoundWordTokenFilter

The [](xref:Lucene.Net.Analysis.Compound.DictionaryCompoundWordTokenFilter
DictionaryCompoundWordTokenFilter) uses a dictionary-only approach to
find subwords in a compound word. It is much slower than the one that
uses the hyphenation grammars. You can use it as a first start to
see if your dictionary is good or not because it is much simpler in design.

### Dictionary

The output quality of both token filters is directly connected to the
quality of the dictionary you use. They are language dependent of course.
You always should use a dictionary
that fits to the text you want to index. If you index medical text for
example then you should use a dictionary that contains medical words.
A good start for general text are the dictionaries you find at the
[OpenOffice
dictionaries](http://wiki.services.openoffice.org/wiki/Dictionaries)
Wiki.

### Which variant should I use?

This decision matrix should help you:
<table border="1">
	<tr>
		<th>Token filter</th>
		<th>Output quality</th>
		<th>Performance</th>
	</tr>
	<tr>
		<td>HyphenationCompoundWordTokenFilter</td>
		<td>good if grammar file is good – acceptable otherwise</td>
		<td>fast</td>
	</tr>
	<tr>
		<td>DictionaryCompoundWordTokenFilter</td>
		<td>good</td>
		<td>slow</td>
	</tr>
</table>

### Examples

      public void testHyphenationCompoundWordsDE() throws Exception {
        String[] dict = { "Rind", "Fleisch", "Draht", "Schere", "Gesetz",
            "Aufgabe", "Überwachung" };

        Reader reader = new FileReader("de_DR.xml");

        HyphenationTree hyphenator = HyphenationCompoundWordTokenFilter
            .getHyphenationTree(reader);

        HyphenationCompoundWordTokenFilter tf = new HyphenationCompoundWordTokenFilter(
            new WhitespaceTokenizer(new StringReader(
                "Rindfleischüberwachungsgesetz Drahtschere abba")), hyphenator,
            dict, CompoundWordTokenFilterBase.DEFAULT_MIN_WORD_SIZE,
            CompoundWordTokenFilterBase.DEFAULT_MIN_SUBWORD_SIZE,
            CompoundWordTokenFilterBase.DEFAULT_MAX_SUBWORD_SIZE, false);

        CharTermAttribute t = tf.addAttribute(CharTermAttribute.class);
        while (tf.incrementToken()) {
           System.out.println(t);
        }
      }

      public void testHyphenationCompoundWordsWithoutDictionaryDE() throws Exception {
        Reader reader = new FileReader("de_DR.xml");

        HyphenationTree hyphenator = HyphenationCompoundWordTokenFilter
            .getHyphenationTree(reader);

        HyphenationCompoundWordTokenFilter tf = new HyphenationCompoundWordTokenFilter(
            new WhitespaceTokenizer(new StringReader(
                "Rindfleischüberwachungsgesetz Drahtschere abba")), hyphenator);

        CharTermAttribute t = tf.addAttribute(CharTermAttribute.class);
        while (tf.incrementToken()) {
           System.out.println(t);
        }
      }

      public void testDumbCompoundWordsSE() throws Exception {
        String[] dict = { "Bil", "Dörr", "Motor", "Tak", "Borr", "Slag", "Hammar",
            "Pelar", "Glas", "Ögon", "Fodral", "Bas", "Fiol", "Makare", "Gesäll",
            "Sko", "Vind", "Rute", "Torkare", "Blad" };

        DictionaryCompoundWordTokenFilter tf = new DictionaryCompoundWordTokenFilter(
            new WhitespaceTokenizer(
                new StringReader(
                    "Bildörr Bilmotor Biltak Slagborr Hammarborr Pelarborr Glasögonfodral Basfiolsfodral Basfiolsfodralmakaregesäll Skomakare Vindrutetorkare Vindrutetorkarblad abba")),
            dict);
        CharTermAttribute t = tf.addAttribute(CharTermAttribute.class);
        while (tf.incrementToken()) {
           System.out.println(t);
        }
      }