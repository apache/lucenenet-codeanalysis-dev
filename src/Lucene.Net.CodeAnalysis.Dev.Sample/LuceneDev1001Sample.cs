using System.Globalization;

namespace Lucene.Net.CodeAnalysis.Dev.Sample;

public class LuceneDev1001Sample
{
    private readonly float float1 = 1f;

    public void MyMethod()
    {
        string result = float1.ToString(CultureInfo.InvariantCulture);
    }
}
