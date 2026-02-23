# FlowTween

A lightweight, zero-allocation tweening library for Unity built around object pooling and a fluent chaining API. Covers transform animation, UI, audio, lights, cameras, physics, TextMeshPro, materials, and a suite of procedural effects — all without a single managed allocation per tween after the pool is warm.

---

## Features

- **Zero-alloc design** — tweens and sequences are pooled and recycled. Effect interpolators (shake, punch, jello, etc.) are pooled too; no lambdas are captured at call sites
- **Fluent API** — every setter returns `Tween`, so easing, looping, delays, and callbacks chain naturally
- **Sequences** — compose tweens with `Append`, `Join`, `Insert`, `Prepend`, and timed callbacks
- **12 easing curves × 4 modes** — Linear, Sine, Quad, Cubic, Quart, Quint, Expo, Circ, Back, Elastic, Bounce, Spring × In / Out / InOut / OutIn, plus `AnimationCurve` support
- **Groups** — tag tweens with a string or enum key, then `KillGroup`, `PauseGroup`, `ResumeGroup` in one call
- **Global & per-tween time scale** — independent of `Time.timeScale`, with optional unscaled time per tween
- **Speed-based duration** — set units-per-second instead of a fixed duration and let the library derive it from the actual distance
- **Physics-safe** — Rigidbody and Rigidbody2D tweens automatically run in `FixedUpdate`
- **Auto kill on scene unload** — destroys tweens whose targets no longer exist
- **Editor debugger** — a full runtime inspector with virtual scrolling, event log, pool profiler, ease reference curves, and live settings

---

## Installation

Drop the `FlT` folder anywhere inside your project's `Assets` directory. No assembly definitions or package manifests are required.

FlowTween initialises itself before the first scene loads via `[RuntimeInitializeOnLoadMethod]` — you do not need to place a prefab in your scene.

**Optional:** create a `FlowTweenSettings` ScriptableObject at `Resources/FlowTweenSettings` to configure defaults and pool pre-warming. Without it the library runs with sensible fallback values.

---

## Quick Start

```csharp
using FlT;

// Move a transform to world position (0, 5, 0) over 1 second
transform.FlowMove(new Vector3(0f, 5f, 0f), 1f);

// Built-in delay tween;
FlowTween.FlowDelay(delay: 2f, () => print("Hello world"));

// Fade a CanvasGroup out and disable it when done
canvasGroup.FlowFadeDisable(0.4f);

// Scale up with a bounce, loop twice, then call a method
transform.FlowScale(Vector3.one * 1.5f, 0.6f)
         .Bounce().EaseOut()
         .SetLoops(2, Tween.LoopType.Yoyo)
         .OnComplete(OnAnimationDone);
```

---

## Easing

Append a transition method, then optionally an ease direction:

```csharp
tween.Sine().EaseInOut()
tween.Elastic().EaseOut()
tween.Back().EaseIn()
tween.SetCurve(myAnimationCurve)   // custom AnimationCurve
```

**Transitions:** `Linear` `Sine` `Quad` `Cubic` `Quart` `Quint` `Expo` `Circ` `Back` `Elastic` `Bounce` `Spring`  
**Ease modes:** `EaseIn` `EaseOut` `EaseInOut` `EaseOutIn`

---

## Extension Methods by Category

### Transform

| Method | Description |
|---|---|
| `FlowMove(to, duration)` | World position |
| `FlowMoveX/Y/Z(to, duration)` | Single world axis |
| `FlowMoveLocal(to, duration)` | Local position |
| `FlowMoveLocalX/Y/Z(to, duration)` | Single local axis |
| `FlowScale(to, duration)` | Local scale |
| `FlowScaleUniform(scale, duration)` | Uniform scale on all axes |
| `FlowRotate(to, duration)` | World rotation (Quaternion or euler) |
| `FlowRotateLocal(to, duration)` | Local rotation (Quaternion or euler) |
| `FlowSpin(duration)` | Continuous 360° spin on Z |
| `FlowSpin(axis, duration)` | Continuous 360° spin on any axis |

### UI

| Method | Description |
|---|---|
| `FlowFade(to, duration)` | CanvasGroup alpha |
| `FlowFadeIn/Out(duration)` | Convenience wrappers to 1 / 0 |
| `FlowFadeEnable/Disable(duration)` | Fades and toggles `interactable` + `blocksRaycasts` |
| `FlowFade(to, duration)` | Graphic alpha (Image, Text, …) |
| `FlowColor(to, duration)` | Graphic color |
| `FlowGradient(gradient, duration)` | Graphic or SpriteRenderer sampled across a Gradient |
| `FlowAnchorMove(to, duration)` | RectTransform anchored position |
| `FlowAnchorMin/Max(to, duration)` | Anchor bounds |
| `FlowSizeDelta(to, duration)` | RectTransform size |
| `FlowPivot(to, duration)` | Pivot |
| `FlowOffsetMin/Max(to, duration)` | Layout offsets |
| `FlowFillAmount(to, duration)` | Image fill |
| `FlowPosition(to, duration)` | ScrollRect normalised position |
| `FlowValue(to, duration)` | Slider value |
| `FlowSlideIn/Out(direction, offset, duration)` | Edge-based slide transitions |

### Renderer & Material

| Method | Description |
|---|---|
| `FlowFade(to, duration)` | SpriteRenderer alpha |
| `FlowColor(to, duration)` | SpriteRenderer / Renderer color |
| `FlowMaterialFloat(property, to, duration)` | Named shader float |
| `FlowMaterialColor(property, to, duration)` | Named shader color |
| `FlowMaterialVector(property, to, duration)` | Named shader vector |
| `FlowMaterialTiling(property, to, duration)` | Texture tiling |
| `FlowMaterialOffset(property, to, duration)` | Texture offset |
| `FlowBlendShape(index, to, duration)` | SkinnedMeshRenderer blend shape weight |

### Audio

| Method | Description |
|---|---|
| `FlowVolume(to, duration)` | AudioSource volume |
| `FlowPitch(to, duration)` | AudioSource pitch |
| `FlowPanStereo(to, duration)` | Stereo pan (−1 … 1) |
| `FlowFadeOutAndStop(duration)` | Fades to zero then calls `Stop()` |

### Camera & Light

| Method | Description |
|---|---|
| `FlowFov(to, duration)` | Perspective field of view |
| `FlowOrthoSize(to, duration)` | Orthographic size |
| `FlowBackgroundColor(to, duration)` | Camera background color |
| `FlowRect(to, duration)` | Camera viewport Rect |
| `FlowIntensity(to, duration)` | Light intensity |
| `FlowColor(to, duration)` | Light color |
| `FlowRange(to, duration)` | Light range |

### TextMeshPro

| Method | Description |
|---|---|
| `FlowReveal(duration)` | Fade-reveal characters via vertex alpha |
| `FlowTypewriter(duration)` | Crisp character-by-character reveal via `maxVisibleCharacters` |
| `FlowCounter(from, to, duration, format)` | Animate a float counter with a format string |
| `FlowCounter(from, to, duration)` | Animate an integer counter |
| `FlowCounter(from, to, duration, formatter)` | Custom `Func<float, string>` formatter |
| `FlowCharacterColor(to, duration)` | Animate vertex color across all characters |

### Physics (FixedUpdate)

| Method | Description |
|---|---|
| `FlowPosition(to, duration)` | Rigidbody / Rigidbody2D via `MovePosition` |
| `FlowRotation(to, duration)` | Rigidbody / Rigidbody2D via `MoveRotation` |

---

## Procedural Effects

All effects are allocation-free. Each has an internal pooled interpolator that captures its state as fields.

| Method | Description |
|---|---|
| `FlowShake2D(duration, strength, frequency)` | Perlin-noise world-space XY shake |
| `FlowShakeLocal2D(...)` | Local-space XY variant |
| `FlowShake3D(duration, strength, frequency)` | Perlin-noise XYZ shake |
| `FlowShakeLocal3D(...)` | Local-space XYZ variant |
| `FlowShakeRotation3D(duration, strength, frequency)` | All-axis rotation shake |
| `FlowShakeRotation2D(duration, strength, frequency)` | Z-axis rotation shake |
| `FlowShakeRotationAxis(axis, duration, strength, frequency)` | Arbitrary-axis rotation shake |
| `FlowPunchPosition2D(punch, duration, vibrato, elasticity)` | Oscillating 2D position punch |
| `FlowPunchPosition3D(punch, duration, vibrato, elasticity)` | Oscillating 3D position punch |
| `FlowPunchScale2D(punch, duration, vibrato, elasticity)` | Oscillating uniform scale punch |
| `FlowPunchScale3D(punch, duration, vibrato, elasticity)` | Oscillating per-axis scale punch |
| `FlowJello(duration, intensity, frequency)` | Decaying squash-and-stretch oscillation |
| `FlowWobbleRotate(duration, strength, frequency)` | Decaying rotational wobble |
| `FlowSquish(duration, ratio, direction)` | Three-step squash-and-stretch sequence |
| `FlowHeartbeat(duration, intensity, beats)` | Lub-dub double-pulse scale effect |
| `FlowBlink(blinks, blinkDuration, endVisible)` | Rapid enable/disable blink |
| `FlowFloat(amplitude, frequency)` | Continuous sine-wave bobbing (infinite loop) |
| `FlowPulse(scaleMagnitude, frequency)` | Looping scale throb |
| `FlowPulse(canvasGroup, scaleMagnitude, alphaMin, frequency)` | Scale + alpha throb |
| `FlowFlipX/Y(duration, full)` | Card-flip illusion via scale-through-zero |
| `FlowLookAt(targetProvider, duration, upAxis)` | Continuously rotate to face a world position |
| `FlowLookAt2D(targetProvider, duration)` | 2D Z-axis tracking |
| `FlowPath(waypoints, duration, closedLoop, orientToPath)` | Catmull-Rom spline (world space) |
| `FlowPathLocal(waypoints, duration, closedLoop, orientToPath)` | Catmull-Rom spline (local space) |

---

## Sequences

```csharp
FlowTween.Sequence()
    .Append(transform.FlowMove(targetA, 0.5f).Sine().EaseOut())
    .AppendInterval(0.2f)
    .Join(canvasGroup.FlowFade(0f, 0.3f))
    .Append(transform.FlowScale(Vector3.one * 1.2f, 0.3f).Back().EaseOut())
    .AppendCallback(() => Debug.Log("done"))
    .SetLoops(3)
    .OnComplete(OnSequenceFinished)
    .Play();
```

| Method | Description |
|---|---|
| `Append(tween)` | Add after the current write-head |
| `Join(tween)` | Run in parallel with the previous step |
| `Insert(time, tween)` | Place at an explicit timestamp |
| `Prepend(tween)` | Insert at the front, shifting everything forward |
| `AppendInterval(duration)` | Insert a blank gap |
| `AppendCallback(action)` | Fire a callback at the current write-head |
| `InsertCallback(time, action)` | Fire a callback at an explicit time |
| `SetLoops(count)` | Loop the whole sequence |
| `OnComplete(action)` | Callback when the sequence finishes |
| `OnLoop(action)` | Callback on each loop iteration |
| `Pause()` / `Resume()` / `Kill()` | Playback control |

---

## Tween Configuration

```csharp
transform.FlowMove(Vector3.up * 3f, 2f)
    .SetDelay(0.5f)
    .SetLoops(-1, Tween.LoopType.Yoyo)   // -1 = infinite
    .SetFrom(Vector3.down)
    .SetRelative()                         // treat 'to' as an offset from start
    .SetSpeedBase(5f)                      // 5 units/sec, duration derived from distance
    .SetUnscaledTime()                     // ignores Time.timeScale
    .SetTimeScale(2f)                      // per-tween time multiplier
    .SetGroup("UI")                        // tag for group operations
    .SetId(myId)                           // tag for KillById
    .OnStart(() => { })
    .OnUpdate(t => { })                    // receives the eased progress value
    .OnLoop(i => { })
    .OnComplete(() => { })
    .OnKill(() => { });
```

---

## Global Controls

```csharp
FlowTween.KillAll();
FlowTween.PauseAll();
FlowTween.ResumeAll();
FlowTween.CompleteAll();

FlowTween.KillGroup("UI");          // string key
FlowTween.PauseGroup(MyEnum.Combat);// enum key

FlowTween.KillById(someId);

FlowTween.SetGlobalTimeScale(0.5f); // slow-motion, does not affect unscaled tweens

// Per-object convenience (extension on UnityEngine.Object)
myRenderer.FlowKill();
myRenderer.FlowComplete();
```

---

## Virtual Tweens

`FlowVirtual` lets you drive any arbitrary value with a pooled tween and an explicit callback, for cases where no built-in interpolator exists:

```csharp
FlowVirtual.Float(0f, 100f, 1.5f, value =>
{
    scoreText.text = value.ToString("0");
});

FlowVirtual.Vector3(startPos, endPos, 1f, pos =>
{
    lineRenderer.SetPosition(0, pos);
});
```

Available types: `Float`, `Int`, `Vector2`, `Vector3`, `Color`.

> **Note:** for built-in effects and components, prefer the dedicated extension methods over `FlowVirtual` — they use pooled interpolators and produce no managed allocations.

---

## `.Then()` Chaining

Play a second tween immediately after the first completes:

```csharp
transform.FlowMove(pointA, 1f)
    .Then(transform.FlowMove(pointB, 1f));
```

---

## Pool Management

FlowTween uses separate pools for tweens and sequences. Both grow on demand and shrink gradually during idle periods to avoid holding memory indefinitely.

Pool sizing, shrink interval, and shrink percentage are configurable via `FlowTweenSettings`. The debugger's **Profiler** tab shows live pool hit rate, total returns, and per-type interpolator counts.

---

## Editor Debugger

<img width="896" height="547" alt="d4" src="https://github.com/user-attachments/assets/a3bd5d3f-8492-4bdf-8081-df0bb7eeb24c" />
<img width="903" height="550" alt="d3" src="https://github.com/user-attachments/assets/a91249bf-1a41-4482-b1d1-43df34123ddd" />
<img width="859" height="281" alt="d2" src="https://github.com/user-attachments/assets/a9700b01-bc01-45ee-9696-257a2bc2dc8e" />
<img width="896" height="548" alt="d1" src="https://github.com/user-attachments/assets/96467d13-4ff7-422c-8197-a8b5f6c6f32b" />

Open with **Window › Analysis › FlowTween Debugger** or `Alt+Shift+T`.

| Tab | Contents |
|---|---|
| **Update** | All active idle tweens with progress bars, interpolator type, group, and per-tween controls |
| **Fixed** | All active fixed-update tweens |
| **Sequences** | Active sequences with per-step timeline |
| **Groups** | Tweens indexed by group name |
| **Pool** | Pool sizes, hit rates, and per-interpolator-type counts |
| **Event Log** | Start / Complete / Kill / Loop / Pause / Resume events with timestamps |
| **Ease Ref** | Interactive curve previewer for all 48 easing combinations |
| **Profiler** | Active count history sparkline and per-frame stats |
| **Settings** | Live editing of all `FlowTweenSettings` fields |

The debugger uses virtual scrolling — only visible cards are rendered regardless of how many tweens are active.

---

## Custom Interpolators

To animate a property that has no built-in extension method, implement `IPropertyInterpolator<TTarget, TValue>` as a `readonly struct`:

```csharp
internal readonly struct MyCustomInterpolator : IPropertyInterpolator<MyComponent, float>
{
    public float GetValue(MyComponent target) => target.myProperty;

    public void SetValue(MyComponent target, float from, float to, float t)
        => target.myProperty = Mathf.LerpUnclamped(from, to, t);
}

// Use it
FlowTween.GetTween<MyComponent, float, MyCustomInterpolator>(component, duration, targetValue);
```

The `StructTweenInterpolator<TTarget, TValue, TInterp>` wrapper handles pooling, `SetRelative`, `SetFrom`, and `SetSpeedBase` automatically.

---

## Settings

Open with **Window › FlowTween › Settings** or `Ctrl+Shift+Alt+S`.

<img width="715" height="649" alt="settings3" src="https://github.com/user-attachments/assets/32237037-f5d0-440f-9d7c-4c054bb43560" />
<img width="713" height="645" alt="settings2" src="https://github.com/user-attachments/assets/68f622a9-bc73-42ad-8ab1-5af5d735ce0d" />

| Field | Description |
|---|---|
| `defaultTransition` | Transition curve applied to all new tweens |
| `defaultEase` | Ease mode applied to all new tweens |
| `globalTimeScale` | Master time multiplier (does not affect unscaled tweens) |
| `killOnSceneUnload` | Automatically kill tweens when a scene is unloaded |
| `autoKillOrphans` | Kill tweens whose `Target` component has been destroyed |
| `minPoolSize` | Minimum tween/sequence count kept in the pool after shrinking |
| `shrinkInterval` | Seconds between pool shrink passes |
| `shrinkPercent` | Fraction of excess pool items removed per shrink pass |
| `prewarmTweens` | Tweens to allocate at startup |
| `prewarmSequences` | Sequences to allocate at startup |

---

## Requirements

- Unity 2021.3 or later
- TextMeshPro for the TMP extension methods (optional; the rest of the library has no TMP dependency)
