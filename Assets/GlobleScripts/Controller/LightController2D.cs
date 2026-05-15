using UnityEngine;
using Genoverrei.Library.Attribute;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

namespace UIEditor
{
    [CreateHierarchyMenu("Prefab/Light")]
    public class LightController2D : MonoBehaviour
    {
        [Header("Light Settings")]
        [Required]
        [SerializeField] protected Light2D Lighting;

        [Header("Animation Settings")]
        [Required]
        [SerializeField] protected Animator LightAnimator;
        [SerializeField] protected float AnimationSpeed = 1f;

        [Header("Random Settings")]
        [SerializeField] protected int LightIndex;
        [SerializeField] protected float GapTime = 0f;
        [SerializeField] protected List<AnimationClip> LightAnimations = new();

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (!Application.isPlaying) Setup();
        }
#endif

        protected virtual void Start()
        {
            Setup();
            if (LightAnimations.Count > 0) StartCoroutine(LightRandom());
        }

        protected virtual void Setup()
        {
            if (!Lighting) TryGetComponent(out Lighting);
            if (!LightAnimator) TryGetComponent(out LightAnimator);
            if (LightAnimator) LightAnimator.speed = AnimationSpeed;
        }

        protected virtual IEnumerator LightRandom()
        {
            while (true)
            {
                LightIndex = Random.Range(0, LightAnimations.Count);
                var clip = LightAnimations[LightIndex];

                if (clip != null)
                {
                    LightAnimator.Play(clip.name, 0, 0f);
                    yield return new WaitForSeconds(clip.length + GapTime);
                }
                else yield return null;
            }
        }
    }
}