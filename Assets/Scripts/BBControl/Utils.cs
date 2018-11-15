using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Mode { XZ, YZ, XY, X, Y, Z, NONE };

[System.Serializable]
public struct ObjectToMode {
    public GameObject obj;
    public Mode mode;
}

public class Utils : MonoBehaviour {

    public static Vector3 XZ_NORMAL = Vector3.up;
    public static Vector3 YZ_NORMAL = Vector3.right;
    public static Vector3 XY_NORMAL = Vector3.forward;

    /**
     * Check the mapping if the provided GameObject has an associated mode.
     */
    public static Mode FindModeFromObj(ObjectToMode[] objModeMapping, GameObject obj) {
        foreach (ObjectToMode objMode in objModeMapping) {
            if (objMode.obj == obj) {
                return objMode.mode;
            }
        }
        return Mode.NONE;
    }

    /**
     * Given a screen position on a plane or axis, return true iff a
     * 3D intersection point can be found between the projected ray
     * from the camera to the screen position and the plane/axis.
     * If successful, puts the output position in targetPosition.
     */
    public static bool GetProjectedPosition(Vector3 screenPosition, Vector3 origin, Mode mode, out Vector3 projectedPosition) {
        if (mode == Mode.X || mode == Mode.Y || mode == Mode.Z) {
            return GetAxisProjection(screenPosition, origin, mode, out projectedPosition);
        }
        else {
            return GetPlaneProjection(screenPosition, origin, mode, out projectedPosition);
        }
    }

    /**
     * GetProjectedPosition, but restricted to the plane case.
     */
    public static bool GetPlaneProjection(Vector3 screenPosition, Vector3 origin, Mode mode, out Vector3 projectedPosition) {
        Camera cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(screenPosition);

        float t = -1;
        switch (mode) {
            case Mode.XZ:
                t = GetLineToPlaneIntersectionParameter(origin, XZ_NORMAL, ray.origin, ray.direction);
                break;
            case Mode.YZ:
                t = GetLineToPlaneIntersectionParameter(origin, YZ_NORMAL, ray.origin, ray.direction);
                break;
            case Mode.XY:
                t = GetLineToPlaneIntersectionParameter(origin, XY_NORMAL, ray.origin, ray.direction);
                break;
            default:
                Debug.Log("This should never happen. Invalid mode detected: " + mode);
                break;
        }

        if (t <= 0) {
            projectedPosition = Vector3.zero;
            return false;
        } else {
            projectedPosition = ray.origin + ray.direction * t;
            return true;
        }
    }

    /**
     * GetProjectedPosition, but restricted to the axis case.
     */
    public static bool GetAxisProjection(Vector3 screenPosition, Vector3 origin, Mode mode, out Vector3 projectedPosition) {
        Camera cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(screenPosition);
        Vector3 normal, axis;
        switch (mode) {
            case Mode.X:
                normal = XZ_NORMAL;
                axis = Vector3.right;
                break;
            case Mode.Y:
                normal = XY_NORMAL;
                axis = Vector3.up;
                break;
            case Mode.Z:
                normal = XZ_NORMAL;
                axis = Vector3.forward;
                break;
            default:
                Debug.Log("This should never happen. Invalid mode detected: " + mode);
                projectedPosition = Vector3.zero;
                return false;
        }

        float t = GetLineToPlaneIntersectionParameter(origin, normal, ray.origin, ray.direction);
        if (t > 0) {
            Vector3 projectedPlane = ray.origin + ray.direction * t;
            Vector3 linePlane = ProjectPointToLine(projectedPlane, origin, axis);
            projectedPosition = linePlane;
            return true;
        }

        projectedPosition = Vector3.zero;
        return false;
    }

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
