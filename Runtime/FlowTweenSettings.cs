using UnityEngine;
using FlT;

[CreateAssetMenu(fileName = "FlowTweenSettings", menuName = "FlowTween/Settings Asset")]
public class FlowTweenSettings : ScriptableObject
{
    [Header("Easing Defaults")]
    [Tooltip("Default curve shape for all new tweens. Override per-tween with .Quad(), .Spring(), etc.")]
    public Tween.TransitionType defaultTransition = Tween.TransitionType.Linear;

    [Tooltip("Default ease direction for all new tweens. Override per-tween with .EaseIn(), .EaseOut(), etc.")]
    public Tween.EaseType defaultEase = Tween.EaseType.In;

    [Header("Time")]
    [Tooltip("FlowTween-layer time multiplier. Does NOT affect Unity's Time.timeScale. 0 = frozen, 1 = normal.")]
    [Range(0f, 4f)]
    public float globalTimeScale = 1f;

    [Header("Pool — Pre-warm")]
    [Tooltip("Tween objects pre-allocated in the pool before the first frame.")]
    [Range(0, 256)]
    public int prewarmTweens = 32;

    [Tooltip("Sequence objects pre-allocated in the pool before the first frame.")]
    [Range(0, 64)]
    public int prewarmSequences = 8;

    [Header("Pool — Shrink")]
    [Tooltip("The pool is never trimmed below this floor.")]
    [Range(0, 64)]
    public int minPoolSize = 10;

    [Tooltip("Seconds between automatic pool trim passes.")]
    [Range(0.5f, 60f)]
    public float shrinkInterval = 10f;

    [Tooltip("Fraction of excess items removed per trim pass (0 = never shrink, 1 = remove all excess).")]
    [Range(0f, 1f)]
    public float shrinkPercent = 0.25f;

    [Header("Lifecycle")]
    [Tooltip("Automatically kill tweens whose Target belongs to an unloaded scene.")]
    public bool killOnSceneUnload = true;

    [Tooltip("Automatically kill tweens whose Target UnityObject has been destroyed.")]
    public bool autoKillOrphans = true;
}