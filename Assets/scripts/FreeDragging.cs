using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using UnityEditor;
using System;
using System.Linq;

//using UnityEditor;
//using UnityEngine.UI;
using UnityEngine.UI;

public class FreeDragging : MonoBehaviour
{
    // Class Variables

    private bool is2D = true;
    private TargetJoint3D targetJoint = null;
    public float pathResolution;
    private int dot1;
    private GameObject dotMove;
    Vector3 dotMoveDepth = Statics.dotDepth * 1.05f;
    private int dot2;

    private Dictionary<MPath, bool> isCollided = new Dictionary<MPath, bool>();
    private HashSet<int> angleGreaterThan90 = new HashSet<int>();
    //private GameObject node;
    private TrailRenderer trail;

    private float minDist = Statics.lineThickness;
    public Material partMaterial;

    private LevelData level;

    private float touchDuration = 0f;
    private Vector3 touchStart;

    public Homotopy actHomotopy = null;

    private int draggingPath = -1;
    private int draggingPosition = -1;
    private Vector3 lastTouch = Vector3.zero;
    private float distSoFar = 0f;
    private int upperBound;
    private int lowerBound;

    private Vector3 cameraPosition;

    //Freezers
    bool isEndingPath = false;
    bool isDrawingPath = false;

    //3D----------
    public GameObject sphere;

    Vector3 normal;

    MPath pathSelected = null;

    DirectionParticleSystem ps;

    void Start()
    {
        level = Camera.main.GetComponent<LevelData>();
        ps = gameObject.AddComponent<DirectionParticleSystem>();
        ps.partMaterial = partMaterial;
        ps.trailMaterial = new Material(level.pathMat);
        cameraPosition = Camera.main.transform.position;
        Debug.Log("Draw Paths");
        for (int i = 0; i < level.paths.Count; i++)
        {
            Debug.Log("Draw Path " + i);
            var path = level.paths[i];
            StartCoroutine(DrawPath(path, true));
        }
        Statics.isLoading = false;
    }
    TouchHandler th = new TouchHandler();
    // Update
    void Update()
    {
        if (!Statics.deleteObstacle && !isFrozen())
        {
            if (th.hasTouched(1))
            { // && !EventSystem.current.IsPointerOverGameObject (Input.GetTouch (0).fingerId)
                Touch touch1 = th.GetTouch();
                Vector3 touchPoint = Camera.main.ScreenToWorldPoint(touch1.position);
                touchPoint.z = 0;
                if (touch1.phase == TouchPhase.Began && !isFrozen())
                {
                    UpdateStart(touchPoint);
                }
                else if (touch1.phase == TouchPhase.Moved)
                {
                    UpdateMove(touch1.position);
                }
                else if (touch1.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Canceled)
                {
                    UpdateEnd(touchPoint);
                }
                lastTouch = touchPoint;
            }
            else if (th.hasTouched(2))
            {
                UpdateConcatenation();
            }
        }
    }

    private void UpdateConcatenation()
    {
        Touch touch1 = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);
        if (touch2.phase == TouchPhase.Began)
        {
            var touchPoint1 = Camera.main.ScreenToWorldPoint(touch1.position);
            var touchPoint2 = Camera.main.ScreenToWorldPoint(touch2.position);
            TryConcatenation(touchPoint1, touchPoint2);
        }
    }

    void UpdateStart(Vector3 touchPoint)
    {
        touchStart = touchPoint;
        touchDuration = Time.time;
        if (is2D)
        {
            RaycastHit hit;
            Ray ray = new Ray(cameraPosition, touchPoint - cameraPosition);
            bool hasHit = Physics.Raycast(ray, out hit);
            if (hasHit)
            {
                if (Statics.showWindingNumber && pathSelected != null)
                {
                    if (hasHit && hit.collider.CompareTag("Obstacle"))
                    {
                        if (drawingWindingNumberRoutine != null)
                        {
                            StopCoroutine(drawingWindingNumberRoutine);
                        }
                        drawingWindingNumberRoutine = showWindingNumber(pathSelected, hit.collider.gameObject);
                        StartCoroutine(drawingWindingNumberRoutine);
                        Statics.showWindingNumber = false;
                        pathSelected = null;
                        level.HideHintCanvas();
                        Misc.flipButtonSprites("Windung");
                    }
                }
                else if (hit.collider.isTrigger && level.pathDrawingAllowed && !isDrawingPath)
                {
                    DrawLine(hit);
                }
            }
            else
            {
                int pathNumber = -1;
                bool isOnPath = IsOnPath(touchPoint, ref pathNumber, actHomotopy != null);
                Debug.Log("Draw Winding Number?");
                if (isOnPath)
                {
                    if (Statics.showWindingNumber && pathSelected == null)
                    {
                        Debug.Log("Draw Winding Number ?!");
                        if (pathNumber == -2)
                        {
                            pathSelected = actHomotopy.midPath;
                        }
                        else
                        {
                            pathSelected = level.paths[pathNumber];
                        }
                        LevelData.showHint("Touch an Obstacle");
                    }
                    else if (Statics.retractPath)
                    {
                        Statics.retractPath = false;
                        Debug.Log("Collapse Path");
                        if (pathNumber != -2)
                        {
                            Destroy(GameObject.Find("MidPath"));
                            DestroyHomotopy();
                            var path = level.paths[pathNumber];
                            var actMidPath = level.NewPath(path.GetColor(), path.dotFrom, path.dotTo);
                            actMidPath.SetName("MidPath");
                            level.StopParticleSystem(path);
                            level.particleSystems.Add(ps.SetParticleSystem(actMidPath));
                            actHomotopy = level.NewHomotopy(path, actMidPath);
                            pathSelected = actMidPath;
                        }
                        Retract(actHomotopy.midPath);
                        level.HideHintCanvas();
                        Misc.flipButtonSprites("Collapse");
                    }
                    else if (Statics.checkPath)
                    {
                        Statics.checkPath = false;
                        Debug.Log("Check Paths");
                        if (actHomotopy != null && pathNumber != -2)
                        {
                            Connect(actHomotopy.midPath, level.paths[pathNumber], level.paths.IndexOf(actHomotopy.path1), pathNumber);
                        }
                        level.HideHintCanvas();
                        Misc.flipButtonSprites("Check");
                    }
                    else if (level.homotopiesAllowed)
                    {
                        Debug.Log("Started Dragging Path");
                        setPathToDrag(touchPoint, pathNumber);
                    }
                }
                else
                {
                    Debug.Log("Tried Dragging, but no path there or homotopies not allowed");
                }
            }
        }
    }

    void UpdateMove(Vector3 touchPoint)
    {
        Vector3 touchPos = Camera.main.ScreenToWorldPoint(new Vector3(touchPoint.x, touchPoint.y, 0));
        touchPos.z = 0;
        var target = touchPos + dotMoveDepth;
        if (dotMove != null)
        {
            targetJoint.target = target;
        }
        else if (draggingPath != -1)
        {
            var touchDirection = lastTouch - touchPos;
            if (touchDirection.magnitude > Statics.maxMoveDistance)
            {
                touchDirection = touchDirection * (Statics.maxMoveDistance / touchDirection.magnitude);
            }
            DragPath(touchPos, lastTouch);
        }
    }

    void UpdateEnd(Vector3 touchPoint)
    {
        touchDuration = Time.time - touchDuration;
        var touchDistance = Vector2.Distance(touchStart, touchPoint);
        Debug.Log("Touch Duration = " + touchDuration + ", touch distance = " + touchDistance);
        if (dotMove != null)
        {
            Debug.Log("Finish Line");
            FinishLine();
        }
        if (draggingPath != -1)
        {
            Debug.Log("Finish dragging Path");
            //Guess path which is trying do be reached, and snuggle this path against the other
            EndDraggingPath();
        }
        else if (!Statics.showWindingNumber && !Statics.retractPath && touchDuration < 0.2f)
        {
            shortTouch(touchPoint);
        }
        touchDuration = 0f;
    }

    void FixedUpdate()
    {
        if (dotMove != null)
        {
            var velocity = dotMove.GetComponent<Rigidbody>().velocity;
            const float maxVelocity = 10f;
            if (velocity.magnitude > maxVelocity)
            {
                dotMove.GetComponent<Rigidbody>().velocity = velocity.normalized * maxVelocity;
            }
        }
    }

    void shortTouch(Vector3 touchPoint)
    {
        List<int> pathNums = new List<int>();
        bool isOnPath = IsOnPaths(touchPoint, ref pathNums);
        if (isOnPath)
        {
            Debug.Log("Cycle through paths ");
            foreach (var item in pathNums)
            {
                Debug.Log(item);
            }
            int order = level.paths[pathNums[0]].sortingOrder;
            for (int i = 0; i < pathNums.Count - 1; i++)
            {
                level.paths[pathNums[i]].sortingOrder = level.paths[pathNums[i + 1]].sortingOrder;
            }
            level.paths[pathNums[pathNums.Count - 1]].sortingOrder = order;
        }
        else if (level.dotSettingAllowed)
        {
            DrawDot(touchPoint);
        }
    }

    GameObject segmentObject = null;

    IEnumerator showWindingNumber(MPath path, GameObject obstacle)
    {
        Debug.Log("Draw Winding Number " + path.pathNumber);
        var a = 0.05f;
        if (segmentObject != null)
        {
            Destroy(segmentObject);
        }
        var middle = obstacle.transform.position;
        Debug.Log("For Path " + path.pathNumber);

        var spiral = new MLine(new GameObject("WindingSpiral " + path.pathNumber));
        spiral.SetMaterial(level.pathMat);
        spiral.sortingOrder = 99;
        spiral.width = Statics.lineThickness / 2f;

        var windingAngles = new List<float>();

        segmentObject = new GameObject("Segment");
        var segment = new MLine(segmentObject);
        segment.SetMaterial(level.pathMat);
        segment.sortingOrder = 100;
        segment.width = Statics.lineThickness / 4f;
        segment.SetLocalPosition(0, middle);
        segment.SetColor(Color.red);

        var startingAngle = 360f + Vector3.SignedAngle(path.GetPosition(0) - middle, Vector3.right, Vector3.back);
        var angle = startingAngle;

        var position = -1;

        Vector3 pointBefore = path.GetPosition(0);
        for (int i = 0; i < path.GetPositions().Count; i++)
        {
            var point = path.GetPosition(i);
            var angleDiff = Vector3.SignedAngle(point - middle, pointBefore - middle, Vector3.back);
            var newAngle = angle + angleDiff;
            if (angle < startingAngle && angleDiff > 0)
            {
                while (windingAngles[position] < newAngle)
                {
                    if (position > 0)
                    {
                        spiral.RemovePosition(position);
                        windingAngles.RemoveAt(position);
                        position--;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else if (angle > startingAngle && angleDiff < 0)
            {
                Debug.Log("As angle " + angle + " and startingAngle " + startingAngle + " and angleDiff " + angleDiff + ", start deleting");
                while (windingAngles[position] > newAngle)
                {
                    Debug.Log("As windingAngles[" + position + "] = " + windingAngles[position] + " > " + newAngle + ", continue at position " + position);
                    if (position > 0)
                    {
                        spiral.RemovePosition(position);
                        windingAngles.RemoveAt(position);
                        position--;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            Debug.Log("Position " + position + " <-> " + windingAngles.Count + " and " + spiral.positionCount);
            angle += angleDiff;
            var r = obstacle.transform.lossyScale.x / 2f + a * (startingAngle + Mathf.Abs(startingAngle - angle)) * Mathf.Deg2Rad;
            var p = angle * Mathf.Deg2Rad;
            var vector3 = new Vector3(r * Mathf.Cos(p), r * Mathf.Sin(p), 0) + middle;
            Debug.Log("Set position " + position);
            position++;
            spiral.SetLocalPosition(position, vector3);
            windingAngles.Add(angle);
            pointBefore = point;
            segment.SetLocalPosition(1, point, true);
            spiral.SetMesh();
            yield return null;
        }
        yield return new WaitForSeconds(2);
        segmentObject.SetActive(false);
        float alpha = spiral.GetColor().a;
        for (float t = 0.0f; t < 1.0f; t += Time.deltaTime / 10f)
        {
            Color newColor = new Color(spiral.GetColor().r, spiral.GetColor().g, spiral.GetColor().b, Mathf.Lerp(alpha, 0, t));
            spiral.SetColor(newColor);
            yield return null;
        }
        Destroy(segmentObject);
        yield break;
    }

    void TryConcatenation(Vector3 position1, Vector3 position2)
    {
        int pathNumber1 = -1;
        int pathNumber2 = -1;
        bool isOnPath1 = IsOnPath(position1, ref pathNumber1);
        bool isOnPath2 = IsOnPath(position2, ref pathNumber2);
        if (isOnPath1 && isOnPath2 && pathNumber1 != pathNumber2)
        {
            Debug.Log("Try to concatenate Path " + pathNumber1 + " and Path " + pathNumber2);
            var path1 = level.paths[pathNumber1];
            var path2 = level.paths[pathNumber2];
            ConcatenatePaths(path1, path2);
        }
    }

    bool isFrozen()
    {
        return isEndingPath || isDrawingPath;
    }

    void ConcatenatePaths(MPath path1, MPath path2)
    {
        if (path1.dotTo.Equals(path2.dotFrom))
        {
            if (actHomotopy != null)
            {
                DestroyHomotopy();
            }
            draggingPath = -1;
            Debug.Log("Concatenate!");
            level.StopParticleSystem(path1);
            path1.dotTo = path2.dotTo;

            StartCoroutine(SlowBuildPath(path1, path2));
        }
        else
        {
            Debug.Log("Not concatenable");
        }
    }

    IEnumerator SlowBuildPath(MPath path1, MPath path2)
    {
        Debug.Log("DrawPath " + path2);
        isDrawingPath = true;

        var countBefore = path1.Count;

        int sum = 0;
        var duration = 250f;

        float chunkSize = (float)path2.Count / duration;
        path1.gameObject.SetActive(true);
        path1.sortingOrder = Statics.LineSortingOrder;
        Statics.LineSortingOrder++;
        Debug.Log(chunkSize + "  = chunk");
        for (int i = 0; i < path2.Count; i++)
        {
            path1.SetPosition(countBefore + i, path2.GetPosition(i));
            sum++;
            if (sum > chunkSize)
            {
                sum = 0;
                path1.SetMesh();
                yield return null;
            }
        }

        for (int i = 0; i < path1.Count; i++)
        {
            var newPos = new Vector3(path1.GetPosition(i).x, path1.GetPosition(i).y, Statics.dotDepth.z * 0.99f);
            path1.SetPosition(i, newPos);
        }
        path1.SetMesh();
        isDrawingPath = false;

        var list1n = path1.GetNormals();
        var list2n = path2.GetNormals();
        list1n.AddRange(list2n);
        path1.SetNormals(list1n);

        DeletePath(path2);

        var index = level.paths.IndexOf(path1);
        level.particleSystems[index] = ps.SetParticleSystem(path1);
        level.StartParticleSystem(path1);
        yield break;
    }

    void DeletePath(MPath path)
    {
        level.StopParticleSystem(path);
        level.DeletePath(path);
        level.RecalculateHomClasses();
    }

    void DragPath(Vector3 touchPosition, Vector3 touchBefore)
    {
        bool dragSnuggledPositions;
        //		Debug.Log ("Homotopy has " + actHomotopy.closeNodePositions.Count + " snuggled Positions");
        if (actHomotopy.snugglingNodePositions.Contains(draggingPosition))
        {
            dragSnuggledPositions = true;
        }
        else
        {
            dragSnuggledPositions = false;
        }

        var path = actHomotopy.midPath;
        float t0 = (float)draggingPosition / path.Count;
        var distance = Vector2.Distance(touchPosition, touchBefore);
        var lowerLimit = Statics.lineThickness / 2f;
        var maxUpperLimit = Mathf.Min(t0, 1f - t0);
        var t = Mathf.Clamp01(distSoFar / 3f);
        float firstLowerSnugglePosition = maxUpperLimit;
        float firstUpperSnugglePosition = maxUpperLimit;

        if (!dragSnuggledPositions)
        {
            for (int i = draggingPosition; i < path.Count; i++)
            {
                if (i < upperBound)
                {
                    if (actHomotopy.snugglingNodePositions.Contains(i))
                    {
                        firstUpperSnugglePosition = (float)i / path.Count - t0;
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            for (int i = draggingPosition - 1; i > -1; i--)
            {
                if (i > lowerBound)
                {
                    if (actHomotopy.snugglingNodePositions.Contains(i))
                    {
                        firstLowerSnugglePosition = t0 - (float)i / path.Count;
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }
        float lowerB = Mathf.SmoothStep(lowerLimit, Mathf.Min(maxUpperLimit, firstLowerSnugglePosition), t);
        float upperB = Mathf.SmoothStep(lowerLimit, Mathf.Min(maxUpperLimit, firstUpperSnugglePosition), t);
        normal = (touchPosition - touchBefore);
        distSoFar += distance;

        var goalPoint = new Vector3(touchPosition.x, touchPosition.y, path.GetPosition(draggingPosition).z);
        Collider collider;
        var hasHit = Misc.HasHit(goalPoint, out collider);
        if (!hasHit)
        {
            path.SetPosition(draggingPosition, goalPoint);
            for (int i = draggingPosition + 1; i < path.Count - 1; i++)
            {
                if (i < upperBound)
                {
                    if (!setPoint(i, i + 1, normal, lowerB, upperB, t0, true))
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            for (int i = draggingPosition - 1; i > 0; i--)
            {
                if (i > lowerBound)
                {
                    if (!setPoint(i, i - 1, normal, lowerB, upperB, t0, false))
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }

    IEnumerator AdjustPositions()
    {
        while (draggingPath != -1 && draggingPosition != -1)
        {
            var path = actHomotopy.midPath;
            for (int i = draggingPosition + 1; i < path.Count - 1; i++)
            {
                if (i < upperBound)
                {
                    if (!setPoint2(i, path, normal, true))
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            for (int i = draggingPosition - 1; i > 0; i--)
            {
                if (i > lowerBound)
                {
                    if (!setPoint2(i, path, normal, false))
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            for (int i = 1; i < path.Count - 1; i++)
            {
                var position = path.GetPosition(i);
                Collider collider;
                bool hasHit = Misc.HasHit(position, out collider);
                if (hasHit)
                {
                    var newPos = Misc.PushOutOfObstacle(collider, position);
                    path.SetPosition(i, newPos);
                }
            }

            for (int i = 0; i < path.Count - 1; i++)
            {
                var position = path.GetPosition(i);
                var positionNext = path.GetPosition(i + 1);
                var dist = Vector3.Distance(position, positionNext);
                var meanDist = Statics.meanDist * 2f;
                if (dist > meanDist)
                {
                    Collider collider;
                    Vector3 positionMid = Vector3.Lerp(position, positionNext, 0.5f);
                    bool hasHit = Misc.HasHit(position, positionNext, out collider);
                    if (hasHit)
                    {
                        positionMid = Misc.PushOutOfObstacle(collider, positionMid);
                    }
                    Debug.Log("Distance between position " + i + " and " + (i + 1) + " is " + dist + ">" + meanDist);
                    var thisToMid = Vector3.Distance(position, positionMid);
                    Debug.Log("Distance between position " + i + " and midPosition is " + thisToMid + " vs. " + meanDist);
                    var midToNext = Vector3.Distance(positionMid, positionNext);
                    Debug.Log("Distance between position positionMid and " + (i + 1) + " is " + midToNext + " vs. " + meanDist);
                    Debug.Log("Insert new Position " + positionMid + " at " + (i + 1));
                    Debug.Log("DraggingPosition = " + draggingPosition);
                    if (thisToMid > 0 && midToNext > 0)
                    {
                        path.InsertPositionAt(i + 1, positionMid);
                        //						line.InsertNormalAt (i + 1, Vector3.Lerp (line.GetNormal (i), line.GetNormal (i + 1), 0.5f));
                        if (draggingPosition >= i + 1)
                        {
                            draggingPosition++;
                        }
                        //					Debug.Log ("Now DraggingPosition = " + draggingPosition);
                        //					Debug.Log ("Now Distance between position " + i + " and " + (i + 1) + " is " + Vector3.Distance (line.GetPosition (i), line.GetPosition (i + 1)) + " and the next is " + Vector3.Distance (line.GetPosition (i + 1), line.GetPosition (i + 2)));
                        i = Mathf.Max(0, i - 1);
                    }
                }
            }

            path.SetMesh();

            if (Statics.mesh)
            {
                actHomotopy.setMesh(level.GetStaticPositions());
            }
            else
            {
                actHomotopy.AddCurveToBundle(level.pathFactory, draggingPosition);
            }

            yield return null;
        }
        yield break;
    }

    bool setPoint2(int i, MPath path, Vector3 normal, bool up)
    {
        if (i < 1 || i == path.Count - 1)
        {
            return false;
        }

        int nextNode;
        int beforeNode;
        if (up)
        {
            nextNode = i - 1;
            beforeNode = i + 1;
        }
        else
        {
            nextNode = i + 1;
            beforeNode = i - 1;
        }
        var point = path.GetPosition(i);
        var nextPoint = path.GetPosition(nextNode);
        var pointBefore = path.GetPosition(beforeNode);

        var distToNext = Vector3.Distance(point, nextPoint);
        if (distToNext > Statics.meanDist)
        {
            var direction = nextPoint - point;
            //			Debug.Log ("Distance from " + i + " to " + nextNode + " = " + Vector2.Distance (nextPoint, point));
            var f = Mathf.Abs(direction.magnitude - Statics.meanDist);
            var goalPoint = point + direction.normalized * f;

            Collider collider;
            var hasHit = Misc.HasHit(goalPoint, out collider);
            if (hasHit)
            {
                path.SetPosition(i, Misc.PushOutOfObstacle(collider, goalPoint));
            }
            else
            {
                //				if (Vector2.Distance (goalPoint, line.GetPosition (i)) > 1f) {
                //					Debug.Log ("f from " + i + " to " + nextNode + " = " + f);
                //					EditorApplication.isPaused = true;
                //				}
                path.SetPosition(i, goalPoint);
                return true;
            }
        }
        return false;
    }

    bool setPoint(int i, int nexti, Vector3 normal, float lowerB, float upperB, float t0, bool up)
    {
        var path = actHomotopy.midPath;
        var draggingPoint = path.GetPosition(draggingPosition);
        Vector3 point = path.GetPosition(i);
        var direction = point - draggingPoint;
        var factor = GetFactor(i, t0, lowerB, upperB);
        var dirToNext = point - path.GetPosition(nexti);
        var t = 1f;
        Vector3 pullDirection = t * normal + (1 - t) * dirToNext;
        var goalPoint = point + factor * pullDirection;

        if (i > 0 && i < path.Count - 1)
        {
            int nextNode;
            if (up)
            {
                nextNode = i - 1;
            }
            else
            {
                nextNode = i + 1;
            }
            var nextPoint = path.GetPosition(nextNode);
            var distToNext = Vector3.Distance(point, nextPoint);
            if (distToNext < Statics.meanDist * 1.01f)
            {
                return false;
            }
        }
        Collider collider;
        var hasHit = Misc.HasHit(goalPoint, out collider);
        if (hasHit)
        {
            var staticNormal = Misc.GetNormal(collider, goalPoint);
            float staticAngle = Vector3.Angle(staticNormal, direction);
            if (staticAngle < 90)
            {
                //pull
                if (up)
                {
                    upperBound = i;
                }
                else
                {
                    lowerBound = i;
                }
            }
            return false;
        }
        path.SetPosition(i, goalPoint);
        return true;
    }

    float GetFactor(float i, float t0, float lowerB, float upperB)
    {
        float count = actHomotopy.midPath.Count;
        float t = i / count;
        if (i == 0 || i == count - 1)
        {
            return 0;
        }
        if (t < t0)
        {
            return Mathf.SmoothStep(0f, 1f, (t - (t0 - lowerB)) / lowerB);
        }
        else
        {
            return Mathf.SmoothStep(0f, 1f, -(t - (t0 + upperB)) / upperB);
        }
    }

    void EndDraggingPath()
    {
        Debug.Log("End Dragging Path");
        isEndingPath = true;
        MPath path = actHomotopy.midPath;

        Debug.Log("Smoothline");
        path.smoothLine();
        path.RefinePath(actHomotopy.closestPositions);
        Debug.Log("Start check if snuggling");
        StartCoroutine(CheckIfPathsAreSnuggling());
        Debug.Log("End check if snuggling, start snuggletopath");
    }

    IEnumerator CheckIfPathsAreSnuggling()
    {
        Debug.Log("Check if snuggling");
        var midPath = actHomotopy.midPath;
        var closestPositions = actHomotopy.closestPositions;
        closestPositions.Clear();
        for (int i = draggingPosition; i < midPath.Count - 1; i++)
        {
            var pos = midPath.GetPosition(i);
            float closest = 9999f;
            foreach (var otherPath in level.paths)
            {
                if (!actHomotopy.path1.Equals(otherPath))
                {
                    for (int k = 0; k < otherPath.Count; k++)
                    {
                        Vector3 pos2 = otherPath.GetPosition(k);
                        var dist = Vector2.Distance(pos, pos2);
                        if (dist < closest)
                        {
                            closestPositions[i] = pos2;
                            closest = dist;
                        }
                    }
                }
            }
            if (closest < 9999f && closest < minDist) { }
            else
            {
                closestPositions.Remove(i);
                break;
            }
        }

        for (int i = draggingPosition - 1; i > 0; i--)
        {
            var pos = midPath.GetPosition(i);
            float closest = 9999f;
            foreach (var otherPath in level.paths)
            {
                if (!actHomotopy.path1.Equals(otherPath))
                {
                    for (int k = 0; k < otherPath.Count; k++)
                    {
                        Vector3 pos2 = otherPath.GetPosition(k);
                        var dist = Vector2.Distance(pos, pos2);
                        if (dist < closest)
                        {
                            closestPositions[i] = pos2;
                            closest = dist;
                        }
                    }
                }
            }
            if (closest < 9999f && closest < minDist) { }
            else
            {
                closestPositions.Remove(i);
                break;
            }
        }
        Debug.Log("End check if snuggling, start snuggletopath");
        StartCoroutine(SnuggleToPath());
        yield break;
    }

    IEnumerator SnuggleToPath()
    {
        Debug.Log("Snuggle To Path");
        Dictionary<int, Vector3> goalPoints = new Dictionary<int, Vector3>();
        Dictionary<int, Vector3> startPoints = new Dictionary<int, Vector3>();
        var closestPositions = actHomotopy.closestPositions;
        var path = actHomotopy.midPath;
        foreach (var pair in closestPositions)
        {
            var position = pair.Key;
            var closestPosition = pair.Value;
            float range = 30f;
            float counter = 1f;
            for (int i = 1; i < range; i++)
            {
                var index = position - i;
                if (index > -1)
                {
                    var backwardsPosition = path.GetPosition(index);
                    if (closestPositions.ContainsKey(index))
                    {
                        break;
                    }
                    var factor = Mathf.SmoothStep(1, 0, Mathf.Clamp01(counter / range));
                    var direction = (closestPosition - backwardsPosition);
                    var goalPoint = backwardsPosition + factor * direction;
                    goalPoints[index] = goalPoint;
                    counter++;
                }
            }
            counter = 1f;
            for (int i = 1; i < range; i++)
            {
                var index = position + i;
                if (index < path.Count)
                {
                    var backwardsPosition = path.GetPosition(index);
                    if (closestPositions.ContainsKey(index))
                    {
                        break;
                    }
                    var factor = Mathf.SmoothStep(1, 0, Mathf.Clamp01(counter / range));
                    var direction = (closestPosition - backwardsPosition);
                    var goalPoint = backwardsPosition + factor * direction;
                    goalPoints[index] = goalPoint;
                    counter++;
                }
            }
            goalPoints.Add(position, closestPosition);
        }
        foreach (var entry in goalPoints)
        {
            startPoints.Add(entry.Key, path.GetPosition(entry.Key));
        }
        float duration = 1;
        bool localMorph = true;
        var time = 0f;
        while (localMorph)
        {
            localMorph = false;
            time += Time.deltaTime;
            var t = Mathf.Clamp01(time / duration);

            foreach (var entry in goalPoints)
            {
                var index = entry.Key;
                var goalPoint = entry.Value;
                var startPoint = startPoints[index];
                var goalPointStep = Vector3.Lerp(startPoint, goalPoint, t);

                var distance = Vector2.Distance(goalPointStep, goalPoint);

                path.SetPosition(index, goalPointStep);
                if (t < 0.99f)
                {
                    localMorph = true;
                }
            }
            path.SetMesh();
            if (Statics.mesh)
            {
                actHomotopy.setMesh(level.GetStaticPositions());
            }
            else
            {
                actHomotopy.AddCurveToBundle(level.pathFactory, draggingPosition);
            }
            yield return null;
        }
        Debug.Log("Done Snuggle To Path, start refinePath");
        actHomotopy.snugglingNodePositions = path.RefinePath(actHomotopy.closestPositions);
        if (Statics.mesh)
        {
            actHomotopy.setMesh(level.GetStaticPositions());
        }
        else
        {
            actHomotopy.AddCurveToBundle(level.pathFactory, draggingPosition);
        }
        upperBound = actHomotopy.midPath.Count;
        lowerBound = -1;
        distSoFar = 0f;
        draggingPath = -1;
        draggingPosition = -1;
        CheckIfHomotopyDone();
        isEndingPath = false;
        yield break;
    }

    void CheckIfHomotopyDone()
    {
        Debug.Log("Start CheckIfHomotopyDone");
        var midPath = actHomotopy.midPath;
        var dotFromPos = midPath.dotFrom.transform.position;
        var dotToPos = midPath.dotTo.transform.position;
        var pathHomClasses = level.pathHomClasses;
        var indexOfPath1 = level.paths.IndexOf(actHomotopy.path1);
        Debug.Log("Index of Path 1 is " + indexOfPath1);
        var dot = CheckIfNullHomotopic();
        if (dot != null)
        {
            DestroyHomotopy();
        }
        else
        {
            List<int> homClass = level.GetHomotopyClass(indexOfPath1);
            Debug.Log("Homclass of this homotopy contains " + string.Join(",", homClass.Select(x => x.ToString()).ToArray()));
            foreach (var otherPathNum in homClass)
            {
                if (otherPathNum != indexOfPath1)
                {
                    var otherPath = level.paths[otherPathNum];
                    Debug.Log("Check Path " + otherPath.pathNumber);
                    bool homotopic = true;
                    Debug.Log("Check if homotopic");
                    for (int i = 0; i < midPath.Count; i++)
                    {
                        var midPathPos = midPath.GetPosition(i);
                        if (Vector2.Distance(midPathPos, dotFromPos) > Statics.dotRadius && Vector2.Distance(midPathPos, dotToPos) > Statics.dotRadius)
                        {
                            bool existsNearNode = false;
                            for (int j = 0; j < otherPath.Count; j++)
                            {
                                var otherPathPos = otherPath.GetPosition(j);
                                var dist = Vector2.Distance(midPathPos, otherPathPos);
                                if (dist < Statics.homotopyNearness)
                                {
                                    existsNearNode = true;
                                    break;
                                }
                            }
                            if (!existsNearNode)
                            {
                                homotopic = false;
                                break;
                            }
                        }
                    }
                    if (homotopic)
                    {
                        for (int i = 0; i < otherPath.Count; i++)
                        {
                            var otherPathPos = otherPath.GetPosition(i);
                            if (Vector2.Distance(otherPathPos, dotFromPos) > Statics.dotRadius && Vector2.Distance(otherPathPos, dotToPos) > Statics.dotRadius)
                            {
                                bool existsNearNode = false;
                                for (int j = 0; j < midPath.Count; j++)
                                {
                                    var midPathPos = midPath.GetPosition(j);
                                    var dist = Vector2.Distance(otherPathPos, midPathPos);
                                    if (dist < Statics.homotopyNearness)
                                    {
                                        existsNearNode = true;
                                        break;
                                    }
                                }
                                if (!existsNearNode)
                                {
                                    homotopic = false;
                                    break;
                                }
                            }
                        }

                    }
                    Debug.Log("Check if homotopic, done : " + homotopic.ToString());
                    if (homotopic)
                    {
                        if (Statics.mesh)
                        {
                            actHomotopy.setMesh(level.GetStaticPositions());
                        }
                        else
                        {
                            actHomotopy.AddCurveToBundle(level.pathFactory, draggingPosition);
                        }
                        otherPath.SetColor(midPath.GetColor());
                        DestroyHomotopy();
                        break;
                    }
                }
            }
        }
    }

    private GameObject CheckIfNullHomotopic()
    {
        var midPath = actHomotopy.midPath;
        foreach (var dot in level.dots)
        {
            var positions = midPath.GetPositions();
            bool isNull = true;
            foreach (var position in positions)
            {
                if (Vector2.Distance(dot.transform.position, position) > 2 * Statics.dotRadius)
                {
                    isNull = false;
                    break;
                }
            }
            if (isNull)
            {
                return dot;
            }
        }
        return null;
    }

    Vector3 SetOnSurface(Vector3 dotPosition)
    {
        return new Vector3(dotPosition.x, dotPosition.y, dotMoveDepth.z);
    }

    void DrawLine(RaycastHit hit)
    {
        dot1 = level.dots.IndexOf(hit.collider.gameObject);
        var dotPosition = level.dots[dot1].transform.position;
        if (dot1 != -1)
        {
            touchStart = hit.collider.gameObject.transform.position;
            Debug.Log("Instantiate");
            dotMove = Instantiate(level.dots[dot1]);
            dotMove.GetComponent<SphereCollider>().isTrigger = false;
            dotMove.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;
            dotMove.transform.position = SetOnSurface(dotPosition);
            trail = setTrail();
            Statics.LineSortingOrder++;
            if (targetJoint == null)
            {
                Debug.Log("add2");
                targetJoint = dotMove.AddComponent<TargetJoint3D>() as TargetJoint3D;
                targetJoint.target = touchStart;
            }
        }
        else
        {
            dotMove = null;
        }
    }

    TrailRenderer setTrail()
    {
        var trail = dotMove.GetComponentInChildren<TrailRenderer>();
        trail.textureMode = LineTextureMode.Tile;
        trail.material = level.trailMat;
        trail.startWidth = Statics.lineThickness;
        trail.endWidth = Statics.lineThickness;
        return trail;
    }

    void DestroyHomotopy()
    {
        if (actHomotopy != null)
        {
            level.StopParticleSystem(actHomotopy.midPath);
            actHomotopy.Clear();
            actHomotopy = null;
        }
    }

    IEnumerator adjustPositions;

    void setPathToDrag(Vector3 touchPoint, int pathNumber)
    {
        int numOnPath = -1;
        Vector3 vector = new Vector3();
        bool isOnPath = GetPositionOnPath(pathNumber, touchPoint, ref numOnPath, ref vector);
        MPath path;
        if (pathNumber == -2)
        {
            path = actHomotopy.midPath;
        }
        else
        {
            path = level.paths[pathNumber];
        }
        if (isOnPath && numOnPath != 0 && numOnPath != path.Count - 1)
        {
            Debug.Log("Start Dragging Line " + pathNumber);
            if (adjustPositions != null)
            {
                StopCoroutine(adjustPositions);
            }
            if (pathNumber != -2)
            {
                var path1 = level.paths[pathNumber];
                if (actHomotopy != null)
                {
                    Destroy(GameObject.Find("MidPath"));
                    DestroyHomotopy();
                }
                var actMidPath = level.NewPath(path1.GetColor(), path1.dotFrom, path1.dotTo);
                actMidPath.SetName("MidPath");
                level.StopParticleSystem(path1);
                level.particleSystems.Add(ps.SetParticleSystem(actMidPath));
                actHomotopy = level.NewHomotopy(path1, actMidPath);
            }
            upperBound = actHomotopy.midPath.Count;
            lowerBound = -1;
            draggingPath = -2;
            draggingPosition = numOnPath;

            adjustPositions = AdjustPositions();
            StartCoroutine(adjustPositions);
        }
        else
        {
            Debug.Log("Not on Line");
        }
    }

    void DrawDot(Vector3 touchPoint)
    {
        RaycastHit hit;
        var hasHit = Physics.SphereCast(cameraPosition, Statics.dotRadius, touchPoint - cameraPosition, out hit);
        if (!hasHit)
        {
            Debug.Log("Touch not on some Path, so set Dot");
            GameObject dot = Instantiate(level.dotPrefab);
            dot.transform.position = touchPoint;
            dot.transform.position += Vector3.back;
            if (level.staticsAllowed || Statics.levelType.Equals("dots"))
            {
                dot.GetComponent<SpriteRenderer>().color = level.GetNextColor(MType.Dot);
            }
            else
            {
                dot.GetComponent<SpriteRenderer>().color = Color.gray;
            }
            level.addDot(dot);
            dot.GetComponent<SpriteRenderer>().sortingOrder = 100;
        }
    }

    void FinishLine()
    {
        if (dotMove.GetComponent<dotBehaviour>().IsTriggered())
        {
            Debug.Log("Triggered");
            var dotBehaviourDotMove = dotMove.GetComponent<dotBehaviour>();
            var bumpedDot = dotBehaviourDotMove.GetTriggerObject();
            dot2 = level.dots.IndexOf(bumpedDot);
            var path = level.NewPath(level.dots[dot1], level.dots[dot2]);
            Debug.Log("Number of trail Positions: " + trail.positionCount);
            var factor = 1.5f;
            for (int i = 0; i < trail.positionCount; i++)
            {
                path.SetPosition(i, new Vector3(trail.GetPosition(i).x, trail.GetPosition(i).y, Statics.dotDepth.z * factor));
            }
            path.InsertPositionAt(0, new Vector3(level.dots[dot1].transform.position.x, level.dots[dot1].transform.position.y, Statics.dotDepth.z * factor));
            path.SetPosition(path.Count, new Vector3(bumpedDot.transform.position.x, bumpedDot.transform.position.y, Statics.dotDepth.z * factor));
            //Set Colors
            dotMove.transform.position = bumpedDot.transform.position;
            dotMove.GetComponent<SpriteRenderer>().enabled = false;
            StartCoroutine(DrawPath(path, false));
        }
        else
        {
            Destroy(dotMove);
            isDrawingPath = false;
        }
    }

    void SetDotColors(int dot1, int dot2)
    {
        var group1 = new List<int>();
        var group2 = new List<int>();
        for (int i = 0; i < level.dotHomClasses.Count; i++)
        {
            var group = level.dotHomClasses[i];
            if (group.Contains(dot1))
            {
                group1 = group;
            }
            else if (group.Contains(dot2))
            {
                group2 = group;
            }
        }
        for (int i = 0; i < group2.Count; i++)
        {
            var item = group2[i];
            level.dots[item].GetComponent<SpriteRenderer>().color = level.dots[dot1].GetComponent<SpriteRenderer>().color;
            group1.Add(item);
        }
    }

    // end of update
    //Returns number of path, index on path
    bool GetPositionOnPath(int pathNumber, Vector3 touchPoint, ref int numOnPath, ref Vector3 posOnPath)
    {
        numOnPath = -1;
        float distToPath = Statics.lineThickness * 2f;
        float minDist = distToPath;
        if (pathNumber == -2)
        {
            var path = actHomotopy.midPath;
            for (int j = 0; j < path.Count; j++)
            {
                var value = path.GetPosition(j);
                var dist = Vector2.Distance(touchPoint, value);
                if ((dist < distToPath && numOnPath == -1) || (dist < minDist && numOnPath != -1))
                {
                    numOnPath = j;
                    minDist = dist;
                    posOnPath = value;
                }
            }
            if (numOnPath != -1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            var path = level.paths[pathNumber];
            for (int j = 0; j < path.Count; j++)
            {
                var position = path.GetPosition(j);
                var dist = Vector2.Distance(touchPoint, position);
                if ((dist < distToPath && numOnPath == -1) || (dist < minDist && numOnPath != -1))
                {
                    numOnPath = j;
                    minDist = dist;
                    posOnPath = position;
                }
            }
            if (numOnPath != -1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    bool IsOnPath(Vector3 touchPoint, ref int pathNumber, bool actHomotopyNotNull = false)
    {
        pathNumber = -1;
        if (actHomotopyNotNull)
        {
            if (actHomotopy != null)
            {
                Debug.Log("Check Midpath");
                var path = actHomotopy.midPath;
                var node = 0;
                while (node < path.Count)
                {
                    var dist = Vector2.Distance(touchPoint, path.GetPosition(node));
                    if (dist < minDist)
                    {
                        pathNumber = -2;
                        return true;
                    }
                    node++;
                }
            }
        }
        Debug.Log("Check other Paths");
        for (int i = 0; i < level.paths.Count; i++)
        {
            var path = level.paths[i];
            if (actHomotopyNotNull && path.Equals(actHomotopy.path1))
            {
                //nothing
            }
            else
            {
                var node = 0;
                while (node < path.Count)
                {
                    var dist = Vector2.Distance(touchPoint, path.GetPosition(node));
                    if (dist < minDist)
                    {
                        pathNumber = i;
                        return true;
                    }
                    node++;
                }
            }
        }
        return false;
    }

    bool IsOnPaths(Vector3 touchPoint, ref List<int> pathNumbers, bool actHomotopyNotNull = false)
    {
        pathNumbers = new List<int>();
        for (int i = 0; i < level.paths.Count; i++)
        {
            var path = level.paths[i];
            var node = 0;
            while (node < path.Count)
            {
                var dist = Vector2.Distance(touchPoint, path.GetPosition(node));
                if (dist < minDist)
                {
                    pathNumbers.Add(i);
                    break;
                }
                node++;
            }
        }
        if (pathNumbers.Count > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    bool IsOnMidPath(Vector3 worldPoint, ref int numOnPath, ref Vector3 vector)
    {
        numOnPath = -1;
        float distToPath = Statics.lineThickness * 3f;
        float minDist = distToPath;
        var path = actHomotopy.midPath;
        var node = 0;
        int counter = 0;
        while (node < path.Count)
        {
            var value = path.GetPosition(node);
            var dist = Vector2.Distance(worldPoint, value);
            if ((dist < distToPath && numOnPath == -1) || (dist < minDist && numOnPath != -1))
            {
                numOnPath = counter;
                minDist = dist;
                vector = value;
            }
            counter++;
            node++;
        }
        if (numOnPath != -1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    IEnumerator DrawPath(MPath path, bool onLoad)
    {
        Debug.Log("DrawPath " + path);
        isDrawingPath = true;
        isCollided[path] = false;
        path.gameObject.SetActive(false);
        path.smoothLine();
        path.RefinePath();
        //		ShortenPath (path);
        int sum = 0;
        var duration = 150f;
        if (onLoad)
        {
            duration = 100f;
        }
        float chunkSize = (float)path.Count / duration;
        var savedPositions = path.GetPositions();
        path.ClearPositions();
        path.gameObject.SetActive(true);
        path.sortingOrder = Statics.LineSortingOrder;
        Statics.LineSortingOrder++;
        Debug.Log(chunkSize + "  = chunk");
        for (int i = 0; i < savedPositions.Count; i++)
        {
            path.SetPosition(i, savedPositions[i]);
            sum++;
            if (sum > chunkSize)
            {
                sum = 0;
                path.SetMesh();
                yield return null;
            }
        }

        for (int i = 0; i < path.Count; i++)
        {
            var newPos = new Vector3(path.GetPosition(i).x, path.GetPosition(i).y, Statics.dotDepth.z * 0.99f);
            path.SetPosition(i, newPos);
        }
        path.SetMesh();

        SetDotColors(dot1, dot2);
        Destroy(dotMove);
        if (!onLoad)
        {
            level.addPath(path);
        }
        isDrawingPath = false;

        level.particleSystems.Add(ps.SetParticleSystem(path));

        level.RecalculateHomClasses();
        yield break;
    }

    Connector C;

    public void Connect(MPath path1, MPath path2, int path1Num, int path2Num)
    {
        Debug.Log("Start retracting");
        C = this.gameObject.AddComponent<Connector>();
        C.StartConnector(path1, path2, path1Num, path2Num, level);
    }

    Retractor R;
    private IEnumerator drawingWindingNumberRoutine = null;

    public void Retract(MPath path)
    {
        Debug.Log("Start retracting");
        R = this.gameObject.AddComponent<Retractor>();
        R.StartRetractor(path, 2f);
    }

}