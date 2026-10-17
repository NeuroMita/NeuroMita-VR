# Ресурсы внешнего VR-мода

`prebuilt/neuromita-vr-assets` — готовый AssetBundle для Windows x64 / Unity 6000.3.12f1. Упаковщик мода использует его по умолчанию. Git LFS не нужен: исходный проверенный bundle занимает около 160 КиБ.

`source/` — отдельный минимальный Unity-проект. В нём находятся два меша кистей, материалы указателя и затемнения, comfort shader, `.meta` с исходными GUID и Editor-скрипт сборки. Полный игровой проект не требуется. Профиль контроллеров и OpenXR backend находятся в C# runtime мода, а не в этом проекте.

## Изменение ресурсов

1. Откройте `assets/source` в Unity 6000.3.12f1 с модулем Windows Build Support. Дождитесь импорта.
2. Измените меши, материалы или shader в `Assets/NeuroMitaVR`. Сохраняйте `.meta`, имена адресуемых ресурсов и связи материалов с shader.
3. Выполните `NeuroMita VR → Build Windows AssetBundle`. Скрипт собирает Windows64 bundle в соседний `assets/prebuilt`.
4. Проверьте новый bundle в совместимом host с модом. Закоммитьте изменённые исходники и bundle вместе; обновите SHA-256 и сведения в `prebuilt/provenance.json`.

Имена загрузки: `NeuroMitaVrComfort`, `NeuroMitaVrLeftHand`, `NeuroMitaVrPointer`, `NeuroMitaVrRightHand`. Сырые `.asset` и `.mat` не подменяют bundle при запуске игры.

`Library`, `Temp`, `Logs`, `UserSettings`, импортированные пакеты и вспомогательные bundle manifests исключены из Git. Не добавляйте сюда игровой FBX, текстуры персонажей, BepInEx или Unity/OpenXR DLL.

## Происхождение и лицензии

Готовый bundle перенесён из установленного тестового мода; это не новая пересборка минимальным asset-проектом. Новый проект и скрипт подготовлены из тех же исходных ресурсов, но его импорт и сборка в Unity пока не проверены. Точные хеши копий записаны в `prebuilt/provenance.json`.

Кисти ранее извлечены из `Assets/NeuroMita/FBX/Player/PlayerModelNewTest.fbx` скриптом `VrHandMeshBuilder` Unity-полигона. Исходный FBX в репозиторий мода не включён. Лицензия кода NeuroMita не предоставляет прав на сторонние модели или иные материалы. Наличие мешей здесь не означает их перелицензирование; их происхождение и условия нужно учитывать при публичном распространении. Подробности: [source-provenance](../docs/source-provenance.md), [лицензия проекта](../LICENSE).
