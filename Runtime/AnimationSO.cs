using System;
using System.Collections.Generic;
using UnityEngine;

namespace YustasGames
{
    public class AnimationSO : ScriptableObject
    {
        public float AnimationTime;
        public List<PropertyAnimation> PropertyAnimations = new List<PropertyAnimation>();
    }
    
    [Serializable]
    public class PropertyAnimation
    {
        public AnimationPropertyType Type;
        public string TransformPath;
        public AnimationCurve Curve;

        public bool IsTransformAnimation => Type != AnimationPropertyType.ColliderEnabled &&
                                            Type != AnimationPropertyType.Unknown &&
                                            Type != AnimationPropertyType.SpriteRendererAlpha;
    }

    public enum AnimationPropertyType
    {
        Unknown,
        LocalPosX,
        LocalPosY,
        LocalPosZ,
        LocalRotX,
        LocalRotY,
        LocalRotZ,
        LocalScaleX,
        LocalScaleY,
        LocalScaleZ,
        LocalEulerX,
        LocalEulerY,
        LocalEulerZ,
        IsActive,
        ColliderEnabled,
        SpriteRendererAlpha
    }
}
