using UnityEngine;
using UnityEditor;
using System.Collections;
using PoseRecorder;

[CustomEditor(typeof(HumanPoseRecorder))]
public class HumanPoseRecorderEditor : Editor {

    // save file path
    SerializedProperty savePath;
    SerializedProperty fileName;

    SerializedProperty leftHand;
    SerializedProperty rightHand;

    SerializedProperty startRecordKey;
    SerializedProperty stopRecordKey;

    // change number of saved frames
    SerializedProperty recordLimitedFrames;
    SerializedProperty frameNo;

    void OnEnable ()
    {
		savePath = serializedObject.FindProperty("savePath");
		fileName = serializedObject.FindProperty("fileName");

        leftHand = serializedObject.FindProperty("leftHand");
        rightHand = serializedObject.FindProperty("rightHand");

        startRecordKey = serializedObject.FindProperty("startRecordKey");
		stopRecordKey = serializedObject.FindProperty("stopRecordKey");

		recordLimitedFrames = serializedObject.FindProperty("recordLimitedFrames");
        frameNo = serializedObject.FindProperty("frameNo");
	}

	public override void OnInspectorGUI() {
		serializedObject.Update();

		EditorGUILayout.LabelField("== Path Settings ==");

		if (GUILayout.Button("Set Save Path")) {
			string defaultName = serializedObject.targetObject.name + "-Animation";
			string targetPath = EditorUtility.SaveFilePanelInProject("Save Pose File To ..", defaultName, "pose", "Please select a folder and enter the file name");

			int lastIndex = targetPath.LastIndexOf ("/");
			savePath.stringValue = targetPath.Substring (0, lastIndex + 1);
			string toFileName = targetPath.Substring (lastIndex + 1);

			fileName.stringValue = toFileName;
		}
		EditorGUILayout.PropertyField (savePath);
		EditorGUILayout.PropertyField (fileName);


		EditorGUILayout.Space ();

		// keys setting
		EditorGUILayout.LabelField( "== Control Keys ==" );
        EditorGUILayout.PropertyField(leftHand);
        EditorGUILayout.PropertyField(rightHand);
        EditorGUILayout.PropertyField(startRecordKey);
		EditorGUILayout.PropertyField(stopRecordKey);

		EditorGUILayout.Space ();

		// Other Settings
		EditorGUILayout.LabelField( "== Other Settings ==" );

		// recording frames setting
		recordLimitedFrames.boolValue = EditorGUILayout.Toggle( "Record Limited Frames", recordLimitedFrames.boolValue );

		if (recordLimitedFrames.boolValue)
			EditorGUILayout.PropertyField (frameNo);

		serializedObject.ApplyModifiedProperties ();
	}
}
