using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils : MonoBehaviour {

    /**
     * Given a point and line defined by its origin and direction,
     * output the location on the line closest to the point.
     */
    public static Vector3 ProjectPointToLine(Vector3 point, Vector3 lineOrigin, Vector3 lineDirection) {
        float t = Vector3.Dot(lineDirection, point - lineOrigin);
        return lineOrigin + t * lineDirection;
    }

    /**
     * Given a 2D point and 2D line defined by its origin and direction,
     * output the location on the line closest to the point.
     */
    public static Vector2 ProjectPointToLine(Vector2 point, Vector2 lineOrigin, Vector2 lineDirection) {
        float t = Vector2.Dot(lineDirection, point - lineOrigin);
        return lineOrigin + t * lineDirection;
    }

    /**
     * Given a point and plane,
     * output the location on the plane closest to the point.
     */
    public static Vector3 GetPointToPlaneClosestPoint(Vector3 point, Vector3 planeOrigin, Vector3 planeNormal) {
        float t = Vector3.Dot(planeNormal, point - planeOrigin);
        return point + planeNormal * t;
    }

    /**
     * Given a line described by A and a line described by B, output "t",
     * the parameterized location on line A that is closest to line B.
     */
    public static float GetLineToLineClosestPointParameter(Vector3 pA, Vector3 dirA, Vector3 pB, Vector3 dirB) {
        float b = Vector3.Dot(dirA, dirB);
        float c = Vector3.Dot(dirA, pB - pA);
        float f = Vector3.Dot(dirB, pB - pA);
        float denom = 1 - b * b;
        if (Mathf.Abs(denom) > 0.0001) {
            return c - b * f / denom;
        }
        else {
            print("Reached");
            return 0;
        }
    }

    /**
     * Given mathematical descriptions of a plane and ray, output "t",
     * the parameterized location on the ray where the intersection occurs.
     * Return -1 if no intersection is found (to some epsilon of error).
     * https://www.scratchapixel.com/lessons/3d-basic-rendering/minimal-ray-tracer-rendering-simple-shapes/ray-plane-and-ray-disk-intersection
     */
    public static float GetLineToPlaneIntersectionParameter(Vector3 planeOrigin, Vector3 planeNormal, Vector3 rayOrigin, Vector3 rayDirection) {
        float denom = Vector3.Dot(planeNormal, rayDirection);
        if (Mathf.Abs(denom) > 0.0001) {
            return Vector3.Dot(planeOrigin - rayOrigin, planeNormal) / denom;
        } else {
            return -1;
        }
    }

    /**
     * Modify the colors of all siblings of an object (including itself).
     * Returns one of the original materials, or null if no renderers are found.
     */
    public static Material ChangeSiblingMaterial(GameObject obj, Material mat) {
        Material storedMat = null;
        for (int i = 0; i < obj.transform.parent.childCount; i++) {
            GameObject child = obj.transform.parent.GetChild(i).gameObject;
            if (child.GetComponent<Renderer>() != null) {
                storedMat = child.GetComponent<Renderer>().material;
                child.GetComponent<Renderer>().material = mat;
            }
        }
        return storedMat;
    }
}
