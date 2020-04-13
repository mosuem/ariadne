using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MLine {
	private List<Vector3> positions;
	private List<Vector3> normals;

	public int positionCount { get { return positions.Count; } }
	private MeshRenderer renderer;
	private Mesh mesh;

	//	private MeshCollider coll;
	public float width;
	public bool hasNormals {
		get { return normals.Count > 0; }
	}

	public int sortingOrder { set { renderer.sortingOrder = value; } get { return renderer.sortingOrder; } }

	//	public List<Vector3> PositionList ()
	//	{
	//		List<Vector3> list = new List<Vector3> ();
	//		foreach (var point in positions) {
	//			list.Add (point);
	//		}
	//		return list;
	//	}

	public MLine (GameObject pathObject) {
		positions = new List<Vector3> ();
		normals = new List<Vector3> ();

		mesh = pathObject.AddComponent<MeshFilter> ().mesh;
		renderer = pathObject.AddComponent<MeshRenderer> ();
		renderer.sortingOrder = sortingOrder;
		//		coll = parent.AddComponent <MeshCollider> ();
	}

	public void ClearPositions () {
		positions.Clear ();
	}

	public void SetMesh () {
		if (positionCount > 1 && (normals.Count == 0 || normals.Count >= positionCount)) {
			mesh.Clear ();
			var vertices = new Vector3[positionCount * 2];
			var signs = new List<int> ();
			int counter = 0;
			for (int i = 0; i < positionCount; i++) {
				var vertex0 = i > 0 ? GetLocalPosition (i - 1) : 2 * GetLocalPosition (0) - GetLocalPosition (1);
				var vertex1 = GetLocalPosition (i);
				var vertex2 = i < positionCount - 1 ? GetLocalPosition (i + 1) : 2 * GetLocalPosition (i) - GetLocalPosition (i - 1);
				var direction0 = vertex1 - vertex0;
				var direction1 = vertex2 - vertex1;

				var sign = 1;
				var angle = Vector3.Dot (direction0, direction1);
				var cross = Vector3.Cross (direction0, direction1);
				if (Vector3.Dot (Vector3.back, cross) < 0) { // Or > 0
					sign = -1;
				}
				signs.Add (sign);

				Vector3 normal;
				if (normals.Count > 0) {
					normal = normals[i];
				} else {
					normal = Vector3.back;
				}
				var side0 = Vector3.Cross (direction0, normal);
				var side1 = Vector3.Cross (direction1, normal);
				var side = Vector3.Lerp (side0, side1, 0.5f);
				side.Normalize ();
				side *= width / 2f;
				vertices[counter++] = vertex1 + side;
				vertices[counter++] = vertex1 - side;
			}
			counter = 4;
			var triangles = new int[(positionCount - 1) * 2 * 3];
			Pair<int> pair = new Pair<int> (2, 3);
			Pair<int> pair2 = new Pair<int> (0, 1);
			for (int i = 0; i < positionCount - 1; i++) {

				triangles[6 * i] = 2 * i;
				triangles[6 * i + 1] = 2 * (i + 1);
				triangles[6 * i + 2] = 2 * i + 1;

				triangles[6 * i + 3] = 2 * i + 1;
				triangles[6 * i + 4] = 2 * (i + 1);
				triangles[6 * i + 5] = 2 * (i + 1) + 1;
			}
			mesh.vertices = vertices;
			mesh.triangles = triangles;
			mesh.RecalculateBounds ();
			mesh.RecalculateNormals ();
		}
	}

	public Vector3 GetLocalPosition (int i) {
		return positions[i];
	}

	public Vector3 GetNormal (int i) {
		return normals[i];
	}

	public List<Vector3> GetNormals () {
		return new List<Vector3> (normals);
	}

	public void InsertLocalPositionAt (int index, Vector3 arg) {
		positions.Insert (index, arg);
	}

	public void AddLocalPosition (Vector3 arg, bool setMesh = false) {
		positions.Add (arg);
		if (setMesh) {
			SetMesh ();
		}
	}

	public void SetLocalPosition (int j, Vector3 arg, bool setMesh = false) {
		Vector3 item = arg;
		if (j == positionCount) {
			positions.Add (item);
		} else {
			positions[j] = item;
		}
		if (setMesh) {
			SetMesh ();
		}
	}

	public void SetNormal (int j, Vector3 arg, bool setMesh = false) {
		if (j == normals.Count) {
			normals.Add (arg);
		} else {
			normals[j] = arg;
		}
		if (setMesh) {
			SetMesh ();
		}
	}
	public void InsertNormalAt (int index, Vector3 item) {
		normals.Insert (index, item);
	}

	public void AddNormal (Vector3 arg, bool setMesh = false) {
		normals.Add (arg);
		if (setMesh) {
			SetMesh ();
		}
	}

	public void SetLocalPositions (List<Vector3> list) {
		positions = list;
	}

	public void SetNormals (List<Vector3> list) {
		normals = list;
	}

	public void RemovePosition (int i) {
		if (i >= 0 && i < positionCount) {
			positions.RemoveAt (i);
		}
	}

	public void RemoveNormal (int i) {
		if (i >= 0 && i < normals.Count) {
			normals.RemoveAt (i);
		}
	}

	public Color GetColor () {
		return renderer.material.GetColor ("_Color");
	}

	public void SetColor (Color color) {
		renderer.material.SetColor ("_Color", color);
	}

	public void SetMaterial (Material pPathMaterial) {
		renderer.material = pPathMaterial;
	}

	public int GetNearestPointTo (Vector3 point) {
		float smallestDistance = 100000f;
		int index = 0;
		for (int i = 0; i < positionCount; i++) {
			var dist = Vector2.Distance (point, GetLocalPosition (i));
			if (dist < smallestDistance) {
				index = i;
				smallestDistance = dist;
			}
		}
		return index;
	}

	public void setNormal (int i, Vector3 normal) {
		var index = i * 4 + 3 < mesh.normals.Length ? i * 4 : (mesh.normals.Length - 4);
		mesh.normals[index] = normal;
		mesh.normals[index + 1] = normal;
		mesh.normals[index + 2] = normal;
		mesh.normals[index + 3] = normal;
	}

	internal List<Vector3> GetLocalPositions () {
		List<Vector3> list = new List<Vector3> (positions);
		return list;
	}
}