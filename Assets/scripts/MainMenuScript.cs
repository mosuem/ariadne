using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class MainMenuScript : MonoBehaviour
{
    // Use this for initialization
    public Texture2D tex;

    void Awake()
    {
        Statics.folderPath = Application.persistentDataPath + "/" + "MyFiles" + "/";
        // Statics.folderPath = Application.streamingAssetsPath + "\\";
        XRSettings.enabled = false;
        var levelTypes = new string[] { "dots", "homotopies", "paths" };
        foreach (var levelType in levelTypes)
        {
            string folderPath = Statics.folderPath + levelType;
            var folder = Directory.CreateDirectory(folderPath);
            var filesDots = Misc.GetFiles(levelType, "dat");
            if (filesDots.Count == 0)
            {
                Debug.Log("No Files found");
                GameObject gameObject1 = GameObject.Find(levelType);
                if (gameObject1 != null)
                {
                    gameObject1.GetComponent<Button>().interactable = false;
                }
            }
        }
    }

    void Start()
    {
        GameObject myEventSystem = GameObject.Find("EventSystem");
        myEventSystem.GetComponent<UnityEngine.EventSystems.EventSystem>().SetSelectedGameObject(null);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnGUI()
    {

        if (Statics.isLoading)
        {
            //GUI.Label (new Rect (0, 0, Screen.currentResolution.width, Screen.currentResolution.height), "Loading...", guiStyle);
            GUI.Window(0, new Rect(0, 0, Screen.currentResolution.width, Screen.currentResolution.height), DoMyWindow, "", GUI.skin.GetStyle("window"));
        }
    }

    void DoMyWindow(int windowID)
    {
        GUIStyle guiStyle = GUI.skin.GetStyle("button");
        guiStyle.fontSize = 60;
        guiStyle.alignment = TextAnchor.MiddleCenter;
        Color color = Color.white;
        GUI.contentColor = color;
        var width = 400;
        var height = 100;
        GUI.Box(new Rect(Screen.currentResolution.width / 2 - width / 2, Screen.currentResolution.height / 2 - height / 2, width, height), "Loading...", guiStyle);
    }

}
