using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationControl : MonoBehaviour {

    public float sensitivity = 1;

    private bool rotating = false;
    private Quaternion startingRotation;
    private Vector3 planeNormal;
    private Vector2 lineOrigin;
    private Vector2 lineDir;

	void Update () {
		if (InputManager.instance.touchDown) {
            Ray ray = Camera.main.ScreenPointToRay(InputManager.instance.position);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, GizmoControl.GIZMO_LAYER_MASK, QueryTriggerInteraction.Collide)) {
                GameObject obj = hit.collider.gameObject;
                startingRotation = transform.rotation;
                print(obj.name);

                // Project collison hit point to plane of rotation
                planeNormal = obj.transform.TransformDirection(Vector3.up);
                Vector3 origin3D = Utils.GetPointToPlaneClosestPoint(hit.point, transform.position, planeNormal);

                // Compute tangent 3D line
                Vector3 dir3D = Vector3.Cross(origin3D - transform.position, planeNormal).normalized;

                // Compute 2D line by projecting 2 points on the tangent3D line to the plane
                Vector3 origin2D = Camera.main.WorldToScreenPoint(origin3D);
                Vector3 dir2D = (Camera.main.WorldToScreenPoint(origin3D + dir3D) - origin2D).normalized;

                // Strip out the 3rd element to the 2D line
                lineOrigin = origin2D;
                lineDir = dir2D;

                rotating = true;
            }
        } else if (InputManager.instance.touch && rotating) {
            // Project touch position to line
            Vector2 pos = Utils.ProjectPointToLine(InputManager.instance.position, lineOrigin, lineDir);

            // Compute rotation amount based off of distance from lineOrigin
            float distance = Vector2.Distance(pos, lineOrigin);
            if ((pos.x - lineOrigin.x) / lineDir.x < 0) {
                distance *= -1;
            }
            float angle = distance * -sensitivity;

            // Rotate obj from its starting rotation by theta
            transform.rotation = startingRotation;
            transform.RotateAround(transform.position, planeNormal, angle);
        } else if (InputManager.instance.touchUp) {
            rotating = false;
        }
	}
}
