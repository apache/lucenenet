namespace Lucene.Net.Search.Similarities
{
    public abstract class Lambda
    {
        // .NET Port : this was originally named "lambda" but switching 
        // to the .NET naming convention causes collison between the 
        // member name and the name of the enclosing type
        public abstract float CalculateLambda(BasicStats stats);
        
        public abstract Explanation Explain(BasicStats stats);

        public abstract override string ToString();
    }
}