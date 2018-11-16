using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddControl : MonoBehaviour {

    public static float SPAWN_DISTANCE = 1;

    public GameObject boundingBoxPrefab;

	private void Update () {
		if (InputManager.instance.touchDown && !InputManager.instance.touchDownUI) {
            Vector3 position = Camera.main.transform.position + Camera.main.transform.forward * SPAWN_DISTANCE;
            GameObject boundingBox = Instantiate(boundingBoxPrefab, position, Quaternion.identity);
        }
	}
}
