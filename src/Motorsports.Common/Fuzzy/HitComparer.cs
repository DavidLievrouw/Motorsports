namespace Motorsports.Common.Fuzzy;

internal sealed class HitComparer : IComparer<(int Score, int Index)>
{
    public int Compare((int Score, int Index) x, (int Score, int Index) y)
    {
        var c = y.Score.CompareTo(x.Score);
        return c != 0 ? c : x.Index.CompareTo(y.Index);
    }
}
