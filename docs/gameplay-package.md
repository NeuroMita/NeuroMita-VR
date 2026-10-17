# Установка экспериментального игрового VR-мода

Это игровой runtime, а не XR probe. Проверен запуск на Windows x64 IL2CPP host, собранном из 46d89dce1 (Unity 6000.3.12f1). Совместимость с другими релизами не подтверждена. Начните с отдельной копии игры.

1. Установите BepInEx 6 для Unity IL2CPP x64. Проверенный baseline: 6.0.0-be.788, Il2CppInterop 1.5.3. Запустите обычную игру один раз для генерации interop, затем закройте её.
2. Распакуйте игровой vr-mod ZIP в папку с NeuroMita.exe с сохранением структуры. Не переносите interop из чужого билда.
3. Если ранее ставили NeuroMita.XR.Probe.dll, переместите его из BepInEx/plugins в другую папку. Он может находиться непосредственно в plugins или во вложенном каталоге.
4. Подключите шлем к ПК, запустите SteamVR и убедитесь, что SteamVR видит шлем и контроллеры. SteamVR должен быть активным OpenXR runtime.
5. Запустите NeuroMita.exe из этой копии игры. Отдельная команда Unity Editor не требуется. Альтернатива: .\Start-VrMod.ps1 -GameDirectory 'путь к игре'.

Bootstrap по умолчанию включает VR и через несколько секунд создаёт внешний runtime. Его настройки сохраняются в BepInEx/config. При ошибке смотрите BepInEx/LogOutput.log и Unity Player.log. Строки Native session creation: True, Touch controller actions attached, Native XR display=True, input=True и OpenXR active подтверждают этапы запуска, но не качество изображения и физического управления.

Базовая раскладка Touch: левый стик — движение, правый — поворот; правый trigger — указатель, grip — взаимодействие; A — прыжок/подтверждение чата, B — radial menu; X — чат, Y — пауза; левый trigger — инвентарь, grip — микрофон. Одновременное удержание обоих стиков — recenter. Раскладка перенесена из Unity-runtime; физические нажатия во внешнем моде ещё предстоит принять.

Пакет содержит bootstrap DLL, runtime DLL, AssetBundle, нативные UnityOpenXR/openxr_loader и subsystem manifest. Игра, BepInEx и generated interop в ZIP не входят. Уведомления Unity OpenXR сохранены в Notices. Перед публичной раздачей ресурсов из Unity-проекта отдельно проверьте права на них: MIT собственных адаптеров их не перелицензирует.

Ограничения: нет полной проверки house/паузы/смартфона/радио в шлеме через мод; native controller profile пока только Oculus Touch. Старый API-only host не предоставляет новые TV HUD suppression hooks. Если host вырезал tracking-origin notifications, после смены origin используйте ручной recenter.