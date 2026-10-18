# NeuroMita VR v0.1.0-alpha.1 — для NeuroMita 0.1.1

Экспериментальный внешний VR-мод. Это alpha, а не завершённая VR-версия игры. Версия мода независима от версии основной игры; игра не обновляется и её GameAssembly.dll не заменяется.

## Что скачать

В Assets этого релиза выберите **NeuroMita-VR-v0.1.0-alpha.1-for-NeuroMita-0.1.1-win-x64.zip**. GitHub Source code ZIP — исходники для разработчиков, его недостаточно для установки. Игра и BepInEx в пакет не входят. DLL, VR assets и нативный OpenXR runtime в пакете уже есть; Unity Editor и исходники игры не нужны.

## Установка

1. Закройте игру. Сделайте отдельную копию установленной NeuroMita **0.1.1**. Проверен Windows x64 IL2CPP host с внутренней версией **v2026.10.15**, Unity **6000.3.12f1**. Другие версии не проверены.
2. Скачайте **BepInEx 6 Unity IL2CPP x64, build 788**: [официальная страница сборки](https://builds.bepinex.dev/projects/bepinex_be/788). Выберите вариант Unity IL2CPP win-x64. Не берите Mono, x86 или BepInEx 5.
3. Распакуйте BepInEx в папку с **NeuroMita.exe**. Один раз запустите игру, дождитесь создания `BepInEx/interop`, затем закройте. На первом запуске генерация библиотек может занять время; не копируйте interop от другой игры/версии.
4. Распакуйте ZIP мода **в эту же папку с NeuroMita.exe**, сохраняя структуру, а не внутрь BepInEx и не в отдельную вложенную папку. Получатся `BepInEx/plugins/NeuroMitaVRmod/NeuroMitaVRmod.dll`, `NeuroMita.VR.Runtime.dll`, `neuromita-vr-assets`, а также OpenXR-файлы внутри `NeuroMita_Data`.
5. Откройте `NeuroMita_Data/boot.config` в текстовом редакторе, предварительно сохранив копию. Добавьте отдельной строкой **`xrsdk-pre-init-library=UnityOpenXR`**, если её ещё нет. Если параметр уже существует, используйте одно значение UnityOpenXR, не создавайте дубль. Остальные строки сохраняйте. Мод не заменяет boot.config целиком.
6. Если раньше ставили диагностический `NeuroMita.XR.Probe.dll`, уберите его из `BepInEx/plugins`. Проба не должна работать одновременно с игровым модом.
7. Подключите шлем к ПК обычным PCVR-способом, запустите **SteamVR**. Шлем и контроллеры должны быть доступны; в SteamVR выберите его активным **OpenXR runtime**.
8. Запустите **NeuroMita.exe** обычным способом. Отдельный VR-билд/Unity Editor не нужны. Настройки мода доступны в паузе через **Настройки VR / VR Settings**.

## Управление Touch / Quest

- Левый стик — движение; нажатие левого стика — спринт согласно режиму в настройках.
- Правый стик вбок — поворот; вниз после возврата в центр — присесть/встать. В интерфейсе вертикальная ось прокручивает; в селфи управляет framing.
- Правый триггер — луч/клик/перетаскивание; правый grip — взаимодействие.
- A — прыжок/контекстное подтверждение; на экране смерти — пропустить вступление или печать, а не перезапустить игру.
- X — чат; удержание X 3 с — консоль и штатный каталог команд. Y — пауза/назад. B — меню действий.
- Левый триггер — инвентарь, левый grip — push-to-talk. В QTE действуют показанные подписи.
- Правый stick click — help или ручное моргание, когда эта механика активна. Оба stick click удерживать — recenter.
- Сила короткой вибрации UI/контакта/урона регулируется в VR Settings: Off / Low / Normal.

## Изменения и ограничения

Убрана аварийная Harmony-подмена BeginExternalOverride с ошибкой native→managed trampoline при nullable аргументах; добавлена защита уничтоженных креплений предметов при смене rig/сцены. Радио увеличено на 25%, volume targets расширены; scoped взаимодействия разрешают ходьбу/поворот, выход при увеличении дистанции на 1 м от открытия. Подготовлены death controls, contact/damage/bat haptics, seated crouch, QTE, TV/snowball input и VR-контекст диалога.

Меню/квартира, радио и рисование ранее подтверждались пользователем, но это не полная приёмка каждой последующей версии. **TV/YouTube остаётся проблемным и не считается исправленным в шлеме**. Недостаточно проверены QTE/захваты, смерть/повтор, сон, предметные действия, бита и новые haptics. Внешние file/login dialogs могут потребовать desktop. Карточная игра исключена. Подробная актуальная очередь: [аудит](https://github.com/NeuroMita/NeuroMita-VR/blob/codex/vr-module/docs/vr-interaction-audit.md).

## Проверка этого пакета

DLL собираются без ошибок; статический аудит IL2CPP и 22 проверки input/haptic policy проходят. В этом кандидате убран опасный перехват камеры и исправлена проверка уничтоженных Unity-объектов. Повторная проверка мольберта в VR не завершена: на последнем запуске OpenXR вернул XR_ERROR_RUNTIME_FAILURE при создании instance, до запуска VR-сессии. Устранение краша требует повторного подтверждения в шлеме; alpha не выдаётся за полностью проверенную сборку.

## Если не работает / удаление

Проверьте версию игры, вариант BepInEx, OpenXR runtime, строку boot.config и расположение файлов. Для запуска без VR: `NeuroMita.exe --no-vr`. Логи: `BepInEx/LogOutput.log` и Unity Player.log; перед публичной отправкой удалите личные пути/данные.

Для удаления закройте игру, уберите `BepInEx/plugins/NeuroMitaVRmod`, восстановите сохранённый boot.config. Нативные файлы OpenXR удаляйте по `package-receipt.json` только если они добавлены этим пакетом и не нужны другим модам. BepInEx/другие плагины не удаляйте целиком.

---

# English quick install

**Experimental v0.1.0-alpha.1 mod for NeuroMita 0.1.1.** Mod and game versions are independent. Tested host: Windows x64 IL2CPP v2026.10.15 / Unity 6000.3.12f1. This is not a finished VR edition. TV/YouTube and several gameplay scenarios still have known issues.

1. Close the game and make a separate copy of NeuroMita 0.1.1.
2. Install **BepInEx 6 Unity IL2CPP win-x64 build 788** from the [official build page](https://builds.bepinex.dev/projects/bepinex_be/788) into the directory containing NeuroMita.exe. Run once to generate BepInEx/interop, then close the game.
3. Download **NeuroMita-VR-v0.1.0-alpha.1-for-NeuroMita-0.1.1-win-x64.zip** from Assets, not GitHub's Source code ZIP. Extract into that same game directory, preserving folders. The game and BepInEx are separate downloads; the mod DLLs, assets and OpenXR files are included.
4. Back up NeuroMita_Data/boot.config and add `xrsdk-pre-init-library=UnityOpenXR` once. Preserve other settings; do not duplicate the key. Remove any old NeuroMita.XR.Probe.dll from plugins.
5. Connect the headset/controllers through your PCVR connection, start SteamVR and select SteamVR as the active OpenXR runtime. Start NeuroMita.exe. No Unity Editor or game source access is required.
6. VR Settings are available in pause. Left stick moves / click sprints; right stick turns / downward gesture toggles crouch; aim and right trigger click; grip interacts; Y pauses/goes back; hold X for 3 seconds for the command console. Hold both stick clicks to recenter. A skips intro/text on the death screen; use the ray and trigger for Restart/Exit.

Use `NeuroMita.exe --no-vr` for desktop mode. To uninstall, close the game, remove the mod plugin folder and restore boot.config. Only remove native files listed in package-receipt.json when no other mod depends on them. Keep other plugins intact. Logs may contain personal data; inspect before sharing.

Build validation: DLL builds, static IL2CPP audit and 22 input/haptic policy checks pass. A fresh VR easel smoke test remains unconfirmed: the last launch failed at OpenXR instance creation (XR_ERROR_RUNTIME_FAILURE), before a VR session was available. Earlier headset results do not constitute acceptance of this exact package.
