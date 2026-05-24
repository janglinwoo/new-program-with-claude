#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

// Unity 메뉴: Soulslike > Build Scene
// 이 스크립트를 Assets/Editor/ 폴더에 넣고 실행하면
// 씬 오브젝트 생성, 컴포넌트 부착, 레이어 설정이 자동으로 완료됩니다.
public static class SoulslikeSceneBuilder
{
    [MenuItem("Soulslike/Build Scene (Auto Setup)")]
    public static void BuildScene()
    {
        EnsureLayers();
        EnsureTags();

        var ground = CreateGround();
        var player = CreatePlayer();
        var cam = CreateCamera(player);
        var enemy = CreateEnemy(player);
        var boss = CreateBoss(player);
        var hud = CreateHUD();

        BakeNavMesh();

        // Wire up camera references
        var camCtrl = cam.GetComponent<CameraController>();
        var serialCam = new SerializedObject(camCtrl);
        serialCam.FindProperty("target").objectReferenceValue = player.transform;
        serialCam.FindProperty("lockOn").objectReferenceValue = player.GetComponent<LockOnSystem>();
        serialCam.ApplyModifiedProperties();

        Selection.activeGameObject = player;
        Debug.Log("[SoulslikeSceneBuilder] 씬 자동 구성 완료!");
        EditorUtility.DisplayDialog("완료", "소울라이크 씬 구성이 완료되었습니다!\n\n" +
            "1. 애니메이터 컨트롤러를 Player/Enemy에 연결하세요.\n" +
            "2. 무기 오브젝트의 Collider를 HitboxController에 연결하세요.\n" +
            "3. Play 버튼을 눌러 테스트하세요.", "확인");
    }

    // ── Ground ──────────────────────────────────────────────────────────────
    static GameObject CreateGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(10f, 1f, 10f);
        ground.layer = LayerMask.NameToLayer("Default");

        // NavMesh Surface는 패키지 필요 — 대신 Static 플래그 설정
        GameObjectUtility.SetStaticEditorFlags(ground,
            StaticEditorFlags.NavigationStatic |
            StaticEditorFlags.ContributeGI);
        return ground;
    }

    // ── Player ──────────────────────────────────────────────────────────────
    static GameObject CreatePlayer()
    {
        var root = new GameObject("Player");
        root.tag = "Player";
        root.layer = LayerMask.NameToLayer("Default");
        root.transform.position = new Vector3(0f, 0.5f, 0f);

        // 비주얼 (임시 캡슐)
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = Vector3.zero;
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

        // 무기 히트박스
        var weapon = new GameObject("Weapon");
        weapon.transform.SetParent(root.transform);
        weapon.transform.localPosition = new Vector3(0.6f, 0.8f, 0.8f);
        var weaponCol = weapon.AddComponent<BoxCollider>();
        weaponCol.size = new Vector3(0.2f, 0.2f, 0.8f);
        weaponCol.isTrigger = true;
        weaponCol.enabled = false;

        // CharacterController
        var cc = root.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        // Systems
        root.AddComponent<HealthSystem>();
        root.AddComponent<StaminaSystem>();

        var lockOn = root.AddComponent<LockOnSystem>();
        var serialLock = new SerializedObject(lockOn);
        serialLock.FindProperty("enemyLayer").intValue = LayerMask.GetMask("Enemy");
        serialLock.FindProperty("obstructionLayer").intValue = LayerMask.GetMask("Default");
        serialLock.ApplyModifiedProperties();

        var hitbox = root.AddComponent<HitboxController>();
        var serialHitbox = new SerializedObject(hitbox);
        var hitboxList = serialHitbox.FindProperty("hitboxes");
        hitboxList.arraySize = 1;
        hitboxList.GetArrayElementAtIndex(0).objectReferenceValue = weaponCol;
        serialHitbox.ApplyModifiedProperties();

        root.AddComponent<PlayerController>();
        root.AddComponent<PlayerCombat>();

        return root;
    }

    // ── Camera ──────────────────────────────────────────────────────────────
    static GameObject CreateCamera(GameObject player)
    {
        var camObj = new GameObject("SoulsCamera");
        camObj.transform.position = new Vector3(0f, 3f, -5f);

        var camChild = new GameObject("MainCamera");
        camChild.tag = "MainCamera";
        camChild.transform.SetParent(camObj.transform);
        camChild.transform.localPosition = Vector3.zero;
        camChild.AddComponent<Camera>();
        camChild.AddComponent<AudioListener>();

        var ctrl = camObj.AddComponent<CameraController>();
        var serial = new SerializedObject(ctrl);
        serial.FindProperty("collisionLayer").intValue = LayerMask.GetMask("Default");
        serial.ApplyModifiedProperties();

        return camObj;
    }

    // ── Enemy ────────────────────────────────────────────────────────────────
    static GameObject CreateEnemy(GameObject player)
    {
        var root = new GameObject("Enemy");
        root.layer = LayerMask.NameToLayer("Enemy");
        root.transform.position = new Vector3(5f, 0.5f, 5f);
        root.tag = "Enemy";

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = Vector3.zero;
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

        var col = root.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.center = new Vector3(0f, 0.9f, 0f);

        var agent = root.AddComponent<NavMeshAgent>();
        agent.speed = 3.5f;
        agent.stoppingDistance = 1.8f;

        var weapon = new GameObject("EnemyWeapon");
        weapon.transform.SetParent(root.transform);
        weapon.transform.localPosition = new Vector3(0.6f, 0.8f, 0.8f);
        var weaponCol = weapon.AddComponent<BoxCollider>();
        weaponCol.isTrigger = true;
        weaponCol.enabled = false;

        root.AddComponent<HealthSystem>();

        var hitbox = root.AddComponent<HitboxController>();
        var serialHitbox = new SerializedObject(hitbox);
        var hitboxList = serialHitbox.FindProperty("hitboxes");
        hitboxList.arraySize = 1;
        hitboxList.GetArrayElementAtIndex(0).objectReferenceValue = weaponCol;
        serialHitbox.ApplyModifiedProperties();

        var ai = root.AddComponent<EnemyAI>();
        var serialAi = new SerializedObject(ai);
        serialAi.FindProperty("playerLayer").intValue = LayerMask.GetMask("Default");
        serialAi.ApplyModifiedProperties();

        return root;
    }

    // ── Boss ─────────────────────────────────────────────────────────────────
    static GameObject CreateBoss(GameObject player)
    {
        var root = new GameObject("Boss");
        root.layer = LayerMask.NameToLayer("Enemy");
        root.transform.position = new Vector3(0f, 0.5f, 15f);
        root.tag = "Enemy";

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

        var col = root.AddComponent<CapsuleCollider>();
        col.height = 2.7f;
        col.radius = 0.75f;
        col.center = new Vector3(0f, 1.35f, 0f);

        var agent = root.AddComponent<NavMeshAgent>();
        agent.speed = 3f;
        agent.stoppingDistance = 2.5f;
        agent.radius = 0.75f;

        var weapon = new GameObject("BossWeapon");
        weapon.transform.SetParent(root.transform);
        weapon.transform.localPosition = new Vector3(0.8f, 1f, 1f);
        var weaponCol = weapon.AddComponent<BoxCollider>();
        weaponCol.size = new Vector3(0.3f, 0.3f, 1.5f);
        weaponCol.isTrigger = true;
        weaponCol.enabled = false;

        root.AddComponent<HealthSystem>();

        var hitbox = root.AddComponent<HitboxController>();
        var serialHitbox = new SerializedObject(hitbox);
        var hitboxList = serialHitbox.FindProperty("hitboxes");
        hitboxList.arraySize = 1;
        hitboxList.GetArrayElementAtIndex(0).objectReferenceValue = weaponCol;
        serialHitbox.ApplyModifiedProperties();

        var ai = root.AddComponent<BossAI>();
        var serialAi = new SerializedObject(ai);
        serialAi.FindProperty("playerLayer").intValue = LayerMask.GetMask("Default");
        serialAi.ApplyModifiedProperties();

        // 보스방 진입 트리거
        var trigger = new GameObject("BossTrigger");
        trigger.transform.position = new Vector3(0f, 0.5f, 8f);
        var triggerCol = trigger.AddComponent<BoxCollider>();
        triggerCol.isTrigger = true;
        triggerCol.size = new Vector3(6f, 2f, 1f);

        var spawner = trigger.AddComponent<BossSpawner>();
        var serialSpawner = new SerializedObject(spawner);
        serialSpawner.FindProperty("boss").objectReferenceValue = ai;
        serialSpawner.FindProperty("bossName").stringValue = "고대의 군주";
        serialSpawner.ApplyModifiedProperties();

        return root;
    }

    // ── HUD ──────────────────────────────────────────────────────────────────
    static GameObject CreateHUD()
    {
        var canvas = new GameObject("HUD_Canvas");
        var c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();

        // HP Bar
        var hpBar = CreateSlider(canvas.transform, "HealthBar",
            new Vector2(-250f, -40f), new Vector2(300f, 20f));

        // Stamina Bar
        var staminaBar = CreateSlider(canvas.transform, "StaminaBar",
            new Vector2(-250f, -70f), new Vector2(300f, 15f));

        // Boss Bar (비활성)
        var bossRoot = new GameObject("BossBar");
        bossRoot.transform.SetParent(canvas.transform, false);
        var bossRt = bossRoot.AddComponent<RectTransform>();
        bossRt.anchoredPosition = new Vector2(0f, -Screen.height * 0.5f + 60f);
        bossRoot.SetActive(false);

        var bossSlider = CreateSlider(bossRoot.transform, "BossHealthBar",
            Vector2.zero, new Vector2(500f, 25f));

        var bossNameObj = new GameObject("BossName");
        bossNameObj.transform.SetParent(bossRoot.transform, false);
        var bossText = bossNameObj.AddComponent<Text>();
        bossText.text = "Boss";
        bossText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bossText.fontSize = 18;
        bossText.color = Color.white;
        bossText.alignment = TextAnchor.MiddleCenter;
        var bossTextRt = bossNameObj.GetComponent<RectTransform>();
        bossTextRt.anchoredPosition = new Vector2(0f, 20f);
        bossTextRt.sizeDelta = new Vector2(300f, 30f);

        // Lock-on Reticle
        var reticle = new GameObject("LockOnReticle");
        reticle.transform.SetParent(canvas.transform, false);
        var img = reticle.AddComponent<Image>();
        img.color = new Color(1f, 1f, 0f, 0.8f);
        var rRt = reticle.GetComponent<RectTransform>();
        rRt.sizeDelta = new Vector2(30f, 30f);
        reticle.SetActive(false);

        // HUDManager
        var hud = canvas.AddComponent<HUDManager>();
        var serial = new SerializedObject(hud);
        serial.FindProperty("healthSlider").objectReferenceValue =
            hpBar.GetComponent<Slider>();
        serial.FindProperty("staminaSlider").objectReferenceValue =
            staminaBar.GetComponent<Slider>();
        serial.FindProperty("bossBarRoot").objectReferenceValue = bossRoot;
        serial.FindProperty("bossHealthSlider").objectReferenceValue =
            bossSlider.GetComponent<Slider>();
        serial.FindProperty("bossNameText").objectReferenceValue = bossText;
        serial.FindProperty("lockOnReticle").objectReferenceValue = img;
        serial.ApplyModifiedProperties();

        return canvas;
    }

    static GameObject CreateSlider(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        var slider = obj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(obj.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;

        // Fill Area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(obj.transform, false);
        var fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.sizeDelta = Vector2.zero;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = name.Contains("Health") ? new Color(0.8f, 0.1f, 0.1f) :
                        name.Contains("Stamina") ? new Color(0.1f, 0.7f, 0.2f) :
                        new Color(0.9f, 0.7f, 0.1f);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;

        slider.fillRect = fillRt;

        return obj;
    }

    // ── 레이어 / 태그 ───────────────────────────────────────────────────────
    static void EnsureLayers()
    {
        EnsureLayer("Enemy");
    }

    static void EnsureLayer(string layerName)
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++)
        {
            var layer = layers.GetArrayElementAtIndex(i);
            if (layer.stringValue == layerName) return;
        }

        for (int i = 8; i < layers.arraySize; i++)
        {
            var layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"레이어 생성: {layerName} (index {i})");
                return;
            }
        }
    }

    static void EnsureTags()
    {
        EnsureTag("Enemy");
    }

    static void EnsureTag(string tagName)
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = tagManager.FindProperty("tags");

        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tagName) return;
        }

        tags.arraySize++;
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();
    }

    static void BakeNavMesh()
    {
        // NavMesh는 UnityEditor.AI.NavMeshBuilder로 굽기 (동기 bake)
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        Debug.Log("[SoulslikeSceneBuilder] NavMesh 베이크 완료");
    }
}
#endif
