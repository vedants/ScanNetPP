using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Attached to the camera to allow for WASD-QE movement.
 */
public class SimulatorControl : MonoBehaviour {

    public float mouseSensitivity;
    public float speed;

    private Vector2 storedMousePosition;
    private Quaternion storedRotation;

    private void Update() {
        // Process rotation through holding right click
        if (Input.GetMouseButtonDown(1)) {
            storedMousePosition = Input.mousePosition;
            storedRotation = transform.rotation;
        } else if (Input.GetMouseButton(1)) {
            Vector2 mousePosition = Input.mousePosition;
            Vector2 diff = (mousePosition - storedMousePosition) * mouseSensitivity;
            transform.rotation = storedRotation;
            transform.Rotate(-diff.y, diff.x, 0);
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        }

        // Process lateral movement through WASD and vertical movement through QE
        Vector3 movement = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) {
            movement.z = 1;
        } else if (Input.GetKey(KeyCode.S)) {
            movement.z = -1;
        }
        if (Input.GetKey(KeyCode.A)) {
            movement.x = -1;
        } else if (Input.GetKey(KeyCode.D)) {
            movement.x = 1;
        }
        if (Input.GetKey(KeyCode.Q)) {
            movement.y = -1;
        } else if (Input.GetKey(KeyCode.E)) {
            movement.y = 1;
        }

        movement = movement.normalized * speed * Time.deltaTime;
        transform.Translate(movement, Space.Self);
    }
}
