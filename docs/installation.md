# Отдельный диагностический XR probe

## Доступно сейчас

Игровой внешний VR-мод уже существует: [установка игрового пакета](gameplay-package.md), [статус и управление](../README.md). Этот документ относится только к **отдельному BepInEx XR deployment probe**. Он проверяет загрузку нативного OpenXR рядом с игрой; меню, руки и игровые VR-механики в probe не входят.

## Диагностическая установка

1. Возьмите отдельную копию Windows x64 IL2CPP-билда NeuroMita. Проверенный ранее host: исходники 46d89dce1, Unity 6000.3.12f1. Совместимость с произвольным новым релизом не подтверждена.
2. Установите BepInEx **6 IL2CPP Windows x64**, а не вариант Mono. Проверенная версия: 6.0.0-be.788+5b766a3. Инструкция: [официальная установка](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html), [официальные сборки](https://builds.bepinex.dev/projects/bepinex_be).
3. Один раз запустите EXE игры и дождитесь генерации BepInEx/interop и BepInEx/LogOutput.log, затем закройте игру.
4. Соберите диагностический ZIP по [инструкции разработчика](development.md) либо используйте локально подготовленный ZIP с именем xr-probe-<дата>.zip. В публичном репозитории бинарники не хранятся и готовый релиз пока не обещан.
5. Проверьте package-receipt.json: kind должен быть diagnostic-only. Имя папки *_Data в архиве должно совпадать с вашей игрой.
6. Распакуйте ZIP в папку с EXE игры. Не накладывайте его поверх другой уже установленной версии UnityOpenXR: для проверки используйте отдельную копию игры.

Ожидаемая структура для имени NeuroMita:

    NeuroMita.exe
    BepInEx/plugins/NeuroMita.XR.Probe/NeuroMita.XR.Probe.dll
    NeuroMita_Data/Plugins/x86_64/UnityOpenXR.dll
    NeuroMita_Data/Plugins/x86_64/openxr_loader.dll
    NeuroMita_Data/UnitySubsystems/UnityOpenXR/UnitySubsystemsManifest.json
    Notices/
    package-receipt.json

BepInEx устанавливается отдельно. Для игрового режима используйте отдельный gameplay пакет с NeuroMitaVRmod.dll и NeuroMita.VR.Runtime.dll, не диагностический ZIP. Не копируйте DLL из Unity Editor в BepInEx/interop.

## Проверка 1: обнаружение OpenXR

Просто запустите EXE игры. Через несколько секунд откройте BepInEx/LogOutput.log:

- XR_DESCRIPTOR_PROBE_PASS — найдены OpenXR Display и OpenXR Input.
- XR_DESCRIPTOR_PROBE_MISSING — отсутствует один из подсистемных дескрипторов.
- XR_DESCRIPTOR_PROBE_FAIL — исключение при проверке.

Шлем и SteamVR для этой проверки не обязательны. Изображение в шлеме здесь не ожидается.

## Проверка 2: попытка создания XR-сессии

1. Подключите шлем к ПК своим обычным PCVR-способом и проверьте, что он действительно доступен в SteamVR. Для беспроводного шлема недостаточно просто открыть SteamVR на ПК.
2. В настройках SteamVR в разделе OpenXR проверьте Current OpenXR Runtime. Для проверки через SteamVR выберите SteamVR; при необходимости используйте Set SteamVR as OpenXR Runtime.
3. Из PowerShell в репозитории мода запустите:

    .\tools\Start-XrProbe.ps1 -GameDirectory 'D:\Games\NeuroMita-Test' -SessionProbe

Можно также запустить скрипт Start-XrProbe.ps1 из распакованного диагностического ZIP: .\Start-XrProbe.ps1 -GameDirectory . -SessionProbe. Путь выше является примером: укажите свою тестовую копию игры. Если EXE называется иначе, добавьте -GameExecutable 'ДругоеИмя.exe'.

Альтернатива без скрипта: задайте NEUROMITA_XR_SESSION_PROBE=1 только для процесса запуска игры. Обычный запуск EXE без этой переменной выполняет лишь проверку дескрипторов.

В BepInEx/LogOutput.log ищите:

- XR_NATIVE_INITIALIZE_RESULT=True — инициализация нативного API прошла.
- XR_NATIVE_CREATE_SESSION_RESULT=True — попытка создания сессии успешна.
- XR_NATIVE_CREATE_SESSION_RESULT=False — сессия не создана; нужны подробности в логе игры и OpenXR runtime.
- XR native session probe cleaned up — проба освободила свою сессию.

**Проба сразу очищает сессию. Она не запускает стереорендеринг, трекинг игрового игрока или управление. Чёрный/обычный экран в шлеме в этом тесте сам по себе не определяет результат; смотрите маркеры в логе.**

Переключателя, превращающего probe в игровой runtime, нет. Внешний игровой мод имеет собственный bootstrap и VR Settings; устанавливайте его отдельно и убирайте probe из plugins.

## Если сессия не создаётся

Сохраните BepInEx/LogOutput.log, лог Unity Player и соответствующий клиентский лог SteamVR. В установке Steam логи обычно лежат в каталоге logs; XR-клиентский файл имеет имя xrclient_<имя процесса>.txt. Не прикладывайте сырые логи публично без просмотра: в них бывают локальные пути и идентификаторы.

Ранее общий XR_ERROR_RUNTIME_FAILURE удалось уточнить по SteamVR до VRInitError_Driver_WirelessHmdNotConnectedAndNoSteam: SteamVR работал, но беспроводной шлем не был подключён. Такая причина не означает, что все будущие XR_ERROR_RUNTIME_FAILURE будут вызваны тем же.

## Удаление диагностики

Закройте игру. Удалите добавленный каталог BepInEx/plugins/NeuroMita.XR.Probe. В отдельной тестовой копии можно также убрать три нативных файла из списка package-receipt.json, если они были добавлены именно этой диагностикой. Общую установку BepInEx и файлы других модов не удаляйте.

## Игровой режим

Подключить шлем и SteamVR → запустить установленный gameplay мод. Актуальные требования и шаги: [игровой пакет](gameplay-package.md). Главное меню и квартира подтверждены на публичном v2026.10.15; это не полная проверка всех механик.
