using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using UnityEditor;
using System;
using System.Linq;
using TriangleNet.Topology;
using UnityEditor;

class MeshCutter : MonoBehaviour {
    public Manifold manifold;
    private bool buildingGraph = true;
    Graph g;

    Mesh mesh;
    private List<Vector3> vertices;
    int lastTriangle;

    float startTime = 0f;
    Dictionary<string, float> timerDict = new Dictionary<string, float> ();
    private int oldVertexNumber;
    private List<int> meshTriangles;
    private List<Vector3> meshNormals;
    public List<Manifold> manifolds;

    void Start () {
        StartCoroutine (buildGraph ());
    }

    void DebugTime (string f = "") {
        var time = Time.realtimeSinceStartup;
        if (!timerDict.ContainsKey (f)) {
            Debug.Log ("Start " + f);
            timerDict[f] = time;
        } else {
            Debug.Log (f + " " + "Duration: " + (time - timerDict[f]));
        }
    }

    IEnumerator buildGraph () {
        buildingGraph = true;
        while (manifold == null) {
            yield return null;
        }
        GameObject mGameObject = manifold.gameObject;
        Debug.Log ("Building Graph for " + mGameObject.name);
        g = new Graph (mGameObject.GetComponent<MeshFilter> ().mesh);
        buildingGraph = false;
        Debug.Log ("Built Graph");
        yield return null;
    }
    public IEnumerator Glue (List<int> bound1, List<int> bound2, bool isClosed = false) {
        while (buildingGraph)
            yield return new WaitForSeconds (0.1f);
        Debug.Log ("Start Glueing");
        mesh = manifold.gameObject.GetComponent<MeshFilter> ().mesh;
        vertices = mesh.vertices.ToList ();
        float[, ] distances = new float[vertices.Count, vertices.Count];
        for (int i2 = 0; i2 < vertices.Count; i2++) {
            for (int j2 = i2 + 1; j2 < vertices.Count; j2++) {
                distances[i2, j2] = 0.3f;
            }
        }

        for (int i2 = 0; i2 < vertices.Count; i2++) {
            for (int j2 = i2 + 1; j2 < vertices.Count; j2++) {
                if (g.areNeighbors (i2, j2)) {
                    // distances[i, j] = Vector3.Distance (vertices[i], vertices[j]);
                }
            }
        }
        meshTriangles = mesh.triangles.ToList ();
        meshNormals = mesh.normals.ToList ();

        bound1 = bound1.Distinct ().ToList ();
        bound2 = bound2.Distinct ().ToList ();

        // foreach (var index in bound1) {
        //     Vector3 endPoint = vertices[index];
        //     Misc.DebugSphere (manifold.gameObject.transform.TransformPoint (endPoint), Color.red, "Bound 1 " + index);
        // }

        // foreach (var index in bound2) {
        //     Vector3 endPoint = vertices[index];
        //     Misc.DebugSphere (manifold.gameObject.transform.TransformPoint (endPoint), Color.blue, "Bound 2 " + index);
        // }

        if (bound2.Count < bound1.Count) {
            var temp = bound1;
            bound1 = bound2;
            bound2 = temp;
        }
        float ratio = (float) bound2.Count / bound1.Count;
        if (ratio < 1) {
            Debug.LogWarning ("Ratio should be greater 1");
        }
        var batch = new List<int> ();
        int num = Mathf.FloorToInt (ratio);
        var ratioSum = 0f;
        // Debug.Log ("num " + num);
        // Debug.Log ("ratio " + ratio);
        for (int i1 = 0; i1 < bound1.Count; i1++) {
            ratioSum += ratio - num;
            if (ratioSum > 1) {
                batch.Add (num + 1);
                ratioSum = ratioSum - 1;
            } else {
                batch.Add (num);
            }
        }

        Debug.Log (meshTriangles.Count);

        int start1;
        int start2;
        GetStarts (bound1, bound2, out start1, out start2);
        Debug.Log ("Start at " + start1 + " and " + start2);

        int last1 = start1 == 0 ? bound1.Count - 1 : start1 - 1;

        int i = start1;
        int j = start2;
        do {
            for (int k = 0; k < batch[i]; k++) {
                int v = bound2[j];
                int j1 = Next (bound2, j);
                int i1 = bound1[i];
                AddTriangle (i1, j1, v);
                j = j + 1 < bound2.Count ? j + 1 : 0;
            }
            AddTriangle (bound1[i], Next (bound1, i), bound2[j]);
            i = i + 1 < bound1.Count ? i + 1 : 0;
        } while (i != last1);
        if (isClosed) {
            do {
                AddTriangle (bound1[last1], Next (bound2, j), bound2[j]);
                j = j + 1 < bound2.Count ? j + 1 : 0;
            } while (j != start2);
            AddTriangle (bound1[last1], bound2[start2], bound2[j]);
            AddTriangle (bound1[last1], bound1[start1], bound2[start2]);
        }

        for (int k = 0; k < newMeshTriangles.Count / 3; k++) {
            var t1 = newMeshTriangles[3 * k];
            var t2 = newMeshTriangles[3 * k + 1];
            var t3 = newMeshTriangles[3 * k + 2];
            meshTriangles.Add (t1);
            meshTriangles.Add (t3);
            meshTriangles.Add (t2);
        }

        meshTriangles.AddRange (newMeshTriangles);
        Debug.Log (meshTriangles.Count);
        mesh.triangles = meshTriangles.ToArray ();
        mesh.RecalculateNormals ();
        // yield return StartCoroutine (buildGraph ());
        // var md = new MeshDeform (mesh, g, distances);
        // StartCoroutine (md.Deform ());
    }

    private void GetStarts (List<int> bound1, List<int> bound2, out int start, out int start2) {
        start = 0;
        start2 = getNearestPoint (bound1.First (), bound2);
    }

    private static int Next (List<int> bound, int index) {
        return index + 1 < bound.Count ? bound[index + 1] : bound[0];
    }

    private void AddTriangles (int i, List<int> list, List<int> bound2) {
        for (int index = 0; index < list.Count - 1; index++) {
            AddTriangle (i, list[index], list[index + 1]);
        }
    }

    private int getNearestPoint (int index, List<int> bound2) {
        var minDist = float.MaxValue;
        var minIndex = -1;
        for (int i = 0; i < bound2.Count; i++) {
            int index2 = bound2[i];
            float dist = Vector3.Distance (vertices[index], vertices[index2]);
            if (dist < minDist) {
                minIndex = i;
                minDist = dist;
            }
        }
        return minIndex;
    }
    List<int> newMeshTriangles = new List<int> ();
    internal Dictionary<Manifold, List<int>> newBoundaries = new Dictionary<Manifold, List<int>> ();

    private void AddTriangle (int i, int j, int v) {
        newMeshTriangles.Add (i);
        newMeshTriangles.Add (j);
        newMeshTriangles.Add (v);
    }
    public IEnumerator Cap (MPath path) {
        while (buildingGraph)
            yield return new WaitForSeconds (0.01f);

        mesh = manifold.gameObject.GetComponent<MeshFilter> ().mesh;
        var vertices = mesh.vertices.ToList ();
        var normals = mesh.normals.ToList ();
        var triangles = mesh.triangles.ToList ();
        var normal = GetNormalCenter (path, normals, vertices);
        normals.Add (normal);

        var center = path.GetCenterPosition () + normal.normalized * 0.1f;

        vertices.Add (center);

        var newVertex = vertices.IndexOf (center);
        for (int i = 0; i < path.Count; i++) {
            int vertex = vertices.IndexOf (path.GetLocalPosition (i));
            int nextVertex = vertices.IndexOf (path.GetLocalPosition (i + 1 < path.Count ? i + 1 : 0));

            triangles.Add (vertex);
            triangles.Add (nextVertex);
            triangles.Add (newVertex);
        }
        mesh.vertices = vertices.ToArray ();
        mesh.triangles = triangles.ToArray ();
        mesh.normals = normals.ToArray ();
        SetSwitchedManifold (manifold);

        manifold.gameObject.GetComponent<MeshCollider> ().sharedMesh = mesh;
    }

    private Vector3 GetNormalCenter (MPath path, List<Vector3> normals, List<Vector3> vertices) {
        var centerNormal = Vector3.zero;
        for (int i = 0; i < path.Count; i++) {
            centerNormal += normals[vertices.IndexOf (path.GetLocalPosition (i))];
        }
        return centerNormal / (float) path.Count;
    }

    public IEnumerator Cut (MPath path) {
        while (buildingGraph)
            yield return new WaitForSeconds (0.01f);

        DebugTime ("Total Cut");
        Vector3 normal = path.GetNormal (0);
        path.InsertPositionAt (0, path.dotFrom.transform.position);
        path.InsertNormalAt (0, normal);
        path.AddPosition (path.dotTo.transform.position);
        path.AddNormal (normal);

        DebugTime ("Identify Cuts");
        mesh = manifold.gameObject.GetComponent<MeshFilter> ().mesh;
        var vertices = mesh.vertices.ToList ();
        meshTriangles = mesh.triangles.ToList ();
        meshNormals = mesh.normals.ToList ();

        // Misc.DebugSphere (vertices[1299], Color.blue, "1299");
        // Misc.DebugSphere (vertices[1334], Color.blue, "1334");
        // Misc.DebugSphere (vertices[1335], Color.blue, "1335");
        // DebugAtTriangle (2596, Color.white, "2596");
        // DebugAtTriangle (2597, Color.white, "2597");
        var cutPointIdentifier = new CutPointIdentifier (g, manifold);
        cutPointIdentifier.IdentifyCuts (path);

        var cutPoints = cutPointIdentifier.cutPoints;
        var cutEdges = cutPointIdentifier.cutEdges;
        var cutPointType = cutPointIdentifier.cutPointType;
        var cutTriangles = cutPointIdentifier.cutTriangles;
        var cutAlongEdges = cutPointIdentifier.cutAlongEdges;
        var cutNodes = cutPointIdentifier.cutNodes;
        DebugTime ("Identify Cuts");
        Debug.Log ("Identification done, resulting in " + cutPoints.Count + " cut Points.");

        List<HashSet<int>> bounds;
        List<HashSet<int>> meshVertexPres;
        GetNewMeshVertices (vertices, cutNodes, cutAlongEdges, cutEdges, out bounds, out meshVertexPres);
        Debug.Log ("GetBounds done, resulting in " + bounds.Count + " bounds.");
        int numComponents = meshVertexPres.Count;

        DebugTime ("Set up Polygons");
        var polyAtTriangle = new Dictionary<int, List<MPolygon>> ();
        foreach (var i in cutTriangles.Keys) {
            var index = cutTriangles[i];

            polyAtTriangle[index] = new List<MPolygon> ();
            var list = new MPolygon ();
            list.Add (meshTriangles[index * 3]);
            list.Add (meshTriangles[index * 3 + 1]);
            list.Add (meshTriangles[index * 3 + 2]);
            polyAtTriangle[index].Add (list);
        }
        DebugTime ("Set up Polygons");
        // foreach (var item in cutTriangles.Keys) {
        //     DebugAtTriangle (cutTriangles[item], Color.red, item + "");
        // }
        DebugTime ("Cut up Polygons");
        oldVertexNumber = vertices.Count;
        var polygonCutter = new PolygonCutter (meshTriangles, meshNormals, vertices, polyAtTriangle);
        var sidesOfCut = polygonCutter.Cut (cutPoints, cutEdges, cutNodes, cutTriangles);
        List<Vector3> normals = polygonCutter.normals;
        var cutPointsToVertices = polygonCutter.cutPointVertices;
        DebugTime ("Cut up Polygons");
        // for (int i = 0; i < cutPoints.Count; i++) {
        //     Vector3 cp = cutPoints[i];
        //     Misc.DebugSphere (cp, Color.red, i + "cp");
        // }

        DebugTime ("Triangulate");
        var to = new TriangleOrdering (vertices, polyAtTriangle, bounds, cutNodes, cutPointsToVertices, sidesOfCut);
        Pair<HashSet<int>> sideBounds;
        var polygons = to.orderTriangles (out sideBounds);
        DebugTime ("Triangulate");
        Debug.Log ("Triangulation done, resulting in " + polygons.Count + " lists of polygons.");

        // foreach (var item in sidesOfCut.left) {
        //     DebugAtPolygon (vertices, item, Color.green);
        // }
        // foreach (var item in sidesOfCut.right) {
        //     DebugAtPolygon (vertices, item, Color.red);
        // }

        var newMeshVertexPres = new List<HashSet<int>> ();
        newMeshVertexPres.Add (new HashSet<int> ());
        newMeshVertexPres.Add (new HashSet<int> ());
        foreach (var bound in sideBounds.left) {
            newMeshVertexPres[0].UnionWith (meshVertexPres[bound]);
        }
        foreach (var bound in sideBounds.right) {
            newMeshVertexPres[1].UnionWith (meshVertexPres[bound]);
        }
        numComponents = 2;
        if (newMeshVertexPres[0].SetEquals (newMeshVertexPres[1])) {
            numComponents = 1;
            polygons[0].AddRange (polygons[1]);
        }
        DebugTime ("GetTriangles");
        List<List<int>> meshts;
        List<List<Vector3>> meshvs, meshns;
        SetUpNewMeshes (vertices, cutTriangles, newMeshVertexPres, out meshts, out meshvs, out meshns);
        DebugTime ("Set Normals and Triangles");

        DebugTime ("Set up manifolds");
        SetUpManifolds (vertices, meshts, meshvs, meshns, normals, polygons, cutPoints, numComponents);
        SetSwitchedManifolds ();
        DebugTime ("Set up manifolds"); //SLOW
        DebugTime ("Total Cut");
    }

    private void SetSwitchedManifolds () {
        foreach (var m in manifolds) {
            SetSwitchedManifold (m);
        }
    }

    private static void SetSwitchedManifold (Manifold m) {
        if (m.gameObjectSwitched != null) {
            GameObject.Destroy (m.gameObjectSwitched);
        }
        m.gameObjectSwitched = new GameObject ("Switched");
        m.gameObjectSwitched.transform.position = m.gameObject.transform.position;
        m.gameObjectSwitched.transform.rotation = m.gameObject.transform.rotation;

        m.gameObjectSwitched.transform.parent = m.gameObject.transform;

        var mf = m.gameObjectSwitched.AddComponent<MeshFilter> ();

        var renderer = m.gameObjectSwitched.AddComponent<MeshRenderer> ();
        renderer.material = m.gameObject.GetComponent<MeshRenderer> ().material;

        var mesh = new Mesh ();
        var mesh2 = m.gameObject.GetComponent<MeshFilter> ().mesh;
        mesh.vertices = mesh2.vertices;
        var oldTriangles = mesh2.triangles;
        int[] newTriangles = new int[oldTriangles.Length];
        for (int i = 0; i < oldTriangles.Length / 3; i++) {
            newTriangles[3 * i] = oldTriangles[3 * i];
            newTriangles[3 * i + 1] = oldTriangles[3 * i + 2];
            newTriangles[3 * i + 2] = oldTriangles[3 * i + 1];
        }
        var oldNormals = mesh2.normals;
        var newNormals = new Vector3[oldNormals.Length];
        for (int i = 0; i < oldNormals.Length; i++) {
            newNormals[i] = -oldNormals[i];
        }
        mesh.triangles = newTriangles;
        mesh.normals = newNormals;
        mf.mesh = mesh;
    }

    internal void Add (Manifold m2, ref List<List<int>> boundaryCurves) {
        var mesh = this.manifold.gameObject.GetComponent<MeshFilter> ().mesh;
        var v1 = mesh.vertices;
        var t1 = mesh.triangles;
        var n1 = mesh.normals;

        var mesh2 = m2.gameObject.GetComponent<MeshFilter> ().mesh;
        var v2 = mesh2.vertices;
        var t2 = mesh2.triangles;
        var n2 = mesh2.normals;

        var newVerts = new Vector3[v1.Length + v2.Length];
        var newTris = new int[t1.Length + t2.Length];
        var newNorms = new Vector3[n1.Length + n2.Length];

        var localToWorld1 = manifold.gameObject.transform.localToWorldMatrix;
        var localToWorld2 = m2.gameObject.transform.localToWorldMatrix;
        for (int i = 0; i < v1.Length; i++) {
            // newVerts[i] = localToWorld1.MultiplyPoint3x4 (v1[i]);
            newVerts[i] = v1[i];
        }
        for (int i = 0; i < v2.Length; i++) {
            // newVerts[i + v1.Length] = localToWorld2.MultiplyPoint3x4 (v2[i]);
            Vector3 vector3 = m2.gameObject.transform.TransformPoint (v2[i]);
            newVerts[i + v1.Length] = manifold.gameObject.transform.InverseTransformPoint (vector3);
            // newVerts[i + v1.Length] = v2[i];
        }

        for (int i = 0; i < t1.Length; i++) {
            newTris[i] = t1[i];
        }
        for (int i = 0; i < t2.Length; i++) {
            newTris[i + t1.Length] = t2[i] + v1.Length;
        }

        for (int i = 0; i < n1.Length; i++) {
            newNorms[i] = n1[i];
        }
        for (int i = 0; i < n2.Length; i++) {
            newNorms[i + n1.Length] = n2[i];
        }
        foreach (var bound in boundaryCurves) {
            for (int i = 0; i < bound.Count; i++) {
                bound[i] = bound[i] + v1.Length;
            }
            manifold.AddBoundary (bound);
        }

        mesh.vertices = newVerts;
        mesh.triangles = newTris;
        mesh.normals = newNorms;
        mesh.RecalculateBounds ();
        StartCoroutine (buildGraph ());
    }

    private void DebugAtPolygon (List<Vector3> vertices, MPolygon t, Color c) {
        Vector3 barycenter = (vertices[t[0]] + vertices[t[1]] + vertices[t[2]]) / 3;
        Misc.DebugSphere (barycenter, c, "Hit: " + t.ToString ());
    }
    private void SetUpNewMeshes (List<Vector3> vertices, Dictionary<int, int> cutTriangles, List<HashSet<int>> meshVertexPres, out List<List<int>> meshts, out List<List<Vector3>> meshvs, out List<List<Vector3>> meshns) {
        List<List<int>> meshtPres = new List<List<int>> ();
        for (int i = 0; i < meshVertexPres.Count; i++) {
            var meshvPre = meshVertexPres[i];
            var list = GetTriangles (meshvPre, cutTriangles);
            meshtPres.Add (list);
        }
        DebugTime ("GetTriangles"); //SLOW
        Debug.Log ("GetTriangles done, resulting in " + meshtPres.Count + " triangle lists.");

        DebugTime ("Set Normals and Triangles");
        meshts = new List<List<int>> ();
        meshvs = new List<List<Vector3>> ();
        meshns = new List<List<Vector3>> ();
        for (int i = 0; i < meshtPres.Count; i++) {
            var meshtPre = meshtPres[i];
            var meshvPre = meshVertexPres[i];

            var meshT = new List<int> (meshtPre.Count);
            var meshV = new List<Vector3> (meshvPre.Count);

            //Replace vertices with new positions in triangle list
            Dictionary<int, int> newVertexPosition = new Dictionary<int, int> ();
            var mesh1n = new List<Vector3> ();
            foreach (var vIndex in meshvPre) {
                meshV.Add (vertices[vIndex]);
                mesh1n.Add (meshNormals[vIndex]);
                newVertexPosition[vIndex] = meshV.Count - 1;
            }
            for (int k = 0; k < meshtPre.Count; k++) {
                meshT.Add (newVertexPosition[meshtPre[k]]);
            }
            //Add normals for mesh

            meshts.Add (meshT);
            meshvs.Add (meshV);
            meshns.Add (mesh1n);
        }
    }

    private void SetUpManifolds (List<Vector3> vertices, List<List<int>> meshts, List<List<Vector3>> meshvs, List<List<Vector3>> meshns, List<Vector3> normals, Dictionary<int, List<MPolygon>> polygons, List<Vector3> cutPoints, int numComponents) {
        manifolds = new List<Manifold> ();
        int largerManifold = 1;
        if (meshvs[0].Count > meshvs[1].Count) {
            largerManifold = 0;
        }
        manifold.gameObject.GetComponent<MeshRenderer> ().enabled = false;
        for (int i = 0; i < numComponents; i++) {
            var mesh1v = meshvs[i];
            var mesh1t = meshts[i];
            var mesh1n = meshns[i];
            Color randomColor = Color.grey;
            if (i != largerManifold) {
                randomColor = UnityEngine.Random.ColorHSV ();
            }
            Manifold meshObj = CreateMesh (mesh1v, mesh1t, vertices, normals, polygons[i], mesh1n, randomColor);
            var boundary = new List<int> ();
            foreach (var cp in cutPoints) {
                var index = mesh1v.IndexOf (cp);
                if (index != -1) {
                    boundary.Add (index);
                } else {
                    Debug.LogError ("Does not contain cutpoint at " + cp);
                }
            }
            newBoundaries[meshObj] = boundary;
            meshObj.AddBoundary (boundary);
            manifolds.Add (meshObj);
        }
    }

    private void GetNewMeshVertices (List<Vector3> vertices, Dictionary<int, int> cutNodes, Dictionary<int, PolygonSide> cutAlongEdges, Dictionary<int, PolygonSide> cutEdges, out List<HashSet<int>> bounds, out List<HashSet<int>> meshVertexPres) {
        List<int> unusedVertices = new List<int> ();
        for (int i = 0; i < vertices.Count; i++) {
            unusedVertices.Add (i);
        }

        var boundNeighbors = new Dictionary<int, HashSet<int>> ();
        foreach (var edge in cutEdges.Values) {
            if (!boundNeighbors.ContainsKey (edge.left)) {
                boundNeighbors[edge.left] = new HashSet<int> ();
            }
            if (!boundNeighbors.ContainsKey (edge.right)) {
                boundNeighbors[edge.right] = new HashSet<int> ();
            }
            boundNeighbors[edge.left].Add (edge.right);
            boundNeighbors[edge.right].Add (edge.left);
        }

        HashSet<int> cutAlongEdgesSet = new HashSet<int> ();
        foreach (var edge in cutAlongEdges.Values) {
            cutAlongEdgesSet.Add (edge.left);
            cutAlongEdgesSet.Add (edge.right);
        }

        //Find the new mesh vertices
        bounds = new List<HashSet<int>> ();
        meshVertexPres = new List<HashSet<int>> ();
        var counter = 0;
        while (unusedVertices.Count > 0 && ++counter < 12) {
            HashSet<int> bound;
            HashSet<int> meshv;
            GetNewMeshVertices (unusedVertices.First (), boundNeighbors, cutNodes, cutAlongEdgesSet, out bound, out meshv);
            meshVertexPres.Add (meshv);
            foreach (var vertex in meshv) {
                unusedVertices.Remove (vertex);
            }
            bounds.Add (bound);
            // foreach (var item in meshv) {
            //     Misc.DebugSphere (vertices[item], Color.red, "Mesh " + counter);
            // }
        }
        if (counter > 9) {
            Debug.LogError ("unusedVertices.Count " + unusedVertices.Count + " after many iterations");
        }
    }

    private void DebugAtTriangle (int hit1, Color c, string name) {
        var t = GetTriangle (hit1);
        Vector3 barycenter = (mesh.vertices[t[0]] + mesh.vertices[t[1]] + mesh.vertices[t[2]]) / 3;
        Misc.DebugSphere (barycenter, c, "Hit: " + hit1 + " " + name);
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

    Manifold CreateMesh (List<Vector3> vertices, List<int> triangles, List<Vector3> verticesComplete, List<Vector3> normalsComplete, List<MPolygon> triangulatedPolygons, List<Vector3> normals, Color c) {
        var meshObject = new GameObject ("MeshObj1");
        var mf = meshObject.AddComponent<MeshFilter> ();

        var renderer = meshObject.AddComponent<MeshRenderer> ();
        renderer.material = meshObject.GetComponent<MeshRenderer> ().material;
        renderer.material.color = c;

        var mesh = new Mesh ();
        mf.mesh = mesh;

        foreach (var triangle in triangulatedPolygons) {
            for (int i = 0; i < 3; i++) {
                if (triangle.Count == 3) {
                    int vertex = triangle[i];

                    int index;
                    var point = verticesComplete[vertex];
                    if (!vertices.Contains (point)) {
                        index = vertices.Count;
                        vertices.Add (point);
                        normals.Add (normalsComplete[vertex]);
                    } else {
                        index = vertices.IndexOf (point);
                    }
                    triangles.Add (index);
                }
            }
        }

        mesh.vertices = vertices.ToArray ();
        mesh.triangles = triangles.ToArray ();
        mesh.normals = normals.ToArray ();
        meshObject.gameObject.tag = "Sphere";
        meshObject.AddComponent<MeshCollider> ();

        // var manifold = new Manifold (meshObject);
        return manifold;
    }

    List<int> GetTriangles (HashSet<int> vertices, Dictionary<int, int> cutTriangles) {
        Dictionary<int, int>.ValueCollection values = cutTriangles.Values;
        List<int> newTriangles = new List<int> ();
        for (int i = 0; i < meshTriangles.Count / 3; i++) {
            if (!values.Contains (i)) {
                var n0 = meshTriangles[i * 3 + 0];
                var n1 = meshTriangles[i * 3 + 1];
                var n2 = meshTriangles[i * 3 + 2];
                if (vertices.Contains (n0)) {
                    if (vertices.Contains (n1)) {
                        if (vertices.Contains (n2)) {
                            newTriangles.Add (n0);
                            newTriangles.Add (n1);
                            newTriangles.Add (n2);
                        }
                    }
                }
            }
        }
        return newTriangles;
    }

    void GetNewMeshVertices (int start, Dictionary<int, HashSet<int>> boundNeighbors, Dictionary<int, int> cutNodes, HashSet<int> cutAlongEdges, out HashSet<int> bound, out HashSet<int> vertices) {
        vertices = new HashSet<int> ();
        bound = new HashSet<int> ();
        Queue<int> q = new Queue<int> ();
        var visited = new HashSet<int> ();
        q.Enqueue (start);
        visited.Add (start);
        var counter = 0;
        while (q.Count > 0 && ++counter < 100000) {
            var current = q.Dequeue ();
            vertices.Add (current);
            List<int> neighbors = g.getNeighborsAt (current);
            if (boundNeighbors.ContainsKey (current)) {
                if (!cutNodes.ContainsValue (current)) {
                    bound.Add (current);
                }
                foreach (var neighbor in boundNeighbors[current]) {
                    neighbors.Remove (neighbor);
                }
            }
            if (!cutNodes.ContainsValue (current)) { //!cutAlongEdges.Contains (current) &&
                foreach (var neighbor in neighbors) {
                    if (!visited.Contains (neighbor)) {
                        q.Enqueue (neighbor);
                        visited.Add (neighbor);
                    }
                    if (cutNodes.ContainsValue (neighbor)) {
                        bound.Add (current);
                    }
                }
            }
        }
        if (counter > 99999) {
            Debug.LogError ("q does not get 0");
        }
    }

    private bool Contains (ref List<PolygonSide> cutEdges, int neighbor) {
        foreach (var edge in cutEdges) {
            if (edge.left == neighbor || edge.right == neighbor) {
                return true;
            }
        }
        return false;
    }

    Vector3 getIntersection (Vector3 lineStart, Vector3 lineEnde, Vector3 pointLeft, Vector3 pointRight, Vector3 normPlane) {
        var l = lineEnde - lineStart;
        var l0 = lineStart;
        var ln = Vector3.Dot (l, normPlane);
        if (ln != 0f) {
            var d = Vector3.Dot (pointLeft - l0, normPlane) / ln;
            if (d >= 0f && d <= 1f) {
                var possibleIntersection = d * l + l0;
                if (Vector3.Dot (pointRight - pointLeft, possibleIntersection - pointLeft) > 0 && Vector3.Dot (pointLeft - pointRight, possibleIntersection - pointRight) > 0) {
                    return possibleIntersection;
                }
            }
        }
        return Vector3.positiveInfinity;
    }

}