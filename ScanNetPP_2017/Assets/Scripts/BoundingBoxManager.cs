using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StructureAR;

public class BoundingBoxManager : MonoBehaviour {

	public GameObject BoundingBoxPrefab;
	private bool isBoundingBoxPlaceable;
	private float SPAWN_DISTANCE = 1.0f; 

	// Use this for initialization
	void Start () {
        Manager.StructureARGameEvent += HandleStructureARGameEvent;		
	}
	
	// Update is called once per frame
	void Update () {

	    if (Input.touchCount > 0)
	    {
	    	PlaceBoundingBox();
	    }	
	}

	protected void HandleStructureARGameEvent(object sender, GameEventArgs args)
    {
        switch(args.gameState)
        {
            case SensorState.DeviceNotReady:
            	isBoundingBoxPlaceable = false;
            	break;
			case SensorState.CameraAccessRequired:
				isBoundingBoxPlaceable = false;
				break;
			case SensorState.DeviceNeedsCharging:
				isBoundingBoxPlaceable = false;
				break;
            case SensorState.DeviceReady:
            	isBoundingBoxPlaceable = false;
            	break;
            case SensorState.Playing:
            	isBoundingBoxPlaceable = false;
            	break;
            case SensorState.Reset:
            	isBoundingBoxPlaceable = false;
            	break;
            case SensorState.Scanning:
            	isBoundingBoxPlaceable = true;
            	break;
            case SensorState.WaitingForMesh:
            	isBoundingBoxPlaceable = false;
                break;
        }
    }

	void PlaceBoundingBox() {
		if (!isBoundingBoxPlaceable) return;
		Vector3 t = Camera.main.transform.position + Camera.main.transform.forward * SPAWN_DISTANCE;
		Quaternion r = Quaternion.identity;
		GameObject boundingBox = GameObject.Instantiate(BoundingBoxPrefab);
		boundingBox.transform.position = t;
	}
}
