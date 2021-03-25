// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using NUnit.Framework;

namespace Lucene.Net.Analysis.El
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

    public class TestGreekStemmer : BaseTokenStreamTestCase
    {
        internal static readonly Analyzer a = new GreekAnalyzer(TEST_VERSION_CURRENT);

        [Test]
        public virtual void TestMasculineNouns()
        {
            // -ος
            CheckOneTerm(a, "άνθρωπος", "ανθρωπ");
            CheckOneTerm(a, "ανθρώπου", "ανθρωπ");
            CheckOneTerm(a, "άνθρωπο", "ανθρωπ");
            CheckOneTerm(a, "άνθρωπε", "ανθρωπ");
            CheckOneTerm(a, "άνθρωποι", "ανθρωπ");
            CheckOneTerm(a, "ανθρώπων", "ανθρωπ");
            CheckOneTerm(a, "ανθρώπους", "ανθρωπ");
            CheckOneTerm(a, "άνθρωποι", "ανθρωπ");

            // -ης
            CheckOneTerm(a, "πελάτης", "πελατ");
            CheckOneTerm(a, "πελάτη", "πελατ");
            CheckOneTerm(a, "πελάτες", "πελατ");
            CheckOneTerm(a, "πελατών", "πελατ");

            // -ας/-ες
            CheckOneTerm(a, "ελέφαντας", "ελεφαντ");
            CheckOneTerm(a, "ελέφαντα", "ελεφαντ");
            CheckOneTerm(a, "ελέφαντες", "ελεφαντ");
            CheckOneTerm(a, "ελεφάντων", "ελεφαντ");

            // -ας/-αδες
            CheckOneTerm(a, "μπαμπάς", "μπαμπ");
            CheckOneTerm(a, "μπαμπά", "μπαμπ");
            CheckOneTerm(a, "μπαμπάδες", "μπαμπ");
            CheckOneTerm(a, "μπαμπάδων", "μπαμπ");

            // -ης/-ηδες
            CheckOneTerm(a, "μπακάλης", "μπακαλ");
            CheckOneTerm(a, "μπακάλη", "μπακαλ");
            CheckOneTerm(a, "μπακάληδες", "μπακαλ");
            CheckOneTerm(a, "μπακάληδων", "μπακαλ");

            // -ες
            CheckOneTerm(a, "καφές", "καφ");
            CheckOneTerm(a, "καφέ", "καφ");
            CheckOneTerm(a, "καφέδες", "καφ");
            CheckOneTerm(a, "καφέδων", "καφ");

            // -έας/είς
            CheckOneTerm(a, "γραμματέας", "γραμματε");
            CheckOneTerm(a, "γραμματέα", "γραμματε");
            // plural forms conflate w/ each other, not w/ the sing forms
            CheckOneTerm(a, "γραμματείς", "γραμματ");
            CheckOneTerm(a, "γραμματέων", "γραμματ");

            // -ους/οι
            CheckOneTerm(a, "απόπλους", "αποπλ");
            CheckOneTerm(a, "απόπλου", "αποπλ");
            CheckOneTerm(a, "απόπλοι", "αποπλ");
            CheckOneTerm(a, "απόπλων", "αποπλ");

            // -ους/-ουδες
            CheckOneTerm(a, "παππούς", "παππ");
            CheckOneTerm(a, "παππού", "παππ");
            CheckOneTerm(a, "παππούδες", "παππ");
            CheckOneTerm(a, "παππούδων", "παππ");

            // -ης/-εις
            CheckOneTerm(a, "λάτρης", "λατρ");
            CheckOneTerm(a, "λάτρη", "λατρ");
            CheckOneTerm(a, "λάτρεις", "λατρ");
            CheckOneTerm(a, "λάτρεων", "λατρ");

            // -υς
            CheckOneTerm(a, "πέλεκυς", "πελεκ");
            CheckOneTerm(a, "πέλεκυ", "πελεκ");
            CheckOneTerm(a, "πελέκεις", "πελεκ");
            CheckOneTerm(a, "πελέκεων", "πελεκ");

            // -ωρ
            // note: nom./voc. doesn't conflate w/ the rest
            CheckOneTerm(a, "μέντωρ", "μεντωρ");
            CheckOneTerm(a, "μέντορος", "μεντορ");
            CheckOneTerm(a, "μέντορα", "μεντορ");
            CheckOneTerm(a, "μέντορες", "μεντορ");
            CheckOneTerm(a, "μεντόρων", "μεντορ");

            // -ων
            CheckOneTerm(a, "αγώνας", "αγων");
            CheckOneTerm(a, "αγώνος", "αγων");
            CheckOneTerm(a, "αγώνα", "αγων");
            CheckOneTerm(a, "αγώνα", "αγων");
            CheckOneTerm(a, "αγώνες", "αγων");
            CheckOneTerm(a, "αγώνων", "αγων");

            // -ας/-ηδες
            CheckOneTerm(a, "αέρας", "αερ");
            CheckOneTerm(a, "αέρα", "αερ");
            CheckOneTerm(a, "αέρηδες", "αερ");
            CheckOneTerm(a, "αέρηδων", "αερ");

            // -ης/-ητες
            CheckOneTerm(a, "γόης", "γο");
            CheckOneTerm(a, "γόη", "γοη"); // too short
                                           // the two plural forms conflate
            CheckOneTerm(a, "γόητες", "γοητ");
            CheckOneTerm(a, "γοήτων", "γοητ");
        }

        [Test]
        public virtual void TestFeminineNouns()
        {
            // -α/-ες,-ών
            CheckOneTerm(a, "φορά", "φορ");
            CheckOneTerm(a, "φοράς", "φορ");
            CheckOneTerm(a, "φορές", "φορ");
            CheckOneTerm(a, "φορών", "φορ");

            // -α/-ες,-ων
            CheckOneTerm(a, "αγελάδα", "αγελαδ");
            CheckOneTerm(a, "αγελάδας", "αγελαδ");
            CheckOneTerm(a, "αγελάδες", "αγελαδ");
            CheckOneTerm(a, "αγελάδων", "αγελαδ");

            // -η/-ες
            CheckOneTerm(a, "ζάχαρη", "ζαχαρ");
            CheckOneTerm(a, "ζάχαρης", "ζαχαρ");
            CheckOneTerm(a, "ζάχαρες", "ζαχαρ");
            CheckOneTerm(a, "ζαχάρεων", "ζαχαρ");

            // -η/-εις
            CheckOneTerm(a, "τηλεόραση", "τηλεορασ");
            CheckOneTerm(a, "τηλεόρασης", "τηλεορασ");
            CheckOneTerm(a, "τηλεοράσεις", "τηλεορασ");
            CheckOneTerm(a, "τηλεοράσεων", "τηλεορασ");

            // -α/-αδες
            CheckOneTerm(a, "μαμά", "μαμ");
            CheckOneTerm(a, "μαμάς", "μαμ");
            CheckOneTerm(a, "μαμάδες", "μαμ");
            CheckOneTerm(a, "μαμάδων", "μαμ");

            // -ος
            CheckOneTerm(a, "λεωφόρος", "λεωφορ");
            CheckOneTerm(a, "λεωφόρου", "λεωφορ");
            CheckOneTerm(a, "λεωφόρο", "λεωφορ");
            CheckOneTerm(a, "λεωφόρε", "λεωφορ");
            CheckOneTerm(a, "λεωφόροι", "λεωφορ");
            CheckOneTerm(a, "λεωφόρων", "λεωφορ");
            CheckOneTerm(a, "λεωφόρους", "λεωφορ");

            // -ου
            CheckOneTerm(a, "αλεπού", "αλεπ");
            CheckOneTerm(a, "αλεπούς", "αλεπ");
            CheckOneTerm(a, "αλεπούδες", "αλεπ");
            CheckOneTerm(a, "αλεπούδων", "αλεπ");

            // -έας/είς
            // note: not all forms conflate
            CheckOneTerm(a, "γραμματέας", "γραμματε");
            CheckOneTerm(a, "γραμματέως", "γραμματ");
            CheckOneTerm(a, "γραμματέα", "γραμματε");
            CheckOneTerm(a, "γραμματείς", "γραμματ");
            CheckOneTerm(a, "γραμματέων", "γραμματ");
        }

        [Test]
        public virtual void TestNeuterNouns()
        {
            // ending with -ο
            // note: nom doesnt conflate
            CheckOneTerm(a, "βιβλίο", "βιβλι");
            CheckOneTerm(a, "βιβλίου", "βιβλ");
            CheckOneTerm(a, "βιβλία", "βιβλ");
            CheckOneTerm(a, "βιβλίων", "βιβλ");

            // ending with -ι
            CheckOneTerm(a, "πουλί", "πουλ");
            CheckOneTerm(a, "πουλιού", "πουλ");
            CheckOneTerm(a, "πουλιά", "πουλ");
            CheckOneTerm(a, "πουλιών", "πουλ");

            // ending with -α
            // note: nom. doesnt conflate
            CheckOneTerm(a, "πρόβλημα", "προβλημ");
            CheckOneTerm(a, "προβλήματος", "προβλημα");
            CheckOneTerm(a, "προβλήματα", "προβλημα");
            CheckOneTerm(a, "προβλημάτων", "προβλημα");

            // ending with -ος/-ους
            CheckOneTerm(a, "πέλαγος", "πελαγ");
            CheckOneTerm(a, "πελάγους", "πελαγ");
            CheckOneTerm(a, "πελάγη", "πελαγ");
            CheckOneTerm(a, "πελάγων", "πελαγ");

            // ending with -ός/-ότος
            CheckOneTerm(a, "γεγονός", "γεγον");
            CheckOneTerm(a, "γεγονότος", "γεγον");
            CheckOneTerm(a, "γεγονότα", "γεγον");
            CheckOneTerm(a, "γεγονότων", "γεγον");

            // ending with -υ/-ιου
            CheckOneTerm(a, "βράδυ", "βραδ");
            CheckOneTerm(a, "βράδι", "βραδ");
            CheckOneTerm(a, "βραδιού", "βραδ");
            CheckOneTerm(a, "βράδια", "βραδ");
            CheckOneTerm(a, "βραδιών", "βραδ");

            // ending with -υ/-ατος
            // note: nom. doesnt conflate
            CheckOneTerm(a, "δόρυ", "δορ");
            CheckOneTerm(a, "δόρατος", "δορατ");
            CheckOneTerm(a, "δόρατα", "δορατ");
            CheckOneTerm(a, "δοράτων", "δορατ");

            // ending with -ας
            CheckOneTerm(a, "κρέας", "κρε");
            CheckOneTerm(a, "κρέατος", "κρε");
            CheckOneTerm(a, "κρέατα", "κρε");
            CheckOneTerm(a, "κρεάτων", "κρε");

            // ending with -ως
            CheckOneTerm(a, "λυκόφως", "λυκοφω");
            CheckOneTerm(a, "λυκόφωτος", "λυκοφω");
            CheckOneTerm(a, "λυκόφωτα", "λυκοφω");
            CheckOneTerm(a, "λυκοφώτων", "λυκοφω");

            // ending with -ον/-ου
            // note: nom. doesnt conflate
            CheckOneTerm(a, "μέσον", "μεσον");
            CheckOneTerm(a, "μέσου", "μεσ");
            CheckOneTerm(a, "μέσα", "μεσ");
            CheckOneTerm(a, "μέσων", "μεσ");

            // ending in -ον/-οντος
            // note: nom. doesnt conflate
            CheckOneTerm(a, "ενδιαφέρον", "ενδιαφερον");
            CheckOneTerm(a, "ενδιαφέροντος", "ενδιαφεροντ");
            CheckOneTerm(a, "ενδιαφέροντα", "ενδιαφεροντ");
            CheckOneTerm(a, "ενδιαφερόντων", "ενδιαφεροντ");

            // ending with -εν/-εντος
            CheckOneTerm(a, "ανακοινωθέν", "ανακοινωθεν");
            CheckOneTerm(a, "ανακοινωθέντος", "ανακοινωθεντ");
            CheckOneTerm(a, "ανακοινωθέντα", "ανακοινωθεντ");
            CheckOneTerm(a, "ανακοινωθέντων", "ανακοινωθεντ");

            // ending with -αν/-αντος
            CheckOneTerm(a, "σύμπαν", "συμπ");
            CheckOneTerm(a, "σύμπαντος", "συμπαντ");
            CheckOneTerm(a, "σύμπαντα", "συμπαντ");
            CheckOneTerm(a, "συμπάντων", "συμπαντ");

            // ending with  -α/-ακτος
            CheckOneTerm(a, "γάλα", "γαλ");
            CheckOneTerm(a, "γάλακτος", "γαλακτ");
            CheckOneTerm(a, "γάλατα", "γαλατ");
            CheckOneTerm(a, "γαλάκτων", "γαλακτ");
        }

        [Test]
        public virtual void TestAdjectives()
        {
            // ending with -ής, -ές/-είς, -ή
            CheckOneTerm(a, "συνεχής", "συνεχ");
            CheckOneTerm(a, "συνεχούς", "συνεχ");
            CheckOneTerm(a, "συνεχή", "συνεχ");
            CheckOneTerm(a, "συνεχών", "συνεχ");
            CheckOneTerm(a, "συνεχείς", "συνεχ");
            CheckOneTerm(a, "συνεχές", "συνεχ");

            // ending with -ης, -ες/-εις, -η
            CheckOneTerm(a, "συνήθης", "συνηθ");
            CheckOneTerm(a, "συνήθους", "συνηθ");
            CheckOneTerm(a, "συνήθη", "συνηθ");
            // note: doesn't conflate
            CheckOneTerm(a, "συνήθεις", "συν");
            CheckOneTerm(a, "συνήθων", "συνηθ");
            CheckOneTerm(a, "σύνηθες", "συνηθ");

            // ending with -υς, -υ/-εις, -ια
            CheckOneTerm(a, "βαθύς", "βαθ");
            CheckOneTerm(a, "βαθέος", "βαθε");
            CheckOneTerm(a, "βαθύ", "βαθ");
            CheckOneTerm(a, "βαθείς", "βαθ");
            CheckOneTerm(a, "βαθέων", "βαθ");

            CheckOneTerm(a, "βαθιά", "βαθ");
            CheckOneTerm(a, "βαθιάς", "βαθι");
            CheckOneTerm(a, "βαθιές", "βαθι");
            CheckOneTerm(a, "βαθιών", "βαθ");

            CheckOneTerm(a, "βαθέα", "βαθε");

            // comparative/superlative
            CheckOneTerm(a, "ψηλός", "ψηλ");
            CheckOneTerm(a, "ψηλότερος", "ψηλ");
            CheckOneTerm(a, "ψηλότατος", "ψηλ");

            CheckOneTerm(a, "ωραίος", "ωραι");
            CheckOneTerm(a, "ωραιότερος", "ωραι");
            CheckOneTerm(a, "ωραιότατος", "ωραι");

            CheckOneTerm(a, "επιεικής", "επιεικ");
            CheckOneTerm(a, "επιεικέστερος", "επιεικ");
            CheckOneTerm(a, "επιεικέστατος", "επιεικ");
        }


        [Test]
        public virtual void TestVerbs()
        {
            // note, past/present verb stems will not conflate (from the paper)
            //-ω,-α/-.ω,-.α
            CheckOneTerm(a, "ορίζω", "οριζ");
            CheckOneTerm(a, "όριζα", "οριζ");
            CheckOneTerm(a, "όριζε", "οριζ");
            CheckOneTerm(a, "ορίζοντας", "οριζ");
            CheckOneTerm(a, "ορίζομαι", "οριζ");
            CheckOneTerm(a, "οριζόμουν", "οριζ");
            CheckOneTerm(a, "ορίζεσαι", "οριζ");

            CheckOneTerm(a, "όρισα", "ορισ");
            CheckOneTerm(a, "ορίσω", "ορισ");
            CheckOneTerm(a, "όρισε", "ορισ");
            CheckOneTerm(a, "ορίσει", "ορισ");

            CheckOneTerm(a, "ορίστηκα", "οριστ");
            CheckOneTerm(a, "οριστώ", "οριστ");
            CheckOneTerm(a, "οριστείς", "οριστ");
            CheckOneTerm(a, "οριστεί", "οριστ");

            CheckOneTerm(a, "ορισμένο", "ορισμεν");
            CheckOneTerm(a, "ορισμένη", "ορισμεν");
            CheckOneTerm(a, "ορισμένος", "ορισμεν");

            // -ω,-α/-ξω,-ξα
            CheckOneTerm(a, "ανοίγω", "ανοιγ");
            CheckOneTerm(a, "άνοιγα", "ανοιγ");
            CheckOneTerm(a, "άνοιγε", "ανοιγ");
            CheckOneTerm(a, "ανοίγοντας", "ανοιγ");
            CheckOneTerm(a, "ανοίγομαι", "ανοιγ");
            CheckOneTerm(a, "ανοιγόμουν", "ανοιγ");

            CheckOneTerm(a, "άνοιξα", "ανοιξ");
            CheckOneTerm(a, "ανοίξω", "ανοιξ");
            CheckOneTerm(a, "άνοιξε", "ανοιξ");
            CheckOneTerm(a, "ανοίξει", "ανοιξ");

            CheckOneTerm(a, "ανοίχτηκα", "ανοιχτ");
            CheckOneTerm(a, "ανοιχτώ", "ανοιχτ");
            CheckOneTerm(a, "ανοίχτηκα", "ανοιχτ");
            CheckOneTerm(a, "ανοιχτείς", "ανοιχτ");
            CheckOneTerm(a, "ανοιχτεί", "ανοιχτ");

            CheckOneTerm(a, "ανοίξου", "ανοιξ");

            //-ώ/-άω,-ούσα/-άσω,-ασα
            CheckOneTerm(a, "περνώ", "περν");
            CheckOneTerm(a, "περνάω", "περν");
            CheckOneTerm(a, "περνούσα", "περν");
            CheckOneTerm(a, "πέρναγα", "περν");
            CheckOneTerm(a, "πέρνα", "περν");
            CheckOneTerm(a, "περνώντας", "περν");

            CheckOneTerm(a, "πέρασα", "περασ");
            CheckOneTerm(a, "περάσω", "περασ");
            CheckOneTerm(a, "πέρασε", "περασ");
            CheckOneTerm(a, "περάσει", "περασ");

            CheckOneTerm(a, "περνιέμαι", "περν");
            CheckOneTerm(a, "περνιόμουν", "περν");

            CheckOneTerm(a, "περάστηκα", "περαστ");
            CheckOneTerm(a, "περαστώ", "περαστ");
            CheckOneTerm(a, "περαστείς", "περαστ");
            CheckOneTerm(a, "περαστεί", "περαστ");

            CheckOneTerm(a, "περασμένο", "περασμεν");
            CheckOneTerm(a, "περασμένη", "περασμεν");
            CheckOneTerm(a, "περασμένος", "περασμεν");

            // -ώ/-άω,-ούσα/-άξω,-αξα
            CheckOneTerm(a, "πετώ", "πετ");
            CheckOneTerm(a, "πετάω", "πετ");
            CheckOneTerm(a, "πετούσα", "πετ");
            CheckOneTerm(a, "πέταγα", "πετ");
            CheckOneTerm(a, "πέτα", "πετ");
            CheckOneTerm(a, "πετώντας", "πετ");
            CheckOneTerm(a, "πετιέμαι", "πετ");
            CheckOneTerm(a, "πετιόμουν", "πετ");

            CheckOneTerm(a, "πέταξα", "πεταξ");
            CheckOneTerm(a, "πετάξω", "πεταξ");
            CheckOneTerm(a, "πέταξε", "πεταξ");
            CheckOneTerm(a, "πετάξει", "πεταξ");

            CheckOneTerm(a, "πετάχτηκα", "πεταχτ");
            CheckOneTerm(a, "πεταχτώ", "πεταχτ");
            CheckOneTerm(a, "πεταχτείς", "πεταχτ");
            CheckOneTerm(a, "πεταχτεί", "πεταχτ");

            CheckOneTerm(a, "πεταμένο", "πεταμεν");
            CheckOneTerm(a, "πεταμένη", "πεταμεν");
            CheckOneTerm(a, "πεταμένος", "πεταμεν");

            // -ώ/-άω,-ούσα / -έσω,-εσα
            CheckOneTerm(a, "καλώ", "καλ");
            CheckOneTerm(a, "καλούσα", "καλ");
            CheckOneTerm(a, "καλείς", "καλ");
            CheckOneTerm(a, "καλώντας", "καλ");

            CheckOneTerm(a, "καλούμαι", "καλ");
            // pass. imperfect /imp. progressive doesnt conflate
            CheckOneTerm(a, "καλούμουν", "καλουμ");
            CheckOneTerm(a, "καλείσαι", "καλεισα");

            CheckOneTerm(a, "καλέστηκα", "καλεστ");
            CheckOneTerm(a, "καλεστώ", "καλεστ");
            CheckOneTerm(a, "καλεστείς", "καλεστ");
            CheckOneTerm(a, "καλεστεί", "καλεστ");

            CheckOneTerm(a, "καλεσμένο", "καλεσμεν");
            CheckOneTerm(a, "καλεσμένη", "καλεσμεν");
            CheckOneTerm(a, "καλεσμένος", "καλεσμεν");

            CheckOneTerm(a, "φορώ", "φορ");
            CheckOneTerm(a, "φοράω", "φορ");
            CheckOneTerm(a, "φορούσα", "φορ");
            CheckOneTerm(a, "φόραγα", "φορ");
            CheckOneTerm(a, "φόρα", "φορ");
            CheckOneTerm(a, "φορώντας", "φορ");
            CheckOneTerm(a, "φοριέμαι", "φορ");
            CheckOneTerm(a, "φοριόμουν", "φορ");
            CheckOneTerm(a, "φοριέσαι", "φορ");

            CheckOneTerm(a, "φόρεσα", "φορεσ");
            CheckOneTerm(a, "φορέσω", "φορεσ");
            CheckOneTerm(a, "φόρεσε", "φορεσ");
            CheckOneTerm(a, "φορέσει", "φορεσ");

            CheckOneTerm(a, "φορέθηκα", "φορεθ");
            CheckOneTerm(a, "φορεθώ", "φορεθ");
            CheckOneTerm(a, "φορεθείς", "φορεθ");
            CheckOneTerm(a, "φορεθεί", "φορεθ");

            CheckOneTerm(a, "φορεμένο", "φορεμεν");
            CheckOneTerm(a, "φορεμένη", "φορεμεν");
            CheckOneTerm(a, "φορεμένος", "φορεμεν");

            // -ώ/-άω,-ούσα / -ήσω,-ησα
            CheckOneTerm(a, "κρατώ", "κρατ");
            CheckOneTerm(a, "κρατάω", "κρατ");
            CheckOneTerm(a, "κρατούσα", "κρατ");
            CheckOneTerm(a, "κράταγα", "κρατ");
            CheckOneTerm(a, "κράτα", "κρατ");
            CheckOneTerm(a, "κρατώντας", "κρατ");

            CheckOneTerm(a, "κράτησα", "κρατ");
            CheckOneTerm(a, "κρατήσω", "κρατ");
            CheckOneTerm(a, "κράτησε", "κρατ");
            CheckOneTerm(a, "κρατήσει", "κρατ");

            CheckOneTerm(a, "κρατούμαι", "κρατ");
            CheckOneTerm(a, "κρατιέμαι", "κρατ");
            // this imperfect form doesnt conflate 
            CheckOneTerm(a, "κρατούμουν", "κρατουμ");
            CheckOneTerm(a, "κρατιόμουν", "κρατ");
            // this imp. prog form doesnt conflate
            CheckOneTerm(a, "κρατείσαι", "κρατεισα");

            CheckOneTerm(a, "κρατήθηκα", "κρατ");
            CheckOneTerm(a, "κρατηθώ", "κρατ");
            CheckOneTerm(a, "κρατηθείς", "κρατ");
            CheckOneTerm(a, "κρατηθεί", "κρατ");
            CheckOneTerm(a, "κρατήσου", "κρατ");

            CheckOneTerm(a, "κρατημένο", "κρατημεν");
            CheckOneTerm(a, "κρατημένη", "κρατημεν");
            CheckOneTerm(a, "κρατημένος", "κρατημεν");

            // -.μαι,-.μουν / -.ώ,-.ηκα
            CheckOneTerm(a, "κοιμάμαι", "κοιμ");
            CheckOneTerm(a, "κοιμόμουν", "κοιμ");
            CheckOneTerm(a, "κοιμάσαι", "κοιμ");

            CheckOneTerm(a, "κοιμήθηκα", "κοιμ");
            CheckOneTerm(a, "κοιμηθώ", "κοιμ");
            CheckOneTerm(a, "κοιμήσου", "κοιμ");
            CheckOneTerm(a, "κοιμηθεί", "κοιμ");

            CheckOneTerm(a, "κοιμισμένο", "κοιμισμεν");
            CheckOneTerm(a, "κοιμισμένη", "κοιμισμεν");
            CheckOneTerm(a, "κοιμισμένος", "κοιμισμεν");
        }

        [Test]
        public virtual void TestExceptions()
        {
            CheckOneTerm(a, "καθεστώτα", "καθεστ");
            CheckOneTerm(a, "καθεστώτος", "καθεστ");
            CheckOneTerm(a, "καθεστώς", "καθεστ");
            CheckOneTerm(a, "καθεστώτων", "καθεστ");

            CheckOneTerm(a, "χουμε", "χουμ");
            CheckOneTerm(a, "χουμ", "χουμ");

            CheckOneTerm(a, "υποταγεσ", "υποταγ");
            CheckOneTerm(a, "υποταγ", "υποταγ");

            CheckOneTerm(a, "εμετε", "εμετ");
            CheckOneTerm(a, "εμετ", "εμετ");

            CheckOneTerm(a, "αρχοντασ", "αρχοντ");
            CheckOneTerm(a, "αρχοντων", "αρχοντ");
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new GreekStemFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}