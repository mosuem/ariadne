using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MPath : MonoBehaviour {

	public int pathNumber;
	private MLine line;
	//	public LinkedList<Vector3> points;
	public Dictionary<int, Vector3> colliderNormals;
	public GameObject dotFrom;
	public GameObject dotTo;
	public List<int> canonicalPath;
	public Manifold parentManifold;
	public bool hasNormals {
		get { return line.hasNormals; }
	}

	public int sortingOrder { set { line.sortingOrder = value; } get { return line.sortingOrder; } }

	public int Count { get { return line.positionCount; } }

	internal void SetLine (GameObject pathObject, Material mat) {
		line = new MLine (pathObject);
		line.SetMaterial (mat);
		line.sortingOrder = Statics.LineSortingOrder;
	}

	internal void SetWidth (float lineThickness) {
		line.width = lineThickness;
	}

	public void SetColor (Color c) {
		line.SetColor (c);
	}

	public void smoothLine () {
		Debug.Log ("Start SmoothLine");
		if (line.positionCount > 3) {
			for (int i = 0; i < line.positionCount - 2; i++) {
				bool isOnLeftEdge = colliderNormals.ContainsKey (i) && !colliderNormals.ContainsKey (i + 2);
				bool isOnRightEdge = colliderNormals.ContainsKey (i + 2) && !colliderNormals.ContainsKey (i);
				if (!isOnLeftEdge && !isOnRightEdge) {
					var p1 = GetPosition (i);
					var p = GetPosition (i + 1);
					var p2 = GetPosition (i + 2);
					if (Vector3.Distance (p1, p2) * Statics.smoothFactor < Vector3.Distance (p1, p) + Vector3.Distance (p, p2)) {
						var midPoint = Vector3.Lerp (p1, p2, 0.5f);
						Vector3 midNormal = Vector3.back;
						if (line.hasNormals) {
							midNormal = Vector3.Lerp (line.GetNormal (i), line.GetNormal (i + 2), 0.5f);
						}
						RaycastHit2D hit = Physics2D.CircleCast (p1, Statics.lineThickness * 0.5f, p2 - p1, Vector2.Distance (p1, p2));
						if (hit.collider != null && hit.collider.isTrigger == false) {
							//Nothing
						} else {
							SetPosition (i + 1, midPoint);
							if (line.hasNormals) {
								line.SetNormal (i + 1, midNormal);
							}
						}
					}
				}
			}
		}
		Debug.Log ("End SmoothLine");
	}

	public List<int> RefinePath () {
		return RefinePath (null);
	}

	public List<int> RefinePath (Dictionary<int, Vector3> closestPositions) {
		return RefinePath (closestPositions, Statics.meanDist);
	}

	public List<int> RefinePath (Dictionary<int, Vector3> closestPositions, float meanDist) {
		Debug.Log ("Start RefinePath");
		float minCircleCirc = 2f / Statics.meanDist;
		int forecastDepth = (int) minCircleCirc;
		Debug.Log ("Path count = " + Count + ", refine with depth " + forecastDepth);
		if (Count > 0) {
			int j = 0;
			while (j < Count - 1) {
				var point = GetPosition (j);
				var next = true;
				for (int i = 1; i < forecastDepth; i++) {
					if (j + i < Count - 1) {
						var nextPoint = GetPosition (j + i);
						var distance = Vector3.Distance (point, nextPoint);
						if (i == 1 && distance > meanDist * 1.01f) {
							var midPoint = Vector3.Lerp (point, nextPoint, Mathf.Clamp01 (meanDist / distance));
							InsertPositionAt (j + 1, midPoint);
							if (line.hasNormals) {
								line.InsertNormalAt (j + 1, line.GetNormal (j));
							}
							next = false;
							break;
						}
						if (distance < meanDist * 0.99f) {
							bool removed = false;
							for (int k = j + 1; k <= j + i; k++) {
								if (k < Count - 1) {
									line.RemovePosition (k);
									line.RemoveNormal (k);
									removed = true;
								}
							}
							if (removed) {
								next = false;
								break;
							}
						}
					} else {
						break;
					}
				}
				if (next) {
					j++;
				}
			}
		}

		var closeNodePositions = new List<int> ();
		if (closestPositions != null) {
			foreach (var pair in closestPositions) {
				var index = pair.Key;
				if (index < line.positionCount) {
					var vector3 = GetPosition (index);
					for (int node = 0; node < Count; node++) {
						if (Vector3.Distance (GetPosition (node), vector3) < meanDist) {
							closeNodePositions.Add (node);
							break;
						}
					}
				}
			}
		}

		Debug.Log ("Refined Path " + pathNumber + ", length after: " + Count);
		return closeNodePositions;
	}

	public void Simplify (float epsilon) {
		Debug.Log ("Start simplify with epsilon = " + epsilon + ", number of points before: " + line.positionCount);
		var newList = DouglasPeucker.DouglasPeuckerReduction (GetPositions (), epsilon);
		SetPositions (newList);
		Debug.Log ("End simplify with epsilon = " + epsilon + ", number of points after: " + line.positionCount);
	}

	internal Vector3 GetCenterPosition () {
		var center = Vector3.zero;
		for (int i = 0; i < Count; i++) {
			center += line.GetLocalPosition (i);
		}
		return center / (float) Count;
	}

	public void Concatenate (MPath path2) {
		var list1 = GetPositions ();
		var list2 = path2.GetPositions ();
		list1.AddRange (list2);
		SetPositions (list1);

		var list1n = line.GetNormals ();
		var list2n = path2.line.GetNormals ();
		list1n.AddRange (list2n);
		line.SetNormals (list1n);
		Destroy (path2);
		dotTo = path2.dotTo;

		line.SetMesh ();
	}

	internal void SetName (string v) {
		gameObject.name = v;
	}

	public Vector3 GetPosition (int i) {
		// return positions[i] + gameObject.transform.position;
		return gameObject.transform.TransformPoint (line.GetLocalPosition (i));
	}

	public List<Vector3> GetPositions () {
		var list = line.GetLocalPositions ();
		for (int i = 0; i < list.Count; i++) {
			list[i] = gameObject.transform.TransformPoint (list[i]);
		}
		return list;
	}
	public void InsertPositionAt (int index, Vector3 arg) {
		line.InsertLocalPositionAt (index, gameObject.transform.InverseTransformPoint (arg));
	}

	public Vector3 GetNormal (int i) {
		return line.GetNormal (i);
	}

	public void AddPosition (Vector3 arg, bool setMesh = false) {
		line.AddLocalPosition (gameObject.transform.InverseTransformPoint (arg), setMesh);
	}

	public void SetPositions (List<Vector3> list) {
		for (int i = 0; i < list.Count; i++) {
			list[i] = gameObject.transform.InverseTransformPoint (list[i]);
		}
		line.SetLocalPositions (list);
	}

	public void SetPosition (int j, Vector3 arg, bool setMesh = false) {
		line.SetLocalPosition (j, gameObject.transform.InverseTransformPoint (arg), setMesh);
	}
	internal Color GetColor () {
		return line.GetColor ();
	}

	public void AttachTo (Manifold manifold) {
		this.gameObject.transform.parent = manifold.gameObject.transform;
		parentManifold = manifold;
	}

	internal void SetMesh () {
		line.SetMesh ();
	}

	internal List<Vector3> GetNormals () {
		return line.GetNormals ();
	}

	internal void SetNormals (List<Vector3> list1n) {
		line.SetNormals (list1n);
	}

	internal void ClearPositions () {
		line.ClearPositions ();
	}

	internal void setNormal (int i, Vector3 right) {
		line.setNormal (i, right);
	}

	internal void InsertNormalAt (int v, Vector3 right) {
		line.InsertNormalAt (v, right);
	}

	internal void SetNormal (int i, Vector3 vector3) {
		line.setNormal (i, vector3);
	}

	internal void RemovePosition (int i) {
		line.RemovePosition (i);
	}

	internal void RemoveNormal (int i) {
		line.RemoveNormal (i);
	}

	internal Vector3 GetLocalPosition (int i) {
		return line.GetLocalPosition (i);
	}

	internal void AddNormal (Vector3 normal) {
		line.AddNormal (normal);
	}
}