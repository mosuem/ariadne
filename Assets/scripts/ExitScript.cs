using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExitScript : MonoBehaviour {
    public GameObject hintCanvas;

    public void MainMenuAndSave () {
        Debug.Log ("Saving");
        var level = Camera.main.GetComponent<LevelData> ();
        level.SaveObjects (Statics.levelType, level.levelNumber);
        SceneManager.LoadScene ("mainMenu");
    }

    public void SaveTo (string levelType) {
        Debug.Log ("Saving " + levelType);
        var level = Camera.main.GetComponent<LevelData> ();
        level.SaveObjects (levelType);
        SceneManager.LoadScene ("mainMenu");
        // showSaveButtons();
    }

    public void ShowWindingNumber () {
        Statics.showWindingNumber = !Statics.showWindingNumber;
        hintCanvas.SetActive (Statics.showWindingNumber && Statics.showingHints);
        Statics.hintCanvasActive = Statics.showWindingNumber && Statics.showingHints;
        LevelData.showHint ("Touch a path");
        Misc.flipButtonSprites ("Windung");
    }

    public void checkPaths () {
        Statics.checkPath = !Statics.checkPath;
        Misc.flipButtonSprites ("Check");
        hintCanvas.SetActive (Statics.checkPath && Statics.showingHints);
        Statics.hintCanvasActive = Statics.checkPath && Statics.showingHints;
        LevelData.showHint ("Touch a path");
    }

    public void collapsePath () {
        Statics.retractPath = !Statics.retractPath;
        Misc.flipButtonSprites ("Collapse");
        hintCanvas.SetActive (Statics.retractPath && Statics.showingHints);
        Statics.hintCanvasActive = Statics.retractPath && Statics.showingHints;
        LevelData.showHint ("Touch a path");
    }

    public void MainMenuNoSave () {
        SceneManager.LoadScene ("mainMenu");
    }

    public void LoadScene (string str) {
        Statics.isLoading = false;
        Debug.Log ("Load scene " + str);
        SceneManager.LoadScene (str);
    }

    public void LoadLevelChoice () {
        SceneManager.LoadScene ("levelChoice");
    }

    public void LoadOptions () {
        SceneManager.LoadScene ("optionMenu");
    }

    public void AlgebraButton () {
        if (Statics.showingAlgebra) {
            Statics.showingAlgebra = false;
            GameObject.Find ("Algebra").GetComponentInChildren<Text> ().text = "Hiding Algebra";
        } else {
            Statics.showingAlgebra = true;
            GameObject.Find ("Algebra").GetComponentInChildren<Text> ().text = "Showing Algebra";
        }
    }

    public void FirstPersonButton () {
        if (Statics.firstPerson) {
            Statics.firstPerson = false;
            GameObject.Find ("FirstPerson").GetComponentInChildren<Text> ().text = "Over View";
        } else {
            Statics.firstPerson = true;
            GameObject.Find ("FirstPerson").GetComponentInChildren<Text> ().text = "First Person View";
        }
    }

    public void ShowHintsButton () {
        if (Statics.showingHints) {
            Statics.showingHints = false;
            GameObject.Find ("ShowHints").GetComponentInChildren<Text> ().text = "Hiding Hints";
        } else {
            Statics.showingHints = true;
            GameObject.Find ("ShowHints").GetComponentInChildren<Text> ().text = "Showing Hints";
        }
    }

    public void MeshTypeButton () {
        if (Statics.mesh) {
            Statics.mesh = false;
            GameObject.Find ("MeshType").GetComponentInChildren<Text> ().text = "Family of curves";
        } else {
            Statics.mesh = true;
            GameObject.Find ("MeshType").GetComponentInChildren<Text> ().text = "Filled-In Mesh";
        }
    }

    public void NextLevelButton (int level) {
        Statics.nextSceneNumber = level;
        SceneManager.LoadScene ("all");
    }

    public void setLevel (string levelName) {
        Statics.levelType = levelName;
        Statics.isLoading = true;
        Statics.isSphere = false;
        if (levelName.Equals ("all")) {
            SceneManager.LoadScene ("all");
        } else if (levelName.Equals ("sphere")) {
            Statics.isSphere = true;
            Statics.nextSceneNumber = 1;
            Statics.levelType = "sphere";
            Debug.Log ("levelType:" + Statics.levelType);
            SceneManager.LoadScene ("sphere");
        } else if (levelName.Equals ("torus")) {
            Statics.isTorus = true;
            Statics.nextSceneNumber = 1;
            Statics.levelType = "sphere";
            Debug.Log ("levelType:" + Statics.levelType);
            SceneManager.LoadScene ("sphere");
        } else {
            SceneManager.LoadScene ("levelChoice");
        }
    }

    public void Exit () {
        Application.Quit ();
    }

    public void DeleteLevel (int levelNum) {
        string path = Statics.folderPath + Statics.levelType + "/";
        Debug.Log (Statics.levelType);
        Debug.Log ("Delete " + path + "level" + levelNum + ".dat");
        File.Delete (path + "level" + levelNum + ".dat");
        Debug.Log ("Delete " + path + "level" + levelNum + ".png");
        File.Delete (path + "level" + levelNum + ".png");
    }

    public void drawCircle () {
        Statics.drawCircle = true;
        showDrawButtons ();
    }

    public void drawRectangle () {
        Statics.drawRectangle = true;
        showDrawButtons ();
    }

    public void showDrawButtons () {
        var circle = FindObject ("Circle");
        if (circle.activeSelf) {
            circle.SetActive (false);
            FindObject ("Rectangle").SetActive (false);
        } else {
            circle.SetActive (true);
            FindObject ("Rectangle").SetActive (true);
        }
    }

    public void showDeleteButtons () {
        var DeleteObstacle = FindObject ("DeleteObstacle");
        if (DeleteObstacle.activeSelf) {
            DeleteObstacle.SetActive (false);
            FindObject ("DeleteAll").SetActive (false);
        } else {
            DeleteObstacle.SetActive (true);
            FindObject ("DeleteAll").SetActive (true);
        }
    }

    public void showSaveButtons () {
        var lines = FindObject ("Lines");
        if (lines.activeSelf) {
            lines.SetActive (false);
            FindObject ("Dots").SetActive (false);
            FindObject ("Homotopies").SetActive (false);
        } else {
            lines.SetActive (true);
            FindObject ("Dots").SetActive (true);
            FindObject ("Homotopies").SetActive (true);
        }
    }

    private GameObject FindObject (string name) {
        Transform[] trs = GameObject.Find ("Canvas").GetComponentsInChildren<Transform> (true);
        foreach (Transform t in trs) {
            if (t.name == name) {
                return t.gameObject;
            }
        }
        return null;
    }

    public void DeleteObject () {
        Statics.deleteObstacle = true;
    }

    public void DeleteAll () {
        var level = Camera.main.GetComponent<LevelData> ();
        var dragging = Camera.main.GetComponent<FreeDragging> ();
        if (dragging != null) {
            if (dragging.actHomotopy != null) {
                dragging.actHomotopy.Clear ();
                dragging.actHomotopy = null;
            }
        } else {
            var dragging3D = Camera.main.GetComponent<FreeDragging3D> ();
            if (dragging3D != null) {
                if (dragging3D.actHomotopy != null) {
                    dragging3D.actHomotopy.Clear ();
                    dragging3D.actHomotopy = null;
                }
            }
        }
        foreach (var path in level.paths) {
            Destroy (path);
        }
        foreach (var dot in level.dots) {
            GameObject.Destroy (dot);
        }
        foreach (var obstacle in level.statics) {
            GameObject.Destroy (obstacle);
        }
        level.paths.Clear ();
        level.dots.Clear ();
        level.statics.Clear ();
    }

    public void CutPath () {
        Statics.cutPath = true;
    }

    public void GluePaths () {
        Statics.gluePaths = true;
    }

    public void Undo () {
        var level = Camera.main.GetComponent<LevelData> ();
        MType lastType = level.PopLast ();
        if (lastType == MType.Dot) {
            var last = level.dots.Last ();
            level.dots.Remove (last);
            Destroy (last);
        } else if (lastType == MType.Path) {
            var dragging = Camera.main.GetComponent<FreeDragging> ();
            var dragging3D = Camera.main.GetComponent<FreeDragging3D> ();
            if (dragging != null) {
                if (dragging.actHomotopy != null) {
                    dragging.actHomotopy.Clear ();
                    dragging.actHomotopy = null;
                }
            } else if (dragging3D != null) {
                if (dragging3D.actHomotopy != null) {
                    dragging3D.actHomotopy.Clear ();
                    dragging3D.actHomotopy = null;
                }
            }
            var last = level.paths.Last ();
            level.paths.Remove (last);
            Destroy (last);
        } else if (lastType == MType.Obstacle) {
            var last = level.statics.Last ();
            level.statics.Remove (last);
            Destroy (last);
        }
    }

}