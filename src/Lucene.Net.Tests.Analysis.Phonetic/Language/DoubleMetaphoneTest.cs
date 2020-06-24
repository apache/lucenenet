using NUnit.Framework;
using System;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Analysis.Phonetic.Language
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
    /// Tests <see cref="DoubleMetaphone"/>
    /// </summary>
    public class DoubleMetaphoneTest : StringEncoderAbstractTest<DoubleMetaphone>
    {
        /**
     * Test data from http://aspell.net/test/orig/batch0.tab.
     *
     * "Copyright (C) 2002 Kevin Atkinson (kevina@gnu.org). Verbatim copying
     * and distribution of this entire article is permitted in any medium,
     * provided this notice is preserved."
     *
     * Massaged the test data in the array below.
     */
        private static readonly string[][] FIXTURE = { new string[] {
            "Accosinly", "Occasionally" }, new string[] {
            "Ciculer", "Circler" }, new string[] {
            "Circue", "Circle" }, new string[] {
            "Maddness", "Madness" }, new string[] {
            "Occusionaly", "Occasionally" }, new string[] {
            "Steffen", "Stephen" }, new string[] {
            "Thw", "The" }, new string[] {
            "Unformanlly", "Unfortunately" }, new string[] {
            "Unfortally", "Unfortunately" }, new string[] {
            "abilitey", "ability" }, new string[] {
            "abouy", "about" }, new string[] {
            "absorbtion", "absorption" }, new string[] {
            "accidently", "accidentally" }, new string[] {
            "accomodate", "accommodate" }, new string[] {
            "acommadate", "accommodate" }, new string[] {
            "acord", "accord" }, new string[] {
            "adultry", "adultery" }, new string[] {
            "aggresive", "aggressive" }, new string[] {
            "alchohol", "alcohol" }, new string[] {
            "alchoholic", "alcoholic" }, new string[] {
            "allieve", "alive" }, new string[] {
            "alot", "a lot" }, new string[] {
            "alright", "all right" }, new string[] {
            "amature", "amateur" }, new string[] {
            "ambivilant", "ambivalent" }, new string[] {
            "amification", "amplification" }, new string[] {
            "amourfous", "amorphous" }, new string[] {
            "annoint", "anoint" }, new string[] {
            "annonsment", "announcement" }, new string[] {
            "annoyting", "anting" }, new string[] {
            "annuncio", "announce" }, new string[] {
            "anonomy", "anatomy" }, new string[] {
            "anotomy", "anatomy" }, new string[] {
            "antidesestablishmentarianism", "antidisestablishmentarianism" }, new string[] {
            "antidisestablishmentarism", "antidisestablishmentarianism" }, new string[] {
            "anynomous", "anonymous" }, new string[] {
            "appelet", "applet" }, new string[] {
            "appreceiated", "appreciated" }, new string[] {
            "appresteate", "appreciate" }, new string[] {
            "aquantance", "acquaintance" }, new string[] {
            "aratictature", "architecture" }, new string[] {
            "archeype", "archetype" }, new string[] {
            "aricticure", "architecture" }, new string[] {
            "artic", "arctic" }, new string[] {
            "asentote", "asymptote" }, new string[] {
            "ast", "at" }, new string[] {
            "asterick", "asterisk" }, new string[] {
            "asymetric", "asymmetric" }, new string[] {
            "atentively", "attentively" }, new string[] {
            "autoamlly", "automatically" }, new string[] {
            "bankrot", "bankrupt" }, new string[] {
            "basicly", "basically" }, new string[] {
            "batallion", "battalion" }, new string[] {
            "bbrose", "browse" }, new string[] {
            "beauro", "bureau" }, new string[] {
            "beaurocracy", "bureaucracy" }, new string[] {
            "beggining", "beginning" }, new string[] {
            "beging", "beginning" }, new string[] {
            "behaviour", "behavior" }, new string[] {
            "beleive", "believe" }, new string[] {
            "belive", "believe" }, new string[] {
            "benidifs", "benefits" }, new string[] {
            "bigginging", "beginning" }, new string[] {
            "blait", "bleat" }, new string[] {
            "bouyant", "buoyant" }, new string[] {
            "boygot", "boycott" }, new string[] {
            "brocolli", "broccoli" }, new string[] {
            "buch", "bush" }, new string[] {
            "buder", "butter" }, new string[] {
            "budr", "butter" }, new string[] {
            "budter", "butter" }, new string[] {
            "buracracy", "bureaucracy" }, new string[] {
            "burracracy", "bureaucracy" }, new string[] {
            "buton", "button" }, new string[] {
            "byby", "by by" }, new string[] {
            "cauler", "caller" }, new string[] {
            "ceasar", "caesar" }, new string[] {
            "cemetary", "cemetery" }, new string[] {
            "changeing", "changing" }, new string[] {
            "cheet", "cheat" }, new string[] {
            "cicle", "circle" }, new string[] {
            "cimplicity", "simplicity" }, new string[] {
            "circumstaces", "circumstances" }, new string[] {
            "clob", "club" }, new string[] {
            "coaln", "colon" }, new string[] {
            "cocamena", "cockamamie" }, new string[] {
            "colleaque", "colleague" }, new string[] {
            "colloquilism", "colloquialism" }, new string[] {
            "columne", "column" }, new string[] {
            "comiler", "compiler" }, new string[] {
            "comitmment", "commitment" }, new string[] {
            "comitte", "committee" }, new string[] {
            "comittmen", "commitment" }, new string[] {
            "comittmend", "commitment" }, new string[] {
            "commerciasl", "commercials" }, new string[] {
            "commited", "committed" }, new string[] {
            "commitee", "committee" }, new string[] {
            "companys", "companies" }, new string[] {
            "compicated", "complicated" }, new string[] {
            "comupter", "computer" }, new string[] {
            "concensus", "consensus" }, new string[] {
            "confusionism", "confucianism" }, new string[] {
            "congradulations", "congratulations" }, new string[] {
            "conibation", "contribution" }, new string[] {
            "consident", "consistent" }, new string[] {
            "consident", "consonant" }, new string[] {
            "contast", "constant" }, new string[] {
            "contastant", "constant" }, new string[] {
            "contunie", "continue" }, new string[] {
            "cooly", "coolly" }, new string[] {
            "copping", "coping" }, new string[] {
            "cosmoplyton", "cosmopolitan" }, new string[] {
            "courst", "court" }, new string[] {
            "crasy", "crazy" }, new string[] {
            "cravets", "caveats" }, new string[] {
            "credetability", "credibility" }, new string[] {
            "criqitue", "critique" }, new string[] {
            "croke", "croak" }, new string[] {
            "crucifiction", "crucifixion" }, new string[] {
            "crusifed", "crucified" }, new string[] {
            "ctitique", "critique" }, new string[] {
            "cumba", "combo" }, new string[] {
            "custamisation", "customization" }, new string[] {
            "dag", "dog" }, new string[] {
            "daly", "daily" }, new string[] {
            "danguages", "dangerous" }, new string[] {
            "deaft", "draft" }, new string[] {
            "defence", "defense" }, new string[] {
            "defenly", "defiantly" }, new string[] {
            "definate", "definite" }, new string[] {
            "definately", "definitely" }, new string[] {
            "dependeble", "dependable" }, new string[] {
            "descrption", "description" }, new string[] {
            "descrptn", "description" }, new string[] {
            "desparate", "desperate" }, new string[] {
            "dessicate", "desiccate" }, new string[] {
            "destint", "distant" }, new string[] {
            "develepment", "developments" }, new string[] {
            "developement", "development" }, new string[] {
            "develpond", "development" }, new string[] {
            "devulge", "divulge" }, new string[] {
            "diagree", "disagree" }, new string[] {
            "dieties", "deities" }, new string[] {
            "dinasaur", "dinosaur" }, new string[] {
            "dinasour", "dinosaur" }, new string[] {
            "direcyly", "directly" }, new string[] {
            "discuess", "discuss" }, new string[] {
            "disect", "dissect" }, new string[] {
            "disippate", "dissipate" }, new string[] {
            "disition", "decision" }, new string[] {
            "dispair", "despair" }, new string[] {
            "disssicion", "discussion" }, new string[] {
            "distarct", "distract" }, new string[] {
            "distart", "distort" }, new string[] {
            "distroy", "destroy" }, new string[] {
            "documtations", "documentation" }, new string[] {
            "doenload", "download" }, new string[] {
            "dongle", "dangle" }, new string[] {
            "doog", "dog" }, new string[] {
            "dramaticly", "dramatically" }, new string[] {
            "drunkeness", "drunkenness" }, new string[] {
            "ductioneery", "dictionary" }, new string[] {
            "dur", "due" }, new string[] {
            "duren", "during" }, new string[] {
            "dymatic", "dynamic" }, new string[] {
            "dynaic", "dynamic" }, new string[] {
            "ecstacy", "ecstasy" }, new string[] {
            "efficat", "efficient" }, new string[] {
            "efficity", "efficacy" }, new string[] {
            "effots", "efforts" }, new string[] {
            "egsistence", "existence" }, new string[] {
            "eitiology", "etiology" }, new string[] {
            "elagent", "elegant" }, new string[] {
            "elligit", "elegant" }, new string[] {
            "embarass", "embarrass" }, new string[] {
            "embarassment", "embarrassment" }, new string[] {
            "embaress", "embarrass" }, new string[] {
            "encapsualtion", "encapsulation" }, new string[] {
            "encyclapidia", "encyclopedia" }, new string[] {
            "encyclopia", "encyclopedia" }, new string[] {
            "engins", "engine" }, new string[] {
            "enhence", "enhance" }, new string[] {
            "enligtment", "Enlightenment" }, new string[] {
            "ennuui", "ennui" }, new string[] {
            "enought", "enough" }, new string[] {
            "enventions", "inventions" }, new string[] {
            "envireminakl", "environmental" }, new string[] {
            "enviroment", "environment" }, new string[] {
            "epitomy", "epitome" }, new string[] {
            "equire", "acquire" }, new string[] {
            "errara", "error" }, new string[] {
            "erro", "error" }, new string[] {
            "evaualtion", "evaluation" }, new string[] {
            "evething", "everything" }, new string[] {
            "evtually", "eventually" }, new string[] {
            "excede", "exceed" }, new string[] {
            "excercise", "exercise" }, new string[] {
            "excpt", "except" }, new string[] {
            "excution", "execution" }, new string[] {
            "exhileration", "exhilaration" }, new string[] {
            "existance", "existence" }, new string[] {
            "expleyly", "explicitly" }, new string[] {
            "explity", "explicitly" }, new string[] {
            "expresso", "espresso" }, new string[] {
            "exspidient", "expedient" }, new string[] {
            "extions", "extensions" }, new string[] {
            "factontion", "factorization" }, new string[] {
            "failer", "failure" }, new string[] {
            "famdasy", "fantasy" }, new string[] {
            "faver", "favor" }, new string[] {
            "faxe", "fax" }, new string[] {
            "febuary", "february" }, new string[] {
            "firey", "fiery" }, new string[] {
            "fistival", "festival" }, new string[] {
            "flatterring", "flattering" }, new string[] {
            "fluk", "flux" }, new string[] {
            "flukse", "flux" }, new string[] {
            "fone", "phone" }, new string[] {
            "forsee", "foresee" }, new string[] {
            "frustartaion", "frustrating" }, new string[] {
            "fuction", "function" }, new string[] {
            "funetik", "phonetic" }, new string[] {
            "futs", "guts" }, new string[] {
            "gamne", "came" }, new string[] {
            "gaurd", "guard" }, new string[] {
            "generly", "generally" }, new string[] {
            "ghandi", "gandhi" }, new string[] {
            "goberment", "government" }, new string[] {
            "gobernement", "government" }, new string[] {
            "gobernment", "government" }, new string[] {
            "gotton", "gotten" }, new string[] {
            "gracefull", "graceful" }, new string[] {
            "gradualy", "gradually" }, new string[] {
            "grammer", "grammar" }, new string[] {
            "hallo", "hello" }, new string[] {
            "hapily", "happily" }, new string[] {
            "harrass", "harass" }, new string[] {
            "havne", "have" }, new string[] {
            "heellp", "help" }, new string[] {
            "heighth", "height" }, new string[] {
            "hellp", "help" }, new string[] {
            "helo", "hello" }, new string[] {
            "herlo", "hello" }, new string[] {
            "hifin", "hyphen" }, new string[] {
            "hifine", "hyphen" }, new string[] {
            "higer", "higher" }, new string[] {
            "hiphine", "hyphen" }, new string[] {
            "hippie", "hippy" }, new string[] {
            "hippopotamous", "hippopotamus" }, new string[] {
            "hlp", "help" }, new string[] {
            "hourse", "horse" }, new string[] {
            "houssing", "housing" }, new string[] {
            "howaver", "however" }, new string[] {
            "howver", "however" }, new string[] {
            "humaniti", "humanity" }, new string[] {
            "hyfin", "hyphen" }, new string[] {
            "hypotathes", "hypothesis" }, new string[] {
            "hypotathese", "hypothesis" }, new string[] {
            "hystrical", "hysterical" }, new string[] {
            "ident", "indent" }, new string[] {
            "illegitament", "illegitimate" }, new string[] {
            "imbed", "embed" }, new string[] {
            "imediaetly", "immediately" }, new string[] {
            "imfamy", "infamy" }, new string[] {
            "immenant", "immanent" }, new string[] {
            "implemtes", "implements" }, new string[] {
            "inadvertant", "inadvertent" }, new string[] {
            "incase", "in case" }, new string[] {
            "incedious", "insidious" }, new string[] {
            "incompleet", "incomplete" }, new string[] {
            "incomplot", "incomplete" }, new string[] {
            "inconvenant", "inconvenient" }, new string[] {
            "inconvience", "inconvenience" }, new string[] {
            "independant", "independent" }, new string[] {
            "independenent", "independent" }, new string[] {
            "indepnends", "independent" }, new string[] {
            "indepth", "in depth" }, new string[] {
            "indispensible", "indispensable" }, new string[] {
            "inefficite", "inefficient" }, new string[] {
            "inerface", "interface" }, new string[] {
            "infact", "in fact" }, new string[] {
            "influencial", "influential" }, new string[] {
            "inital", "initial" }, new string[] {
            "initinized", "initialized" }, new string[] {
            "initized", "initialized" }, new string[] {
            "innoculate", "inoculate" }, new string[] {
            "insistant", "insistent" }, new string[] {
            "insistenet", "insistent" }, new string[] {
            "instulation", "installation" }, new string[] {
            "intealignt", "intelligent" }, new string[] {
            "intejilent", "intelligent" }, new string[] {
            "intelegent", "intelligent" }, new string[] {
            "intelegnent", "intelligent" }, new string[] {
            "intelejent", "intelligent" }, new string[] {
            "inteligent", "intelligent" }, new string[] {
            "intelignt", "intelligent" }, new string[] {
            "intellagant", "intelligent" }, new string[] {
            "intellegent", "intelligent" }, new string[] {
            "intellegint", "intelligent" }, new string[] {
            "intellgnt", "intelligent" }, new string[] {
            "intensionality", "intensionally" }, new string[] {
            "interate", "iterate" }, new string[] {
            "internation", "international" }, new string[] {
            "interpretate", "interpret" }, new string[] {
            "interpretter", "interpreter" }, new string[] {
            "intertes", "interested" }, new string[] {
            "intertesd", "interested" }, new string[] {
            "invermeantial", "environmental" }, new string[] {
            "irregardless", "regardless" }, new string[] {
            "irresistable", "irresistible" }, new string[] {
            "irritible", "irritable" }, new string[] {
            "islams", "muslims" }, new string[] {
            "isotrop", "isotope" }, new string[] {
            "isreal", "israel" }, new string[] {
            "johhn", "john" }, new string[] {
            "judgement", "judgment" }, new string[] {
            "kippur", "kipper" }, new string[] {
            "knawing", "knowing" }, new string[] {
            "latext", "latest" }, new string[] {
            "leasve", "leave" }, new string[] {
            "lesure", "leisure" }, new string[] {
            "liasion", "lesion" }, new string[] {
            "liason", "liaison" }, new string[] {
            "libary", "library" }, new string[] {
            "likly", "likely" }, new string[] {
            "lilometer", "kilometer" }, new string[] {
            "liquify", "liquefy" }, new string[] {
            "lloyer", "layer" }, new string[] {
            "lossing", "losing" }, new string[] {
            "luser", "laser" }, new string[] {
            "maintanence", "maintenance" }, new string[] {
            "majaerly", "majority" }, new string[] {
            "majoraly", "majority" }, new string[] {
            "maks", "masks" }, new string[] {
            "mandelbrot", "Mandelbrot" }, new string[] {
            "mant", "want" }, new string[] {
            "marshall", "marshal" }, new string[] {
            "maxium", "maximum" }, new string[] {
            "meory", "memory" }, new string[] {
            "metter", "better" }, new string[] {
            "mic", "mike" }, new string[] {
            "midia", "media" }, new string[] {
            "millenium", "millennium" }, new string[] {
            "miniscule", "minuscule" }, new string[] {
            "minkay", "monkey" }, new string[] {
            "minum", "minimum" }, new string[] {
            "mischievious", "mischievous" }, new string[] {
            "misilous", "miscellaneous" }, new string[] {
            "momento", "memento" }, new string[] {
            "monkay", "monkey" }, new string[] {
            "mosaik", "mosaic" }, new string[] {
            "mostlikely", "most likely" }, new string[] {
            "mousr", "mouser" }, new string[] {
            "mroe", "more" }, new string[] {
            "neccessary", "necessary" }, new string[] {
            "necesary", "necessary" }, new string[] {
            "necesser", "necessary" }, new string[] {
            "neice", "niece" }, new string[] {
            "neighbour", "neighbor" }, new string[] {
            "nemonic", "pneumonic" }, new string[] {
            "nevade", "Nevada" }, new string[] {
            "nickleodeon", "nickelodeon" }, new string[] {
            "nieve", "naive" }, new string[] {
            "noone", "no one" }, new string[] {
            "noticably", "noticeably" }, new string[] {
            "notin", "not in" }, new string[] {
            "nozled", "nuzzled" }, new string[] {
            "objectsion", "objects" }, new string[] {
            "obsfuscate", "obfuscate" }, new string[] {
            "ocassion", "occasion" }, new string[] {
            "occuppied", "occupied" }, new string[] {
            "occurence", "occurrence" }, new string[] {
            "octagenarian", "octogenarian" }, new string[] {
            "olf", "old" }, new string[] {
            "opposim", "opossum" }, new string[] {
            "organise", "organize" }, new string[] {
            "organiz", "organize" }, new string[] {
            "orientate", "orient" }, new string[] {
            "oscilascope", "oscilloscope" }, new string[] {
            "oving", "moving" }, new string[] {
            "paramers", "parameters" }, new string[] {
            "parametic", "parameter" }, new string[] {
            "paranets", "parameters" }, new string[] {
            "partrucal", "particular" }, new string[] {
            "pataphysical", "metaphysical" }, new string[] {
            "patten", "pattern" }, new string[] {
            "permissable", "permissible" }, new string[] {
            "permition", "permission" }, new string[] {
            "permmasivie", "permissive" }, new string[] {
            "perogative", "prerogative" }, new string[] {
            "persue", "pursue" }, new string[] {
            "phantasia", "fantasia" }, new string[] {
            "phenominal", "phenomenal" }, new string[] {
            "picaresque", "picturesque" }, new string[] {
            "playwrite", "playwright" }, new string[] {
            "poeses", "poesies" }, new string[] {
            "polation", "politician" }, new string[] {
            "poligamy", "polygamy" }, new string[] {
            "politict", "politic" }, new string[] {
            "pollice", "police" }, new string[] {
            "polypropalene", "polypropylene" }, new string[] {
            "pompom", "pompon" }, new string[] {
            "possable", "possible" }, new string[] {
            "practicle", "practical" }, new string[] {
            "pragmaticism", "pragmatism" }, new string[] {
            "preceeding", "preceding" }, new string[] {
            "precion", "precision" }, new string[] {
            "precios", "precision" }, new string[] {
            "preemptory", "peremptory" }, new string[] {
            "prefices", "prefixes" }, new string[] {
            "prefixt", "prefixed" }, new string[] {
            "presbyterian", "Presbyterian" }, new string[] {
            "presue", "pursue" }, new string[] {
            "presued", "pursued" }, new string[] {
            "privielage", "privilege" }, new string[] {
            "priviledge", "privilege" }, new string[] {
            "proceedures", "procedures" }, new string[] {
            "pronensiation", "pronunciation" }, new string[] {
            "pronisation", "pronunciation" }, new string[] {
            "pronounciation", "pronunciation" }, new string[] {
            "properally", "properly" }, new string[] {
            "proplematic", "problematic" }, new string[] {
            "protray", "portray" }, new string[] {
            "pscolgst", "psychologist" }, new string[] {
            "psicolagest", "psychologist" }, new string[] {
            "psycolagest", "psychologist" }, new string[] {
            "quoz", "quiz" }, new string[] {
            "radious", "radius" }, new string[] {
            "ramplily", "rampantly" }, new string[] {
            "reccomend", "recommend" }, new string[] {
            "reccona", "raccoon" }, new string[] {
            "recieve", "receive" }, new string[] {
            "reconise", "recognize" }, new string[] {
            "rectangeles", "rectangle" }, new string[] {
            "redign", "redesign" }, new string[] {
            "reoccurring", "recurring" }, new string[] {
            "repitition", "repetition" }, new string[] {
            "replasments", "replacement" }, new string[] {
            "reposable", "responsible" }, new string[] {
            "reseblence", "resemblance" }, new string[] {
            "respct", "respect" }, new string[] {
            "respecally", "respectfully" }, new string[] {
            "roon", "room" }, new string[] {
            "rought", "roughly" }, new string[] {
            "rsx", "RSX" }, new string[] {
            "rudemtry", "rudimentary" }, new string[] {
            "runnung", "running" }, new string[] {
            "sacreligious", "sacrilegious" }, new string[] {
            "saftly", "safely" }, new string[] {
            "salut", "salute" }, new string[] {
            "satifly", "satisfy" }, new string[] {
            "scrabdle", "scrabble" }, new string[] {
            "searcheable", "searchable" }, new string[] {
            "secion", "section" }, new string[] {
            "seferal", "several" }, new string[] {
            "segements", "segments" }, new string[] {
            "sence", "sense" }, new string[] {
            "seperate", "separate" }, new string[] {
            "sherbert", "sherbet" }, new string[] {
            "sicolagest", "psychologist" }, new string[] {
            "sieze", "seize" }, new string[] {
            "simpfilty", "simplicity" }, new string[] {
            "simplye", "simply" }, new string[] {
            "singal", "signal" }, new string[] {
            "sitte", "site" }, new string[] {
            "situration", "situation" }, new string[] {
            "slyph", "sylph" }, new string[] {
            "smil", "smile" }, new string[] {
            "snuck", "sneaked" }, new string[] {
            "sometmes", "sometimes" }, new string[] {
            "soonec", "sonic" }, new string[] {
            "specificialy", "specifically" }, new string[] {
            "spel", "spell" }, new string[] {
            "spoak", "spoke" }, new string[] {
            "sponsered", "sponsored" }, new string[] {
            "stering", "steering" }, new string[] {
            "straightjacket", "straitjacket" }, new string[] {
            "stumach", "stomach" }, new string[] {
            "stutent", "student" }, new string[] {
            "styleguide", "style guide" }, new string[] {
            "subisitions", "substitutions" }, new string[] {
            "subjecribed", "subscribed" }, new string[] {
            "subpena", "subpoena" }, new string[] {
            "substations", "substitutions" }, new string[] {
            "suger", "sugar" }, new string[] {
            "supercede", "supersede" }, new string[] {
            "superfulous", "superfluous" }, new string[] {
            "susan", "Susan" }, new string[] {
            "swimwear", "swim wear" }, new string[] {
            "syncorization", "synchronization" }, new string[] {
            "taff", "tough" }, new string[] {
            "taht", "that" }, new string[] {
            "tattos", "tattoos" }, new string[] {
            "techniquely", "technically" }, new string[] {
            "teh", "the" }, new string[] {
            "tem", "team" }, new string[] {
            "teo", "two" }, new string[] {
            "teridical", "theoretical" }, new string[] {
            "tesst", "test" }, new string[] {
            "tets", "tests" }, new string[] {
            "thanot", "than or" }, new string[] {
            "theirselves", "themselves" }, new string[] {
            "theridically", "theoretical" }, new string[] {
            "thredically", "theoretically" }, new string[] {
            "thruout", "throughout" }, new string[] {
            "ths", "this" }, new string[] {
            "titalate", "titillate" }, new string[] {
            "tobagan", "tobaggon" }, new string[] {
            "tommorrow", "tomorrow" }, new string[] {
            "tomorow", "tomorrow" }, new string[] {
            "tradegy", "tragedy" }, new string[] {
            "trubbel", "trouble" }, new string[] {
            "ttest", "test" }, new string[] {
            "tunnellike", "tunnel like" }, new string[] {
            "tured", "turned" }, new string[] {
            "tyrrany", "tyranny" }, new string[] {
            "unatourral", "unnatural" }, new string[] {
            "unaturral", "unnatural" }, new string[] {
            "unconisitional", "unconstitutional" }, new string[] {
            "unconscience", "unconscious" }, new string[] {
            "underladder", "under ladder" }, new string[] {
            "unentelegible", "unintelligible" }, new string[] {
            "unfortunently", "unfortunately" }, new string[] {
            "unnaturral", "unnatural" }, new string[] {
            "upcast", "up cast" }, new string[] {
            "upmost", "utmost" }, new string[] {
            "uranisium", "uranium" }, new string[] {
            "verison", "version" }, new string[] {
            "vinagarette", "vinaigrette" }, new string[] {
            "volumptuous", "voluptuous" }, new string[] {
            "volunteerism", "voluntarism" }, new string[] {
            "volye", "volley" }, new string[] {
            "wadting", "wasting" }, new string[] {
            "waite", "wait" }, new string[] {
            "wan't", "won't" }, new string[] {
            "warloord", "warlord" }, new string[] {
            "whaaat", "what" }, new string[] {
            "whard", "ward" }, new string[] {
            "whimp", "wimp" }, new string[] {
            "wicken", "weaken" }, new string[] {
            "wierd", "weird" }, new string[] {
            "wrank", "rank" }, new string[] {
            "writeen", "righten" }, new string[] {
            "writting", "writing" }, new string[] {
            "wundeews", "windows" }, new string[] {
            "yeild", "yield" }, new string[] {
            "youe", "your" }
        };

        /**
         * A subset of FIXTURE generated by this test.
         */
        private static readonly string[][] MATCHES = { new string[] {
            "Accosinly", "Occasionally" }, new string[] {
            "Maddness", "Madness" }, new string[] {
            "Occusionaly", "Occasionally" }, new string[] {
            "Steffen", "Stephen" }, new string[] {
            "Thw", "The" }, new string[] {
            "Unformanlly", "Unfortunately" }, new string[] {
            "Unfortally", "Unfortunately" }, new string[] {
            "abilitey", "ability" }, new string[] {
            "absorbtion", "absorption" }, new string[] {
            "accidently", "accidentally" }, new string[] {
            "accomodate", "accommodate" }, new string[] {
            "acommadate", "accommodate" }, new string[] {
            "acord", "accord" }, new string[] {
            "adultry", "adultery" }, new string[] {
            "aggresive", "aggressive" }, new string[] {
            "alchohol", "alcohol" }, new string[] {
            "alchoholic", "alcoholic" }, new string[] {
            "allieve", "alive" }, new string[] {
            "alot", "a lot" }, new string[] {
            "alright", "all right" }, new string[] {
            "amature", "amateur" }, new string[] {
            "ambivilant", "ambivalent" }, new string[] {
            "amourfous", "amorphous" }, new string[] {
            "annoint", "anoint" }, new string[] {
            "annonsment", "announcement" }, new string[] {
            "annoyting", "anting" }, new string[] {
            "annuncio", "announce" }, new string[] {
            "anotomy", "anatomy" }, new string[] {
            "antidesestablishmentarianism", "antidisestablishmentarianism" }, new string[] {
            "antidisestablishmentarism", "antidisestablishmentarianism" }, new string[] {
            "anynomous", "anonymous" }, new string[] {
            "appelet", "applet" }, new string[] {
            "appreceiated", "appreciated" }, new string[] {
            "appresteate", "appreciate" }, new string[] {
            "aquantance", "acquaintance" }, new string[] {
            "aricticure", "architecture" }, new string[] {
            "asterick", "asterisk" }, new string[] {
            "asymetric", "asymmetric" }, new string[] {
            "atentively", "attentively" }, new string[] {
            "bankrot", "bankrupt" }, new string[] {
            "basicly", "basically" }, new string[] {
            "batallion", "battalion" }, new string[] {
            "bbrose", "browse" }, new string[] {
            "beauro", "bureau" }, new string[] {
            "beaurocracy", "bureaucracy" }, new string[] {
            "beggining", "beginning" }, new string[] {
            "behaviour", "behavior" }, new string[] {
            "beleive", "believe" }, new string[] {
            "belive", "believe" }, new string[] {
            "blait", "bleat" }, new string[] {
            "bouyant", "buoyant" }, new string[] {
            "boygot", "boycott" }, new string[] {
            "brocolli", "broccoli" }, new string[] {
            "buder", "butter" }, new string[] {
            "budr", "butter" }, new string[] {
            "budter", "butter" }, new string[] {
            "buracracy", "bureaucracy" }, new string[] {
            "burracracy", "bureaucracy" }, new string[] {
            "buton", "button" }, new string[] {
            "byby", "by by" }, new string[] {
            "cauler", "caller" }, new string[] {
            "ceasar", "caesar" }, new string[] {
            "cemetary", "cemetery" }, new string[] {
            "changeing", "changing" }, new string[] {
            "cheet", "cheat" }, new string[] {
            "cimplicity", "simplicity" }, new string[] {
            "circumstaces", "circumstances" }, new string[] {
            "clob", "club" }, new string[] {
            "coaln", "colon" }, new string[] {
            "colleaque", "colleague" }, new string[] {
            "colloquilism", "colloquialism" }, new string[] {
            "columne", "column" }, new string[] {
            "comitmment", "commitment" }, new string[] {
            "comitte", "committee" }, new string[] {
            "comittmen", "commitment" }, new string[] {
            "comittmend", "commitment" }, new string[] {
            "commerciasl", "commercials" }, new string[] {
            "commited", "committed" }, new string[] {
            "commitee", "committee" }, new string[] {
            "companys", "companies" }, new string[] {
            "comupter", "computer" }, new string[] {
            "concensus", "consensus" }, new string[] {
            "confusionism", "confucianism" }, new string[] {
            "congradulations", "congratulations" }, new string[] {
            "contunie", "continue" }, new string[] {
            "cooly", "coolly" }, new string[] {
            "copping", "coping" }, new string[] {
            "cosmoplyton", "cosmopolitan" }, new string[] {
            "crasy", "crazy" }, new string[] {
            "croke", "croak" }, new string[] {
            "crucifiction", "crucifixion" }, new string[] {
            "crusifed", "crucified" }, new string[] {
            "cumba", "combo" }, new string[] {
            "custamisation", "customization" }, new string[] {
            "dag", "dog" }, new string[] {
            "daly", "daily" }, new string[] {
            "defence", "defense" }, new string[] {
            "definate", "definite" }, new string[] {
            "definately", "definitely" }, new string[] {
            "dependeble", "dependable" }, new string[] {
            "descrption", "description" }, new string[] {
            "descrptn", "description" }, new string[] {
            "desparate", "desperate" }, new string[] {
            "dessicate", "desiccate" }, new string[] {
            "destint", "distant" }, new string[] {
            "develepment", "developments" }, new string[] {
            "developement", "development" }, new string[] {
            "develpond", "development" }, new string[] {
            "devulge", "divulge" }, new string[] {
            "dieties", "deities" }, new string[] {
            "dinasaur", "dinosaur" }, new string[] {
            "dinasour", "dinosaur" }, new string[] {
            "discuess", "discuss" }, new string[] {
            "disect", "dissect" }, new string[] {
            "disippate", "dissipate" }, new string[] {
            "disition", "decision" }, new string[] {
            "dispair", "despair" }, new string[] {
            "distarct", "distract" }, new string[] {
            "distart", "distort" }, new string[] {
            "distroy", "destroy" }, new string[] {
            "doenload", "download" }, new string[] {
            "dongle", "dangle" }, new string[] {
            "doog", "dog" }, new string[] {
            "dramaticly", "dramatically" }, new string[] {
            "drunkeness", "drunkenness" }, new string[] {
            "ductioneery", "dictionary" }, new string[] {
            "ecstacy", "ecstasy" }, new string[] {
            "egsistence", "existence" }, new string[] {
            "eitiology", "etiology" }, new string[] {
            "elagent", "elegant" }, new string[] {
            "embarass", "embarrass" }, new string[] {
            "embarassment", "embarrassment" }, new string[] {
            "embaress", "embarrass" }, new string[] {
            "encapsualtion", "encapsulation" }, new string[] {
            "encyclapidia", "encyclopedia" }, new string[] {
            "encyclopia", "encyclopedia" }, new string[] {
            "engins", "engine" }, new string[] {
            "enhence", "enhance" }, new string[] {
            "ennuui", "ennui" }, new string[] {
            "enventions", "inventions" }, new string[] {
            "envireminakl", "environmental" }, new string[] {
            "enviroment", "environment" }, new string[] {
            "epitomy", "epitome" }, new string[] {
            "equire", "acquire" }, new string[] {
            "errara", "error" }, new string[] {
            "evaualtion", "evaluation" }, new string[] {
            "excede", "exceed" }, new string[] {
            "excercise", "exercise" }, new string[] {
            "excpt", "except" }, new string[] {
            "exhileration", "exhilaration" }, new string[] {
            "existance", "existence" }, new string[] {
            "expleyly", "explicitly" }, new string[] {
            "explity", "explicitly" }, new string[] {
            "failer", "failure" }, new string[] {
            "faver", "favor" }, new string[] {
            "faxe", "fax" }, new string[] {
            "firey", "fiery" }, new string[] {
            "fistival", "festival" }, new string[] {
            "flatterring", "flattering" }, new string[] {
            "flukse", "flux" }, new string[] {
            "fone", "phone" }, new string[] {
            "forsee", "foresee" }, new string[] {
            "frustartaion", "frustrating" }, new string[] {
            "funetik", "phonetic" }, new string[] {
            "gaurd", "guard" }, new string[] {
            "generly", "generally" }, new string[] {
            "ghandi", "gandhi" }, new string[] {
            "gotton", "gotten" }, new string[] {
            "gracefull", "graceful" }, new string[] {
            "gradualy", "gradually" }, new string[] {
            "grammer", "grammar" }, new string[] {
            "hallo", "hello" }, new string[] {
            "hapily", "happily" }, new string[] {
            "harrass", "harass" }, new string[] {
            "heellp", "help" }, new string[] {
            "heighth", "height" }, new string[] {
            "hellp", "help" }, new string[] {
            "helo", "hello" }, new string[] {
            "hifin", "hyphen" }, new string[] {
            "hifine", "hyphen" }, new string[] {
            "hiphine", "hyphen" }, new string[] {
            "hippie", "hippy" }, new string[] {
            "hippopotamous", "hippopotamus" }, new string[] {
            "hourse", "horse" }, new string[] {
            "houssing", "housing" }, new string[] {
            "howaver", "however" }, new string[] {
            "howver", "however" }, new string[] {
            "humaniti", "humanity" }, new string[] {
            "hyfin", "hyphen" }, new string[] {
            "hystrical", "hysterical" }, new string[] {
            "illegitament", "illegitimate" }, new string[] {
            "imbed", "embed" }, new string[] {
            "imediaetly", "immediately" }, new string[] {
            "immenant", "immanent" }, new string[] {
            "implemtes", "implements" }, new string[] {
            "inadvertant", "inadvertent" }, new string[] {
            "incase", "in case" }, new string[] {
            "incedious", "insidious" }, new string[] {
            "incompleet", "incomplete" }, new string[] {
            "incomplot", "incomplete" }, new string[] {
            "inconvenant", "inconvenient" }, new string[] {
            "inconvience", "inconvenience" }, new string[] {
            "independant", "independent" }, new string[] {
            "independenent", "independent" }, new string[] {
            "indepnends", "independent" }, new string[] {
            "indepth", "in depth" }, new string[] {
            "indispensible", "indispensable" }, new string[] {
            "inefficite", "inefficient" }, new string[] {
            "infact", "in fact" }, new string[] {
            "influencial", "influential" }, new string[] {
            "innoculate", "inoculate" }, new string[] {
            "insistant", "insistent" }, new string[] {
            "insistenet", "insistent" }, new string[] {
            "instulation", "installation" }, new string[] {
            "intealignt", "intelligent" }, new string[] {
            "intelegent", "intelligent" }, new string[] {
            "intelegnent", "intelligent" }, new string[] {
            "intelejent", "intelligent" }, new string[] {
            "inteligent", "intelligent" }, new string[] {
            "intelignt", "intelligent" }, new string[] {
            "intellagant", "intelligent" }, new string[] {
            "intellegent", "intelligent" }, new string[] {
            "intellegint", "intelligent" }, new string[] {
            "intellgnt", "intelligent" }, new string[] {
            "intensionality", "intensionally" }, new string[] {
            "internation", "international" }, new string[] {
            "interpretate", "interpret" }, new string[] {
            "interpretter", "interpreter" }, new string[] {
            "intertes", "interested" }, new string[] {
            "intertesd", "interested" }, new string[] {
            "invermeantial", "environmental" }, new string[] {
            "irresistable", "irresistible" }, new string[] {
            "irritible", "irritable" }, new string[] {
            "isreal", "israel" }, new string[] {
            "johhn", "john" }, new string[] {
            "kippur", "kipper" }, new string[] {
            "knawing", "knowing" }, new string[] {
            "lesure", "leisure" }, new string[] {
            "liasion", "lesion" }, new string[] {
            "liason", "liaison" }, new string[] {
            "likly", "likely" }, new string[] {
            "liquify", "liquefy" }, new string[] {
            "lloyer", "layer" }, new string[] {
            "lossing", "losing" }, new string[] {
            "luser", "laser" }, new string[] {
            "maintanence", "maintenance" }, new string[] {
            "mandelbrot", "Mandelbrot" }, new string[] {
            "marshall", "marshal" }, new string[] {
            "maxium", "maximum" }, new string[] {
            "mic", "mike" }, new string[] {
            "midia", "media" }, new string[] {
            "millenium", "millennium" }, new string[] {
            "miniscule", "minuscule" }, new string[] {
            "minkay", "monkey" }, new string[] {
            "mischievious", "mischievous" }, new string[] {
            "momento", "memento" }, new string[] {
            "monkay", "monkey" }, new string[] {
            "mosaik", "mosaic" }, new string[] {
            "mostlikely", "most likely" }, new string[] {
            "mousr", "mouser" }, new string[] {
            "mroe", "more" }, new string[] {
            "necesary", "necessary" }, new string[] {
            "necesser", "necessary" }, new string[] {
            "neice", "niece" }, new string[] {
            "neighbour", "neighbor" }, new string[] {
            "nemonic", "pneumonic" }, new string[] {
            "nevade", "Nevada" }, new string[] {
            "nickleodeon", "nickelodeon" }, new string[] {
            "nieve", "naive" }, new string[] {
            "noone", "no one" }, new string[] {
            "notin", "not in" }, new string[] {
            "nozled", "nuzzled" }, new string[] {
            "objectsion", "objects" }, new string[] {
            "ocassion", "occasion" }, new string[] {
            "occuppied", "occupied" }, new string[] {
            "occurence", "occurrence" }, new string[] {
            "octagenarian", "octogenarian" }, new string[] {
            "opposim", "opossum" }, new string[] {
            "organise", "organize" }, new string[] {
            "organiz", "organize" }, new string[] {
            "orientate", "orient" }, new string[] {
            "oscilascope", "oscilloscope" }, new string[] {
            "parametic", "parameter" }, new string[] {
            "permissable", "permissible" }, new string[] {
            "permmasivie", "permissive" }, new string[] {
            "persue", "pursue" }, new string[] {
            "phantasia", "fantasia" }, new string[] {
            "phenominal", "phenomenal" }, new string[] {
            "playwrite", "playwright" }, new string[] {
            "poeses", "poesies" }, new string[] {
            "poligamy", "polygamy" }, new string[] {
            "politict", "politic" }, new string[] {
            "pollice", "police" }, new string[] {
            "polypropalene", "polypropylene" }, new string[] {
            "possable", "possible" }, new string[] {
            "practicle", "practical" }, new string[] {
            "pragmaticism", "pragmatism" }, new string[] {
            "preceeding", "preceding" }, new string[] {
            "precios", "precision" }, new string[] {
            "preemptory", "peremptory" }, new string[] {
            "prefixt", "prefixed" }, new string[] {
            "presbyterian", "Presbyterian" }, new string[] {
            "presue", "pursue" }, new string[] {
            "presued", "pursued" }, new string[] {
            "privielage", "privilege" }, new string[] {
            "priviledge", "privilege" }, new string[] {
            "proceedures", "procedures" }, new string[] {
            "pronensiation", "pronunciation" }, new string[] {
            "pronounciation", "pronunciation" }, new string[] {
            "properally", "properly" }, new string[] {
            "proplematic", "problematic" }, new string[] {
            "protray", "portray" }, new string[] {
            "pscolgst", "psychologist" }, new string[] {
            "psicolagest", "psychologist" }, new string[] {
            "psycolagest", "psychologist" }, new string[] {
            "quoz", "quiz" }, new string[] {
            "radious", "radius" }, new string[] {
            "reccomend", "recommend" }, new string[] {
            "reccona", "raccoon" }, new string[] {
            "recieve", "receive" }, new string[] {
            "reconise", "recognize" }, new string[] {
            "rectangeles", "rectangle" }, new string[] {
            "reoccurring", "recurring" }, new string[] {
            "repitition", "repetition" }, new string[] {
            "replasments", "replacement" }, new string[] {
            "respct", "respect" }, new string[] {
            "respecally", "respectfully" }, new string[] {
            "rsx", "RSX" }, new string[] {
            "runnung", "running" }, new string[] {
            "sacreligious", "sacrilegious" }, new string[] {
            "salut", "salute" }, new string[] {
            "searcheable", "searchable" }, new string[] {
            "seferal", "several" }, new string[] {
            "segements", "segments" }, new string[] {
            "sence", "sense" }, new string[] {
            "seperate", "separate" }, new string[] {
            "sicolagest", "psychologist" }, new string[] {
            "sieze", "seize" }, new string[] {
            "simplye", "simply" }, new string[] {
            "sitte", "site" }, new string[] {
            "slyph", "sylph" }, new string[] {
            "smil", "smile" }, new string[] {
            "sometmes", "sometimes" }, new string[] {
            "soonec", "sonic" }, new string[] {
            "specificialy", "specifically" }, new string[] {
            "spel", "spell" }, new string[] {
            "spoak", "spoke" }, new string[] {
            "sponsered", "sponsored" }, new string[] {
            "stering", "steering" }, new string[] {
            "straightjacket", "straitjacket" }, new string[] {
            "stumach", "stomach" }, new string[] {
            "stutent", "student" }, new string[] {
            "styleguide", "style guide" }, new string[] {
            "subpena", "subpoena" }, new string[] {
            "substations", "substitutions" }, new string[] {
            "supercede", "supersede" }, new string[] {
            "superfulous", "superfluous" }, new string[] {
            "susan", "Susan" }, new string[] {
            "swimwear", "swim wear" }, new string[] {
            "syncorization", "synchronization" }, new string[] {
            "taff", "tough" }, new string[] {
            "taht", "that" }, new string[] {
            "tattos", "tattoos" }, new string[] {
            "techniquely", "technically" }, new string[] {
            "teh", "the" }, new string[] {
            "tem", "team" }, new string[] {
            "teo", "two" }, new string[] {
            "teridical", "theoretical" }, new string[] {
            "tesst", "test" }, new string[] {
            "theridically", "theoretical" }, new string[] {
            "thredically", "theoretically" }, new string[] {
            "thruout", "throughout" }, new string[] {
            "ths", "this" }, new string[] {
            "titalate", "titillate" }, new string[] {
            "tobagan", "tobaggon" }, new string[] {
            "tommorrow", "tomorrow" }, new string[] {
            "tomorow", "tomorrow" }, new string[] {
            "trubbel", "trouble" }, new string[] {
            "ttest", "test" }, new string[] {
            "tyrrany", "tyranny" }, new string[] {
            "unatourral", "unnatural" }, new string[] {
            "unaturral", "unnatural" }, new string[] {
            "unconisitional", "unconstitutional" }, new string[] {
            "unconscience", "unconscious" }, new string[] {
            "underladder", "under ladder" }, new string[] {
            "unentelegible", "unintelligible" }, new string[] {
            "unfortunently", "unfortunately" }, new string[] {
            "unnaturral", "unnatural" }, new string[] {
            "upcast", "up cast" }, new string[] {
            "verison", "version" }, new string[] {
            "vinagarette", "vinaigrette" }, new string[] {
            "volunteerism", "voluntarism" }, new string[] {
            "volye", "volley" }, new string[] {
            "waite", "wait" }, new string[] {
            "wan't", "won't" }, new string[] {
            "warloord", "warlord" }, new string[] {
            "whaaat", "what" }, new string[] {
            "whard", "ward" }, new string[] {
            "whimp", "wimp" }, new string[] {
            "wicken", "weaken" }, new string[] {
            "wierd", "weird" }, new string[] {
            "wrank", "rank" }, new string[] {
            "writeen", "righten" }, new string[] {
            "writting", "writing" }, new string[] {
            "wundeews", "windows" }, new string[] {
            "yeild", "yield" },
        };

        /**
         * Tests encoding APIs in one place.
         */
        private void AssertDoubleMetaphone(string expected, string source)
        {
            Assert.AreEqual(expected, this.StringEncoder.Encode(source));
            //try
            //{
            //    Assert.AreEqual(expected, this.StringEncoder.Encode((object)source));
            //}
            //catch (EncoderException e) {
            //    Assert.Fail("Unexpected expection: " + e);
            //}
            Assert.AreEqual(expected, this.StringEncoder.GetDoubleMetaphone(source));
            Assert.AreEqual(expected, this.StringEncoder.GetDoubleMetaphone(source, false));
        }

        /**
         * Tests encoding APIs in one place.
         */
        public void AssertDoubleMetaphoneAlt(string expected, string source)
        {
            Assert.AreEqual(expected, this.StringEncoder.GetDoubleMetaphone(source, true));
        }

        public void DoubleMetaphoneEqualTest(string[][] pairs, bool useAlternate)
        {
            this.ValidateFixture(pairs);
            foreach (string[] pair in pairs)
            {
                String name0 = pair[0];
                String name1 = pair[1];
                String failMsg = "Expected match between " + name0 + " and " + name1 + " (use alternate: " + useAlternate + ")";
                Assert.True(this.StringEncoder.IsDoubleMetaphoneEqual(name0, name1, useAlternate), failMsg);
                Assert.True(this.StringEncoder.IsDoubleMetaphoneEqual(name1, name0, useAlternate), failMsg);
                if (!useAlternate)
                {
                    Assert.True(this.StringEncoder.IsDoubleMetaphoneEqual(name0, name1), failMsg);
                    Assert.True(this.StringEncoder.IsDoubleMetaphoneEqual(name1, name0), failMsg);
                }
            }
        }

        public void DoubleMetaphoneNotEqualTest(bool alternate)
        {
            Assert.False(this.StringEncoder.IsDoubleMetaphoneEqual("Brain", "Band", alternate));
            Assert.False(this.StringEncoder.IsDoubleMetaphoneEqual("Band", "Brain", alternate));

            if (!alternate)
            {
                Assert.False(this.StringEncoder.IsDoubleMetaphoneEqual("Brain", "Band"));
                Assert.False(this.StringEncoder.IsDoubleMetaphoneEqual("Band", "Brain"));
            }
        }

        protected override DoubleMetaphone CreateStringEncoder()
        {
            return new DoubleMetaphone();
        }

        [Test]
        public void TestDoubleMetaphone()
        {
            AssertDoubleMetaphone("TSTN", "testing");
            AssertDoubleMetaphone("0", "The");
            AssertDoubleMetaphone("KK", "quick");
            AssertDoubleMetaphone("PRN", "brown");
            AssertDoubleMetaphone("FKS", "fox");
            AssertDoubleMetaphone("JMPT", "jumped");
            AssertDoubleMetaphone("AFR", "over");
            AssertDoubleMetaphone("0", "the");
            AssertDoubleMetaphone("LS", "lazy");
            AssertDoubleMetaphone("TKS", "dogs");
            AssertDoubleMetaphone("MKFR", "MacCafferey");
            AssertDoubleMetaphone("STFN", "Stephan");
            AssertDoubleMetaphone("KSSK", "Kuczewski");
            AssertDoubleMetaphone("MKLL", "McClelland");
            AssertDoubleMetaphone("SNHS", "san jose");
            AssertDoubleMetaphone("SNFP", "xenophobia");

            AssertDoubleMetaphoneAlt("TSTN", "testing");
            AssertDoubleMetaphoneAlt("T", "The");
            AssertDoubleMetaphoneAlt("KK", "quick");
            AssertDoubleMetaphoneAlt("PRN", "brown");
            AssertDoubleMetaphoneAlt("FKS", "fox");
            AssertDoubleMetaphoneAlt("AMPT", "jumped");
            AssertDoubleMetaphoneAlt("AFR", "over");
            AssertDoubleMetaphoneAlt("T", "the");
            AssertDoubleMetaphoneAlt("LS", "lazy");
            AssertDoubleMetaphoneAlt("TKS", "dogs");
            AssertDoubleMetaphoneAlt("MKFR", "MacCafferey");
            AssertDoubleMetaphoneAlt("STFN", "Stephan");
            AssertDoubleMetaphoneAlt("KXFS", "Kutchefski");
            AssertDoubleMetaphoneAlt("MKLL", "McClelland");
            AssertDoubleMetaphoneAlt("SNHS", "san jose");
            AssertDoubleMetaphoneAlt("SNFP", "xenophobia");
            AssertDoubleMetaphoneAlt("FKR", "Fokker");
            AssertDoubleMetaphoneAlt("AK", "Joqqi");
            AssertDoubleMetaphoneAlt("HF", "Hovvi");
            AssertDoubleMetaphoneAlt("XRN", "Czerny");
        }

        [Test]
        public void TestEmpty()
        {
            Assert.AreEqual(null, this.StringEncoder.GetDoubleMetaphone(null));
            Assert.AreEqual(null, this.StringEncoder.GetDoubleMetaphone(""));
            Assert.AreEqual(null, this.StringEncoder.GetDoubleMetaphone(" "));
            Assert.AreEqual(null, this.StringEncoder.GetDoubleMetaphone("\t\n\r "));
        }

        /**
         * Test setting maximum length
         */
        [Test]
        public void TestSetMaxCodeLength()
        {
            String value = "jumped";

            DoubleMetaphone doubleMetaphone = new DoubleMetaphone();

            // Sanity check of default settings
            Assert.AreEqual(4, doubleMetaphone.MaxCodeLen, "Default Max Code Length");
            Assert.AreEqual("JMPT", doubleMetaphone.GetDoubleMetaphone(value, false), "Default Primary");
            Assert.AreEqual("AMPT", doubleMetaphone.GetDoubleMetaphone(value, true), "Default Alternate");

            // Check setting Max Code Length
            doubleMetaphone.MaxCodeLen = (3);
            Assert.AreEqual(3, doubleMetaphone.MaxCodeLen, "Set Max Code Length");
            Assert.AreEqual("JMP", doubleMetaphone.GetDoubleMetaphone(value, false), "Max=3 Primary");
            Assert.AreEqual("AMP", doubleMetaphone.GetDoubleMetaphone(value, true), "Max=3 Alternate");
        }

        [Test]
        public void TestIsDoubleMetaphoneEqualBasic()
        {
            string[][]
        testFixture = { new string[] { "Case", "case" }, new string[] {
                "CASE", "Case" }, new string[]{
                "caSe", "cAsE" }, new string[]{
                "cookie", "quick" }, new string[]{
                "quick", "cookie" }, new string[]{
                "Brian", "Bryan" }, new string[]{
                "Auto", "Otto" }, new string[]{
                "Steven", "Stefan" }, new string[]{
                "Philipowitz", "Filipowicz" }
        };
            DoubleMetaphoneEqualTest(testFixture, false);
            DoubleMetaphoneEqualTest(testFixture, true);
        }

        /**
         * Example in the original article but failures in this Java impl:
         */
        [Test]
        public void TestIsDoubleMetaphoneEqualExtended1()
        {
            //        String[][] testFixture = new String[][] { { "Smith", "Schmidt" }
            //        };
            //        doubleMetaphoneEqualTest(testFixture, false);
            //        doubleMetaphoneEqualTest(testFixture, true);
        }

        [Test]
        public void TestIsDoubleMetaphoneEqualExtended2()
        {
            string[][]
        testFixture = { new string[] { "Jablonski", "Yablonsky" }
        };
            //doubleMetaphoneEqualTest(testFixture, false);
            DoubleMetaphoneEqualTest(testFixture, true);
        }

        /**
         * Used to generate the MATCHES array and test possible matches from the
         * FIXTURE array.
         */
        [Test]
        public void TestIsDoubleMetaphoneEqualExtended3()
        {
            this.ValidateFixture(FIXTURE);
            StringBuilder failures = new StringBuilder();
            StringBuilder matches = new StringBuilder();
            String cr = Environment.NewLine;
            matches.Append("private static final String[][] MATCHES = {" + cr);
            int failCount = 0;
            for (int i = 0; i < FIXTURE.Length; i++)
            {
                String name0 = FIXTURE[i][0];
                String name1 = FIXTURE[i][1];
                bool match1 = this.StringEncoder.IsDoubleMetaphoneEqual(name0, name1, false);
                bool match2 = this.StringEncoder.IsDoubleMetaphoneEqual(name0, name1, true);
                if (match1 == false && match2 == false)
                {
                    string failMsg = "[" + i + "] " + name0 + " and " + name1 + cr;
                    failures.Append(failMsg);
                    failCount++;
                }
                else
                {
                    matches.Append("{\"" + name0 + "\", \"" + name1 + "\"}," + cr);
                }
            }
            matches.Append("};");
            // Turn on to print a new MATCH array
            //System.out.println(matches.toString());
            if (failCount > 0)
            {
                // Turn on to see which pairs do NOT match.
                // String msg = failures.toString();
                //fail(failCount + " failures out of " + FIXTURE.length + ". The
                // following could be made to match: " + cr + msg);
            }
        }

        [Test]
        public void TestIsDoubleMetaphoneEqualWithMATCHES()
        {
            this.ValidateFixture(MATCHES);
            for (int i = 0; i < MATCHES.Length; i++)
            {
                String name0 = MATCHES[i][0];
                String name1 = MATCHES[i][1];
                bool match1 = this.StringEncoder.IsDoubleMetaphoneEqual(name0, name1, false);
                bool match2 = this.StringEncoder.IsDoubleMetaphoneEqual(name0, name1, true);
                if (match1 == false && match2 == false)
                {
                    Assert.Fail("Expected match [" + i + "] " + name0 + " and " + name1);
                }
            }
        }

        [Test]
        public void TestIsDoubleMetaphoneNotEqual()
        {
            DoubleMetaphoneNotEqualTest(false);
            DoubleMetaphoneNotEqualTest(true);
        }

        [Test]
        public void TestCCedilla()
        {
            Assert.True(this.StringEncoder.IsDoubleMetaphoneEqual("\u00e7", "S")); // c-cedilla
        }

        [Test]
        public void TestNTilde()
        {
            Assert.True(this.StringEncoder.IsDoubleMetaphoneEqual("\u00f1", "N")); // n-tilde
        }

        public void ValidateFixture(string[][] pairs)
        {
            if (pairs.Length == 0)
            {
                Assert.Fail("Test fixture is empty");
            }
            for (int i = 0; i < pairs.Length; i++)
            {
                if (pairs[i].Length != 2)
                {
                    Assert.Fail("Error in test fixture in the data array at index " + i);
                }
            }
        }
    }
}
