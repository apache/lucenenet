using Html2Markdown.Replacement;
using HtmlAgilityPack;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace JavaDocToMarkdownConverter.Formatters
{
    /// <summary>
    /// For some reason the normal markdown converter doesn't get a few html elements so this removes those
    /// </summary>
    public class ExtraHtmlElementReplacer : IReplacer
    {
        private static readonly Regex MetaTag = new Regex(@"<meta\s+.*?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex TitleTag = new Regex(@"<title>.*?</title>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Regex HeadTag = new Regex(@"<head>.*?</head>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        private static readonly Regex BodyStart = new Regex(@"<body>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex BodyEnd = new Regex(@"</body>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex HtmlStart = new Regex(@"<html>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex HtmlEnd = new Regex(@"</html>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Replace(string html)
        {
            foreach(var r in new[] { MetaTag, TitleTag, HeadTag,BodyStart, BodyEnd, HtmlStart, HtmlEnd })
            {
                html = r.Replace(html, string.Empty);
            }

            return html;
            

            //var htmlDoc = new HtmlDocument();
            //using (var input = new StringReader(html))
            //{
            //    htmlDoc.Load(input);
            //}

            //foreach(var e in HtmlElements)
            //{
            //    RemoveElements(htmlDoc, e);
            //}

            //var sb = new StringBuilder();
            //using (var output = new StringWriter(sb))
            //{
            //    htmlDoc.Save(output);
            //}
            //return sb.ToString();
        }

        //private void RemoveElements(HtmlDocument htmlDoc, string elementMatch)
        //{
        //    var metaTags = htmlDoc.DocumentNode.SelectNodes(elementMatch);
        //    if (metaTags == null) return;
        //    foreach (var m in metaTags)
        //    {
        //        m.ParentNode.RemoveChild(m);
        //    }
        //}
    }
}
