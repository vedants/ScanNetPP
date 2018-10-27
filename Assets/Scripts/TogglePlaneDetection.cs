using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;

public class TogglePlaneDetection : MonoBehaviour {
	public ARPlaneManager arplanemanager;
	private bool isDetecting  = true; 
	public Button btn; 

	public void togglePlaneDetectionStatus() { 
		isDetecting = ! isDetecting;
		arplanemanager.enabled = isDetecting;
		if (isDetecting) {
			btn.GetComponent<Text>().text = "place pumpkin";
		} else {
			btn.GetComponent<Text>().text = "move pumpkin";
		}
	}
}
