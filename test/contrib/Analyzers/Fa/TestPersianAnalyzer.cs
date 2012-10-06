/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Fa;
using Lucene.Net.Test.Analysis;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analyzers.Fa
{
/*
 * Test the Persian Analyzer
 * 
 */
public class TestPersianAnalyzer : BaseTokenStreamTestCase {

  /*
   * This test fails with NPE when the stopwords file is missing in classpath
   */
  public void testResourcesAvailable() {
    new PersianAnalyzer(Version.LUCENE_CURRENT);
  }

  /*
   * This test shows how the combination of tokenization (breaking on zero-width
   * non-joiner), normalization (such as treating arabic YEH and farsi YEH the
   * same), and stopwords creates a light-stemming effect for verbs.
   * 
   * These verb forms are from http://en.wikipedia.org/wiki/Persian_grammar
   */
  public void testBehaviorVerbs(){
    Analyzer a = new PersianAnalyzer(Version.LUCENE_CURRENT);
    // active present indicative
    AssertAnalyzesTo(a, "می‌خورد", new String[] { "خورد" });
    // active preterite indicative
    AssertAnalyzesTo(a, "خورد", new String[] { "خورد" });
    // active imperfective preterite indicative
    AssertAnalyzesTo(a, "می‌خورد", new String[] { "خورد" });
    // active future indicative
    AssertAnalyzesTo(a, "خواهد خورد", new String[] { "خورد" });
    // active present progressive indicative
    AssertAnalyzesTo(a, "دارد می‌خورد", new String[] { "خورد" });
    // active preterite progressive indicative
    AssertAnalyzesTo(a, "داشت می‌خورد", new String[] { "خورد" });

    // active perfect indicative
    AssertAnalyzesTo(a, "خورده‌است", new String[] { "خورده" });
    // active imperfective perfect indicative
    AssertAnalyzesTo(a, "می‌خورده‌است", new String[] { "خورده" });
    // active pluperfect indicative
    AssertAnalyzesTo(a, "خورده بود", new String[] { "خورده" });
    // active imperfective pluperfect indicative
    AssertAnalyzesTo(a, "می‌خورده بود", new String[] { "خورده" });
    // active preterite subjunctive
    AssertAnalyzesTo(a, "خورده باشد", new String[] { "خورده" });
    // active imperfective preterite subjunctive
    AssertAnalyzesTo(a, "می‌خورده باشد", new String[] { "خورده" });
    // active pluperfect subjunctive
    AssertAnalyzesTo(a, "خورده بوده باشد", new String[] { "خورده" });
    // active imperfective pluperfect subjunctive
    AssertAnalyzesTo(a, "می‌خورده بوده باشد", new String[] { "خورده" });
    // passive present indicative
    AssertAnalyzesTo(a, "خورده می‌شود", new String[] { "خورده" });
    // passive preterite indicative
    AssertAnalyzesTo(a, "خورده شد", new String[] { "خورده" });
    // passive imperfective preterite indicative
    AssertAnalyzesTo(a, "خورده می‌شد", new String[] { "خورده" });
    // passive perfect indicative
    AssertAnalyzesTo(a, "خورده شده‌است", new String[] { "خورده" });
    // passive imperfective perfect indicative
    AssertAnalyzesTo(a, "خورده می‌شده‌است", new String[] { "خورده" });
    // passive pluperfect indicative
    AssertAnalyzesTo(a, "خورده شده بود", new String[] { "خورده" });
    // passive imperfective pluperfect indicative
    AssertAnalyzesTo(a, "خورده می‌شده بود", new String[] { "خورده" });
    // passive future indicative
    AssertAnalyzesTo(a, "خورده خواهد شد", new String[] { "خورده" });
    // passive present progressive indicative
    AssertAnalyzesTo(a, "دارد خورده می‌شود", new String[] { "خورده" });
    // passive preterite progressive indicative
    AssertAnalyzesTo(a, "داشت خورده می‌شد", new String[] { "خورده" });
    // passive present subjunctive
    AssertAnalyzesTo(a, "خورده شود", new String[] { "خورده" });
    // passive preterite subjunctive
    AssertAnalyzesTo(a, "خورده شده باشد", new String[] { "خورده" });
    // passive imperfective preterite subjunctive
    AssertAnalyzesTo(a, "خورده می‌شده باشد", new String[] { "خورده" });
    // passive pluperfect subjunctive
    AssertAnalyzesTo(a, "خورده شده بوده باشد", new String[] { "خورده" });
    // passive imperfective pluperfect subjunctive
    AssertAnalyzesTo(a, "خورده می‌شده بوده باشد", new String[] { "خورده" });

    // active present subjunctive
    AssertAnalyzesTo(a, "بخورد", new String[] { "بخورد" });
  }

  /*
   * This test shows how the combination of tokenization and stopwords creates a
   * light-stemming effect for verbs.
   * 
   * In this case, these forms are presented with alternative orthography, using
   * arabic yeh and whitespace. This yeh phenomenon is common for legacy text
   * due to some previous bugs in Microsoft Windows.
   * 
   * These verb forms are from http://en.wikipedia.org/wiki/Persian_grammar
   */
  public void testBehaviorVerbsDefective(){
    Analyzer a = new PersianAnalyzer(Version.LUCENE_CURRENT);
    // active present indicative
    AssertAnalyzesTo(a, "مي خورد", new String[] { "خورد" });
    // active preterite indicative
    AssertAnalyzesTo(a, "خورد", new String[] { "خورد" });
    // active imperfective preterite indicative
    AssertAnalyzesTo(a, "مي خورد", new String[] { "خورد" });
    // active future indicative
    AssertAnalyzesTo(a, "خواهد خورد", new String[] { "خورد" });
    // active present progressive indicative
    AssertAnalyzesTo(a, "دارد مي خورد", new String[] { "خورد" });
    // active preterite progressive indicative
    AssertAnalyzesTo(a, "داشت مي خورد", new String[] { "خورد" });

    // active perfect indicative
    AssertAnalyzesTo(a, "خورده است", new String[] { "خورده" });
    // active imperfective perfect indicative
    AssertAnalyzesTo(a, "مي خورده است", new String[] { "خورده" });
    // active pluperfect indicative
    AssertAnalyzesTo(a, "خورده بود", new String[] { "خورده" });
    // active imperfective pluperfect indicative
    AssertAnalyzesTo(a, "مي خورده بود", new String[] { "خورده" });
    // active preterite subjunctive
    AssertAnalyzesTo(a, "خورده باشد", new String[] { "خورده" });
    // active imperfective preterite subjunctive
    AssertAnalyzesTo(a, "مي خورده باشد", new String[] { "خورده" });
    // active pluperfect subjunctive
    AssertAnalyzesTo(a, "خورده بوده باشد", new String[] { "خورده" });
    // active imperfective pluperfect subjunctive
    AssertAnalyzesTo(a, "مي خورده بوده باشد", new String[] { "خورده" });
    // passive present indicative
    AssertAnalyzesTo(a, "خورده مي شود", new String[] { "خورده" });
    // passive preterite indicative
    AssertAnalyzesTo(a, "خورده شد", new String[] { "خورده" });
    // passive imperfective preterite indicative
    AssertAnalyzesTo(a, "خورده مي شد", new String[] { "خورده" });
    // passive perfect indicative
    AssertAnalyzesTo(a, "خورده شده است", new String[] { "خورده" });
    // passive imperfective perfect indicative
    AssertAnalyzesTo(a, "خورده مي شده است", new String[] { "خورده" });
    // passive pluperfect indicative
    AssertAnalyzesTo(a, "خورده شده بود", new String[] { "خورده" });
    // passive imperfective pluperfect indicative
    AssertAnalyzesTo(a, "خورده مي شده بود", new String[] { "خورده" });
    // passive future indicative
    AssertAnalyzesTo(a, "خورده خواهد شد", new String[] { "خورده" });
    // passive present progressive indicative
    AssertAnalyzesTo(a, "دارد خورده مي شود", new String[] { "خورده" });
    // passive preterite progressive indicative
    AssertAnalyzesTo(a, "داشت خورده مي شد", new String[] { "خورده" });
    // passive present subjunctive
    AssertAnalyzesTo(a, "خورده شود", new String[] { "خورده" });
    // passive preterite subjunctive
    AssertAnalyzesTo(a, "خورده شده باشد", new String[] { "خورده" });
    // passive imperfective preterite subjunctive
    AssertAnalyzesTo(a, "خورده مي شده باشد", new String[] { "خورده" });
    // passive pluperfect subjunctive
    AssertAnalyzesTo(a, "خورده شده بوده باشد", new String[] { "خورده" });
    // passive imperfective pluperfect subjunctive
    AssertAnalyzesTo(a, "خورده مي شده بوده باشد", new String[] { "خورده" });

    // active present subjunctive
    AssertAnalyzesTo(a, "بخورد", new String[] { "بخورد" });
  }

  /*
   * This test shows how the combination of tokenization (breaking on zero-width
   * non-joiner or space) and stopwords creates a light-stemming effect for
   * nouns, removing the plural -ha.
   */
  public void testBehaviorNouns(){
    Analyzer a = new PersianAnalyzer(Version.LUCENE_CURRENT);
    AssertAnalyzesTo(a, "برگ ها", new String[] { "برگ" });
    AssertAnalyzesTo(a, "برگ‌ها", new String[] { "برگ" });
  }

  /*
   * Test showing that non-persian text is treated very much like SimpleAnalyzer
   * (lowercased, etc)
   */
  public void testBehaviorNonPersian(){
    Analyzer a = new PersianAnalyzer(Version.LUCENE_CURRENT);
    AssertAnalyzesTo(a, "English test.", new String[] { "english", "test" });
  }
  
  /*
   * Basic test ensuring that reusableTokenStream works correctly.
   */
  public void testReusableTokenStream(){
    Analyzer a = new PersianAnalyzer(Version.LUCENE_CURRENT);
    AssertAnalyzesToReuse(a, "خورده مي شده بوده باشد", new String[] { "خورده" });
    AssertAnalyzesToReuse(a, "برگ‌ها", new String[] { "برگ" });
  }
  
  /*
   * Test that custom stopwords work, and are not case-sensitive.
   */
  public void testCustomStopwords(){
    PersianAnalyzer a = new PersianAnalyzer(Version.LUCENE_CURRENT, new String[] { "the", "and", "a" });
    AssertAnalyzesTo(a, "The quick brown fox.", new String[] { "quick",
        "brown", "fox" });
  }

}

}
