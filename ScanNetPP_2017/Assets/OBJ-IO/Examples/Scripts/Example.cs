using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityExtension;
using System.Text;
using UnityEngine.Networking;

using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter))]
public class Example : MonoBehaviour
{
	//------------------------------------------------------------------------------------------------------------	
	private const string INPUT_PATH = @"Assets/OBJ-IO/Examples/Meshes/Teapot.obj";
	private const string OUTPUT_PATH = @"Assets/OBJ-IO/Examples/Meshes/Teapot_Modified.obj";

    //------------------------------------------------------------------------------------------------------------	
	private void Start() {
		//	Load the OBJ in
		Debug.Log("starting loading...");
		var lStream = new FileStream(INPUT_PATH, FileMode.Open);

		var lOBJData = OBJLoader.LoadOBJ(lStream);
		Debug.Log("done loading...");
		var lMeshFilter = GetComponent<MeshFilter>();

		lMeshFilter.mesh.LoadOBJ(lOBJData);
		lStream.Close();
		
		lStream = null;
		lOBJData = null;


		//	Wiggle Vertices in Mesh
		var lVertices = lMeshFilter.mesh.vertices;
		for (int lCount = 0; lCount < lVertices.Length; ++lCount) {
			lVertices[lCount] = lVertices[lCount] + Vector3.up * Mathf.Sin(lVertices[lCount].x) * 4f;
		}

		lMeshFilter.mesh.vertices = lVertices;

		/*Export mesh as obj and send it over the network*/
		StringBuilder sBuilder = new StringBuilder();
		lOBJData = lMeshFilter.mesh.EncodeOBJ();
		OBJLoader.ExportOBJ(lOBJData, sBuilder);
		StartCoroutine(SendStringOverNetwork(sBuilder));
	}


	private IEnumerator SendStringOverNetwork(StringBuilder sBuilder) {
		string url = "http://scannetpp.pythonanywhere.com/upload_mesh";
		string msg = sBuilder.ToString();
		print(msg);
	    byte[] msg_utf8 = System.Text.Encoding.UTF8.GetBytes(msg);

	    UnityWebRequest www = UnityWebRequest.Post(url, msg);
	    www.SetRequestHeader("Content-Type", "application/text");

	    yield return www.SendWebRequest();

	    if(www.isNetworkError || www.isHttpError) {
	        Debug.Log(www.error);
	    }
	    else {
	        Debug.Log("Upload complete!");
	    }
	}
}