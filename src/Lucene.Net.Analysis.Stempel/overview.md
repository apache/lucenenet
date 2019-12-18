---
uid: Lucene.Net.Analysis.Stempel
summary: *content
---

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

# _Stempel_ - Algorithmic Stemmer for Polish Language

## Introduction

A method for conflation of different inflected word forms is an important component of many Information Retrieval systems. It helps to improve the system's recall and can significantly reduce the index size. This is especially true for highly-inflectional languages like those from the Slavic language family (Czech, Slovak, Polish, Russian, Bulgarian, etc).

This page describes a software package consisting of high-quality stemming tables for Polish, and a universal algorithmic stemmer, which operates using these tables. The stemmer code is taken virtually unchanged from the [Egothor project](http://www.egothor.org).

The software distribution includes stemmer tables prepared using an extensive corpus of Polish language (see details below).

This work is available under Apache-style Open Source license - the stemmer code is covered by Egothor License, the tables and other additions are covered by Apache License 2.0. Both licenses allow to use the code in Open Source as well as commercial (closed source) projects.

### Terminology

A short explanation is in order about the terminology used in this text.

In the following sections I make a distinction between __stem__ and __lemma__.

Lemma is a base grammatical form (dictionary form, headword) of a word. Lemma is an existing, grammatically correct word in some human language.

Stem on the other hand is just a unique token, not necessarily making any sense in any human language, but which can serve as a unique label instead of lemma for the same set of inflected forms. Quite often stem is referred to as a "root" of the word - which is incorrect and misleading (stems sometimes have very little to do with the linguistic root of a word, i.e. a pattern found in a word which is common to all inflected forms or within a family of languages).

For an IR system stems are usually sufficient, for a morphological analysis system obviously lemmas are a must. In practice, various stemmers produce a mix of stems and lemmas, as is the case with the stemmer described here. Additionally, for some languages, which use suffix-based inflection rules many stemmers based on suffix-stripping will produce a large percentage of stems equivalent to lemmas. This is however not the case for languages with complex, irregular inflection rules (such as Slavic languages) - here simplistic suffix-stripping stemmers produce very poor results.

### Background

Lemmatization is a process of finding the base, non-inflected form of a word. The result of lemmatization is a correct existing word, often in nominative case for nouns and infinitive form for verbs. A given inflected form may correspond to several lemmas (e.g. "found" -> find, found) - the correct choice depends on the context.  

 Stemming is concerned mostly with finding a unique "root" of a word, which not necessarily results in any existing word or lemma. The quality of stemming is measured by the rate of collisions (overstemming - which causes words with different lemmas to be incorrectly conflated into one "root"), and the rate of superfluous word "roots" (understemming - which assigns several "roots" to words with the same lemma).   

 Both stemmer and lemmatizer can be implemented in various ways. The two most common approaches are:  

*   dictionary-based: where the stemmer uses an extensive dictionary
of morphological forms in order to find the corresponding stem or lemma

*   algorithmic: where the stemmer uses an algorithm, based on
general morphological properties of a given language plus a set of
heuristic rules  

There are many existing and well-known implementations of stemmers for
English (Porter, Lovins, Krovetz) and other European languages
([Snowball](http://snowball.tartarus.org)). There are also
good quality commercial lemmatizers for Polish. However, there is only
one
freely available Polish stemmer, implemented by
[Dawid
Weiss](http://www.cs.put.poznan.pl/dweiss/xml/projects/lametyzator/index.xml?lang=en), based on the "ispell" dictionary and Jan Daciuk's [FSA package](http://www.eti.pg.gda.pl/%7Ejandac/). That
stemmer is dictionary-based. This means that even
though it can achieve
perfect accuracy for previously known word forms found in its
dictionary, it
completely fails in case of all other word forms. This deficiency is
somewhat mitigated by the comprehensive dictionary distributed with
this stemmer (so there is a high probability that most of the words in
the input text will be found in the dictionary), however the problem
still remains (please see the page above for more detailed description).  

The implementation described here uses an algorithmic method. This
method
and particular algorithm implementation are described in detail in
[1][2].
The main advantage of algorithmic stemmers is their ability to process
previously
unseen word forms with high accuracy. This particular algorithm uses a
set
of
transformation rules (patch commands), which describe how a word with a
given pattern should be transformed to its stem. These rules are first
learned from a training corpus. They don't
cover
all possible cases, so there is always some loss of precision/recall
(which
means that even the words from the training corpus are sometimes
incorrectly stemmed).  

## Algorithm and implementation<span style="font-style: italic;"></span>

The algorithm and its Java implementation is described in detail in the
publications cited below. Here's just a short excerpt from [2]:  

<center>
<div style="width: 80%;" align="justify">"The aim is separation of the
stemmer execution code from the data
structures [...]. In other words, a static algorithm configurable by
data must be developed. The word transformations that happen in the
stemmer must be then encoded to the data tables.  

The tacit input of our method is a sample set (a so-called dictionary)
of words (as keys) and their stems. Each record can be equivalently
stored as a key and the record of key's transformation to its
respective stem. The transformation record is termed a patch command
(P-command). It must be ensured that P-commands are universal, and that
P-commands can transform any word to its stem. Our solution[6,8] is
based on the Levenstein metric [10], which produces P-command as the
minimum cost path in a directed graph.  

One can imagine the P-command as an algorithm for an operator (editor)
that rewrites a string to another string. The operator can use these
instructions (PP-command's): <span style="font-weight: bold;">removal </span>-
deletes a sequence of characters starting at the current cursor
position and moves the cursor to the next character. The length of this
sequence is the parameter; <span style="font-weight: bold;">insertion </span>-
inserts a character ch, without moving the cursor. The character ch is
a parameter; <span style="font-weight: bold;">substitution </span>
- rewrites a character at the current cursor position to the character
ch and moves the cursor to the next character. The character ch is a
parameter; <span style="font-weight: bold;">no operation</span> (NOOP)
- skip a sequence of characters starting at the current cursor
position. The length of this sequence is the parameter.  

The P-commands are applied from the end of a word (right to left). This
assumption can reduce the set of P-command's, because the last NOOP,
moving the cursor to the end of a string without any changes, need not
be stored."</div>
</center>

Data structure used to keep the dictionary (words and their P-commands)
is a trie. Several optimization steps are applied in turn to reduce and
optimize the initial trie, by eliminating useless information and
shortening the paths in the trie.  

Finally, in order to obtain a stem from the input word, the word is
passed once through a matching path in the trie (applying at each node
the P-commands stored there). The result is a word stem.  

## Corpus

_(to be completed...)_

The following Polish corpora have been used:

*   [Polish
dictionary
from ispell distribution](http://sourceforge.net/project/showfiles.php?group_id=49316&package_id=65354)

*   [Wzbogacony korpus
sÅ‚ownika frekwencyjnego](http://www.mimuw.edu.pl/polszczyzna/)

*   The
Bible (so called "TysiÄ…clecia") - unauthorized electronic version

*   [Analizator
morfologiczny SAM v. 3.4](http://www.mimuw.edu.pl/polszczyzna/Debian/sam34_3.4a.02-1_i386.deb) - this was used to recover lemmas
missing from other texts

This step was the most time-consuming - and it would probably be even more tedious and difficult if not for the help of [Python](http://www.python.org/). The source texts had to be brought to a common encoding (UTF-8) - some of them used quite ancient encodings like Mazovia or DHN - and then scripts were written to collect all lemmas and inflected forms from the source texts. In cases when the source text was not tagged, I used the SAM analyzer to produce lemmas. In cases of ambiguous lemmatization I decided to put references to inflected forms from all base forms.  

All grammatical categories were allowed to appear in the corpus, i.e. nouns, verbs, adjectives, numerals, and pronouns. The resulting corpus consisted of roughly 87,000+ inflection sets, i.e. each set consisted of one base form (lemma) and many inflected forms. However, because of the nature of the training method I restricted these sets to include only those where there were at least 4 inflected forms. Sets with 3 or less inflected forms were removed, so that the final corpus consisted of ~69,000 unique sets, which in turn contained ~1.5 mln inflected forms.   

## Testing

I tested the stemmer tables produced using the implementation described above. The following sections give some details about the testing setup. 

### Testing procedure

The testing procedure was as follows: 

*   the whole corpus of ~69,000 unique sets was shuffled, so that the
input sets were in random order.

*   the corpus was split into two parts - one with 30,000 sets (Part
1), the other with ~39,000 sets (Part 2).

*   Training samples were drawn in sequential order from the Part 1.
Since the sets were already randomized, the training samples were also
randomized, but this procedure ensured that each larger training sample
contained all smaller samples.

*   Part 2 was used for testing. Note: this means that the testing
run used _only_ words previously unseen during the training
phase. This is the worst scenario, because it means that stemmer must
extrapolate the learned rules to unknown cases. This also means that in
a real-life case (where the input is a mix between known and unknown
words) the F-measure of the stemmer will be even higher than in the
table below.

### Test results

The following table summarizes test results for varying sizes of training samples. The meaning of the table columns is described below: 

*   __training sets:__ the number of training sets. One set
consists of one lemma and at least 4 and up to ~80 inflected forms
(including pre- and suffixed forms).

*   __testing forms:__ the number of testing forms. Only inflected
forms were used in testing.

*   __stem OK:__ the number of cases when produced output was a
correct (unique) stem. Note: quite often correct stems were also
correct lemmas.

*   __lemma OK:__ the number of cases when produced output was a
correct lemma.

*   __missing:__ the number of cases when stemmer was unable to
provide any output.

*   __stem bad:__ the number of cases when produced output was a
stem, but already in use identifying a different set.

*   __lemma bad:__ the number of cases when produced output was an
incorrect lemma. Note: quite often in such case the output was a
correct stem.

*   __table size:__ the size in bytes of the stemmer table.

<div align="center">
<table border="1" cellpadding="2" cellspacing="0">
  <tbody>
    <tr bgcolor="#a0b0c0">
      <th>Training sets</th>
      <th>Testing forms</th>
      <th>Stem OK</th>
      <th>Lemma OK</th>
      <th>Missing</th>
      <th>Stem Bad</th>
      <th>Lemma Bad</th>
      <th>Table size [B]</th>
    </tr>
    <tr align="right">
      <td>100</td>
      <td>1022985</td>
      <td>842209</td>
      <td>593632</td>
      <td>172711</td>
      <td>22331</td>
      <td>256642</td>
      <td>28438</td>
    </tr>
    <tr align="right">
      <td>200</td>
      <td>1022985</td>
      <td>862789</td>
      <td>646488</td>
      <td>153288</td>
      <td>16306</td>
      <td>223209</td>
      <td>48660</td>
    </tr>
    <tr align="right">
      <td>500</td>
      <td>1022985</td>
      <td>885786</td>
      <td>685009</td>
      <td>130772</td>
      <td>14856</td>
      <td>207204</td>
      <td>108798</td>
    </tr>
    <tr align="right">
      <td>700</td>
      <td>1022985</td>
      <td>909031</td>
      <td>704609</td>
      <td>107084</td>
      <td>15442</td>
      <td>211292</td>
      <td>139291</td>
    </tr>
    <tr align="right">
      <td>1000</td>
      <td>1022985</td>
      <td>926079</td>
      <td>725720</td>
      <td>90117</td>
      <td>14941</td>
      <td>207148</td>
      <td>183677</td>
    </tr>
    <tr align="right">
      <td>2000</td>
      <td>1022985</td>
      <td>942886</td>
      <td>746641</td>
      <td>73429</td>
      <td>14903</td>
      <td>202915</td>
      <td>313516</td>
    </tr>
    <tr align="right">
      <td>5000</td>
      <td>1022985</td>
      <td>954721</td>
      <td>759930</td>
      <td>61476</td>
      <td>14817</td>
      <td>201579</td>
      <td>640969</td>
    </tr>
    <tr align="right">
      <td>7000</td>
      <td>1022985</td>
      <td>956165</td>
      <td>764033</td>
      <td>60364</td>
      <td>14620</td>
      <td>198588</td>
      <td>839347</td>
    </tr>
    <tr align="right">
      <td>10000</td>
      <td>1022985</td>
      <td>965427</td>
      <td>775507</td>
      <td>50797</td>
      <td>14662</td>
      <td>196681</td>
      <td>1144537</td>
    </tr>
    <tr align="right">
      <td>12000</td>
      <td>1022985</td>
      <td>967664</td>
      <td>782143</td>
      <td>48722</td>
      <td>14284</td>
      <td>192120</td>
      <td>1313508</td>
    </tr>
    <tr align="right">
      <td>15000</td>
      <td>1022985</td>
      <td>973188</td>
      <td>788867</td>
      <td>43247</td>
      <td>14349</td>
      <td>190871</td>
      <td>1567902</td>
    </tr>
    <tr align="right">
      <td>17000</td>
      <td>1022985</td>
      <td>974203</td>
      <td>791804</td>
      <td>42319</td>
      <td>14333</td>
      <td>188862</td>
      <td>1733957</td>
    </tr>
    <tr align="right">
      <td>20000</td>
      <td>1022985</td>
      <td>976234</td>
      <td>791554</td>
      <td>40058</td>
      <td>14601</td>
      <td>191373</td>
      <td>1977615</td>
    </tr>
  </tbody>
</table>
</div>

I also measured the time to produce a stem (which involves traversing a trie, retrieving a patch command and applying the patch command to the input string). On a machine running Windows XP (Pentium 4, 1.7 GHz, JDK 1.4.2_03 HotSpot), for tables ranging in size from 1,000 to 20,000 cells, the time to produce a single stem varies between 5-10 microseconds.  

This means that the stemmer can process up to <span style="font-weight: bold;">200,000 words per second</span>, an outstanding result when compared to other stemmers (Morfeusz - ~2,000 w/s, FormAN (MS Word analyzer) - ~1,000 w/s).  

The package contains a class `org.getopt.stempel.Benchmark`, which you can use to produce reports like the one below:  

--------- Stemmer benchmark report: -----------  
Stemmer table:  /res/tables/stemmer_2000.out  
Input file:     ../test3.txt  
Number of runs: 3  

 RUN NUMBER:            1       2       3  
 Total input words      1378176 1378176 1378176  
 Missed output words    112     112     112  
 Time elapsed [ms]      6989    6940    6640  
 Hit rate percent       99.99%  99.99%  99.99%  
 Miss rate percent      00.01%  00.01%  00.01%  
 Words per second       197192  198584  207557  
 Time per word [us]     5.07    5.04    4.82  

## Summary

The results of these tests are very encouraging. It seems that using the training corpus and the stemming algorithm described above results in a high-quality stemmer useful for most applications. Moreover, it can also be used as a better than average lemmatizer.

Both the author of the implementation (Leo Galambos, <leo.galambos AT egothor DOT org>) and the author of this compilation (Andrzej Bialecki <ab AT getopt DOT org>) would appreciate any feedback and suggestions for further improvements.

## Bibliography

1.  Galambos, L.: Multilingual Stemmer in Web Environment, PhD
Thesis,
Faculty of Mathematics and Physics, Charles University in Prague, in
press.

2.  Galambos, L.: Semi-automatic Stemmer Evaluation. International
Intelligent Information Processing and Web Mining Conference, 2004,
Zakopane, Poland.

3.  Galambos, L.: Lemmatizer for Document Information Retrieval
Systems in JAVA.<span style="text-decoration: underline;"> </span>[<http://www.informatik.uni-trier.de/%7Eley/db/conf/sofsem/sofsem2001.html#Galambos01>](http://www.informatik.uni-trier.de/%7Eley/db/conf/sofsem/sofsem2001.html#Galambos01)
SOFSEM 2001, Piestany, Slovakia.   