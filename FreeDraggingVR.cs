using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//using UnityEditor;
using System;
using System.Linq;
using UnityEngine.XR;
using UnityEngine.XR.WSA.Input;

public class FreeDraggingVR : MonoBehaviour
{
// 	// Class Variables
// 	public float frequency = 1000000;
// 	public float pathResolution;
// 	private int dot1;
// 	private GameObject dotMove;
// 	private int dot2;

// 	private Dictionary<MPath, bool> isCollided = new Dictionary<MPath,bool> ();
// 	private HashSet<int> angleGreaterThan90 = new HashSet<int> ();
// 	//private GameObject node;
// 	private Color trailColor;
// 	private TrailRenderer trail;

// 	MLine newLine;
// 	private float minDist = Statics.lineThickness;
// 	public Material partMaterial;

// 	private float dotRadius = 0.3f;
// 	private LevelData level;

// 	private float touchDuration = 0f;
// 	private Vector3 touchStart;

// 	private Homotopy actHomotopy = null;

// 	GameObject circle = null;
// 	GameObject rectangle = null;

// 	private bool drawing = false;
// 	private Vector3 drawPos;
// 	private int draggingPath = -1;
// 	private int draggingPosition = -1;
// 	private Vector3 lastTouch = Vector3.zero;
// 	private float distSoFar = 0f;
// 	private int upperBound;
// 	private int lowerBound;
// 	private GameObject draggingObstacle;
// 	private Vector3 draggingOffset;

// 	bool isDrawingPath = false;

// 	List<Vector3> lineNormals = new List<Vector3> ();

// 	//3D----------
// 	public GameObject manifoldVR;
// 	static Vector3 farAwayVec = new Vector3 (100f, 100f, 100f);
// 	Vector3 touchPos = farAwayVec;


// 	public List<Manifold> manifolds = new List<Manifold> ();
// 	Manifold actManifold;
// 	MeshCutter mc;

// 	void Start ()
// 	{
// 		Debug.Log (Statics.isSphere);
// 		level = Camera.main.GetComponent<LevelData> ();
// 		Debug.Log ("Draw Paths");
// 		for (int i = 0; i < level.paths.Count; i++) {
// 			Debug.Log ("Draw Path " + i);
// 			var path = level.paths [i];
// 			StartCoroutine (DrawPath (path, true));
// 		}
// 		Debug.Log ("End draw Paths");
// 		Manifold manifold;
// 		if (Statics.isSphere) {
// 			manifold = Misc.BuildSphere (level.manifoldMat, Color.grey);
// 		} else if (Statics.isTorus) {
// 			manifold = Misc.BuildTorus (level.manifoldMat, Color.grey);
// 		} else {
// 			Statics.isTorus = true;
// 			manifold = Misc.BuildTorus (level.manifoldMat, Color.grey);
// 		}
// 		manifold.gameObject.transform.position += Vector3.up * 1.2f;
// 		manifold.gameObject.transform.localScale *= 0.05f;
// 		manifolds.Add (manifold);
// //		manifold.GetComponent <MeshRenderer> ().material.color.a = 0.5f;
// 		Statics.lineThickness *= 0.1f;
// 		mc = new MeshCutter (manifold);
// 		Statics.isLoading = false;
// //		var poly = new MPolygon ();
// //		poly.Add (12);
// //		poly.Add (15);
// //		poly.Add (28);
// //		MPolygon poly1;
// //		MPolygon poly2;
// //		poly.cut (new Pair<int> (12, 15), new Pair<int> (28, 12), out poly1, out poly2, 80, 90);
// //		Debug.Log (poly1);
// //		Debug.Log (poly2);
// //
// //		poly2.cut (new Pair<int> (28, 90), new Pair<int> (28, 15), out poly1, out poly2, 100, 101);
// //		Debug.Log (poly1);
// //		Debug.Log (poly2);
// 	}

// 	// Update
// 	void Update ()
// 	{
// 		if (Input.GetKeyDown ("joystick button 16")) {
// 			//			manifold.GetComponent <MeshRenderer> ().material.color = Color.blue;
// 			var interactionSourceStates = InteractionManager.GetCurrentReading ();
// 			foreach (var interactionSourceState in interactionSourceStates) {
// 				if (interactionSourceState.source.handedness == InteractionSourceHandedness.Left) {
// 					Vector3 position;
// 					Quaternion rotation;
// 					interactionSourceState.sourcePose.TryGetPosition (out position, InteractionSourceNode.Pointer);
// 					interactionSourceState.sourcePose.TryGetRotation (out rotation, InteractionSourceNode.Pointer);
// 					Ray ray = new Ray (position, rotation * Vector3.forward);
// 					RaycastHit hit3d = new RaycastHit ();
// 					bool isHit = Physics.Raycast (ray, out hit3d);
// 					if (isHit) {
// 						if (hit3d.collider.gameObject.CompareTag ("Sphere")) {
// 							DrawDot (ray);
// 						} else if (hit3d.collider.gameObject.CompareTag ("Dot") && dotMove == null) {
// 							RaycastHit hit = new RaycastHit ();
// 							foreach (var manifold in manifolds) {
// 								if (manifold.gameObject.GetComponent <MeshCollider> ().Raycast (ray, out hit, 20f)) {
// 									actManifold = manifold;
// 									DrawLine3D (hit3d);
// 									break;
// 								}
// 							}
// 						}
// 						break;
// 					}
// 				}
// 			}
// 		}
// 		if (isDrawingPath) {
// 			var interactionSourceStates = InteractionManager.GetCurrentReading ();
// 			foreach (var interactionSourceState in interactionSourceStates) {
// 				if (interactionSourceState.source.handedness == InteractionSourceHandedness.Left) {
// 					Vector3 position;
// 					Quaternion rotation;
// 					interactionSourceState.sourcePose.TryGetPosition (out position, InteractionSourceNode.Pointer);
// 					interactionSourceState.sourcePose.TryGetRotation (out rotation, InteractionSourceNode.Pointer);
// 					Ray ray = new Ray (position, rotation * Vector3.forward);
// 					RaycastHit hit3d = new RaycastHit ();
// 					bool isHit = actManifold.gameObject.GetComponent<MeshCollider> ().Raycast (ray, out hit3d, 20f);
// 					if (isHit && hit3d.collider.gameObject.CompareTag ("Sphere")) {
// 						Statics.isDragging = true;
// 						touchPos = hit3d.point;
// 						Misc.DebugSphere (touchPos, Color.red, "touchpos");
// 						var direction = touchPos - dotMove.transform.position;
// 						moveDot (direction, hit3d);
// 						break;
// 					}
// 				}
// 			}
// 		}
// 		if (Input.GetKeyUp ("joystick button 16")) {
// //			manifold.GetComponent <MeshRenderer> ().material.color = Color.red;
// 			if (dotMove != null) {
// 				Debug.Log ("Finish Line");
// 				dotMove.GetComponent<Rigidbody> ().velocity = Vector3.zero;
// 				FinishLine ();
// 			}
// 			actManifold = null;
// 			isDrawingPath = false;
// 			Statics.isDragging = false;
// 		}
// 	}


// 	Vector3 SetOnSurface (Vector3 point, RaycastHit hit)
// 	{
// 		return point + Statics.dotSpacer * 0.5f * hit.normal * 0.05f;
// 	}

// 	void moveDot (Vector3 direction, RaycastHit hit)
// 	{
// 		var speed = 0.1f * 0.05f;
// 		if (direction.magnitude > 0.1f * 0.05f) {
// 			direction *= speed / direction.magnitude;
// 		}
// 		dotMove.transform.position += direction;
// 		var pair = RealSetOnSurface (dotMove.transform.position, hit.normal);
// 		dotMove.transform.position = pair.left;
// 		dotMove.transform.forward = -pair.right;

// 		if (trail.positionCount > lineNormals.Count) {
// 			lineNormals.Add (hit.normal);
// 		}
// 	}

// 	void DragPath (Vector3 touchPosition, Vector3 touchBefore)
// 	{
// 		Debug.Log ("Start DragPath(" + touchPosition.ToString ("F5") + ", " + touchBefore.ToString ("F5") + ")");
// 		bool dragSnuggledPositions;
// 		//		Debug.Log ("Homotopy has " + actHomotopy.closeNodePositions.Count + " snuggled Positions");
// 		if (actHomotopy.snugglingNodePositions.Contains (draggingPosition)) {
// 			dragSnuggledPositions = true;
// 		} else {
// 			dragSnuggledPositions = false;
// 		}

// 		var path = actHomotopy.midPath;
// 		var line = path.line;
// 		float t0 = (float)draggingPosition / path.Count;
// 		float distance;
// 		if (Statics.isSphere) {
// 			var dist = Vector3.Distance (touchPosition, touchBefore);
// 			var angle = Mathf.Asin (dist / Statics.sphereRadius);
// 			distance = angle * Statics.sphereRadius;
// 		} else if (Statics.isTorus) {
// 			distance = Vector3.Distance (touchPosition, touchBefore);
// 		} else {
// 			distance = Vector3.Distance (touchPosition, touchBefore);
// 		}
// 		Debug.Log ("Distance is " + distance);
// 		var lowerLimit = Statics.lineThickness / 2f;
// 		var maxUpperLimit = Mathf.Min (t0, 1f - t0);
// 		var t = Mathf.Clamp01 (distSoFar / 3f);
// 		float firstLowerSnugglePosition = maxUpperLimit;
// 		float firstUpperSnugglePosition = maxUpperLimit;

// 		if (!dragSnuggledPositions) {
// 			for (int i = draggingPosition; i < line.positionCount; i++) {
// 				if (i < upperBound) {
// 					if (actHomotopy.snugglingNodePositions.Contains (i)) {
// 						firstUpperSnugglePosition = (float)i / line.positionCount - t0;
// 						break;
// 					}
// 				} else {
// 					break;
// 				}
// 			}
// 			for (int i = draggingPosition - 1; i > -1; i--) {
// 				if (i > lowerBound) {
// 					if (actHomotopy.snugglingNodePositions.Contains (i)) {
// 						firstLowerSnugglePosition = t0 - (float)i / line.positionCount;
// 						break;
// 					}
// 				} else {
// 					break;
// 				}
// 			}
// 		}
// 		float lowerB = Mathf.SmoothStep (lowerLimit, Mathf.Min (maxUpperLimit, firstLowerSnugglePosition), t);
// 		float upperB = Mathf.SmoothStep (lowerLimit, Mathf.Min (maxUpperLimit, firstUpperSnugglePosition), t);
// 		Vector3 normal = (touchPosition - touchBefore);
// 		distSoFar += distance;

// 		for (int i = draggingPosition; i < line.positionCount - 1; i++) {
// 			if (i < upperBound) {
// 				if (!setPoint (i, normal, lowerB, upperB, t0, true)) {
// 					break;
// 				}
// 			} else {
// 				break;
// 			}
// 		}
// 		for (int i = draggingPosition - 1; i > 0; i--) {
// 			if (i > lowerBound) {
// 				if (!setPoint (i, normal, lowerB, upperB, t0, false)) {
// 					break;
// 				}
// 			} else {
// 				break;
// 			}
// 		}
// 		path.line.SetMesh ();
// 		Debug.Log ("End DragPath(" + touchPosition.ToString ("F5") + ", " + touchBefore.ToString ("F5") + ")");
// 	}

// 	void StartDraggingObstacle (int indexOfStatic, Vector3 touchPoint)
// 	{
// 		if (indexOfStatic != -1) {
// 			draggingObstacle = level.statics [indexOfStatic];
// 			draggingOffset = draggingObstacle.transform.position - touchPoint;
// 		}
// 	}

// 	void DragObstacle (Vector3 touchPoint)
// 	{
// 		draggingObstacle.transform.position = touchPoint + draggingOffset;
// 	}

// 	bool setPoint (int i, Vector3 normal, float lowerB, float upperB, float t0, bool up)
// 	{
// 		var line = actHomotopy.midPath.line;
// 		var draggingPoint = line.GetLocalPosition (draggingPosition);
// 		Vector3 point = line.GetLocalPosition (i);
// 		var direction = point - draggingPoint;
// 		var factor = GetFactor (i, t0, lowerB, upperB);
// 		var goalPoint = point + factor * normal;

// 		var pair = RealSetOnSurface (goalPoint, line.GetNormal (i));
// 		if (pair != null) {
// 			goalPoint = pair.left;
// 			line.setNormal (i, pair.right);
// 		}

// 		if (i > 0 && i < line.positionCount - 1) {
// 			int nextNode;
// 			if (up) {
// 				nextNode = i - 1;
// 			} else {
// 				nextNode = i + 1;
// 			}
// 			var nextPoint = line.GetLocalPosition (nextNode);
// 			var distToNext = Vector3.Distance (goalPoint, nextPoint);
// 			if (distToNext < Statics.meanDist * 1.01f) {
// 				Debug.Log ("Return false at " + i + " as the distance to next " + nextNode + " is too small");
// 				return false;
// 			}
// 		}
// 		Collider collider;
// 		var hasHit = Misc.HasHit (goalPoint, out collider);
// 		if (hasHit) {
// 			var staticNormal = Misc.GetNormal (collider, goalPoint);
// 			float staticAngle = Vector3.Angle (staticNormal, direction);
// 			if (staticAngle < 90) {
// 				//pull
// 				if (up) {
// 					upperBound = i;
// 				} else {
// 					lowerBound = i;
// 				}
// 			}
// 			Debug.Log ("Return false at " + i + " as a collider has been hit");
// 			return false;
// 		}
// 		//		Debug.Log ("Move position " + i + " from " + line.GetPosition (i).ToString ("F5") + " to " + goalPoint.ToString ("F5"));
// 		line.SetLocalPosition (i, goalPoint);
// 		line.SetNormal (i, pair.right);
// 		return true;
// 	}

// 	Pair<Vector3> RealSetOnSurface (Vector3 point, Vector3 normal)
// 	{
// 		normal.Normalize ();
// 		Ray ray = new Ray (point, -normal);
// 		RaycastHit hit;
// 		//		Misc.DebugSphere (goalPoint, Color.green, "Goalpoint" + i + " upper: " + upperBound + " lower " + lowerBound);
// 		var distDown = 1f;
// 		var distUp = 2f;
// 		if (actManifold.gameObject.GetComponent<MeshCollider> ().Raycast (ray, out hit, distDown)) {
// 			var newPoint = SetOnSurface (hit.point, hit);
// 			return new Pair<Vector3> (newPoint, hit.normal);
// 		} else {
// 			Ray ray2 = new Ray (point + normal, -normal);
// 			if (actManifold.gameObject.GetComponent<MeshCollider> ().Raycast (ray2, out hit, distUp)) {
// 				var newPoint = SetOnSurface (hit.point, hit);
// 				return new Pair<Vector3> (newPoint, hit.normal);
// 			}
// 		}
// 		return null;
// 	}

// 	float GetFactor (int i, float t0, float lowerB, float upperB)
// 	{
// 		MPath path = actHomotopy.midPath;
// 		var count = path.line.positionCount;
// 		float t = (float)i / count;
// 		if (i == 0 || i == count - 1) {
// 			return 0;
// 		}
// 		float f;
// 		if (t < t0) {
// 			f = Mathf.SmoothStep (0f, 1f, (t - (t0 - lowerB)) / lowerB);
// 		} else {
// 			f = Mathf.SmoothStep (0f, 1f, -(t - (t0 + upperB)) / upperB);
// 		}
// 		return f;
// 	}

// 	void EndDraggingPath ()
// 	{
// 		Debug.Log ("End Dragging Path");
// 		MPath path = actHomotopy.midPath;
// 		Debug.Log ("Refine");
// 		path.RefinePath ();
// 		Debug.Log ("Smoothline");
// 		path.smoothLine ();
// 		Debug.Log ("Start check if snuggling");
// 		StartCoroutine (CheckIfPathsAreSnuggling ());
// 		Debug.Log ("End check if snuggling, start snuggletopath");
// 	}

// 	IEnumerator CheckIfPathsAreSnuggling ()
// 	{
// 		var midLine = actHomotopy.midPath.line;
// 		Debug.Log ("Check if snuggling");
// 		var closestPositions = actHomotopy.closestPositions;
// 		closestPositions.Clear ();
// 		for (int i = draggingPosition; i < midLine.positionCount; i++) {
// 			var pos = midLine.GetLocalPosition (i);
// 			float closest = 9999f;
// 			foreach (var path in level.paths) {
// 				if (!actHomotopy.path1.Equals (path)) {
// 					var otherLine = path.line;
// 					for (int k = 0; k < otherLine.positionCount; k++) {
// 						Vector3 pos2 = otherLine.GetLocalPosition (k);
// 						var dist = Vector2.Distance (pos, pos2);
// 						if (dist < closest) {
// 							closestPositions [i] = pos2;
// 							closest = dist;
// 						}
// 					}
// 				}
// 			}
// 			if (closest < 9999f && closest < minDist) {
// 			} else {
// 				closestPositions.Remove (i);
// 				break;
// 			}
// 		}

// 		for (int i = draggingPosition - 1; i >= 0; i--) {
// 			var pos = midLine.GetLocalPosition (i);
// 			float closest = 9999f;
// 			foreach (var path in level.paths) {
// 				if (!actHomotopy.path1.Equals (path)) {
// 					var otherLine = path.line;
// 					for (int k = 0; k < otherLine.positionCount; k++) {
// 						Vector3 pos2 = otherLine.GetLocalPosition (k);
// 						var dist = Vector2.Distance (pos, pos2);
// 						if (dist < closest) {
// 							closestPositions [i] = pos2;
// 							closest = dist;
// 						}
// 					}
// 				}
// 			}
// 			if (closest < 9999f && closest < minDist) {
// 			} else {
// 				closestPositions.Remove (i);
// 				break;
// 			}
// 		}
// 		Debug.Log ("End check if snuggling, start snuggletopath");
// 		StartCoroutine (SnuggleToPath ());
// 		yield break;
// 	}

// 	IEnumerator SnuggleToPath ()
// 	{
// 		Debug.Log ("Snuggle To Path");
// 		Dictionary<int, Vector3> goalPoints = new Dictionary<int, Vector3> ();
// 		Dictionary<int, Vector3> startPoints = new Dictionary<int, Vector3> ();
// 		var closestPositions = actHomotopy.closestPositions;
// 		var path = actHomotopy.midPath;
// 		foreach (var pair in closestPositions) {
// 			var position = pair.Key;
// 			var closestPosition = pair.Value;
// 			float range = 30f;
// 			float counter = 1f;
// 			for (int i = 1; i < range; i++) {
// 				var index = position - i;
// 				if (index > -1) {
// 					var backwardsPosition = path.GetPosition (index);
// 					if (closestPositions.ContainsKey (index)) {
// 						break;
// 					}
// 					var factor = Mathf.SmoothStep (1, 0, Mathf.Clamp01 (counter / range));
// 					var direction = (closestPosition - backwardsPosition);
// 					var goalPoint = backwardsPosition + factor * direction;
// 					goalPoints [index] = goalPoint;
// 					counter++;
// 				}
// 			}
// 			counter = 1f;
// 			for (int i = 1; i < range; i++) {
// 				var index = position + i;
// 				if (index < path.line.positionCount) {
// 					var backwardsPosition = path.GetPosition (index);
// 					if (closestPositions.ContainsKey (index)) {
// 						break;
// 					}
// 					var factor = Mathf.SmoothStep (1, 0, Mathf.Clamp01 (counter / range));
// 					var direction = (closestPosition - backwardsPosition);
// 					var goalPoint = backwardsPosition + factor * direction;
// 					goalPoints [index] = goalPoint;
// 					counter++;
// 				}
// 			}
// 			goalPoints.Add (position, closestPosition);
// 		}
// 		foreach (var entry in goalPoints) {
// 			startPoints.Add (entry.Key, path.GetPosition (entry.Key));
// 		}
// 		float duration = 1;
// 		bool localMorph = true;
// 		var time = 0f;
// 		while (localMorph) {
// 			localMorph = false;
// 			time += Time.deltaTime;
// 			var t = Mathf.Clamp01 (time / duration);

// 			foreach (var entry in goalPoints) {
// 				var index = entry.Key;
// 				var goalPoint = entry.Value;
// 				var startPoint = startPoints [index];
// 				var goalPointStep = Vector3.Lerp (startPoint, goalPoint, t);

// 				var distance = Vector2.Distance (goalPointStep, goalPoint);

// 				path.line.SetLocalPosition (index, goalPointStep);
// 				if (t < 0.99f) {
// 					localMorph = true;
// 				}
// 			}
// 			if (Statics.mesh) {
// 				actHomotopy.setMesh (level.GetStaticPositions ());
// 			} else {
// 				actHomotopy.AddCurveToBundle (level.pathFactory, draggingPosition);
// 			}
// 			yield return null;
// 		}
// 		Debug.Log ("Done Snuggle To Path, start refinePath");
// 		actHomotopy.snugglingNodePositions = path.RefinePath (actHomotopy.closestPositions);
// 		if (Statics.mesh) {
// 			actHomotopy.setMesh (level.GetStaticPositions ());
// 		} else {
// 			actHomotopy.AddCurveToBundle (level.pathFactory, draggingPosition);
// 		}
// 		upperBound = actHomotopy.midPath.Count;
// 		lowerBound = -1;
// 		distSoFar = 0f;
// 		draggingPath = -1;
// 		draggingPosition = -1;
// 		CheckIfHomotopyDone ();
// 		yield break;
// 	}

// 	void CheckIfHomotopyDone ()
// 	{
// 		Debug.Log ("Start CheckIfHomotopyDone");
// 		var midPath = actHomotopy.midPath;
// 		var dotFromPos = midPath.dotFrom.transform.position;
// 		var dotToPos = midPath.dotTo.transform.position;
// 		var circleSize = level.dotPrefab.transform.localScale.x;
// 		var pathHomClasses = level.pathHomClasses;
// 		List<int> homClass = null;
// 		var indexOfPath1 = level.paths.IndexOf (actHomotopy.path1);
// 		foreach (var item in pathHomClasses) {
// 			if (item.Contains (indexOfPath1)) {
// 				homClass = item;
// 			}
// 		}
// 		Debug.Log ("Homclass of this homotopy contains " + string.Join (",", homClass.Select (x => x.ToString ()).ToArray ()));
// 		foreach (var otherPathNum in homClass) {
// 			if (otherPathNum != indexOfPath1) {
// 				var otherPath = level.paths [otherPathNum];
// 				Debug.Log ("Check Path " + otherPath.pathNumber);
// 				bool homotopic = true;
// 				Debug.Log ("Check if homotopic");
// 				for (int i = 0; i < midPath.Count; i++) {
// 					var midPathPos = midpath.GetPosition (i);
// 					if (Vector2.Distance (midPathPos, dotFromPos) > circleSize && Vector2.Distance (midPathPos, dotToPos) > circleSize) {
// 						bool existsNearNode = false;
// 						for (int j = 0; j < otherPath.Count; j++) {
// 							var otherPathPos = otherpath.GetPosition (j);
// 							var dist = Vector2.Distance (midPathPos, otherPathPos);
// 							if (dist < Statics.homotopyNearness) {
// 								existsNearNode = true;
// 								break;
// 							}
// 						}
// 						if (!existsNearNode) {
// 							homotopic = false;
// 							break;
// 						}	
// 					}
// 				}
// 				if (homotopic) {
// 					for (int i = 0; i < otherPath.Count; i++) {
// 						var otherPathPos = otherpath.GetPosition (i);
// 						if (Vector2.Distance (otherPathPos, dotFromPos) > circleSize && Vector2.Distance (otherPathPos, dotToPos) > circleSize) {
// 							bool existsNearNode = false;
// 							for (int j = 0; j < midPath.Count; j++) {
// 								var midPathPos = midpath.GetPosition (j);
// 								var dist = Vector2.Distance (otherPathPos, midPathPos);
// 								if (dist < Statics.homotopyNearness) {
// 									existsNearNode = true;
// 									break;
// 								}
// 							}
// 							if (!existsNearNode) {
// 								homotopic = false;
// 								break;
// 							}	
// 						}
// 					}

// 				}
// 				Debug.Log ("Check if homotopic, done : " + homotopic.ToString ());	
// 				if (homotopic) {
// 					if (Statics.mesh) {
// 						actHomotopy.setMesh (level.GetStaticPositions ());
// 					} else {
// 						actHomotopy.AddCurveToBundle (level.pathFactory, draggingPosition);
// 					}
// 					actHomotopy.SetColor (level.GetRandomColor ());
// 					otherPath.SetColor (midPath.color);
// 					Destroy (actHomotopy.midPath.line.gameObject);
// 					actHomotopy = null;
// 					break;
// 				}
// 			}
// 		}
// 	}

// 	void DrawLine3D (RaycastHit hit)
// 	{
// 		dot1 = level.dots.IndexOf (hit.collider.gameObject);
// 		if (dot1 != -1) {
// 			isDrawingPath = true;
// 			Debug.Log ("Instantiate");
// 			dotMove = Instantiate (level.dots [dot1]);
// 			dotMove.transform.localScale *= 0.05f;
// 			dotMove.GetComponent<BoxCollider> ().isTrigger = false;
// 			dotMove.GetComponent<Rigidbody> ().collisionDetectionMode = CollisionDetectionMode.Continuous;
// 			dotMove.transform.position = SetOnSurface (hit.point, hit);
// 			dotMove.transform.parent = actManifold.gameObject.transform.parent;
// 			trail = setTrail ();
// 			lineNormals = new List<Vector3> ();
// 		} else {
// 			Debug.Log ("Set DotMove to null");
// 			dotMove = null;
// 		}
// 	}

// 	TrailRenderer setTrail ()
// 	{
// 		var trail = dotMove.GetComponentInChildren<TrailRenderer> ();
// 		trail.textureMode = LineTextureMode.Tile;
// 		trail.material = level.trailMat;
// 		trail.startWidth = Statics.lineThickness;
// 		trail.endWidth = Statics.lineThickness;
// 		trailColor = level.GetRandomColor ();
// 		trail.minVertexDistance *= 0.1f;
// 		return trail;
// 	}

// 	void setPathToDrag (Vector3 touchPoint, int pathNumber)
// 	{
// 		int numOnPath = -1;
// 		Vector3 vector = new Vector3 ();
// 		bool isOnPath = GetPositionOnPath (pathNumber, touchPoint, ref numOnPath, ref vector);
// 		MPath path;
// 		if (pathNumber == -2) {
// 			path = actHomotopy.midPath;
// 		} else {
// 			path = level.paths [pathNumber];
// 		}
// 		if (isOnPath && numOnPath != 0 && numOnPath != path.Count - 1) {
// 			Debug.Log ("Start Dragging Line " + pathNumber);
// 			if (pathNumber != -2) {
// 				var path1 = level.paths [pathNumber];
// 				if (actHomotopy != null) {
// 					Destroy (GameObject.Find ("MidPath"));
// 					actHomotopy.Clear ();
// 					actHomotopy = null;
// 				}
// 				var actMidPath = level.NewPath (path1.color, "MidPath", path1.dotFrom, path1.dotTo);
// 				actHomotopy = level.NewHomotopy (path1, actMidPath);
// 			}
// 			upperBound = actHomotopy.midPath.line.positionCount;
// 			lowerBound = -1;
// 			draggingPath = -2;
// 			draggingPosition = numOnPath;
// 		} else {
// 			Debug.Log ("Not on Line");
// 		}
// 	}


// 	void DrawDot (Ray ray)
// 	{
// 		RaycastHit hit;
// 		foreach (var manifold in manifolds) {
// 			if (manifold.gameObject.GetComponent<MeshCollider> ().Raycast (ray, out hit, 20f)) {
// 				Debug.Log ("Touch not on some Path, so set Dot");
// 				GameObject dot;
// 				dot = Instantiate (level.dotPrefab3D);
// 				dot.transform.localScale *= 0.05f;
// 				dot.transform.position = SetOnSurface (hit.point, hit);
// 				dot.transform.forward = -hit.normal;
// 				dot.GetComponent<SpriteRenderer> ().material.SetColor ("_Color", level.GetRandomColor ());
// 				dot.transform.parent = manifold.gameObject.transform;
// 				level.addDot (dot);
// 			}
// 		}
// 	}

// 	static Ray GetRay ()
// 	{
// 		Ray ray = Camera.main.ScreenPointToRay (Input.GetTouch (0).position);
// 		return ray;
// 	}

// 	void FinishLine ()
// 	{
// 		if (dotMove.GetComponent<dotBehaviour3D> ().IsTriggered ()) {
// 			Debug.Log ("Triggered");
// 			var dotBehaviourDotMove = dotMove.GetComponent<dotBehaviour3D> ();
// 			var bumpedDot = dotBehaviourDotMove.GetTriggerObject ();
// 			dot2 = level.dots.IndexOf (bumpedDot);
// 			var path = level.NewPath (trailColor, level.dots [dot1], level.dots [dot2]);
// 			for (int i = 0; i < trail.positionCount; i++) {
// 				path.line.SetLocalPosition (i, trail.GetPosition (i));
// 				path.line.SetNormal (i, lineNormals [i]);
// 			}
// 			path.line.InsertPositionAt (0, level.dots [dot1].transform.position);
// 			path.line.InsertNormalAt (0, -level.dots [dot1].transform.forward);
// 			var count = path.Count;
// 			path.line.SetLocalPosition (count, bumpedDot.transform.position);
// 			path.line.SetNormal (count, -bumpedDot.transform.forward);
// 			path.line.SetMesh ();
// 			path.AttachTo (actManifold);
// 			//Set Colors
// 			dotMove.transform.position = bumpedDot.transform.position;
// 			dotMove.GetComponent <SpriteRenderer> ().enabled = false;
// 			StartCoroutine (DrawPath (path, false));
// 		} else {
// //			Destroy (dotMove);
// 		}
// 		Statics.isDragging = false;
// 	}

// 	void SetDotColors (int dot1, int dot2)
// 	{
// 		var group1 = new List<int> ();
// 		var group2 = new List<int> ();
// 		for (int i = 0; i < level.dotHomClasses.Count; i++) {
// 			var group = level.dotHomClasses [i];
// 			if (group.Contains (dot1)) {
// 				group1 = group;
// 			} else if (group.Contains (dot2)) {
// 				group2 = group;
// 			}
// 		}
// 		for (int i = 0; i < group2.Count; i++) {
// 			var item = group2 [i];
// 			var color = level.dots [dot1].GetComponent<SpriteRenderer> ().material.GetColor ("_Color");
// 			level.dots [item].GetComponent<SpriteRenderer> ().material.SetColor ("_Color", color);
// 			group1.Add (item);
// 		}
// 	}

// 	void ShortenPath (MPath path)
// 	{
// 		Debug.Log ("Shorten from start");
// 		var startPosition = path.dotFrom.transform.position;
// 		var node = 0;
// 		var counter = 0;
// 		while (node < path.Count) {
// 			if (Vector2.Distance (path.GetPosition (node), startPosition) >= dotRadius) {
// 				break;
// 			}
// 			counter++;
// 			node++;
// 		}
// 		for (int i = 0; i < node; i++) {
// 			path.line.RemovePosition (i);
// 			path.line.RemoveNormal (i);
// 		}
// 		Debug.Log ("Shorten " + counter + ", now end");
// 		counter = 0;
// 		var endPosition = path.dotTo.transform.position;
// 		node = path.Count - 1;
// 		while (node >= 0) {
// 			if (Vector2.Distance (path.GetPosition (node), endPosition) >= dotRadius) {
// 				break;
// 			}
// 			counter++;
// 			node--;
// 		}
// 		for (int i = 1; i < node; i++) {
// 			path.line.RemovePosition (path.Count - i);
// 			path.line.RemoveNormal (path.Count - i);
// 		}
// 		Debug.Log ("Shorten " + counter);
// 	}


// 	// end of update
// 	//Returns number of path, index on path
// 	bool GetPositionOnPath (int pathNumber, Vector3 touchPoint, ref int numOnPath, ref Vector3 posOnPath)
// 	{
// 		numOnPath = -1;
// 		float distToPath = Statics.lineThickness * 2f;
// 		float minDist = distToPath;
// 		if (pathNumber == -2) {
// 			var path = actHomotopy.midPath;
// 			for (int j = 0; j < path.Count; j++) {
// 				var value = path.GetPosition (j);
// 				var dist = Vector2.Distance (touchPoint, value);
// 				if ((dist < distToPath && numOnPath == -1) || (dist < minDist && numOnPath != -1)) {
// 					numOnPath = j;
// 					minDist = dist;
// 					posOnPath = value;
// 				}
// 			}
// 			if (numOnPath != -1) {
// 				return true;
// 			} else {
// 				return false;
// 			}
// 		} else {
// 			var path = level.paths [pathNumber];
// 			for (int j = 0; j < path.Count; j++) {
// 				var value = path.GetPosition (j);
// 				var dist = Vector2.Distance (touchPoint, value);
// 				if ((dist < distToPath && numOnPath == -1) || (dist < minDist && numOnPath != -1)) {
// 					numOnPath = j;
// 					minDist = dist;
// 					posOnPath = value;
// 				}
// 			}
// 			if (numOnPath != -1) {
// 				return true;
// 			} else {
// 				return false;
// 			}
// 		}
// 	}

// 	bool IsOnPath (Vector3 touchPoint, ref int pathNumber, bool checkMidPathFirst = false)
// 	{
// 		pathNumber = -1;
// 		if (checkMidPathFirst) {
// 			Debug.Log ("Check Midpath");
// 			var path = actHomotopy.midPath;
// 			var node = 0;
// 			while (node < path.Count) {
// 				var dist = Vector3.Distance (touchPoint, path.GetPosition (node));
// 				if (dist < minDist) {
// 					pathNumber = -2;
// 					return true;
// 				}
// 				node++;
// 			}
// 		}
// 		Debug.Log ("Check other Paths");
// 		for (int i = 0; i < level.paths.Count; i++) {
// 			var path = level.paths [i];
// 			var node = 0;
// 			while (node < path.Count) {
// 				var dist = Vector3.Distance (touchPoint, path.GetPosition (node));
// 				if (dist < minDist) {
// 					pathNumber = i;
// 					return true;
// 				}
// 				node++;
// 			}
// 		}
// 		return false;
// 	}

// 	bool IsOnMidPath (Vector3 worldPoint, ref int numOnPath, ref Vector3 vector)
// 	{
// 		numOnPath = -1;
// 		float distToPath = Statics.lineThickness * 3f;
// 		float minDist = distToPath;
// 		var path = actHomotopy.midPath;
// 		var node = 0;
// 		int counter = 0;
// 		while (node < path.Count) {
// 			var value = path.GetPosition (node);
// 			var dist = Vector2.Distance (worldPoint, value);
// 			if ((dist < distToPath && numOnPath == -1) || (dist < minDist && numOnPath != -1)) {
// 				numOnPath = counter;
// 				minDist = dist;
// 				vector = value;
// 			}
// 			counter++;
// 			node++;
// 		}
// 		if (numOnPath != -1) {
// 			return true;
// 		} else {
// 			return false;
// 		}
// 	}

// 	IEnumerator DrawPath (MPath path, bool onLoad)
// 	{
// 		Debug.Log ("DrawPath " + path);
// 		isCollided [path] = false;
// 		var line = path.line;
// 		line.gameObject.SetActive (false);
// 		path.smoothLine ();
// 		path.RefinePath (null, Statics.meanDist * 0.05f);
// 		ShortenPath (path);
// 		path.DrawArrows ();
// 		trail.Clear ();
// 		dotMove.SetActive (false);

// 		int sum = 0;
// 		var duration = 250f;
// 		if (onLoad) {
// 			duration = 200f;
// 		}
// 		float chunkSize = (float)path.Count / duration;
// 		var savedPositions = path.GetPositions ();
// 		line.ClearPositions ();
// 		var node = 0;
// 		line.gameObject.SetActive (true);
// 		for (int i = 0; i < savedPositions.Count; i++) {
// 			line.SetLocalPosition (i, savedPositions [node]);
// 			node++;
// 			sum++;
// 			if (sum > path.Count / duration) {
// 				sum = 0;
// 				line.SetMesh ();
// 				yield return null;
// 			}
// 		}

// 		SetDotColors (dot1, dot2);
// 		Destroy (dotMove);
// 		level.addPath (path);
// 		isDrawingPath = false;
// 		//		var pathObject = line.gameObject;
// 		//		var partsystem = pathObject.AddComponent<ParticleSystem> ();
// 		//		var renderer = partsystem.GetComponent<ParticleSystemRenderer> ();
// 		//		renderer.maxParticleSize = 0.01f;
// 		//		renderer.minParticleSize = 0.01f;
// 		//		renderer.material = partMaterial;
// 		//		var main = partsystem.main;
// 		//		main.maxParticles = 1;
// 		//		main.startSpeed = 0;
// 		//		main.startLifetime = 1000;
// 		//		main.playOnAwake = true;
// 		//		var emission = partsystem.emission;
// 		//		emission.rateOverTime = 0.5f;
// 		//		StartCoroutine (AddParticleSystem (path));

// 		yield break;
// 	}


// 	int GetNearestPointTo (Vector3 point, MLine line)
// 	{
// 		float smallestDistance = 100000f;
// 		int index = 0;
// 		for (int i = 0; i < line.positionCount; i++) {
// 			var dist = Vector2.Distance (point, line.GetLocalPosition (i));
// 			if (dist < smallestDistance) {
// 				index = i;
// 				smallestDistance = dist;
// 			}
// 		}
// 		return index;
// 	}

}