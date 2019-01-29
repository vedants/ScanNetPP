using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DimBoxes;

public class SUNInputManager : MonoBehaviour {

    public static string BOUNDING_BOX_LAYER_NAME = "BoundingBox";
    public static Color[] colors = new Color[] { Color.red, Color.yellow, Color.green, Color.blue, Color.cyan, Color.magenta};
    public static float TOLERANCE = 0.2f;

    public Camera s2Cam, s3Cam, s4Cam;
    public GameObject linePrefab;
    public GameObject SUNBoundingBoxPrefab;
    public InputField inputField;
    public GameObject blackoutPanel;

    int BOUNDING_BOX_LAYER;
    int s2Clicks = 0;
    GameObject line1, line2;
    LineRenderer ren1, ren2;
    bool actionsEnabled = true;
    bool manipulatingHeight = false;
    bool topNotBottom = true;
    Camera camInUse;
    Transform tInUse;
    List<GameObject> stack = new List<GameObject>();

    private void Start() {
        BOUNDING_BOX_LAYER = LayerMask.GetMask(BOUNDING_BOX_LAYER_NAME);
    }

    private void Update() {
        if (Input.GetMouseButtonDown(0) && actionsEnabled) {
            int section = GetSection();
            switch (section) {
                case 2:
                    S2ProcessClick();
                    break;
                case 3:
                    S34ProcessClick(s3Cam);
                    break;
                case 4:
                    S34ProcessClick(s4Cam);
                    break;
            }
        } else if (Input.GetMouseButton(0) && actionsEnabled) {
            int section = GetSection();
            switch (section) {
                case 3:
                    if (camInUse == s3Cam) {
                        S34Update();
                    }
                    break;
                case 4:
                    if (camInUse == s4Cam) {
                        S34Update();
                    }
                    break;
            }
        } else if (Input.GetMouseButtonUp(0) && actionsEnabled) {
            manipulatingHeight = false;
        }

        if (Input.GetKeyDown(KeyCode.Backspace) && stack.Count > 0 && actionsEnabled) {
            Destroy(stack[stack.Count - 1]);
            stack.RemoveAt(stack.Count - 1);
        }

        S2Update();
        LabelUpdate();
    }

    private int GetSection() {
        double x = Input.mousePosition.x / Screen.width;
        double y = Input.mousePosition.y / Screen.height;
        if (x < 0.5f && y < 0.5f) {
            return 3;
        } else if (x < 0.5f && y >= 0.5f) {
            return 1;
        } else if (x > 0.5f && y < 0.5f) {
            return 4;
        } else {
            return 2;
        }
    }

    #region Section 2 - 3 Point Clicks and Initial Bounding Box Creation

    private void S2ProcessClick() {
        Ray r = s2Cam.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, s2Cam.nearClipPlane));
        Vector3 p = r.origin;
        p.y = 0;
        switch (s2Clicks) {
            case 0:
                line1 = Instantiate(linePrefab);
                ren1 = line1.GetComponent<LineRenderer>();
                ren1.positionCount = 2;
                ren1.SetPosition(0, p);
                s2Clicks += 1;
                break;
            case 1:
                line2 = Instantiate(linePrefab);
                ren2 = line2.GetComponent<LineRenderer>();
                ren2.positionCount = 2;
                ren2.SetPosition(0, p);
                s2Clicks += 1;
                break;
            case 2:
                Vector3 v = ren1.GetPosition(1) - ren1.GetPosition(0);
                Vector3 ortho = Vector3.Cross(v, Vector3.up).normalized;
                Vector3 proj = ProjectPointToLine(p, ren1.GetPosition(1), ortho);

                Vector3 origin = ren1.GetPosition(1);
                Vector3 v1 = ren1.GetPosition(0) - origin;
                Vector3 v2 = proj - origin;

                GameObject g = Instantiate(SUNBoundingBoxPrefab);
                g.transform.position = origin + v1 / 2 + v2 / 2;
                g.transform.forward = -v2; // 1st line segment is set as the front
                g.transform.localScale = new Vector3(v1.magnitude, 1, v2.magnitude);
                g.AddComponent<BoundBox>().lineColor = colors[Random.Range(0, colors.Length)];

                stack.Add(g);
                S2Cleanup();
                tInUse = g.transform;
                SetLabelMode(true);
                break;
        }
    }

    private void S2Update() {
        if (s2Clicks == 1 || s2Clicks == 2) {
            Ray r = s2Cam.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, s2Cam.nearClipPlane));
            Vector3 p = r.origin;
            p.y = 0;

            if (s2Clicks == 1) {
                ren1.SetPosition(1, p);
            } else {
                Vector3 v = ren1.GetPosition(1) - ren1.GetPosition(0);
                Vector3 ortho = Vector3.Cross(v, Vector3.up).normalized;
                Vector3 proj = ProjectPointToLine(p, ren1.GetPosition(1), ortho);
                ren2.SetPosition(1, proj);
            }
        }
    }

    private void S2Cleanup() {
        Destroy(line1);
        line1 = null;
        ren1 = null;
        Destroy(line2);
        line2 = null;
        ren2 = null;
        s2Clicks = 0;
    }

    #endregion

    #region Labeling

    private void LabelUpdate() {
        if (inputField.interactable && !inputField.isFocused) {
            inputField.Select();
        }

        if (inputField.interactable && Input.GetKeyDown(KeyCode.Return)) {
            // TODO: Store text somewhere if that's needed.
            SetLabelMode(false);
            inputField.text = "";
        }
    }

    private void SetLabelMode(bool b) {
        inputField.interactable = b;
        actionsEnabled = !b;
        blackoutPanel.SetActive(b);
    }

    #endregion

    #region Section 3 - Height Adjustment

    private void S34ProcessClick(Camera cam) {
        Ray r = cam.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, s2Cam.nearClipPlane));
        RaycastHit hit;
        if (Physics.Raycast(r, out hit, Mathf.Infinity, BOUNDING_BOX_LAYER)) {
            if (hit.transform.gameObject == tInUse.gameObject) {
                float y = hit.point.y;
                Transform t = tInUse;
                float top = t.position.y + t.localScale.y / 2;
                float bottom = t.position.y - t.localScale.y / 2;
                float clearance = t.localScale.y / 2 * TOLERANCE;
                if (Mathf.Abs(top - y) < clearance) {
                    manipulatingHeight = true;
                    topNotBottom = true;
                    camInUse = cam;
                }
                else if (Mathf.Abs(bottom - y) < clearance) {
                    manipulatingHeight = true;
                    topNotBottom = false;
                    camInUse = cam;
                }
            }
        }
    }

    private void S34Update() {
        if (manipulatingHeight && tInUse != null) {
            Ray r = camInUse.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, s2Cam.nearClipPlane));
            float top = tInUse.position.y + tInUse.localScale.y / 2;
            float bottom = tInUse.position.y - tInUse.localScale.y / 2;
            if (topNotBottom) {
                top = r.origin.y;
            } else {
                bottom = r.origin.y;
            }

            tInUse.position = new Vector3(tInUse.position.x, (top + bottom) / 2, tInUse.position.z);
            tInUse.localScale = new Vector3(tInUse.localScale.x, top - bottom, tInUse.localScale.z);
        }
    }

    #endregion

    #region Utils

    public static Vector3 ProjectPointToLine(Vector3 point, Vector3 lineOrigin, Vector3 lineDirection) {
        float t = Vector3.Dot(lineDirection, point - lineOrigin);
        return lineOrigin + t * lineDirection;
    }

    #endregion
}
