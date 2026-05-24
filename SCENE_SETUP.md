# Unity Souls-like — Scene Setup Guide

## Project Layout

```
Assets/Scripts/
├── Player/
│   ├── PlayerController.cs       — 이동, 회피, 중력, 상태머신
│   ├── PlayerCombat.cs           — 공격, 가드, 패리
│   └── PlayerAnimationEvents.cs  — 애니메이션 이벤트 콜백
├── Systems/
│   ├── HealthSystem.cs           — HP 공유 컴포넌트 (플레이어/적 공용)
│   ├── StaminaSystem.cs          — 스태미나 소모·회복
│   ├── LockOnSystem.cs           — 락온 타겟 탐색·전환
│   └── HitboxController.cs       — 히트박스 활성/비활성 + IParryable
├── Camera/
│   └── CameraController.cs       — 자유시점 / 락온 카메라
├── Enemy/
│   ├── EnemyAI.cs                — 상태머신 기반 일반 적 AI
│   ├── BossAI.cs                 — 페이즈 전환 + 4종 보스 패턴
│   └── BossSpawner.cs            — 보스방 진입 트리거
└── UI/
    └── HUDManager.cs             — HP/스태미나 바, 보스 바, 락온 레티클
```

---

## Player GameObject 설정

1. **컴포넌트 추가** (순서 무관)
   - `CharacterController`
   - `PlayerController`
   - `PlayerCombat`
   - `HealthSystem`
   - `StaminaSystem`
   - `LockOnSystem`
   - `HitboxController`

2. **HitboxController**
   - Inspector의 `Hitboxes` 리스트에 무기 오브젝트의 `Collider`를 연결
   - Collider는 평소 **비활성(disabled)** 상태로 유지

3. **PlayerController Inspector**
   - `Camera Transform` → 메인 카메라 Transform

4. **LockOnSystem Inspector**
   - `Enemy Layer` → 적 레이어 마스크
   - `Obstruction Layer` → 지형 레이어 마스크

5. **Tag** → `Player` 설정 필수

---

## Camera GameObject 설정

1. 별도 빈 오브젝트에 `CameraController` 부착
2. Inspector:
   - `Target` → 플레이어 Transform
   - `Lock On` → LockOnSystem 컴포넌트 참조
   - `Collision Layer` → 지형 레이어 마스크

---

## Enemy (일반) 설정

컴포넌트:
- `NavMeshAgent`
- `EnemyAI`
- `HealthSystem`
- `HitboxController`

Inspector:
- `Patrol Points` → 순찰 경로 Transform 배열 (없으면 Idle 상태 유지)
- `Player Layer` → 플레이어 레이어 마스크

---

## Boss 설정

컴포넌트 (`EnemyAI` 대신 `BossAI` 사용):
- `NavMeshAgent`
- `BossAI`
- `HealthSystem`
- `HitboxController`
- `BossSpawner` (보스방 진입 트리거 오브젝트에 별도 부착)

BossSpawner Inspector:
- `Boss` → BossAI 컴포넌트 참조
- `Boss Name` → HUD에 표시할 보스 이름

---

## 입력 키맵

| 키 | 동작 |
|---|---|
| WASD | 이동 |
| Shift | 달리기 |
| Space | 회피 (스태미나 소모) |
| 마우스 좌클릭 | 약공격 / 콤보 |
| 마우스 우클릭 | 강공격 |
| Ctrl + 우클릭 | 패리 |
| Ctrl 유지 | 가드 |
| F | 락온 토글 |
| Q / E | 락온 타겟 전환 |

---

## 애니메이터 파라미터

| 이름 | 타입 | 용도 |
|---|---|---|
| Speed | Float | 이동 블렌드 |
| Dodge | Trigger | 회피 |
| LightAttack | Trigger | 약공격 |
| HeavyAttack | Trigger | 강공격 |
| ComboIndex | Int | 콤보 단계 (1~3) |
| Block | Bool | 가드 자세 |
| Parry | Trigger | 패리 |
| Stagger | Trigger | 경직 |
| Dead | Trigger | 사망 |
| Attack | Trigger | 적 공격 (EnemyAI) |
| Slam / Sweep / Charge / RageBurst | Trigger | 보스 패턴 |
| Phase2 | Trigger | 보스 페이즈 전환 |

---

## HUD Canvas 설정

`HUDManager` 컴포넌트 Inspector에 연결:
- `Health Slider` — 플레이어 HP 슬라이더
- `Stamina Slider` — 스태미나 슬라이더
- `Boss Bar Root` — 보스 바 오브젝트 (비활성 상태로 시작)
- `Boss Health Slider` — 보스 HP 슬라이더
- `Boss Name Text` — 보스 이름 텍스트
- `Lock On Reticle` — 락온 UI 이미지

---

## NavMesh 굽기

1. `Window > AI > Navigation` 열기
2. 지형 오브젝트를 `Navigation Static`으로 설정
3. `Bake` 탭에서 Bake 실행
