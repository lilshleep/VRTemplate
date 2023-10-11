using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using System.IO;
using System;
using TMPro;

/* TODO
 * add instruction screen
 * add demo sequence
 * add start screen
 * add high score list
 * add stimuli memorization
 * add practice run
 * add break
 * change trials and current trials to flags to check number of trials of each position
 */

public class StimControl : MonoBehaviour
{
    // independent variable being tested
    // calculated for 10m distance from camera to deg0
    public string[] pos = { "deg0", "deg30", "deg-30" };
    public string[] ecc = { "0", "+30", "-30" };

    // counter for finishing the program
    public int currentTrial = 0;
    public int trials = 100;

    // global variables
    public string[] stimuli = { "Face1", "Face2", "Face3" };
    public float preCue_time = (float)0.5; // wait time before cue is shown after trial ends
    public float cue_time = (float)0.2; // time that the cue is on screen
    public float time_min = (float)0.5; // minimum time between cue disappears and stimulus
    public float time_max = (float)1.5; // maximum time between cue disappears and stimulus
    public float cueToStim_time = (float)0;

    static string dataPath = Directory.GetCurrentDirectory() + "/Assets/Data/";
    string logFile = dataPath + "rtData-" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".csv";
    Random rnd = new Random();
    private bool start = false;
    private string log; // new line of data
    private int i, j; // random index
    private bool in_use = false;    // avoid user clicking multiple buttons at same time
    private bool in_training = true;
    private int phase = 0;

    void Start()
    {
        trials += 1;
        if (!Directory.Exists(dataPath))
        {
            Directory.CreateDirectory(dataPath);
        }
        File.WriteAllText(logFile, "CueShowTime,ObjShowTime,ReactionTime,Eccentricity,StimType,Guess,Correct\n");
        GameObject.Find("Text1").transform.position = GameObject.Find("textPos").transform.position;

    }

    IEnumerator change()
    {
        currentTrial++;
        yield return new WaitForSecondsRealtime(preCue_time); // wait before trial starts
        GameObject.Find("Cue").transform.position = GameObject.Find("cuePos").transform.position; // Cue appears at center
        log = DateTimeOffset.Now.ToUnixTimeMilliseconds() + ","; // CueShowTime
        yield return new WaitForSecondsRealtime(cue_time); // Cue stays there for this long

        // randomizes stimulus every round
        i = rnd.Next(0, pos.Length);
        j = rnd.Next(0, stimuli.Length);

        // wait time between cue and stimulus
        cueToStim_time = (float)((rnd.NextDouble() * (time_max - time_min)) + time_min);

        GameObject.Find("Cue").transform.position = GameObject.Find("Disappear").transform.position; // Cue disappears
        // waits before showing stimulus
        yield return new WaitForSecondsRealtime(cueToStim_time);

        // shows stimulus
        GameObject.Find(stimuli[j]).transform.position = GameObject.Find(pos[i]).transform.position; // StimType appears
        log += DateTimeOffset.Now.ToUnixTimeMilliseconds() + ","; // ObjShowTime
        start = true;
        start = true;
        in_use = false;
    }

    void Update()
    {
        string responseKey = "";
        if (in_training) {
            in_use = true;
            if (Input.GetKeyDown(KeyCode.Space) && phase == 0)
            {
                GameObject.Find("Text1").transform.position = GameObject.Find("Disappear").transform.position;
                GameObject.Find("Text2").transform.position = GameObject.Find("textPos").transform.position;
                GameObject.Find("Face1").transform.position = GameObject.Find("deg0").transform.position;
                phase++;
            }
            if (Input.GetKeyDown(KeyCode.V) && phase == 1)
            {
                GameObject.Find("Text2").transform.position = GameObject.Find("Disappear").transform.position;
                GameObject.Find("Face1").transform.position = GameObject.Find("Disappear").transform.position;
                GameObject.Find("Text3").transform.position = GameObject.Find("textPos").transform.position;
                GameObject.Find("Face2").transform.position = GameObject.Find("deg0").transform.position;
                phase++;
            }
            if (Input.GetKeyDown(KeyCode.B) && phase == 2)
            {
                GameObject.Find("Text3").transform.position = GameObject.Find("Disappear").transform.position;
                GameObject.Find("Face2").transform.position = GameObject.Find("Disappear").transform.position;
                GameObject.Find("Text4").transform.position = GameObject.Find("textPos").transform.position;
                GameObject.Find("Face3").transform.position = GameObject.Find("deg0").transform.position;
                phase++;
            }
            if (Input.GetKeyDown(KeyCode.N) && phase == 3)
            {
                GameObject.Find("Text4").transform.position = GameObject.Find("Disappear").transform.position;
                GameObject.Find("Face3").transform.position = GameObject.Find("Disappear").transform.position;
                in_use = false;
                in_training = false;
            }
        }
        else if (!in_use) {
            if (Input.GetKeyDown(KeyCode.V)) { responseKey = "Face1"; }
            else if (Input.GetKeyDown(KeyCode.B)) { responseKey = "Face2"; }
            else if (Input.GetKeyDown(KeyCode.N)) { responseKey = "Face3"; }
            if (responseKey != "")
            {
                in_use = true;
                if (start)
                {
                    log += DateTimeOffset.Now.ToUnixTimeMilliseconds() + ","; // ReactionTime
                    log += ecc[i] + "," + stimuli[j] + "," + responseKey + ","; // independentVar, StimType, Guess
                    if (stimuli[j] == responseKey)
                    {
                        log += "True\n";
                    }
                    else
                    {
                        log += "False\n";
                    }
                    File.AppendAllText(logFile, log);
                    log = "";
                }
                for (int k = 0; k < stimuli.Length; k++)
                {
                    GameObject.Find(stimuli[k]).transform.position = GameObject.Find("Disappear").transform.position;
                }
                StartCoroutine(change());
            }
        }

        if (Input.GetKey(KeyCode.Escape))
        {
            // this only works in editor view
            UnityEditor.EditorApplication.isPlaying = false;
            // this only works for built programs
            // Application.Quit();
        }
        if (currentTrial > trials)
        {
            // this only works in editor view
            UnityEditor.EditorApplication.isPlaying = false;
            // this only works for built programs
            // Application.Quit();
        }
    }
}
