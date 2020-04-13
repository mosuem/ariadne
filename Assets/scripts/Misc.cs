using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public static class Misc {
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
    public static Vector3 GetNormal (Collider collider, Vector3 collPoint) {
        Vector3 normal;
        if (collider.GetType () == typeof (BoxCollider)) {
            var coll = (BoxCollider) collider;
            var center = coll.bounds.center;
            var d = Vector3.Distance (collPoint, center);
            var halfWidth = coll.size.x / 2;
            var halfHeight = coll.size.y / 2;
            if (collPoint.x > center.x + halfWidth) {
                //Right Side
                if (collPoint.y > center.y + halfHeight) {
                    //Top Side
                    //3
                    normal = collPoint - center;
                } else if (collPoint.y > center.y + halfHeight) {
                    //Bottom Side
                    // 5
                    normal = collPoint - center;
                } else {
                    //Mid
                    //4
                    normal = Vector2.right * d;
                }
            } else if (collPoint.x < center.x - halfWidth) {
                //Left Side
                if (collPoint.y > center.y + halfHeight) {
                    //Top Side
                    //1
                    normal = collPoint - center;
                } else if (collPoint.y > center.y + halfHeight) {
                    //Bottom Side
                    // 7
                    normal = collPoint - center;
                } else {
                    //Mid
                    //8
                    normal = Vector2.left * d;
                }
            } else {
                //Up or Down
                if (collPoint.y > center.y + halfHeight) {
                    //Top Side
                    //2
                    normal = Vector2.up * d;
                } else {
                    //Bottom Side
                    //6
                    normal = Vector2.down * d;
                }
            }
        } else if (collider.GetType () == typeof (SphereCollider)) {
            var coll = (SphereCollider) collider;
            normal = collPoint - coll.bounds.center;
        } else {
            normal = Vector2.zero;
        }
        return normal;
    }

    public static Vector3 PushOutOfObstacle (Collider collider, Vector3 position) {
        var normal = GetNormal (collider, position);
        if (collider.GetType () == typeof (SphereCollider)) {
            var coll = (SphereCollider) collider;
            return collider.transform.position + (Statics.lineRadius + collider.gameObject.transform.localScale.x / 2) * normal.normalized;
        } else if (collider.GetType () == typeof (BoxCollider)) {
            var coll = (BoxCollider) collider;
            return coll.ClosestPointOnBounds (position) + Statics.lineRadius * normal.normalized;
        }
        return Vector3.zero;
    }

    public static bool HasHit (Vector3 goalPoint, out Collider collider) {
        var colliders = Physics.OverlapSphere (goalPoint, Statics.lineRadius);
        var hasHit = false;
        collider = null;
        foreach (var coll in colliders) {
            if (coll.gameObject.CompareTag ("Obstacle")) {
                hasHit = true;
                collider = coll;
                break;
            }
        }
        return hasHit;
    }

    public static bool HasHit (Vector3 point1, Vector3 point2, out Collider collider) {
        var colliders = Physics.OverlapCapsule (point1, point2, Statics.lineRadius);
        var hasHit = false;
        collider = null;
        foreach (var coll in colliders) {
            if (coll.gameObject.CompareTag ("Obstacle")) {
                hasHit = true;
                collider = coll;
                break;
            }
        }
        return hasHit;
    }

    public static bool hitObstacle (RaycastHit hit) {
        return hit.collider.isTrigger == false;
    }

    //	public static Vector3 PushOutOfObstacle (RaycastHit2D hit2)
    //	{
    //		var coll = hit2.collider;
    //		var hitPoint = hit2.point;
    //		Vector3 normal = GetNormal (coll, hitPoint);
    //		var newPos = coll.gameObject.transform.position + normal * ((normal.magnitude + Statics.dotRadius) / normal.magnitude);
    //		return newPos;
    //	}

    public static int DirCount (DirectoryInfo d) {
        int i = 0;
        // Add file sizes.
        FileInfo[] fis = d.GetFiles ();
        foreach (FileInfo fi in fis) {
            if (fi.Extension.Contains ("dat"))
                i++;
        }
        return i;
    }

    public static int DirCount (string levelType, string dataType) {
        string gameDataProjectFilePath = "/StreamingAssets/";
        var d = new DirectoryInfo (Application.dataPath + gameDataProjectFilePath + levelType + "/");
        int i = 0;
        // Add file sizes.
        FileInfo[] fis = d.GetFiles ();
        foreach (FileInfo fi in fis) {
            if (fi.Extension.Contains (dataType))
                i++;
        }
        return i;
    }

    public static int MaxLevel (string levelType) {
        int max = 0;
        string gameDataProjectFilePath = "/StreamingAssets/";
        var d = new DirectoryInfo (Application.dataPath + gameDataProjectFilePath + levelType + "/");
        // Add file sizes.
        FileInfo[] fis = d.GetFiles ();
        foreach (FileInfo fi in fis) {
            if (fi.Extension.Contains ("dat")) {
                string number = Regex.Replace (fi.Name, "[^0-9]", "");
                var num = int.Parse (number);
                if (num > max) {
                    max = num;
                }
            }
        }
        return max;
    }

    public static List<FileInfo> GetFiles (string levelType, string dataType) {
        List<FileInfo> files = new List<FileInfo> ();
        var sep = "/";
        string folderPath = Statics.folderPath + levelType + sep;
        Debug.Log ("Get files of type " + dataType + " in folder " + folderPath);
        var d = new DirectoryInfo (folderPath);
        // Add file sizes.
        FileInfo[] fis = d.GetFiles ();
        foreach (FileInfo fi in fis) {
            if (fi.Extension.Contains (dataType))
                files.Add (fi);
        }
        return files;
    }

    //	public static Vector3 SetOnSurface (Vector3 vector, float spacer)
    //	{
    //		if (Statics.isSphere) {
    //			return vector.normalized * (Statics.sphereRadius + spacer);
    //		} else if (Statics.isTorus) {
    //			var onRing = new Vector3 (vector.x, 1.2f, vector.z).normalized;
    //			onRing *= Statics.torusRadius1;
    //			return (vector - onRing).normalized * (Statics.torusRadius2 + spacer) + onRing;
    //		} else {
    //			return new Vector3 (vector.x, vector.y, spacer);
    //		}
    //	}

    public static Vector3 ManifoldCenter (Vector3 vector) {
        if (Statics.isSphere) {
            return Vector3.zero;
        } else if (Statics.isTorus) {
            var vector3 = new Vector3 (vector.x, 0, vector.z);
            return vector3.normalized * Statics.torusRadius1;
        } else {
            return new Vector3 (vector.x, vector.y, 0);
        }
    }

    public static Manifold BuildSphere (Material mat, Color c) {
        GameObject gameObject = new GameObject ("Sphere");
        gameObject.tag = "Sphere";
        MeshFilter filter = gameObject.AddComponent<MeshFilter> ();
        Mesh mesh = filter.mesh;
        mesh.Clear ();

        float radius = Statics.sphereRadius;
        // Longitude |||
        int nbLong = 48;
        // Latitude ---
        int nbLat = 32;

        #region Vertices
        Vector3[] vertices = new Vector3[(nbLong + 1) * nbLat + 2];
        float _pi = Mathf.PI;
        float _2pi = _pi * 2f;

        vertices[0] = Vector3.up * radius;
        for (int lat = 0; lat < nbLat; lat++) {
            float a1 = _pi * (float) (lat + 1) / (nbLat + 1);
            float sin1 = Mathf.Sin (a1);
            float cos1 = Mathf.Cos (a1);

            for (int lon = 0; lon <= nbLong; lon++) {
                float a2 = _2pi * (float) (lon == nbLong ? 0 : lon) / nbLong;
                float sin2 = Mathf.Sin (a2);
                float cos2 = Mathf.Cos (a2);

                vertices[lon + lat * (nbLong + 1) + 1] = new Vector3 (sin1 * cos2, cos1, sin1 * sin2) * radius;
            }
        }
        vertices[vertices.Length - 1] = Vector3.up * -radius;
        #endregion

        #region Normales		
        Vector3[] normales = new Vector3[vertices.Length];
        for (int n = 0; n < vertices.Length; n++)
            normales[n] = vertices[n].normalized;
        #endregion

        #region UVs
        Vector2[] uvs = new Vector2[vertices.Length];
        uvs[0] = Vector2.up;
        uvs[uvs.Length - 1] = Vector2.zero;
        for (int lat = 0; lat < nbLat; lat++)
            for (int lon = 0; lon <= nbLong; lon++)
                uvs[lon + lat * (nbLong + 1) + 1] = new Vector2 ((float) lon / nbLong, 1f - (float) (lat + 1) / (nbLat + 1));
        #endregion

        #region Triangles
        int nbFaces = vertices.Length;
        int nbTriangles = nbFaces * 2;
        int nbIndexes = nbTriangles * 3;
        int[] triangles = new int[nbIndexes];

        //Top Cap
        int i = 0;
        for (int lon = 0; lon < nbLong; lon++) {
            triangles[i++] = lon + 2;
            triangles[i++] = lon + 1;
            triangles[i++] = 0;
        }

        //Middle
        for (int lat = 0; lat < nbLat - 1; lat++) {
            for (int lon = 0; lon < nbLong; lon++) {
                int current = lon + lat * (nbLong + 1) + 1;
                int next = current + nbLong + 1;

                triangles[i++] = current;
                triangles[i++] = current + 1;
                triangles[i++] = next + 1;

                triangles[i++] = current;
                triangles[i++] = next + 1;
                triangles[i++] = next;
            }
        }

        //Bottom Cap
        for (int lon = 0; lon < nbLong; lon++) {
            triangles[i++] = vertices.Length - 1;
            triangles[i++] = vertices.Length - (lon + 2) - 1;
            triangles[i++] = vertices.Length - (lon + 1) - 1;
        }
        #endregion

        mesh.vertices = vertices;
        mesh.normals = normales;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateBounds ();
        var renderer = gameObject.AddComponent<MeshRenderer> ();
        renderer.material = mat;
        renderer.material.color = c;

        var collider = gameObject.AddComponent<MeshCollider> ();
        //		collider.isTrigger = true;

        var manifold = gameObject.AddComponent<Manifold> ();
        return manifold;
    }

    public static Manifold BuildTorus (Material mat, Color color) {
        GameObject gameObject = new GameObject ("Torus");
        gameObject.tag = "Sphere";
        MeshFilter filter = gameObject.AddComponent<MeshFilter> ();
        Mesh mesh = filter.mesh;
        mesh.Clear ();

        float radius1 = Statics.torusRadius1;
        float radius2 = Statics.torusRadius2;
        int nbRadSeg = 48;
        int nbSides = 36;

        #region Vertices		
        Vector3[] vertices = new Vector3[nbRadSeg * nbSides];
        float _2pi = Mathf.PI * 2f;
        for (int seg = 0; seg < nbRadSeg; seg++) {

            float t1 = (float) seg / nbRadSeg * _2pi;
            Vector3 r1 = new Vector3 (Mathf.Cos (t1) * radius1, 0f, Mathf.Sin (t1) * radius1);

            for (int side = 0; side < nbSides; side++) {

                Vector3 normale = Vector3.Cross (r1, Vector3.up);
                float t2 = (float) side / nbSides * _2pi;
                Vector3 r2 = Quaternion.AngleAxis (-t1 * Mathf.Rad2Deg, Vector3.up) * new Vector3 (Mathf.Sin (t2) * radius2, Mathf.Cos (t2) * radius2);

                vertices[side + seg * nbSides] = r1 + r2;
            }
        }
        #endregion

        #region Triangles
        int nbTriangles = vertices.Length * 2;
        int[] triangles = new int[(nbTriangles * 3)];
        /*
        a b
        c d
         */
        int i = 0;
        for (int seg = 0; seg < nbRadSeg; seg++) {
            for (int side = 0; side < nbSides; side++) {
                int nextSide = side + 1 == nbSides ? 0 : side + 1;
                int nextSeg = seg + 1 == nbRadSeg ? 0 : (seg + 1) * nbSides;
                var a = side + seg * nbSides;
                var b = nextSide + seg * nbSides;
                var c = side + nextSeg;
                var d = nextSide + nextSeg;

                int next = side + nextSeg;

                triangles[i++] = a;
                triangles[i++] = c;
                triangles[i++] = b;

                triangles[i++] = b;
                triangles[i++] = c;
                triangles[i++] = d;
            }
        }
        #endregion

        mesh.vertices = vertices;
        // mesh.normals = normales;
        // mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateBounds ();
        mesh.RecalculateNormals ();
        var renderer = gameObject.AddComponent<MeshRenderer> ();
        renderer.material = mat;
        renderer.material.color = Color.grey;

        var collider = gameObject.AddComponent<MeshCollider> ();
        //		collider.isTrigger = true;

        var manifold = gameObject.AddComponent<Manifold> ();
        return manifold;
    }

    public static Manifold BuildHalfTorus (Material mat, Color color) {
        GameObject gameObject = new GameObject ("Torus");
        gameObject.tag = "Sphere";
        MeshFilter filter = gameObject.AddComponent<MeshFilter> ();
        Mesh mesh = filter.mesh;
        mesh.Clear ();

        float radius1 = Statics.torusRadius1;
        float radius2 = Statics.torusRadius2;
        int nbRadSeg = 48;
        int nbSides = 36;

        #region Vertices		
        Vector3[] vertices = new Vector3[nbRadSeg * nbSides];
        float _2pi = Mathf.PI * 2f;
        for (int seg = 0; seg < nbRadSeg; seg++) {

            float t1 = (float) seg / nbRadSeg * (7f * Mathf.PI / 4f);
            Vector3 r1 = new Vector3 (Mathf.Cos (t1) * radius1, 0f, Mathf.Sin (t1) * radius1);

            for (int side = 0; side < nbSides; side++) {

                Vector3 normale = Vector3.Cross (r1, Vector3.up);
                float t2 = (float) side / nbSides * _2pi;
                Vector3 r2 = Quaternion.AngleAxis (-t1 * Mathf.Rad2Deg, Vector3.up) * new Vector3 (Mathf.Sin (t2) * radius2, Mathf.Cos (t2) * radius2);

                vertices[side + seg * nbSides] = r1 + r2;
            }
        }
        #endregion

        #region Triangles
        int nbTriangles = vertices.Length * 2;
        int[] triangles = new int[(nbTriangles * 3)];
        /*
        a b
        c d
         */
        int i = 0;
        for (int seg = 0; seg < nbRadSeg - 1; seg++) {
            for (int side = 0; side < nbSides; side++) {
                int nextSide = side + 1 == nbSides ? 0 : side + 1;
                int nextSeg = seg + 1 == nbRadSeg ? 0 : (seg + 1) * nbSides;
                var a = side + seg * nbSides;
                var b = nextSide + seg * nbSides;
                var c = side + nextSeg;
                var d = nextSide + nextSeg;

                int next = side + nextSeg;

                triangles[i++] = a;
                triangles[i++] = c;
                triangles[i++] = b;

                triangles[i++] = b;
                triangles[i++] = c;
                triangles[i++] = d;
            }
        }
        #endregion

        mesh.vertices = vertices;
        // mesh.normals = normales;
        // mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateBounds ();
        mesh.RecalculateNormals ();
        var renderer = gameObject.AddComponent<MeshRenderer> ();
        renderer.material = mat;
        renderer.material.color = Color.white;

        var collider = gameObject.AddComponent<MeshCollider> ();
        //		collider.isTrigger = true;

        var manifold = gameObject.AddComponent<Manifold> ();
        return manifold;
    }
    public static Manifold BuildDisc (Material mat, Color color) {
        GameObject gameObject = new GameObject ("Torus");
        gameObject.transform.position += Vector3.right * 8f;
        gameObject.transform.Rotate (Vector3.right * -90f, Space.World);
        gameObject.tag = "Sphere";
        MeshFilter filter = gameObject.AddComponent<MeshFilter> ();
        Mesh mesh = filter.mesh;
        mesh.Clear ();

        float radius1 = Statics.torusRadius2 / 2f;
        int nbSides = 72;

        #region Vertices		
        Vector3[] vertices = new Vector3[nbSides + 1];
        float _2pi = Mathf.PI * 2f;
        vertices[0] = Vector3.zero;
        for (int side = 0; side < nbSides; side++) {
            float t1 = (float) side / nbSides * _2pi;
            Vector3 r1 = new Vector3 (Mathf.Cos (t1) * radius1, 0f, Mathf.Sin (t1) * radius1);
            vertices[side + 1] = r1;
            // DebugSphere (gameObject.transform.localToWorldMatrix.MultiplyPoint3x4 (r1), Color.magenta, (side + 1) + "");
        }
        #endregion

        #region Triangles
        int nbTriangles = nbSides;
        int[] triangles = new int[(nbTriangles * 3)];
        /*
        a b
        c d
         */
        int i = 0;
        for (int side = 1; side < nbSides; side++) {
            var a = 0;
            var b = side;
            var c = side + 1 == nbSides ? 1 : side + 1;
            triangles[i++] = a;
            triangles[i++] = c;
            triangles[i++] = b;
            // DebugSphere ((vertices[a] + vertices[b] + vertices[c]) / 3, Color.magenta, side + "");
        }
        #endregion

        mesh.vertices = vertices;
        // mesh.normals = normales;
        // mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateBounds ();
        mesh.RecalculateNormals ();
        var renderer = gameObject.AddComponent<MeshRenderer> ();
        renderer.material = mat;
        renderer.material.color = Color.white;

        var collider = gameObject.AddComponent<MeshCollider> ();
        //		collider.isTrigger = true;

        var manifold = gameObject.AddComponent<Manifold> ();
        return manifold;
    }

    public static Manifold BuildHalfCylinder (Material mat, Color color) {
        GameObject gameObject = new GameObject ("Torus");
        gameObject.tag = "Sphere";
        MeshFilter filter = gameObject.AddComponent<MeshFilter> ();
        Mesh mesh = filter.mesh;
        mesh.Clear ();

        float radius1 = Statics.torusRadius1;
        float radius2 = Statics.torusRadius2;
        int nbRadSeg = 48;
        int nbSides = 36;

        #region Vertices		
        var vertices = new List<Vector3> ();
        for (int seg = 0; seg < nbRadSeg; seg++) {

            float theta = (float) seg / nbRadSeg;
            var x = 4 * Mathf.Cos (theta * Mathf.PI + Mathf.PI);
            var y = 4 * Mathf.Sin (theta * Mathf.PI + Mathf.PI);
            for (int side = 0; side < nbSides; side++) {
                var z = (float) side / nbSides;

                Vector3 vector3 = new Vector3 (x, y, z * 10f);
                vertices.Add (vector3);
            }
        }
        #endregion

        #region Triangles
        var triangles = new List<int> ();
        /*
        a b
        c d
         */
        for (int seg = 0; seg < nbRadSeg - 1; seg++) {
            for (int side = 0; side < nbSides - 1; side++) {
                int nextSide = side + 1;
                int nextSeg = (seg + 1) * nbSides;
                var a = side + seg * nbSides;
                var b = nextSide + seg * nbSides;
                var c = side + nextSeg;
                var d = nextSide + nextSeg;

                int next = side + nextSeg;

                triangles.Add (a);
                triangles.Add (c);
                triangles.Add (b);

                triangles.Add (b);
                triangles.Add (c);
                triangles.Add (d);

                triangles.Add (a);
                triangles.Add (b);
                triangles.Add (c);
            }
        }
        #endregion

        mesh.vertices = vertices.ToArray ();
        // mesh.normals = normales;
        // mesh.uv = uvs;
        mesh.triangles = triangles.ToArray ();

        mesh.RecalculateBounds ();
        mesh.RecalculateNormals ();
        var renderer = gameObject.AddComponent<MeshRenderer> ();
        renderer.material = mat;
        renderer.material.color = Color.yellow;

        var collider = gameObject.AddComponent<MeshCollider> ();
        //		collider.isTrigger = true;

        var manifold = gameObject.AddComponent<Manifold> ();
        return manifold;
    }

    public static Manifold BuildTorusOriginal (Material mat, Color c) {
        GameObject gameObject = new GameObject ("Torus");
        gameObject.tag = "Sphere";
        MeshFilter filter = gameObject.AddComponent<MeshFilter> ();
        Mesh mesh = filter.mesh;
        mesh.Clear ();

        float radius1 = Statics.torusRadius1;
        float radius2 = Statics.torusRadius2;
        int nbRadSeg = 48;
        int nbSides = 36;

        #region Vertices		
        Vector3[] vertices = new Vector3[(nbRadSeg + 1) * (nbSides + 1)];
        float _2pi = Mathf.PI * 2f;
        for (int seg = 0; seg <= nbRadSeg; seg++) {
            int currSeg = seg == nbRadSeg ? 0 : seg;

            float t1 = (float) currSeg / nbRadSeg * _2pi;
            Vector3 r1 = new Vector3 (Mathf.Cos (t1) * radius1, 0f, Mathf.Sin (t1) * radius1);

            for (int side = 0; side <= nbSides; side++) {
                int currSide = side == nbSides ? 0 : side;

                Vector3 normale = Vector3.Cross (r1, Vector3.up);
                float t2 = (float) currSide / nbSides * _2pi;
                Vector3 r2 = Quaternion.AngleAxis (-t1 * Mathf.Rad2Deg, Vector3.up) * new Vector3 (Mathf.Sin (t2) * radius2, Mathf.Cos (t2) * radius2);

                vertices[side + seg * (nbSides + 1)] = r1 + r2;
            }
        }
        #endregion

        #region Normales		
        Vector3[] normales = new Vector3[vertices.Length];
        for (int seg = 0; seg <= nbRadSeg; seg++) {
            int currSeg = seg == nbRadSeg ? 0 : seg;

            float t1 = (float) currSeg / nbRadSeg * _2pi;
            Vector3 r1 = new Vector3 (Mathf.Cos (t1) * radius1, 0f, Mathf.Sin (t1) * radius1);

            for (int side = 0; side <= nbSides; side++) {
                normales[side + seg * (nbSides + 1)] = (vertices[side + seg * (nbSides + 1)] - r1).normalized;
            }
        }
        #endregion

        #region UVs
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int seg = 0; seg <= nbRadSeg; seg++)
            for (int side = 0; side <= nbSides; side++)
                uvs[side + seg * (nbSides + 1)] = new Vector2 ((float) seg / nbRadSeg, (float) side / nbSides);
        #endregion

        #region Triangles
        int nbFaces = vertices.Length;
        int nbTriangles = nbFaces * 2;
        int nbIndexes = nbTriangles * 3;
        int[] triangles = new int[nbIndexes];

        int i = 0;
        for (int seg = 0; seg <= nbRadSeg; seg++) {
            for (int side = 0; side <= nbSides - 1; side++) {
                int current = side + seg * (nbSides + 1);
                int next = side + (seg < (nbRadSeg) ? (seg + 1) * (nbSides + 1) : 0);

                if (i < triangles.Length - 6) {
                    triangles[i++] = current;
                    triangles[i++] = next;
                    triangles[i++] = next + 1;

                    triangles[i++] = current;
                    triangles[i++] = next + 1;
                    triangles[i++] = current + 1;
                }
            }
        }
        #endregion

        mesh.vertices = vertices;
        mesh.normals = normales;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateBounds ();
        var renderer = gameObject.AddComponent<MeshRenderer> ();
        renderer.material = mat;
        renderer.material.color = c;

        var collider = gameObject.AddComponent<MeshCollider> ();
        //		collider.isTrigger = true;

        var manifold = gameObject.AddComponent<Manifold> ();
        return manifold;
    }

    public static void flipButtonSprites (string name) {
        UnityEngine.UI.Button button = GameObject.Find (name).GetComponent<UnityEngine.UI.Button> ();
        Color c = new Color ();
        ColorUtility.TryParseHtmlString ("#969696FF", out c);
        Color c2 = new Color ();
        ColorUtility.TryParseHtmlString ("#FFFFFFFF", out c2);

        if (button.image.color.Equals (c)) {
            button.image.color = c2;
        } else {
            button.image.color = c;
        }
        //		UnityEngine.UI.ColorBlock cb = button.colors;
        //		cb.normalColor = c;
        //		cb.highlightedColor = c2;
        //		button.colors = cb;
    }

    public static string DebugList<T> (string name, List<T> list) {
        string s = name + ": ";
        if (list.Count > 0) {
            for (int i = 0; i < list.Count - 1; i++) {
                s += list[i].ToString () + ", ";
            }
            s += list[list.Count - 1].ToString ();
        }
        Debug.Log (s);
        return s;
    }

    public static string DebugDict<T> (string name, Dictionary<T, T> dict) {
        string s = name + ": ";
        foreach (var item in dict.Keys) {
            s += item.ToString () + ": " + dict[item].ToString () + ", ";
        }
        Debug.Log (s);
        return s;
    }

    public static GameObject DebugSphere (Vector3 endPoint, Color color, string name = "DebugSphere") {
        var s = GameObject.CreatePrimitive (PrimitiveType.Sphere);
        s.GetComponent<SphereCollider> ().enabled = false;
        s.name = name;
        s.transform.position = endPoint;
        s.transform.localScale *= 0.05f;
        s.GetComponent<Renderer> ().material.color = color;
        return s;
    }
}