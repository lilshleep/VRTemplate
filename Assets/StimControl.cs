using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using System.IO;
using System;
using TMPro;

/* TODO
 * add high score list
 */

/* NEEDS
 * - to be bound to one and only one object in the code
 * - all names in the first section of variables to be real objects
 *     - and match their case sensitive names
 * - a position to show participant text called textPos
 * - a position to show the cue called cuePos
 * - a object called "Cue"
 */

public class StimControl : MonoBehaviour
{
    // independent variable being tested
    // calculated for 10m distance from camera to deg0
    public string[] pos = { "deg0", "deg30", "deg-30" }; // different random positions available (Unity object names)
    public string[] ecc = { "0", "+30", "-30" }; // names to write to csv file, corresponding respectively to pos
    public string[] stimuli = { "Face1", "Face2", "Face3" }; // names of different stimuli

    // self explanatory
    public string[] instrTextValues = {
    // instruction 1
    @"You will be reacting to three different faces in this protocol, and
        pressing the keys v, b, and n for each one. Please try to react to the
        faces and don't try to anticipate them. Press Spacebar when ready.",
    // instruction 2
    @"This is Face 1. Press v to continue.",
    // instruction 3
    @"This is Face 2. Press b to continue.",
    // instruction 4
    @"This is Face 3. Press n to continue.",
    // instruction 5
    @"Here are some practice rounds to familiarize you with the protocol.
        Press Spacebar to begin.",
    };

    // counter for finishing the program
    public int currentTrial = 1;
    public int trainingTrials = 3;
    public int trials = 5;

    // global variables for time
    public float preCue_time = (float)0.5; // wait time before cue is shown after trial ends
    public float cue_time = (float)0.2; // time that the cue is on screen
    public float time_min = (float)0.5; // minimum time between cue disappears and stimulus    
    public float time_max = (float)1.5; // maximum time between cue disappears and stimulus
    public float cueToStim_time = (float)0; // randomly set later in code

    public int countdownTime = 5; // time between training and experiment phase

    // phase of experiment
    public int phase = 0;
    private bool in_use = false;    // avoid user clicking multiple buttons at same time
    private bool start = false;     // it's the first trial
    /*
     * Phase -1,-2,-3... = in-between phase 1, 2, or 3, while co-routines are in the middle of running
     * Phase 0 = name input
     * Phase 1 = start / instructions
     * Phase 2 = training phase
     * Phase 3 = break 
     * Phase 4 = data taking phase
     * Phase 5 = thank you screen / demographics survey reminder\
     * in_use = currently going through the change coroutine, has not shown next stimulus yet
     */

    //misc variables
    static string dataPath = Directory.GetCurrentDirectory() + "/Assets/Data/";
    string logFile; // fileName, set in phase 0 after getting participant name
    Random rnd = new Random();
    private string responseKey = "";
    private string log; // new line of data
    private int instrNum = 0; // index used to increment instructions
    private int posIndex, stimIndex; // indices for pos and stimuli respectively randomized later in code (need global scope since they're used in multiple functions)
    public GameObject instrText; // text object for instructions
    public GameObject trainingText; // text object for training
    public TMP_InputField nameInputField; // UI object for name Input

    IEnumerator change()
    {
        currentTrial++;
        yield return new WaitForSecondsRealtime(preCue_time); // wait before trial starts
        GameObject.Find("Cue").transform.position = GameObject.Find("cuePos").transform.position; // Cue appears at center
        log = DateTimeOffset.Now.ToUnixTimeMilliseconds() + ","; // CueShowTime
        yield return new WaitForSecondsRealtime(cue_time); // Cue stays there for this long

        // randomizes stimulus every round
        posIndex = rnd.Next(0, pos.Length);
        stimIndex = rnd.Next(0, stimuli.Length);

        // wait time between cue and stimulus
        cueToStim_time = (float)((rnd.NextDouble() * (time_max - time_min)) + time_min);

        GameObject.Find("Cue").transform.position = GameObject.Find("Disappear").transform.position; // Cue disappears
        // waits before showing stimulus
        yield return new WaitForSecondsRealtime(cueToStim_time);

        // shows stimulus
        GameObject.Find(stimuli[stimIndex]).transform.position = GameObject.Find(pos[posIndex]).transform.position; // StimType appears
        log += DateTimeOffset.Now.ToUnixTimeMilliseconds() + ","; // ObjShowTime
        start = true;
        in_use = false;
    }

    void phase0()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            logFile = dataPath + nameInputField.text + "-rtData-" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".csv";
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }
            File.WriteAllText(logFile, "CueShowTime,ObjShowTime,ReactionTime,Eccentricity,StimType,Guess,Correct\n");

            Debug.Log($"Data file started for {nameInputField.text}");
            GameObject.Find("Canvas").transform.position = GameObject.Find("Disappear").transform.position; // canvas disappears

            phase = 1;
            instrText.GetComponent<TextMeshPro>().text = instrTextValues[instrNum];
            instrText.transform.position = GameObject.Find("textPos").transform.position;
            return;
        }
    }

    void phase1() // start and instruction phase
    {
        if (Input.GetKeyDown(KeyCode.Space) && instrNum == 0)
        {
            instrNum++;
            instrText.GetComponent<TextMeshPro>().text = instrTextValues[instrNum];
            GameObject.Find("Face1").transform.position = GameObject.Find("deg0").transform.position;
        }
        if (Input.GetKeyDown(KeyCode.V) && instrNum == 1)
        {
            instrNum++;
            instrText.GetComponent<TextMeshPro>().text = instrTextValues[instrNum];
            GameObject.Find("Face1").transform.position = GameObject.Find("Disappear").transform.position;
            GameObject.Find("Face2").transform.position = GameObject.Find("deg0").transform.position;
        }
        if (Input.GetKeyDown(KeyCode.B) && instrNum == 2)
        {
            instrNum++;
            instrText.GetComponent<TextMeshPro>().text = instrTextValues[instrNum];
            GameObject.Find("Face2").transform.position = GameObject.Find("Disappear").transform.position;
            GameObject.Find("Face3").transform.position = GameObject.Find("deg0").transform.position;
        }
        if (Input.GetKeyDown(KeyCode.N) && instrNum == 3)
        {
            instrNum++;
            instrText.GetComponent<TextMeshPro>().text = instrTextValues[instrNum];
            GameObject.Find("Face3").transform.position = GameObject.Find("Disappear").transform.position;
        }
        if (Input.GetKeyDown(KeyCode.Space) && instrNum == 4)
        {
            instrText.GetComponent<TextMeshPro>().text = instrTextValues[instrNum];
            instrText.transform.position = GameObject.Find("Disappear").transform.position;
            phase = 2;
            StartCoroutine(change());
            start = false;
        }
    }


    IEnumerator phase2() // training phase
    {
        phase *= -1;
        if (!in_use)
        {
            if (Input.GetKeyDown(KeyCode.V)) { responseKey = "Face1"; }
            else if (Input.GetKeyDown(KeyCode.B)) { responseKey = "Face2"; }
            else if (Input.GetKeyDown(KeyCode.N)) { responseKey = "Face3"; }
            if (responseKey != "")
            {
                in_use = true;
                for (int k = 0; k < stimuli.Length; k++)
                {
                    GameObject.Find(stimuli[k]).transform.position = GameObject.Find("Disappear").transform.position;
                }
                if (start)
                {
                    if (stimuli[stimIndex] == responseKey)
                    {
                        trainingText.GetComponent<TextMeshPro>().text = "Correct!";
                        trainingText.transform.position = GameObject.Find("textPos").transform.position;
                        yield return new WaitForSecondsRealtime((float)1.5);
                        trainingText.transform.position = GameObject.Find("Disappear").transform.position;
                    }
                    else
                    {
                        trainingText.GetComponent<TextMeshPro>().text = "Incorrect.";
                        trainingText.transform.position = GameObject.Find("textPos").transform.position;
                        yield return new WaitForSecondsRealtime((float)1.5);
                        trainingText.transform.position = GameObject.Find("Disappear").transform.position;
                    }
                }
                for (int k = 0; k < stimuli.Length; k++)
                {
                    GameObject.Find(stimuli[k]).transform.position = GameObject.Find("Disappear").transform.position;
                }
                responseKey = "";
                if (currentTrial > trainingTrials)
                {
                    trainingText.GetComponent<TextMeshPro>().text = "";
                    trainingText.transform.position = GameObject.Find("textPos").transform.position;
                    currentTrial = 1;
                    phase = 3;
                    start = false;
                    yield break;
                }
                StartCoroutine(change());
            }
        }
        phase *= -1;
    }

    IEnumerator phase3()
    {
        phase *= -1;
        trainingText.GetComponent<TextMeshPro>().text = $"Training has finished. The experiment will begin in {countdownTime} seconds";
        yield return new WaitForSecondsRealtime((float)1);
        countdownTime -= 1;
        phase *= -1;
        if (countdownTime == 0)
        {
            trainingText.GetComponent<TextMeshPro>().text = "";
            trainingText.transform.position = GameObject.Find("Disappear").transform.position;
            StartCoroutine(change());
            phase = 4;
            yield break;
        }
    }
    void phase4()
    {
        if (!in_use)
        {
            if (Input.GetKeyDown(KeyCode.V)) { responseKey = "Face1"; }
            else if (Input.GetKeyDown(KeyCode.B)) { responseKey = "Face2"; }
            else if (Input.GetKeyDown(KeyCode.N)) { responseKey = "Face3"; }
            if (responseKey != "")
            {
                in_use = true;
                if (start)
                {
                    log += DateTimeOffset.Now.ToUnixTimeMilliseconds() + ","; // ReactionTime
                    log += ecc[posIndex] + "," + stimuli[stimIndex] + "," + responseKey + ","; // independentVar, StimType, Guess
                    if (stimuli[stimIndex] == responseKey)
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
                responseKey = "";
                if (currentTrial > trials)
                {
                    phase = 5;
                    return;
                }
                StartCoroutine(change());
            }
        }
    }
    IEnumerator phase5()
    {
        phase *= -1;
        instrText.GetComponent<TextMeshPro>().text = "Thank you for taking data for us! Please take your demographics survey now";
        instrText.transform.position = GameObject.Find("textPos").transform.position;
        yield return new WaitForSecondsRealtime((float)2);
        UnityEditor.EditorApplication.isPlaying = false;
        phase *= -1;
    }

    void Start()
    {
        instrText = GameObject.Find("instrText");
        trainingText = GameObject.Find("trainingText");
        nameInputField = GameObject.Find("nameInput").GetComponent<TMP_InputField>(); ; // UI object for name Input
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            // this only works in editor view
            UnityEditor.EditorApplication.isPlaying = false;
            // this only works for built programs
            // Application.Quit();
        }
        else if (phase < 0)
        {
            return;
        }
        else if (phase == 0) // name input
        {
            phase0();
        }
        else if(phase == 1) // in instructions / start phase
        {
            phase1();
        }
        else if (phase == 2) // in training phase
        {
            StartCoroutine(phase2());
        }
        else if (phase == 3) // break between training and data taking
        {
            StartCoroutine(phase3());
        }
        else if (phase == 4) // in data taking phase
        {
            phase4();
        }
        else if (phase == 5) // thank you / demographics survey reminder
        {
            StartCoroutine(phase5());
        }
    }
}
