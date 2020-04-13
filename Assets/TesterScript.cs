using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TesterScript : MonoBehaviour {
    // Start is called before the first frame update
    void Start () {
        var poly = new MPolygon ();
        poly.Add (12);
        poly.Add (15);
        poly.Add (28);
        List<MPolygon> newPolys;
        poly.Cut (new PolygonSide (12, 15), 28, out newPolys, 3);
        Misc.DebugList ("Polys: ", newPolys);
    }

    // Update is called once per frame
    void Update () {

    }
}