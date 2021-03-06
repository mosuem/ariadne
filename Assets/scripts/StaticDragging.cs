using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using UnityEditor;
using System;
using System.Linq;

public class StaticDragging : MonoBehaviour {
	LevelData level;
	public GameObject wallPrefab;

	void Start () {
		level = Camera.main.GetComponent<LevelData> ();
		setWalls ();
	}

	private void setWalls () {
		var leftPos = Camera.main.ScreenToWorldPoint (new Vector3 (0, Screen.height / 2, 50));
		var rightPos = Camera.main.ScreenToWorldPoint (new Vector3 (Screen.width, Screen.height / 2, 50));
		var topPos = Camera.main.ScreenToWorldPoint (new Vector3 (Screen.width / 2, Screen.height, 50));
		var bottomPos = Camera.main.ScreenToWorldPoint (new Vector3 (Screen.width / 2, 0, 50));

		const float width = 0.5f;
		var left = Instantiate (wallPrefab, leftPos, wallPrefab.transform.rotation);
		left.name = "Left Wall";
		left.transform.localScale = new Vector3 (width, 25, 20);
		var right = Instantiate (wallPrefab, rightPos, wallPrefab.transform.rotation);
		right.name = "Right Wall";
		right.transform.localScale = new Vector3 (width, 25, 20);
		var top = Instantiate (wallPrefab, topPos, wallPrefab.transform.rotation);
		top.name = "Top Wall";
		top.transform.localScale = new Vector3 (40, width, 20);
		var bottom = Instantiate (wallPrefab, bottomPos, wallPrefab.transform.rotation);
		bottom.name = "Bottom Wall";
		bottom.transform.localScale = new Vector3 (40, width, 20);
	}

	bool drawing;

	GameObject circle;

	GameObject rectangle;

	Vector3 drawPos;
	private GameObject draggingObstacle;
	private Vector3 draggingOffset;
	TouchHandler th = new TouchHandler();
	// Update
	void Update () {
		if (th.hasTouched(1)) { // && !EventSystem.current.IsPointerOverGameObject (Input.GetTouch (0).fingerId)
			Touch touch1 = th.GetTouch ();
			Vector3 touchPoint = Camera.main.ScreenToWorldPoint (touch1.position);
			touchPoint.z = 0;
			var cameraPosition = Camera.main.transform.position;
			if (touch1.phase == TouchPhase.Began) {
				if (Statics.drawCircle) {
					drawing = true;
					circle = Instantiate (level.staticCirclePrefab);
					drawPos = touchPoint + Statics.dotDepth;
					circle.transform.position = drawPos;
					circle.transform.localScale = Vector3.zero;
					level.addStatic (circle);
				} else if (Statics.drawRectangle) {
					drawing = true;
					rectangle = Instantiate (level.staticRectPrefab);
					drawPos = touchPoint + Statics.dotDepth;
					rectangle.transform.position = drawPos;
					rectangle.transform.localScale = Vector3.zero;
					level.addStatic (rectangle);
				} else if (Statics.deleteObstacle) {
					RaycastHit hit;
					bool hasHit = Physics.Raycast (cameraPosition, touchPoint - cameraPosition, out hit);
					if (hasHit && hit.collider.CompareTag ("Obstacle")) {
						level.statics.Remove (hit.collider.gameObject);
						Destroy (hit.collider.gameObject);
					}
					Statics.deleteObstacle = false;
				} else {
					RaycastHit hit;
					bool hasHit = Physics.Raycast (cameraPosition, touchPoint - cameraPosition, out hit);
					if (hasHit) {
						if (hit.collider.gameObject.CompareTag ("Obstacle") && level.staticsAllowed) {
							var index = level.statics.IndexOf (hit.collider.gameObject);
							StartDraggingObstacle (index, touchPoint);
						}
					}
				}
			} else if (touch1.phase == TouchPhase.Moved) {
				if (Statics.drawCircle) {
					circle.transform.localScale = Vector3.one * Vector3.Magnitude (circle.transform.position - touchPoint);
				} else if (Statics.drawRectangle) {
					var xFactor = drawPos.x - touchPoint.x;
					var yFactor = drawPos.y - touchPoint.y;
					rectangle.transform.localScale = new Vector3 (Mathf.Abs (xFactor), Mathf.Abs (yFactor), 0);
					rectangle.transform.position = drawPos - new Vector3 (xFactor / 2f, yFactor / 2f, 0);
				} else if (draggingObstacle != null) {
					DragObstacle (touchPoint);
				}
			} else if (touch1.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Canceled) {
				if (drawing) {
					Debug.Log ("Drawing ended");
					drawing = false;
					Statics.drawCircle = false;
					Statics.drawRectangle = false;
				}
				if (draggingObstacle != null) {
					Debug.Log ("Finish dragging gameObject");
					draggingObstacle = null;
				}
			}
		}
	}

	void StartDraggingObstacle (int indexOfStatic, Vector3 touchPoint) {
		if (indexOfStatic != -1) {
			draggingObstacle = level.statics[indexOfStatic];
			draggingOffset = draggingObstacle.transform.position - touchPoint;
		}
	}

	void DragObstacle (Vector3 touchPoint) {
		draggingObstacle.transform.position = touchPoint + draggingOffset;
	}
}