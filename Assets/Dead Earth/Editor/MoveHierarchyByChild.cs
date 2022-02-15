using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MoveHierarchyByChild : EditorWindow
{
    private static EditorWindow Window = null;
    private Transform _fromObject = null;
    private Transform  _toObject = null;


    [MenuItem("GameObject/+Move Hierarchy By Child")]
    static void Init()
    {
        Window = EditorWindow.GetWindow<MoveHierarchyByChild>();
        Window.maxSize = new Vector2(350, 260);
        Window.minSize = new Vector2(350, 259);
        Window.titleContent = new GUIContent("Move Hierarchy By Child");
        Window.Show();
    }

    void OnGUI()
    {

        _fromObject  = (Transform)EditorGUILayout.ObjectField("Child To Move", _fromObject, typeof(Transform), true);
        _toObject    = (Transform)EditorGUILayout.ObjectField("Destination Transform", _toObject, typeof(Transform), true);

        if (_fromObject != null && _toObject != null)
        {
            if (GUILayout.Button("Perform Move Hierarchy", GUILayout.ExpandWidth(true)))
            {
                Vector3 targetPosition = _toObject.position;
                Vector3 parentPosition = _fromObject.root.position;
                Vector3 amountToOffset = targetPosition - _fromObject.position;

                parentPosition += amountToOffset;
                _fromObject.root.position = parentPosition;
            }

        }
    }
}
