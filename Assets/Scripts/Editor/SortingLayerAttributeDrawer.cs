using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(SortingLayerAttribute))]
public class SortingLayerAttributeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var layers = SortingLayer.layers;
        var names = new string[layers.Length];
        for (int i = 0; i < layers.Length; i++)
            names[i] = layers[i].name;

        var currentIdx = System.Array.FindIndex(layers, l => l.id == property.intValue);
        if (currentIdx < 0) currentIdx = 0;

        var selectedIdx = EditorGUI.Popup(position, label.text, currentIdx, names);
        property.intValue = layers[selectedIdx].id;
    }
}
