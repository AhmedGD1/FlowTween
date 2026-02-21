# FlowTween

A lightweight, pooled, zero-allocation tweening library for Unity. FlowTween provides a fluent API for animating transforms, UI elements, audio, lights, cameras, physics bodies, and arbitrary values — with built-in sequences, easing, groups, and an Editor debugger.

---

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
- [Tween Methods](#tween-methods)
  - [Transform](#transform)
  - [UI / RectTransform](#ui--recttransform)
  - [Canvas Group](#canvas-group)
  - [Sprite Renderer](#sprite-renderer)
  - [Graphic (UI)](#graphic-ui)
  - [Renderer / Material](#renderer--material)
  - [Audio Source](#audio-source)
  - [Light](#light)
  - [Camera](#camera)
  - [Rigidbody / Rigidbody2D](#rigidbody--rigidbody2d)
  - [Scroll Rect & Slider](#scroll-rect--slider)
  - [TextMeshPro](#textmeshpro)
- [Virtual Tweens](#virtual-tweens)
- [Juice Effects](#juice-effects)
  - [Shake](#shake)
  - [Punch](#punch)
  - [Squish & Jello](#squish--jello)
  - [Heartbeat](#heartbeat)
  - [Wobble Rotate](#wobble-rotate)
  - [Flip](#flip)
  - [Blink](#blink)
  - [Slide In / Out](#slide-in--out)
  - [Float & Pulse](#float--pulse)
- [Tween Configuration](#tween-configuration)
  - [Easing & Transitions](#easing--transitions)
  - [Custom Curves](#custom-curves)
  - [Loops](#loops)
  - [Delay](#delay)
  - [Time Scale & Unscaled Time](#time-scale--unscaled-time)
  - [Speed-Based Duration](#speed-based-duration)
  - [Relative Tweens](#relative-tweens)
  - [Update Mode (Fixed)](#update-mode-fixed)
  - [Callbacks](#callbacks)
  - [IDs & Groups](#ids--groups)
- [Sequences](#sequences)
- [Global Controls](#global-controls)
- [Then (Chaining)](#then-chaining)
- [Custom Interpolators](#custom-interpolators)
- [Editor Debugger](#editor-debugger)

---

## Installation

1. Copy all `.cs` files into any folder inside your Unity project's `Assets/` directory (e.g. `Assets/FlowTween/`).
2. Ensure **TextMeshPro** is installed (required for `FlowReveal`). If you don't use TMP, remove the TMP references from `FlowTweenExtensions.cs` and `PropertyInterpolator.cs`.
3. FlowTween auto-initialises itself via `[RuntimeInitializeOnLoadMethod]` — no scene setup required.

**Recommended Unity Version:** Unity 2021.2+

---

## Quick Start

```csharp
using FlT;

// Move a transform to world position (1,2,0) over 0.5 seconds
transform.FlowMove(new Vector3(1f, 2f, 0f), 0.5f);

// Fade a CanvasGroup out over 1 second, then do something
canvasGroup.FlowFadeOut(1f).OnComplete(() => gameObject.SetActive(false));

// Chain a sine ease
transform.FlowScale(Vector3.one * 1.2f, 0.3f).Sine().EaseOut();

// Virtual float tween (no target object needed)
FlowVirtual.Float(0f, 1f, 2f, value => material.SetFloat("_Blend", value));
```

---

## Core Concepts

| Concept | Description |
|---|---|
| **Tween** | A single animated property change from A to B over time. |
| **Sequence** | An ordered timeline of tweens with support for parallel steps, gaps, and callbacks. |
| **Pool** | Tweens and Sequences are recycled automatically — no GC allocations after warm-up. |
| **FlowTween** | The singleton `MonoBehaviour` manager. Auto-created; survives scene loads. |
| **FlowVirtual** | Creates tweens not bound to a Unity Object, driven by an `Action<T>` callback. |
| **IPropertyInterpolator** | Struct interface for defining how a property is read and written. Extend this to support custom types. |

---

## Tween Methods

All methods return a `Tween` so you can chain configuration calls. The tween starts playing immediately.

### Transform

```csharp
transform.FlowMove(Vector3 to, float duration)
transform.FlowMoveX(float to, float duration)
transform.FlowMoveY(float to, float duration)
transform.FlowMoveZ(float to, float duration)
transform.FlowMoveLocal(Vector3 to, float duration)

transform.FlowScale(Vector3 to, float duration)
transform.FlowScaleUniform(float scale, float duration)   // shorthand for Vector3.one * scale

transform.FlowRotate(Quaternion to, float duration)
transform.FlowRotate(Vector3 eulerTo, float duration)
transform.FlowRotateLocal(Quaternion to, float duration)
transform.FlowRotateLocal(Vector3 eulerTo, float duration)

transform.FlowSpin(float duration)                        // 360° on Z
transform.FlowSpin(Vector3 axis, float duration)          // 360° on custom axis
```

### UI / RectTransform

```csharp
rectTransform.FlowAnchorMove(Vector2 to, float duration)
rectTransform.FlowAnchorMin(Vector2 to, float duration)
rectTransform.FlowAnchorMax(Vector2 to, float duration)
rectTransform.FlowSizeDelta(Vector2 to, float duration)
```

### Canvas Group

```csharp
canvasGroup.FlowFade(float to, float duration)
canvasGroup.FlowFadeIn(float duration)
canvasGroup.FlowFadeOut(float duration)
```

### Sprite Renderer

```csharp
spriteRenderer.FlowFade(float to, float duration)
spriteRenderer.FlowFadeIn(float duration)
spriteRenderer.FlowFadeOut(float duration)
spriteRenderer.FlowColor(Color to, float duration)
```

### Graphic (UI)

Applies to `Image`, `Text`, `RawImage`, and any other `UnityEngine.UI.Graphic` subclass.

```csharp
graphic.FlowFade(float to, float duration)
graphic.FlowFadeIn(float duration)
graphic.FlowFadeOut(float duration)
graphic.FlowColor(Color to, float duration)
image.FlowFillAmount(float to, float duration)
```

### Renderer / Material

Uses `MaterialPropertyBlock` — does not create a new material instance.

```csharp
renderer.FlowColor(Color to, float duration)   // animates _Color property
```

### Audio Source

```csharp
audioSource.FlowVolume(float to, float duration)
audioSource.FlowPitch(float to, float duration)
```

### Light

```csharp
light.FlowIntensity(float to, float duration)
light.FlowColor(Color to, float duration)
light.FlowRange(float to, float duration)
```

### Camera

```csharp
camera.FlowFov(float to, float duration)
camera.FlowOrthoSize(float to, float duration)
```

### Rigidbody / Rigidbody2D

Physics tweens automatically use `FixedUpdate` and call `MovePosition` / `MoveRotation` — no teleportation.

```csharp
rigidbody.FlowPosition(Vector3 to, float duration)
rigidbody.FlowRotation(Quaternion to, float duration)

rigidbody2D.FlowPosition(Vector2 to, float duration)
rigidbody2D.FlowRotation(float to, float duration)
```

### Scroll Rect & Slider

```csharp
scrollRect.FlowPosition(Vector2 normalizedPosition, float duration)
slider.FlowValue(float to, float duration)
```

### TextMeshPro

```csharp
tmpText.FlowReveal(float duration)   // animates maxVisibleCharacters from 0 to full count
```

---

## Virtual Tweens

Use `FlowVirtual` when you need to animate a value that isn't a Unity Object property:

```csharp
FlowVirtual.Float(float from, float to, float duration, Action<float> onUpdate)
FlowVirtual.Int(int from, int to, float duration, Action<int> onUpdate)
FlowVirtual.Vector2(Vector2 from, Vector2 to, float duration, Action<Vector2> onUpdate)
FlowVirtual.Vector3(Vector3 from, Vector3 to, float duration, Action<Vector3> onUpdate)
FlowVirtual.Color(Color from, Color to, float duration, Action<Color> onUpdate)
```

Example:

```csharp
FlowVirtual.Int(0, 100, duration: 2f, val => scoreLabel.text = val.ToString());
```

---

## Juice Effects

Convenience methods that combine tweens/sequences into polished game-feel effects.

### Shake

Perlin-noise driven, smoothly damping to zero. All variants return a `Tween`.

```csharp
transform.FlowShake2D(float duration, float strength = 1f, float frequency = 20f)
transform.FlowShakeLocal2D(float duration, float strength = 1f, float frequency = 20f)
transform.FlowShake3D(float duration, float strength = 1f, float frequency = 20f)
transform.FlowShakeLocal3D(float duration, float strength = 1f, float frequency = 20f)

transform.FlowShakeRotation2D(float duration, float strength = 15f, float frequency = 20f)
transform.FlowShakeRotation3D(float duration, float strength = 15f, float frequency = 20f)
transform.FlowShakeRotationAxis(Vector3 axis, float duration, float strength = 15f, float frequency = 20f)
```

### Punch

Sinusoidal oscillation that decays to the start value. Returns a `Tween`.

```csharp
transform.FlowPunchPosition2D(Vector2 punch, float duration, int vibrato = 10, float elasticity = 1f)
transform.FlowPunchScale2D(float punch, float duration, int vibrato = 10, float elasticity = 1f)
transform.FlowPunchPosition3D(Vector3 punch, float duration, int vibrato = 10, float elasticity = 1f)
transform.FlowPunchScale3D(Vector3 punch, float duration, int vibrato = 10, float elasticity = 1f)
```

### Squish & Jello

```csharp
// Three-step squash-and-stretch. Returns a Sequence.
transform.FlowSquish(float duration, float ratio = 0.2f, SquishDirection direction = SquishDirection.Up)

// Oscillating squash-and-stretch decaying like gelatine. Returns a Tween.
transform.FlowJello(float duration, float intensity = 0.25f, float frequency = 4f)
```

### Heartbeat

```csharp
// Double-pulse (lub-dub) scale effect. Returns a Sequence.
transform.FlowHeartbeat(float duration, float intensity = 0.3f, int beats = 1)
```

### Wobble Rotate

```csharp
// Decaying Z-axis oscillation. Returns a Tween.
transform.FlowWobbleRotate(float duration, float strength = 20f, float frequency = 4f)
```

### Flip

```csharp
// Card-flip illusion by scaling through zero on Y. Returns a Sequence.
transform.FlowFlipY(float duration, bool full = false)

// Flip on the X axis (top-to-bottom). Returns a Sequence.
transform.FlowFlipX(float duration, bool full = false)
```

### Blink

```csharp
// Rapid fade in/out. Returns a Sequence.
spriteRenderer.FlowBlink(int blinks = 4, float blinkSpeed = 0.1f, bool endVisible = true)
```

### Slide In / Out

```csharp
// Slides from an offset into the current anchored position. Returns a Tween.
rectTransform.FlowSlideIn(SlideDirection direction, float offset, float duration)

// Slides out toward an edge. Returns a Tween.
rectTransform.FlowSlideOut(SlideDirection direction, float offset, float duration)

// SlideDirection: Left, Right, Up, Down
```

### Float & Pulse

```csharp
// Continuous sine-wave bobbing loop. Returns a Tween — call .Kill() to stop.
transform.FlowFloat(float amplitude = 0.2f, float frequency = 1f)

// Looping scale + alpha throb. Returns a Sequence — call .Kill() to stop.
transform.FlowPulse(CanvasGroup canvasGroup, float scaleMagnitude = 0.05f, float alphaMin = 0.7f, float frequency = 1f)

// Scale-only looping throb. Returns a Tween.
transform.FlowPulse(float scaleMagnitude = 0.05f, float frequency = 1f)
```

---

## Tween Configuration

All configuration methods return `this` for chaining.

### Easing & Transitions

```csharp
tween.SetTransition(Tween.TransitionType transition)
tween.SetEase(Tween.EaseType ease)

// Shorthand transition methods:
tween.Linear()  .Sine()   .Quad()    .Cubic()
tween.Quart()   .Quint()  .Expo()    .Circ()
tween.Back()    .Elastic() .Bounce() .Spring()

// Shorthand ease methods:
tween.EaseIn()  .EaseOut()  .EaseInOut()  .EaseOutIn()
```

**Available `TransitionType` values:** `Linear`, `Sine`, `Quad`, `Cubic`, `Quart`, `Quint`, `Expo`, `Circ`, `Back`, `Elastic`, `Bounce`, `Spring`

**Available `EaseType` values:** `In`, `Out`, `InOut`, `OutIn`

Default transition and ease can be set globally:

```csharp
FlowTween.SetDefaultTransition(Tween.TransitionType.Sine);
FlowTween.SetDefaultEase(Tween.EaseType.Out);
```

### Custom Curves

```csharp
tween.SetCurve(AnimationCurve curve)
```

When a custom curve is set it takes priority over `TransitionType`/`EaseType`.

### Loops

```csharp
tween.SetLoops(int count, Tween.LoopType type = LoopType.Restart)
// count = -1 for infinite loops
// LoopType.Restart — jumps back to start each loop
// LoopType.Yoyo    — reverses direction each loop
```

### Delay

```csharp
tween.SetDelay(float seconds)
```

### Time Scale & Unscaled Time

```csharp
tween.SetTimeScale(float scale)      // default 1.0
tween.SetUnscaledTime(bool value)    // ignores Time.timeScale when true
```

### Speed-Based Duration

Override the duration so the tween travels at a fixed speed rather than a fixed time:

```csharp
tween.SetSpeedBase(float unitsPerSecond)
// Duration is calculated as distance / speed on the first frame.
```

### Relative Tweens

Interpret the `to` value as an offset from the current value rather than an absolute target:

```csharp
tween.SetRelative()

// Example: move 5 units to the right of wherever the object currently is
transform.FlowMove(new Vector3(5f, 0f, 0f), 1f).SetRelative();
```

### Update Mode (Fixed)

Force a tween to update in `FixedUpdate` instead of `Update`:

```csharp
tween.SetUpdateMode(Tween.TweenUpdateMode.Fixed)
```

Note: Physics tweens (`FlowPosition`/`FlowRotation` on `Rigidbody`) set this automatically.

### Callbacks

```csharp
tween.OnStart(Action callback)
tween.OnUpdate(Action<float> callback)    // receives the eased t value [0..1]
tween.OnComplete(Action callback)
tween.OnLoop(Action<int> loopCount)
tween.OnKill(Action callback)
```

### IDs & Groups

```csharp
tween.SetId(object id)
tween.SetGroup(string groupName)

// Target specific tweens later:
FlowTween.KillById(object id);
FlowTween.KillGroup(string group);
FlowTween.PauseGroup(string group);
FlowTween.ResumeGroup(string group);
```

---

## Sequences

A `Sequence` is a timeline of tweens. Once configured, call `.Play()` to start it.

```csharp
Sequence seq = FlowTween.Sequence()

    // Append: add a step after all existing ones
    .Append(transform.FlowMove(new Vector3(5, 0, 0), 1f).Sine().EaseOut())

    // Join: run in parallel with the most recently appended step
    .Join(canvasGroup.FlowFade(0f, 0.5f))

    // AppendInterval: blank gap
    .AppendInterval(0.25f)

    // Insert: place at an explicit time
    .Insert(0.1f, transform.FlowScale(Vector3.one * 1.5f, 0.2f))

    // Callbacks
    .AppendCallback(() => Debug.Log("After move!"))
    .InsertCallback(0.5f, () => Debug.Log("At 0.5s"))

    // Prepend: insert before everything, shifts timeline forward
    .Prepend(transform.FlowScale(Vector3.zero, 0.3f))

    // Loops (-1 = infinite)
    .SetLoops(3)
    .OnLoop(loopIndex => Debug.Log($"Loop {loopIndex}"))
    .OnComplete(() => Debug.Log("All done"))

    .Play();

// Control at any time:
seq.Pause();
seq.Resume();
seq.Kill();
```

**Important:** Tweens added to a sequence are removed from the active tween list and their lifetime is managed by the sequence. Do not call `.Kill()` on them individually.

---

## Global Controls

```csharp
// All active tweens
FlowTween.KillAll();
FlowTween.PauseAll();
FlowTween.ResumeAll();
FlowTween.CompleteAll();

// By target object
target.FlowKill();
target.FlowComplete();

// By id or group
FlowTween.KillById(id);
FlowTween.KillGroup("myGroup");

// All sequences
FlowTween.KillSequences();
FlowTween.PauseSequences();
FlowTween.ResumeSequences();
```

---

## Then (Chaining)

Run a tween after another finishes. The second tween is removed from the active list until the first completes.

```csharp
Tween a = transform.FlowMove(new Vector3(5, 0, 0), 1f);
Tween b = transform.FlowMove(Vector3.zero, 1f);
a.Then(b);
```

---

## Custom Interpolators

Implement `IPropertyInterpolator<TTarget, TValue>` as a `readonly struct` to add support for any property:

```csharp
internal readonly struct MyFloatInterpolator : IPropertyInterpolator<MyComponent, float>
{
    public float GetValue(MyComponent target) => target.myValue;
    public void SetValue(MyComponent target, float from, float to, float t)
        => target.myValue = Mathf.LerpUnclamped(from, to, t);
}

// Use it:
FlowTween.GetTween<MyComponent, float, MyFloatInterpolator>(myComponent, 1f, 100f);
```

Using `readonly struct` ensures the interpolator has zero heap allocations — the generic pool handles the wrapper object.

---

## Editor Debugger

Open via **Window → Analysis → FlowTween Debugger**.

The debugger shows all active tweens (Update and Fixed) and sequences in real time:

- **Progress bar** showing elapsed / total duration
- **State badge** — Playing (green), Paused (yellow), Completed (grey)
- **Target name**, ID, and Group label
- **Per-tween controls** — Pause, Resume, Complete, Kill
- **Filter bar** — search by target name, ID, or group; toggle visibility of paused/playing/completed
- **Settings tab** — configure refresh rate, global default transition/ease, and global bulk controls

The debugger only operates during Play Mode.

---
