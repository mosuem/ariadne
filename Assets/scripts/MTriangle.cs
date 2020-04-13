using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using UnityEditor;
using System;
using System.Linq;
using TriangleNet.Topology;
using UnityEditor;

class MTriangle {
	private int[] innerList = new int[3];

	public int this [int i] {
		get { return innerList[i]; }
		set { innerList[i] = value; }
	}

	public List<int> ToList () {
		return innerList.ToList ();
	}

	public override string ToString () {
		String s = "Triangle: ";
		for (int i = 0; i < 2; i++) {
			s += this [i] + ", ";
		}
		s += this [2];
		return s;
	}

	internal List<PolygonSide> GetSides () {
		var sides = new List<PolygonSide> ();
		sides.Add (new PolygonSide (this [0], this [2]));
		sides.Add (new PolygonSide (this [2], this [1]));
		sides.Add (new PolygonSide (this [1], this [0]));
		return sides;
	}

	internal bool isNeighbor (MTriangle t2, out PolygonSide neighbor) {
		var sides = GetSides ();
		foreach (var side in sides) {
			if (t2.innerList.Contains (side.left) && t2.innerList.Contains (side.right)) {
				neighbor = side;
				return true;
			}
		}

		// var intersect = this.innerList.Intersect (t2.innerList).ToList ();
		// if (intersect.Count == 2) {
		// 	neighbor = new Pair<int> (intersect[0], intersect[1]);
		// 	return true;
		// } else {
		neighbor = null;
		return false;
		// }
	}
}