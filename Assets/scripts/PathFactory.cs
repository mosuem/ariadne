using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFactory {
	private Material mat;
	private int counter;

	public PathFactory (Material pathMaterial) {
		mat = pathMaterial;
	}

	public MPath newPath (Color color, GameObject from, GameObject to) {
		float height = counter * 0.001f;
		string name = "Path " + counter;
		var pathObject = new GameObject (name);
		pathObject.tag = "Path";
		var path = pathObject.AddComponent<MPath> ();
		path.pathNumber = counter++;
		path.dotFrom = from;
		path.dotTo = to;
		path.SetLine (pathObject, mat);
		path.SetColor (color);

		path.SetWidth (Statics.lineThickness);
		var depthVector = Statics.lineDepth + Vector3.back * height;
		path.colliderNormals = new Dictionary<int, Vector3> ();
		return path;
	}
}