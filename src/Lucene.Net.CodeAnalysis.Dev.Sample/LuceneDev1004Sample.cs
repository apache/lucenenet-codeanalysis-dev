namespace Lucene.Net.CodeAnalysis.Dev.Sample;

public class LuceneDev1004Sample
{
    public static byte[] GetVersionByteArrayFromCompactInt32(int version) // ICU4N specific - Renamed from GetVersionByteArrayFromCompactInt
    {
        return new byte[] {
            (byte)(version >> 24),
            (byte)(version >> 16),
            (byte)(version >> 8),
            (byte)(version)
        };
    }
}
