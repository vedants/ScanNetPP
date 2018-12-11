using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddControl : MonoBehaviour {

    public static float SPAWN_DISTANCE = 1;

    public GameObject boundingBoxPrefab;
    public GameObject boundingBox2DPrefab;

	private void Update () {
		if (InputManager.instance.touchDown && !InputManager.instance.touchDownUI) {
            Vector3 position = Camera.main.transform.position + Camera.main.transform.forward * SPAWN_DISTANCE;
            GameObject boundingBox = Instantiate(boundingBoxPrefab, position, Quaternion.identity);
            boundingBox.name = boundingBoxPrefab.name;
            boundingBox.transform.SetParent(GizmoControl.instance.boundingBoxParent);
            GizmoControl.instance.SetupObj(boundingBox);

            // 2D Bounding Box
            GameObject boundingBox2D = Instantiate(boundingBox2DPrefab);
            boundingBox2D.transform.SetParent(GizmoControl.instance.boundingBox2DParent);
            boundingBox2D.GetComponent<BB2D>().linkedObj = boundingBox;
            boundingBox.GetComponent<BBState>().linked2DBoundingBox = boundingBox2D;

            // Update 2D Bounding Box State
            if (!Switcher2D3D.instance.active2D) {
                boundingBox2D.SetActive(false);
            }
        }
	}
}
