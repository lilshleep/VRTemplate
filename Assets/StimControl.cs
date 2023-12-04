using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using System.IO;
using System;
using TMPro;

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
    // calculated for 0.5m distance from camera to deg0
    public string[] pos = { "deg0", "deg30", "deg-30" }; // different random positions available (Unity object names)
    public string[] ecc = { "0", "+30", "-30" }; // names to write to csv file, corresponding respectively to pos
    public string[] stimuli = { "face1", "face2", "face3" }; // names of different stimuli (Unity object names)

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
     * Phase 5 = thank you screen / demographics survey reminder
     * in_use = currently going through the change coroutine, has not shown next stimulus yet
     */

    //misc variables
    static string dataPath = Directory.GetCurrentDirectory() + "/Assets/Data/";
    public string logFile; // fileName, set in phase 0 after getting participant name
    Random rnd = new Random();
    private string responseKey = "";
    private string log; // new line of data
    private int instrNum = 0; // index used to increment instructions
    private int ivIndex, stimIndex; // indices for pos and stimuli respectively randomized later in code (need global scope since they're used in multiple functions)
    public GameObject instrText; // text object for instructions
    public TMP_InputField nameInputField; // UI object for name Input
    public string participantID;

    IEnumerator change()
    {
        currentTrial++;
        yield return new WaitForSecondsRealtime(preCue_time); // wait before trial starts
        GameObject.Find("cue").transform.position = GameObject.Find("cuePos").transform.position; // Cue appears at center
        log = DateTimeOffset.Now.ToUnixTimeMilliseconds() + ","; // CueShowTime
        yield return new WaitForSecondsRealtime(cue_time); // Cue stays there for this long

        // randomizes stimulus every round
        ivIndex = rnd.Next(0, pos.Length);
        stimIndex = rnd.Next(0, stimuli.Length);

        // wait time between cue and stimulus
        cueToStim_time = (float)((rnd.NextDouble() * (time_max - time_min)) + time_min);

        GameObject.Find("cue").transform.position = GameObject.Find("disappearPos").transform.position; // Cue disappears
        // waits before showing stimulus
        yield return new WaitForSecondsRealtime(cueToStim_time);

        // shows stimulus
        GameObject.Find(stimuli[stimIndex]).transform.position = GameObject.Find(pos[ivIndex]).transform.position; // StimType appears
        log += DateTimeOffset.Now.ToUnixTimeMilliseconds() + ","; // ObjShowTime
        start = true;
        in_use = false;
    }

    void phase0() // participant name/ID input phase
    {
        // creates data file and sets participant name
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            participantID = nameInputField.text;

            // creates data folder / file
            logFile = dataPath + participantID + "-rtData-" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".csv";
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }
            File.WriteAllText(logFile, "CueShowTime,ObjShowTime,ReactionTime,Eccentricity,StimType,Guess,Correct\n");
            Debug.Log($"Data file started for {nameInputField.text}");
            
            // moves canvas to behind the plane
            GameObject.Find("Canvas").transform.position = GameObject.Find("disappearPos").transform.position; // canvas disappears
            
            // warns if logging has not started
            bool loggingStarted = GameObject.Find("eyeTracking").GetComponent<EyeTrackingControl>().logging;
            if (!loggingStarted)
            {
                Debug.Log("Eye tracking was not started.");
            }

            // sets things up for phase 1
            phase = 1;
            instrText.GetComponent<TextMeshPro>().text = instrTextValues[instrNum];
            instrText.transform.position = GameObject.Find("textPos").transform.position;
            return;
        }
    }

    void phase1() // start and instruction phase
    {
        // moves onto face 1 / instruction 2
        if (Input.GetKeyDown(KeyCode.Space) && instrNum == 0)
        {
            instrNum++;
            instrText.GetComponent<TextMeshPro>().text = instrTextValues[instrNum];
            GameObject.Find("face1").transform.position = GameObject.Find("deg0").transform.position;
        }
        // moves onto face 2 / instruction 3
        if (Input.GetKeyDown(KeyCode.V) && instrNum == 1)
        {
            instrNum++;
            instrText.GetComponent<TextMeshPro>().text = instrTextValues[instrNum];
            GameObject.Find("face1").transform.position = GameObject.Find("disappearPos").transform.position;
            GameObject.Find("face2").transform.position = GameObject.Find("deg0").transform.position;
        }
        // moves onto face 3 / instruction 4
        if (Input.GetKeyDown(KeyCode.B) && instrNum == 2)
        {
            instrNum++;
            instrText.GetComponent<TextMeshPro>().text = instrTextValues[instrNum];
            GameObject.Find("face2").transform.position = GameObject.Find("disappearPos").transform.position;
            GameObject.Find("face3").transform.position = GameObject.Find("deg0").transform.position;
        }
        // describes training rounds and removes face 3
        if (Input.GetKeyDown(KeyCode.N) && instrNum == 3)
        {
            instrNum++;
            instrText.GetComponent<TextMeshPro>().text = instrTextValues[instrNum];
            GameObject.Find("face3").transform.position = GameObject.Find("disappearPos").transform.position;
        }
        // removes instruction text and sets up phase 2
        if (Input.GetKeyDown(KeyCode.Space) && instrNum == 4)
        {
            instrText.transform.position = GameObject.Find("disappearPos").transform.position;
            // setup for phase 2, starts the first training trial
            phase = 2;
            StartCoroutine(change());
        }
    }


    IEnumerator phase2() // training phase
    {
        phase *= -1;
        // checks if a trial is currently running
        if (!in_use)
        {
            // sets response key
            if (Input.GetKeyDown(KeyCode.V)) { responseKey = "face1"; }
            else if (Input.GetKeyDown(KeyCode.B)) { responseKey = "face2"; }
            else if (Input.GetKeyDown(KeyCode.N)) { responseKey = "face3"; }
            // if one of the buttons has been pressed, log data and set up next trial
            if (responseKey != "")
            {
                in_use = true;
                // displays correct or incorrect for the participant
                if (stimuli[stimIndex] == responseKey)
                {
                    instrText.GetComponent<TextMeshPro>().text = "Correct!";
                    instrText.transform.position = GameObject.Find("textPos").transform.position;
                    yield return new WaitForSecondsRealtime((float)1.5);
                    instrText.transform.position = GameObject.Find("disappearPos").transform.position;
                }
                else
                {
                    instrText.GetComponent<TextMeshPro>().text = "Incorrect.";
                    instrText.transform.position = GameObject.Find("textPos").transform.position;
                    yield return new WaitForSecondsRealtime((float)1.5);
                    instrText.transform.position = GameObject.Find("disappearPos").transform.position;
                }
                // removes all stimuli to behind the plane
                for (int k = 0; k < stimuli.Length; k++)
                {
                    GameObject.Find(stimuli[k]).transform.position = GameObject.Find("disappearPos").transform.position;
                }
                // resets response key
                responseKey = "";

                // if the number of training trials specified has been reached, set up phase 3
                if (currentTrial > trainingTrials)
                {
                    // removes correct or incorrect
                    instrText.GetComponent<TextMeshPro>().text = "";
                    instrText.transform.position = GameObject.Find("disappearPos").transform.position;
                    // resets trial counter, and sets up phase 3
                    currentTrial = 1;
                    phase = 3;
                    yield break;
                }
                //starts next trial
                StartCoroutine(change());
            }
        }
        phase *= -1;
    }

    IEnumerator phase3() // break phase
    {
        phase *= -1;
        // shows and updates text for break
        instrText.GetComponent<TextMeshPro>().text = $"Training has finished. The experiment will begin in {countdownTime} seconds";
        instrText.transform.position = GameObject.Find("textPos").transform.position;
        yield return new WaitForSecondsRealtime((float)1);
        countdownTime -= 1;
        // if countdown reaches 0, sets up next phase
        if (countdownTime == 0)
        {
            // removes text
            instrText.GetComponent<TextMeshPro>().text = "";
            instrText.transform.position = GameObject.Find("disappearPos").transform.position;
            // sets up phase 4, starting the first trial
            StartCoroutine(change());
            start = false;
            phase = 4;
            yield break;
        }
        phase *= -1;
    }

    void phase4() // data taking phase
    {
        // checks if a trial is currently running
        if (!in_use)
        {
            // sets response key
            if (Input.GetKeyDown(KeyCode.V)) { responseKey = "face1"; }
            else if (Input.GetKeyDown(KeyCode.B)) { responseKey = "face2"; }
            else if (Input.GetKeyDown(KeyCode.N)) { responseKey = "face3"; }
            // if one of the buttons has been pressed, log data and set up next trial
            if (responseKey != "")
            {
                in_use = true;
                // only logs data after the first trial that started in the last phase
                if (start)
                {
                    // logs data
                    log += DateTimeOffset.Now.ToUnixTimeMilliseconds() + ","; // ReactionTime
                    log += ecc[ivIndex] + "," + stimuli[stimIndex] + "," + responseKey + ","; // independentVar, StimType, Guess
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
                // removes stimuli to behind plane
                for (int k = 0; k < stimuli.Length; k++)
                {
                    GameObject.Find(stimuli[k]).transform.position = GameObject.Find("disappearPos").transform.position;
                }
                // resets the response key
                responseKey = "";
                // if the number of experimental trials specified has been reached, go to the next phase
                if (currentTrial > trials)
                {
                    phase = 5;
                    return;
                }
                StartCoroutine(change());
            }
        }
    }

    IEnumerator phase5() // thank you screen / demographics survey reminder
    {
        phase *= -1;
        // shows text for 2 seconds and ends the protocol
        instrText.GetComponent<TextMeshPro>().text = "Thank you for taking data for us! Please take your demographics survey now";
        instrText.transform.position = GameObject.Find("textPos").transform.position;
        yield return new WaitForSecondsRealtime((float)2);
        UnityEditor.EditorApplication.isPlaying = false;
        phase *= -1;
    }

    void Start()
    {
        // initiates variables
        instrText = GameObject.Find("instrText"); // text object used
        nameInputField = GameObject.Find("nameInputField").GetComponent<TMP_InputField>(); ; // UI object for name Input
    }

    void Update()
    {
        // checks if escape is pressed to end the protocol
        if (Input.GetKey(KeyCode.Escape))
        {
            // this only works in editor view
            UnityEditor.EditorApplication.isPlaying = false;
            // this only works for built programs
            // Application.Quit();
        }
        // runs code depending on which phase is currently ongoing
        else if (phase < 0)
        {
            return;
        }
        else if (phase == 0) // name input
        {
            phase0();
        }
        else if (phase == 1) // in instructions / start phase
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
    void OnApplicationQuit()
    {
        // adds pc info to the 
        // Check if the file exists
        string pcDataFilePath = Directory.GetCurrentDirectory() + "/Assets/Data/runData.csv";
        if (!File.Exists(pcDataFilePath))
        {
            // Create file and write headers
            using (StreamWriter writer = new StreamWriter(pcDataFilePath))
            {
                writer.WriteLine("cpuID,file,Trials");
            }
        }
        // Append the computer name and time to the file
        using (StreamWriter writer = File.AppendText(pcDataFilePath))
        {
            string computerName = SystemInfo.deviceName;
            string pcID = SystemInfo.deviceUniqueIdentifier;
            currentTrial--;

            string nameAndTime = logFile;
            int lastIndex = Math.Max(logFile.LastIndexOf('/'), logFile.LastIndexOf('\\'));
            // If a slash or backslash is found, return the substring from just after it
            if (lastIndex != -1)
            {
                nameAndTime =  logFile.Substring(lastIndex + 1);
            }

            writer.WriteLine($"{computerName},{pcID},{nameAndTime},{currentTrial}");
        }
    }
}