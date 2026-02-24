# FlowTween

A tweening library for Unity built around a fluent, chainable API. No setup required — drop the files in, and it works.

---

## Installation

<img width="351" height="152" alt="image" src="https://github.com/user-attachments/assets/8cbacd72-dd02-4c63-84cc-a1f12e58bb63" />


Open Unity Package Manager -> install from git URL -> paste repo git URL

---

## Quick Start

```csharp
using FlT;

// Move a transform to a world position over 0.5s
transform.FlowMove(new Vector3(3f, 0f, 0f), 0.5f);

// Fade a CanvasGroup out with an ease curve
canvasGroup.FlowFadeOut(0.3f).Sine().EaseOut();

// Chain callbacks and modifiers
transform.FlowScale(Vector3.one * 1.5f, 0.4f)
    .Back()
    .EaseOut()
    .SetDelay(0.1f)
    .OnComplete(() => Debug.Log("done"));
```

Every `Flow*` method returns a `Tween` you can configure before it starts playing. Tweens are pooled and begin automatically

---

## Component Coverage

### Transform
| Method | Description |
|---|---|
| `FlowMove(to, duration)` | World position |
| `FlowMoveX/Y/Z(to, duration)` | Single world axis |
| `FlowMoveLocal(to, duration)` | Local position |
| `FlowMoveLocalX/Y/Z(to, duration)` | Single local axis |
| `FlowRotate(to, duration)` | World rotation (Quaternion or Euler) |
| `FlowRotateLocal(to, duration)` | Local rotation (Quaternion or Euler) |
| `FlowScale(to, duration)` | Local scale |
| `FlowScaleUniform(scale, duration)` | Uniform scale shorthand |
| `FlowSpin(duration)` | Continuous 360° Z-axis spin |
| `FlowSpin(axis, duration)` | Continuous 360° spin around any axis |
| `FlowPath(waypoints, duration)` | Catmull-Rom spline, world space |
| `FlowPathLocal(waypoints, duration)` | Catmull-Rom spline, local space |
| `FlowLookAt(provider, duration)` | Track a world-space target |
| `FlowLookAt2D(provider, duration)` | 2D version (Z-axis only) |

### UI
| Method | Description |
|---|---|
| `FlowAnchorMove(to, duration)` | RectTransform anchored position |
| `FlowSizeDelta(to, duration)` | RectTransform size |
| `FlowAnchorMin/Max(to, duration)` | Anchor min/max |
| `FlowOffsetMin/Max(to, duration)` | Offset edges |
| `FlowPivot(to, duration)` | Pivot point |
| `FlowFade/FadeIn/FadeOut` | CanvasGroup or Graphic alpha |
| `FlowFadeEnable/FadeDisable` | Fade with interactable/blocksRaycasts toggle |
| `FlowColor(to, duration)` | Graphic color |
| `FlowGradient(gradient, duration)` | Sample a Gradient over time |
| `FlowFillAmount(to, duration)` | Image fill |
| `FlowValue(to, duration)` | Slider value |
| `FlowPosition(to, duration)` | ScrollRect scroll position |

### TextMeshPro *(requires `FLOWTWEEN_TMP_SUPPORT` define)*
| Method | Description |
|---|---|
| `FlowReveal(duration)` | Fade-in all characters via vertex alpha |
| `FlowTypewriter(duration)` | Reveal characters one by one |
| `FlowCounter(from, to, duration)` | Animate a number display |
| `FlowCounter(from, to, duration, formatter)` | Custom format callback |
| `FlowCharacterColor(to, duration)` | Vertex color on all characters |

### SpriteRenderer
`FlowFade`, `FlowFadeIn`, `FlowFadeOut`, `FlowColor`, `FlowGradient`

### Renderer / Material
`FlowColor`, `FlowMaterialFloat`, `FlowMaterialColor`, `FlowMaterialVector`, `FlowMaterialTiling`, `FlowMaterialOffset`

### Light
`FlowIntensity`, `FlowColor`, `FlowRange`

### Camera
`FlowFov`, `FlowOrthoSize`, `FlowBackgroundColor`, `FlowRect`

### AudioSource
`FlowVolume`, `FlowPitch`, `FlowPanStereo`, `FlowFadeOutAndStop`

### Rigidbody / Rigidbody2D
`FlowPosition`, `FlowRotation` — automatically run in `FixedUpdate`.

### SkinnedMeshRenderer
`FlowBlendShape(shapeIndex, to, duration)`

---

## Virtual Tweens

For animating arbitrary values not tied to a component, use `FlowVirtual`:

```csharp
FlowVirtual.Float(0f, 100f, 1f, value => myField = value);
FlowVirtual.Int(0, 10, 0.5f, value => score = value);
FlowVirtual.Vector2(Vector2.zero, Vector2.one, 1f, v => rect.pivot = v);
FlowVirtual.Vector3(from, to, duration, v => myVar = v);
FlowVirtual.Color(Color.black, Color.white, 1f, c => mat.color = c);
```

---

## Easing

Every tween accepts a transition type and an ease direction. Chain them directly on the returned tween:

```csharp
transform.FlowMove(target, 0.5f).Elastic().EaseOut();
transform.FlowScale(big, 0.3f).Back().EaseInOut();

// or

transform.FlowMove(target, 0.5f).SetTransition(Tween.TransitionType.Linear).SetEase(Tween.EaseType.Out);
transform.FlowScale(big, 0.3f).SetTransition(Tween.TransitionType.Back).SetEase(Tween.EaseType.InOut);
```

**Transition types:** `Linear`, `Sine`, `Quad`, `Cubic`, `Quart`, `Quint`, `Expo`, `Circ`, `Back`, `Elastic`, `Bounce`, `Spring`

**Ease directions:** `EaseIn()`, `EaseOut()`, `EaseInOut()`, `EaseOutIn()`

You can also supply a custom `AnimationCurve`:

```csharp
transform.FlowMove(target, 1f).SetCurve(myCurve);
```

---

## Tween Modifiers

```csharp
tween
    .SetDelay(0.2f)                          // Wait before starting
    .SetLoops(3)                              // Repeat N times
    .SetLoops(-1)                             // Infinite loops
    .SetLoops(2, Tween.LoopType.Yoyo)        // Ping-pong
    .SetFrom(Vector3.zero)                    // Override start value
    .SetRelative()                            // Target is an offset, not absolute
    .SetTimeScale(0.5f)                       // Per-tween time multiplier
    .SetUnscaledTime()                        // Ignore Time.timeScale
    .SetSpeedBase(5f)                         // Duration derived from distance / speed
    .SetId("my-tween")                        // Identifier for KillById
    .SetGroup("UI")                           // Named group for batch control
    .SetUpdateMode(Tween.TweenUpdateMode.Fixed) // Run in FixedUpdate
    .SetCurve(curve)                          // Custom AnimationCurve
```

---

## Callbacks

```csharp
tween
    .OnStart(() => { })
    .OnUpdate(t => { })       // t is the eased progress (0–1)
    .OnLoop(count => { })
    .OnComplete(() => { })
    .OnKill(() => { });
```

---

## Chaining with `.Then()`

```csharp
transform.FlowMove(posA, 0.5f)
    .Then(transform.FlowMove(posB, 0.5f)
        .Then(transform.FlowMove(posC, 0.5f)));
```

Each tween in the chain starts when the previous one completes.

---

## Sequences

Sequences let you compose multiple tweens with precise timing control.

```csharp
FlowTween.Sequence()
    .Append(transform.FlowMove(posA, 0.5f))          // after the previous step
    .Append(canvasGroup.FlowFadeOut(0.3f))
    .Join(transform.FlowScale(Vector3.one * 1.2f, 0.3f)) // parallel with previous
    .AppendInterval(0.1f)                             // blank gap
    .Insert(0f, transform.FlowRotate(rot, 1f))        // at an explicit time
    .AppendCallback(() => Debug.Log("halfway"))
    .InsertCallback(0.25f, () => Debug.Log("at 0.25s"))
    .Prepend(someOtherTween)                          // shift everything forward
    .SetLoops(2)
    .OnComplete(() => Debug.Log("sequence done"))
    .Play();
```

Sequences support the same loop, callback, and control API as individual tweens.

---

## Effect Helpers

These are higher-level effects built on top of the core tween system.

```csharp
// Squash and stretch (returns a Sequence)
transform.FlowSquish(0.4f, ratio: 0.25f, SquishDirection.Up);

// Oscillating jello wobble
transform.FlowJello(0.6f, intensity: 0.25f, frequency: 4f);

// Heartbeat double-pulse
transform.FlowHeartbeat(0.5f, intensity: 0.3f, beats: 2);

// Decaying rotation wobble
transform.FlowWobbleRotate(0.5f, strength: 20f, frequency: 4f);

// Punch position/scale (2D and 3D variants)
transform.FlowPunchPosition(new Vector2(10f, 0f), 0.4f, vibrato: 8);
transform.FlowPunchScale(0.3f, 0.4f);
transform.FlowPunchPosition3D(new Vector3(0, 0, 5f), 0.4f);

// Shake position/rotation/scale
transform.FlowShakePosition(strength: 0.3f, duration: 0.5f);
transform.FlowShakeRotation(strength: 15f, duration: 0.5f);
transform.FlowShakeScale(strength: 0.2f, duration: 0.5f);

// Card flip
transform.FlowFlipY(0.4f);           // 180° flip
transform.FlowFlipY(0.4f, full: true); // 360° flip
transform.FlowFlipX(0.4f);

// Blink
spriteRenderer.FlowBlink(blinks: 4, blinkDuration: 0.1f);
canvasGroup.FlowBlink(blinks: 3);

// UI slide in/out
rectTransform.FlowSlideIn(SlideDirection.Left, offset: 200f, duration: 0.3f);
rectTransform.FlowSlideOut(SlideDirection.Right, offset: 200f, duration: 0.3f);
```

---

## Global Controls

```csharp
// All tweens
FlowTween.KillAll();
FlowTween.PauseAll();
FlowTween.ResumeAll();
FlowTween.CompleteAll();

// By target object
FlowTween.KillTarget(gameObject);
FlowTween.CompleteTarget(gameObject);

// By id
FlowTween.KillById("my-tween");

// By group
FlowTween.KillGroup("UI");
FlowTween.PauseGroup("UI");
FlowTween.ResumeGroup("UI");

// Enum groups (no string allocation)
FlowTween.KillGroup(TweenGroup.Enemies);

// Extension shortcuts on any UnityEngine.Object
transform.FlowKill();
canvasGroup.FlowComplete();

// Sequences
FlowTween.KillSequences();
FlowTween.PauseSequences();
FlowTween.ResumeSequences();
```

---

## Async / Await

Tweens and sequences can be awaited natively:

```csharp
// Simple await
await transform.FlowMove(target, 0.5f);

// With cancellation
await transform.FlowMove(target, 0.5f).AwaitAsync(cancellationToken);

// Sequences
Sequence seq = FlowTween.Sequence()
    .Append(...)
    .Play();
await seq;
```

Awaiting a tween with `SetLoops(-1)` will throw — use a `CancellationToken` to control infinite tweens from async code instead.

---

## Settings

Create a `FlowTweenSettings` asset at `Resources/FlowTweenSettings` to configure the library globally:

| Setting | Default | Description |
|---|---|---|
| `defaultTransition` | Linear | Curve shape for all new tweens |
| `defaultEase` | In | Ease direction for all new tweens |
| `globalTimeScale` | 1.0 | FlowTween-layer time multiplier (independent of `Time.timeScale`) |
| `prewarmTweens` | 32 | Tween pool size at startup |
| `prewarmSequences` | 8 | Sequence pool size at startup |
| `minPoolSize` | 10 | Pool floor — never trimmed below this |
| `shrinkInterval` | 10s | How often excess pool entries are released |
| `shrinkPercent` | 0.25 | Fraction of excess removed per trim pass |
| `killOnSceneUnload` | true | Kill tweens whose target belongs to an unloaded scene |
| `autoKillOrphans` | true | Kill tweens whose target `UnityObject` has been destroyed |

Settings can also be applied at runtime:

```csharp
FlowTween.ApplySettings(mySettingsAsset);
```

---

## Object Pooling

All `Tween` and `Sequence` objects are pooled. You don't manage this yourself — tweens return to the pool automatically when they complete or are killed. You can pre-warm the pool manually if needed:

```csharp
FlowTween.Prewarm(tweenCount: 64, sequenceCount: 16);
```

Pool hit rates and pool sizes are exposed as static properties for profiling (`FlowTween.TweenPoolHitRate`, `FlowTween.TweenPoolSize`, etc.).

---

## Editor Tools

Two editor windows are included, accessible via the **FlowTween** menu:

**Debug Window** — Live view of all active tweens, fixed tweens, pending `.Then()` chains, and sequences. Shows progress bars, elapsed time, duration, interpolator type, and group membership for every running tween.

<img width="711" height="616" alt="image" src="https://github.com/user-attachments/assets/85891e6a-d44b-4ddf-a375-5dafb94c93c8" />
<img width="711" height="644" alt="image" src="https://github.com/user-attachments/assets/c04bea1a-b4d2-4bef-b51d-da19259e6070" />

**Settings Window** — GUI editor for the `FlowTweenSettings` asset with live apply during Play Mode.

<img width="710" height="646" alt="image" src="https://github.com/user-attachments/assets/09f26e37-25de-4ac6-9f36-c8437ef3567f" />

---

## TMP Support

TextMeshPro methods (`FlowReveal`, `FlowTypewriter`, `FlowCounter`, `FlowCharacterColor`) are compiled conditionally. Add `FLOWTWEEN_TMP_SUPPORT` to your project's **Scripting Define Symbols** in Player Settings to enable them.

---

## Notes

- The `[FlowTween]` GameObject is created automatically before the first scene loads and marked `DontDestroyOnLoad`. You'll see it in the hierarchy during Play Mode — this is expected.
- Tweens that target a destroyed `UnityEngine.Object` are killed automatically on the next update when `autoKillOrphans` is enabled.
- `Rigidbody` and `Rigidbody2D` tweens automatically switch to `FixedUpdate` mode. All other tweens run in `Update` by default, but this can be changed per-tween with `SetUpdateMode`.
- `SetRelative()` can be applied to individual tweens or to an entire `Sequence` at once via `sequence.SetRelative()`.
