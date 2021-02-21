using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using elZach.EditorHelper;
using System.IO;

public class InstantRecorderWindow : EditorWindow
{
    [MenuItem("Window/Gamejam Tools/Instant Recorder")]
    public static void Init()
    {
        InstantRecorderWindow window = (InstantRecorderWindow)EditorWindow.GetWindow(typeof(InstantRecorderWindow));
        window.titleContent = new GUIContent("Instant Recorder");
        window.minSize = new Vector2(300, 175);
        window.Show();
    }

    static int selectedDeviceIndex;
    int recordingLength = 5;
    int recordingFrequency = 44100;
    bool trimOnSave = true;
    float trimLevel = 350f;

    AudioClip recording;
    static AudioClip savedClip;
    static string folderPath = "Assets/";

    double lockedUntil;

    bool isRecompiling = false;

    private void OnGUI()
    {
        selectedDeviceIndex = EditorGUILayout.Popup(selectedDeviceIndex, Microphone.devices);

        recordingLength = EditorGUILayout.IntField("Time", recordingLength);
        recordingFrequency = EditorGUILayout.IntField("Frequency", recordingFrequency);
        trimOnSave = EditorGUILayout.Toggle("Trim On Save", trimOnSave);
        trimLevel = EditorGUILayout.FloatField("Silence Level", trimLevel);

        if (Microphone.devices.Length > 0)
        {
            if (!Microphone.IsRecording(Microphone.devices[selectedDeviceIndex]))
            {
                if (GUILayout.Button("Record"))
                {
                    recording = Microphone.Start(Microphone.devices[selectedDeviceIndex], false, recordingLength, recordingFrequency);
                    savedClip = null;
                }
            }
            else
            {
                if (GUILayout.Button("Stop Recording"))
                {
                    Microphone.End(Microphone.devices[selectedDeviceIndex]);
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Can't find Microphone Devices.", MessageType.Warning);
            EditorGUI.BeginDisabledGroup(isRecompiling);
            if (GUILayout.Button("Attempt Fix"))
            {
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                void Fix(object o)
                {
                    isRecompiling = false;
                    UnityEditor.Compilation.CompilationPipeline.compilationFinished -= Fix;
                }
                UnityEditor.Compilation.CompilationPipeline.compilationFinished -= Fix;
                UnityEditor.Compilation.CompilationPipeline.compilationFinished += Fix;
                isRecompiling = true;
            }
            EditorGUI.EndDisabledGroup();
        }

        EditorGUI.BeginDisabledGroup(EditorApplication.timeSinceStartup < lockedUntil);

        if ((recording || savedClip))
            if (GUILayout.Button("Play"))
            {
                if (Microphone.devices.Length > 0 && Microphone.IsRecording(Microphone.devices[selectedDeviceIndex]))
                    Microphone.End(Microphone.devices[selectedDeviceIndex]);
                PlayClip(recording ?? savedClip);
                lockedUntil = EditorApplication.timeSinceStartup + (recording ? recording.length : savedClip.length);
            }
        if (recording)
            if (GUILayout.Button("Save Clip"))
            {
                if(Microphone.IsRecording(Microphone.devices[selectedDeviceIndex]))
                    Microphone.End(Microphone.devices[selectedDeviceIndex]);
                string path = EditorUtility.SaveFilePanel("Save Recorded Clip", folderPath, "recording", "wav");
                path = Datahandling.EnsureAssetDataPath(path);
                AudioSave.Save(path, recording, true, trimLevel);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                recording = null;
                savedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                folderPath = path.Substring(0, path.LastIndexOf("/"));
                Debug.Log(folderPath);
            }
        EditorGUI.EndDisabledGroup();
    }

    private const string Clip =
#if UNITY_2020_2_OR_NEWER
            "PreviewClip";
#else
            "Clip";
#endif

    public static void PlayClip(AudioClip clip)
    {
        PlayClip(clip, 0, false);
        return;
    }

    public static void PlayClip(AudioClip clip, int startSample)
    {
        if (!clip) return;

        Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
        Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
        MethodInfo method = audioUtilClass.GetMethod(
            "PlayClip",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new System.Type[] {
                typeof(AudioClip),
                typeof(Int32)
        },
        null
        );
        method.Invoke(
            null,
            new object[] {
                clip,
                startSample
        }
        );
    }

    public static void PlayClip(AudioClip clip, int startSample, bool loop)
    {
        if (!clip) return;
        Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
        Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
        MethodInfo method = audioUtilClass.GetMethod(
            "Play" + Clip,
            BindingFlags.Static | BindingFlags.Public,
            null,
            new System.Type[] {
                typeof(AudioClip),
                typeof(Int32),
                typeof(Boolean)
        },
        null
        );
        method.Invoke(
            null,
            new object[] {
                clip,
                startSample,
                loop
        }
        );
    }
}
