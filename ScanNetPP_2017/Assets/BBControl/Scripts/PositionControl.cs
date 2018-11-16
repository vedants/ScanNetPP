using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionControl : MonoBehaviour {

    public static Vector3 XZ_NORMAL = Vector3.up;
    public static Vector3 YZ_NORMAL = Vector3.right;
    public static Vector3 XY_NORMAL = Vector3.forward;

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
        ResizeGizmo();
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
                Mode mode = Utils.FindModeFromObj(objModeMapping, hit.collider.gameObject);
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
                Utils.GetProjectedPosition(InputManager.instance.position, transform.position, storedMode, out storedProjectedPosition);
                storedMat = Utils.ChangeSiblingMaterial(storedGizmoObj, selectedMat);
                moving = true;
            }
        } else if (InputManager.instance.touching && moving) {
            Vector3 targetProjectedPosition;
            bool success = Utils.GetProjectedPosition(InputManager.instance.position, transform.position, storedMode, out targetProjectedPosition);
            if (success) {
                Vector3 newPosition = storedPosition + (targetProjectedPosition - storedProjectedPosition);
                if ((newPosition - storedPosition).sqrMagnitude <= GizmoControl.MAX_DISTANCE * GizmoControl.MAX_DISTANCE) {
                    transform.position = newPosition;
                } else {
                    transform.position = storedPosition + (newPosition - storedPosition).normalized * GizmoControl.MAX_DISTANCE;
                }
            }
        } else if (InputManager.instance.touchUp && moving) {
            storedMode = Mode.NONE;
            Utils.ChangeSiblingMaterial(storedGizmoObj, storedMat);
            storedGizmoObj = null;
            moving = false;
        }

        if (linkedObj != null) {
            linkedObj.transform.position = transform.position;
        }

        ResizeGizmo();
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
}
