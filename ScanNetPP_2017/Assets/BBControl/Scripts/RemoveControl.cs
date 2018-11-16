using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveControl : MonoBehaviour {
	
	void Update () {
        if (InputManager.instance.touchDown && !InputManager.instance.touchDownUI) {
            Ray ray = Camera.main.ScreenPointToRay(InputManager.instance.position);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, GizmoControl.GIZMO_LAYER_MASK, QueryTriggerInteraction.Collide)) {
                GameObject obj = hit.collider.gameObject;
                if (obj.CompareTag(GizmoControl.BOUNDING_BOX_TAG)) {
                    print(obj.name);
                    Destroy(obj);
                    GizmoControl.instance.selectedObj = null;
                }
            }
        }
    }
}
