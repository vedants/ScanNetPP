using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour {

    public static InputManager instance = null;

    public RectTransform[] uiRectTransforms;

    public bool touchDown; // True on the frame mouse/finger pressed down
    public bool touchDownUI; // True on the frame mouse/finger pressed down on ui
    public bool touchUp; // True while pressed down
    public bool touching; // True on the frame input was released
    public Vector2 position; // Mouse or finger position

	void Start () {
        if (instance == null) {
            instance = this;
        } else if (instance != this) {
            Destroy(this);
        }

        ResetState();
	}

    /**
     * This function is called after all normal Update() functions have finished.
     * We set touchDownUI to true in GizmoControl when the OnClick event handler.
     * This setup relies on OnClick event handlers happening in a different stage of
     * execution from the Update() and LateUpdate() functions.
     */
    private void LateUpdate() {
        ResetState();
#if UNITY_EDITOR
        MouseUpdate();
#else
        TouchUpdate();
#endif
        if (touchDown) {
            foreach (RectTransform rt in uiRectTransforms) {
                if (BBUtils.IsPointInRT(position, rt)) {
                    touchDownUI = true;
                    break;
                }
            }
        }
    }

    void MouseUpdate() {
        touchDown = Input.GetMouseButtonDown(0);
        touchUp = Input.GetMouseButtonUp(0);
        touching = Input.GetMouseButton(0);
        position = Input.mousePosition;
    }

    void TouchUpdate() {
        if (Input.touchCount > 0) {
            Touch touch = Input.GetTouch(0);
            TouchPhase phase = touch.phase;

            touchDown = phase == TouchPhase.Began;
            touchUp = phase == TouchPhase.Ended;
            touching = phase == TouchPhase.Moved || phase == TouchPhase.Stationary;
            position = touch.position;
        }
    }

    /**
     * Resets states of all input.
     */
    private void ResetState() {
        touchDown = false;
        touchDownUI = false;
        touchUp = false;
        touching = false;
        position = Vector3.zero;
    }
}
