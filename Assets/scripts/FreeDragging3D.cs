using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using UnityEditor;
using System;
using System.Linq;

public class FreeDragging3D : MonoBehaviour {
    // Class Variables
    public float frequency = 1000000;
    public float pathResolution;
    private int dot1;
    private GameObject dotMove;
    private int dot2;

    private Dictionary<MPath, bool> isCollided = new Dictionary<MPath, bool> ();
    private HashSet<int> angleGreaterThan90 = new HashSet<int> ();
    //private GameObject node;
    private TrailRenderer trail;

    MLine newLine;
    private float minDist = Mathf.Max (Statics.lineThickness * 2f, Statics.dotSpacer * 2f);
    public Material partMaterial;

    private float dotRadius = 0.3f;
    internal LevelData level;

    private float touchDuration = 0f;
    private Vector3 touchStart;

    public Homotopy actHomotopy = null;

    GameObject circle = null;
    GameObject rectangle = null;

    private bool drawing = false;
    private Vector3 drawPos;
    private int draggingPath = -1;
    private int draggingPosition = -1;
    private Vector3 lastTouch = Vector3.zero;
    private float distSoFar = 0f;
    private int upperBound;
    private int lowerBound;
    private GameObject draggingObstacle;
    private Vector3 draggingOffset;

    List<Vector3> lineNormals = new List<Vector3> ();

    bool isDrawingPath = false;

    //3D----------
    public List<Manifold> manifolds = new List<Manifold> ();
    Manifold actManifold;
    static Vector3 farAwayVec = new Vector3 (100f, 100f, 100f);
    Vector3 touchPos = farAwayVec;

    DirectionParticleSystem ps;
    private bool dontDrawDot;

    void Start () {
        Debug.Log ("levelType:" + Statics.levelType);
        level = Camera.main.GetComponent<LevelData> ();

        Debug.Log ("levelType:" + Statics.levelType);
        ps = gameObject.AddComponent<DirectionParticleSystem> ();
        ps.partMaterial = partMaterial;
        ps.trailMaterial = new Material (level.pathMat);

        Manifold manifold;
        // Manifold manifold2 = null;
        if (Statics.isSphere) {
            manifold = Misc.BuildSphere (level.manifoldMat, Color.grey);
        } else if (Statics.isTorus) {
            manifold = Misc.BuildTorus (level.manifoldMat, Color.grey);
        } else {
            Statics.isTorus = true;
            manifold = Misc.BuildTorus (level.manifoldMat, Color.grey);
            // manifold = Misc.BuildHalfTorus (level.manifoldMat, Color.grey);
            // manifold2 = Misc.BuildDisc (level.manifoldMat, Color.yellow);
        }
        manifolds.Add (manifold);
        // manifolds.Add (manifold2);
        Debug.Log ("Draw Paths");
        for (int i = 0; i < level.paths.Count; i++) {
            Debug.Log ("Draw Path " + i);
            var path = level.paths[i];
            StartCoroutine (DrawPath (path, true));
            path.AttachTo (manifold);
        }
        Debug.Log ("End draw Paths");
        var color = manifold.gameObject.GetComponent<MeshRenderer> ().material.color;
        color.a = 0.5f;
        manifold.gameObject.GetComponent<MeshRenderer> ().material.color = color;
        Camera.main.gameObject.transform.parent.transform.Rotate (new Vector3 (45, 0, 0));
        Statics.isLoading = false;

        // List<int> bound1 = new List<int> ();
        // List<int> bound2 = new List<int> ();
        // for (int i = 1692; i < 1728; i++) {
        //     bound1.Add (i);
        // }
        // for (int i = 19; i < 73; i++) {
        //     bound2.Add (i);
        // }
        // for (int i = 1; i < 19; i++) {
        //     bound2.Add (i);
        // }
        // bound1.Reverse();
        // StartCoroutine (GlueManifolds (manifold, bound1, manifold2, bound2, true));
    }

    // Update
    void Update () {
        int tapCount = Input.touchCount;
        if (tapCount == 1 && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject (Input.GetTouch (0).fingerId)) { // && !EventSystem.current.IsPointerOverGameObject (Input.GetTouch (0).fingerId)
            Touch touch1 = Input.GetTouch (0);
            Vector3 touchPoint = Camera.main.ScreenToWorldPoint (touch1.position);
            touchPoint.z = 0;
            if (touch1.phase == TouchPhase.Began) {
                UpdateStart (touch1, touchPoint);
            } else if (touch1.phase == TouchPhase.Moved) {
                UpdateMove (touch1, touchPoint);
            } else if (touch1.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Canceled) {
                UpdateEnd (touchPoint);
            }
        } else if (tapCount == 2 && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject (Input.GetTouch (0).fingerId) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject (Input.GetTouch (1).fingerId)) {
            UpdateConcatenation ();
        }
    }

    private void UpdateConcatenation () {
        Touch touch1 = Input.GetTouch (0);
        Touch touch2 = Input.GetTouch (1);
        if (touch2.phase == TouchPhase.Began) {
            RaycastHit hit3D1 = new RaycastHit ();
            var isHit1 = Physics.Raycast (Camera.main.ScreenPointToRay (touch1.position), out hit3D1, 100f);

            RaycastHit hit3D2 = new RaycastHit ();
            var isHit2 = Physics.Raycast (Camera.main.ScreenPointToRay (touch2.position), out hit3D2, 100f);

            if (isHit1 && isHit2) {
                Debug.Log ("Try Concat");
                var touchPoint1 = hit3D1.point;
                var touchPoint2 = hit3D2.point;
                TryConcatenation (touchPoint1, touchPoint2);
            }
        }
    }

    private void UpdateStart (Touch touch1, Vector3 touchPoint) {
        touchStart = touchPoint;
        touchDuration = Time.time;
        var ray = Camera.main.ScreenPointToRay (touch1.position);
        var hits = Physics.RaycastAll (ray);
        foreach (var hit3D in hits) {
            if (hit3D.collider.gameObject.CompareTag ("Dot") && level.pathDrawingAllowed && !isDrawingPath) {
                Ray ray2 = Camera.main.ScreenPointToRay (touch1.position);
                RaycastHit hit = new RaycastHit ();
                foreach (var manifold in manifolds) {
                    if (manifold.gameObject.GetComponent<MeshCollider> ().Raycast (ray2, out hit, 20f)) {
                        actManifold = manifold;
                        DrawLine3D (hit3D);
                        Statics.isDragging = true;
                        break;
                    }
                }
            } else if (hit3D.collider.gameObject.CompareTag ("Obstacle") && level.staticsAllowed) {
                var index = level.statics.IndexOf (hit3D.collider.gameObject);
                StartDraggingObstacle (index, touchPoint);
                Statics.isDragging = true;
            } else if (hit3D.collider.gameObject.CompareTag ("Sphere") && level.homotopiesAllowed) {
                int pathNumber = -1;
                Misc.DebugSphere (hit3D.point, Color.red, "Touchpoint");
                bool isOnPath = hitPath (Camera.main.transform.position, hit3D.point, ref pathNumber, true);
                // bool isOnPath = IsOnPath (hit3D.point, ref pathNumber, actHomotopy != null, true);
                if (isOnPath) {
                    dontDrawDot = true;
                    Debug.Log ("Is on Path!");
                    if (Statics.cutPath) {
                        Debug.Log ("Cut Path");
                        Statics.cutPath = false;
                        StartCoroutine (CutPath (pathNumber));
                    } else if (Statics.gluePaths && Statics.path1Set == -1) {
                        Debug.Log ("Set Path 1");
                        Statics.path1Set = pathNumber;
                    } else if (Statics.gluePaths && Statics.path1Set != -1) {
                        Debug.Log ("Set Path 2");
                        if (pathNumber == Statics.path1Set) {
                            Debug.Log ("Cap along Path " + pathNumber);
                            StartCoroutine (CapPath (pathNumber));
                        } else {
                            Debug.Log ("Cap along Paths " + Statics.path1Set + " and " + pathNumber);
                            Manifold manifold1 = level.paths[Statics.path1Set].parentManifold;
                            Manifold manifold2 = level.paths[pathNumber].parentManifold;
                            StartCoroutine (GlueManifolds (level.paths[Statics.path1Set], level.paths[pathNumber], true));
                        }
                        Statics.gluePaths = false;
                        Statics.path1Set = -1;
                    } else {
                        Debug.Log ("Started Dragging Path");
                        // setPathToDrag (hit3D.point, pathNumber);
                        // Statics.isDragging = true;
                        // touchPos = farAwayVec;
                    }
                } else {
                    draggingPath = -1;
                    Debug.Log ("Tried Dragging, but no path there");
                }
            }
        }
    }

    private IEnumerator GlueManifolds (MPath mPath1, MPath mPath2, bool isClosed) {
        var m1 = mPath1.parentManifold;
        var m2 = mPath2.parentManifold;
        MeshCutter meshCutter = m1.gameObject.GetComponent<MeshCutter> ();

        var list = new List<List<int>> ();
        list.AddRange (m2.getBoundaries ());
        meshCutter.Add (m2, ref list);
        m2.Destroy ();
        var boundaries = meshCutter.manifold.getBoundaries ();
        var bound1 = GetBoundFromPath (m1, boundaries, mPath1);
        var bound2 = GetBoundFromPath (m1, boundaries, mPath2);
        Debug.Log ("Glue paths " + bound1.Count + " and " + bound2.Count);
        yield return StartCoroutine (meshCutter.Glue (bound1, bound2, isClosed));
    }

    private List<int> GetBoundFromPath (Manifold m1, List<List<int>> boundaries, MPath path) {
        var vertices = m1.gameObject.GetComponent<MeshFilter> ().mesh.vertices.ToList ();
        foreach (var bound in boundaries) {
            var found = true;
            for (int i = 0; i < bound.Count; i++) {
                var index = vertices.IndexOf (path.GetPosition (i));
                if (index == -1) {
                    found = false;
                    break;
                }
            }
            if (found) {
                return bound;
            }
        }
        return null;
    }

    private IEnumerator GlueManifold (List<int> bound1, List<int> bound2, bool isClosed) {
        MeshCutter meshCutter = manifolds.First ().gameObject.GetComponent<MeshCutter> ();
        yield return StartCoroutine (meshCutter.Glue (bound1, bound2, isClosed));
    }

    private IEnumerator GlueManifolds (Manifold m1, List<int> bound1, Manifold m2, List<int> bound2, bool isClosed) {
        MeshCutter meshCutter = manifolds.First ().gameObject.GetComponent<MeshCutter> ();
        var list = new List<List<int>> ();
        list.Add (bound2);
        meshCutter.Add (m2, ref list);
        m2.Destroy ();
        var boundaries = meshCutter.manifold.getBoundaries ();
        yield return StartCoroutine (meshCutter.Glue (boundaries[0], boundaries[1], isClosed));
    }

    private IEnumerator CutPath (int pathNumber) {
        MPath pathToCut = level.paths[pathNumber];
        MeshCutter meshCutter = pathToCut.parentManifold.gameObject.GetComponent<MeshCutter> ();
        Debug.Log ("Cut path " + pathNumber + " on manifold " + pathToCut.parentManifold.gameObject.name);
        yield return StartCoroutine (meshCutter.Cut (pathToCut));
        // foreach (var manifold in meshCutter.manifolds) {
        // Vector3[] vertices = manifold.gameObject.GetComponent<MeshFilter> ().mesh.vertices;
        // foreach (var bound in manifold.getBoundaries ()) {
        //     for (int i = 0; i < bound.Count; i++) {
        //         int item = (int) bound[i];
        //         Misc.DebugSphere (vertices[item], Color.red, "Bound " + i + " of manifold " + manifold.gameObject.name);
        //     }
        // }
        // }
        var startDot = pathToCut.dotFrom;
        var cutPieces = meshCutter.manifolds;
        manifolds.Remove (pathToCut.parentManifold);
        pathToCut.parentManifold.Destroy ();
        level.paths.RemoveAt (pathNumber);
        foreach (var newManifold in cutPieces) {
            var vertices = newManifold.gameObject.GetComponent<MeshFilter> ().mesh.vertices;
            var normals = newManifold.gameObject.GetComponent<MeshFilter> ().mesh.normals;
            var startDot2 = Instantiate (startDot);
            startDot2.transform.parent = newManifold.gameObject.transform;
            MPath path = level.NewPath (startDot2, startDot2);

            var boundary = meshCutter.newBoundaries[newManifold];
            for (int j = 0; j < boundary.Count; j++) {
                path.SetPosition (j, vertices[boundary[j]]);
            }
            var list = new List<Vector3> ();
            for (int i1 = 0; i1 < boundary.Count; i1++) {
                list.Add (normals[boundary[i1]]);
            }
            path.SetNormals (list);
            path.SetMesh ();

            level.addPath (path);
            path.AttachTo (newManifold);
            Debug.Log ("Add Path " + level.paths.Count + " to manifold " + newManifold.gameObject.name);
        }
        manifolds.AddRange (cutPieces);
        // manifolds.Add (cutPieces[);
    }

    private IEnumerator CapPath (int pathNumber) {
        MPath pathToCap = level.paths[pathNumber];
        MeshCutter meshCutter = pathToCap.parentManifold.gameObject.GetComponent<MeshCutter> ();
        yield return StartCoroutine (meshCutter.Cap (pathToCap));
    }

    private void UpdateMove (Touch touch1, Vector3 touchPoint) {
        /**
         * 
         * 					MOVE
         * 
         * 
         * */
        if (Statics.drawCircle) {
            circle.transform.localScale = Vector3.one * Vector3.Magnitude (circle.transform.position - touchPoint);
        } else if (Statics.drawRectangle) {
            var xFactor = drawPos.x - touchPoint.x;
            var yFactor = drawPos.y - touchPoint.y;
            rectangle.transform.localScale = new Vector3 (xFactor, yFactor, 0);
            rectangle.transform.position = drawPos - new Vector3 (xFactor / 2f, yFactor / 2f, 0);
        } else if (draggingObstacle != null) {
            DragObstacle (touchPoint);
        } else {
            Ray ray = Camera.main.ScreenPointToRay (touch1.position);
            RaycastHit hit = new RaycastHit ();
            if (dotMove != null) {
                if (actManifold.gameObject.GetComponent<MeshCollider> ().Raycast (ray, out hit, 50f)) {
                    lastTouch = touchPos;
                    touchPos = SetOnSurface (hit.point, hit);
                    //							Debug.DrawLine (dotMove.transform.position, touchPos, Color.red, 1000f);
                    Statics.isDragging = true;
                    var direction = touchPos - dotMove.transform.position;
                    moveDot (direction, hit);
                }
            } else if (draggingPath != -1) {
                foreach (var manifold in manifolds) {
                    if (manifold.gameObject.GetComponent<MeshCollider> ().Raycast (ray, out hit, 50f)) {
                        actManifold = manifold;
                        lastTouch = touchPos;
                        touchPos = SetOnSurface (hit.point, hit);
                        if (lastTouch != farAwayVec) {
                            DragPath (touchPos, lastTouch);
                        }
                    }
                }
            }
        }
    }

    private void UpdateEnd (Vector3 touchPoint) {
        /**
         * 
         * 					END
         * 
         * 
         * */
        if (drawing) {
            Debug.Log ("Drawing ended");
            drawing = false;
            Statics.drawCircle = false;
            Statics.drawRectangle = false;
        }
        touchDuration = Time.time - touchDuration;
        var touchDistance = Vector2.Distance (touchStart, touchPoint);
        Debug.Log ("Touch Duration = " + touchDuration + ", touch distance = " + touchDistance);
        if (touchDuration < 0.2f && !dontDrawDot) {
            int pathNum = -1;
            bool isOnPath = IsOnPath (touchPoint, ref pathNum);
            if (level.dotSettingAllowed && !isOnPath) {
                DrawDot (touchPoint);
            }
        }
        if (dotMove != null) {
            Debug.Log ("Finish Line");
            dotMove.GetComponent<Rigidbody> ().velocity = Vector3.zero;
            FinishLine ();
        } else if (draggingObstacle != null) {
            Debug.Log ("Finish dragging gameObject");
            draggingObstacle = null;
            Statics.isDragging = false;
        }

        if (draggingPath != -1) {
            Debug.Log ("Finish dragging Path");
            //Guess path which is trying do be reached, and snuggle this path against the other
            EndDraggingPath ();
            Statics.isDragging = false;
        }
        touchDuration = 0f;
        dontDrawDot = false;
    }

    Vector3 SetOnSurface (Vector3 point, RaycastHit hit, bool isDot = false) {
        var spacer = Statics.pathSpacer;
        if (isDot) {
            spacer = Statics.dotSpacer;
        }
        return point + spacer * 0.5f * hit.normal;
    }

    void moveDot (Vector3 direction, RaycastHit hit) {
        var speed = 0.1f;
        if (direction.magnitude > 0.1f) {
            direction *= speed / direction.magnitude;
        }
        float distance;
        var newPosition = dotMove.transform.position + direction;
        var pair = RealSetOnSurface (newPosition, hit.normal, out distance, true);

        if (pair != null && distance < 1f) {
            dotMove.transform.position = pair.left;
            var normal = pair.right;
            dotMove.transform.forward = -normal;
            if (trail.positionCount > lineNormals.Count) {
                lineNormals.Add (normal);
            }
        } else {
            // dotMove.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
    }
    void TryConcatenation (Vector3 position1, Vector3 position2) {
        int pathNumber1 = -1;
        int pathNumber2 = -1;
        bool isOnPath1 = IsOnPath (position1, ref pathNumber1);
        bool isOnPath2 = IsOnPath (position2, ref pathNumber2);
        if (isOnPath1 && isOnPath2 && pathNumber1 != pathNumber2) {
            Debug.Log ("Try to concatenate Path " + pathNumber1 + " and Path " + pathNumber2);
            var path1 = level.paths[pathNumber1];
            var path2 = level.paths[pathNumber2];
            ConcatenatePaths (path1, path2);
        }
    }

    bool isFrozen () {
        return isDrawingPath;
    }

    void ConcatenatePaths (MPath path1, MPath path2) {
        if (path1.dotTo.Equals (path2.dotFrom)) {
            if (actHomotopy != null) {
                DestroyHomotopy ();
            }
            draggingPath = -1;
            Debug.Log ("Concatenate!");
            level.StopParticleSystem (path1);
            path1.Concatenate (path2);
            DeletePath (path2);
            level.StartParticleSystem (path1);
        } else {
            Debug.Log ("Not concatenable");
        }
    }

    void DeletePath (MPath path) {
        level.StopParticleSystem (path);
        level.DeletePath (path);
        level.RecalculateHomClasses ();
    }

    void DragPath (Vector3 touchPosition, Vector3 touchBefore) {
        Debug.Log ("Start DragPath(" + touchPosition.ToString ("F5") + ", " + touchBefore.ToString ("F5") + ")");
        bool dragSnuggledPositions;
        //		Debug.Log ("Homotopy has " + actHomotopy.closeNodePositions.Count + " snuggled Positions");
        if (actHomotopy.snugglingNodePositions.Contains (draggingPosition)) {
            dragSnuggledPositions = true;
        } else {
            dragSnuggledPositions = false;
        }

        var path = actHomotopy.midPath;
        Debug.Log ("with line.normals: " + path.GetNormals ().Count + " vs. " + path.Count);
        float t0 = (float) draggingPosition / path.Count;
        float distance;
        if (Statics.isSphere) {
            var dist = Vector3.Distance (touchPosition, touchBefore);
            var angle = Mathf.Asin (dist / Statics.sphereRadius);
            distance = angle * Statics.sphereRadius;
        } else if (Statics.isTorus) {
            distance = Vector3.Distance (touchPosition, touchBefore);
        } else {
            distance = Vector3.Distance (touchPosition, touchBefore);
        }
        Debug.Log ("Distance is " + distance);
        var lowerLimit = Statics.lineThickness / 2f;
        var maxUpperLimit = Mathf.Min (t0, 1f - t0);
        var t = Mathf.Clamp01 (distSoFar / 3f);
        float firstLowerSnugglePosition = maxUpperLimit;
        float firstUpperSnugglePosition = maxUpperLimit;

        if (!dragSnuggledPositions) {
            for (int i = draggingPosition; i < path.Count; i++) {
                if (i < upperBound) {
                    if (actHomotopy.snugglingNodePositions.Contains (i)) {
                        firstUpperSnugglePosition = (float) i / path.Count - t0;
                        break;
                    }
                } else {
                    break;
                }
            }
            for (int i = draggingPosition - 1; i > -1; i--) {
                if (i > lowerBound) {
                    if (actHomotopy.snugglingNodePositions.Contains (i)) {
                        firstLowerSnugglePosition = t0 - (float) i / path.Count;
                        break;
                    }
                } else {
                    break;
                }
            }
        }
        float lowerB = Mathf.SmoothStep (lowerLimit, Mathf.Min (maxUpperLimit, firstLowerSnugglePosition), t);
        float upperB = Mathf.SmoothStep (lowerLimit, Mathf.Min (maxUpperLimit, firstUpperSnugglePosition), t);
        Vector3 normal = (touchPosition - touchBefore);
        distSoFar += distance;

        for (int i = draggingPosition; i < path.Count - 1; i++) {
            if (i < upperBound) {
                if (!setPoint (i, normal, lowerB, upperB, t0, true)) {
                    break;
                }
            } else {
                break;
            }
        }
        for (int i = draggingPosition - 1; i > 0; i--) {
            if (i > lowerBound) {
                if (!setPoint (i, normal, lowerB, upperB, t0, false)) {
                    break;
                }
            } else {
                break;
            }
        }
        path.SetMesh ();
        Debug.Log ("End DragPath(" + touchPosition.ToString ("F5") + ", " + touchBefore.ToString ("F5") + ")");
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

    bool setPoint (int i, Vector3 normal, float lowerB, float upperB, float t0, bool up) {
        var midPath = actHomotopy.midPath;
        var draggingPoint = midPath.GetPosition (draggingPosition);
        Vector3 point = midPath.GetPosition (i);
        var direction = point - draggingPoint;
        var factor = GetFactor (i, t0, lowerB, upperB);
        var goalPoint = point + factor * normal;

        float distance;
        var pair = RealSetOnSurface (goalPoint, midPath.GetNormal (i), out distance);
        if (pair != null) {
            goalPoint = pair.left;
            midPath.setNormal (i, pair.right);
        }

        if (i > 0 && i < midPath.Count - 1) {
            int nextNode;
            if (up) {
                nextNode = i - 1;
            } else {
                nextNode = i + 1;
            }
            var nextPoint = midPath.GetPosition (nextNode);
            var distToNext = Vector3.Distance (goalPoint, nextPoint);
            if (distToNext < Statics.meanDist * 1.01f) {
                Debug.Log ("Return false at " + i + " as the distance to next " + nextNode + " is too small");
                return false;
            }
        }
        Collider collider;
        var hasHit = Misc.HasHit (goalPoint, out collider);
        if (hasHit) {
            var staticNormal = Misc.GetNormal (collider, goalPoint);
            float staticAngle = Vector3.Angle (staticNormal, direction);
            if (staticAngle < 90) {
                //pull
                if (up) {
                    upperBound = i;
                } else {
                    lowerBound = i;
                }
            }
            Debug.Log ("Return false at " + i + " as a collider has been hit");
            return false;
        }
        //		Debug.Log ("Move position " + i + " from " + line.GetPosition (i).ToString ("F5") + " to " + goalPoint.ToString ("F5"));
        midPath.SetPosition (i, goalPoint);
        return true;
    }

    Pair<Vector3> RealSetOnSurface (Vector3 point, Vector3 normal, out float distance, bool isDot = false) {
        normal.Normalize ();
        Ray ray = new Ray (point, -normal);
        RaycastHit hit;
        //		Misc.DebugSphere (goalPoint, Color.green, "Goalpoint" + i + " upper: " + upperBound + " lower " + lowerBound);
        var distDown = 0.5f;
        // var distUp = 2f;
        if (actManifold.gameObject.GetComponent<MeshCollider> ().Raycast (ray, out hit, distDown)) {
            var newPoint = SetOnSurface (hit.point, hit, isDot);
            distance = Vector3.Distance (hit.point, point);
            return new Pair<Vector3> (newPoint, hit.normal);
        } else {
            // Ray ray2 = new Ray (point + normal.normalized, -normal);
            // if (actManifold.gameObject.GetComponent<MeshCollider> ().Raycast (ray2, out hit, distUp)) {
            //     var newPoint = SetOnSurface (hit.point, hit, isDot);
            //     distance = Vector3.Distance (hit.point, point);
            //     return new Pair<Vector3> (newPoint, hit.normal);
            // }
        }
        distance = float.PositiveInfinity;
        return null;
    }

    float GetFactor (int i, float t0, float lowerB, float upperB) {
        var count = actHomotopy.midPath.Count;
        float t = (float) i / count;
        if (i == 0 || i == count - 1) {
            return 0;
        }
        if (t < t0) {
            return Mathf.SmoothStep (0f, 1f, (t - (t0 - lowerB)) / lowerB);
        } else {
            return Mathf.SmoothStep (0f, 1f, -(t - (t0 + upperB)) / upperB);
        }
    }

    void EndDraggingPath () {
        Debug.Log ("End Dragging Path");
        MPath path = actHomotopy.midPath;
        Debug.Log ("Refine");
        path.RefinePath ();
        Debug.Log ("Put On Surface");
        PutPathOnSurface (path);
        Debug.Log ("Smoothline");
        path.smoothLine ();
        Debug.Log ("Start check if snuggling");
        StartCoroutine (CheckIfPathsAreSnuggling ());
        Debug.Log ("End check if snuggling, start snuggletopath");
    }

    void PutPathOnSurface (MPath path) {
        for (int i = 0; i < path.Count; i++) {
            var point = path.GetPosition (i);
            var normal = path.GetNormal (i);

            float distance;
            var pair = RealSetOnSurface (point, normal, out distance);
            if (pair != null) {
                point = pair.left;
                path.setNormal (i, pair.right);
            }

        }
    }

    IEnumerator CheckIfPathsAreSnuggling () {
        var midPath = actHomotopy.midPath;
        Debug.Log ("Check if snuggling");
        var closestPositions = actHomotopy.closestPositions;
        closestPositions.Clear ();
        for (int i = draggingPosition; i < midPath.Count; i++) {
            var pos = midPath.GetPosition (i);
            float closest = 9999f;
            foreach (var otherPath in level.paths) {
                if (!actHomotopy.path1.Equals (otherPath)) {
                    for (int k = 0; k < otherPath.Count; k++) {
                        Vector3 pos2 = otherPath.GetPosition (k);
                        var dist = Vector2.Distance (pos, pos2);
                        if (dist < closest) {
                            closestPositions[i] = pos2;
                            closest = dist;
                        }
                    }
                }
            }
            if (closest < 9999f && closest < minDist) { } else {
                closestPositions.Remove (i);
                break;
            }
        }

        for (int i = draggingPosition - 1; i >= 0; i--) {
            var pos = midPath.GetPosition (i);
            float closest = 9999f;
            foreach (var otherPath in level.paths) {
                if (!actHomotopy.path1.Equals (otherPath)) {
                    for (int k = 0; k < otherPath.Count; k++) {
                        Vector3 pos2 = otherPath.GetPosition (k);
                        var dist = Vector2.Distance (pos, pos2);
                        if (dist < closest) {
                            closestPositions[i] = pos2;
                            closest = dist;
                        }
                    }
                }
            }
            if (closest < 9999f && closest < minDist) { } else {
                closestPositions.Remove (i);
                break;
            }
        }
        Debug.Log ("End check if snuggling, start snuggletopath");
        StartCoroutine (SnuggleToPath ());
        yield break;
    }

    IEnumerator SnuggleToPath () {
        Debug.Log ("Snuggle To Path");
        Dictionary<int, Vector3> goalPoints = new Dictionary<int, Vector3> ();
        Dictionary<int, Vector3> startPoints = new Dictionary<int, Vector3> ();
        var closestPositions = actHomotopy.closestPositions;
        var path = actHomotopy.midPath;
        foreach (var pair in closestPositions) {
            var position = pair.Key;
            var closestPosition = pair.Value;
            float range = 30f;
            float counter = 1f;
            for (int i = 1; i < range; i++) {
                var index = position - i;
                if (index > -1) {
                    var backwardsPosition = path.GetPosition (index);
                    if (closestPositions.ContainsKey (index)) {
                        break;
                    }
                    var factor = Mathf.SmoothStep (1, 0, Mathf.Clamp01 (counter / range));
                    var direction = (closestPosition - backwardsPosition);
                    var goalPoint = backwardsPosition + factor * direction;
                    goalPoints[index] = goalPoint;
                    counter++;
                }
            }
            counter = 1f;
            for (int i = 1; i < range; i++) {
                var index = position + i;
                if (index < path.Count) {
                    var backwardsPosition = path.GetPosition (index);
                    if (closestPositions.ContainsKey (index)) {
                        break;
                    }
                    var factor = Mathf.SmoothStep (1, 0, Mathf.Clamp01 (counter / range));
                    var direction = (closestPosition - backwardsPosition);
                    var goalPoint = backwardsPosition + factor * direction;
                    goalPoints[index] = goalPoint;
                    counter++;
                }
            }
            goalPoints.Add (position, closestPosition);
        }
        foreach (var entry in goalPoints) {
            startPoints.Add (entry.Key, path.GetPosition (entry.Key));
        }
        float duration = 1;
        bool localMorph = true;
        var time = 0f;
        while (localMorph) {
            localMorph = false;
            time += Time.deltaTime;
            var t = Mathf.Clamp01 (time / duration);

            foreach (var entry in goalPoints) {
                var index = entry.Key;
                var goalPoint = entry.Value;
                var startPoint = startPoints[index];
                var goalPointStep = Vector3.Lerp (startPoint, goalPoint, t);

                var distance = Vector2.Distance (goalPointStep, goalPoint);

                path.SetPosition (index, goalPointStep);
                if (t < 0.99f) {
                    localMorph = true;
                }
            }
            //			if (Statics.mesh) {
            //				actHomotopy.setMesh (level.GetStaticPositions ());
            //			} else {
            //				actHomotopy.AddCurveToBundle (level.pathFactory, draggingPosition);
            //			}
            yield return null;
        }
        Debug.Log ("Done Snuggle To Path, start refinePath");
        actHomotopy.snugglingNodePositions = path.RefinePath (actHomotopy.closestPositions);
        //		if (Statics.mesh) {
        //			actHomotopy.setMesh (level.GetStaticPositions ());
        //		} else {
        //			actHomotopy.AddCurveToBundle (level.pathFactory, draggingPosition);
        //		}
        upperBound = actHomotopy.midPath.Count;
        lowerBound = -1;
        distSoFar = 0f;
        draggingPath = -1;
        draggingPosition = -1;
        CheckIfHomotopyDone ();
        yield break;
    }

    void CheckIfHomotopyDone () {
        Debug.Log ("Start CheckIfHomotopyDone");
        var midPath = actHomotopy.midPath;
        var dotFromPos = midPath.dotFrom.transform.position;
        var dotToPos = midPath.dotTo.transform.position;
        var circleSize = level.dotPrefab.transform.localScale.x;
        var pathHomClasses = level.pathHomClasses;
        List<int> homClass = null;
        var indexOfPath1 = level.paths.IndexOf (actHomotopy.path1);
        foreach (var item in pathHomClasses) {
            if (item.Contains (indexOfPath1)) {
                homClass = item;
            }
        }
        Debug.Log ("Homclass of this homotopy contains " + string.Join (",", homClass.Select (x => x.ToString ()).ToArray ()));
        foreach (var otherPathNum in homClass) {
            if (otherPathNum != indexOfPath1) {
                var otherPath = level.paths[otherPathNum];
                Debug.Log ("Check Path " + otherPath.pathNumber);
                bool homotopic = true;
                Debug.Log ("Check if homotopic");
                for (int i = 0; i < midPath.Count; i++) {
                    var midPathPos = midPath.GetPosition (i);
                    if (Vector2.Distance (midPathPos, dotFromPos) > circleSize && Vector2.Distance (midPathPos, dotToPos) > circleSize) {
                        bool existsNearNode = false;
                        for (int j = 0; j < otherPath.Count; j++) {
                            var otherPathPos = otherPath.GetPosition (j);
                            var dist = Vector2.Distance (midPathPos, otherPathPos);
                            if (dist < Statics.homotopyNearness) {
                                existsNearNode = true;
                                break;
                            }
                        }
                        if (!existsNearNode) {
                            homotopic = false;
                            break;
                        }
                    }
                }
                if (homotopic) {
                    for (int i = 0; i < otherPath.Count; i++) {
                        var otherPathPos = otherPath.GetPosition (i);
                        if (Vector2.Distance (otherPathPos, dotFromPos) > circleSize && Vector2.Distance (otherPathPos, dotToPos) > circleSize) {
                            bool existsNearNode = false;
                            for (int j = 0; j < midPath.Count; j++) {
                                var midPathPos = midPath.GetPosition (j);
                                var dist = Vector2.Distance (otherPathPos, midPathPos);
                                if (dist < Statics.homotopyNearness) {
                                    existsNearNode = true;
                                    break;
                                }
                            }
                            if (!existsNearNode) {
                                homotopic = false;
                                break;
                            }
                        }
                    }

                }
                Debug.Log ("Check if homotopic, done : " + homotopic.ToString ());
                if (homotopic) {
                    if (Statics.mesh) {
                        actHomotopy.setMesh (level.GetStaticPositions ());
                    } else {
                        actHomotopy.AddCurveToBundle (level.pathFactory, draggingPosition);
                    }
                    actHomotopy.SetColor (level.GetNextColor (MType.Path));
                    otherPath.SetColor (midPath.GetColor ());
                    Destroy (actHomotopy.midPath.gameObject);
                    actHomotopy = null;
                    break;
                }
            }
        }
        actHomotopy.midPath.SetMesh ();
    }

    void DrawLine3D (RaycastHit hit) {
        dot1 = level.dots.IndexOf (hit.collider.gameObject);
        if (dot1 != -1) {
            isDrawingPath = true;
            Debug.Log ("Instantiate");
            dotMove = Instantiate (level.dots[dot1]);
            dotMove.GetComponent<BoxCollider> ().isTrigger = false;
            dotMove.GetComponent<Rigidbody> ().collisionDetectionMode = CollisionDetectionMode.Continuous;
            dotMove.transform.position = SetOnSurface (hit.point, hit, true);
            dotMove.transform.parent = actManifold.gameObject.transform.parent;
            trail = setTrail ();
            lineNormals = new List<Vector3> ();
            Camera.main.GetComponent<RotateCamera> ().SetPlayer (dotMove);
        } else {
            Debug.Log ("Set DotMove to null");
            dotMove = null;
        }
    }

    TrailRenderer setTrail () {
        var trail = dotMove.GetComponentInChildren<TrailRenderer> ();
        trail.gameObject.transform.position += dotMove.transform.forward.normalized * 0.5f * (Statics.dotSpacer - Statics.pathSpacer);
        trail.textureMode = LineTextureMode.Tile;
        trail.material = level.trailMat;
        trail.startWidth = Statics.lineThickness;
        trail.endWidth = Statics.lineThickness;
        trail.enabled = true;
        trail.Clear ();
        return trail;
    }

    void DestroyHomotopy () {
        if (actHomotopy != null) {
            level.StopParticleSystem (actHomotopy.midPath);
            actHomotopy.Clear ();
            actHomotopy = null;
        }
    }

    void setPathToDrag (Vector3 touchPoint, int pathNumber) {
        int numOnPath = -1;
        Vector3 vector = new Vector3 ();
        bool isOnPath = GetPositionOnPath (pathNumber, touchPoint, ref numOnPath, ref vector);
        MPath path;
        if (pathNumber == -2) {
            path = actHomotopy.midPath;
        } else {
            path = level.paths[pathNumber];
        }
        if (isOnPath && numOnPath != 0 && numOnPath != path.Count - 1) {
            Debug.Log ("Start Dragging Line " + pathNumber + " at position " + numOnPath);
            if (pathNumber != -2) {
                var path1 = level.paths[pathNumber];
                actManifold = path1.parentManifold;
                if (actHomotopy != null) {
                    Destroy (GameObject.Find ("MidPath"));
                    actHomotopy.Clear ();
                    actHomotopy = null;
                }
                var actMidPath = level.NewPath (path1.GetColor (), path1.dotFrom, path1.dotTo);
                actMidPath.SetName ("MidPath");
                level.StopParticleSystem (path1);
                level.particleSystems.Add (ps.SetParticleSystem (actMidPath));
                actMidPath.AttachTo (actManifold);
                actHomotopy = level.NewHomotopy (path1, actMidPath);
            }
            upperBound = actHomotopy.midPath.Count;
            lowerBound = -1;
            draggingPath = -2;
            draggingPosition = numOnPath;
        } else {
            Debug.Log ("Not on Line");
        }
    }

    void DrawDot (Vector3 touchPoint) {
        var ray = GetRay ();
        RaycastHit hit;
        foreach (var manifold in manifolds) {
            if (manifold.gameObject.GetComponent<MeshCollider> ().Raycast (ray, out hit, 100f)) {
                Debug.Log ("Touch not on some Path, so set Dot");
                GameObject dot;
                dot = Instantiate (level.dotPrefab3D);
                dot.transform.position = SetOnSurface (hit.point, hit, true);
                dot.transform.forward = -hit.normal;
                dot.GetComponent<SpriteRenderer> ().material.SetColor ("_Color", level.GetNextColor (MType.Dot));
                dot.transform.parent = manifold.gameObject.transform;
                level.addDot (dot);
            }
        }
    }

    static Ray GetRay () {

        Ray ray = Camera.main.ScreenPointToRay (Input.GetTouch (0).position);
        return ray;
    }

    void FinishLine () {
        if (dotMove.GetComponent<dotBehaviour3D> ().IsTriggered ()) {
            Debug.Log ("Finish line, normals: " + lineNormals.Count + " vs. pos: " + trail.positionCount);
            while (lineNormals.Count < trail.positionCount) {
                lineNormals.Add (-dotMove.transform.forward);
            }
            // for (int i = 0; i < lineNormals.Count; i++)
            // {
            //     Debug.DrawRay(trail.GetPosition(i), lineNormals[i], Color.red, 1000f);
            // }
            var dotBehaviourDotMove = dotMove.GetComponent<dotBehaviour3D> ();
            var bumpedDot = dotBehaviourDotMove.GetTriggerObject ();
            dot2 = level.dots.IndexOf (bumpedDot);
            var path = level.NewPath (level.dots[dot1], level.dots[dot2]);
            for (int i = 0; i < trail.positionCount; i++) {
                path.SetPosition (i, trail.GetPosition (i));
                path.SetNormal (i, lineNormals[i]);
            }
            var position = level.dots[dot1].transform.position;
            float distance;
            var pair = RealSetOnSurface (position, -level.dots[dot1].transform.forward, out distance);
            path.InsertPositionAt (0, pair.left);
            path.InsertNormalAt (0, pair.right);
            var count = path.Count;
            var position2 = bumpedDot.transform.position;
            var pair2 = RealSetOnSurface (position2, -bumpedDot.transform.forward, out distance);
            path.SetPosition (count, pair2.left);
            path.SetNormal (count, pair2.right);
            path.SetMesh ();
            path.AttachTo (actManifold);
            //Set Colors
            dotMove.transform.position = bumpedDot.transform.position;
            dotMove.GetComponent<SpriteRenderer> ().enabled = false;
            StartCoroutine (DrawPath (path, false));
        } else {
            //			Destroy (dotMove);
        }
        Statics.isDragging = false;
    }

    void SetDotColors (int dot1, int dot2) {
        var group1 = new List<int> ();
        var group2 = new List<int> ();
        for (int i = 0; i < level.dotHomClasses.Count; i++) {
            var group = level.dotHomClasses[i];
            if (group.Contains (dot1)) {
                group1 = group;
            } else if (group.Contains (dot2)) {
                group2 = group;
            }
        }
        for (int i = 0; i < group2.Count; i++) {
            var item = group2[i];
            var color = level.dots[dot1].GetComponent<SpriteRenderer> ().material.GetColor ("_Color");
            level.dots[item].GetComponent<SpriteRenderer> ().material.SetColor ("_Color", color);
            group1.Add (item);
        }
    }

    void ShortenPath (MPath path) {
        Debug.Log ("Shorten from start");
        var startPosition = path.dotFrom.transform.position;
        var node = 0;
        var counter = 0;
        while (node < path.Count) {
            if (Vector2.Distance (path.GetPosition (node), startPosition) >= dotRadius) {
                break;
            }
            counter++;
            node++;
        }
        for (int i = 0; i < node; i++) {
            path.RemovePosition (i);
            path.RemoveNormal (i);
        }
        Debug.Log ("Shorten " + counter + ", now end");
        counter = 0;
        var endPosition = path.dotTo.transform.position;
        node = path.Count - 1;
        while (node >= 0) {
            if (Vector2.Distance (path.GetPosition (node), endPosition) >= dotRadius) {
                break;
            }
            counter++;
            node--;
        }
        for (int i = 1; i < node; i++) {
            path.RemovePosition (path.Count - i);
            path.RemoveNormal (path.Count - i);
        }
        Debug.Log ("Shorten " + counter);
    }

    // end of update
    //Returns number of path, index on path
    bool GetPositionOnPath (int pathNumber, Vector3 touchPoint, ref int numOnPath, ref Vector3 posOnPath) {
        numOnPath = -1;
        float distToPath = Statics.lineThickness * 2f;
        float minDist = distToPath;
        if (pathNumber == -2) {
            var path = actHomotopy.midPath;
            for (int j = 0; j < path.Count; j++) {
                var value = path.GetPosition (j);
                var dist = Vector2.Distance (touchPoint, value);
                if ((dist < distToPath && numOnPath == -1) || (dist < minDist && numOnPath != -1)) {
                    numOnPath = j;
                    minDist = dist;
                    posOnPath = value;
                }
            }
            if (numOnPath != -1) {
                return true;
            } else {
                return false;
            }
        } else {
            var path = level.paths[pathNumber];
            for (int j = 0; j < path.Count; j++) {
                var value = path.GetPosition (j);
                var dist = Vector2.Distance (touchPoint, value);
                if ((dist < distToPath && numOnPath == -1) || (dist < minDist && numOnPath != -1)) {
                    numOnPath = j;
                    minDist = dist;
                    posOnPath = value;
                }
            }
            if (numOnPath != -1) {
                return true;
            } else {
                return false;
            }
        }
    }

    bool IsOnPath (Vector3 touchPoint, ref int pathNumber, bool checkMidPathFirst = false, bool debug = false) {
        pathNumber = -1;
        if (checkMidPathFirst) {
            Debug.Log ("Check Midpath");
            var path = actHomotopy.midPath;
            var node = 0;
            while (node < path.Count) {
                var dist = Vector3.Distance (touchPoint, path.GetPosition (node));
                if (dist < minDist) {
                    pathNumber = -2;
                    return true;
                }
                node++;
            }
        }
        Debug.Log ("Check other Paths");
        for (int i = 0; i < level.paths.Count; i++) {
            var path = level.paths[i];
            Debug.Log ("Check path " + i);
            var node = 0;
            while (node < path.Count) {
                var nextNode = node + 1 < path.Count ? node + 1 : 0;
                var dist = LinearAlgebra.distanceToSegment (touchPoint, path.GetPosition (node), path.GetPosition (nextNode));
                if (Statics.gluePaths && debug) {
                    Misc.DebugSphere (touchPoint, Color.blue, "Touchpoint " + node + ": " + dist + " " + minDist + " " + Statics.path1Set);
                    Misc.DebugSphere (path.GetPosition (node), Color.blue, "Path " + i + " at " + node);
                    Misc.DebugSphere (path.GetPosition (nextNode), Color.blue, "Path " + i + " at " + nextNode);
                }
                // var dist = Vector3.Distance (touchPoint, path.GetPosition (node));
                // Debug.Log (dist);
                if (dist < minDist) {
                    pathNumber = i;
                    return true;
                }
                node++;
            }
        }
        Debug.Log ("No path near is On path");
        return false;
    }

    bool hitPath (Vector3 a, Vector3 b, ref int pathNumber, bool debug = false) {
        for (int i = 0; i < level.paths.Count; i++) {
            var path = level.paths[i];
            var node = 0;
            while (node < path.Count) {
                var nextNode = node + 1 < path.Count ? node + 1 : 0;
                float dist;
                var touchPoint = LinearAlgebra.getClosest (a, b, path.GetPosition (node), path.GetPosition (nextNode), out dist);
                if (Statics.gluePaths && debug) {
                    Misc.DebugSphere (touchPoint, Color.blue, "Touchpoint " + node + ": " + dist + " " + minDist + " " + Statics.path1Set);
                    Misc.DebugSphere (path.GetPosition (node), Color.blue, "Path " + i + " at " + node);
                    Misc.DebugSphere (path.GetPosition (nextNode), Color.blue, "Path " + i + " at " + nextNode);
                }
                // var dist = Vector3.Distance (touchPoint, path.GetPosition (node));
                if (dist < minDist) {
                    pathNumber = i;
                    return true;
                }
                node++;
            }
        }
        Debug.Log ("No path near");
        return false;
    }

    bool IsOnMidPath (Vector3 worldPoint, ref int numOnPath, ref Vector3 vector) {
        numOnPath = -1;
        float distToPath = Statics.lineThickness * 3f;
        float minDist = distToPath;
        var path = actHomotopy.midPath;
        var node = 0;
        int counter = 0;
        while (node < path.Count) {
            var value = path.GetPosition (node);
            var dist = Vector2.Distance (worldPoint, value);
            if ((dist < distToPath && numOnPath == -1) || (dist < minDist && numOnPath != -1)) {
                numOnPath = counter;
                minDist = dist;
                vector = value;
            }
            counter++;
            node++;
        }
        if (numOnPath != -1) {
            return true;
        } else {
            return false;
        }
    }

    IEnumerator DrawPath (MPath path, bool onLoad) {
        Debug.Log ("DrawPath " + path);
        isCollided[path] = false;
        path.gameObject.SetActive (false);
        // path.smoothLine ();
        // path.RefinePath ();
        //		ShortenPath (path);
        //		path.DrawArrows ();
        // trail.Clear();
        // dotMove.SetActive(false);

        int sum = 0;
        var duration = 250f;
        if (onLoad) {
            duration = 10f;
        }
        float chunkSize = (float) path.Count / duration;
        var savedPositions = path.GetPositions ();
        path.ClearPositions ();
        var node = 0;
        path.gameObject.SetActive (true);
        for (int i = 0; i < savedPositions.Count; i++) {
            path.SetPosition (i, savedPositions[node]);
            node++;
            sum++;
            if (sum > path.Count / duration) {
                sum = 0;
                path.SetMesh ();
                yield return null;
            }
        }
        path.SetMesh ();

        SetDotColors (dot1, dot2);
        Destroy (dotMove);
        level.addPath (path);
        isDrawingPath = false;
        //		var pathObject = line.gameObject;
        //		var partsystem = pathObject.AddComponent<ParticleSystem> ();
        //		var renderer = partsystem.GetComponent<ParticleSystemRenderer> ();
        //		renderer.maxParticleSize = 0.01f;
        //		renderer.minParticleSize = 0.01f;
        //		renderer.material = partMaterial;
        //		var main = partsystem.main;
        //		main.maxParticles = 1;
        //		main.startSpeed = 0;
        //		main.startLifetime = 1000;
        //		main.playOnAwake = true;
        //		var emission = partsystem.emission;
        //		emission.rateOverTime = 0.5f;
        //		StartCoroutine (AddParticleSystem (path));

        level.particleSystems.Add (ps.SetParticleSystem (path));
        // StartCoroutine (CutPath (level.paths.Count - 1));
        yield break;
    }

    int GetNearestPointTo (Vector3 point, MPath path) {
        float smallestDistance = 100000f;
        int index = 0;
        for (int i = 0; i < path.Count; i++) {
            var dist = Vector2.Distance (point, path.GetPosition (i));
            if (dist < smallestDistance) {
                index = i;
                smallestDistance = dist;
            }
        }
        return index;
    }

}