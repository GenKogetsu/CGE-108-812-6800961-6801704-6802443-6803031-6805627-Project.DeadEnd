using UnityEngine;
using Genoverrei.Library.Attribute;
using UnityEngine.Rendering.Universal;

[CreateHierarchyMenu("Prefab/Light")]
public class LightController2D : MonoBehaviour
{
    [Header("Light Settings")]
    [Required]
    [SerializeField] protected Light2D Lighting;

    [Header("Animation Settings")]
    [Required]
    [SerializeField] protected Animator LightAnimator;
    [SerializeField] protected int PatternIndex = 0;
    [SerializeField] protected float Gaptime = 0f;
    [SerializeField] protected List<AnimationClip> LightAnimations;

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (Application.isPlaying) return;
        Setup();
    }
#endif
    protected virtual void Start()
    {
        Setup();
        StartCoroutine(LightRandom());
    }

    protected virtual void Setup()
    {
        if (!Lighting) this.TryGetComponent(out Lighting);
        
        if (!LightAnimator) this.TryGetComponent(out LightAnimator);
        LightAnimations = LightAnimator ? new(LightAnimator.runtimeAnimatorController.animationClips) : new();

    }

    protected virtual IEnumerator LightRandom()
    {
        while (true) 
        {
            PatternIndex = Random.Range(0, LightAnimations.Count);
            LightAnimator.Play(LightAnimations[PatternIndex].name);

            yield return new WaitForSeconds(LightAnimations[PatternIndex].length + Gaptime);
        }
    }
}
