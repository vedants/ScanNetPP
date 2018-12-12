using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/**
 * Takes bounding box transform data and writes it to a JSON file.
 */
public class BBSerializer : MonoBehaviour {

    public static string PARENT_NAME = "BoundingBoxes";

    [Serializable]
    public class BBInfo {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        public string label;
    }

    [Serializable]
    public class BBList {
        public BBInfo[] boundingBoxes;
    }

    public void Update() {
        if (Input.GetKeyDown(KeyCode.S)) {
            //string json = BBToJson();
            //WriteJsonToFile(json, "testing");
            WriteJsonToServer(BBToJson());
        }
    }

    /**
     * Takes all bounding boxes under the bounding box parent object serializes them into a JSON blob string.
     */
    public void UploadBBData() {
        WriteJsonToServer(BBToJson());
    }

    private string BBToJson() {
        GameObject bbParent = GameObject.Find(PARENT_NAME);
        BBInfo[] boundingBoxes = new BBInfo[bbParent.transform.childCount];
        for (int i = 0; i < boundingBoxes.Length; i++) {
            Transform childTransform = bbParent.transform.GetChild(i);
            BBInfo bbInfo = new BBInfo();
            bbInfo.position = childTransform.position;
            bbInfo.rotation = childTransform.eulerAngles;
            bbInfo.scale = childTransform.localScale;
            bbInfo.label = childTransform.GetComponent<BBState>().label;
            boundingBoxes[i] = bbInfo;
        }

        BBList bbList = new BBList();
        bbList.boundingBoxes = boundingBoxes;
        return JsonUtility.ToJson(bbList);
    }

    /**
     * Writes the contents of a json blob to a file w/ the provided name.
     */
    private void WriteJsonToFile(string json, string name) {
        string filePath = Application.persistentDataPath + "/" + name + ".txt";
        print(filePath);
        System.IO.File.WriteAllText(filePath, json);
    }

    /**
     * POSTs the contents of a json blob to a REST API endpoint.
     */

    public void WriteJsonToServer (string json) {
        StartCoroutine(SendMsgOverNetwork(json));
    }

    private IEnumerator SendMsgOverNetwork(string msg) {
        string url = "http://scannetpp.pythonanywhere.com/upload_bb_data";
        UnityWebRequest www = UnityWebRequest.Post(url, msg);
        www.SetRequestHeader("Content-Type", "application/text");

        yield return www.SendWebRequest();

        if(www.isNetworkError || www.isHttpError) {
            Debug.Log(www.error);
        }
        else {
            Debug.Log("Bounding box data upload complete!");
        }
    }

}
