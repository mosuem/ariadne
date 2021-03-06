using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LineDragging : MonoBehaviour
{

	// Class Variables
	public float frequency = 1000000;
	public List<GameObject> dots = new List<GameObject> ();
	private int dot1;
	private GameObject dotMove;
	private int dot2;

	public List<LinkedList<Vector2>> paths = new List<LinkedList<Vector2>> ();
	private List<LineRenderer> lines = new List<LineRenderer> ();
	private Dictionary<int, Vector3> colliderPos = new Dictionary<int, Vector3> ();
	private List<bool> isCollided = new List<bool> ();
	private List<Dictionary<int, Vector2>> colliderNormals = new List<Dictionary<int, Vector2>> ();
	private HashSet<int> angleGreaterThan90 = new HashSet<int> ();
	public Material mat;
	private List<Color> myColors = new List<Color> ();
	private string[] myHexColors = {
		"#E57373",//Red
		"#F06292",//Pink
		"#BA68C8",//Purple
		"#9575CD",//Deep Purple
		"#7986CB",//Indigo
		"#64B5F6",//Blue
		"#4FC3F7",//Light Blue
		"#4DD0E1",//Cyan
		"#4DB6AC",//Teal
		"#81C784",//Green
		"#AED581",//Light Green
		"#DCE775",//lime
		"#FF8A65",//Deep Orange
		"#A1887F",//Brown
	};
	private GameObject arrow;
	private int arrowT0;
	private int arrowT1;
	private int firstPath = -1;
	LineRenderer newLine;
	const float lineThickness = 0.05f;
	const float minDist = lineThickness / 5f;
	private bool isMorphing = false;


	void Start ()
	{
		setColors ();
		for (int i = 0; i < dots.Count; i++) {
			dots [i].GetComponent<SpriteRenderer> ().color = GetRandomColor ();
		}
		//CreateRope ();
	}

	// Update
	void Update ()
	{
		int tapCount = Input.touchCount;
		if (tapCount == 1) {// && !EventSystem.current.IsPointerOverGameObject (Input.GetTouch (0).fingerId)
			Touch touch1 = Input.GetTouch (0);
			Vector2 touchOrigin = touch1.position;
			Vector2 worldPoint = Camera.main.ScreenToWorldPoint (touchOrigin);
			if (touch1.phase == TouchPhase.Began) {
				if (!isMorphing) {
					//Draw Arrow
					int pathNum = -1;
					int numOnPath = -1;
					bool isOnPath = IsOnPath (worldPoint, ref pathNum, ref numOnPath);
					Debug.Log ("isonpath: " + isOnPath + " " + pathNum + " " + numOnPath);
					if (isOnPath) {
						firstPath = pathNum;
						arrowT0 = numOnPath;
						arrow = new GameObject ();
						var line = arrow.AddComponent<LineRenderer> ();
						line.positionCount = 2;
						line.SetPosition (0, worldPoint);
						line.SetPosition (1, worldPoint);
						line.startWidth = lineThickness;
						line.endWidth = lineThickness;
					}
				}
			} else if (touch1.phase == TouchPhase.Moved) {
				Vector3 touchPos = Camera.main.ScreenToWorldPoint (new Vector3 (touch1.position.x, touch1.position.y, 0));
				touchPos.z = 0;
				var target = new Vector2 (touchPos.x, touchPos.y);
				if (arrow) {
					arrow.GetComponent<LineRenderer> ().SetPosition (1, target);
				}
				//dotMove.transform.position = touchPos;
			} else if (touch1.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Canceled) {
				if (arrow) {
					int pathNum = -1;
					int numOnPath = -1;
					bool isOnPath = IsOnPath (worldPoint, ref pathNum, ref numOnPath);
					Debug.Log ("isonpath: " + isOnPath + " " + pathNum + " " + numOnPath);
					if (isOnPath && pathNum != firstPath) {
						arrowT1 = numOnPath;
						float t0 = (float)arrowT0 / paths [firstPath].Count;
						float t1 = (float)arrowT1 / paths [pathNum].Count;
						setMorph (t0, t1, firstPath, pathNum);
					} 
					firstPath = -1;
					Destroy (arrow);
				}
			}
		}
	}

	void setColors ()
	{
		foreach (var hex in myHexColors) {
			Color color;
			ColorUtility.TryParseHtmlString (hex, out color);
			myColors.Add (color);
		}
	}

	Color GetRandomColor ()
	{
		var rand = Random.Range (0, myColors.Count);
		var color = myColors [rand];
		myColors.RemoveAt (rand);
		return color;
	}

	// end of update
	//Returns number of path, index on path
	bool IsOnPath (Vector2 worldPoint, ref int pathNumber, ref int numOnPath)
	{
		pathNumber = -1;
		float minDist = 1000f;
		for (int i = 0; i < paths.Count; i++) {
			if (firstPath != i) {
				var path = paths [i];
				var node = path.First;
				for (int j = 0; j < path.Count; j++) {
					var dist = Vector2.Distance (worldPoint, node.Value);
					if ((dist < 0.5f && pathNumber == -1) || (dist < minDist && pathNumber != -1)) {
						pathNumber = i;
						numOnPath = j;
						minDist = dist;
					}
					node = node.Next;
				}
				if (pathNumber != -1) {
					return true;
				}
			}
		}
		return false;
	}

	public void setMorphDiag ()
	{
		//setMorph ();
	}

	public void setMorphStraight ()
	{
		//setMorph ();
	}

	public void setMorph (float t0, float t1, int pathNum1, int pathNum2)
	{
		Debug.Log ("Start setMorph to Paths " + pathNum1 + " and " + pathNum2);
		StopCoroutine (MorphLines (t0, pathNum1, pathNum2));
		isMorphing = true;
		if (newLine != null) {
			Destroy (newLine.gameObject);
		}
		t0 = Mathf.Clamp01 (t0);
		t1 = Mathf.Clamp01 (t1);
		var path1 = paths [pathNum1];
		var path2 = paths [pathNum2];
		var gameObject2 = new GameObject ("NewPath");
		gameObject2.SetActive (false);
		newLine = gameObject2.AddComponent<LineRenderer> ();
		newLine.positionCount = lines [pathNum1].positionCount;
		newLine.startWidth = lineThickness;
		newLine.endWidth = lineThickness;

		var colliderNormal = colliderNormals [pathNum1];
		var isPath1Collided = isCollided [pathNum1];

		angleGreaterThan90.Clear ();
		colliderNormal.Clear ();
		for (int i = 0; i < path1.Count; i++) {
			var point1 = lines [pathNum1].GetPosition (i);
			var t = (float)i / (float)(path1.Count - 1);
			Vector3 goalPoint;
			float b = Mathf.Min (t0, 1f - t0);
			if (t < t0 - b || t > t0 + b) {
				goalPoint = point1;
			} else {
				var line2 = lines [pathNum2];
				float etaT;
				if (t >= t0) {
					etaT = (t - t0) / (1f - t0) * (1f - t1) + t1;
				} else {
					etaT = t1 - (t0 - t) / t0 * t1;
				}
				var tP2 = etaT * (float)(path2.Count - 1);
				var point2 = line2.GetPosition (Mathf.RoundToInt (tP2));
				float f = 0f;
				if (t < t0) {
					f = Mathf.SmoothStep (0f, 1f, (t - (t0 - b)) / b);
				} else {
					f = Mathf.SmoothStep (0f, 1f, -(t - (t0 + b)) / b);
				}
				goalPoint = f * point2 + (1f - f) * point1;
				if (Vector2.Distance (goalPoint, point2) < minDist) {
					goalPoint = point2;
				}

				RaycastHit2D hit = Physics2D.CircleCast (point1, lineThickness * 0.5f, goalPoint - point1, Vector2.Distance (point1, goalPoint));
				if (hit.collider != null && hit.collider.isTrigger == false) {
					var normal = GetNormal (hit.collider, hit.point);
					colliderNormal [i] = normal;
					float angle = Vector2.Angle (normal, point2 - point1);
					if (angle > 90) {
						//pull
						angleGreaterThan90.Add (i);
					}
					colliderPos [i] = hit.point + normal * (lineThickness * 0.5f);
					goalPoint = colliderPos [i];
				} else {
					colliderNormal.Remove (i);
				}
			}
			newLine.SetPosition (i, goalPoint);
		}
		if (colliderNormal.Count > 0) {
			isCollided [pathNum1] = true;
		}
		StartCoroutine (MorphLines (t0, pathNum1, pathNum2));
		Debug.Log ("End setMorph to Paths " + pathNum1 + " and " + pathNum2);
	}

	/**
	 * 		1		2		3
	 *			 _________
	 *			|		 |
	 *			|		 |  4
	 *		8	|		 |
	 *			|________|
	 * 						
	 * 		7		6		5
	 **/
	Vector2 GetNormal (Collider2D collider, Vector3 collPoint)
	{
		Vector3 normal;
		if (collider.GetType () == typeof(BoxCollider2D)) {
			var coll = (BoxCollider2D)collider;
			var center = coll.bounds.center;
			var halfWidth = coll.size.x / 2;
			var halfHeight = coll.size.y / 2;
			if (collPoint.x > center.x + halfWidth) {
				//Right Side
				if (collPoint.y > center.y + halfHeight) {
					//Top Side
					//3
					normal = collPoint - center;
					normal.Normalize ();
				} else if (collPoint.y > center.y + halfHeight) {
					//Bottom Side
					// 5
					normal = collPoint - center;
					normal.Normalize ();
				} else {
					//Mid
					//4
					normal = Vector2.right;
				}
			} else if (collPoint.x < center.x - halfWidth) {
				//Left Side
				if (collPoint.y > center.y + halfHeight) {
					//Top Side
					//1
					normal = collPoint - center;
					normal.Normalize ();
				} else if (collPoint.y > center.y + halfHeight) {
					//Bottom Side
					// 7
					normal = collPoint - center;
					normal.Normalize ();
				} else {
					//Mid
					//8
					normal = Vector2.left;
				}
			} else {
				//Up or Down
				if (collPoint.y > center.y + halfHeight) {
					//Top Side
					//2
					normal = Vector2.up;
				} else {
					//Bottom Side
					//6
					normal = Vector2.down;
				}
			}
		} else if (collider.GetType () == typeof(CircleCollider2D)) {
			var coll = (CircleCollider2D)collider;
			normal = collPoint - coll.bounds.center;
			normal.Normalize ();
		} else {
			normal = Vector2.zero;
		}
		//Debug.DrawLine (collPoint, collPoint + normal, Color.red, 5);
		return normal;
	}


	//					GameObject circle = GameObject.CreatePrimitive (PrimitiveType.Sphere);
	//					circle.transform.localScale = Vector3.one * 0.05f;
	//					circle.transform.position = goalPoint;
	void smoothLine (int pathNum)
	{
		var line = lines [pathNum];
		if (line.positionCount > 3) {
			var colliderNormal = colliderNormals [pathNum];
			for (int i = 0; i < line.positionCount - 2; i++) {
				bool isOnLeftEdge = colliderNormal.ContainsKey (i) && !colliderNormal.ContainsKey (i + 2);
				bool isOnRightEdge = colliderNormal.ContainsKey (i + 2) && !colliderNormal.ContainsKey (i);
				if (!isOnLeftEdge && !isOnRightEdge) {
					var p1 = line.GetPosition (i);
					var p2 = line.GetPosition (i + 2);
					var midPoint = Vector3.Lerp (p1, p2, 0.5f);
					RaycastHit2D hit = Physics2D.CircleCast (p1, lineThickness * 0.5f, p2 - p1, Vector2.Distance (p1, p2));
					if (hit.collider != null && hit.collider.isTrigger == false) {
						//Nothing
					} else {
						Debug.DrawLine (p1, p2, Color.red, 2);
						line.SetPosition (i + 1, midPoint);
					}
				}
			}
		}
	}

	void RefinePath (int i)
	{
		float meanDist = 0.01f;
		var path = paths [i];
		Debug.Log ("Refining Path " + i + ", length before: " + path.Count);
		Debug.Log ("Path count = " + path.Count);
		if (path.Count > 0) {
			var node = path.First;
			var nextStepNode = node.Next;
			while (node.Next != null) {
				var point1 = node.Value;
				var point2 = node.Next.Value;
				var distance = Vector2.Distance (point1, point2);
				if (distance > meanDist) {
					var vector2 = Vector2.Lerp (point1, point2, 0.5f);
					path.AddAfter (node, vector2);
				} else if (distance < meanDist / 2.5f) {
					path.Remove (node.Next);
				} else {
					node = node.Next;
				}
			}
		}
		var line = lines [i];
		line.positionCount = path.Count;
		var node2 = path.First;
		for (int j = 0; j < path.Count; j++) {
			line.SetPosition (j, node2.Value);
			node2 = node2.Next;
		}
		Debug.Log ("Refined Path " + i + ", length after: " + path.Count);
	}

	public float SamplePathLength (int pathNum)
	{
		var list = paths [pathNum];
		float sum = 0f;
		var node = list.First;
		var count = (list.Count - 1);
		var numSamples = Mathf.Max (count / 5, 30);
		int k = Mathf.Min (count, numSamples);
		int step = count / k;
		for (int i = 0; i < k; i++) {
			sum += Vector2.Distance (node.Value, node.Next.Value);
			for (int j = 0; j < step; j++) {
				node = node.Next;
			}
		}
		if (sum == 0) {
			return 1f;
		}
		sum = sum / (float)k;
		Debug.Log ("Sampled Length is " + sum);
		return sum;
	}

//	void DrawPath (int pathNum)
//	{
//		isCollided.Add (false);
//		colliderNormals.Add (new Dictionary<int, Vector2> ());
//		var path = paths [pathNum];
//		var line = new GameObject ("Path " + pathNum).AddComponent<LineRenderer> ();
//		line.GetComponent<Renderer> ().material = mat;
//		line.GetComponent<Renderer> ().material.SetColor ("_Color", trailColor);
//		lines.Add (line);
//		RefinePath (pathNum);
//		line.startWidth = lineThickness;
//		line.endWidth = lineThickness;
//	}
//
	IEnumerator MorphLines (float t0, int pathNum1, int pathNum2)
	{
		bool localmorph = true;
		var path = paths [pathNum1];
		var colliderNormal = colliderNormals [pathNum1];
		Debug.Log ("Start MorphLines Path " + pathNum1);
		var upperBound = path.Count;
		var upperBound2 = path.Count;
		var lowerBound = -1;
		var lowerBound2 = -1;
		float b = Mathf.Min (t0, 1f - t0);
		float smoothStartTime = 1f;
		float timePassed = 0f;
		float minStepLength = 0.001f;
		HashSet<int> collidedPoints = new HashSet<int> ();
		while (localmorph) {
			localmorph = false;
			//Make a smooth start
			timePassed += Time.deltaTime;
			var smoothStartFactor = Mathf.SmoothStep (0, 1, timePassed / smoothStartTime);

			for (int i = arrowT0 + 1; i < path.Count; i++) {
				if (i < upperBound && !collidedPoints.Contains (i)) {
					var point1 = lines [pathNum1].GetPosition (i);
					var point2 = newLine.GetPosition (i);

					float t = (float)i / path.Count;
					var f = Mathf.SmoothStep (0f, 1f, -(t - (t0 + b)) / b);
					var factor = Mathf.SmoothStep (0, 1, f);
					var stepLength = factor * Time.deltaTime;
					var goalPoint = Vector2.MoveTowards (point1, point2, stepLength * smoothStartFactor);

					var dist = Vector2.Distance (goalPoint, point2);
					if (dist < minDist) {
						bool iCantMoveThis = angleGreaterThan90.Contains (i);
						if (iCantMoveThis) {
							upperBound2 = i;
							//TODO: some kind of fadeout, meaning that in this loop the next points are moved only by some percentage.
						}
					}
					lines [pathNum1].SetPosition (i, goalPoint);
					if (dist > minDist && stepLength > minStepLength) {
						localmorph = true;
					}	
				} else {
					break;
				}
			}
			upperBound = upperBound2;
			for (int i = arrowT0; i > -1; i--) {
				if (i > lowerBound && !collidedPoints.Contains (i)) {
					var point1 = lines [pathNum1].GetPosition (i);
					var point2 = newLine.GetPosition (i);

					float t = (float)i / path.Count;
					float f = Mathf.SmoothStep (0f, 1f, (t - (t0 - b)) / b);
					var factor = Mathf.SmoothStep (0, 1, f);
					var stepSize = factor * Time.deltaTime;
					var goalPoint = Vector2.MoveTowards (point1, point2, stepSize * smoothStartFactor);

					var dist = Vector2.Distance (goalPoint, point2);
					if (dist < minDist) {
						bool iCantMoveThis = angleGreaterThan90.Contains (i);
						if (iCantMoveThis) {
							lowerBound2 = i;
						}
					} 
					lines [pathNum1].SetPosition (i, goalPoint);
					if (dist > minDist && stepSize > minStepLength) {
						localmorph = true;
					}	
				} else {
					break;
				}
			}
			lowerBound = lowerBound2;
			yield return null;
		}
		smoothLine (pathNum1);
		Debug.Log ("Start setting Path to Line");
		var node = path.First;
		var line = lines [pathNum1];
		for (int i = 0; i < path.Count; i++) {
			node.Value = line.GetPosition (i);
			node = node.Next;
		}
		Debug.Log ("End setting Path to Line");
		RefinePath (pathNum1);
		//EditorApplication.isPaused = true;
		if (newLine != null) {
			Destroy (newLine.gameObject);
		}
		var node1 = paths [pathNum1].First;
		var node2 = paths [pathNum2].First;
		bool homotopic = true;
		Debug.Log ("Check if homotopic");
		while (node1.Next != null) {
			bool existsNearNode = false;
			while (node2.Next != null) {
				var dist = Vector2.Distance (node1.Value, node2.Value);
				if (dist < lineThickness * 4f) {
					existsNearNode = true;
					break;
				}
				node2 = node2.Next;
			}
			node1 = node1.Next;
			node2 = paths [pathNum2].First;
			if (!existsNearNode) {
				homotopic = false;
				break;
			}
		}
		if (homotopic) {
			node1 = paths [pathNum2].First;
			node2 = paths [pathNum1].First;
			while (node1.Next != null) {
				bool existsNearNode = false;
				while (node2.Next != null) {
					var dist = Vector2.Distance (node1.Value, node2.Value);
					if (dist < lineThickness * 4f) {
						existsNearNode = true;
						break;
					}
					node2 = node2.Next;
				}
				node1 = node1.Next;
				node2 = paths [pathNum1].First;
				if (!existsNearNode) {
					homotopic = false;
					break;
				}
			}
		}
		Debug.Log ("Check if homotopic, done : " + homotopic.ToString ());
		if (homotopic) {
			StartCoroutine (Homotopy (pathNum1, pathNum2));
			lines [pathNum2].GetComponent<Renderer> ().material.SetColor ("_Color", lines [pathNum1].GetComponent<Renderer> ().material.GetColor ("_Color"));
		}
		isMorphing = false;
		Debug.Log ("End MorphLines Path " + pathNum1);
		yield break;
	}

	IEnumerator Homotopy (int pathNum1, int pathNum2)
	{
		var path = paths [pathNum1];
		Debug.Log ("Start MorphLines Path " + pathNum1);
		float minStepLength = 0.001f;

		bool localmorph = true;
		while (localmorph) {
			localmorph = false;

			for (int i = 0; i < path.Count; i++) {
				var point1 = lines [pathNum1].GetPosition (i);
				var eta = (float)i / path.Count;
				var t = eta * paths [pathNum2].Count;
				var point2 = lines [pathNum2].GetPosition (Mathf.RoundToInt (t));
				var stepLength = Time.deltaTime;
				var goalPoint = Vector2.MoveTowards (point1, point2, stepLength);

				var dist = Vector2.Distance (goalPoint, point2);
				lines [pathNum1].SetPosition (i, goalPoint);

				if (dist > minDist && stepLength > minStepLength) {
					localmorph = true;
				}	
			}
			yield return null;
		}
		Debug.Log ("Start setting Path to Line");
		var node = path.First;
		var line = lines [pathNum1];
		for (int i = 0; i < path.Count; i++) {
			node.Value = line.GetPosition (i);
			node = node.Next;
		}	
		Debug.Log ("End setting Path to Line");
		//EditorApplication.isPaused = true;
		Destroy (lines [pathNum2].gameObject);
		lines.RemoveAt (pathNum2);
		paths.RemoveAt (pathNum2);
		Debug.Log ("End MorphLines Path ");
		yield break;
	}

}