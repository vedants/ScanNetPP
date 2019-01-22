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
                storedMode = BBUtils.FindModeFromObj(objModeMapping, hit.collider.gameObject);
                BBUtils.GetProjectedPosition(InputManager.instance.position, transform.position, storedMode, out storedProjectedPosition, transform);
                storedMat = storedGizmoObj.GetComponent<Renderer>().material;
                storedGizmoObj.GetComponent<Renderer>().material = selectedMat;
                scaling = true;
            }
        } else if (InputManager.instance.touching && scaling) {
            Vector3 targetProjectedPosition;
            bool success = BBUtils.GetProjectedPosition(InputManager.instance.position, transform.position, storedMode, out targetProjectedPosition, transform);
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

    private void ResizeGizmo() {
        float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
        Vector3 scale = Vector3.one * distance * scaleFactor;
        foreach (ObjectToMode objectToMode in objModeMapping) {
            objectToMode.obj.transform.localScale = scale;
        }
    }
}
