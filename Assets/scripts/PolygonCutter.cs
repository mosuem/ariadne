using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PolygonCutter {
    internal List<Vector3> normals;
    private List<int> meshTriangles;
    private List<Vector3> vertices;
    private Dictionary<int, List<MPolygon>> polyAtTriangle;
    internal Dictionary<int, int> cutPointVertices = new Dictionary<int, int> ();

    public PolygonCutter (List<int> meshTriangles, List<Vector3> normals, List<Vector3> vertices, Dictionary<int, List<MPolygon>> polyAtTriangle) {
        this.vertices = vertices;
        this.polyAtTriangle = polyAtTriangle;
        this.normals = normals;
        this.meshTriangles = meshTriangles;
    }

    private MTriangle GetTriangle (int index) {
        var t = new MTriangle ();
        if (meshTriangles.Count < (index * 3 + 2) || index == -1) {
            Debug.LogWarning ("Trying to access " + index + " but " + (index * 3 + 2) + " > " + meshTriangles.Count);
        }
        t[0] = meshTriangles[index * 3 + 0];
        t[1] = meshTriangles[index * 3 + 1];
        t[2] = meshTriangles[index * 3 + 2];
        return t;
    }

    internal Pair<List<MPolygon>> Cut (List<Vector3> cutPoints, Dictionary<int, PolygonSide> cutEdges, Dictionary<int, int> cutNodes, Dictionary<int, int> cutTriangles) {
        var lhs = new List<MPolygon> ();
        var rhs = new List<MPolygon> ();
        for (int i = 1; i < cutPoints.Count + 1; i++) {
            PolygonSide pairEntry = null;
            PolygonSide pairExit = null;
            int vertexEntry = -1;
            int vertexExit = -1;

            int bIndex = i - 1;
            var pointEntry = cutPoints[bIndex];
            if (cutEdges.ContainsKey (bIndex)) {
                pairEntry = cutEdges[bIndex];
            } else if (cutNodes.ContainsKey (bIndex)) {
                vertexEntry = cutNodes[bIndex];
                // Debug.Log ("Vertex entry not null at " + vertexEntry);
            }

            int tIndex = i == cutPoints.Count ? 0 : i;
            var pointExit = cutPoints[tIndex];
            if (cutEdges.ContainsKey (tIndex)) {
                pairExit = cutEdges[tIndex];
            } else if (cutNodes.ContainsKey (tIndex)) {
                vertexExit = cutNodes[tIndex];
                // Debug.Log ("Vertex exit not null at " + vertexExit);
            }
            List<MPolygon> newPolys = null;
            var triangle = -1;
            MPolygon remove = null;

            if (pairEntry != null && vertexExit != -1) {
                // Debug.Log ("Cut from " + pairEntry + " to " + vertexExit + " at triangle " + tIndex);
                triangle = cutTriangles[tIndex];
                var cutPointIndices = CutUpPolygons (triangle, pairEntry, pointEntry, vertexExit, pointExit, out newPolys, out remove);
                if (cutPointIndices.Count == 1) {
                    cutPointVertices[cutPointIndices[0]] = tIndex;
                    lhs.Add (newPolys[1]);
                    rhs.Add (newPolys[0]);
                    if (newPolys[0].Contains (1370)) {
                        Debug.Log ("p-v Adding " + newPolys[0] + " to rhs and " + newPolys[1] + " to lhs");
                    }
                } else {
                    // Debug.Log ("Same entry and exit at p-v " + pairEntry + " to " + vertexExit + " with " + cutTriangles[bIndex] + " and " + cutTriangles[tIndex] + " " + isLHS (GetTriangle (triangle), pairEntry, vertexExit));
                    // if (isLHS (GetTriangle (triangle), pairEntry, vertexExit)) {
                    //     lhs.AddRange (polyAtTriangle[cutTriangles[tIndex]]);
                    //     foreach (var item in polyAtTriangle[cutTriangles[tIndex]]) {
                    //         if (item.Contains (1299)) {
                    //             Debug.Log ("p-v Adding " + item);
                    //         }
                    //     }
                    // } else {
                    //     rhs.AddRange (polyAtTriangle[cutTriangles[tIndex]]);
                    // }
                }
            } else if (vertexEntry != -1 && pairExit != null) {
                // Debug.Log ("Cut from " + vertexEntry + " to " + pairExit);
                triangle = cutTriangles[tIndex];
                var cutPointIndices = CutUpPolygons (triangle, pairExit, pointExit, vertexEntry, pointEntry, out newPolys, out remove);
                if (cutPointIndices.Count == 1) {
                    cutPointVertices[cutPointIndices[0]] = tIndex;
                    lhs.Add (newPolys[0]);
                    rhs.Add (newPolys[1]);
                    if (newPolys[1].Contains (1370)) {
                        Debug.Log ("v-p Adding " + newPolys[1] + " to rhs and " + newPolys[0] + " to lhs");
                    }
                } else {
                    // Debug.Log ("Same entry and exit at v-p " + pairExit + " to " + vertexEntry + " with " + cutTriangles[bIndex] + " and " + cutTriangles[tIndex] + " " + isLHS (GetTriangle (triangle), pairExit, vertexEntry));
                    // if (isLHS (GetTriangle (triangle), pairExit, vertexEntry)) {
                    //     lhs.AddRange (polyAtTriangle[cutTriangles[tIndex]]);
                    //     foreach (var item in polyAtTriangle[cutTriangles[tIndex]]) {
                    //         if (item.Contains (1299)) {
                    //             Debug.Log ("v-p Adding " + item);
                    //         }
                    //     }
                    // } else {
                    //     rhs.AddRange (polyAtTriangle[cutTriangles[tIndex]]);
                    // }
                }
            } else if (vertexEntry != -1 && vertexExit != -1) {
                //Nothing
            } else if (pairEntry != null && pairExit != null) {
                if (!pairEntry.SameAs (pairExit)) {
                    triangle = cutTriangles[tIndex];
                    var cutPointIndices = CutUpPolygons (triangle, pairEntry, pairExit, pointEntry, pointExit, out newPolys, out remove);
                    if (cutPointIndices.Count == 2) {
                        cutPointVertices[cutPointIndices[0]] = tIndex;
                        cutPointVertices[cutPointIndices[1]] = bIndex;
                        lhs.Add (newPolys[0]);
                        rhs.Add (newPolys[1]);
                        if (newPolys[1].Contains (1335)) {
                            Debug.Log ("p-p Adding " + newPolys[1] + " to rhs and " + newPolys[0] + " to lhs");
                        }
                    }
                } else {
                    // Debug.Log ("Same entry and exit at " + pairEntry + " to " + pairExit + " with " + cutTriangles[bIndex] + " and " + cutTriangles[tIndex]);
                    // Misc.DebugSphere (vertices[pairEntry.left], Color.white, "Entry " + pairEntry + " left");
                    // Misc.DebugSphere (vertices[pairEntry.right], Color.white, "Entry " + pairEntry + " right");
                    // rhs.AddRange (polyAtTriangle[cutTriangles[bIndex]]);
                    // foreach (var item in polyAtTriangle[cutTriangles[bIndex]]) {
                    //     if (item.Contains (1297)) {
                    //         Debug.Log ("e-e Adding " + item + " to rhs");
                    //     }
                    // }
                    // lhs.AddRange (polyAtTriangle[cutTriangles[tIndex]]);
                }
            }

            if (newPolys != null) {
                polyAtTriangle[triangle].Remove (remove);
                lhs.Remove (remove);
                rhs.Remove (remove);
                polyAtTriangle[triangle].AddRange (newPolys);
            }
        }
        return new Pair<List<MPolygon>> (lhs, rhs);
    }

    private bool isLHS (MTriangle mTriangle, PolygonSide pairEntry, int vertexExit) {
        foreach (var side in mTriangle.GetSides ()) {
            if (pairEntry.SameAs (side)) {
                if (vertexExit == side.left) {
                    return false;
                } else if (vertexExit == side.right) {
                    return true;
                }
            }
        }
        return false;
    }

    private List<int> CutUpPolygons (int triangle, PolygonSide pairEntry, PolygonSide pairExit, Vector3 pointEntry, Vector3 pointExit, out List<MPolygon> newPolys, out MPolygon remove) {
        var polys = polyAtTriangle[triangle];
        newPolys = null;
        remove = null;
        var cutPointIndices = new List<int> ();
        for (int j = 0; j < polys.Count; j++) {
            var polygon = polys[j];
            var numNewVerts = -1;

            bool entryInPolygon = isOnPolygonSide (pointEntry, polygon, out pairEntry);
            bool exitInPolygon = isOnPolygonSide (pointExit, polygon, out pairExit);
            if (entryInPolygon != exitInPolygon) {
                Debug.LogWarning ("Entry but not Exit in Polygon (or exit but not entry. This probably means the polygon was cross-cutted.");
            }
            if (entryInPolygon && exitInPolygon) {
                remove = polygon;
                numNewVerts = CutPolygon (polygon, out newPolys, pairEntry, pairExit);
            }

            if (numNewVerts > 0) {
                var newV1 = vertices.Count;
                var newV2 = vertices.Count + 1;
                if (!vertices.Contains (pointExit)) {
                    vertices.Add (pointExit);
                    normals.Add (GetMiddleNormal (pairExit, pointExit));
                    cutPointIndices.Add (newV1);
                } else {
                    var newIndex = vertices.IndexOf (pointExit);
                    foreach (var pol in newPolys) {
                        int index = pol.IndexOf (newV1);
                        pol.RemoveAt (index);
                        pol.Insert (index, newIndex);
                    }
                    cutPointIndices.Add (newIndex);
                }

                if (!vertices.Contains (pointEntry)) {
                    vertices.Add (pointEntry);
                    normals.Add (GetMiddleNormal (pairEntry, pointEntry));
                    cutPointIndices.Add (newV2);
                } else {
                    var newIndex = vertices.IndexOf (pointEntry);
                    foreach (var pol in newPolys) {
                        int index = pol.IndexOf (newV2);
                        pol.RemoveAt (index);
                        pol.Insert (index, newIndex);
                    }
                    cutPointIndices.Add (newIndex);
                }
                break;
            }
        }
        return cutPointIndices;
    }

    private Vector3 GetMiddleNormal (PolygonSide pair, Vector3 point) {
        float t = Vector3.Distance (vertices[pair.left], point) / Vector3.Distance (vertices[pair.left], vertices[pair.right]);
        Vector3 middleNormal = Vector3.Lerp (normals[pair.left], normals[pair.right], t);
        return middleNormal;
    }

    private List<int> CutUpPolygons (int triangle, PolygonSide side, Vector3 pointSide, int corner, Vector3 pointCorner, out List<MPolygon> newPolys, out MPolygon remove) {
        var polys = polyAtTriangle[triangle];
        var cutPointIndices = new List<int> ();
        newPolys = null;
        remove = null;
        if (!LinearAlgebra.isInSegment (pointCorner, vertices[side.left], vertices[side.right])) {
            for (int j = 0; j < polys.Count; j++) {
                var polygon = polys[j];
                var numNewVerts = -1;

                bool entryInPolygon = isPolygonCorner (pointCorner, polygon, out corner);
                bool exitInPolygon = isOnPolygonSide (pointSide, polygon, out side);

                if (entryInPolygon && exitInPolygon) {
                    remove = polygon;
                    numNewVerts = CutPolygon (polygon, out newPolys, side, corner);
                }

                if (numNewVerts > 0) {
                    var newV1 = vertices.Count;
                    if (!vertices.Contains (pointSide)) {
                        vertices.Add (pointSide);
                        normals.Add (GetMiddleNormal (side, pointSide));
                        cutPointIndices.Add (newV1);
                    } else {
                        var newIndex = vertices.IndexOf (pointSide);
                        foreach (var pol in newPolys) {
                            int index = pol.IndexOf (newV1);
                            pol.RemoveAt (index);
                            pol.Insert (index, newIndex);
                        }
                        cutPointIndices.Add (newIndex);
                    }
                    break;
                }
            }
        }
        return cutPointIndices;
    }

    private bool isOnPolygonSide (Vector3 intersection, MPolygon polygon, out PolygonSide intersectSide) {
        foreach (var side in polygon.GetSides ()) {
            if (LinearAlgebra.isInSegment (intersection, vertices[side.left], vertices[side.right])) {
                intersectSide = side;
                return true;
            }
        }
        intersectSide = null;
        return false;
    }

    private bool isPolygonCorner (Vector3 intersection, MPolygon polygon, out int intersectCorner) {
        foreach (var corner in polygon) {
            if (LinearAlgebra.IsNear (intersection, vertices[corner])) {
                intersectCorner = corner;
                return true;
            }
        }
        intersectCorner = -1;
        return false;
    }

    private int CutPolygon (MPolygon polygon, out List<MPolygon> newPolys, PolygonSide sideEntry, PolygonSide sideExit) {
        return polygon.Cut (sideEntry, sideExit, out newPolys, vertices.Count);
    }

    private int CutPolygon (MPolygon polygon, out List<MPolygon> newPolys, PolygonSide side, int corner) {
        return polygon.Cut (side, corner, out newPolys, vertices.Count);
    }
}