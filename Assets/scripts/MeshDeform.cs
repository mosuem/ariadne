using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using UnityEditor;
using System;
using System.Linq;
using UnityEditor;
using Random = UnityEngine.Random;

public class MeshDeform {
    private Mesh mesh;
    private Vector3[] vertices;
    private float[, ] distances;
    private int[] meshTriangles;
    private Vector3[] normals;
    private List<List<int>> tList;
    private Graph g;
    private Vector3[] sums;
    private float epsilon = 0.0001f;
    private float t = 0f;
    private Color[] colors;

    private GameObject[] spheres;

    public MeshDeform (Mesh mesh, Graph g, float[, ] distances) {
        this.mesh = mesh;
        this.vertices = mesh.vertices;
        this.distances = distances;
        this.meshTriangles = mesh.triangles;
        this.normals = mesh.normals;
        this.tList = new List<List<int>> ();
        colors = new Color[vertices.Length];
        spheres = new GameObject[vertices.Length];
        for (int index = 0; index < meshTriangles.Length / 3; index++) {
            var t = new List<int> ();
            t.Add (meshTriangles[index * 3 + 0]);
            t.Add (meshTriangles[index * 3 + 1]);
            t.Add (meshTriangles[index * 3 + 2]);
            tList.Add (t);
        }
        sums = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++) {
            sums[i] = vertices[i] + Random.insideUnitSphere * 0.001f;
            // spheres[i] = Misc.DebugSphere (vertices[i], Color.white, i + "");
        }
        mesh.vertices = sums;
        this.g = g;
    }
    public IEnumerator Deform () {
        bool vertexMoving = true;
        while (vertexMoving) {
            vertexMoving = false;
            for (int i = 0; i < vertices.Length; i++) {
                sums[i] = Vector3.zero;
            }
            for (int i = 0; i < vertices.Length; i++) {
                // sums[i] = t * EnergyGaussian (i) + (1f - t) * EnergySimple (i);
                // sums[i] = EnergyGaussian (i);
                sums[i] = EnergySimple (i);
                // EnergyStraighten (i);
                // sums[i] = EnergyMeanCurvature (i);
                if (sums[i].magnitude > epsilon) {
                    vertexMoving = true;
                }
            }
            for (int i = 0; i < vertices.Length; i++) {
                if (sums[i].magnitude > 1f) {
                    sums[i].Normalize ();
                }
                vertices[i] = vertices[i] + sums[i];
                // GameObject.Destroy (spheres[i]);
                // spheres[i].GetComponent<Renderer> ().material.color = colors[i];
            }
            mesh.vertices = vertices;
            Debug.Log ("Cycle");
            // mesh.colors = colors;
            yield return new WaitForSeconds (0.01f);
        }
    }

    private Vector3 EnergySimple (int i) {
        var sum = Vector3.zero;
        var neighbors = g.getNeighborsAt (i);
        foreach (var j in neighbors) {
            var dir = vertices[j] - vertices[i];
            var f = getDistance (i, j) - Vector3.Distance (vertices[i], vertices[j]);
            sum += -f * dir;
        }
        return sum * 0.02f;
    }
    private void EnergyStraighten (int i) {
        var neighbors = g.getNeighborsAt (i);
        foreach (var j in neighbors) {
            // var angleSum = Vector3.Angle (normals[i], vertices[j] - vertices[i]);
            // if (angleSum > 90.5f) {
            //     sums[j] += normals[j] * 0.01f;
            // } else if (angleSum < 89.5f) {
            //     sums[j] += -normals[j] * 0.01f;
            // }
            Vector3 dir = vertices[i] - vertices[j];
            float d = Vector3.Distance (vertices[i], vertices[j]);
            if (Mathf.Abs (d - 0.3f) > 0.1f) {
                sums[i] += dir * (1 - 0.3f / d) * 0.1f;
                sums[j] += -dir * (1 - 0.3f / d) * 0.1f;
            }
        }
    }
    private Vector3 EnergyMeanCurvature (int i) {
        var neighbors = g.getNeighborsAt (i);
        var A = 0f;
        for (int i1 = 0; i1 < neighbors.Count - 1; i1++) {
            A += GetArea (i, neighbors[i1], neighbors[i1 + 1]);
        }
        var anglestar = g.getAngleStar (i);
        var sum = Vector3.zero;
        foreach (var j in anglestar.Keys) {
            var beforej = anglestar[j].left;
            var afterj = anglestar[j].right;
            float alphaj = Cot (i, beforej, j);
            float betaj = Cot (i, afterj, j);
            sum += (alphaj + betaj) * (vertices[i] - vertices[j]);
        }
        var hn = sum / (2f * A);
        return hn;
    }

    private Vector3 EnergyMeanCurvature2 (int i) {
        var lambda = 0.001f;

        var neighbors = g.getNeighborsAt (i);
        var angleSum = 0f;
        foreach (var j in neighbors) {
            angleSum += Vector3.Angle (normals[i], vertices[j] - vertices[i]) - 90f;
        }

        return -lambda * angleSum * normals[i];
    }

    private Vector3 EnergyGaussian (int i) {
        var neighbors = g.getNeighborsAt (i);
        var triangles = g.getTrianglesAt (i);
        var A = 0f;
        for (int i1 = 0; i1 < neighbors.Count - 1; i1++) {
            A += GetArea (i, neighbors[i1], neighbors[i1 + 1]);
        }
        var sum = 0f;
        foreach (var item in triangles) {
            var t = tList[item];
            var index = t.IndexOf (i);
            var first = index == 0 ? 2 : index - 1;
            var second = index == 2 ? 0 : index + 1;
            sum += Vector3.Angle (vertices[t[first]] - vertices[i], vertices[t[second]] - vertices[i]);
        }
        var K = (360f - sum) * Mathf.Deg2Rad / A;

        var H = 0.5f * EnergyMeanCurvature (i).magnitude;
        var k1 = H + Mathf.Sqrt (H * H - K);
        var k2 = H - Mathf.Sqrt (H * H - K);
        float t2 = k1 * k2;
        spheres[i].name = (k1 * k2) * Mathf.Rad2Deg + "";
        colors[i] = t2 > 0 ? Color.Lerp (Color.white, Color.green, Mathf.Clamp01 (t2 * Mathf.Rad2Deg)) : Color.Lerp (Color.white, Color.red, Mathf.Clamp01 (-t2 * Mathf.Rad2Deg));
        var k = float.IsNaN (k1 * k2) ? 0f : k1 * k2;
        return k * normals[i] * 0.1f;
    }

    // private List<int> getNeighbors (int i, HashSet<int> triangles) {
    //     List<int> neighbors = new List<int> ();
    //     int lastTriangle = triangles.First ();
    //     var tri = tList[lastTriangle];
    //     var neighbor = tri[0] == i? tri[1] : tri[0];
    //     neighbors.Add (neighbor);
    //     while (neighbors.Count < triangles.Count) {
    //         foreach (var triangle in triangles) {
    //             if (triangle != lastTriangle) {
    //                 List<int> list1 = tList[triangle];
    //                 if (list1.Contains (neighbor)) {
    //                     for (int j = 0; j < 3; j++) {
    //                         if (list1[j] != i && list1[j] != neighbor) {
    //                             neighbor = list1[j];
    //                             break;
    //                         }
    //                     }
    //                     lastTriangle = triangle;
    //                     neighbors.Add (neighbor);
    //                     break;
    //                 }
    //             }
    //         }
    //     }
    //     return neighbors;
    // }

    private float GetArea (int i, int j, int v) {
        Vector3 AB = vertices[v] - vertices[i];
        Vector3 AC = vertices[j] - vertices[i];
        return Vector3.Magnitude (Vector3.Cross (AB, AC)) / 2;
    }

    private float Cot (int i, int v, int j) {
        var angle = Vector3.Angle (vertices[i] - vertices[v], vertices[j] - vertices[v]);
        // Debug.Log ("angle " + angle);
        return 1f / Mathf.Tan (angle * Mathf.Deg2Rad);
    }

    private float getDistance (int i, int j) {
        if (i < j) {
            return distances[i, j];
        } else {
            return distances[j, i];
        }
    }
}