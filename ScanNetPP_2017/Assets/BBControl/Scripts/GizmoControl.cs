using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using StructureAR;

public class GizmoControl : MonoBehaviour {

    public static GizmoControl instance;
    public enum Tool { POSITION, ROTATION, SCALE, ADD, REMOVE, NONE };

    public static string GIZMO_LAYER_NAME = "Gizmo";
    public static string BOUNDING_BOX_TAG = "BoundingBox";
    public static int GIZMO_LAYER;
    public static int GIZMO_LAYER_MASK;
    public static float MAX_DISTANCE = 100;

    public bool forceActive;
    public GameObject selectedObj;
    public Tool currentTool;
    public Color selectedColor;
    public Color storedColor;

    public string[] names;
    public Tool[] tools;
    public GameObject[] prefabs;
    public Button[] buttons;

    private int gizmoLayer;
    private bool toolsEnabled;
    private Dictionary<string, Tool> nameToTool = new Dictionary<string, Tool>();
    private Dictionary<Tool, GameObject> toolToPrefab = new Dictionary<Tool, GameObject>();
    private Dictionary<Tool, Button> toolToButton = new Dictionary<Tool, Button>();
    private GameObject toolPrefab;
    private GameObject toolObj;

    private void Start() {
        if (instance == null) {
            instance = this;
        } else if (instance != this) {
            Destroy(this);
        }

        GIZMO_LAYER = LayerMask.NameToLayer(GIZMO_LAYER_NAME);
        GIZMO_LAYER_MASK = LayerMask.GetMask(GIZMO_LAYER_NAME);
        currentTool = Tool.NONE;

        Manager.StructureARGameEvent += HandleStructureARGameEvent;

        for (int i = 0; i < names.Length; i++) {
            nameToTool.Add(names[i], tools[i]);
            toolToPrefab.Add(tools[i], prefabs[i]);
            toolToButton.Add(tools[i], buttons[i]);
        }

        EnableTools();
        
    }

    private void Update() {
        if (InputManager.instance.touchDown && !InputManager.instance.touchDownUI) {
            Ray ray = Camera.main.ScreenPointToRay(InputManager.instance.position);
            RaycastHit hit;
            GameObject obj = null;
            bool setupRequired = false, cleanupRequired = true;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity)) {
                obj = hit.collider.gameObject;
                if (obj.CompareTag(BOUNDING_BOX_TAG)) {
                    if (obj != selectedObj) {
                        setupRequired = true;
                    } else {
                        cleanupRequired = false;
                    }
                } else if (obj.layer == GIZMO_LAYER) {
                    cleanupRequired = false;
                }
            }

            if (cleanupRequired && selectedObj != null) {
                if (currentTool == Tool.POSITION || currentTool == Tool.ROTATION || currentTool == Tool.SCALE) {
                    CleanupToolOnObj();
                }
                selectedObj = null;
            }

            if (setupRequired) {
                selectedObj = obj;
                if (currentTool == Tool.POSITION || currentTool == Tool.ROTATION || currentTool == Tool.SCALE) {
                    SetupToolOnObj(selectedObj, currentTool);
                }
            }
        }
    }

    /**
     * Called when one of the buttons is selected.
     */
    public void OnClickButton(string toolName) {
        // Extract values
        Tool tool; Button button; GameObject prefab;
        nameToTool.TryGetValue(toolName, out tool);
        toolToButton.TryGetValue(tool, out button);
        toolToPrefab.TryGetValue(tool, out prefab);

        // Deselect all buttons
        foreach (Button b in toolToButton.Values) {
            SetColor(b, storedColor);
        }

        // Deselecting a tool
        if (tool == currentTool) {
            CleanupToolOnObj();
            currentTool = Tool.NONE;
            toolPrefab = null;
        }
        // Select a new tool
        else {
            SetColor(button, selectedColor);
            CleanupToolOnObj();
            currentTool = tool;
            toolPrefab = prefab;
            SetupToolOnObj(selectedObj, currentTool);
        }
    }

    /**
     * Set the color of a button.
     */
    private void SetColor(Button button, Color color) {
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color;
        button.colors = colors;
    }

    /**
     * Apply the given tool to the given GameObject.
     */
    private void SetupToolOnObj(GameObject obj, Tool tool) {
        switch (tool) {
            case Tool.ADD:
            case Tool.REMOVE:
                toolObj = Instantiate(toolPrefab);
                break;
            case Tool.POSITION:
                if (selectedObj != null) {
                    toolObj = Instantiate(toolPrefab, obj.transform.position, Quaternion.identity);
                    PositionControl positionControl = toolObj.GetComponent<PositionControl>();
                    positionControl.LinkObject(selectedObj);
                }
                break;
            case Tool.ROTATION:
                if (selectedObj != null) {
                    toolObj = Instantiate(toolPrefab, obj.transform.position, obj.transform.rotation);
                    RotationControl rotationControl = toolObj.GetComponent<RotationControl>();
                    rotationControl.LinkObject(selectedObj);
                }
                break;
            case Tool.SCALE:
                if (selectedObj != null) {
                    toolObj = Instantiate(toolPrefab, obj.transform.position, obj.transform.rotation);
                    ScaleControl scaleControl = toolObj.GetComponent<ScaleControl>();
                    scaleControl.LinkObject(selectedObj);
                }
                break;
        }
    }

    /**
     * Clean-up and remove the given tool from the given GameObject.
     */
    private void CleanupToolOnObj() {
        Destroy(toolObj);
        toolObj = null;
    }

    protected void HandleStructureARGameEvent(object sender, GameEventArgs args) {
        /*
        bool newToolsEnabled = toolsEnabled;
        switch (args.gameState) {
            case SensorState.DeviceNotReady:
            case SensorState.CameraAccessRequired:
            case SensorState.DeviceNeedsCharging:
            case SensorState.DeviceReady:
            case SensorState.Playing:
            case SensorState.Reset:
            case SensorState.WaitingForMesh:
                newToolsEnabled = true;
                break;
            case SensorState.Scanning:
                newToolsEnabled = true;
                break;
        }

        if (forceActive) {
            newToolsEnabled = true;
        }

        if (newToolsEnabled && !toolsEnabled) {
            EnableTools();
        } else {
            DisableTools();
        }

        toolsEnabled = newToolsEnabled;
        */ 
    }

    private void EnableTools() {
        foreach (Button button in toolToButton.Values) {
            button.interactable = true;
        }
    }

    private void DisableTools() {
        CleanupToolOnObj();
        currentTool = Tool.NONE;
        toolPrefab = null;

        foreach (Button button in toolToButton.Values) {
            button.interactable = false;
        }
    }
}
