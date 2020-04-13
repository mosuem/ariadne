using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinearAlgebra {
    private static readonly float epsilon = 0.01f;

    internal static bool isInSegment (Vector3 c, Vector3 a, Vector3 b) {
        var cross = Vector3.Cross (b - a, c - a);
        if (Vector3.Magnitude (cross) > epsilon) {
            return false;
        }
        float dot = Vector3.Dot (b - a, c - a);
        if (dot < 0) {
            return false;
        }
        float dist = Vector3.Distance (a, b);
        var squaredLength = dist * dist;
        if (dot > squaredLength) {
            return false;
        }
        return true;
    }

    private float getDistance (Vector3 p1, Vector3 p2, Vector3 q1, Vector3 q2) {
        var u = p2 - p1;
        var v = q2 - q1;
        var w = p1 - q1;
        var d1d2 = Vector3.Cross (u, v);
        return Mathf.Abs (Vector3.Dot (d1d2 / Vector3.Magnitude (d1d2), w));
    }

    internal static Vector3 getClosest (Vector3 p0, Vector3 p1, Vector3 q0, Vector3 q1, out float distance) {
        var u = p1 - p0;
        var v = q1 - q0;
        var w = p0 - q0;
        var a = Vector3.Dot (u, u); // always >= 0
        var b = Vector3.Dot (u, v);
        var c = Vector3.Dot (v, v); // always >= 0
        var d = Vector3.Dot (u, w);
        var e = Vector3.Dot (v, w);
        var D = a * c - b * b; // always >= 0
        float sc, sN, sD = D; // sc = sN / sD, default sD = D >= 0
        float tc, tN, tD = D; // tc = tN / tD, default tD = D >= 0

        // compute the line parameters of the two closest points
        const float epsilon = 0.0000001f;
        if (D < epsilon) { // the lines are almost parallel
            Debug.LogWarning ("Lines are parallel");
            sN = 0f; // force using point P0 on segment S1
            sD = 1f; // to prevent possible division by 0.0 later
            tN = e;
            tD = c;
        } else { // get the closest points on the infinite lines
            sN = (b * e - c * d);
            tN = (a * e - b * d);
            if (sN < 0.0) { // sc < 0 => the s=0 edge is visible
                sN = 0f;
                tN = e;
                tD = c;
            } else if (sN > sD) { // sc > 1  => the s=1 edge is visible
                sN = sD;
                tN = e + b;
                tD = c;
            }
        }

        if (tN < 0.0) { // tc < 0 => the t=0 edge is visible
            tN = 0f;
            // recompute sc for this edge
            if (-d < 0.0)
                sN = 0f;
            else if (-d > a)
                sN = sD;
            else {
                sN = -d;
                sD = a;
            }
        } else if (tN > tD) { // tc > 1  => the t=1 edge is visible
            tN = tD;
            // recompute sc for this edge
            if ((-d + b) < 0.0)
                sN = 0;
            else if ((-d + b) > a)
                sN = sD;
            else {
                sN = (-d + b);
                sD = a;
            }
        }
        // finally do the division to get sc and tc
        sc = (Mathf.Abs (sN) < epsilon ? 0f : sN / sD);
        tc = (Mathf.Abs (tN) < epsilon ? 0f : tN / tD);

        // get the difference of the two closest points
        Vector3 dP = w + (sc * u) - (tc * v); // =  S1(sc) - S2(tc)

        // return norm (dP); // return the closest distance
        if (tc > 0.99f || tc < 0.01f) {
            Debug.LogWarning ("Closest Point at end of interval, as tc = " + tc);
        }
        // Debug.LogWarning ("Distance is " + Vector3.Distance (p0 + sc * u, q0 + tc * v) + " but nearest Distance is " + getDistance (p0, p1, q0, q1) + " or " + Vector3.Magnitude (dP));

        // Vector3 dP = w + (sc * u) - (tc * v); // =  L1(sc) - L2(tc)
        distance = Vector3.Magnitude (dP);
        // Misc.DebugSphere (p0 + sc * u, Color.white, "Other point, tn = " + tN + " td = " + tD + " D = " + D);
        return q0 + tc * v;
    }

    public static float distanceToSegment (Vector3 v, Vector3 a, Vector3 b) {
        Vector3 ab = b - a;
        Vector3 av = v - a;

        if (Vector3.Dot (av, ab) <= 0f) // Point is lagging behind start of the segment, so perpendicular distance is not viable.
            return av.magnitude; // Use distance to start of segment instead.

        Vector3 bv = v - b;

        if (Vector3.Dot (bv, ab) >= 0.0) // Point is advanced past the end of the segment, so perpendicular distance is not viable.
            return bv.magnitude; // Use distance to end of the segment instead.

        return Vector3.Cross (ab, av).magnitude / ab.magnitude; // Perpendicular distance of point to segment.
    }

    internal static bool IsNear (Vector3 intersection, Vector3 vector3) {
        if (Vector3.Distance (intersection, vector3) < epsilon) {
            return true;
        }
        return false;
    }
}