using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using UnityEditor;
using System;
using System.Linq;

public class Graph {
	bool[, ] edges;

	int n;

	Dictionary<int, HashSet<int>> trianglesV = new Dictionary<int, HashSet<int>> ();

	Dictionary<int, List<int>> triangles = new Dictionary<int, List<int>> ();
	Dictionary<int, Dictionary<int, Pair<int>>> angleStars = new Dictionary<int, Dictionary<int, Pair<int>>> ();
	public Graph (Mesh m) {
		Vector3[] vertices = m.vertices;
		int[] triangles1 = m.triangles;
		n = vertices.Length;
		edges = new bool[n, n];
		for (int i = 0; i < triangles1.Length / 3; i++) {
			var v1 = triangles1[3 * i];
			var v2 = triangles1[3 * i + 1];
			var v3 = triangles1[3 * i + 2];

			edges[v1, v2] = true;
			edges[v2, v1] = true;
			edges[v2, v3] = true;
			edges[v3, v2] = true;
			edges[v3, v1] = true;
			edges[v1, v3] = true;
		}

		for (int i = 0; i < n; i++) {
			trianglesV[i] = new HashSet<int> ();
		}

		for (int i = 0; i < triangles1.Length / 3; i++) {
			var v1 = triangles1[3 * i + 0];
			var v2 = triangles1[3 * i + 1];
			var v3 = triangles1[3 * i + 2];
			trianglesV[v1].Add (i);
			trianglesV[v2].Add (i);
			trianglesV[v3].Add (i);
			List<int> list = new List<int> ();
			list.Add (v1);
			list.Add (v2);
			list.Add (v3);
			triangles[i] = list;
		}

		for (int i = 0; i < n; i++) {
			angleStars[i] = getOppositeAngleStar (i);
		}
	}

	private Dictionary<int, Pair<int>> getOppositeAngleStar (int v) {
		var dict = new Dictionary<int, Pair<int>> ();
		var neighbors = getNeighborsAt (v);
		if (neighbors.Count > 2) {
			foreach (var neighbor in neighbors) {
				List<int> triangles = getTriangles (v, neighbor);
				if (triangles.Count == 2) {
					dict[neighbor] = new Pair<int> (getOther (triangles[0], v, neighbor), getOther (triangles[1], v, neighbor));
				}
			}
		}
		return dict;
	}

	public Dictionary<int, Pair<int>> getAngleStar (int i) {
		return angleStars[i];
	}

	private int getOther (int tIndex, int v1, int v2) {
		var tri = triangles[tIndex];
		if ((tri[0] == v1 || tri[0] == v2) && (tri[1] == v2 || tri[1] == v1)) {
			return tri[2];
		} else if ((tri[2] == v1 || tri[2] == v2) && (tri[1] == v2 || tri[1] == v1)) {
			return tri[0];
		} else if ((tri[0] == v1 || tri[0] == v2) && (tri[2] == v2 || tri[2] == v1)) {
			return tri[1];
		}
		Debug.LogError ("Triangle " + tIndex + " does not contain " + v1 + " and " + v2);
		return -1;
	}

	public List<int> getNeighborsAt (int vertex) {
		var neighbors = new List<int> ();
		for (int i = 0; i < n; i++) {
			if (edges[vertex, i]) {
				neighbors.Add (i);
			}
		}
		return neighbors;
	}

	public HashSet<int> getTrianglesAt (int vertex) {
		return trianglesV[vertex];
	}

	public List<int> getTriangles (int v1, int v2) {
		var list1 = trianglesV[v1];
		var list2 = trianglesV[v2];
		var enumerable = list1.Intersect (list2);
		return enumerable.ToList ();
	}

	internal bool areNeighbors (int i, int j) {
		return edges[i, j];
	}
}