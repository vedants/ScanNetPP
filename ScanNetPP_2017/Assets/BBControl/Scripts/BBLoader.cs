using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;

public class BBLoader : MonoBehaviour {

    public string path;
    public GameObject boundingBoxPrefab;
    public GameObject boundingBox2DPrefab;
    public Transform boundingBoxParent;
    public RectTransform boundingBox2DParent;

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            string json = ReadJsonFromFile(path);
            LoadJson(json);
        }
    }

    /**
     * Given a json blob representing the a BBList,
     * load and instantiate bounding boxes to match the contents.
     */
    public void LoadJson(string json) {
        BBList bbList = JsonUtility.FromJson<BBList>(json);
        foreach (BBInfo bbInfo in bbList.boundingBoxes) {
            GameObject boundingBox = Instantiate(boundingBoxPrefab, bbInfo.position, Quaternion.Euler(bbInfo.rotation));
            boundingBox.transform.localScale = bbInfo.scale;
            boundingBox.transform.SetParent(boundingBoxParent);
            boundingBox.GetComponent<BBState>().label = bbInfo.label;

            // 2D
            GameObject boundingBox2D = Instantiate(boundingBox2DPrefab);
            boundingBox2D.transform.SetParent(boundingBox2DParent);
            boundingBox2D.GetComponent<BB2D>().linkedObj = boundingBox;
        }
    }

    /**
     * Load in a JSON string from a (full) file path.
     */
    public string ReadJsonFromFile(string filePath) {
        StreamReader reader = new StreamReader(filePath);
        return reader.ReadToEnd();
    }
}
