namespace Lucene.Net.CodeAnalysis.Dev.Sample;

public class LuceneDev1002Sample
{
    private readonly float float1 = 1f;
    private readonly float float2 = 3.14f;

    public void MyMethod()
    {
        long foo = 33;
        var result = ((double)float1 * (double)float2) / foo;
    }
}
