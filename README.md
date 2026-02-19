# 🧊 On-Device Procedural 3D Builder

> An engine-agnostic, offline-first Flutter framework for procedural 3D scene composition — designed to host Unity (or any native rendering engine) as a platform view, with a clean Dart orchestration layer that runs entirely on-device.

---

## 📌 Project Vision

Build a fully offline, on-device 3D modelling and scene composition tool. The application acts as a **host framework** — the Flutter layer handles configuration, validation, scene management, lifecycle orchestration, and UI, while a native 3D engine (Unity, Godot, etc.) is embedded as a platform view for actual rendering.

**Core design principles:**
- 🔌 **Engine-agnostic** — swap rendering engines without touching core logic
- 📵 **Fully offline** — no cloud dependency, all processing on-device
- 🧱 **Clean architecture** — strict separation between UI, orchestration, validation, and engine
- 🛡️ **Production-hardened** — defensive guards, structured exceptions, validated state machines
- 🧪 **Test-first** — every layer covered by unit and integration tests

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│                    Flutter UI Layer                  │
│         SceneHostScreen  ·  EngineContainer          │
└────────────────────┬────────────────────────────────┘
                     │ injects
┌────────────────────▼────────────────────────────────┐
│              RenderingOrchestrator                   │
│  • SceneValidator (strict Schema v1.0)               │
│  • EngineLifecycleManager (state machine)            │
│  • Event relay stream (UI-safe broadcast)            │
└────────────────────┬────────────────────────────────┘
                     │ implements
┌────────────────────▼────────────────────────────────┐
│              RenderEngine (abstract contract)        │
│         MockEngineAdapter  ·  [Unity adapter — WIP]  │
└─────────────────────────────────────────────────────┘
```

---

## 📁 Project Structure

```
lib/
├── core/
│   ├── config/          # AppConfig — environment-aware configuration
│   ├── errors/          # EngineException, ValidationException
│   ├── logging/         # AppLogger — structured, level-based logging
│   └── utils/           # JsonUtils
├── engine/
│   ├── contract/        # RenderEngine interface, EngineEvent
│   ├── adapters/        # MockEngineAdapter (dev/test)
│   ├── lifecycle/       # EngineLifecycleManager (state machine)
│   └── orchestrator/    # RenderingOrchestrator (core pipeline)
├── scene/
│   ├── fixtures/        # TestSceneFixture (Schema v1.0 test data)
│   ├── models/          # SceneModel
│   ├── repository/      # SceneRepository
│   └── validators/      # SceneValidator (strict schema enforcement)
├── features/
│   └── scene_host/      # SceneHostScreen, EngineContainer
├── services/            # LocalCacheService
└── main.dart            # DI composition root

test/
├── widget_test.dart              # App smoke test
├── pipeline_integration_test.dart  # 12 pipeline integration tests
└── stability_stress_test.dart      # 16 stability/stress tests
```

---

## ✅ Phase 1 Progress — Core & Engine Foundation

> **Status: Complete** · 29/29 tests passing · `flutter analyze` clean

### Phase 1.1 — Core Foundation
- [x] `AppConfig` — immutable, environment-aware (`development` / `staging` / `production`)
- [x] `AppLogger` — timestamped, level-based logging (info / warning / error), injected config
- [x] `EngineException` — structured engine error with `code`, `message`, `metadata`, `originalError`
- [x] `ValidationException` — structured schema error with `fieldName` and `reason`

### Phase 1.2 — Project Structure
- [x] Scalable, engine-agnostic folder hierarchy under `lib/`
- [x] No global singletons — all services injected via constructors
- [x] Clean dependency boundaries between layers

### Phase 1.3 — Engine Abstraction Layer
- [x] `RenderEngine` — abstract contract (`initialize`, `loadScene`, `clearScene`, `dispose`, `events`)
- [x] `EngineEvent` — immutable event struct with `type`, `payload`, `timestamp`
- [x] `EngineLifecycleManager` — 6-state machine with validated transitions, `isBusy`, `isDisposed`, `reset()`
- [x] `MockEngineAdapter` — simulates async engine with broadcast events, post-dispose guards

### Phase 1.4 — Strict Schema Validator
- [x] `SceneValidator.validateStrict()` — enforces Scene Schema v1.0
- [x] Validates: required root keys, no unknown keys, schema version, unique material/object IDs
- [x] Validates: `base_color` format, geometry primitives (`cube`, `sphere`, `cylinder`, `plane`, `dome`, `arch`), `material_ref` presence
- [x] Read-only — never mutates input; throws `ValidationException` on first violation

### Phase 1.5 — Rendering Pipeline Integration
- [x] `RenderingOrchestrator` — bridges validation → lifecycle → engine
- [x] Validates with `SceneValidator.validateStrict()` before any engine call
- [x] Event-driven lifecycle: `scene_ready` → `ready`, `error` → `error`
- [x] Exposes `events` relay stream (UI subscribes here, not to engine directly)

### Phase 1.6 — Scene Host UI
- [x] `SceneHostScreen` — injects orchestrator, auto-inits on load, shows live state
- [x] `EngineContainer` — placeholder for future Unity platform view with lifecycle badge overlay
- [x] `TestSceneFixture` — Schema v1.0 compliant test scene (cube + material)
- [x] `main.dart` — full DI composition root wiring all layers

### Phase 1.7 — Stability Hardening
- [x] Post-disposal guard on every orchestrator method (`ORCHESTRATOR_DISPOSED`)
- [x] Concurrent load rejection — throws `EngineException(LOAD_ALREADY_IN_PROGRESS)` when rendering
- [x] Idempotent `dispose()` — safe to call multiple times
- [x] `reinitialize()` — error recovery path (`error → uninitialized → initializing → ready`)
- [x] 16 stress tests: rapid load rejection, dispose-during-render, valid→invalid→valid, recovery guards

---

## 🧪 Test Coverage

| Suite | Tests | Coverage |
|---|---|---|
| Widget smoke test | 1 | App mounts, SceneHostScreen visible |
| Pipeline integration | 12 | Valid scene, schema violations, lifecycle, disposal |
| Stability stress | 16 | Concurrent loads, post-dispose, recovery, state machine |
| **Total** | **29** | **All passing ✅** |

---

## 🔜 Upcoming Phases

- **Phase 2** — Unity Platform View integration (Android)
- **Phase 3** — Scene builder UI (object placement, material picker)
- **Phase 4** — Local scene persistence (SQLite / Hive)
- **Phase 5** — Export pipeline (GLTF / OBJ)

---

## 🛠️ Getting Started

```bash
# Install dependencies
flutter pub get

# Run tests
flutter test

# Run the app
flutter run
```

**Requirements:** Flutter SDK `>=3.0.0`, Dart `>=3.0.0`

---

## 📄 License

Private repository — all rights reserved.
