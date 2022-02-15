using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class EditorBootstrapper
{
    private static EditorWindow Window = null;

    static EditorBootstrapper() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update += Startup;
    }

    static void Startup() {
        EditorApplication.update -= Startup;


        // Do your stuff here,
        Window = EditorWindow.GetWindow<WelcomeMessage>();
        Window.maxSize = new Vector2(1000, 650);
        Window.minSize = new Vector2(1000, 650);
        Window.titleContent = new GUIContent("Welcome - Dead Earth");
        Window.Show();
    }
}

public class WelcomeMessage : EditorWindow
{
    private Transform _fromObject = null;
    private Transform _toObject = null;
    private Texture2D _logo = null;

    string Message =
        "Hi Everyone,\n\n" +
        "As we move into Part III of Dead Earth Development, I thought I would release my current version of the project which has all of the code and systems in-place that we will be developing step-by-step over the coming videos." +
        "\n\n" +
        "As many of you will know, before I teach a new system in the videos, I first implement it in my own prototype project - <b>This is that project</b>.\n\nDon't worry though, I will still be building all of this stuff - step-by-step - in the coming videos so you won't be left alone to figure out how all the new code in this project works. " +
        "But for those of you who can't wait to start exploring the <B>FPS Arms & Weapons System</B> or the <B>Procedural IK Systems</B> (used to help our Zombies traverse steps and open doors) - this project release is for you.\n\n" +
        "<B>PLEASE NOTE:</B>\n\nBecause I will be covering all this new code over the next batch of videos, I will not be giving detailed explanations of how this code works on the Forums - this release is for people who feel confident digging through the code themselves.\n\n" +
        "Updates and Changes in this Project\n\n" +
        "<size=10>> Complete FPS Arms and Weapons System.\n\n" +
        "> AI Procedural Foot IK Sytem for Steps with Custom Editor.\n\n" +
        "> AI Door System with Head and Hand IK.\n\n" +
        "> MANY Bug Fixes and Additions made to the AI State Machine Framework to add support for additional functionality.\n\n" +
        "> Two Sets of Baked Reflection Probes - For Power and No Power Hospital.\n\n" +
        "> Baked NavMesh with <B>Special Geometry</B> to help shape the NavMesh for Steps and Non-Crawl areas.\n\n" +
        "> Updated Prop Prefabs. Colliders have been coverted to either Convex Mesh or Box Colliders to provide robust collision of Discarded / Dropped items.\n\n" +
        "> and much more</size>\n\n\n" +
        "Warmest Regards\n" +
        "<B>Gary Simmons</B>\n" +
        "<I>Course Instructor</I>\n\n<size=8><b>Note:</b> You can stop this message from showing by deleting the 'Welcome Message.cs' script from the Assets/Dead Earth/Editor folder.</size>";

    GUIStyle style = new GUIStyle();

    [MenuItem("Welcome Message/Display Welcome Dialog")]
    static void Init() {
        // Get existing open window or if none, make a new one:
        WelcomeMessage window = (WelcomeMessage) EditorWindow.GetWindow(typeof(WelcomeMessage));
        window.Show();
    }

    private void OnEnable() {
        if (!_logo)
            _logo = (Texture2D) EditorGUIUtility.Load("Welcome Message/Welcome Message.png");
    }

    void OnGUI() {
        style.wordWrap = true;
        style.padding = new RectOffset(20, 20, 20, 20);
        style.richText = true;

        GUIStyle logoStyle = new GUIStyle("label");
        GUILayout.Label(_logo, logoStyle, new GUILayoutOption[] {GUILayout.Width(1000), GUILayout.Height(100)});


        EditorGUILayout.Separator();

        EditorGUILayout.TextArea(Message, style, GUILayout.Height(300));

        EditorGUILayout.Separator();
        EditorGUILayout.Separator();
        if (GUI.Button(new Rect(870, 620, 100, 20), "Close")) {
            Close();
        }
    }
}