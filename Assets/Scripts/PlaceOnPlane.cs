using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Lean.Touch;


[RequireComponent(typeof(ARSessionOrigin))]
public class PlaceOnPlane : MonoBehaviour
{

    public GameObject m_PlacedPrefab;
    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    public GameObject placedPrefab
    {
        get { return m_PlacedPrefab; }
        set { m_PlacedPrefab = value; }
    }
    
    public GameObject spawnedObject { get; private set; }
    ARSessionOrigin m_SessionOrigin;

    int currState;
    public Button toggleModeBtn; 
    private string[] toggleModeBtnTexts;    
    
    //global variables for line drawing 
    GameObject currLine; 
    bool lineDrawingStarted;
    int lineRendererIndex; 
    public Material lineMat; 
    
    void Awake()
    {   
        m_SessionOrigin = GetComponent<ARSessionOrigin>();
        currState = 0;
        lineDrawingStarted = false;
    }

    void Start() 
    {
        toggleModeBtnTexts = new string[] {"Placing Mode", "Drawing Mode"};
        toggleModeBtn.GetComponentInChildren<Text>().text = toggleModeBtnTexts[0];
    }

    void Update()
    {
        if (currState == 0)  //in placing mode 
        { 
            CheckAndPlaceObject();
        }

        else if (currState == 1) // in drawing mode 
        {
            CheckAndDrawLine();
        }
    }

    public void ToggleState() 
    {
        currState = (currState + 1) % 2;
        toggleModeBtn.GetComponentInChildren<Text>().text = toggleModeBtnTexts[currState];
    }

    public void AddNewObject()

    {
        //On-screen gestures apply to all gameobjects with Lean components attached. 
        //This ensures that scaling and rotatation only apply to the most recently added cube, 
        //but the corollary is that you can't modify old cubes.
        spawnedObject.GetComponent<LeanScale>().enabled = false; 
        spawnedObject.GetComponent<LeanRotate>().enabled = false; 
        spawnedObject = null;
    }


    void CheckAndPlaceObject() 
    {
        if (Input.touchCount > 0) 
        {
            Touch touch = Input.GetTouch(0); 

            if (m_SessionOrigin.Raycast(touch.position, s_Hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = s_Hits[0].pose; 
                if (spawnedObject == null) 
                {
                    //create an object from prefab
                    spawnedObject = Instantiate(m_PlacedPrefab, hitPose.position, hitPose.rotation);
                }
                else  
                {
                    // just move the already spawned object
                    spawnedObject.transform.position = hitPose.position;
                }
            }
        }
    }

    void CheckAndDrawLine() 
    {
        if (Input.touchCount > 0 || Input.GetMouseButton(0))  //touching screen
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
            RaycastHit hit;

            #if UNITY_EDITOR
                ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            #endif

            if (Physics.Raycast(ray, out hit)) {
                if (hit.transform.tag == "pumpkin") {
                    if (!lineDrawingStarted) 
                    {
                        //initialize line
                        GameObject lineObject = new GameObject("Line");
                        lineObject.transform.SetParent(hit.transform);
                        lineObject.AddComponent<LineRenderer>();
                        LineRenderer line = lineObject.GetComponent<LineRenderer>();
                        line.SetWidth(0.005f, 0.005f);
                        line.useWorldSpace =false;
                        line.material = lineMat;
                        line.SetColors(Color.black, Color.black);
                        line.SetVertexCount(0);

                        lineRendererIndex = 0;
                        currLine = lineObject;
                        lineDrawingStarted = true;
                    } 

                    //add a point to the line
                    currLine.GetComponent<LineRenderer>().SetVertexCount(lineRendererIndex+1);
                    currLine.GetComponent<LineRenderer>().SetPosition(lineRendererIndex, hit.point);
                    lineRendererIndex ++; 
                    print(lineRendererIndex);
                }
            }
        }

        else 
        {
            lineDrawingStarted = false;
        }
    }            
}