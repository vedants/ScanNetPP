using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextTransformer : MonoBehaviour {

    public float scaleFactor;
    public float gap;

    private Transform parent;
    private Renderer meshRenderer;
    private Text text;
    private BBState state;

	void Start () {
        parent = transform.parent;
        meshRenderer = transform.parent.GetComponent<Renderer>();
        text = transform.Find("Text").GetComponent<Text>();
        state = transform.parent.GetComponent<BBState>();
	}
	
	void Update () {
        Camera cam = Camera.main;

        // Update text
        text.text = state.label;

        // Update position
        float maxY = meshRenderer.bounds.max.y;
        transform.position = new Vector3(parent.position.x, maxY + gap, parent.position.z);

        // Update rotation
        transform.LookAt(cam.transform, Vector3.up);

        // Update scale
        //float distance = Vector3.Distance(cam.transform.position, transform.position);
        //transform.SetParent(null);
        //transform.localScale = Vector3.one * scaleFactor;
        //transform.SetParent(parent);
	}
}
