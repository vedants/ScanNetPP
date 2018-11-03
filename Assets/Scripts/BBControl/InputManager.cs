using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour {

    public static InputManager instance = null;

    public bool touchDown; // True on the frame input pressed down
    public bool touchUp; // True while pressed down
    public bool touch; // True on the frame input was released

	// Use this for initialization
	void Start () {
        if (instance == null) {
            instance = this;
        } else if (instance != this) {
            Destroy(this);
        }

        touchDown = false;
        touchUp = false;
        touch = false;
	}
	
	// Update is called once per frame
	void Update () {
        touchDown = Input.GetMouseButtonDown(0);
        touchUp = Input.GetMouseButtonUp(0);
        touch = Input.GetMouseButton(0);
	}
}
