/*********************************************************************
  This file is part of the Structure SDK.
  Copyright © 2015 Occipital, Inc. All rights reserved.
  http://structure.io
**********************************************************************/

using UnityEngine;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.IO;
using System.Threading;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Callbacks;
//If you get a "missing assembly reference" on the below line, this is because you do not have Unity iOS Build Support installed.
using UnityEditor.iOS.Xcode;

using PlistEntry = System.Collections.Generic.KeyValuePair<string, UnityEditor.iOS.Xcode.PlistElement>; 

// IOSArchitectures taken from: http://forum.unity3d.com/threads/4-6-ios-64-bit-beta.290551/page-11
enum IOSArchitectures : int
{
	Armv7 = 0,
	Arm64 = 1,
	Universal = 2,
};

[InitializeOnLoad]
public class StructurePlugin : MonoBehaviour
{
	static StructurePlugin ()
	{
		// Set the iOS target version to iOS 8.
		PlayerSettings.iOS.targetOSVersion = iOSTargetOSVersion.iOS_8_0;
		
		// Restrict deployment to iPad only.
		PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
		
		// Use IL2CPP (the only arm64-capable one) as a scripting backend.
		PlayerSettings.SetPropertyInt("ScriptingBackend", (int)ScriptingImplementation.IL2CPP, BuildTargetGroup.iOS);
		
		// Assuming you want to build Arm64 only.
		PlayerSettings.SetPropertyInt("Architecture", (int)IOSArchitectures.Arm64, BuildTargetGroup.iOS);
		
		EditorApplication.update += Update;
	}
	
	static void Update ()
	{
		// Called once per frame, by the editor.
	}
	
	[PostProcessBuildAttribute]
	public static void OnPostprocessBuild (BuildTarget target, string pathToBuiltProject)
	{
		if (target != BuildTarget.iOS)
			return;
		
		//NOTE: these patches are not robust to Xcode generation by Append, only Rebuild
		PatchXcodeProject(pathToBuiltProject);
		
		PatchInfoPlist(pathToBuiltProject);
		
		// This needs to done after we have rewritten everything, because it may trigger an Xcode launch.
		SelectXcodeBuildConfiguration(pathToBuiltProject, "Release");
	}

	public static string checkPBXProjectPath (string projectPath)
	{
		//In versions of Unity < 5.1.3p2,
		// the xcode project path returned by PBXProject.GetPBXProjectPath
		// is incorrect. We fix it here.

		string projectBundlePath = Path.GetDirectoryName(projectPath);

		if (projectBundlePath.EndsWith(".xcodeproj"))
			return projectPath;
		else
			return projectBundlePath + ".xcodeproj/project.pbxproj";
	}

	public static void SetBuildSettingsForProjectTargetConfig (PBXProject project, string targetGuid, string configName)
	{
		string config = project.BuildConfigByName(targetGuid, configName);

		if (config == null)
			return;

		project.SetBuildPropertyForConfig(config, "DEBUG_INFORMATION_FORMAT", "dwarf");
		project.SetBuildPropertyForConfig(config, "ONLY_ACTIVE_ARCH", "YES");
		project.SetBuildPropertyForConfig(config, "ENABLE_BITCODE", "NO");
	}

	public static void SetBuildSettingsForProjectTarget (PBXProject project, string targetGuid)
	{
		SetBuildSettingsForProjectTargetConfig(project, targetGuid, "Debug");
		SetBuildSettingsForProjectTargetConfig(project, targetGuid, "Release");
		SetBuildSettingsForProjectTargetConfig(project, targetGuid, "ReleaseForRunning");
		SetBuildSettingsForProjectTargetConfig(project, targetGuid, "ReleaseForProfiling");
	}

	public static void PatchXcodeProject (string pathToBuiltProject)
	{
		PBXProject project = new PBXProject();
		
		string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);

		projectPath = checkPBXProjectPath(projectPath);

		project.ReadFromFile(projectPath);
		
		string guid = project.TargetGuidByName("Unity-iPhone");

		project.AddFrameworkToProject(guid, "ExternalAccessory.framework", false);

		// When Metal rendering support is disabled in Unity, the generated project will not link the Metal framework.
		project.AddFrameworkToProject(guid, "Metal.framework", false);

		project.AddFrameworkToProject(guid, "Accelerate.framework", false);
		project.AddFrameworkToProject(guid, "AVFoundation.framework", false);
		project.AddFrameworkToProject(guid, "CoreGraphics.framework", false);
		project.AddFrameworkToProject(guid, "CoreImage.framework", false);
        project.AddFrameworkToProject(guid, "CoreMedia.framework", false);
        project.AddFrameworkToProject(guid, "CoreMotion.framework", false);
        project.AddFrameworkToProject(guid, "CoreVideo.framework", false);
        project.AddFrameworkToProject(guid, "Foundation.framework", false);
        project.AddFrameworkToProject(guid, "GLKit.framework", false);
        project.AddFrameworkToProject(guid, "ImageIO.framework", false);
        project.AddFrameworkToProject(guid, "MessageUI.framework", false);
        project.AddFrameworkToProject(guid, "OpenGLES.framework", false);
        project.AddFrameworkToProject(guid, "UIKit.framework", false);

		project.AddFileToBuild(guid, project.AddFile("usr/lib/libz.dylib", "Frameworks/libz.dylib", PBXSourceTree.Sdk));

		SetBuildSettingsForProjectTarget(project, guid);

		project.WriteToFile(projectPath);
	}
	
	public static void PatchInfoPlist(string pathToBuiltProject)
	{
		string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
		
		PlistDocument plist = new PlistDocument();
		plist.ReadFromFile(plistPath);
		
		
		// =================================
		// We must do this here instead of passing the plist to
		//  a useful helper function because Unity refuses to build functions
		//  where a variable of type PlistDocument is passed.
		
		string key = "UISupportedExternalAccessoryProtocols";
		string[] values = new string[3]
		{
			"io.structure.control",
			"io.structure.depth",
			"io.structure.infrared"
		};
		
		if (!plist.root.values.ContainsKey(key))
		{
			PlistElementArray array = new PlistElementArray();
			foreach (string value in values)
				array.AddString(value);
			
			plist.root.values.Add (new PlistEntry(key, array));
		}
		// =================================
		
		// Camera access on iOS10.
		plist.root.values.Add( new PlistEntry("NSCameraUsageDescription", new PlistElementString("Camera image used for motion tracking and AR compositing") ) );

		// Enable file sharing.
		plist.root.values.Add( new PlistEntry("UIFileSharingEnabled", new PlistElementBoolean(true) ) );		
		
		plist.WriteToFile(plistPath);
	}
	
	public static void TriggerXcodeDefaultSharedSchemeGeneration (string pathToBuiltProject)
	{
		// Launch Xcode to trigger the scheme generation.
		ProcessStartInfo proc = new ProcessStartInfo();
		
		proc.FileName = "open";
		proc.WorkingDirectory = pathToBuiltProject;
		proc.Arguments = "Unity-iPhone.xcodeproj";
		proc.WindowStyle = ProcessWindowStyle.Hidden;
		proc.UseShellExecute = true;
		Process.Start(proc);
		
		Thread.Sleep(3000);
	}
	
	public static string GetDefaultSharedSchemePath (string pathToBuiltProject)
	{
		return Path.Combine(pathToBuiltProject, "Unity-iPhone.xcodeproj/xcshareddata/xcschemes/Unity-iPhone.xcscheme");
	}
	
	public static void SelectXcodeBuildConfiguration (string pathToBuiltProject, string configuration)
	{
		string schemePath = GetDefaultSharedSchemePath(pathToBuiltProject);
		
		if (!File.Exists(schemePath))
			TriggerXcodeDefaultSharedSchemeGeneration(pathToBuiltProject);
		
		if (!File.Exists(schemePath))
		{
			//Debug.Log("Xcode scheme project generation failed. You will need to manually select the 'Release' configuration. The deployed iOS application performance will be disastrous, otherwise.");
			return;
		}
		
		XmlDocument xml = new XmlDocument();
		xml.Load(schemePath);
		XmlNode node = xml.SelectSingleNode("Scheme/LaunchAction");
		node.Attributes["buildConfiguration"].Value = configuration;
		xml.Save(schemePath);
	}	
}

#endif
