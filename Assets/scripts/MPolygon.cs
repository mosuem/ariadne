using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using UnityEditor;
using System;
using System.Linq;
using TriangleNet.Topology;
using UnityEditor;

public class MPolygon : List<int> {
	public MPolygon (List<int> part3) : base (part3) { }

	public List<MPolygon> Triangulate () {
		List<MPolygon> polys = new List<MPolygon> ();
		for (int i = 2; i < Count; i++) {
			MPolygon newPoly = new MPolygon ();
			newPoly.Add (this [0]);
			newPoly.Add (this [i - 1]);
			newPoly.Add (this [i]);
			polys.Add (newPoly);
		}
		return polys;
	}

	public MPolygon () : base () { }

	bool contains (PolygonSide pairEntry) {
		for (int i = 1; i < Count; i++) {
			if (pairEntry.left.Equals (this [i - 1]) && pairEntry.right.Equals (this [i])) {
				return true;
			}
		}
		if (pairEntry.left.Equals (this [Count - 1]) && pairEntry.right.Equals (this [0])) {
			return true;
		}
		return false;
	}

	public bool ContainsEdge (PolygonSide pairEntry, Dictionary<PolygonSide, int> replacedNeighbors) {
		if (!replacedNeighbors.ContainsKey (pairEntry)) {
			return contains (pairEntry) || contains (pairEntry.Reversed ());
		} else {
			var neighbor = replacedNeighbors[pairEntry];
			var pair1 = new PolygonSide (pairEntry.left, neighbor);
			var pair2 = new PolygonSide (pairEntry.right, neighbor);
			return contains (pair1) || contains (pair1.Reversed ()) || contains (pair2) || contains (pair2.Reversed ());
		}
	}

	public bool ContainsEdge (PolygonSide pairEntry) {
		return contains (pairEntry) || contains (pairEntry.Reversed ());
	}

	public int Cut (PolygonSide a, PolygonSide b, out List<MPolygon> newPolys, int vertexCount) {
		if (!a.SameAs (b)) {
			var newV1 = vertexCount;
			var newV2 = vertexCount + 1;
			var part1 = new List<int> ();
			var part2 = new List<int> ();
			var part3 = new List<int> ();
			if (contains (a.Reversed ())) {
				a.Reverse ();
			}
			if (contains (b.Reversed ())) {
				b.Reverse ();
			}
			List<int> side1 = GetSideWalk (a.right, b.left);
			side1.Add (newV1);
			side1.Add (newV2);
			List<int> side2 = GetSideWalk (b.right, a.left);
			side2.Add (newV2);
			side2.Add (newV1);
			newPolys = new List<MPolygon> ();
			newPolys.Add (new MPolygon (side1));
			newPolys.Add (new MPolygon (side2));
			return 2;
		}
		newPolys = null;
		return -1;
	}

	public int Cut (PolygonSide a, int corner, out List<MPolygon> newPolys, int vertexCount) {
		// Debug.Log ("Check if not contain corner");
		if (!a.Contains (corner)) {
			// Debug.Log ("Does not contain corner");
			var newV1 = vertexCount;
			if (contains (a.Reversed ())) {
				a.Reverse ();
			}
			List<int> side1 = GetSideWalk (corner, a.left);
			side1.Add (newV1);
			List<int> side2 = GetSideWalk (a.right, corner);
			side2.Add (newV1);
			newPolys = new List<MPolygon> ();
			newPolys.Add (new MPolygon (side1));
			newPolys.Add (new MPolygon (side2));
			return 2;
		}
		newPolys = null;
		return -1;
	}

	private List<int> GetSideWalk (int from, int to) {
		// Debug.Log ("Get Side Walk from " + from + " to " + to + " in " + ToString());
		int i1 = IndexOf (from);
		int i2 = IndexOf (to);
		if (i1 <= i2) {
			return GetRange (i1, i2 - i1 + 1);
		} else {
			var part1 = GetRange (i1, Count - i1);
			var part2 = GetRange (0, i2 + 1);
			part1.AddRange (part2);
			return part1;
		}
	}

	public override string ToString () {
		String s = "Polygon: ";
		for (int i = 0; i < Count - 1; i++) {
			s += this [i] + ", ";
		}
		s += this [Count - 1];
		return s;
	}

	internal void Reduce (List<Vector3> vertices) {
		var newPoly = new MPolygon ();
		foreach (var vertex in this) {
			if (!newPoly.Contains (vertex)) {
				newPoly.Add (vertex);
			}
		}
		this.Clear ();
		this.AddRange (newPoly);
	}

	internal List<PolygonSide> GetSides () {
		List<PolygonSide> list = new List<PolygonSide> ();
		for (int i = 0; i < Count - 1; i++) {
			list.Add (new PolygonSide (this [i], this [i + 1]));
		}
		list.Add (new PolygonSide (this [Count - 1], this [0]));
		return list;
	}
}