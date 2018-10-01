using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

public class InvRemapper : EditorWindow {

	private Transform targetPath;
    private AnimationClip anim;
	private string filePath;
	private string animName;


	[MenuItem("Xiexe/Tools/Inventory Remapper")]
    static void Init()
    {
        InvRemapper window = (InvRemapper)GetWindow(typeof(InvRemapper), false, "Inv. Remapper");
		
        window.Show();
    }

	private void OnGUI()
    {
		targetPath = (Transform)EditorGUILayout.ObjectField(new GUIContent("Inventory Location", "Where you want the inventory to be."), targetPath, typeof(Transform), true);
		animName = EditorGUILayout.TextField("Animation Name", animName);

		if(targetPath != null){
			if (GUILayout.Button("Generate"))
        	{
				remapInv(targetPath);
			}
		}
	}

	private void remapInv(Transform target){
		string pathToInv = target.transform.GetHierarchyPath();
		
		string[] splitString = pathToInv.Split('/');
		ArrayUtility.RemoveAt(ref splitString, 0);
		pathToInv = string.Join("/", splitString);



		string pathToEditor = findAssetPath() + "/Editor";
		string pathToTemplate = pathToEditor + "/Templates/BehaviorKeyframeTemplate.anim";
		string pathToGenerated = pathToEditor + "/Generated";

        if (!Directory.Exists(pathToGenerated)) {
            Directory.CreateDirectory(pathToGenerated);
			AssetDatabase.Refresh();
		}

		AddInventory(pathToTemplate, pathToGenerated, pathToInv, target, pathToEditor);

		//Debug.Log("There are " + curves.Length + " curves, or maybe keyframes, who knows, in this animation.");

	}


	private void AddInventory(string pathToTemplate, string pathToGenerated, string pathToInv, Transform target, string pathToEditor){

		Object InvPrefab = (Object)AssetDatabase.LoadAssetAtPath(pathToEditor + "/Prefab/Inv.prefab", typeof(Object));
		//Debug.Log(InvPrefab);
		GameObject invSpawn = Instantiate(InvPrefab, target.position, target.rotation) as GameObject;
		invSpawn.transform.parent = target.transform;
		invSpawn.name = "Inv" + "_" + target.name;
		invSpawn.transform.localScale = new Vector3(1,1,1);

		string InventoryPath = invSpawn.name;
		string pathToInventory = pathToInv + "/" + InventoryPath;

		if(pathToInv == ""){
			pathToInventory = InventoryPath;
		}
		
		//Debug.Log(pathToInv + "/" + InventoryPath);
		//CreateEnableAnims(pathToTemplate, pathToGenerated, InventoryPath);
		
		CreateAnimationFolders(pathToTemplate, pathToGenerated, pathToInventory, InventoryPath);
	}

	private void CreateAnimationFolders(string pathToTemplate, string pathToGenerated, string pathToInventory, string InventoryPath){
		string dir = pathToGenerated + "/" + InventoryPath;
		string enableDir = dir + "/" + "Enable Anims";
		string disableDir = dir + "/" + "Disable Anims";

		if(!Directory.Exists(dir)){
			Directory.CreateDirectory(dir);
		}

		if(!Directory.Exists(enableDir)){
			Directory.CreateDirectory(enableDir);
			Directory.CreateDirectory(disableDir);
		}
		
		CreateEnableAnims(pathToTemplate, pathToGenerated, pathToInventory, enableDir, disableDir);
		AssetDatabase.Refresh();
	}

	private void CreateEnableAnims(string pathToTemplate, string pathToGenerated, string pathToInv, string enableDir, string disableDir){

		int curSlot = 1;
		int numSlots = 7;

		for (int i = 0; i < numSlots; i++){
			FileUtil.CopyFileOrDirectory(pathToTemplate, enableDir + "/" + animName + "_Enable_" + curSlot + ".anim");
			AssetDatabase.Refresh();

			anim = (AnimationClip)AssetDatabase.LoadAssetAtPath(enableDir + "/" + animName + "_Enable_" + curSlot + ".anim", typeof(AnimationClip));
			
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

				anim.SetCurve(pathToInv + "/Inv_" + curSlot + "/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve);
				anim.SetCurve(pathToInv + "/Inv_" + curSlot + "/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve);
				
				curSlot++;
		}
		CreateDisableAnims(pathToTemplate, pathToGenerated, pathToInv, enableDir, disableDir);
	}

	private void CreateDisableAnims(string pathToTemplate, string pathToGenerated, string pathToInv, string enableDir, string disableDir){

		int curSlot = 1;
		int numSlots = 8;

		for (int i = 0; i < numSlots; i++){
			

			if (curSlot == 8){
				FileUtil.CopyFileOrDirectory(pathToTemplate, disableDir + "/" + animName + "_Disable_ALL" + ".anim");
				AssetDatabase.Refresh();
				anim = (AnimationClip)AssetDatabase.LoadAssetAtPath(disableDir + "/" + animName + "_Disable_ALL" + ".anim", typeof(AnimationClip));
			}
			else{
				FileUtil.CopyFileOrDirectory(pathToTemplate, disableDir + "/" + animName + "_Disable_" + curSlot + ".anim");
				AssetDatabase.Refresh();
				anim = (AnimationClip)AssetDatabase.LoadAssetAtPath(disableDir + "/" + animName + "_Disable_" + curSlot + ".anim", typeof(AnimationClip));
			}
			
			
				Keyframe[] enableKeys = new Keyframe[2];
				Keyframe enableBegin = new Keyframe(0, 0);
				Keyframe enableEnd = new Keyframe(0.5f, 0);
				enableBegin.outTangent = float.PositiveInfinity;
				enableBegin.inTangent = float.NegativeInfinity;
				enableEnd.inTangent = float.NegativeInfinity;
				enableEnd.outTangent = float.PositiveInfinity;
				enableKeys[0] = enableBegin;
				enableKeys[1] = enableEnd;
				AnimationCurve enableCurve = new AnimationCurve(enableKeys);

				Keyframe[] disableKeys = new Keyframe[2];
				Keyframe disableBegin = new Keyframe(0, 1);
				Keyframe disableEnd = new Keyframe(0.5f, 1);
				disableBegin.outTangent = float.PositiveInfinity;
				disableBegin.inTangent = float.NegativeInfinity;
				disableEnd.inTangent = float.NegativeInfinity;
				disableEnd.outTangent = float.PositiveInfinity;
				disableKeys[0] = disableBegin;
				disableKeys[1] = disableEnd;
				AnimationCurve disableCurve = new AnimationCurve(disableKeys);

				if(curSlot == 8){
						anim.SetCurve(pathToInv + "/Inv_1/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve);
						anim.SetCurve(pathToInv + "/Inv_1/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve);

						anim.SetCurve(pathToInv + "/Inv_2/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve);
						anim.SetCurve(pathToInv + "/Inv_2/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve);

						anim.SetCurve(pathToInv + "/Inv_3/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve);
						anim.SetCurve(pathToInv + "/Inv_3/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve);

						anim.SetCurve(pathToInv + "/Inv_4/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve);
						anim.SetCurve(pathToInv + "/Inv_4/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve);

						anim.SetCurve(pathToInv + "/Inv_5/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve);
						anim.SetCurve(pathToInv + "/Inv_5/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve);

						anim.SetCurve(pathToInv + "/Inv_6/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve);
						anim.SetCurve(pathToInv + "/Inv_6/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve);

						anim.SetCurve(pathToInv + "/Inv_7/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve);
						anim.SetCurve(pathToInv + "/Inv_7/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve);
					return;
				}

				anim.SetCurve(pathToInv + "/Inv_" + curSlot + "/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve);
				anim.SetCurve(pathToInv + "/Inv_" + curSlot + "/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve);
				
				curSlot++;
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
//------------------
}
