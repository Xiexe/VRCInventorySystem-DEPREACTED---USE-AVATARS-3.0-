using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

public class InvRemapper : EditorWindow
{
    private int itemAmount = 1;
    private Transform[] targetPath = new Transform[7];

    private Animator avatar;
    private bool[] enableByDefault = new bool[7];

    [MenuItem("Xiexe/Tools/Inventory Remapper")]
    static void Init()
    {
        InvRemapper window = (InvRemapper)GetWindow(typeof(InvRemapper), false, "Inv. Remapper");
        window.minSize = new Vector2(350, 299);
        window.maxSize = new Vector2(350, 300);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Space(8);
        doLabel("Avatar", 12, TextAnchor.MiddleCenter);
        avatar = (Animator)EditorGUILayout.ObjectField(new GUIContent("Avatar: ", "Your avatar."), avatar, typeof(Animator), true);

        if (avatar == null)
        {
            EditorGUILayout.HelpBox("You must assign an Avatar to generate the inventory on.", MessageType.Warning);
        }

        if (avatar != null)
        {
            GUILayout.Space(8);
            doLabel("Inventory", 12, TextAnchor.MiddleCenter);
            itemAmount = EditorGUILayout.IntSlider("Inventory Size: ", itemAmount, 1, 7);


            GUILayout.Space(10);
            doLabel("Enable by Default", 10, TextAnchor.MiddleRight);
            for (int i = 0; i < itemAmount; i++)
            {
                EditorGUILayout.BeginHorizontal();
                targetPath[i] = (Transform)EditorGUILayout.ObjectField(new GUIContent("Item " + (i + 1) + ": ", "The Item you want to be in your inventory."), targetPath[i], typeof(Transform), true, GUILayout.Width(300));
                GUILayout.FlexibleSpace();
                enableByDefault[i] = EditorGUILayout.Toggle("", enableByDefault[i], GUILayout.Width(15));
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Generate Inventory"))
            {
                for (int j = 0; j < targetPath.Length; j++)
                {
                    remapInv(targetPath[j], enableByDefault[j]);
                }
            }
        }
    }

    private static string GetGameObjectPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

    private void remapInv(Transform target, bool enableDefault)
    {
        //Bail if the target is null.
        if (target == null)
        {
            return;
        }

        string pathToInv = GetGameObjectPath(target.transform);
        string[] splitString = pathToInv.Split('/');

        ArrayUtility.RemoveAt(ref splitString, 0);
        ArrayUtility.RemoveAt(ref splitString, splitString.Length - 1);

        pathToInv = string.Join("/", splitString);

        string assetPath = findAssetPath();
        string pathToEditor = assetPath + "/Editor";
        string pathToAnimFolder = assetPath + "/Animations";
        string pathToTemplate = pathToEditor + "/Templates/BehaviorKeyframeTemplate.anim";
        string pathToGenerated = pathToAnimFolder + "/" + avatar.name;

        if (!Directory.Exists(pathToGenerated))
        {
            Directory.CreateDirectory(pathToGenerated);
            AssetDatabase.Refresh();
        }

        CreateInvAndMoveObject(pathToTemplate, pathToGenerated, pathToInv, pathToEditor, target, enableDefault);
    }

    private void CreateInvAndMoveObject(string pathToTemplate, string pathToGenerated, string pathToInv, string pathToEditor, Transform target, bool enableDefault)
    {

        //Make sure we don't allow you to generate an inventory slot within an inventory slot.
        if (target.transform.parent.name == "Object")
        {

            switch (enableDefault)
            {
                case true:
                    target.transform.parent.gameObject.SetActive(true);
                    break;

                case false:
                    target.transform.parent.gameObject.SetActive(false);
                    break;
            }
            return;
        }

        //Load the prefab for the inventory slot.
        Object slotPrefab = (Object)AssetDatabase.LoadAssetAtPath(pathToEditor + "/Prefab/Inv_Single_Slot.prefab", typeof(Object));

        //Instantiate the prefab, set the parent to your items parent, set the name to match your item, and set the scale to 1.
        GameObject invSpawn = Instantiate(slotPrefab, target.position, target.rotation) as GameObject;
        invSpawn.transform.parent = target.transform.parent;
        invSpawn.name = "Inv_" + target.name;
        invSpawn.transform.localScale = new Vector3(1, 1, 1);

        //Get the Object child of the prefab, and set the parent of your target item to the object slot.
        Transform objectSlot = invSpawn.transform.GetChild(0).GetChild(0).GetChild(0);
        target.transform.parent = objectSlot;

        //If you want the object to be enabled by default, set the object slot to active.
        if (enableDefault)
        {
            objectSlot.transform.gameObject.SetActive(true);
        }

        //Call create Globals
        CreateGlobalDisable(pathToTemplate, pathToGenerated, pathToInv, target.name);
    }

    //Create the Global Disable animation
    private void CreateGlobalDisable(string pathToTemplate, string pathToGenerated, string pathToInv, string objName)
    {
        //Out directory, and our disableAll animation path
        string globalDir = pathToGenerated + "/Global Animations";
        string globalAnimLoc = globalDir + "/DISABLE_ALL - " + avatar.name + ".anim";

        //If the global directory doesn't exit, we need to create it.
        if (!Directory.Exists(globalDir))
        {
            Directory.CreateDirectory(globalDir);
            AssetDatabase.Refresh();
        }

        //Same as above but with the global animation file
        if ((AnimationClip)AssetDatabase.LoadAssetAtPath(globalAnimLoc, typeof(AnimationClip)) == null)
        {
            FileUtil.CopyFileOrDirectory(pathToTemplate, globalAnimLoc);
            AssetDatabase.Refresh();
        }

        AnimationClip anim = (AnimationClip)AssetDatabase.LoadAssetAtPath(globalAnimLoc, typeof(AnimationClip));

        if (pathToInv == "")
        {
            anim.SetCurve("Inv_" + objName + "/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve());
            anim.SetCurve("Inv_" + objName + "/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve());
        }
        else
        {
            anim.SetCurve(pathToInv + "/Inv_" + objName + "/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve());
            anim.SetCurve(pathToInv + "/Inv_" + objName + "/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve());
        }

        CreateGlobalEnable(pathToTemplate, pathToGenerated, pathToInv, objName, globalDir);
    }

    private void CreateGlobalEnable(string pathToTemplate, string pathToGenerated, string pathToInv, string objName, string globalDir)
    {
        string globalAnimLoc = globalDir + "/ENABLE_ALL - " + avatar.name + ".anim";

        if (!Directory.Exists(globalDir))
        {
            Directory.CreateDirectory(globalDir);
            AssetDatabase.Refresh();
        }

        if ((AnimationClip)AssetDatabase.LoadAssetAtPath(globalAnimLoc, typeof(AnimationClip)) == null)
        {
            FileUtil.CopyFileOrDirectory(pathToTemplate, globalAnimLoc);
            AssetDatabase.Refresh();
        }

        AnimationClip anim = (AnimationClip)AssetDatabase.LoadAssetAtPath(globalAnimLoc, typeof(AnimationClip));

        if (pathToInv == "")
        {
            anim.SetCurve("Inv_" + objName + "/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve());
            anim.SetCurve("Inv_" + objName + "/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve());
        }
        else
        {
            anim.SetCurve(pathToInv + "/Inv_" + objName + "/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve());
            anim.SetCurve(pathToInv + "/Inv_" + objName + "/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve());
        }

        CreateEnable(pathToTemplate, pathToGenerated, pathToInv, objName);
    }

    private void CreateEnable(string pathToTemplate, string pathToGenerated, string pathToInv, string objName)
    {
        string enableDir = pathToGenerated + "/Enable Animations";
        string enableAnimLoc = enableDir + "/" + objName + "_ENABLE.anim";

        if (!Directory.Exists(enableDir))
        {
            Directory.CreateDirectory(enableDir);
            AssetDatabase.Refresh();
        }

        if ((AnimationClip)AssetDatabase.LoadAssetAtPath(enableAnimLoc, typeof(AnimationClip)) == null)
        {
            FileUtil.CopyFileOrDirectory(pathToTemplate, enableAnimLoc);
            AssetDatabase.Refresh();
        }

        AnimationClip anim = (AnimationClip)AssetDatabase.LoadAssetAtPath(enableAnimLoc, typeof(AnimationClip));

        if (pathToInv == "")
        {
            anim.SetCurve("Inv_" + objName + "/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve());
            anim.SetCurve("Inv_" + objName + "/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve());
        }
        else
        {
            anim.SetCurve(pathToInv + "/Inv_" + objName + "/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve());
            anim.SetCurve(pathToInv + "/Inv_" + objName + "/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve());
        }

        CreateDisable(pathToTemplate, pathToGenerated, pathToInv, objName);
    }


    private void CreateDisable(string pathToTemplate, string pathToGenerated, string pathToInv, string objName)
    {
        string disableDir = pathToGenerated + "/Disable Animations";
        string disableAnimLoc = disableDir + "/" + objName + "_DISABLE.anim";

        if (!Directory.Exists(disableDir))
        {
            Directory.CreateDirectory(disableDir);
            AssetDatabase.Refresh();
        }

        if ((AnimationClip)AssetDatabase.LoadAssetAtPath(disableAnimLoc, typeof(AnimationClip)) == null)
        {
            FileUtil.CopyFileOrDirectory(pathToTemplate, disableAnimLoc);
            AssetDatabase.Refresh();
        }

        AnimationClip anim = (AnimationClip)AssetDatabase.LoadAssetAtPath(disableAnimLoc, typeof(AnimationClip));

        if (pathToInv == "")
        {
            anim.SetCurve("Inv_" + objName + "/ENABLE", typeof(UnityEngine.Behaviour), "m_Enabled", disableCurve());
            anim.SetCurve("Inv_" + objName + "/ENABLE/DISABLE", typeof(UnityEngine.Behaviour), "m_Enabled", enableCurve());
        }
        else
        {
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

        string filePath = string.Join("/", splitString);
        return filePath;
    }

    private AnimationCurve CreateConstantCurve(float value)
    {
        Keyframe[] keys = new Keyframe[2];
        Keyframe begin = new Keyframe(0, value);
        Keyframe end = new Keyframe(0.5f, value);
        begin.outTangent = float.PositiveInfinity;
        begin.inTangent = float.NegativeInfinity;
        end.inTangent = float.NegativeInfinity;
        end.outTangent = float.PositiveInfinity;
        keys[0] = begin;
        keys[1] = end;
        AnimationCurve curve = new AnimationCurve(keys);
        return curve;
    }

    // Create Disable Curves
    private AnimationCurve disableCurve()
    {
        return CreateConstantCurve(0f);
    }
    // Create Enable Curves
    private AnimationCurve enableCurve()
    {
        return CreateConstantCurve(1f);
    }

    //GuiLabel
    public static void doLabel(string text, int textSize, TextAnchor anchor)
    {
        GUILayout.Label(text, new GUIStyle(EditorStyles.label)
        {
            alignment = anchor,
            wordWrap = true,
            fontSize = textSize
        });
    }
    //------------------
}
