using J2N.IO;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Ko
{
    public class POS
    {
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

        public class Tag
        {
            public int Code { get; set; }
            public string Description { get; set; }

            public string Name { get; set; }

        }

        private static readonly ByteBuffer buffer;

        public static List<Type> TypesList = new() {
            Type.MORPHEME,
            Type.COMPOUND,
            Type.INFLECT,
            Type.PREANALYSIS
        };

        public static List<Tag> TagsList = new() {
            new() { Code = 100, Description = "Verbal endings" },
            new() { Code = 110, Description = "Interjection" },
            new() { Code = 120, Description = "Ending Particle" },
            new() { Code = 130, Description = "General Adverb" },
            new() { Code = 131, Description = "Conjunctive adverb" },
            new() { Code = 140, Description = "Modifier" },
            new() { Code = 150, Description = "General Noun" },
            new() { Code = 151, Description = "Proper Noun" },
            new() { Code = 152, Description = "Dependent noun" },
            new() { Code = 153, Description = "Dependent noun" },
            new() { Code = 154, Description = "Pronoun" },
            new() { Code = 155, Description = "Numeral" },
            new() { Code = 160, Description = "Terminal punctuation" },
            new() { Code = 161, Description = "Chinese Characeter" },
            new() { Code = 162, Description = "Foreign language" },
            new() { Code = 163, Description = "Number" },
            new() { Code = 164, Description = "Space" },
            new() { Code = 165, Description = "Closing brackets" },
            new() { Code = 166, Description = "Opening brackets" },
            new() { Code = 167, Description = "Separator" },
            new() { Code = 168, Description = "Other symbol" },
            new() { Code = 169, Description = "Ellipsis" },
            new() { Code = 170, Description = "Adjective" },
            new() { Code = 171, Description = "Negative designator" },
            new() { Code = 172, Description = "Positive designator" },
            new() { Code = 173, Description = "Verb" },
            new() { Code = 174, Description = "Auxiliary Verb or Adjective" },
            new() { Code = 181, Description = "Prefix" },
            new() { Code = 182, Description = "Root" },
            new() { Code = 183, Description = "Adjective Suffix" },
            new() { Code = 184, Description = "Noun Suffix" },
            new() { Code = -185, Description = "Verb" },
            new() { Code = -999, Description = "Unknown" },
            new() { Code = -1, Description = "Unknown" },
            new() { Code = -1, Description = "Unknown" },
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

        public static Tag ResolveTag(byte tag)
        {
            short tagVal = buffer.GetInt16(tag);
            if (tagVal < Tags.Count)
            {
                return TagsList[tagVal];
            }
            return Tags["UNKNOWN"];
        }

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