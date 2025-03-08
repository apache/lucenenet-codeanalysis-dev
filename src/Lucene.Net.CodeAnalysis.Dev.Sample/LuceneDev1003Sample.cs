namespace Lucene.Net.CodeAnalysis.Dev.Sample;

public class LuceneDev1003Sample
{
    public static bool ParseChar(string id, int[] pos, char ch)
    {
        int start = pos[0];
        //pos[0] = PatternProps.SkipWhiteSpace(id, pos[0]);
        if (pos[0] == id.Length ||
            id[pos[0]] != ch)
        {
            pos[0] = start;
            return false;
        }
        ++pos[0];
        return true;
    }
}
