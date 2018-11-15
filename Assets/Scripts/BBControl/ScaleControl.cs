using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleControl : MonoBehaviour {

    public float scaleFactor;

    [SerializeField] private ObjectToMode[] objModeMapping;
    private GameObject linkedObj;
    private Mode storedMode;
    private bool scaling;

    /**
     * Link a given object to the gizmo.
     */
    public void LinkObject(GameObject obj) {
        linkedObj = obj;
        transform.rotation = linkedObj.transform.rotation;

        // Move cubes to bounding box edges
        int x = 1, y = 1, z = 1;
        foreach (ObjectToMode objToMode in objModeMapping) {
            float extent;
            switch (objToMode.mode) {
                case Mode.X:
                    extent = linkedObj.transform.localScale.x / 2;
                    objToMode.obj.transform.position = transform.position;
                    objToMode.obj.transform.Translate(Vector3.right * extent * x);
                    x *= -1;
                    break;
                case Mode.Y:
                    extent = linkedObj.transform.localScale.y / 2;
                    objToMode.obj.transform.position = transform.position;
                    objToMode.obj.transform.Translate(Vector3.up * extent * y);
                    y *= -1;
                    break;
                case Mode.Z:
                    extent = linkedObj.transform.localScale.z / 2;
                    objToMode.obj.transform.position = transform.position;
                    objToMode.obj.transform.Translate(Vector3.forward * extent * z);
                    z *= -1;
                    break;
            }
        }
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

        } else if (InputManager.instance.touch && scaling) {

        } else if (InputManager.instance.touchUp && scaling) {

        }
    }
}
