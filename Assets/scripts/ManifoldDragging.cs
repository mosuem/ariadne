using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManifoldDragging : MonoBehaviour {

	Vector2 touchPos = Vector2.positiveInfinity;
	Vector2 touchPos22 = Vector2.positiveInfinity;

	Vector3 rotationCenter = Vector3.positiveInfinity;
	private Vector2 rotationTouchPoint = Vector2.positiveInfinity;
	private Vector3 normal;
	Vector2 touchPos1 = Vector2.positiveInfinity;
	Vector2 touchPos2 = Vector2.positiveInfinity;
	Vector2 touchPos3 = Vector2.positiveInfinity;
	Manifold m2;

	// Update is called once per frame
	void Update () {
		int tapCount = Input.touchCount;
		if (tapCount == 2 && notOnGUI (2)) {
			Touch touch1 = Input.GetTouch (0);
			Touch touch2 = Input.GetTouch (1);
			if (touch2.phase == TouchPhase.Began) {
				Ray ray1 = Camera.main.ScreenPointToRay (touch1.position);
				Ray ray2 = Camera.main.ScreenPointToRay (touch2.position);
				RaycastHit hitInfo1;
				RaycastHit hitInfo2;
				var hit1 = this.gameObject.GetComponent<MeshCollider> ().Raycast (ray1, out hitInfo1, 100f);
				var hit2 = this.gameObject.GetComponent<MeshCollider> ().Raycast (ray2, out hitInfo2, 100f);
				if (hit1 && hit2) {
					Statics.isDragging = true;
					touchPos = touch2.position;
				}
			} else if (touch2.phase == TouchPhase.Moved && touchPos.x < float.PositiveInfinity) {
				var vector2 = touch2.position - touchPos;
				this.gameObject.transform.position += new Vector3 (vector2.x, 0, vector2.y) * 0.02f;
				touchPos = touch2.position;
			} else if (touch2.phase == TouchPhase.Ended || touch2.phase == TouchPhase.Canceled) {
				Statics.isDragging = false;
				touchPos = Vector2.positiveInfinity;
			}
		} else if (tapCount == 3 && notOnGUI (3)) {
			Touch touch1 = Input.GetTouch (0);
			Touch touch2 = Input.GetTouch (1);
			Touch touch3 = Input.GetTouch (2);
			if (touch3.phase == TouchPhase.Began) {
				Ray ray1 = Camera.main.ScreenPointToRay (touch1.position);
				Ray ray2 = Camera.main.ScreenPointToRay (touch2.position);
				Ray ray3 = Camera.main.ScreenPointToRay (touch3.position);
				RaycastHit hitInfo1;
				RaycastHit hitInfo2;
				RaycastHit hitInfo3;
				var hit1 = this.gameObject.GetComponent<MeshCollider> ().Raycast (ray1, out hitInfo1, 100f);
				var hit2 = this.gameObject.GetComponent<MeshCollider> ().Raycast (ray2, out hitInfo2, 100f);
				var hit3 = this.gameObject.GetComponent<MeshCollider> ().Raycast (ray3, out hitInfo3, 100f);
				if (hit1 && hit2 && hit3) {
					Statics.isDragging = true;
					touchPos1 = touch1.position;
					touchPos2 = touch2.position;
					touchPos3 = touch3.position;
					rotationCenter = CircleCenterThrough3D (hitInfo1.point, hitInfo2.point, hitInfo3.point);
					rotationTouchPoint = CircleCenterThrough2D (touch1.position, touch2.position, touch3.position);
					normal = Vector3.Cross (hitInfo3.point - hitInfo1.point, hitInfo2.point - hitInfo1.point);
				}
			} else if (touch3.phase == TouchPhase.Moved && touchPos1.x < float.PositiveInfinity) {
				var angle1 = Vector3.SignedAngle (touchPos1 - rotationTouchPoint, touch1.position - rotationTouchPoint, normal);
				var angle2 = Vector3.SignedAngle (touchPos2 - rotationTouchPoint, touch2.position - rotationTouchPoint, normal);
				var angle3 = Vector3.SignedAngle (touchPos3 - rotationTouchPoint, touch3.position - rotationTouchPoint, normal);
				var angle = (angle1 + angle2 + angle3) / 3;
				touchPos1 = touch1.position;
				touchPos2 = touch2.position;
				touchPos3 = touch3.position;
				this.gameObject.transform.RotateAround (rotationCenter, normal, angle);
			} else if (touch3.phase == TouchPhase.Ended || touch3.phase == TouchPhase.Canceled) {
				Statics.isDragging = false;
				touchPos1 = Vector2.positiveInfinity;
				touchPos2 = Vector2.positiveInfinity;
				touchPos3 = Vector2.positiveInfinity;
			}
		} else if (tapCount == 4 && notOnGUI (4)) {
			Touch touch1 = Input.GetTouch (0);
			Touch touch2 = Input.GetTouch (1);
			Touch touch3 = Input.GetTouch (2);
			Touch touch4 = Input.GetTouch (3);
			if (touch4.phase == TouchPhase.Began) {
				Ray ray1 = Camera.main.ScreenPointToRay (touch1.position);
				Ray ray2 = Camera.main.ScreenPointToRay (touch2.position);
				Ray ray3 = Camera.main.ScreenPointToRay (touch3.position);
				Ray ray4 = Camera.main.ScreenPointToRay (touch4.position);
				RaycastHit hitInfo1;
				RaycastHit hitInfo2;
				RaycastHit hitInfo3;
				RaycastHit hitInfo4;
				var hit1 = this.gameObject.GetComponent<MeshCollider> ().Raycast (ray1, out hitInfo1, 100f);
				var hit2 = this.gameObject.GetComponent<MeshCollider> ().Raycast (ray2, out hitInfo2, 100f);
				var hit3 = this.gameObject.GetComponent<MeshCollider> ().Raycast (ray3, out hitInfo3, 100f);
				var hit4 = this.gameObject.GetComponent<MeshCollider> ().Raycast (ray4, out hitInfo4, 100f);
				if (hit1 && hit2 && hit3 && hit4) {
					Statics.isDragging = true;
					touchPos = touch2.position;
					touchPos22 = touch4.position;
					var freeDragging3D = Camera.main.GetComponent<FreeDragging3D> ();
					var m1 = this.gameObject.GetComponent<Manifold> ();

					m2 = GameObject.Instantiate (m1.gameObject).AddComponent<Manifold> ();
					// m2.boundaryCurves = m1.getBoundariesClone ();

					m2.gameObject.name = m1.gameObject.name + " Duplicate";
					m2.ClearObjects ();
					var newDots = new Dictionary<GameObject, GameObject> ();
					foreach (var item in freeDragging3D.level.dots) {
						Transform parent = item.transform.parent;
						if (parent == m1.gameObject.transform) {
							var copy = GameObject.Instantiate (item);
							copy.transform.parent = m2.gameObject.transform;
							newDots[item] = copy;
						}
					}
					var newPaths = new List<MPath> ();
					foreach (var item in freeDragging3D.level.paths) {
						if (item.parentManifold.Equals (m1)) {
							newPaths.Add (item);
						}
					}
					foreach (var item in newPaths) {
						MPath path = freeDragging3D.level.NewPath (newDots[item.dotFrom], newDots[item.dotTo]);
						Destroy (path.gameObject);
						path.gameObject.transform.position = m2.gameObject.transform.position;
						path.SetPositions (item.GetPositions ());
						path.SetNormals (item.GetNormals ());
						path.AttachTo (m2);
						freeDragging3D.level.addPath (path);
					}
					freeDragging3D.manifolds.Add (m2);
				}
			} else if (touch2.phase == TouchPhase.Moved && touchPos.x < float.PositiveInfinity) {
				var vector2 = touch2.position - touchPos;
				this.gameObject.transform.position += new Vector3 (vector2.x, 0, vector2.y) * 0.02f;
				touchPos = touch2.position;

				var vector22 = touch4.position - touchPos22;
				m2.gameObject.transform.position += new Vector3 (vector22.x, 0, vector22.y) * 0.02f;
				touchPos22 = touch4.position;
			} else if (touch2.phase == TouchPhase.Ended || touch2.phase == TouchPhase.Canceled) {
				touchPos = Vector2.positiveInfinity;
				touchPos22 = Vector2.positiveInfinity;
				Statics.isDragging = false;
			}
		}
	}
	public static Vector3 CircleCenterThrough3D (Vector3 a, Vector3 b, Vector3 c) {
		var u1 = b - a;
		var u = u1.normalized;
		var w = Vector3.Cross (c - a, u1).normalized;
		var v = Vector3.Cross (w, u);
		var bx = Vector3.Dot (u1, u);

		var cx = Vector3.Dot (c - a, u);
		var cy = Vector3.Dot (c - a, v);
		var bx2 = bx / 2f;
		var v1 = cx - bx2;
		var h = (v1 * v1 + cy * cy - bx2 * bx2) / (2 * cy);
		return a + (bx / 2) * u + h * v;
	}

	public static Vector2 CircleCenterThrough2D (Vector2 p1, Vector2 p2, Vector2 p3) {
		float x = (Vector2.SqrMagnitude (p1) * (p2.y - p3.y) + Vector2.SqrMagnitude (p2) * (p3.y - p1.y) + Vector2.SqrMagnitude (p3) * (p1.y - p2.y)) / (2 * (p1.x * (p2.y - p3.y) - p1.y * (p2.x - p3.x) + p2.x * p3.y - p3.x * p2.y));
		float y = (Vector2.SqrMagnitude (p1) * (p3.x - p2.x) + Vector2.SqrMagnitude (p2) * (p1.x - p3.x) + Vector2.SqrMagnitude (p3) * (p2.x - p1.x)) / (2 * (p1.x * (p2.y - p3.y) - p1.y * (p2.x - p3.x) + p2.x * p3.y - p3.x * p2.y));
		var center = new Vector2 (x, y);
		var radius = Vector2.Distance (center, p1);
		return center;
	}
	bool notOnGUI (int k) {
		for (int i = 0; i < k; i++) {
			if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject (Input.GetTouch (i).fingerId)) {
				return false;
			}
		}
		return true;
	}
}