using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoControl : MonoBehaviour {

    public enum Tool { POSITION, ROTATION, SCALE, NONE };

    public static string GIZMO_LAYER_NAME = "Gizmo";
    public static int GIZMO_LAYER;
    public static int GIZMO_LAYER_MASK;
    public static float MAX_DISTANCE = 100;

    public GameObject positionTool;
    public GameObject rotationTool;
    public GameObject scaleTool;
    public Tool currentTool;

    private int gizmoLayer;
    private GameObject selectedObj;
    private GameObject toolObj;

    private void Start() {
        GIZMO_LAYER = LayerMask.NameToLayer(GIZMO_LAYER_NAME);
        GIZMO_LAYER_MASK = LayerMask.GetMask(GIZMO_LAYER_NAME);
    }

    private void Update() {
        if (InputManager.instance.touchDown) {
            Ray ray = Camera.main.ScreenPointToRay(InputManager.instance.position);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity)) {
                GameObject obj = hit.collider.gameObject;
                if (obj != selectedObj && obj.layer != GIZMO_LAYER) {
                    SetupToolOnObj(obj, currentTool);
                }
            } else {
                if (selectedObj != null) {
                    CleanupToolOnObj(selectedObj, currentTool);
                }
            }
        }
    }

    /**
     * Apply the given tool to the given GameObject.
     */
    private void SetupToolOnObj(GameObject obj, Tool tool) {
        selectedObj = obj;
        switch (tool) {
            case Tool.POSITION:
                toolObj = Instantiate(positionTool, obj.transform.position, Quaternion.identity);
                PositionControl positionControl = toolObj.GetComponent<PositionControl>();
                positionControl.LinkObject(selectedObj);
                break;
            case Tool.ROTATION:
                toolObj = Instantiate(rotationTool, obj.transform.position, obj.transform.rotation);
                RotationControl rotationControl = toolObj.GetComponent<RotationControl>();
                rotationControl.LinkObject(selectedObj);
                break;
            case Tool.SCALE:
                toolObj = Instantiate(scaleTool, obj.transform.position, obj.transform.rotation);
                ScaleControl scaleControl = toolObj.GetComponent<ScaleControl>();
                scaleControl.LinkObject(selectedObj);
                break;
        }
    }

    /**
     * Cleanupp and remove the given tool from the given GameObject.
     */
    private void CleanupToolOnObj(GameObject obj, Tool tool) {
        switch (tool) {
            case Tool.POSITION:
                toolObj.GetComponent<PositionControl>().UnlinkObject();
                break;
        }
        Destroy(toolObj);
        selectedObj = null;
    }
}
