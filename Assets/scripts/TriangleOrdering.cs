using System.Collections.Generic;
using UnityEngine;

public class TriangleOrdering {
    private List<Vector3> vertices;
    private Dictionary<int, List<MPolygon>> polyAtTriangle;
    private List<HashSet<int>> bounds;
    private Dictionary<int, int> cutNodes;
    private Dictionary<int, int> cutPointsToVertices;
    private Pair<List<MPolygon>> sidesOfCut;

    public TriangleOrdering (List<Vector3> vertices, Dictionary<int, List<MPolygon>> polyAtTriangle, List<HashSet<int>> bounds, Dictionary<int, int> cutNodes, Dictionary<int, int> cutPointsToVertices, Pair<List<MPolygon>> sidesOfCut) {
        this.vertices = vertices;
        this.polyAtTriangle = polyAtTriangle;
        this.bounds = bounds;
        this.cutNodes = cutNodes;
        this.cutPointsToVertices = cutPointsToVertices;
        this.sidesOfCut = sidesOfCut;
    }

    public Dictionary<int, List<MPolygon>> orderTriangles (out Pair<HashSet<int>> sideBounds) {
        var polygonOwners = new Dictionary<MPolygon, int> ();

        sideBounds = new Pair<HashSet<int>> (new HashSet<int> (), new HashSet<int> ());
        foreach (var polygon in sidesOfCut.left) {
            var owner = GetOwnerOfPolygon (bounds, cutNodes, polygon);
            polygonOwners[polygon] = owner;
            if (owner >= 0) {
                sideBounds.left.Add (owner);
            }
            // DebugAtPolygon (vertices, polygon, Color.blue);
        }
        foreach (var polygon in sidesOfCut.right) {
            var owner = GetOwnerOfPolygon (bounds, cutNodes, polygon);
            polygonOwners[polygon] = owner;
            if (owner >= 0) {
                sideBounds.right.Add (owner);
            }
            // DebugAtPolygon (vertices, polygon, Color.red);
        }

        var unownedPolygons = new HashSet<MPolygon> ();
        foreach (var item in polyAtTriangle.Keys) {
            unownedPolygons.UnionWith (polyAtTriangle[item]);
        }
        unownedPolygons.ExceptWith (sidesOfCut.left);
        unownedPolygons.ExceptWith (sidesOfCut.right);

        foreach (var polygon in unownedPolygons) {
            var owner = GetOwnerOfPolygon (bounds, cutNodes, polygon);
            polygonOwners[polygon] = owner;
            if (sideBounds.left.Contains (owner)) {
                sidesOfCut.left.Add (polygon);
            } else if (sideBounds.right.Contains (owner)) {
                sidesOfCut.right.Add (polygon);
            } else {
                Debug.LogError ("Still no owner found for " + polygon);
                foreach (var item in polygon) {
                    Misc.DebugSphere (vertices[item], Color.black, " Polygon " + polygon + " at " + item);
                }
            }
        }

        var polygons = new Dictionary<int, List<MPolygon>> ();
        polygons[0] = new List<MPolygon> ();
        polygons[1] = new List<MPolygon> ();
        foreach (var polygon in sidesOfCut.left) {
            List<MPolygon> newPolys = new List<MPolygon> ();
            polygon.Reduce (vertices);
            if (polygon.Count > 3) {
                newPolys = polygon.Triangulate ();
            } else {
                newPolys.Add (polygon);
            }
            polygons[0].AddRange (newPolys);
        }

        foreach (var polygon in sidesOfCut.right) {
            List<MPolygon> newPolys = new List<MPolygon> ();
            polygon.Reduce (vertices);
            if (polygon.Count > 3) {
                newPolys = polygon.Triangulate ();
            } else {
                newPolys.Add (polygon);
            }
            polygons[1].AddRange (newPolys);
        }

        return polygons;
    }
    private void DebugAtPolygon (List<Vector3> vertices, MPolygon t, Color c) {
        Vector3 barycenter = (vertices[t[0]] + vertices[t[1]] + vertices[t[2]]) / 3;
        Misc.DebugSphere (barycenter, c, "Hit: " + t.ToString ());
    }
    private int GetOwnerOfPolygon (Dictionary<int, List<MPolygon>> polyAtTriangle, Dictionary<MPolygon, int> polygonOwners, Dictionary<int, int> cutPointsToVertices, MPolygon polygon) {
        List<PolygonSide> realNeighborSides;
        List<PolygonSide> fakeNeighborSides;
        getNeighborSides (polyAtTriangle, cutPointsToVertices, polygon, out realNeighborSides, out fakeNeighborSides);
        foreach (var neighborSide in realNeighborSides) {
            var neighbors = getNeighbors (polyAtTriangle, neighborSide);
            neighbors.Remove (polygon);
            foreach (var neighbor in neighbors) {
                int neighborOwner = polygonOwners[neighbor];
                if (neighborOwner >= 0) {
                    return neighborOwner;
                }
            }
        }
        return -1;
    }

    private int GetOwnerOfPolygon (List<HashSet<int>> bounds, Dictionary<int, int> cutNodes, MPolygon polygon) {
        int result = -1;
        foreach (var vertex in polygon) {
            if (!cutNodes.ContainsValue (vertex)) {
                for (int i = 0; i < bounds.Count; i++) {
                    var boundary = bounds[i];
                    if (boundary.Contains (vertex)) {
                        if (result == -1 || result == i) {
                            result = i;
                        } else {
                            Debug.LogWarning ("Vertex " + vertex + " is Part of multiple bounds " + polygon.ToString ());
                            return -2;
                        }
                    }
                }
            }
        }
        if (result != -1) {
            return result;
        } else {
            // Debug.LogWarning ("No Owner found for " + polygon.ToString ());
            // DebugAtPolygon (vertices, polygon, Color.black);
            return -1;
        }
    }
    private List<MPolygon> getNeighbors (Dictionary<int, List<MPolygon>> polyAtTriangle, PolygonSide neighborSide) {
        List<MPolygon> retList = new List<MPolygon> ();
        foreach (var key in polyAtTriangle.Keys) {
            foreach (var polygon in polyAtTriangle[key]) {
                // if (polygon.Contains (neighborSide.left)) {
                //     Misc.DebugList ("Candidate for " + neighborSide, polygon);
                // }
                if (polygon.ContainsEdge (neighborSide)) {
                    retList.Add (polygon);
                }
            }
        }
        return retList;
    }

    private void getNeighborSides (Dictionary<int, List<MPolygon>> polyAtTriangle, Dictionary<int, int> cutPointsToVertices, MPolygon polygon, out List<PolygonSide> realNeighbors, out List<PolygonSide> fakeNeighbors) {
        realNeighbors = new List<PolygonSide> ();
        fakeNeighbors = new List<PolygonSide> ();
        var sides = polygon.GetSides ();
        foreach (var side in sides) {
            if (IsSeparatingEdge (cutPointsToVertices, side)) {
                fakeNeighbors.Add (side);
            } else {
                realNeighbors.Add (side);
            }
        }
    }

    private static bool IsSeparatingEdge (Dictionary<int, int> cutPointsToVertices, PolygonSide side) {
        int cp1, cp2;
        cp1 = -2;
        if (cutPointsToVertices.ContainsKey (side.left)) {
            cp1 = cutPointsToVertices[side.left];
        }
        cp2 = -2;
        if (cutPointsToVertices.ContainsKey (side.right)) {
            cp2 = cutPointsToVertices[side.right];
        }
        return Mathf.Abs (cp1 - cp2) == 1;
    }

}