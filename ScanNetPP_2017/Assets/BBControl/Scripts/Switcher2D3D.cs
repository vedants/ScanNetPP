using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Switcher2D3D : MonoBehaviour {

    public static Switcher2D3D instance;

    public bool active2D = false;
    public Color colorActive2D;
    public Color colorInactive2D;
    public Button button;
    public Transform parent;

    private void Start() {
        if (instance == null) {
            instance = this;
        } else if (instance != this) {
            Destroy(this);
        }

        GizmoControl.instance.SetColor(button, colorInactive2D);
    }

    public void OnClickSwitcher() {
        active2D = !active2D;
        if (active2D) {
            Activate2D();
            GizmoControl.instance.SetColor(button, colorActive2D);
        } else {
            Deactivate2D();
            GizmoControl.instance.SetColor(button, colorInactive2D);
        }
    }

    private void Activate2D() {
        foreach (Transform child in parent) {
            child.gameObject.SetActive(true);
        }
    }

    private void Deactivate2D() {
        foreach (Transform child in parent) {
            child.gameObject.SetActive(false);
        }
    }
}
