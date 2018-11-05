using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionControl : MonoBehaviour {
    
    public enum Mode {XZ, YZ, XY, X, Y, Z, NONE};

    [System.Serializable]
    private struct ObjectToMode {
        public GameObject obj;
        public Mode mode;
    }

    public static Vector3 XZ_NORMAL = Vector3.up;
    public static Vector3 YZ_NORMAL = Vector3.right;
    public static Vector3 XY_NORMAL = Vector3.forward;
    public static float MAX_DISTANCE = 100;

    public float scaleFactor;
    public Material selectedMat;

    [SerializeField] private ObjectToMode[] objModeMapping;
    private GameObject linkedObj;
    private bool moving = false;

    // Blackboard Variables
    private Mode storedMode;
    private Vector3 storedPosition, storedProjectedPosition;
    private GameObject storedGizmoObj;
    private Material storedMat;

    /**
     * Link a given object to the gizmo.
     */
    public void LinkObject(GameObject obj) {
        linkedObj = obj;
        transform.position = linkedObj.transform.position;
    }

    /**
     * Unlink the currently linked object to the gizmo.
     */
    public void UnlinkObject() {
        linkedObj = null;
    }

	void Start () {
        storedMode = Mode.NONE;
    }
	
	void Update () {
        if (InputManager.instance.touchDown) {
            Ray ray = Camera.main.ScreenPointToRay(InputManager.instance.position);
            RaycastHit[] hits;
            hits = Physics.RaycastAll(ray, Mathf.Infinity, GizmoControl.GIZMO_LAYER_MASK, QueryTriggerInteraction.Collide);
            foreach (RaycastHit hit in hits) {
                Mode mode = FindModeFromObj(hit.collider.gameObject);
                if (mode == Mode.X || mode == Mode.Y || mode == Mode.Z) {
                    storedMode = mode;
                    storedGizmoObj = hit.collider.gameObject;
                    break;
                }
                else if (mode != Mode.NONE) {
                    storedMode = mode;
                    storedGizmoObj = hit.collider.gameObject;
                }
            }

            if (storedMode != Mode.NONE) {
                storedPosition = transform.position;
                GetProjectedPosition(InputManager.instance.position, storedMode, out storedProjectedPosition);
                SelectGizmoObject(storedGizmoObj);
                moving = true;
            }
        } else if (InputManager.instance.touch && moving) {
            Vector3 targetProjectedPosition;
            bool success = GetProjectedPosition(InputManager.instance.position, storedMode, out targetProjectedPosition);
            if (success) {
                Vector3 newPosition = storedPosition + (targetProjectedPosition - storedProjectedPosition);
                if ((newPosition - storedPosition).sqrMagnitude <= MAX_DISTANCE * MAX_DISTANCE) {
                    transform.position = newPosition;
                } else {
                    transform.position = storedPosition + (newPosition - storedPosition).normalized * MAX_DISTANCE;
                }
            }
        } else if (InputManager.instance.touchUp) {
            if (storedGizmoObj != null) {
                storedGizmoObj.GetComponent<Renderer>().material = storedMat;
                storedMode = Mode.NONE;
                DeselectGizmoObject(storedGizmoObj);
                storedGizmoObj = null;
                moving = false;
            }
        }

        if (linkedObj != null) {
            linkedObj.transform.position = transform.position;
        }

        ResizeGizmo();
	}

    /**
     * Check the mapping if the provided GameObject has an associated mode.
     */
    private Mode FindModeFromObj(GameObject obj) {
        foreach (ObjectToMode objMode in objModeMapping) {
            if (objMode.obj == obj) {
                return objMode.mode;
            }
        }
        return Mode.NONE;
    }

    /**
     * Modify the colors of the in-use gizmo component.
     */
    private void SelectGizmoObject(GameObject obj) {
        for (int i = 0; i < obj.transform.parent.childCount; i++) {
            GameObject child = obj.transform.parent.GetChild(i).gameObject;
            storedMat = child.GetComponent<Renderer>().material;
            child.GetComponent<Renderer>().material = selectedMat;
        }
    }

    /**
     * Undo the color modification of the no-longer-in-use gizmo component.
     */
    private void DeselectGizmoObject(GameObject obj) {
        for (int i = 0; i < obj.transform.parent.childCount; i++) {
            GameObject child = obj.transform.parent.GetChild(i).gameObject;
            child.GetComponent<Renderer>().material = storedMat;
        }
        storedMat = null;
    }

    /**
     * Resize the gizmo to something appropriate for the distance.
     */
    private void ResizeGizmo() {
        float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
        transform.localScale = Vector3.one * distance * scaleFactor;
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
                t = Utils.GetLineToPlaneIntersectionParameter(transform.position, XZ_NORMAL, ray.origin, ray.direction);
                break;
            case Mode.YZ:
                t = Utils.GetLineToPlaneIntersectionParameter(transform.position, YZ_NORMAL, ray.origin, ray.direction);
                break;
            case Mode.XY:
                t = Utils.GetLineToPlaneIntersectionParameter(transform.position, XY_NORMAL, ray.origin, ray.direction);
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

        float t = Utils.GetLineToPlaneIntersectionParameter(transform.position, normal, ray.origin, ray.direction);
        if (t > 0) {
            Vector3 projectedPlane = ray.origin + ray.direction * t;
            Vector3 linePlane = Utils.ProjectPointToLine(projectedPlane, transform.position, axis);
            projectedPosition = linePlane;
            return true;
        }

        projectedPosition = Vector3.zero;
        return false;
    }
}
