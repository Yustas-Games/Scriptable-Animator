using UnityEditor;
using UnityEngine;

namespace YustasGames
{
    public class AnimationConverter : EditorWindow
    {
        [SerializeField] private AnimationClip _clip;

        private const string Path = "Assets";

        [MenuItem("Yustas Games/Animation Clip Converter")]
        private static void CreateAnimationConverter()
        {
            GetWindow<AnimationConverter>();
        }

        private void OnGUI()
        {
            _clip = (AnimationClip)EditorGUILayout.ObjectField("Clip", _clip, typeof(AnimationClip), false);

            if (_clip == null)
            {
                return;
            }
            
            EditorGUILayout.LabelField("Clip float properties:");
            var bindings = AnimationUtility.GetCurveBindings(_clip);
            foreach(var binding in bindings)
            {
                var curve = AnimationUtility.GetEditorCurve(_clip, binding);
                EditorGUILayout.LabelField($"{binding.propertyName}, Keys: {curve.keys.Length}; Type: {binding.type.Name}");
            }

            if (bindings.Length > 0 && GUILayout.Button("Generate scriptable asset"))
            {
                var asset = CreateInstance<AnimationSO>();

                asset.AnimationTime = _clip.length;
                
                foreach (var binding in bindings)
                {
                    var propertyAnimation = new PropertyAnimation
                    {
                        Type = GetPropertyType(binding),
                        TransformPath = binding.path,
                        Curve = AnimationUtility.GetEditorCurve(_clip, binding)
                    };
                        
                    asset.PropertyAnimations.Add(propertyAnimation);
                }

                var assetName = $"{Path}/{_clip.name}.asset";
                var clip = (AnimationSO)AssetDatabase.LoadAssetAtPath(assetName, typeof(AnimationSO));
                if (clip == null)
                {
                    AssetDatabase.CreateAsset(asset, assetName);
                    AssetDatabase.SaveAssets();
                    return;
                }
                
                clip.PropertyAnimations = asset.PropertyAnimations;
                clip.AnimationTime = asset.AnimationTime;
                AssetDatabase.SaveAssets();
            }
        }

        private AnimationPropertyType GetPropertyType(EditorCurveBinding binding)
        {
            switch (binding.propertyName)
            {
                case "m_LocalScale.x":
                    return AnimationPropertyType.LocalScaleX;
                case "m_LocalScale.y":
                    return AnimationPropertyType.LocalScaleY;
                case "m_LocalScale.z":
                    return AnimationPropertyType.LocalScaleZ;
                case "m_LocalPosition.x":
                    return AnimationPropertyType.LocalPosX;
                case "m_LocalPosition.y":
                    return AnimationPropertyType.LocalPosY;
                case "m_LocalPosition.z":
                    return AnimationPropertyType.LocalPosZ;
                case "m_LocalRotation.x":
                    return AnimationPropertyType.LocalRotX;
                case "m_LocalRotation.y":
                    return AnimationPropertyType.LocalRotY;
                case "m_LocalRotation.z":
                    return AnimationPropertyType.LocalRotZ;
                case "localEulerAnglesRaw.x":
                    return AnimationPropertyType.LocalEulerX;
                case "localEulerAnglesRaw.y":
                    return AnimationPropertyType.LocalEulerY;
                case "localEulerAnglesRaw.z":
                    return AnimationPropertyType.LocalEulerZ;
                case "m_IsActive":
                    return AnimationPropertyType.IsActive;
                case "m_Enabled":
                    if (binding.type.IsSubclassOf(typeof(Collider2D)))
                    {
                        return AnimationPropertyType.ColliderEnabled;
                    }
                    else
                    {
                        return AnimationPropertyType.Unknown;
                    }
                case "m_Color.a":
                    return AnimationPropertyType.SpriteRendererAlpha;
                default:
                    return AnimationPropertyType.Unknown;
            }
        }
    }
}
