using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour {

    public static InputManager instance = null;

    public bool touchDown; // True on the frame mouse/finger pressed down
    public bool touchDownUI; // True on the frame mouse/finger pressed down on ui
    public bool touchUp; // True while pressed down
    public bool touch; // True on the frame input was released
    public Vector2 position; // Mouse or finger position

	// Use this for initialization
	void Start () {
        if (instance == null) {
            instance = this;
        } else if (instance != this) {
            Destroy(this);
        }

        touchDown = false;
        touchDownUI = false;
        touchUp = false;
        touch = false;
        position = Vector3.zero;
	}
	
	// Update is called once per frame
	void Update () {
        touchDown = Input.GetMouseButtonDown(0);
        touchDownUI = touchDown && EventSystem.current.IsPointerOverGameObject();
        touchUp = Input.GetMouseButtonUp(0);
        touch = Input.GetMouseButton(0);
        position = Input.mousePosition;
	}
}
