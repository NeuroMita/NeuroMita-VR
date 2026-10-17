# Проверенный статус — 2026-10-17

Проверки ниже относятся к внешнему BepInEx-моду на изолированном Windows x64 IL2CPP host 46d89dce1 (Unity 6000.3.12f1, BepInEx 6.0.0-be.788, Il2CppInterop 1.5.3). Совместимость с другими host/Unity версиями отдельно не доказана.

- Gameplay C# находится в самом репозитории мода; DLL не компилируется из Unity-checkout. Unity-ветка используется как источник сравнения.
- Bootstrap, runtime, probe и audit tool собираются без ошибок и предупреждений. Runtime audit до новых UI-правок: 1744 достижимых ссылок, нет неразрешённых/stripped; 19 native imports доступны.
- Усиленный audit запрещает достижимые Il2CppSystem.Span/ReadOnlySpan wrappers. Он отклоняет старый игровой ZIP, включая конкретный путь BoxCastNonAlloc -> Span<RaycastHit>, на котором произошёл native crash. Исправленная DLL проходит.
- Реальная игра создаёт OpenXR session и запускает display/input; профиль контроллеров пока Oculus Touch.
- Пользователь подтвердил загрузку квартиры, движение и подбор предмета триггером. Полной ручной приёмки всех механик ещё нет.
- Visual Studio подключена с native и .NET Core engines, PDB мода установлены. First-chance access violation пойман со смешанным стеком: GCHandle::GetTarget -> Il2CppObjectBase.Pointer -> Span<RaycastHit> -> VrHandProxy.Sweep. Исправление: закреплённый буфер и прямой injected physics query. См. docs/debugging.md.
- KindHouse и CrazyHouse загружаются после исправления. Для физической проверки кистей нужны активные контроллеры: загрузка при неотслеживаемых руках сама по себе не проверяет sweep.
- Причина пропуска разовых кнопок: в моде потерялся ранний DefaultExecutionOrder Unity-версии, а API Down/Up действует только в кадре публикации. Добавлен собственный проверяемый узел перед ScriptRunBehaviourUpdate; публикуется один раз за кадр. При удалении снимается только собственный callback, чужие системы сохраняются.
- Диагностическое однокадровое Pause через тот же API в CrazyHouse теперь переводит Gameplay/timeScale=1 в UI/timeScale=0. До исправления тот же тест не открывал паузу. Подтверждение физической Y после этой правки остаётся отдельным шагом.
- Анимация меню больше не отключается вместе с её светом. Свет реально проходит от нуля до intensity=1/range=2; MenuMita обнаруживается повторно после отложенного появления. Визуальное качество освещения в шлеме требует оценки.

VR-управление: левая Y — пауза/назад; правый клик стика — подсказки; оба клика стиков с удержанием — recenter; правый триггер — выбор/клик; правый grip — взаимодействие. У старого API-only host нет локализации ui.gameplay_help.vr: мод предоставляет собственную подсказку вместо F1 fallback.

Остаются открытыми: полная проверка смартфона/selfie, радио/UI Toolkit pointer/scroll/drag, чата и клавиатуры, всех переходов и механик, потери/возвращения tracking, визуальная приёмка. Страница VR-настроек теперь добавлена самим модом в pause menu; её видимость и нажатия в шлеме ещё не проверены. Старый host не содержит новых context-action/TV HUD hooks, используются ограниченные совместимые пути.

Сырые логи, дампы, PDB, игровые файлы, interop и SDK не публикуются и игнорируются Git. HOST_API_PROBE и XR probe не являются частью игрового ZIP. Старые ZIP в dist не содержат этих исправлений; установленная тестовая DLL и исходники новее них. Remote подключён к https://github.com/NeuroMita/NeuroMita-VR; публичность меняет владелец.

Программный UI smoke в CrazyHouse после исправления: PauseMenuElement открылся/закрылся; helpVisible True -> False -> True; Radio открылся, Pause/Back закрыл его и вернул Gameplay. Прогон без физических нажатий не заменяет приёмку кнопок в шлеме. Воспроизведение: tools/Test-VrUi.ps1 -RunDirectory <текущая диагностическая сессия>, начинать из главного меню с закрытой паузой; скрипт изменяет игровую сцену/UI.

## UI checkpoint после разрядки шлема, 2026-10-17

- После возврата из квартиры SceneMenu загрузилась и VR-цикл продолжал работать примерно две минуты. Затем OpenXR завершил сессию; пользователь подтвердил разрядку шлема. Причина визуально застывшего меню пока не воспроизведена, закрытой её не считаем.
- При отложенном появлении MenuMita перестройка VR-камеры теперь повторно размещает меню после применения tracked pose. Это исправление найденного рассогласования, а не доказанное объяснение всего отчёта о зависании.
- В главном меню мод больше не публикует действия квартиры (чат, инвентарь, прыжок и т.д.). Back допускается только для существующего UI back context. В старом логе вызов чата обращался к уже неактивному Interface.
- Убрана регистрация native event interfaces для injected VrToolkitInputRelay. Собственный VR-диспетчер вызывает relay напрямую, сохраняя стандартный ExecuteEvents для обычных UGUI обработчиков. Исправление направлено на NullReferenceException в ExecuteEvents IPointerDown/Move/Enter/Exit, зафиксированные при UI Toolkit взаимодействии.
- В паузе добавлена кнопка VR Settings и страницы настроек мода, включая режим запястья, указатель, движение, руки и размещение UI. Изменения записываются в BepInEx config. Левая Y сначала закрывает страницу настроек.
- Для TVGamesModule добавлены отдельная VR-панель Previous / Play-Continue / Next / Exit TV и выход левой Y, который не открывает одновременно паузу. Desktop menu hints скрываются, восстановление при выгрузке мода предусмотрено. Это ещё не проверка управления всеми мини-играми.
- Новые изменения прошли сборку и static interop audit без ошибок/предупреждений, unresolved/stripped ссылок и missing native exports. Свежий runtime audit: 1818 ссылок, 19 native imports. Шлем разряжен: свежего VR-прогона этих правок не было.
- На следующем прогоне нужны реальный клик по радио и его UI, VR Settings с сохранением после перезапуска, выход из TV и круг главное меню -> квартира -> главное меню с проверкой изображения и нажатий.
Подготовка репозитория: LICENSE заменён точной лицензией канонического NeuroMita по решению автора. В assets/source добавлен минимальный Unity-проект ресурсов, в assets/prebuilt — ранее проверенный bundle (163502 байта) и хеши происхождения. Упаковка с bundle по умолчанию успешно проверена; новый asset-проект пока не импортировался/не собирался в Unity. Старые ZIP остаются историческими артефактами; новый ZIP включает актуальные DLL и уведомления.
## Полный инвентарь: статическая модовая адаптация

VrInventoryDrag обрабатывает только события слотов из VR-диспетчера. Собственный drag icon следует проекции луча на canvas; исходный слот визуально скрывает переносимый предмет. Drop в другой слот того же inventory вызывает штатный SwapItems с уведомлением подписчиков и выбором слота назначения. Drop вне слотов/на исходный слот, закрытие UI, смена inventory, изменение исходного предмета/количества, потеря tracking и сброс pointer отменяют перенос без изменения inventory. После отмены снимается визуальное подавление. Клики и действия context menu сохраняют штатные обработчики; нажатие лучом снаружи закрывает context menu.

Mouse-only coroutine публичного host не запускается для VR drag. Оригинальные desktop Update/OnBeginDrag/SwapCoroutine не патчатся глобально; native host и modding API не менялись. Drag icon исчезает при завершении/отмене, анимация возврата/обмена desktop не воспроизводится. Зависимость от interop полей _owner/_index/_targetInventory/_isAnimating зафиксирована: это совместимость с текущим публичным host, а не новый стабильный host API.

Проверки перед приёмкой в шлеме: перенос предмета в пустой слот; обмен двух заполненных; отмена вне панели; отпускание над исходным слотом; закрытие инвентаря и потеря tracking при удержании; открытие/использование/закрытие контекстного меню правым grip; отсутствие изменений inventory после отмены; desktop без активного VR сохраняет прежнее поведение. Эти сценарии пока не запускались. Сборка и static interop audit не заменяют runtime-проверку.

## Застывшая заставка в шлеме: расследование 2026-10-17

Пользователь сообщил о плоском застывшем логотипе после звука загрузки главного меню. Unity capture уже показывал тёмную сцену с частью Миты; меню и камера были загружены. Это расхождение не считается закрытым одной проверкой display/input.

Предыдущий runtime стартовал по таймеру 5 секунд на SceneAihasto, затем перевязывался на SceneMenu. Bootstrap теперь ждёт MenuSceneResolver root или игрока и окончания SceneService transition, прежде чем запускать XR. Intro больше не захватывается runtime как полноценная VR-сцена. Целенаправленный native test после правки: первая привязка SceneMenu, display=True, renderPasses=2, camera stereoEnabled=True. Проверка сборки/static audit без ошибок: 1855 ссылок и 19 imports. Решение проблемы в шлеме пока не подтверждено пользователем; качество/видимость меню также остаются отдельной проверкой.

Добавлена диагностическая команда views: камеры, canvas, активные graphics и состояние stereo render, без изменения сцены/расположения UI. Во время проверки перезапускалась только установленная NeuroMita по разрешённому пользователем VR-тесту.

## Animated menu camera projection: 2026-10-17

The previous validation host also reproduced a frozen headset image after its stereo intro. SceneMenu's camera animation clips (CameraMenu Start and CameraMenuAlternative Idle) animate near clip plane to -1, matching the original orthographic menu. Setting perspective and near=0.02 only during rig binding allowed that animation to overwrite the VR projection. The captured stereo projection had m22=-0.96078 and m23=1.96078, consistent with near=-1/far=50.

VrPlayerRig now retains near=0.02, perspective and both-eye output during pose application (Update, LateUpdate and the existing pre-cull hook), including when tracking is unavailable. Camera/light animations stay enabled. Restore still returns the saved camera parameters when VR relinquishes the camera. No host or modding API changes.

Built against validation host 46d89dce: zero build warnings/errors; static interop audit 1874 reachable references, 19 native imports, no unresolved references/missing exports. Fresh comparison run 20261017-224509 reached CrazyHouse; capture game-194622.png contains the apartment, pause menu and VR settings rather than black output. Input logs record movement and pause. Headset acceptance and repeating the test on the clean public release remain pending; these observations alone do not close all frozen-frame causes.

## Public host acceptance and UI batch: 2026-10-17

Public v2026.10.15 host (Unity 6000.3.12f1) run 20261017-230856: user confirmed both the main menu and apartment update correctly in the headset with the external mod. Mod was rebuilt against this host's own generated interop. No GameAssembly/host API changes were made. This closes the reported startup frozen/black frame for this tested route, not full gameplay acceptance.

Follow-up public runs revealed independent UI issues. The player camera mask excluded layer UI (5). VrPlayerRig now includes that layer while active and restores its original bit when releasing the camera. VrUiSurface previously passed a managed Vector3[] to GetWorldCorners: interop copied the array, leaving the original buffer unchanged. It now uses an explicit Il2CppStructArray output buffer. The Mita panel no longer drifts thousands of metres away. Its anchor now uses the existing MitaAttentionTargetResolver instead of Links.Head, which was resolving near the origin in the tested scene.

Run 20261017-232047: Mita panel position remained around (0.05, 1.26, -0.20), alpha=1; layer 5 is visible, camera near=0.02. This is geometry/lifecycle evidence; natural opening, pointing and selection near the actor still need headset acceptance. Radio RenderTexture readback radio-202131.png contains the complete populated playlist and playback controls. A broken Toolkit tree was suspected but not confirmed, so no host-tree rebuilding workaround was added. The distant diagnostic-opened panel position motivated a placement fallback: when outside 0.65–1.8m or 55 degrees from the head forward direction, open at a fixed comfortable point in front. That placement fallback is only statically checked so far.

Further work is batched: related fixes get static validation and separate scoped commits, then combined runtime/headset acceptance. Avoid relaunching the game for each small edit. QTE, TV, bed/exit, bat and other audit items remain open. Cards are excluded by user request. Discord VR presence is planned, not implemented.

## QTE controller adapter batch (not installed / not runtime accepted)

VrQteHooks patches only QTEStateController.UpdateChoice/UpdateSingle/UpdateMash/UpdateComboInput when the current external VR runtime owns an active QTE. Existing host Update continues to own timer/timeout and dispatch; callbacks, hold decay/conflict, mash decay and combo sequencing remain equivalent to the source handlers. No global Input.GetKey patch, OS key injection or host binary/API changes. Hook installation is all-or-nothing; failure removes this adapter's hooks, logs a warning and leaves the rest of VR available. Native hook interception itself is not established by a successful static build.

Per active QTE, distinct authored keys map in order to right trigger, left trigger, right grip, left grip, right A, left X, right B. Choice A/B use right/left trigger; single/mash use right trigger. A combo with more than seven distinct keys shows Unmapped VR input for excess keys; it is not automatically completed. Buttons must be released once when the QTE begins, after tracking recovers and after completion before they can be reused by ordinary gameplay. Both controllers and the head must be tracked for QTE input. Tracking loss does not suspend the host timer; this preserves timeout policy and remains a usability limitation to assess.

While the QTE owns controls, published gameplay buttons, locomotion, turn, VR UI clicking, PTT eligibility, help toggles and phone framing are blocked. World pointer cancellation uses existing cancellation semantics. Only the active QTE controller gets the redirected input; other/native desktop handlers retain their original paths. QTE hint labels and generated QTE button text use controller names. Original QTE layout remains; readability, label width and placement still need combined headset testing.

Validation: built against public v2026.10.15 interop, zero build warnings/errors, static audit 2041 reachable references / 19 native imports with no missing exports or unresolved references. No launch or installation for this batch, per the requested grouped-test workflow. Next combined acceptance: Mita menu → music player visibility/Back → single press / mash / held choice / combo → grab escape and failure → tracking loss/recovery → return to ordinary controls. Check interruption/queue transitions and repeated QTE as distinct runs, and confirm a result never also jumps, clicks the world or opens chat. QTE input hooks are version-sensitive host interop integration, not a stable new modding API.

## Live public run 20261017-234816: context panel lifetime failure

User-tested public host ran latest installed fcd2268 through menu and house. At 20:52:44 UTC the runtime stopped after an IL2CPP NullReferenceException in Component.get_gameObject, VrContextActionsPresenter.ResolveInteractionRay line 144. The managed ActionPanel outlived its scene-owned Canvas; checking the managed panel reference did not establish that the native Canvas still existed. The broad frame handler then intentionally cleaned up XR. This is a mod UI lifetime failure, not a SteamVR startup failure.

ResolveInteractionRay now validates the native canvas before accessing gameObject and uses that validated reference for picking. LateUpdate recreates each ActionPanel when its scene canvas has been destroyed, rather than using managed ??= on a surviving wrapper. No new live restart was performed for this correction; native build/static audit passed. Required combined regression: repeated world-context actions, scene/menu transitions and reopening item/world panels without ending XR.


## October 18 static-only interaction batch

Added VrBatSwing, VrBlinkInput, VrSnowballInput and VrDialogueContext in the external mod only. Updated RU/EN README and the interaction audit with bindings, source ownership, reset/cleanup behavior and unaccepted cases. No host/API/release source edits, no Unity operations, no player launch, scene command or hot replacement in the running player. Current player retains its previously installed runtime until a later explicitly scheduled restart/install.

Build and public-host IL2CPP audit pass; native installation and headset behavior are still pending. Bat geometry is estimated from the held visual and needs actual swing tests; blink/help arbitration and snowball physics need live coverage. The dialogue hook preserves original event ids and request retry semantics without adding persistent pending events. Upcoming combined acceptance should cover the preceding console/TV/movement UI batch together with these interactions, rather than restarting for each small feature.


## October 18 seated crouch and audit consolidation

Right-stick downward gesture toggles crouch; left-stick sprint is preserved. Physical crouch hysteresis is independent of the toggle. UI hover/focus, selfie, QTE, sequence, movement lock and non-Gameplay states disarm the gesture until neutral. Scene/player/tracking/death resets clear the toggle. The host still owns Sit, collider and camera height; no teleport or direct capsule override was added. Twelve standalone policy checks are part of Test-Static. Actual seated camera/ceiling behavior remains unaccepted.

Replaced the cumulative interaction audit with one current coverage map, explicit user-confirmed boundaries, remaining implementation gaps, pending acceptance and five selectable next-work directions. Historical diagnoses remain in Git. No live process/Unity/XR was launched or modified for this revision.


## October 18 death input, feedback and public README

Launched clean public host c8fbe61 for the user: PID 8320 / artifacts/public-release/20261018-011608. OpenXR active; live logs confirm installation of dialogue/blink/snowball hooks. The player is not restarted or hot-replaced while the user tests. README RU/EN no longer points external readers at the private Unity VR branch: it explains matching public host + BepInEx interop requirements instead.

Next batch: VrDeathInput adds A skip and release gating, controller hint, target/action isolation and death camera selection. DeathScreenFxRig.BindCanvases is suppressed only during active VR so the host cannot force the adapted canvas back into screen space. Native death flow, lives, reveal, retry and exit callbacks remain intact. VrHaptics centralizes short UI/contact/damage/bat pulses, respects Off/Low/Normal, limits rate and prioritizes damage. Contacts prime silently after tracking changes and do not repeat while held. Damage compares native Health before/after rather than buzzing on a rejected call. All new functionality still needs native installation and headset acceptance on the next run. Static checks include 12 crouch and 10 haptic policy assertions.

The user authorized publishing a GitHub mod release if a current Codex usage window has less than 10 percent remaining; they will change repository visibility themselves. Do not consume usage-reset credits. Latest check during this batch: 20 percent primary / 88 percent weekly remaining, so the conditional release has not been triggered. Verify current limits before publishing; do not infer that this snapshot remains current.


## c8fbe61 user feedback: easel ABI failure, TV visibility, radio and turning

The user reported a crash opening the easel, invisible/unusable TV, radio too small and unavailable stick turning in interactions. Public-run BepInEx log contains repeated native->managed BeginExternalOverride trampoline System.Buffer.Memmove exceptions. Removed that Harmony target entirely. Easel focus is cleared once in its scoped world session using ClearExternalOverride(bool). The next process must validate that the crash/trampoline error is gone; a static build cannot establish this.

Radio is 25 percent wider, mute/volume targets are larger and restored on release. Scoped own locks release Look as well as Move/Input, and rig turn accepts the same interaction contexts. Camera override is cleared before measuring rotation compensation, preventing a zoomed camera from dragging the player body to the TV. Auto-exit is one additional horizontal metre away from the object relative to its opening distance, avoiding immediate closure when opening outside an absolute one-metre radius. TV uses the controller's explicit InteractionController reference and mounts the depth-tested panel two centimetres toward the viewer. All changes still await native/headset acceptance.
