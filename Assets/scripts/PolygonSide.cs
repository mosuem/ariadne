public class PolygonSide : Pair<int> {
    private Pair<int> pair;

    public PolygonSide (Pair<int> pair) : base (pair.left, pair.right) { }

    public PolygonSide (int left, int right) : base (left, right) { }

    new public PolygonSide Reversed () {
        return new PolygonSide (base.Reversed ());
    }
}