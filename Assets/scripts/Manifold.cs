using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using UnityEditor;
using System;
using System.Linq;
using TriangleNet.Topology;
using UnityEditor;

public class Manifold : MonoBehaviour {
	public GameObject gameObjectSwitched;
	MeshCutter mc;
	List<List<int>> boundaryCurves = new List<List<int>> ();

	/// <summary>
	/// Awake is called when the script instance is being loaded.
	/// </summary>
	void Awake () {
		mc = gameObject.AddComponent<MeshCutter> ();
		mc.manifold = this;
		gameObject.AddComponent<ManifoldDragging> ();
	}

	private List<List<int>> getBoundariesClone () {
		var newBounds = new List<List<int>> ();
		foreach (var bound in boundaryCurves) {
			var newBound = new List<int> ();
			foreach (var index in bound) {
				newBound.Add (index);
			}
			newBounds.Add (newBound);
		}
		return newBounds;
	}

	public void Destroy () {
		GameObject.Destroy (gameObject);
	}

	public void AddBoundary (List<int> curve) {
		boundaryCurves.Add (curve);
	}

	internal List<List<int>> getBoundaries () {
		return boundaryCurves;
	}

	internal void ClearObjects () {
		foreach (Transform child in gameObject.transform) {
			if (child.gameObject.CompareTag ("Dot") || child.gameObject.CompareTag ("Path")) {
				GameObject.Destroy (child.gameObject);
			}
		}
	}

}