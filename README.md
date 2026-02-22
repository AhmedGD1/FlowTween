# FlowTween

A lightweight, pooled, zero-allocation tweening library for Unity. FlowTween provides a fluent API for animating transforms, UI elements, audio, lights, cameras, physics bodies, and arbitrary values — with built-in sequences, easing, groups, and an Editor debugger.

---

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Extension Methods](#extension-methods)
- [Virtual Tweens](#virtual-tweens)
- [Easing](#easing)
- [Tween Options](#tween-options)
- [Sequences](#sequences)
- [Groups](#groups)
- [Global Controls](#global-controls)
- [Settings](#settings)
- [Debug Window](#debug-window)
- [Settings Window](#settings-window)
- [Extending with Custom Interpolators](#extending-with-custom-interpolators)
- [Tips & Recommendations](#tips--recommendations)

---

## Installation

Copy all `.cs` files from the `FlT` namespace into your Unity project. The manager singleton (`[FlowTween]`) is created automatically before the first scene loads — no manual setup required.

Optionally place a `FlowTweenSettings` asset at `Resources/FlowTweenSettings` to configure defaults (see [Settings](#settings)).

---

## Quick Start

```csharp
using FlT;

// Move a transform to a world position over 1 second
transform.FlowMove(new Vector3(5f, 0f, 0f), 1f);

// Scale up with a bounce ease, then log when done
transform.FlowScale(Vector3.one * 2f, 0.5f)
    .Bounce()
    .EaseOut()
    .OnComplete(() => Debug.Log("Done!"));

// Fade out a CanvasGroup
canvasGroup.FlowFadeOut(0.3f);

// Animate a custom float value
FlowVirtual.Float(0f, 100f, 1f, value => myText.text = value.ToString("0"))
    .SetDelay(0.5f)
    .Play(); // (tweens from extensions are automatically active; FlowVirtual tweens need no .Play() — they start immediately)
```

---

## Extension Methods

All extension methods live on `FlowTweenExtensions` and follow the pattern `target.FlowXxx(to, duration)`. They return a `Tween` you can chain modifiers onto.

### Transform

| Method | Description |
|---|---|
| `FlowMove(Vector3, float)` | World position |
| `FlowMoveX/Y/Z(float, float)` | Single world axis |
| `FlowMoveLocal(Vector3, float)` | Local position |
| `FlowScale(Vector3, float)` | Local scale |
| `FlowScaleUniform(float, float)` | Uniform scale shorthand |
| `FlowRotate(Quaternion/Vector3, float)` | World rotation |
| `FlowRotateLocal(Quaternion/Vector3, float)` | Local rotation |

### UI & Canvas

| Method | Target | Description |
|---|---|---|
| `FlowFade(float, float)` | `CanvasGroup` | Alpha |
| `FlowFadeIn/Out(float)` | `CanvasGroup` | Convenience fade to 1/0 |
| `FlowAnchorMove(Vector2, float)` | `RectTransform` | Anchored position |
| `FlowAnchorMin/Max(Vector2, float)` | `RectTransform` | Anchor corners |
| `FlowSizeDelta(Vector2, float)` | `RectTransform` | Size delta |
| `FlowFade/FadeIn/FadeOut` | `Graphic` | Graphic alpha |
| `FlowColor(Color, float)` | `Graphic` | Graphic color |
| `FlowFillAmount(float, float)` | `Image` | Fill amount |
| `FlowReveal(float)` | `TMP_Text` | Character-by-character reveal |

### Rendering

| Method | Target | Description |
|---|---|---|
| `FlowFade/FadeIn/FadeOut` | `SpriteRenderer` | Sprite alpha |
| `FlowColor(Color, float)` | `SpriteRenderer` | Sprite color |
| `FlowColor(Color, float)` | `Renderer` | Material color |

### Audio

| Method | Description |
|---|---|
| `FlowVolume(float, float)` | AudioSource volume |
| `FlowPitch(float, float)` | AudioSource pitch |

### Lights & Camera

| Method | Target | Description |
|---|---|---|
| `FlowIntensity(float, float)` | `Light` | Light intensity |
| `FlowColor(Color, float)` | `Light` | Light color |
| `FlowRange(float, float)` | `Light` | Light range |
| `FlowFov(float, float)` | `Camera` | Field of view |
| `FlowOrthoSize(float, float)` | `Camera` | Orthographic size |

### Juice / Procedural Effects

| Method | Description |
|---|---|
| `FlowSquish(float, float, SquishDirection)` | Classic squash-and-stretch sequence |
| `FlowShake2D/3D(float, float, float)` | Perlin noise position shake |
| `FlowShakeLocal2D/3D(...)` | Same in local space |
| `FlowShakeRotation2D/3D(...)` | Perlin noise rotation shake |
| `FlowShakeRotationAxis(Vector3, ...)` | Shake around an arbitrary axis |
| `FlowPunchPosition2D/3D(...)` | Sinusoidal punch effect |
| `FlowPunchScale2D/3D(...)` | Sinusoidal scale punch |

---

## Virtual Tweens

Use `FlowVirtual` when you want to drive a value that doesn't map to a built-in property:

```csharp
// Float
FlowVirtual.Float(0f, 1f, 0.5f, t => myMaterial.SetFloat("_Blend", t));

// Int  
FlowVirtual.Int(0, 100, 2f, i => scoreText.text = i.ToString());

// Vector2 / Vector3 / Color
FlowVirtual.Color(Color.red, Color.blue, 1f, c => myRenderer.material.color = c);
```

---

## Easing

Every `Tween` has a **TransitionType** and an **EaseType**.

### Transition shortcuts (fluent)

```csharp
tween.Linear().Sine().Quad().Cubic().Quart().Quint()
     .Expo().Circ().Back().Elastic().Bounce().Spring()
```

### Ease shortcuts (fluent)

```csharp
tween.EaseIn().EaseOut().EaseInOut().EaseOutIn()
```

### Custom AnimationCurve

```csharp
tween.SetCurve(myAnimationCurve);
```

When a custom curve is set it overrides TransitionType/EaseType.

---

## Tween Options

All set methods return `this` for chaining.

```csharp
transform.FlowMove(target, 1f)
    .SetDelay(0.5f)               // wait before starting
    .SetLoops(3, LoopType.Yoyo)   // repeat 3 times, ping-pong
    .SetRelative()                // treat 'to' as an offset from current value
    .SetFrom(Vector3.zero)        // override the starting value
    .SetTimeScale(0.5f)           // individual speed multiplier
    .SetUnscaledTime()            // ignore Time.timeScale
    .SetSpeedBase(5f)             // duration derived from distance / speed
    .SetId("myTween")             // used for KillById
    .SetGroup("UI")               // group for batch kill/pause/resume
    .SetUpdateMode(TweenUpdateMode.Fixed) // use FixedUpdate instead of Update
    .OnStart(() => { })
    .OnUpdate(t => { })           // receives eased 0→1 progress
    .OnLoop(loop => { })
    .OnComplete(() => { })
    .OnKill(() => { });
```

### Chaining with `.Then()`

```csharp
transform.FlowMove(posA, 0.5f)
    .Then(transform.FlowMove(posB, 0.5f)
        .Then(transform.FlowMove(posC, 0.5f)));
```

### Controlling a tween

```csharp
Tween t = transform.FlowMove(target, 1f);
t.Pause();
t.Resume();
t.Kill();
t.Complete();   // jump to end, fire OnComplete
t.Restart();
```

---

## Sequences

Sequences are pooled, sorted containers for multiple tweens.

```csharp
FlowTween.Sequence()
    .Append(transform.FlowMove(posA, 0.5f))          // runs after the previous step
    .Join(transform.FlowScale(Vector3.one * 2f, 0.3f)) // runs in parallel with the last Append
    .AppendInterval(0.2f)                             // blank gap
    .Append(canvasGroup.FlowFadeOut(0.4f))
    .AppendCallback(() => Debug.Log("Faded out"))
    .InsertCallback(0.1f, () => Debug.Log("At 100ms"))
    .Insert(0f, transform.FlowRotate(Vector3.up * 90f, 1f)) // explicit time
    .Prepend(someOtherTween)                          // shifts everything forward
    .SetLoops(2)
    .OnLoop(i => Debug.Log($"Loop {i}"))
    .OnComplete(() => Debug.Log("Sequence done"))
    .Play();
```

---

## Groups

Assign tweens to named groups (string or any `Enum`) for batch control:

```csharp
transform.FlowMove(target, 1f).SetGroup("UI");
transform.FlowScale(Vector3.one, 1f).SetGroup("UI");

FlowTween.PauseGroup("UI");
FlowTween.ResumeGroup("UI");
FlowTween.KillGroup("UI");

// Enum overloads
enum TweenGroup { UI, Gameplay, Audio }
FlowTween.PauseGroup(TweenGroup.UI);
```

---

## Global Controls

```csharp
FlowTween.KillAll();
FlowTween.PauseAll();
FlowTween.ResumeAll();
FlowTween.CompleteAll();

FlowTween.KillTarget(gameObject);     // kill all tweens targeting an object
FlowTween.CompleteTarget(gameObject);
FlowTween.KillById("myTween");

FlowTween.KillSequences();
FlowTween.PauseSequences();
FlowTween.ResumeSequences();
```

Extension shortcuts on any `UnityEngine.Object`:

```csharp
gameObject.FlowKill();
gameObject.FlowComplete();
```

---

## Settings

Create a `FlowTweenSettings` asset at `Resources/FlowTweenSettings` (right-click → Create → FlowTween → Settings) to configure project-wide defaults.

| Property | Description | Default |
|---|---|---|
| `defaultTransition` | Transition curve used when none is set | `Linear` |
| `defaultEase` | Ease direction used when none is set | `In` |
| `globalTimeScale` | Multiplier applied to all `Update` tweens | `1.0` |
| `killOnSceneUnload` | Kill tweens that target objects in unloaded scenes | `true` |
| `autoKillOrphans` | Kill tweens whose target `UnityEngine.Object` is destroyed | `true` |
| `minPoolSize` | Pools won't shrink below this count | `10` |
| `shrinkInterval` | Seconds between pool shrink passes | `10` |
| `shrinkPercent` | Fraction of excess pool entries removed per shrink | `0.25` |
| `prewarmTweens` | Tween pool pre-allocated at startup | `0` |
| `prewarmSequences` | Sequence pool pre-allocated at startup | `0` |

You can also apply settings at runtime:

```csharp
FlowTween.ApplySettings(mySettingsAsset);
FlowTween.LoadSettings(); // re-read from Resources
```

---

## Debug Window

<img width="896" height="547" alt="d4" src="https://github.com/user-attachments/assets/a3bd5d3f-8492-4bdf-8081-df0bb7eeb24c" />
<img width="903" height="550" alt="d3" src="https://github.com/user-attachments/assets/a91249bf-1a41-4482-b1d1-43df34123ddd" />
<img width="859" height="281" alt="d2" src="https://github.com/user-attachments/assets/a9700b01-bc01-45ee-9696-257a2bc2dc8e" />
<img width="896" height="548" alt="d1" src="https://github.com/user-attachments/assets/96467d13-4ff7-422c-8197-a8b5f6c6f32b" />

Open via **Window → FlowTween → Debug**.

The debug window gives you a live view of the runtime state:

- **Active tweens** — idle and fixed lists, with each tween's target, interpolator type, from/to values, progress bar, loop count, and whether it's playing/paused/pending
- **Active sequences** — step timeline, elapsed progress, loop state, and per-step tween breakdown
- **Pool stats** — tween and sequence pool sizes, hit/miss counts, and hit rate percentage
- **Interpolator pool stats** — per-type pool sizes for all registered interpolators
- **Global controls** — Kill All / Pause All / Resume All / Complete All buttons directly in the window
- **Settings snapshot** — current global time scale, default transition/ease, and behaviour flags

The window auto-refreshes every editor frame during Play Mode.

---

## Settings Window

<img width="715" height="649" alt="settings3" src="https://github.com/user-attachments/assets/32237037-f5d0-440f-9d7c-4c054bb43560" />
<img width="713" height="645" alt="settings2" src="https://github.com/user-attachments/assets/68f622a9-bc73-42ad-8ab1-5af5d735ce0d" />
<img width="713" height="645" alt="settings1" src="https://github.com/user-attachments/assets/3c879a3c-edef-4ded-a686-c1dc6736ded0" />

Open via **Window → FlowTween → Settings** (or by clicking *Edit Settings* from the debug window).

Provides a GUI for creating and editing the `FlowTweenSettings` asset without hunting through the Project panel. Changes made here write to the asset immediately and are applied to the running game on the next call to `ApplySettings`.

---

## Extending with Custom Interpolators

Implement `IPropertyInterpolator<TTarget, TValue>` as a `struct` to teach FlowTween how to read and write any property:

```csharp
public struct MyCustomInterpolator : IPropertyInterpolator<MyComponent, float>
{
    public float GetValue(MyComponent target) => target.myFloat;
    public void  SetValue(MyComponent target, float from, float to, float t)
        => target.myFloat = Mathf.LerpUnclamped(from, to, t);
}

// Use it:
FlowTween.GetTween<MyComponent, float, MyCustomInterpolator>(component, 1f, targetValue);
```

Because interpolators are `struct`s, they allocate nothing at call time. Completed interpolators are pooled automatically.

---

## Tips & Recommendations

**Pool pre-warming** — for scenes with many simultaneous tweens, set `prewarmTweens` in your settings asset to avoid the GC cost of the first allocations.

**Speed-based duration** — `SetSpeedBase(unitsPerSecond)` computes the duration from the actual `from → to` distance. Handy for movement where you want consistent perceived speed regardless of distance.

**`SetRelative()`** — the `to` value becomes an offset added to wherever the target is when the tween starts, not when it was created. Safe to use with `.Then()` chains.

**`autoKillOrphans`** — leave this enabled (the default). It silently removes tweens whose target object has been destroyed, preventing null-reference errors without requiring manual cleanup.

**Sequence vs `.Then()`** — use `Sequence` when you need parallel tracks, callbacks at specific times, or looping groups of tweens. Use `.Then()` for simple linear chains of two or three tweens.

**`SetUnscaledTime()`** — bypasses both `Time.timeScale` and `FlowTween.GlobalTimeScale`. Essential for UI tweens that should remain responsive when the game is paused.

**Groups with enums** — prefer typed enums over raw strings to avoid typos and enable IDE autocomplete.

```csharp
enum Layer { UI, FX, World }
tween.SetGroup(Layer.UI);
FlowTween.KillGroup(Layer.UI);
```
