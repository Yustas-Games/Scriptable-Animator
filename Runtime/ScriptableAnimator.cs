using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace YustasGames
{
    public class ScriptableAnimator : MonoBehaviour
    {
        public AnimationsDict Animations;

        private Dictionary<string, AnimationRuntimeData> _convertedAnimations = new Dictionary<string, AnimationRuntimeData>();
        public string PlayLoopedOnStart;

        private IEnumerable<string> GetAllAnimations => Animations.Keys;

        private readonly List<AnimationRuntimeData> _runningAnimations = new List<AnimationRuntimeData>();

        private bool _cleanupRequired;

        public void Play(string animationName)
        {
            PlayAnimation(animationName);
        }

        public void PlayLooped(string animationName)
        {
            PlayAnimation(animationName, true);
        }

        public void PlayRandom()
        {
            var randomNameIndex = Random.Range(0, Animations.Keys.Count);
            PlayAnimation(Animations.Keys.ToList()[randomNameIndex]);
        }

        private void Awake()
        {
            foreach (var animation in Animations)
            {
                _convertedAnimations.Add(animation.Key, ConvertAnimation(animation.Value));
            }
        }

        private AnimationRuntimeData ConvertAnimation(AnimationSO animation)
        {
            var result = new AnimationRuntimeData(animation);

            UpdateTransformDataDelegate emptyDelegate = delegate { };

            foreach (var propertyAnimations in animation.PropertyAnimations.GroupBy(p => p.TransformPath))
            {
                var targetTransform = string.IsNullOrEmpty(propertyAnimations.Key) ? transform : transform.Find(propertyAnimations.Key);

                UpdateTransformDataDelegate updateTransformDataDelegate = emptyDelegate;

                foreach (var propertyAnimation in propertyAnimations)
                {
                    if (propertyAnimation.IsTransformAnimation)
                    {
                        updateTransformDataDelegate += UpdateTransformDataDelegateFactory(propertyAnimation.Type, propertyAnimation.Curve.Evaluate);
                        updateTransformDataDelegate -= emptyDelegate;
                    }
                    else
                    {
                        PushComponentUpdateDelegate(result, targetTransform, propertyAnimation.Type, propertyAnimation.Curve.Evaluate);
                    }
                }

                var transformEventPair = new TransformEventPair(targetTransform, updateTransformDataDelegate);

                result.Add(transformEventPair);
            }

            return result;
        }

        private void PushComponentUpdateDelegate(AnimationRuntimeData result, Component c, AnimationPropertyType type, Evaluate evaluate)
        {
            switch (type)
            {
                case AnimationPropertyType.ColliderEnabled:
                {
                    var eventPair = new ColliderEventPair(c.GetComponent<Collider2D>(), (collider, time) =>
                    {
                        collider.enabled = evaluate(time) > 0.5f;
                    });
                    result.Add(eventPair);
                    return;
                }
                
                case AnimationPropertyType.SpriteRendererAlpha:
                {
                    var eventPair = new SpriteRendererEventPair(c.GetComponent<SpriteRenderer>(), (sr, time) =>
                    {
                        var color = sr.color;
                        color.a = evaluate(time);
                        sr.color = color;
                    });
                    result.Add(eventPair);
                    return;
                }

                default:
                    return;
            }
        }

        private void PlayAnimation(string animationName, bool loop = false)
        {
            if (!_convertedAnimations.ContainsKey(animationName))
            {
                Debug.Log($"ScriptableAnimator: requesting to play {animationName}, but clip asset not found.");
                return;
            }

            var animation = _convertedAnimations[animationName];

            animation.Play(loop);

            _runningAnimations.Add(animation);
        }

        private delegate void UpdateTransformDataDelegate(ref TransformData transformData, float time);
        private delegate void UpdateColliderDataDelegate(Collider2D collider, float time);
        private delegate void UpdateSpriteRendererDataDelegate(SpriteRenderer collider, float time);
        private delegate float Evaluate(float time);

        private UpdateTransformDataDelegate UpdateTransformDataDelegateFactory(AnimationPropertyType type, Evaluate evaluate)
        {
            switch (type)
            {
                case AnimationPropertyType.LocalPosX:
                    return (ref TransformData t, float time) => t.Pos.x = evaluate(time);

                case AnimationPropertyType.LocalPosY:
                    return (ref TransformData t, float time) => t.Pos.y = evaluate(time);

                case AnimationPropertyType.LocalPosZ:
                    return (ref TransformData t, float time) => t.Pos.z = evaluate(time);

                case AnimationPropertyType.LocalRotX:
                    return (ref TransformData t, float time) => t.Rot.x = evaluate(time);

                case AnimationPropertyType.LocalRotY:
                    return (ref TransformData t, float time) => t.Rot.y = evaluate(time);

                case AnimationPropertyType.LocalRotZ:
                    return (ref TransformData t, float time) => t.Rot.z = evaluate(time);

                case AnimationPropertyType.LocalScaleX:
                    return (ref TransformData t, float time) => t.Scale.x = evaluate(time);

                case AnimationPropertyType.LocalScaleY:
                    return (ref TransformData t, float time) => t.Scale.y = evaluate(time);

                case AnimationPropertyType.LocalScaleZ:
                    return (ref TransformData t, float time) => t.Scale.z = evaluate(time);

                case AnimationPropertyType.LocalEulerX:
                    return (ref TransformData t, float time) => t.LocalEuler.x = evaluate(time);

                case AnimationPropertyType.LocalEulerY:
                    return (ref TransformData t, float time) => t.LocalEuler.y = evaluate(time);

                case AnimationPropertyType.LocalEulerZ:
                    return (ref TransformData t, float time) => t.LocalEuler.z = evaluate(time);

                case AnimationPropertyType.IsActive:
                    return (ref TransformData t, float time) => t.IsActive = evaluate(time);

                default:
                    return (ref TransformData t, float time) => { };
            }
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(PlayLoopedOnStart))
            {
                PlayLooped(PlayLoopedOnStart);
            }
        }

        private void Update()
        {
            for (var i = 0; i < _runningAnimations.Count; i++)
            {
                _cleanupRequired = _cleanupRequired || _runningAnimations[i].Update(Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            if (!_cleanupRequired)
            {
                return;
            }

            _runningAnimations.RemoveAll(runtimeData => runtimeData.IsEnded());
            _cleanupRequired = false;
        }

        private struct TransformEventPair : IUpdateEventPair
        {
            public Transform Transform;
            public UpdateTransformDataDelegate UpdateTransformData;

            public TransformEventPair(Transform transform, UpdateTransformDataDelegate _updateTransformData)
            {
                Transform = transform;

                UpdateTransformData = _updateTransformData;
            }

            public void SetTime(float currentTime)
            {
                var transformData = new TransformData(Transform);

                UpdateTransformData.Invoke(ref transformData, currentTime);

                transformData.SetTransform(Transform);
            }
        }
        
        private struct ColliderEventPair : IUpdateEventPair
        {
            public Collider2D Collider;
            public UpdateColliderDataDelegate UpdateColliderData;

            public ColliderEventPair(Collider2D collider, UpdateColliderDataDelegate updateColliderData)
            {
                Collider = collider;
                UpdateColliderData = updateColliderData;
            }

            public void SetTime(float currentTime)
            {
                UpdateColliderData.Invoke(Collider, currentTime);
            }
        }
        
        private struct SpriteRendererEventPair : IUpdateEventPair
        {
            public SpriteRenderer SpriteRenderer;
            public UpdateSpriteRendererDataDelegate UpdateSpriteRendererData;

            public SpriteRendererEventPair(SpriteRenderer sr, UpdateSpriteRendererDataDelegate spriteRendererDataDelegate)
            {
                SpriteRenderer = sr;
                UpdateSpriteRendererData = spriteRendererDataDelegate;
            }

            public void SetTime(float currentTime)
            {
                UpdateSpriteRendererData.Invoke(SpriteRenderer, currentTime);
            }
        }

        private interface IUpdateEventPair
        {
            void SetTime(float currentTime);
        }

        private struct TransformData
        {
            public Vector3 Pos;
            public Vector3 Rot;
            public Vector3 Scale;
            public Vector3 LocalEuler;
            public float IsActive;

            public TransformData(Transform transform)
            {
                Pos = transform.localPosition;
                Rot = transform.localRotation.eulerAngles;
                Scale = transform.localScale;
                LocalEuler = transform.localEulerAngles;
                IsActive = transform.gameObject.activeSelf ? 1 : 0;
            }

            public void SetTransform(Transform transform)
            {
                if (transform.localPosition != Pos)
                    transform.localPosition = Pos;

                if (transform.localRotation.eulerAngles != Rot)
                    transform.localRotation = Quaternion.Euler(Rot);

                if (transform.localScale != Scale)
                    transform.localScale = Scale;

                if (transform.localEulerAngles != LocalEuler)
                    transform.localEulerAngles = LocalEuler;

                if (transform.gameObject.activeSelf != (IsActive >= 1))
                    transform.gameObject.SetActive(IsActive >= 1);
            }
        }

        private class AnimationRuntimeData
        {
            
            public float Duration { get { return Animation.AnimationTime; } }
            private bool Loop = false;
            private readonly AnimationSO Animation;
            private float CurrentTime = 0f;            
            private List<IUpdateEventPair> ObjectUpdateCache = new List<IUpdateEventPair>();

            public AnimationRuntimeData(AnimationSO animation)
            {
                Animation = animation;
                //force to use Play()
                CurrentTime = Animation.AnimationTime + 1;
            }

            public bool IsEnded()
            {
                return CurrentTime >= Duration;
            }

            public bool Update(float deltaTime)
            {
                for (int j = 0; j < ObjectUpdateCache.Count; j++)
                {
                    ObjectUpdateCache[j].SetTime(CurrentTime);
                }

                CurrentTime += deltaTime;

                if (!Loop)
                    return IsEnded();

                if (IsEnded())
                    CurrentTime -= Duration;

                return false;
            }

            internal void Add(IUpdateEventPair updateEventPair)
            {
                ObjectUpdateCache.Add(updateEventPair);
            }

            internal void Play(bool loop)
            {
                Loop = loop;

                CurrentTime = 0;
            }
        }
    }
    
    [Serializable]
    public class AnimationsDict : Dictionary<string, AnimationSO>, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        private List<string> _keyData = new List<string>();
	
        [SerializeField, HideInInspector]
        private List<AnimationSO> _valueData = new List<AnimationSO>();

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Clear();
            for (int i = 0; i < _keyData.Count && i < _valueData.Count; i++)
            {
                this[_keyData[i]] = _valueData[i];
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            _keyData.Clear();
            _valueData.Clear();

            foreach (var item in this)
            {
                _keyData.Add(item.Key);
                _valueData.Add(item.Value);
            }
        }
    }
}
