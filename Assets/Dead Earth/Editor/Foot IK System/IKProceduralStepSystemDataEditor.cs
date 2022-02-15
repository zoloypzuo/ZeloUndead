using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(IKProceduralStepSystemData))]
public class IKProceduralStepSystemDataEditor : Editor
{
    // The component this editor is the UI for
    IKProceduralStepSystemData _script;

    // The propeties for general settings
    SerializedProperty _footBoneFloorOffset;
    SerializedProperty _raycastOriginHeight;
    SerializedProperty _raycastLength;
    SerializedProperty _IKCooldownSpeed;

    // Animated Down Settings
    SerializedProperty _stepDownIKWeight;
    SerializedProperty _stepDownPelvisHeight;
    SerializedProperty _stepDownPelvisAdjustmentSpeed;
    SerializedProperty _stepDownStrideSize;
    SerializedProperty _stepDownStrideWidth;
    SerializedProperty _stepDownPelvisOffset;
    SerializedProperty _stepDownRayOffset;
    SerializedProperty _stepDownSpeedScale;
    SerializedProperty _stepDownFeetSmoothing;
    SerializedProperty _stepDownArcScale;

    // Animated Up Settings
    SerializedProperty _stepUpIKWeight;
    SerializedProperty _stepUpPelvisHeight;
    SerializedProperty _stepUpPelvisAdjustmentSpeed;
    SerializedProperty _stepUpStrideSize;
    SerializedProperty _stepUpStrideWidth;
    SerializedProperty _stepUpPelvisOffset;
    SerializedProperty _stepUpRayOffset;
    SerializedProperty _stepUpSpeedScale;
    SerializedProperty _stepUpFeetSmoothing;
    SerializedProperty _stepUpArcScale;

    private Texture2D _logo = null;

    // Use this for initialization
    void OnEnable()
    {
        _script = (IKProceduralStepSystemData)target as IKProceduralStepSystemData;

        _footBoneFloorOffset            = serializedObject.FindProperty("_footBoneFloorOffset");
        _raycastOriginHeight            = serializedObject.FindProperty("_raycastOriginHeight");
        _raycastLength                  = serializedObject.FindProperty("_raycastLength");
        _IKCooldownSpeed                = serializedObject.FindProperty("_IKCooldownSpeed");

        _stepDownIKWeight               = serializedObject.FindProperty("_stepDownIKWeight");
        _stepDownPelvisHeight           = serializedObject.FindProperty("_stepDownPelvisHeight");
        _stepDownPelvisAdjustmentSpeed  = serializedObject.FindProperty("_stepDownPelvisAdjustmentSpeed");
        _stepDownStrideSize             = serializedObject.FindProperty("_stepDownStrideSize");
        _stepDownStrideWidth            = serializedObject.FindProperty("_stepDownStrideWidth");
        _stepDownPelvisOffset           = serializedObject.FindProperty("_stepDownPelvisOffset");
        _stepDownRayOffset              = serializedObject.FindProperty("_stepDownRayOffset");
        _stepDownSpeedScale             = serializedObject.FindProperty("_stepDownSpeedScale");
        _stepDownFeetSmoothing          = serializedObject.FindProperty("_stepDownFeetSmoothing");
        _stepDownArcScale               = serializedObject.FindProperty("_stepDownArcScale");

        _stepUpIKWeight                 = serializedObject.FindProperty("_stepUpIKWeight");
        _stepUpPelvisHeight             = serializedObject.FindProperty("_stepUpPelvisHeight");
        _stepUpPelvisAdjustmentSpeed    = serializedObject.FindProperty("_stepUpPelvisAdjustmentSpeed");
        _stepUpStrideSize               = serializedObject.FindProperty("_stepUpStrideSize");
        _stepUpStrideWidth              = serializedObject.FindProperty("_stepUpStrideWidth");
        _stepUpPelvisOffset             = serializedObject.FindProperty("_stepUpPelvisOffset");
        _stepUpRayOffset                = serializedObject.FindProperty("_stepUpRayOffset");
        _stepUpSpeedScale               = serializedObject.FindProperty("_stepUpSpeedScale");
        _stepUpFeetSmoothing            = serializedObject.FindProperty("_stepUpFeetSmoothing");
        _stepUpArcScale                 = serializedObject.FindProperty("_stepUpArcScale");

        if (!_logo) _logo = (Texture2D)EditorGUIUtility.Load("Procedural IK Step System/Procedural IK Step System Logo.png");
    }

    public override void OnInspectorGUI()
    {
       // DrawDefaultInspector();

        // Make sure object is up to date
        serializedObject.Update();
        GUIStyle logoStyle = new GUIStyle("label");
        GUILayout.Label(_logo, logoStyle, new GUILayoutOption[] { GUILayout.Width(447), GUILayout.Height(82) });

        EditorGUILayout.Separator();
        EditorGUILayout.HelpBox("General IK Settings", MessageType.None);
        EditorGUILayout.Separator();

        EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Foot Bone Floor Offset"));
            _footBoneFloorOffset.floatValue = EditorGUILayout.FloatField(_footBoneFloorOffset.floatValue);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Ray Origin height"));
            _raycastOriginHeight.floatValue = EditorGUILayout.FloatField(_raycastOriginHeight.floatValue);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent("Raycast Length"));
            _raycastLength.floatValue = EditorGUILayout.FloatField(_raycastLength.floatValue);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("IK -> Animated Cooldown Speed"));
            _IKCooldownSpeed.floatValue = EditorGUILayout.FloatField(_IKCooldownSpeed.floatValue);
        EditorGUILayout.EndHorizontal();

        // Down Settings
        EditorGUILayout.Separator();
        EditorGUILayout.HelpBox("IK Step Down Settings", MessageType.None);
        EditorGUILayout.Separator();


        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent("IK Mode"));
        EditorGUI.BeginChangeCheck();
        IKStepSystemType t = (IKStepSystemType)EditorGUILayout.EnumPopup(_script.GetIKType(IKStepSystemDirection.down));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Undo IK Step System Down Type");
            _script.SetIKType(IKStepSystemDirection.down, t);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("IK Weight"));
            EditorGUILayout.Slider(_stepDownIKWeight, 0, 1, "");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Pelvis height"));
            _stepDownPelvisHeight.floatValue = EditorGUILayout.FloatField(_stepDownPelvisHeight.floatValue);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Pelvis Adjustment Speed"));
            _stepDownPelvisAdjustmentSpeed.floatValue = EditorGUILayout.FloatField(_stepDownPelvisAdjustmentSpeed.floatValue);
        EditorGUILayout.EndHorizontal();

        if (_script.GetIKType(IKStepSystemDirection.down) != IKStepSystemType.Simple)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Stride Size"));
            _stepDownStrideSize.floatValue = EditorGUILayout.FloatField(_stepDownStrideSize.floatValue);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Stride Width"));
            _stepDownStrideWidth.floatValue = EditorGUILayout.FloatField(_stepDownStrideWidth.floatValue);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Pelvis Offset"));
            _stepDownPelvisOffset.floatValue = EditorGUILayout.FloatField(_stepDownPelvisOffset.floatValue);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Foot Ray Offset"));
            _stepDownRayOffset.floatValue = EditorGUILayout.FloatField(_stepDownRayOffset.floatValue);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Speed Scale"));
            _stepDownSpeedScale.floatValue = EditorGUILayout.FloatField(_stepDownSpeedScale.floatValue);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Feet Smoothing"));
            _stepDownFeetSmoothing.floatValue = EditorGUILayout.FloatField(_stepDownFeetSmoothing.floatValue);
            EditorGUILayout.EndHorizontal();

            if (_script.GetIKType(IKStepSystemDirection.down) == IKStepSystemType.AnimatedArcs)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(new GUIContent("Arc Scale"));
                _stepDownArcScale.floatValue = EditorGUILayout.FloatField(_stepDownArcScale.floatValue);
                EditorGUILayout.EndHorizontal();
            }

        }


        // Up Settings
        EditorGUILayout.Separator();
        EditorGUILayout.HelpBox("IK Step Up Settings", MessageType.None);
        EditorGUILayout.Separator();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent("IK Mode"));
        EditorGUI.BeginChangeCheck();
        t = (IKStepSystemType)EditorGUILayout.EnumPopup(_script.GetIKType(IKStepSystemDirection.up));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Undo IK Step System Up  Type");
            _script.SetIKType(IKStepSystemDirection.up, t);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(new GUIContent("IK Weight"));
        EditorGUILayout.Slider(_stepUpIKWeight, 0, 1, "");
        EditorGUILayout.EndHorizontal();

      
        EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Pelvis height"));
            _stepUpPelvisHeight.floatValue = EditorGUILayout.FloatField(_stepUpPelvisHeight.floatValue);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Pelvis Adjustment Speed"));
            _stepUpPelvisAdjustmentSpeed.floatValue = EditorGUILayout.FloatField(_stepUpPelvisAdjustmentSpeed.floatValue);
        EditorGUILayout.EndHorizontal();


        if (_script.GetIKType(IKStepSystemDirection.up) != IKStepSystemType.Simple)
        {
                EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Stride Size"));
            _stepUpStrideSize.floatValue = EditorGUILayout.FloatField(_stepUpStrideSize.floatValue);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Stride Width"));
            _stepUpStrideWidth.floatValue = EditorGUILayout.FloatField(_stepUpStrideWidth.floatValue);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Pelvis Offset"));
            _stepUpPelvisOffset.floatValue = EditorGUILayout.FloatField(_stepUpPelvisOffset.floatValue);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Foot Ray Offset"));
            _stepUpRayOffset.floatValue = EditorGUILayout.FloatField(_stepUpRayOffset.floatValue);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Speed Scale"));
            _stepUpSpeedScale.floatValue = EditorGUILayout.FloatField(_stepUpSpeedScale.floatValue);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Feet Smoothing"));
            _stepUpFeetSmoothing.floatValue = EditorGUILayout.FloatField(_stepUpFeetSmoothing.floatValue);
            EditorGUILayout.EndHorizontal();

            if (_script.GetIKType(IKStepSystemDirection.up) == IKStepSystemType.AnimatedArcs)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(new GUIContent("Arc Scale"));
                _stepUpArcScale.floatValue = EditorGUILayout.FloatField(_stepUpArcScale.floatValue);
                EditorGUILayout.EndHorizontal();
            }
        }

        // Save changes back to object
        serializedObject.ApplyModifiedProperties();
    }
}
