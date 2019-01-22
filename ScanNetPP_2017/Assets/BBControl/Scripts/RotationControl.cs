using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationControl : MonoBehaviour {

    public float sensitivity;
    public float scaleFactor;
    public Material selectedMat;

    private GameObject linkedObj;
    private bool rotating = false;
    private GameObject storedGizmoObj;
    private Quaternion startingRotation;
    private Vector3 planeNormal;
    private Vector2 lineOrigin;
    private Vector2 lineDir;
    private Material storedMat;

    /**
     * Link a given object to the gizmo.
     */
    public void LinkObject(GameObject obj) {
        linkedObj = obj;
        ResizeGizmo();
    }

    void Update () {
		if (InputManager.instance.touchDown) {
            Ray ray = Camera.main.ScreenPointToRay(InputManager.instance.position);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, GizmoControl.GIZMO_LAYER_MASK, QueryTriggerInteraction.Collide)) {
                storedGizmoObj = hit.collider.gameObject;
                startingRotation = transform.rotation;

                // Project collison hit point to plane of rotation
                planeNormal = storedGizmoObj.transform.TransformDirection(Vector3.up);
                Vector3 origin3D = BBUtils.GetPointToPlaneClosestPoint(hit.point, transform.position, planeNormal);

                // Compute tangent 3D line
                Vector3 dir3D = Vector3.Cross(origin3D - transform.position, planeNormal).normalized;

                // Compute 2D line by projecting 2 points on the tangent3D line to the plane
                Vector3 origin2D = Camera.main.WorldToScreenPoint(origin3D);
                Vector3 dir2D = (Camera.main.WorldToScreenPoint(origin3D + dir3D) - origin2D).normalized;

                // Strip out the 3rd element to the 2D line
                lineOrigin = origin2D;
                lineDir = dir2D;

                storedMat = BBUtils.ChangeSiblingMaterial(storedGizmoObj, selectedMat);
                rotating = true;
            }
        } else if (InputManager.instance.touching && rotating) {
            // Project touch position to line
            Vector2 pos = BBUtils.ProjectPointToLine(InputManager.instance.position, lineOrigin, lineDir);

            // Compute rotation amount based off of distance from lineOrigin
            float distance = Vector2.Distance(pos, lineOrigin);
            if ((pos.x - lineOrigin.x) / lineDir.x < 0) {
                distance *= -1;
            }
            float angle = distance * -sensitivity;

            // Rotate obj from its starting rotation by theta
            transform.rotation = startingRotation;
            transform.RotateAround(transform.position, planeNormal, angle);
        } else if (InputManager.instance.touchUp && rotating) {
            BBUtils.ChangeSiblingMaterial(storedGizmoObj, storedMat);
            storedGizmoObj = null;
            storedMat = null;
            rotating = false;
        }

        if (linkedObj != null) {
            linkedObj.transform.rotation = transform.rotation;
        }

        ResizeGizmo();
    }

    /**
     * Resize the gizmo to something appropriate for the distance.
     */
    private void ResizeGizmo() {
        float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
        transform.localScale = Vector3.one * distance * scaleFactor;
    }
}
