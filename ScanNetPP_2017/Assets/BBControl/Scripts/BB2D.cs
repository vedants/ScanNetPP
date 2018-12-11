using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Manages the 2D bounding box linked to a 3D one.
 * Operates in screenspace, where lower-left corner is (0, 0)
 * and upper-right corner is (screenWidth, screenHeight)
 */
public class BB2D : MonoBehaviour {

    public static Vector2 MARGIN = Vector2.one * 10;
    public static Vector3[] vertices = new Vector3[] {
        new Vector3(0.5f, 0.5f, 0.5f),
        new Vector3(0.5f, 0.5f, -0.5f),
        new Vector3(0.5f, -0.5f, 0.5f),
        new Vector3(0.5f, -0.5f, -0.5f),
        new Vector3(-0.5f, 0.5f, 0.5f),
        new Vector3(-0.5f, 0.5f, -0.5f),
        new Vector3(-0.5f, -0.5f, 0.5f),
        new Vector3(-0.5f, -0.5f, -0.5f),
    };

    public GameObject linkedObj;
    public Vector2 origin; // Lower-left corner
    public Vector2 bounds; // Width then height, in pixels

    private RectTransform rectTransform;
    private Vector2 invertedPixelDimensions;

    private void Start() {
        rectTransform = GetComponent<RectTransform>();
        invertedPixelDimensions = new Vector2(1f / Camera.main.pixelWidth, 1f / Camera.main.pixelHeight);
    }

    private void Update() {
        UpdateState();
    }

    private void UpdateState() {
        // Convert local 3D vertices to world 3D points
        Vector3[] points3DWorld = LocalToWorld(vertices);

        // Convert world 3D points to screenspace 2D points
        Vector2[] points2DScreen = WorldToScreen(points3DWorld);

        // Find min/max points of the 2D points
        Vector2 minPoint2DScreen = FindMinPoint2D(points2DScreen);
        Vector2 maxPoint2DScreen = FindMaxPoint2D(points2DScreen);

        // Set state
        origin = minPoint2DScreen;
        bounds = maxPoint2DScreen - minPoint2DScreen;

        // Convert to ratios (based off of pixel width and height)
        Vector2 minPoint2DRatio = Vector2.Scale(minPoint2DScreen - MARGIN, invertedPixelDimensions);
        Vector2 maxPoint2DRatio = Vector2.Scale(maxPoint2DScreen + MARGIN, invertedPixelDimensions);

        // Update visible state
        rectTransform.position = minPoint2DScreen - MARGIN;
        rectTransform.localScale = maxPoint2DRatio - minPoint2DRatio;
    }

    private Vector3[] LocalToWorld(Vector3[] points3DLocal) {
        Vector3[] points3DWorld = new Vector3[points3DLocal.Length];
        for (int i = 0; i < points3DWorld.Length; i++) {
            points3DWorld[i] = linkedObj.transform.TransformPoint(points3DLocal[i]);
        }
        return points3DWorld;
    }

    private Vector2[] WorldToScreen(Vector3[] points3DWorld) {
        Vector2[] points2D = new Vector2[points3DWorld.Length];
        for (int i = 0; i < points2D.Length; i++) {
            points2D[i] = Camera.main.WorldToScreenPoint(points3DWorld[i]);
        }
        return points2D;
    }

    private Vector2 FindMinPoint2D(Vector2[] points2D) {
        Vector2 minPoint2D = points2D[0];
        foreach (Vector2 point2D in points2D) {
            minPoint2D.x = Mathf.Min(minPoint2D.x, point2D.x);
            minPoint2D.y = Mathf.Min(minPoint2D.y, point2D.y);
        }
        return minPoint2D;
    }

    private Vector2 FindMaxPoint2D(Vector2[] points2D) {
        Vector2 maxPoint2D = points2D[0];
        foreach (Vector2 point2D in points2D) {
            maxPoint2D.x = Mathf.Max(maxPoint2D.x, point2D.x);
            maxPoint2D.y = Mathf.Max(maxPoint2D.y, point2D.y);
        }
        return maxPoint2D;
    }
}
