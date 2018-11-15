using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleControl : MonoBehaviour {

    public float scaleFactor;
    public Material selectedMat;

    [SerializeField] private ObjectToMode[] objModeMapping;
    private GameObject linkedObj;
    private Mode storedMode;
    private bool scaling;
    private GameObject storedGizmoObj;
    private Vector3 storedPosition, storedInversePosition, storedProjectedPosition;
    private Material storedMat;

    /**
     * Link a given object to the gizmo.
     */
    public void LinkObject(GameObject obj) {
        linkedObj = obj;
        transform.rotation = linkedObj.transform.rotation;
        SetGizmoLocations();
    }

    /**
     * Unlink the currently linked object to the gizmo.
     */
    public void UnlinkObject() {
        linkedObj = null;
    }

    void Start() {
        storedMode = Mode.NONE;
    }

    private void Update() {
        if (InputManager.instance.touchDown) {
            Ray ray = Camera.main.ScreenPointToRay(InputManager.instance.position);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, GizmoControl.GIZMO_LAYER_MASK, QueryTriggerInteraction.Collide)) {
                storedGizmoObj = hit.collider.gameObject;
                storedPosition = storedGizmoObj.transform.position;
                storedInversePosition = transform.position - (storedPosition - transform.position);
                storedMode = Utils.FindModeFromObj(objModeMapping, hit.collider.gameObject);
                GetAxisProjection(out storedProjectedPosition);
                storedMat = storedGizmoObj.GetComponent<Renderer>().material;
                storedGizmoObj.GetComponent<Renderer>().material = selectedMat;
                scaling = true;
            }
        } else if (InputManager.instance.touch && scaling) {
            Vector3 targetProjectedPosition;
            bool success = GetAxisProjection(out targetProjectedPosition);
            if (success) {
                Vector3 newPosition = storedPosition + (targetProjectedPosition - storedProjectedPosition);
                if ((newPosition - storedPosition).sqrMagnitude > GizmoControl.MAX_DISTANCE * GizmoControl.MAX_DISTANCE) {
                    newPosition = storedPosition + (newPosition - storedPosition).normalized * GizmoControl.MAX_DISTANCE;
                }

                transform.position = (newPosition + storedInversePosition) / 2;
                linkedObj.transform.position = transform.position;
                float extent = Vector3.Distance(newPosition, storedInversePosition);
                Vector3 currentScale = linkedObj.transform.localScale;
                switch (storedMode) {
                    case Mode.X:
                        linkedObj.transform.localScale = new Vector3(extent, currentScale.y, currentScale.z);
                        break;
                    case Mode.Y:
                        linkedObj.transform.localScale = new Vector3(currentScale.x, extent, currentScale.z);
                        break;
                    case Mode.Z:
                        linkedObj.transform.localScale = new Vector3(currentScale.x, currentScale.y, extent);
                        break;
                }

                SetGizmoLocations();
            }
        } else if (InputManager.instance.touchUp && scaling) {
            storedMode = Mode.NONE;
            storedGizmoObj.GetComponent<Renderer>().material = storedMat;
            storedGizmoObj = null;
            storedMat = null;
            scaling = false;
        }

        ResizeGizmo();
    }

    /**
     * Moves each of the gizmo boxes to the edges of the bounding box.
     */
    private void SetGizmoLocations() {
        int x = 1, y = 1, z = 1;
        foreach (ObjectToMode objToMode in objModeMapping) {
            float extent;
            switch (objToMode.mode) {
                case Mode.X:
                    extent = linkedObj.transform.localScale.x;
                    objToMode.obj.transform.position = transform.position;
                    objToMode.obj.transform.Translate(Vector3.right * extent * x / 2);
                    x *= -1;
                    break;
                case Mode.Y:
                    extent = linkedObj.transform.localScale.y;
                    objToMode.obj.transform.position = transform.position;
                    objToMode.obj.transform.Translate(Vector3.up * extent * y / 2);
                    y *= -1;
                    break;
                case Mode.Z:
                    extent = linkedObj.transform.localScale.z;
                    objToMode.obj.transform.position = transform.position;
                    objToMode.obj.transform.Translate(Vector3.forward * extent * z / 2);
                    z *= -1;
                    break;
            }
        }
    }

    /**
     * Gets the axis projection for the stored variables (mode, position, etc.)
     * and puts it into projectedPosition. Returns true iff successful.
     */
    private bool GetAxisProjection(out Vector3 projectedPosition) {
        Vector3 normal, axis = Vector3.zero;
        switch (storedMode) {
            case Mode.X:
                normal = transform.TransformDirection(Vector3.up);
                axis = transform.TransformDirection(Vector3.right);
                break;
            case Mode.Y:
                normal = transform.TransformDirection(Vector3.right);
                axis = transform.TransformDirection(Vector3.up);
                break;
            case Mode.Z:
                normal = transform.TransformDirection(Vector3.right);
                axis = transform.TransformDirection(Vector3.forward);
                break;
            default:
                projectedPosition = Vector3.zero;
                return false;
        }

        return Utils.PerformAxisProjection(
                InputManager.instance.position,
                transform.position,
                normal,
                axis,
                out projectedPosition);
    }

    private void ResizeGizmo() {
        float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
        Vector3 scale = Vector3.one * distance * scaleFactor;
        foreach (ObjectToMode objectToMode in objModeMapping) {
            objectToMode.obj.transform.localScale = scale;
        }
    }
}
