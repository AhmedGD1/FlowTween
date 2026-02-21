# FlowTween

A fast, zero-allocation tweening library for Unity built around struct-based interpolators and object pooling. Drop it in and animate anything — no setup required.

---

## Features

- **Zero allocations** on the hot path via struct interpolators and typed pools
- **Auto-initializes** — no scene setup, no prefabs, just call and go
- **Sequences** with `Append`, `Join`, `Insert`, `Prepend`, and `AppendInterval`
- **12 transition types** — Sine, Quad, Cubic, Quart, Quint, Expo, Circ, Back, Elastic, Bounce, Spring, Linear
- **4 ease modes** — In, Out, InOut, OutIn
- **Custom AnimationCurve** support
- **Groups** — pause, resume, or kill tweens by tag
- **Scene-aware cleanup** — tweens targeting destroyed scene objects are auto-removed
- **Shake & Punch** effects built in for 2D and 3D
- **FlowVirtual** — tween any value with a callback (float, int, Vector2, Vector3, Color)
- **Yoyo & Restart** loop modes with infinite loop support
- **Unscaled time** support for UI animations during pause screens

---

## Installation

**Unity Package Manager (Git URL)**
```
https://github.com/yourusername/FlowTween.git
```

**Manual**
Copy the `FlowTween` folder into your project's `Assets` directory.

> Requires Unity 2021.3 LTS or newer and TextMeshPro for text reveal support.

---

## Quick Start

No setup needed. FlowTween initializes itself before your first scene loads.

```csharp
// Move
transform.FlowMove(new Vector3(0, 5, 0), duration: 1f).EaseOut().Sine();

// Fade
canvasGroup.FlowFadeOut(duration: 0.5f);

// Scale
transform.FlowScale(Vector3.one * 1.5f, duration: 0.3f).Back().EaseOut();

// Any value
FlowVirtual.Float(0f, 1f, duration: 2f, value => myMaterial.SetFloat("_Dissolve", value));
```

---

## Transitions & Easing

Chain transition and ease methods directly on any tween:

```csharp
transform.FlowMove(target, 1f)
    .Elastic()   // TransitionType
    .EaseOut();  // EaseType
```

| Transitions | Ease Modes |
|-------------|------------|
| `Linear()` `Sine()` `Quad()` `Cubic()` | `EaseIn()` |
| `Quart()` `Quint()` `Expo()` `Circ()` | `EaseOut()` |
| `Back()` `Elastic()` `Bounce()` | `EaseInOut()` |
| `Spring()` | `EaseOutIn()` |

**Custom curve:**
```csharp
transform.FlowMove(target, 1f).SetCurve(myAnimationCurve);
```

---

## Tween Options

```csharp
transform.FlowMove(target, 1f)
    .SetDelay(0.5f)               // wait before starting
    .SetLoops(3, LoopType.Yoyo)   // loop with ping-pong
    .SetLoops(-1)                 // infinite loop
    .SetFrom(Vector3.zero)        // override start value
    .SetRelative()                // treat 'to' as offset from current
    .SetTimeScale(2f)             // local time scale
    .SetUnscaledTime()            // ignore Time.timeScale
    .SetId("my-tween")            // identifier for lookup
    .SetGroup("ui")               // group tag
    .OnStart(() => { })
    .OnUpdate(t => { })
    .OnComplete(() => { })
    .OnLoop(count => { });
```

---

## Sequences

```csharp
FlowTween.Sequence()
    .Append(transform.FlowMove(posA, 0.5f).Sine().EaseOut())
    .Append(transform.FlowScale(Vector3.one * 1.2f, 0.3f).Back().EaseOut())
    .Join(canvasGroup.FlowFadeOut(0.3f))         // runs in parallel with previous
    .AppendInterval(0.2f)                         // wait
    .AppendCallback(() => Debug.Log("Done!"))
    .Insert(0f, bgImage.FlowFade(0f, 1f))         // at explicit time
    .SetLoops(2)
    .OnComplete(() => Debug.Log("Sequence complete"))
    .Play();
```

**Sequence control:**
```csharp
Sequence seq = FlowTween.Sequence()...Play();

seq.Pause();
seq.Resume();
seq.Kill();
```

---

## Chaining Tweens

Run one tween after another completes:

```csharp
transform.FlowMove(posA, 1f)
    .Then(transform.FlowMove(posB, 1f));
```

---

## Shake & Punch

```csharp
// 2D shake (XY plane)
transform.FlowShake2D(duration: 0.5f, strength: 0.3f);
transform.FlowShakeRotation2D(duration: 0.5f, strength: 10f);

// 3D shake
transform.FlowShake3D(duration: 0.5f, strength: 0.5f, randomness: 90f);
transform.FlowShakeRotation3D(duration: 0.5f, strength: 15f);

// Punch (springs back to origin)
transform.FlowPunchPosition2D(new Vector2(0, 1), duration: 0.4f, vibrato: 10);
transform.FlowPunchScale2D(punch: 0.3f, duration: 0.4f);
transform.FlowPunchPosition3D(Vector3.up, duration: 0.4f);
transform.FlowPunchScale3D(Vector3.one * 0.2f, duration: 0.4f);
```

---

## Virtual Tweens

Tween any value with a callback:

```csharp
FlowVirtual.Float(0f, 100f, 2f, value => slider.value = value);
FlowVirtual.Int(0, 500, 2f, value => scoreText.text = value.ToString());
FlowVirtual.Vector3(Vector3.zero, Vector3.one, 1f, v => transform.position = v);
FlowVirtual.Color(Color.white, Color.red, 1f, c => myRenderer.material.color = c);
```

---

## Global Controls

```csharp
// All tweens
FlowTween.KillAll();
FlowTween.PauseAll();
FlowTween.ResumeAll();
FlowTween.CompleteAll();

// By target
transform.FlowKill();
transform.FlowComplete();

// By id
FlowTween.KillById("my-tween");

// By group
FlowTween.KillGroup("ui");
FlowTween.PauseGroup("ui");
FlowTween.ResumeGroup("ui");

// Sequences
FlowTween.KillSequences();
FlowTween.PauseSequences();
FlowTween.ResumeSequences();
```

---

## Built-in Shortcuts

| Target | Methods |
|--------|---------|
| `Transform` | `FlowMove` `FlowMoveX/Y/Z` `FlowMoveLocal` `FlowScale` `FlowScaleUniform` `FlowSquish` `FlowRotate` `FlowRotateLocal` |
| `RectTransform` | `AnchorMove` `FlowSizeDelta` |
| `CanvasGroup` | `FlowFade` `FlowFadeIn` `FlowFadeOut` |
| `Graphic` (UI) | `FlowFade` `FlowFadeIn` `FlowFadeOut` `FlowColor` |
| `SpriteRenderer` | `FlowFade` `FlowFadeIn` `FlowFadeOut` `FlowColor` |
| `Material` | `FlowColor` |
| `AudioSource` | `FlowVolume` `FlowPitch` |
| `Light` | `FlowIntensity` `FlowColor` `FlowRange` |
| `Camera` | `FlowFov` `FlowOrthoSize` |
| `TMP_Text` | `FlowReveal` |

---

## Extending FlowTween

Add your own property interpolator with zero allocations:

```csharp
internal readonly struct MyCustomInterpolator : IPropertyInterpolator<MyComponent, float>
{
    public float GetValue(MyComponent target) => target.myValue;
    public void SetValue(MyComponent target, float from, float to, float t)
        => target.myValue = Mathf.LerpUnclamped(from, to, t);
}

// Use it anywhere
FlowTween.GetTween<MyComponent, float, MyCustomInterpolator>(myComponent, duration: 1f, to: 100f);
```

---

## Performance Notes

- Tweens and sequences are pooled and reused — no GC pressure during normal use
- Struct interpolators avoid virtual dispatch on the animation hot path
- Pool shrinks automatically every 10 seconds to avoid holding excess memory
- Scene-unload cleanup removes tweens targeting destroyed objects automatically

---
