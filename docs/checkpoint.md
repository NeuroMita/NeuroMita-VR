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
