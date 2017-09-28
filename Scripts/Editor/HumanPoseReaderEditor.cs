using UnityEngine;
using UnityEditor;
using System.Collections;
using PoseRecorder;

[CustomEditor(typeof(HumanPoseReader))]
public class HumanPoseReaderEditor : Editor {

    SerializedProperty loadPath;
    SerializedProperty playOnAwake;
    SerializedProperty loadInEditor;

    void OnEnable ()
    {
        loadPath = serializedObject.FindProperty("loadPath");
        playOnAwake = serializedObject.FindProperty("playOnAwake");
        loadInEditor = serializedObject.FindProperty("loadInEditor");
    }

	public override void OnInspectorGUI() {
		serializedObject.Update();

		EditorGUILayout.LabelField("== Path Settings ==");

		if (GUILayout.Button("Set Load Path")) {
            loadPath.stringValue = EditorUtility.OpenFilePanel("Load Pose File ..", "", "pose");
		}
		EditorGUILayout.PropertyField (loadPath);


		EditorGUILayout.Space ();

		EditorGUILayout.LabelField( "== Options ==" );
        EditorGUILayout.PropertyField(playOnAwake);
        EditorGUILayout.PropertyField(loadInEditor);

        serializedObject.ApplyModifiedProperties ();
	}
}
