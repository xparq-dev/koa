# Project KOA — MOBA Standard Control, Camera, and Arena Framing

## Executive decision

Project KOA should use discrete click-to-move, a free camera by default, temporary hero focus while Space is held, one-shot focus after a quick Space tap, and an optional persistent camera lock. The arena should remain a bridge over a chasm, but the exterior must be visually framed with cliff facades, lower-depth silhouettes, mist, and cloud banks so the player never reads it as an unfinished rectangular world.

These decisions preserve the Input Abstraction Layer and Decoupled Core required by Section 1.1. Movement remains a single `TargetDestination` intent sent to Simulation; click detection, command feedback, camera operation, and atmospheric framing remain Presentation concerns.^1

## Evidence from the current implementation

The previous movement adapter sampled both the press edge and the held state of the right mouse button. That converted mouse motion during a hold into a stream of changing destinations. Unity distinguishes `wasPressedThisFrame`, which represents the start of a press, from `isPressed`, which represents the entire held interval.^3 A discrete movement command must therefore be created only from the press edge.

The previous camera controller recomputed its desired position from the hero Transform every `LateUpdate`, so it had no free-camera state. A camera that supports tactical inspection needs an independent world-space focus point. Hero following should update that point only while a temporary or persistent lock is active.

The reference images show a consistent three-layer composition: a bright and readable gameplay surface, a strong architectural or cliff boundary, and a darker low-detail background with atmosphere. The previous view exposed abrupt world edges and large uniform regions, which made the generated arena read as an incomplete scene rather than a suspended battleground.^2

## Control contract

| Input | Result | State duration | Feedback |
|---|---|---:|---|
| Right mouse press on ground | Replace movement destination | One command | Cyan ring and diamond at destination |
| Right mouse hold or drag | No additional destinations | None | Existing marker continues fading |
| Right mouse press on enemy | Select and pursue attack target | One command | Red command marker on target |
| Mouse at screen edge | Pan tactical camera | While at edge | Camera motion only |
| Mouse wheel | Zoom | Per scroll step | Camera distance changes within 8–14 m |
| Quick Space tap | Return focus to the controlled hero | One action | Smooth recenter, then camera remains free |
| Hold Space | Follow the current focus target | While held | Release returns to free camera |
| Y | Toggle persistent follow | Until toggled again | Persistent locked/free state |
| Repeated quick Space taps | Cycle registered allied focus targets | Future team mode | Current 1v1 registration contains only the player |

This mapping satisfies the PC-first click-to-move requirement in Section 7.2 and maintains the required 8–14 metre zoom range in Section 7.1.^1 The `RegisterFocusTarget` hook is intentionally Presentation-only: it supports a later team mode without placing future team logic into the current 1v1 Simulation scope.

## Movement-command design

The screen pointer is converted to a world ray with `Camera.ScreenPointToRay`; Unity defines that ray as originating at the camera near plane and passing through the requested screen pixel.^4 The adapter keeps the hovered Collider for smart enemy commands but projects the movement destination onto the gameplay ground plane. This prevents the top of a tall prop or structure from becoming an elevated movement destination.

The command marker uses two world-space line renderers: a circular confirmation ring and a small directional diamond. Unity's `LineRenderer` is designed for free-floating 3D lines and supports a closed loop, per-line width, and world/local coordinate choice.^5 The marker is Presentation-only, lasts approximately 0.42 seconds, scales inward, fades, and never modifies movement or combat state.

## Camera state model

The camera has two independent states:

1. `FREE`: the focus point is moved by screen-edge input and clamped to the 1v1 arena inspection bounds.
2. `FOLLOW`: the focus point follows the registered Transform with a small velocity look-ahead. FOLLOW is active while Space is held or while persistent lock is enabled.

A quick Space tap places the focus point on the controlled hero and immediately returns control to FREE. Holding Space longer than the tap threshold keeps FOLLOW active until release. Y toggles persistent FOLLOW. This separation avoids ambiguous input: the right mouse button controls units, while camera inspection is performed only with pointer position, Space, Y, and the mouse wheel.

The camera focus is clamped to X -11.5..11.5 and Z -62..62. Those values allow inspection of the full Section 3.1 arena while reducing views beyond the authored environment.^1 Camera geometry visibility is limited to a 180 m far clipping plane; Unity defines `farClipPlane` as the most distant visible point of the camera frustum.^6

## Arena-framing strategy

The visual hierarchy should be implemented in three layers:

| Layer | Purpose | Current treatment |
|---|---|---|
| Gameplay surface | Highest readability and collision clarity | Stone lane, grass shoulders, restrained saturation |
| Boundary silhouette | Communicate height and prevent unfinished edges | Bridge facade panels, broken parapets, cliff rocks |
| Lower atmosphere | Hide world cuts and create scale | Lower valley, rivers, rock spires, waterfalls, mist and cloud banks |

Exterior geometry must not add walkable space. All added facade panels, spires, mist, and cloud banks are placed outside or below the Section 3.1 gameplay surface and have no gameplay Collider. Fog density is increased moderately to unify distant layers; optional future colour grading can be added through URP Volumes, which Unity uses to apply post-processing settings globally or locally to cameras.^7

The default camera distance is 11.5 m. This is close enough to keep heroes, weapons, and skill effects legible, while still allowing the player to zoom out to 14 m for tactical context. The presentation should use contrast to guide attention: neutral stone and dark green at the centre, team colour only on structures and commands, and low-saturation blue-gray in the chasm.

## Architecture and scope

No new camera or marker state is introduced into `KOA.Core`. The existing `InputFrame` remains the platform-neutral boundary described by Section 1.1. `PCInputAdapter` creates a single movement intent, `HeroView` resolves ground versus enemy commands, `MoveCommandIndicatorView` displays feedback, and `TopDownCameraController` owns camera state.

The allied-focus cycle is a compatibility hook, not a claim that team mode is implemented. Section 12 explicitly defers multi-player team-map scope beyond Version 1.0.0.^1 The current 1v1 bootstrap registers only the controlled hero; a future team presentation coordinator can register allies in stable roster order.

## Acceptance checks

- One right-click produces exactly one movement destination.
- Holding the right mouse button and moving the cursor does not redirect the hero.
- Every ground command shows a cyan marker at the accepted destination.
- Every enemy smart command shows a red marker.
- The camera does not follow the hero after normal movement while in FREE state.
- Moving the pointer to any screen edge pans the camera and stops when the pointer leaves the edge.
- A quick Space tap recentres on the hero without leaving the camera permanently locked.
- Holding Space follows the hero and releasing Space returns to FREE.
- Y toggles persistent follow.
- Camera focus cannot leave the authored inspection bounds.
- Default view does not expose a hard rectangular end of the generated arena.
- Added exterior visuals have no Collider and do not change Section 3.1 movement bounds.

## Remaining art risks

The atmosphere pass improves composition using existing runtime materials and imported environment models, but it is still a demo-quality procedural assembly. Final-quality results require authored cliff modules, an arena trim kit, controlled LODs, baked or mixed lighting, and a deliberate URP post-processing profile. These are Presentation production tasks and should be verified during the Phase 3 manual playtest and Phase 4 performance pass rather than treated as completed solely from source inspection.^8

## Sources

1. Project KOA. `Project_KOA_1v1_Complete_Requirement_v1.0.0.md`, Sections 1.1, 3.1, 7.1, 7.2, and 12. Local project document.
2. Project KOA visual references supplied in the implementation conversation, three arena screenshots. Accessed 2026-09-09.
3. Unity Technologies. “[ButtonControl — Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/api/UnityEngine.InputSystem.Controls.ButtonControl.html).” Accessed 2026-09-09.
4. Unity Technologies. “[Camera.ScreenPointToRay](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera.ScreenPointToRay.html).” Unity 6. Accessed 2026-09-09.
5. Unity Technologies. “[LineRenderer](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/LineRenderer.html).” Unity 6. Accessed 2026-09-09.
6. Unity Technologies. “[Camera.farClipPlane](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera-farClipPlane.html).” Unity 6. Accessed 2026-09-09.
7. Unity Technologies. “[Volumes in URP](https://docs.unity3d.com/6000.0/Manual/urp/Volumes.html).” Unity 6. Accessed 2026-09-09.
8. Project KOA. `Project_KOA_Production_Roadmap.md`, Phase 3 manual balance and Phase 4 hardening gates. Local project document.
