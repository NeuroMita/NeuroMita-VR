# Сторонние компоненты

Зависимости скачиваются или предоставляются разработчиком отдельно; их бинарники и исходники игры не хранятся в этом репозитории.

- [BepInEx](https://github.com/BepInEx/BepInEx), baseline 6.0.0-be.788 — загрузчик и SDK. См. лицензию и уведомления соответствующей версии в upstream.
- [Il2CppInterop](https://github.com/BepInEx/Il2CppInterop), baseline 1.5.3 — interop runtime, используемый BepInEx.
- [Unity OpenXR package](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.16/manual/index.html), проверена версия 1.16.1 — источник нативных файлов диагностического ZIP. Package-XrProbe.ps1 переносит LICENSE.md и Third Party Notices.md из разрешённого пакета в Notices/ архива. Включение этих файлов не заменяет проверку условий распространения.
- [OpenXR SDK loader](https://github.com/KhronosGroup/OpenXR-SDK-Source) — нативный loader в диагностике берётся из Unity OpenXR package; уведомления сохраняются из этого пакета.
- [NeuroMita Unity](https://github.com/VinerX/NeuroMita-Unity) — host и исходный проект для перенесённых Gameplay-компонентов. В репозитории сохранена только VR-часть; полный проект игры не включён. Происхождение и исходная ревизия указаны в docs/source-provenance.md.

Собственные исходники мода лицензированы под MIT, см. LICENSE. Этот файл перечисляет сторонние зависимости и не меняет их лицензии. Перенос VR-исходников не распространяет MIT собственных новых адаптеров на другие материалы исходного проекта.
