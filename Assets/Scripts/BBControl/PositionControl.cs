using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionControl : MonoBehaviour {
    
    public enum Mode {XZ, YZ, XY, X, Y, Z, NONE};

    [System.Serializable]
    public struct ObjectToMode {
        public GameObject obj;
        public Mode mode;
    }

    public static Vector3 XZ_NORMAL = Vector3.up;
    public static Vector3 YZ_NORMAL = Vector3.right;
    public static Vector3 XY_NORMAL = Vector3.forward;
    public static string GIMBLE_LAYER = "Gimble";
    public static float MAX_DISTANCE = 100;

    public ObjectToMode[] objModeMapping;

    private int layer;
    private bool movingGimble = false;
    private Mode storedMode;
    private Vector3 storedPosition, storedProjectedPosition;

	void Start () {
		layer = LayerMask.GetMask(GIMBLE_LAYER);
    }
	
	void Update () {
		if (Input.GetMouseButtonDown(0)) {
            Camera cam = Camera.main;
            // Run raycast against gimble elements.
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f, layer, QueryTriggerInteraction.Collide)) {
                storedMode = FindMode(hit.collider.gameObject);
                if (storedMode != Mode.NONE) {
                    storedPosition = transform.position;
                    movingGimble = GetProjectedPosition(Input.mousePosition, storedMode, out storedProjectedPosition);
                }
            }
        } else if (Input.GetMouseButton(0) && movingGimble) {
            Vector3 targetProjectedPosition;
            bool success = GetProjectedPosition(Input.mousePosition, storedMode, out targetProjectedPosition);
            if (success) {
                Vector3 newPosition = storedPosition + (targetProjectedPosition - storedProjectedPosition);
                if ((newPosition - storedPosition).sqrMagnitude <= MAX_DISTANCE * MAX_DISTANCE) {
                    transform.position = newPosition;
                } else {
                    transform.position = storedPosition + (newPosition - storedPosition).normalized * MAX_DISTANCE;
                }
            }
        } else if (Input.GetMouseButtonUp(0)) {
            movingGimble = false;
        }
	}

    /**
     * Check the mapping if the provided GameObject has an associated mode.
     */
    private Mode FindMode(GameObject obj) {
        foreach (ObjectToMode objMode in objModeMapping) {
            if (objMode.obj == obj) {
                return objMode.mode;
            }
        }
        return Mode.NONE;
    }

    /**
     * Given a screen position a plane or axis, return true iff a
     * 3D intersection point can be found between the projected ray
     * from the camera to the screen position and the plane/axis.
     * If successful, puts the output position in targetPosition.
     */
    private bool GetProjectedPosition(Vector3 screenPosition, Mode mode, out Vector3 projectedPosition) {
        if (mode == Mode.X || mode == Mode.Y || mode == Mode.Z) {
            return GetAxisProjection(screenPosition, mode, out projectedPosition);
        } else {
            return GetPlaneProjection(screenPosition, mode, out projectedPosition);
        }
    }

    /**
     * GetProjectedPosition, but restricted to the plane case.
     */
    private bool GetPlaneProjection(Vector3 screenPosition, Mode mode, out Vector3 projectedPosition) {
        Camera cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(screenPosition);

        float t = -1;
        switch (mode) {
            case Mode.XZ:
                t = GetRayPlaneIntersection(transform.position, XZ_NORMAL, ray.origin, ray.direction);
                break;
            case Mode.YZ:
                t = GetRayPlaneIntersection(transform.position, YZ_NORMAL, ray.origin, ray.direction);
                break;
            case Mode.XY:
                t = GetRayPlaneIntersection(transform.position, XY_NORMAL, ray.origin, ray.direction);
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
    private bool GetAxisProjection(Vector3 screenPosition, Mode mode, out Vector3 projectedPosition) {
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

        float t = GetRayPlaneIntersection(transform.position, normal, ray.origin, ray.direction);
        if (t > 0) {
            Vector3 projectedPlane = ray.origin + ray.direction * t;
            Vector3 linePlane = GetPointLineIntersection(projectedPlane, transform.position, axis);
            projectedPosition = linePlane;
            return true;
        }

        projectedPosition = Vector3.zero;
        return false;
    }

    /**
     * Given mathematical descriptions of a plane and ray, output "t",
     * the parameterized location on the ray where the intersection occurs.
     * Return -1 if no intersection is found (to some epsilon of error).
     * https://www.scratchapixel.com/lessons/3d-basic-rendering/minimal-ray-tracer-rendering-simple-shapes/ray-plane-and-ray-disk-intersection
     */
    private float GetRayPlaneIntersection(Vector3 planeOrigin, Vector3 planeNormal, Vector3 rayOrigin, Vector3 rayDirection) {
        float denom = Vector3.Dot(planeNormal, rayDirection);
        if (Mathf.Abs(denom) > 0.0001) {
            return Vector3.Dot(planeOrigin - rayOrigin, planeNormal) / denom;
        } else {
            return -1;
        }
    }

    /**
     * Given a line described by A and a ray described by B, output "t",
     * the parameterized location on the ray that is closest to the line.
     */
    private float GetRayLineIntersection(Vector3 pA, Vector3 dirA, Vector3 pB, Vector3 dirB) {
        float b = Vector3.Dot(dirA, dirB);
        float c = Vector3.Dot(dirA, pB - pA);
        float f = Vector3.Dot(dirB, pB - pA);
        float denom = 1 - b * b;
        if (Mathf.Abs(denom) > 0.0001) {
            return c - b * f / denom;
        } else {
            print("Reached");
            return 0;
        }
    }

    /**
     * Given a line and point, output "t",
     * the location on the line closest to the point.
     */
    private Vector3 GetPointLineIntersection(Vector3 point, Vector3 lineOrigin, Vector3 lineDirection) {
        float t = Vector3.Dot(lineDirection, point - lineOrigin);
        return lineOrigin + t * lineDirection;
    }
}
