using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoControl : MonoBehaviour {

    public enum Tool { POSITION, NONE };

    public static string GIZMO_LAYER = "Gizmo";

    public GameObject positionTool;
    public Tool currentTool;

    private int gizmoLayer;
    private GameObject selectedObj;
    private GameObject toolObj;

    private void Start() {
        gizmoLayer = LayerMask.NameToLayer(GIZMO_LAYER);
    }

    private void Update() {
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity)) {
                GameObject obj = hit.collider.gameObject;
                if (obj != selectedObj && obj.layer != gizmoLayer) {
                    print(obj.name);
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
                toolObj.GetComponent<PositionControl>().LinkObject(selectedObj);
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
