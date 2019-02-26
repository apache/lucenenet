namespace JavaDocToMarkdownConverter.Formatters
{

    //This is exposed in the newer version of Html2Markdown but the later versions don't parse correctly so we have 
    //to remain on our current version and just do this ourselves. 
    public interface IReplacer
    {
        string Replace(string html);
    }
}
