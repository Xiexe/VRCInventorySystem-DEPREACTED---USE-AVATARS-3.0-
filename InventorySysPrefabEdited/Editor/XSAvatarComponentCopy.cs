using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

public class XSAvatarComponentCopy : EditorWindow {

	private Transform src;
	private Transform tar;

	private string filePath;

	[MenuItem("Xiexe/Tools/Component Copy")]
    static void Init()
    {
        XSAvatarComponentCopy window = (XSAvatarComponentCopy)GetWindow(typeof(XSAvatarComponentCopy), false, "Component Copy");
		
        window.Show();
    }

	private void OnGUI()
    {
		src = (Transform)EditorGUILayout.ObjectField(new GUIContent("Source Avatar", "The source of the components."), src, typeof(Transform), true);
		tar = (Transform)EditorGUILayout.ObjectField(new GUIContent("Target Avatar", "The target for the components"), tar, typeof(Transform), true);
		
		if (src == null){
			EditorGUILayout.HelpBox("No Source defined", MessageType.Error);
		}

		if (tar == null){
			EditorGUILayout.HelpBox("No Target defined", MessageType.Error);
		}
		
		
		if(tar != null)
		{
			if(src != null)
			{
				if (GUILayout.Button("Copy Components"))
				{
					CopyComponents(src, tar);
				}
			}
		}
	}

	private void CopyComponents(Transform src, Transform tar){
		int transforms = 1;

		for(int i = 0; i < transforms; i++){
			Transform childObject = tar.transform.GetChild(transforms);
			Transform childObject1 = src.transform.GetChild(transforms);
			childObject.transform.localScale = childObject1.transform.localScale;
			childObject.transform.position = childObject1.transform.position;
			childObject.transform.rotation = childObject1.transform.rotation;
			Debug.Log("Updated " + tar.transform.name);
			transforms++;
		}
	}



//Helper functions
	// Find File Path
	private string findAssetPath()
    {
        string[] guids1 = AssetDatabase.FindAssets("XSAvatarComponentCopy", null);
        string untouchedString = AssetDatabase.GUIDToAssetPath(guids1[0]);
        string[] splitString = untouchedString.Split('/');

        ArrayUtility.RemoveAt(ref splitString, splitString.Length - 1);
        ArrayUtility.RemoveAt(ref splitString, splitString.Length - 1);

        filePath = string.Join("/", splitString);
		return filePath;
    }
//------------------
}
