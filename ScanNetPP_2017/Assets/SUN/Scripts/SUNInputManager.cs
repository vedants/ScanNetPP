using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SUNInputManager : MonoBehaviour {

    public static string BOUNDING_BOX_LAYER_NAME = "BoundingBox";

    private void Start() {
        BOUNDING_BOX_LAYER = LayerMask.GetMask(BOUNDING_BOX_LAYER_NAME);
    }

    private void Update() {
        if (Input.GetMouseButtonDown(0)) {
            int section = GetSection();
            switch (section) {
                case 2:
                    S2ProcessClick();
                    break;
            }
        }

        S2Update();
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

    public Camera s2Cam;
    public GameObject linePrefab;
    public GameObject SUNBoundingBoxPrefab;

    int BOUNDING_BOX_LAYER;
    int s2Clicks = 0;
    float lineY = 0;
    GameObject line1, line2;
    LineRenderer ren1, ren2;

    private void S2ProcessClick() {
        Vector3 p = s2Cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, s2Cam.nearClipPlane));
        Vector3 rayDir = Vector3.Normalize(p - s2Cam.transform.position);
        print(rayDir);
        RaycastHit hit;
        if (Physics.Raycast(new Ray(s2Cam.transform.position, rayDir), out hit, Mathf.Infinity, ~BOUNDING_BOX_LAYER)) {
            switch (s2Clicks) {
                case 0:
                    print("HI");
                    line1 = Instantiate(linePrefab);
                    ren1 = line1.GetComponent<LineRenderer>();
                    lineY = hit.point.y;
                    ren1.positionCount = 2;
                    ren1.SetPosition(0, hit.point);
                    s2Clicks += 1;
                    break;
                case 1:
                    line2 = Instantiate(linePrefab);
                    ren2 = line2.GetComponent<LineRenderer>();
                    ren2.positionCount = 2;
                    ren2.SetPosition(0, new Vector3(hit.point.x, lineY, hit.point.z));
                    s2Clicks += 1;
                    break;
                case 2:
                    // Get vectors that form the corner
                    Vector3 origin = ren1.GetPosition(1);
                    Vector3 v1 = ren1.GetPosition(0) - origin;
                    Vector3 v2 = ren2.GetPosition(1) - origin;

                    // Average the directions
                    Vector3 avg = ((v1 + v2) / 2).normalized;
                    Vector3 z1 = Quaternion.AngleAxis(-45, Vector3.up) * avg * v1.magnitude;
                    Vector3 z2 = Quaternion.AngleAxis(45, Vector3.up) * avg * v2.magnitude;

                    // DEBUG: Visualize the change
                    //ren1.SetPositions(new Vector3[] { origin + z1, origin });
                    //ren2.SetPositions(new Vector3[] { origin, origin + z2 });

                    GameObject g = Instantiate(SUNBoundingBoxPrefab);
                    g.transform.position = origin + z1 / 2 + z2 / 2;
                    g.transform.forward = -z2; // 1st line segment is set as the front
                    g.transform.localScale = new Vector3(z1.magnitude, 1, z2.magnitude);

                    S2Cleanup();
                    break;
            }
        } else {
            S2Cleanup();
        }
    }

    private void S2Update() {
        if (s2Clicks == 1 || s2Clicks == 2) {
            Vector3 p = s2Cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, s2Cam.nearClipPlane));
            Vector3 rayDir = Vector3.Normalize(p - s2Cam.transform.position);
            float t = (lineY - s2Cam.transform.position.y) / rayDir.y;
            Vector3 pos = s2Cam.transform.position + rayDir * t;

            LineRenderer lineRenderer = s2Clicks == 1 ? ren1 : ren2;
            lineRenderer.SetPosition(1, pos);
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
}
