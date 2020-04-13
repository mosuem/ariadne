using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CutPointIdentifier {
    private Graph g;
    private Manifold manifold;
    private Mesh mesh;
    private List<Vector3> vertices;

    public CutPointIdentifier (Graph g, Manifold manifold) {
        this.g = g;
        this.manifold = manifold;
        this.mesh = manifold.gameObject.GetComponent<MeshFilter> ().mesh;
        this.vertices = mesh.vertices.ToList ();
        this.triangles = mesh.triangles;
    }
    internal List<Vector3> cutPoints;
    internal Dictionary<int, PolygonSide> cutEdges;
    internal Dictionary<int, int> cutTriangles;
    internal List<string> cutPointType;
    internal Dictionary<int, int> cutNodes;
    internal Dictionary<int, PolygonSide> cutAlongEdges;
    internal Dictionary<int, int> onVertex;
    int[] triangles;

    string sside = "Side";
    string snode = "Node";
    private List<Vector3> onMeshPositions;
    private List<int> triangleIndices;

    internal bool IdentifyCuts (MPath path) {
        onMeshPositions = new List<Vector3> ();
        triangleIndices = new List<int> ();
        onVertex = new Dictionary<int, int> ();
        int indexNotNeeded;
        var oldVertex = isNearVertex (OnMeshTriangle (path.Count - 1, path, out indexNotNeeded));
        for (int i = 0; i < path.Count; i++) {
            int index;
            Vector3 onMeshPosition = OnMeshTriangle (i, path, out index);
            int vertex = isNearVertex (onMeshPosition);
            if (vertex != -1) {
                if (vertex != oldVertex) {
                    Debug.Log ("Move vertex " + i + " to " + vertex);
                    onMeshPositions.Add (vertices[vertex]);
                    // Misc.DebugSphere (onMeshPositions.Last (), Color.red, "On Mesh On Vertex" + i);
                    triangleIndices.Add (index);
                    onVertex[onMeshPositions.Count - 1] = vertex;
                }
            } else {
                onMeshPositions.Add (onMeshPosition);
                triangleIndices.Add (index);
                // Misc.DebugSphere (onMeshPositions.Last (), Color.red, "On Mesh " + i);
                // DebugAtTriangle (index, Color.blue);
            }
            oldVertex = vertex;
        }

        // for (int i = 0; i < onMeshPositions.Count; i++) {
        //     Misc.DebugSphere (onMeshPositions[i], Color.red, "Position " + i + " in triangle " + triangleIndices[i].First () + "/" + triangleIndices[i].Count);
        // }

        cutAlongEdges = new Dictionary<int, PolygonSide> ();
        var cutThroughTriangles = new Dictionary<int, Pair<PolygonSide>> ();
        cutPointType = new List<string> ();
        // var sinside = "Inside";
        cutPoints = new List<Vector3> ();
        cutEdges = new Dictionary<int, PolygonSide> ();
        cutNodes = new Dictionary<int, int> ();
        cutTriangles = new Dictionary<int, int> ();
        for (int i = 1; i < onMeshPositions.Count; i++) {
            // bool addedCutPoint = false;
            GetIntersection (i);
        }
        // Set cutAlongEdges
        for (int i = 1; i < cutPoints.Count; i++) {
            if (cutEdges.ContainsKey (i - 1) && cutEdges.ContainsKey (i)) {
                var e1 = cutEdges[i - 1];
                var e2 = cutEdges[i];
                if (e1.SameAs (e2)) {
                    Debug.Log ("Cut edges are same for " + i + ", as they are " + e1 + " and " + e2);
                    // cutAlongEdges[i] = new Pair<int> (i - 1, i);
                    cutAlongEdges[i] = e1;
                }
            }
            // Misc.DebugSphere (cutPoints[i], Color.red, "Cut Point " + i + " " + cutPointType[i]);
        }
        //INSERT MID CUTPOINTS
        // for (int i = 0; i < cutPoints.Count; i++) {
        //     bool insertMid = false;
        //     if (cutEdges.ContainsKey (i - 1) && cutEdges.ContainsKey (i)) {
        //         var e1 = cutEdges[i - 1];
        //         var e2 = cutEdges[i];
        //         if (e1.SameAs (e2)) {
        //             Debug.Log ("Cut edges are same for " + i + ", as they are " + e1 + " and " + e2);
        //             insertMid = true;
        //         }
        //     } else if (cutEdges.ContainsKey (i - 1) && cutNodes.ContainsKey (i)) {
        //         var e1 = cutEdges[i - 1];
        //         if (e1.Contains (cutNodes[i])) {
        //             insertMid = true;
        //         }
        //     } else if (cutNodes.ContainsKey (i - 1) && cutEdges.ContainsKey (i)) {
        //         var e1 = cutEdges[i];
        //         if (e1.Contains (cutNodes[i - 1])) {
        //             insertMid = true;
        //         }
        //     }

        //     if (insertMid) {
        //         int c1 = path.line.GetNearestPointTo (cutPoints[i - 1]);
        //         int c2 = path.line.GetNearestPointTo (cutPoints[i]);
        //         Debug.Log ("Insert point at " + i);
        //         AddCutPoint (cutPoints, cutPointType, onMeshPositions[(c1 + c2) / 2], sinside);
        //     }
        // }
        return true;
    }

    private int getNeighboringTriangle (int vertex, MTriangle mTriangle) {
        var triangleCandidates = g.getTrianglesAt (vertex);
        foreach (var t in triangleCandidates) {
            PolygonSide side;
            if (mTriangle.isNeighbor (GetTriangle (t), out side)) {
                Debug.Log ("Is neighbor at side " + side);
                return t;
            }
        }
        Debug.LogError ("Not a neighbor");
        return -1;
    }

    private bool GetIntersection (int i) {
        var start = onMeshPositions[i - 1];
        var end = onMeshPositions[i];

        if (onVertex.ContainsKey (i - 1) && onVertex.ContainsKey (i)) {
            // AddOnVertexCut (i - 1);
            // AddOnVertexCut (i); //Shouldn't happen
            Debug.LogError ("Both on Cut at " + i);
        } else if (!onVertex.ContainsKey (i - 1) && onVertex.ContainsKey (i)) {
            int t = cutTriangles.Count > 0 ? cutTriangles[cutPoints.Count - 1] : -1;
            var index = AddOnVertexCut (i);
            Debug.Log ("Cut to v " + (i - 1) + " to " + i + ", adding " + triangleIndices[i - 1] + " to cuttriangles at " + index);
            var v = triangleIndices[i - 1];
            if (t >= 0) {
                v = getNeighboringTriangle (onVertex[i], GetTriangle (t));
            }
            cutTriangles[index] = v;
            // Misc.DebugSphere (cutPoints[index], Color.red, "Cut Point e-v " + i);
        } else if (onVertex.ContainsKey (i - 1) && !onVertex.ContainsKey (i)) {
            // var index = AddOnVertexCut (i - 1);
            // Debug.Log ("Cut from v " + (i - 1) + " to " + i + ", adding " + v + " to cuttriangles at " + index);
            // cutTriangles[index] = v;
            // Misc.DebugSphere (cutPoints[index], Color.red, "Cut Point v-e " + i);
        } else {
            int t1 = triangleIndices[i - 1];
            int t2 = triangleIndices[i];
            if (t1 != t2) {
                PolygonSide neighbor;
                var counter = 0;
                PolygonSide closestSide = null;
                while (!haveCommonSide (t1, t2, out neighbor) && ++counter < 12) {
                    var closestCutPoint = FindPointOnTriangle (t1, start, end, ref closestSide);
                    int index = AddCutPoint (closestCutPoint, sside);
                    // Misc.DebugSphere (cutPoints[index], Color.red, "Cut Point e-e " + t1 + " " + t2);
                    cutTriangles[index] = t1;
                    cutEdges[index] = closestSide;

                    t1 = GetOtherTriangle (t1, closestSide);
                    start = closestCutPoint;
                }
                if (counter > 9) {
                    Debug.LogError ("Still no common side between " + triangleIndices[i - 1] + " and " + triangleIndices[i]);
                    DebugAtTriangle (triangleIndices[i - 1], Color.white);
                    DebugAtTriangle (triangleIndices[i], Color.black);
                }
                var cutIndex = FindCutPoint (start, end, neighbor);
                cutTriangles[cutIndex] = t1;
            }
        }
        return true;
    }

    private int AddOnVertexCut (int i) {
        var cp = vertices[onVertex[i]];
        if (!cutPoints.Contains (cp)) {
            var index = AddCutPoint (cp, snode);
            cutNodes[index] = onVertex[i];
        }
        return cutPoints.IndexOf (cp);
    }

    private void GetIntersection (Vector3 start, Vector3 end, int t1, int t2, PolygonSide neighbor) {

    }

    private Vector3 FindPointOnTriangle (int t1, Vector3 start, Vector3 end, ref PolygonSide closestSide) {
        var triangle = GetTriangle (t1);
        var sides = triangle.GetSides ();
        var minDist = float.MaxValue;
        Vector3 closestCutPoint = default (Vector3);
        foreach (var side in sides) {
            if (!side.SameAs (closestSide)) {
                float distance;
                var minCutPoint = LinearAlgebra.getClosest (start, end, vertices[side.left], vertices[side.right], out distance);
                // Misc.DebugSphere (closestIntersection, Color.magenta, "Intersection point on Side for " + hit1);
                if (distance < minDist) {
                    closestCutPoint = minCutPoint;
                    closestSide = side;
                    minDist = distance;
                }
            }
        }
        return closestCutPoint;
    }

    private int FindCutPoint (Vector3 start, Vector3 end, PolygonSide neighbor) {
        Vector3 p0 = vertices[neighbor.left];
        Vector3 p1 = vertices[neighbor.right];
        float d;
        var cutPoint = LinearAlgebra.getClosest (start, end, p0, p1, out d);
        int index = AddCutPoint (cutPoint, sside);
        cutEdges[index] = neighbor;
        return index;
    }

    private int AddCutPoint (Vector3 cp, string type) {
        cutPoints.Add (cp);
        cutPointType.Add (type);
        return cutPoints.Count - 1;
    }

    private int isNearVertex (Vector3 onMeshPosition) {
        for (int i = 0; i < vertices.Count; i++) {
            Vector3 vertex = (Vector3) vertices[i];
            float epsilon = 0.05f;
            if (Vector3.Distance (vertex, onMeshPosition) < epsilon) {
                return i;
            }
        }
        return -1;
    }

    Vector3 OnMeshTriangle (int i, MPath path, out int triangleIndex) {
        RaycastHit hit;
        // Debug.Log ("At point " + i + " normals " + path.line.GetNormals ().Count + " path.line " + path.Count);
        var normal = i < path.Count ? path.GetNormal (i) : path.GetNormal (0);
        var position = i < path.Count ? path.GetPosition (i) : path.GetPosition (0);
        Ray ray = new Ray (position, -normal);
        if (manifold.gameObject.GetComponent<MeshCollider> ().Raycast (ray, out hit, 0.5f)) {
            triangleIndex = hit.triangleIndex;
            return hit.point;
        } else {
            Debug.LogWarning ("Warning in OnMesh: Raycast not working, returning closest Point instead");
            triangleIndex = -1;
            return GameObject.FindGameObjectWithTag ("Sphere").GetComponent<MeshCollider> ().ClosestPoint (position);
        }
    }

    private bool haveCommonSide (int hit1, int hit2, out PolygonSide neighbor) {
        var t1 = GetTriangle (hit1);
        var t2 = GetTriangle (hit2);
        if (t1.isNeighbor (t2, out neighbor)) {
            return true;
        } else {
            return false;
        }
    }

    private MTriangle GetTriangle (int index) {
        var t = new MTriangle ();
        if (triangles.Length < (index * 3 + 2) || index == -1) {
            Debug.LogWarning ("Trying to access " + index + " but " + (index * 3 + 2) + " > " + triangles.Length);
        }
        t[0] = triangles[index * 3 + 0];
        t[1] = triangles[index * 3 + 1];
        t[2] = triangles[index * 3 + 2];
        return t;
    }

    private int GetOtherTriangle (int hit1, PolygonSide closestSide) {
        var two = g.getTriangles (closestSide.left, closestSide.right);
        // Debug.Log("Triangles for "  + hit1 + ": " g);
        // Misc.DebugList<int> ("Triangles for " + hit1 + ": ", two);
        two.Remove (hit1);
        if (two.Count > 1) {
            Debug.LogWarning ("In GetOtherTriangle: two has more than one element");
        }
        if (two.Count < 1) {
            // DebugAtTriangle (hit1, Color.red);
            Debug.LogWarning ("In GetOtherTriangle: two has less than one element");
        }
        return two[0];
    }
    private void DebugAtTriangle (int hit1, Color c) {
        var t = GetTriangle (hit1);
        Vector3 barycenter = (mesh.vertices[t[0]] + mesh.vertices[t[1]] + mesh.vertices[t[2]]) / 3;
        Misc.DebugSphere (barycenter, c, "Hit: " + hit1);
    }

}