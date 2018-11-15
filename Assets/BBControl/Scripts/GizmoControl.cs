using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GizmoControl : MonoBehaviour {

    public enum Tool { POSITION, ROTATION, SCALE, NONE };

    public static string GIZMO_LAYER_NAME = "Gizmo";
    public static int GIZMO_LAYER;
    public static int GIZMO_LAYER_MASK;
    public static float MAX_DISTANCE = 100;

    public Tool currentTool;
    public Color selectedColor;
    public Color storedColor;

    public string[] names;
    public Tool[] tools;
    public GameObject[] prefabs;
    public Button[] buttons;

    private int gizmoLayer;
    private Dictionary<string, Tool> nameToTool = new Dictionary<string, Tool>();
    private Dictionary<Tool, GameObject> toolToPrefab = new Dictionary<Tool, GameObject>();
    private Dictionary<Tool, Button> toolToButton = new Dictionary<Tool, Button>();
    private GameObject selectedObj;
    private GameObject toolPrefab;
    private GameObject toolObj;

    private void Start() {
        GIZMO_LAYER = LayerMask.NameToLayer(GIZMO_LAYER_NAME);
        GIZMO_LAYER_MASK = LayerMask.GetMask(GIZMO_LAYER_NAME);

        for (int i = 0; i < names.Length; i++) {
            nameToTool.Add(names[i], tools[i]);
            toolToPrefab.Add(tools[i], prefabs[i]);
            toolToButton.Add(tools[i], buttons[i]);
        }
    }

    private void Update() {
        if (InputManager.instance.touchDown && !InputManager.instance.touchDownUI) {
            Ray ray = Camera.main.ScreenPointToRay(InputManager.instance.position);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity)) {
                GameObject obj = hit.collider.gameObject;
                if (obj != selectedObj && obj.layer != GIZMO_LAYER) {
                    selectedObj = obj;
                    SetupToolOnObj(selectedObj, currentTool);
                }
            } else {
                if (selectedObj != null) {
                    CleanupToolOnObj(selectedObj, currentTool);
                    selectedObj = null;
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
            if (selectedObj != null) {
                CleanupToolOnObj(selectedObj, currentTool);
            }
            currentTool = Tool.NONE;
            toolPrefab = null;
        }
        // Select a new tool
        else {
            SetColor(button, selectedColor);
            if (selectedObj != null) {
                CleanupToolOnObj(selectedObj, currentTool);
            }
            currentTool = tool;
            toolPrefab = prefab;
            if (selectedObj != null) {
                SetupToolOnObj(selectedObj, currentTool);
            }
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
            case Tool.POSITION:
                toolObj = Instantiate(toolPrefab, obj.transform.position, Quaternion.identity);
                PositionControl positionControl = toolObj.GetComponent<PositionControl>();
                positionControl.LinkObject(selectedObj);
                break;
            case Tool.ROTATION:
                toolObj = Instantiate(toolPrefab, obj.transform.position, obj.transform.rotation);
                RotationControl rotationControl = toolObj.GetComponent<RotationControl>();
                rotationControl.LinkObject(selectedObj);
                break;
            case Tool.SCALE:
                toolObj = Instantiate(toolPrefab, obj.transform.position, obj.transform.rotation);
                ScaleControl scaleControl = toolObj.GetComponent<ScaleControl>();
                scaleControl.LinkObject(selectedObj);
                break;
        }
    }

    /**
     * Cleanupp and remove the given tool from the given GameObject.
     */
    private void CleanupToolOnObj(GameObject obj, Tool tool) {
        switch (tool) {
            case Tool.POSITION:
                toolObj.GetComponent<PositionControl>().UnlinkObject();
                break;
        }
        Destroy(toolObj);
        toolObj = null;
    }
}
