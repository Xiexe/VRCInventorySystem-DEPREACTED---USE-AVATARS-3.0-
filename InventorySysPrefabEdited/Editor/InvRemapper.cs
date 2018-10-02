using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

public class InvRemapper : EditorWindow {

	private Transform targetPath;

	private Animator avatar;
    private AnimationClip anim;
	
	private string filePath;
	private string animName;
	
	private bool singleMode = true;

	[MenuItem("Xiexe/Tools/Inventory Remapper")]
    static void Init()
    {
        InvRemapper window = (InvRemapper)GetWindow(typeof(InvRemapper), false, "Inv. Remapper");
		
        window.Show();
    }

	private void OnGUI()
    {
		avatar = (Animator)EditorGUILayout.ObjectField(new GUIContent("Avatar", "Your avatar."), avatar, typeof(Animator), true);

		targetPath = (Transform)EditorGUILayout.ObjectField(new GUIContent("Item Location", "The Item you want to be in your inventory."), targetPath, typeof(Transform), true);

		if(targetPath != null && avatar != null){
			if (GUILayout.Button("Generate Inventory"))
        	{
				remapInv(targetPath);
			}
		}
	}

	private void remapInv(Transform target){
		string pathToInv = target.transform.GetHierarchyPath();
		
		string[] splitString = pathToInv.Split('/');
		
		if (singleMode){
			ArrayUtility.RemoveAt(ref splitString, 0);
			ArrayUtility.RemoveAt(ref splitString, splitString.Length - 1);
		}
		else {
			ArrayUtility.RemoveAt(ref splitString, 0);
		}
		pathToInv = string.Join("/", splitString);

		string assetPath = findAssetPath();
		string pathToEditor = assetPath + "/Editor";
		string pathToAnimFolder = assetPath + "/Animations";
		string pathToTemplate = pathToEditor + "/Templates/BehaviorKeyframeTemplate.anim";
		string pathToGenerated = pathToAnimFolder + "/Generated";

        if (!Directory.Exists(pathToGenerated)) {
            Directory.CreateDirectory(pathToGenerated);
			AssetDatabase.Refresh();
		}

		CreateInvAndMoveObject(pathToTemplate, pathToGenerated, pathToInv, pathToEditor);
	}

	private void CreateInvAndMoveObject(string pathToTemplate, string pathToGenerated, string pathToInv, string pathToEditor){
		Debug.Log(pathToInv);
		Object slotPrefab = (Object)AssetDatabase.LoadAssetAtPath(pathToEditor + "/Prefab/Inv_Single_Slot.prefab", typeof(Object));

		GameObject invSpawn = Instantiate(slotPrefab, targetPath.position, targetPath.rotation) as GameObject;
		invSpawn.transform.parent = targetPath.transform.parent;
		invSpawn.name = "Inv_" + targetPath.name;
		invSpawn.transform.localScale = new Vector3(1,1,1);

		Transform objectSlot = invSpawn.transform.GetChild(0).GetChild(0).GetChild(0);
		targetPath.transform.parent = objectSlot;


		CreateGlobalDisable(pathToTemplate, pathToGenerated, pathToInv, targetPath.name);
	}

	private void CreateGlobalDisable(string pathToTemplate, string pathToGenerated, string pathToInv, string objName){
		string globalDir = pathToGenerated + "/Global Animations";
		string globalAnimLoc = globalDir + "/" + avatar.name + "_DISABLE_GLOBAL.anim";

		if(!Directory.Exists(globalDir)){
			Directory.CreateDirectory(globalDir);
			AssetDatabase.Refresh();
		}
		
		if((AnimationClip)AssetDatabase.LoadAssetAtPath(globalAnimLoc, typeof(AnimationClip)) == null){
			FileUtil.CopyFileOrDirectory(pathToTemplate, globalAnimLoc);
			AssetDatabase.Refresh();
		}

			anim = (AnimationClip)AssetDatabase.LoadAssetAtPath(globalAnimLoc, typeof(AnimationClip));

			if (pathToInv == ""){
				anim.SetCurve("Inv_" + objName + "/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve());
				anim.SetCurve("Inv_" + objName + "/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve());
			}
			else{
				anim.SetCurve(pathToInv + "/Inv_" + objName + "/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve());
				anim.SetCurve(pathToInv + "/Inv_" + objName + "/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve());
			}
			
			
			CreateGlobalEnable(pathToTemplate, pathToGenerated, pathToInv, objName, globalDir);
	}

	private void CreateGlobalEnable(string pathToTemplate, string pathToGenerated, string pathToInv, string objName, string globalDir){
		string globalAnimLoc = globalDir + "/" + avatar.name + "_ENABLE_GLOBAL.anim";

		if(!Directory.Exists(globalDir)){
			Directory.CreateDirectory(globalDir);
			AssetDatabase.Refresh();
		}
		
		if((AnimationClip)AssetDatabase.LoadAssetAtPath(globalAnimLoc, typeof(AnimationClip)) == null){
			FileUtil.CopyFileOrDirectory(pathToTemplate, globalAnimLoc);
			AssetDatabase.Refresh();
		}

			anim = (AnimationClip)AssetDatabase.LoadAssetAtPath(globalAnimLoc, typeof(AnimationClip));

			if (pathToInv == ""){
				anim.SetCurve("Inv_" + objName + "/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve());
				anim.SetCurve("Inv_" + objName + "/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve());
			}
			else{
				anim.SetCurve(pathToInv + "/Inv_" + objName + "/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve());
				anim.SetCurve(pathToInv + "/Inv_" + objName + "/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve());
			}
			
			CreateEnable(pathToTemplate, pathToGenerated, pathToInv, objName);
	}

	private void CreateEnable(string pathToTemplate, string pathToGenerated, string pathToInv, string objName){
		string enableDir = pathToGenerated + "/Enable Animations";
		string enableAnimLoc = enableDir + "/" + objName + "_ENABLE.anim";

		if(!Directory.Exists(enableDir)){
			Directory.CreateDirectory(enableDir);
			AssetDatabase.Refresh();
		}
		
		if((AnimationClip)AssetDatabase.LoadAssetAtPath(enableAnimLoc, typeof(AnimationClip)) == null){
			FileUtil.CopyFileOrDirectory(pathToTemplate, enableAnimLoc);
			AssetDatabase.Refresh();
		}

			anim = (AnimationClip)AssetDatabase.LoadAssetAtPath(enableAnimLoc, typeof(AnimationClip));

			if (pathToInv == ""){
				anim.SetCurve("Inv_" + objName + "/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve());
				anim.SetCurve("Inv_" + objName + "/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve());
			}
			else{
				anim.SetCurve(pathToInv + "/Inv_" + objName + "/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve());
				anim.SetCurve(pathToInv + "/Inv_" + objName + "/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve());
			}

			CreateDisable(pathToTemplate, pathToGenerated, pathToInv, objName);
	}

	
	private void CreateDisable(string pathToTemplate, string pathToGenerated, string pathToInv, string objName){
		string disableDir = pathToGenerated + "/Disable Animations";
		string disableAnimLoc = disableDir + "/" + objName + "_DISABLE.anim";

		if(!Directory.Exists(disableDir)){
			Directory.CreateDirectory(disableDir);
			AssetDatabase.Refresh();
		}
		
		if((AnimationClip)AssetDatabase.LoadAssetAtPath(disableAnimLoc, typeof(AnimationClip)) == null){
			FileUtil.CopyFileOrDirectory(pathToTemplate, disableAnimLoc);
			AssetDatabase.Refresh();
		}

			anim = (AnimationClip)AssetDatabase.LoadAssetAtPath(disableAnimLoc, typeof(AnimationClip));
		
			if (pathToInv == ""){
				anim.SetCurve("Inv_" + objName + "/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve());
				anim.SetCurve("Inv_" + objName + "/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve());
			}
			else{
				anim.SetCurve(pathToInv + "/Inv_" + objName + "/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve());
				anim.SetCurve(pathToInv + "/Inv_" + objName + "/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve());
			}
	}

//Helper functions
	// Find File Path
		private string findAssetPath()
		{
			string[] guids1 = AssetDatabase.FindAssets("InvRemapper", null);
			string untouchedString = AssetDatabase.GUIDToAssetPath(guids1[0]);
			string[] splitString = untouchedString.Split('/');

			ArrayUtility.RemoveAt(ref splitString, splitString.Length - 1);
			ArrayUtility.RemoveAt(ref splitString, splitString.Length - 1);

			filePath = string.Join("/", splitString);
			return filePath;
		}
	// Create Disable Curves
		private AnimationCurve disableCurve(){

			Keyframe[] disableKeys = new Keyframe[2];
			Keyframe disableBegin = new Keyframe(0, 0);
			Keyframe disableEnd = new Keyframe(0.5f, 0);
			disableBegin.outTangent = float.PositiveInfinity;
			disableBegin.inTangent = float.NegativeInfinity;
			disableEnd.inTangent = float.NegativeInfinity;
			disableEnd.outTangent = float.PositiveInfinity;
			disableKeys[0] = disableBegin;
			disableKeys[1] = disableEnd;
			AnimationCurve disableCurve = new AnimationCurve(disableKeys);

			return disableCurve;
		}
	// Create Enable Curves
		private AnimationCurve enableCurve(){

			Keyframe[] enableKeys = new Keyframe[2];
			Keyframe enableBegin = new Keyframe(0, 1);
			Keyframe enableEnd = new Keyframe(0.5f, 1);
			enableBegin.outTangent = float.PositiveInfinity;
			enableBegin.inTangent = float.NegativeInfinity;
			enableEnd.inTangent = float.NegativeInfinity;
			enableEnd.outTangent = float.PositiveInfinity;
			enableKeys[0] = enableBegin;
			enableKeys[1] = enableEnd;
			AnimationCurve enableCurve = new AnimationCurve(enableKeys);

			return enableCurve;
		}
//------------------
}
