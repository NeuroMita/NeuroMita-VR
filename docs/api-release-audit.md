# Modding API: releases против VR и внешнего мода

Проверка 2026-10-17, по локальным refs без обновления remote:

- releases / origin/releases: 0b4c018ad2c35f5a07c4ebd1106f84cb7d46429b.
- codex/neuromita-vr: b89701b7de272970427565cb78c4a27606911e13.
- Мод до этого аудита: ce71ecf.

## Основные контракты совпадают

Git tree Assets/Scripts/NeuroMitaScripts/Modding в обеих ветках: a7626414f14ed009c9048e833b1e0bbc41daeb5e. Различий файлов нет. InputManager, link.xml, hand/contact/composer/selfie/menu-attention contracts также не имеют различий между этими tips. В releases уже есть SetTrackedHands, SetPointerButtons, ClearPointerButtons и InputManager.PointerUp.

Последний pointer API fix присутствует в releases как 05497a724; в VR-истории как dc9712e23. Разные SHA после cherry-pick не означают отсутствие изменения в релизе. git log releases..VR без patch comparison для этого вывода недостаточен.

При установке мода на публичный v2026.10.15 GameAssembly.dll и host API не заменялись. Interop сгенерирован заново; мод пересобран против него и прошёл audit (1818 ссылок, 19 imports, нет unresolved/stripped/missing exports). Для старта OpenXR потребовалась настройка boot.config; это конфигурация native XR, не изменение API.

## Дополнительные публичные поверхности только в VR-ветке

| Изменение | Коммит | Поведение внешнего IL2CPP-мода на releases |
| --- | --- | --- |
| PlayerInteractionController.TryInvokeContextAction(ActionType, GameObject, bool) | 0e75fd135 | Optional reflection lookup; при отсутствии совместимый путь через существующий controller с проверками состояния/цели и ActionPressed/Released |
| ItemStatusController.ViewRoot | 0e75fd135 | VrHostViews.ItemStatusRoot использует доступное interop поле root |
| MusicListUI.SourceRadio | 8134e0554 | VrHostViews.RadioAnchor использует доступное interop поле radio |

Эти три поверхности не входят в совпадающий каталог Modding и не включены в releases. Первые две выделены отдельным API-коммитом; SourceRadio находится в смешанном VR-коммите, целиком переносить его в релиз нельзя. Текущий внешний runtime не требует добавлять эти getters для загрузки, но зависимость от interop полей является ограничением API-инкапсуляции.

ShouldHideViewingHud уже есть у TVDeviceController в текущем releases и совпадает с VR. Старые замечания про отсутствие TV HUD hook относились к более раннему validation host, не к публичному v2026.10.15.

## Различия интеграции, не новые контракты bridge

- InventoryFullUI в releases ещё читает Input.GetMouseButton / Input.mousePosition при drag и context menu. В VR-ветке применены PointerHeld / PointerDown / PointerPosition / PointerWorldPosition. Публичные input API уже существуют, но этот потребитель не подключён к ним. Успешный запуск мода не доказывает перенос/перестановку слотов инвентаря. Остаётся статический gap для модовой адаптации; без отдельного решения не считать parity достигнутым.
- MenuButtonBase в releases активируется на PointerUp, а в VR-ветке — на completed PointerClick (59565f088). Это может активировать release menu при отмене нажатия или отпускании вне кнопки. Мод должен учитывать legacy handler, не требуя изменения host.
- VR settings registry и остальные VR-specific настройки в Unity-ветке не являются универсальным API расширения настроек. Внешний мод предоставляет собственную страницу настроек и BepInEx config.

Аудит сам по себе не меняет releases и не переносит туда API-коммиты. Новый VR-прогон не выполнялся; открытые статические и визуальные дефекты остаются отдельными пунктами проверки.
