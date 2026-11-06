using J2N.IO;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Ko
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
    /// Part of speech classification for Korean based on Sejong corpus classification.
    /// The list of tags and their meanings is available here:
    /// https://docs.google.com/spreadsheets/d/1-9blXKjtjeKZqsf4NzHeYJCrr49-nXeRF6D80udfcwY
    /// </summary>
    public class POS
    {
        /// <summary>
        /// The type of the token as enum.
        /// </summary>
        public enum Type
        {
            //A simple morpheme.
            MORPHEME,

            //Compound noun.
            COMPOUND,

            //Inflected token.
            INFLECT,

            //Pre-analysis token.
            PREANALYSIS,
        }

        /// <summary>
        /// The type of the token as a list.
        /// </summary>
        public static List<Type> TypesList = new() {
            Type.MORPHEME,
            Type.COMPOUND,
            Type.INFLECT,
            Type.PREANALYSIS
        };

        public class Tag
        {
            // Sets and returns the code associated with the tag (as defined in pos-id.def).
            public int Code { get; set; }
            // Sets and returns the description associated with the tag.
            public string Description { get; set; }
            // Sets and returns the name associated with the tag.
            public string Name { get; set; }
        }

        private static readonly ByteBuffer buffer;

        /// <summary>
        /// Part of speech tag for Korean based on Sejong corpus classification.
        /// </summary>
        public static List<Tag> TagsList = new() {
            // Verbal endings
 			new() { Code = 100, Description = "Verbal endings" },
            // Interjection
 			new() { Code = 110, Description = "Interjection" },
            // Ending Particle
 			new() { Code = 120, Description = "Ending Particle" },
            // General Adverb
 			new() { Code = 130, Description = "General Adverb" },
            // Conjunctive adverb
 			new() { Code = 131, Description = "Conjunctive adverb" },
            // Modifier
 			new() { Code = 140, Description = "Modifier" },
            // General Noun
 			new() { Code = 150, Description = "General Noun" },
            // Proper Noun
 			new() { Code = 151, Description = "Proper Noun" },
            // Dependent noun
 			new() { Code = 152, Description = "Dependent noun" },
            // Dependent noun
 			new() { Code = 153, Description = "Dependent noun" },
            // Pronoun
 			new() { Code = 154, Description = "Pronoun" },
            // Numeral
 			new() { Code = 155, Description = "Numeral" },
            // Terminal punctuation
 			new() { Code = 160, Description = "Terminal punctuation" },
            // Chinese Characeter
 			new() { Code = 161, Description = "Chinese Characeter" },
            // Foreign language
 			new() { Code = 162, Description = "Foreign language" },
            // Number
 			new() { Code = 163, Description = "Number" },
            // Space
 			new() { Code = 164, Description = "Space" },
            // Closing brackets
 			new() { Code = 165, Description = "Closing brackets" },
            // Opening brackets
 			new() { Code = 166, Description = "Opening brackets" },
            // Separator
 			new() { Code = 167, Description = "Separator" },
            // Other symbol
 			new() { Code = 168, Description = "Other symbol" },
            // Ellipsis
 			new() { Code = 169, Description = "Ellipsis" },
            // Adjective
 			new() { Code = 170, Description = "Adjective" },
            // Negative designator
 			new() { Code = 171, Description = "Negative designator" },
            // Positive designator
 			new() { Code = 172, Description = "Positive designator" },
            // Verb
 			new() { Code = 173, Description = "Verb" },
            // Auxiliary Verb or Adjective
 			new() { Code = 174, Description = "Auxiliary Verb or Adjective" },
            // Prefix
 			new() { Code = 181, Description = "Prefix" },
            // Root
 			new() { Code = 182, Description = "Root" },
            // Adjective Suffix
 			new() { Code = 183, Description = "Adjective Suffix" },
            // Noun Suffix
 			new() { Code = 184, Description = "Noun Suffix" },
            // Verb
            new() { Code = -185, Description = "Verb" },
            // Unknown
            new() { Code = -999, Description = "Unknown" },
            // Unknown
            new() { Code = -1, Description = "Unknown" },
            // Unknown
            new() { Code = -1, Description = "Unknown" },
            // Unknown
            new() { Code = -1, Description = "Unknown" }
        };

        public static Dictionary<string, Tag> Tags = new Dictionary<string, Tag>() {
            { "E", new Tag { Name = "E", Code = 100, Description = "Verbal endings" } },
            { "IC", new Tag { Name = "IC", Code = 110, Description = "Interjection" } },
            { "J", new Tag { Name = "J", Code = 120, Description = "Ending Particle" } },
            { "MAG", new Tag { Name = "MAG", Code = 130, Description = "General Adverb" } },
            { "MAJ", new Tag { Name = "MAJ", Code = 131, Description = "Conjunctive adverb" } },
            { "MM", new Tag { Name = "MM", Code = 140, Description = "Modifier" } },
            { "NNG", new Tag { Name = "NNG", Code = 150, Description = "General Noun" } },
            { "NNP", new Tag { Name = "NNP", Code = 151, Description = "Proper Noun" } },
            { "NNB", new Tag { Name = "NNB", Code = 152, Description = "Dependent noun" } },
            { "NNBC", new Tag { Name = "NNBC", Code = 153, Description = "Dependent noun" } },
            { "NP", new Tag { Name = "NP", Code = 154, Description = "Pronoun" } },
            { "NR", new Tag { Name = "NR", Code = 155, Description = "Numeral" } },
            { "SF", new Tag { Name = "SF", Code = 160, Description = "Terminal punctuation" } },
            { "SH", new Tag { Name = "SH", Code = 161, Description = "Chinese Characeter" } },
            { "SL", new Tag { Name = "SL", Code = 162, Description = "Foreign language" } },
            { "SN", new Tag { Name = "SN", Code = 163, Description = "Number" } },
            { "SP", new Tag { Name = "SP", Code = 164, Description = "Space" } },
            { "SSC", new Tag { Name = "SSC", Code = 165, Description = "Closing brackets" } },
            { "SSO", new Tag { Name = "SSO", Code = 166, Description = "Opening brackets" } },
            { "SC", new Tag { Name = "SC", Code = 167, Description = "Separator" } },
            { "SY", new Tag { Name = "SY", Code = 168, Description = "Other symbol" } },
            { "SE", new Tag { Name = "SE", Code = 169, Description = "Ellipsis" } },
            { "VA", new Tag { Name = "VA", Code = 170, Description = "Adjective" } },
            { "VCN", new Tag { Name = "VCN", Code = 171, Description = "Negative designator" } },
            { "VCP", new Tag { Name = "VCP", Code = 172, Description = "Positive designator" } },
            { "VV", new Tag { Name = "VV", Code = 173, Description = "Verb" } },
            { "VX", new Tag { Name = "VX", Code = 174, Description = "Auxiliary Verb or Adjective" } },
            { "XPN", new Tag { Name = "XPN", Code = 181, Description = "Prefix" } },
            { "XR", new Tag { Name = "XR", Code = 182, Description = "Root" } },
            { "XSA", new Tag { Name = "XSA", Code = 183, Description = "Adjective Suffix" } },
            { "XSN", new Tag { Name = "XSN", Code = 184, Description = "Noun Suffix" } },
            { "XSV", new Tag { Name = "XSV", Code = -185, Description = "Verb" } },
            { "UNKNOWN", new Tag { Name = "UNKNOWN", Code = -999, Description = "Unknown" } },
            { "UNA", new Tag { Name = "UNA", Code = -1, Description = "Unknown" } },
            { "NA", new Tag { Name = "NA", Code = -1, Description = "Unknown" } },
            { "VSV", new Tag { Name = "VSV", Code = -1, Description = "Unknown" } }
        };

        /// <summary>
        /// Returns the POS.Tag of the provided name.
        /// </summary>
        public static Tag ResolveTag(String name) {
            String tagUpper = name.ToUpper();
            if (tagUpper.StartsWith("J")) {
                return Tags["J"];
            } else if (tagUpper.StartsWith("E")) {
                return Tags["E"];
            } else {
                return Tags[tagUpper];
            }
        }

        /// <summary>
        /// Returns the POS.Tag of the provided tag.
        /// </summary>
        public static Tag ResolveTag(byte tag)
        {
            short tagVal = buffer.GetInt16(tag);
            if (tagVal < Tags.Count)
            {
                return TagsList[tagVal];
            }
            return Tags["UNKNOWN"];
        }

        /// <summary>
        /// Returns the POS.Tag of the provided name.
        /// </summary>
        public static Type ResolveType(string name) {
            if ("*".Equals(name)) {
                return Type.MORPHEME;
            }

            if (Type.TryParse(name.ToUpper(), out Type result))
            {
                return result;
            }
            return Type.PREANALYSIS;
        }

        /// <summary>
        /// Returns the POS.Type of the provided type.
        /// </summary>
        public static Type ResolveType(byte type)
        {
            short typeIdx = buffer.GetInt16(type);
            if (typeIdx < sizeof(Type))
            {
                return TypesList[typeIdx];
            }
            return Type.PREANALYSIS;
        }
    }
}