using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour {

    public static InputManager instance = null;

    public bool touchDown; // True on the frame mouse/finger pressed down
    public bool touchDownUI; // True on the frame mouse/finger pressed down on ui
    public bool touchUp; // True while pressed down
    public bool touching; // True on the frame input was released
    public Vector2 position; // Mouse or finger position

	// Use this for initialization
	void Start () {
        if (instance == null) {
            instance = this;
        } else if (instance != this) {
            Destroy(this);
        }

        ResetState();
	}
	
	// Update is called once per frame
	void Update () {
#if UNITY_EDITOR
        MouseUpdate();
#else
        TouchUpdate();
#endif
    }

    void MouseUpdate() {
        touchDown = Input.GetMouseButtonDown(0);
        touchDownUI = touchDown && EventSystem.current.IsPointerOverGameObject();
        touchUp = Input.GetMouseButtonUp(0);
        touching = Input.GetMouseButton(0);
        position = Input.mousePosition;
    }

    void TouchUpdate() {
        ResetState();
        if (Input.touchCount > 0) {
            Touch touch = Input.GetTouch(0);
            TouchPhase phase = touch.phase;

            touchDown = phase == TouchPhase.Began;
            touchDownUI = touchDown && EventSystem.current.IsPointerOverGameObject(touch.fingerId);
            touchUp = phase == TouchPhase.Ended;
            touching = phase == TouchPhase.Moved || phase == TouchPhase.Stationary;
            position = touch.position;
        }
    }

    private void ResetState() {
        touchDown = false;
        touchDownUI = false;
        touchUp = false;
        touching = false;
        position = Vector3.zero;
    }
}
