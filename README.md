# NeuroMita VR mod — experimental work in progress

Внешний VR-мод для NeuroMita под Windows x64 / Unity IL2CPP / BepInEx 6.

**Это прототип, а не готовый к обычной игре релиз.** Полный игровой runtime уже собирается отдельно от игры и запускается через BepInEx. Перенос и проверка игровых сценариев продолжаются; качество изображения и контроллеров в шлеме ещё не принято.

| Компонент | Проверено |
| --- | --- |
| Bootstrap и игровой runtime | Собираются на реальных IL2CPP interop DLL без ошибок и предупреждений |
| Нативный OpenXR backend | Создаёт сессию, запускает display/input на отдельном тестовом host |
| Touch controller actions | Движение и подбор подтверждены пользователем; остальные кнопки проверяются отдельно |
| Меши кистей, материалы указателя и comfort overlay | Исходники в assets/source, готовый bundle в assets/prebuilt; пересборка отдельным Unity-проектом |
| Изображение, меню и игра в шлеме через мод | Требуют отдельной проверки; запуск не является подтверждением |

Игровой пакет и XR probe различаются: probe остаётся диагностическим и не должен одновременно управлять OpenXR с игровым плагином.

- [Установка игрового пакета](docs/gameplay-package.md).
- [Интерактивная отладка и crash dumps](docs/debugging.md).
- [Диагностика отдельно](docs/installation.md).
- [Сборка из исходников](docs/development.md).
- [Проверенный статус и ограничения](docs/checkpoint.md).
- [Подготовка к публикации](docs/publication.md).
- [Сторонние зависимости](THIRD_PARTY_NOTICES.md).

Игровые VR-компоненты находятся в src/NeuroMita.VR.Runtime/Gameplay: это самостоятельная версия для мода, первоначально перенесённая из [VR-ветки Unity](https://github.com/VinerX/NeuroMita-Unity/tree/codex/neuromita-vr). Сборка DLL больше не читает C# из Unity-checkout. Unity-ветка остаётся полигоном и источником сравнения; происхождение перенесённых файлов описано в docs/source-provenance.md.

- src/NeuroMitaVRmod — BepInEx bootstrap и настройки.
- src/NeuroMita.VR.Runtime — IL2CPP injection, native OpenXR backend, адаптеры и собственная копия игровой VR-логики.
- src/NeuroMita.XR.Probe — независимая диагностика.
- tools/Package-VrMod.ps1 — игровой ZIP; tools/Start-VrMod.ps1 — запуск установленного мода.
- tools/Package-XrProbe.ps1 — отдельный диагностический ZIP.

Билды игры, BepInEx, interop DLL, SDK, ZIP, локальные настройки и сырые логи игнорируются Git. Remote: https://github.com/NeuroMita/NeuroMita-VR; изменение публичности выполняет владелец репозитория.

Код и документация мода распространяются по [лицензии NeuroMita](LICENSE), скопированной из канонического проекта: https://github.com/VinerX/NeuroMita. Copyright (c) 2025–2026 VinerX and NeuroMita contributors. Сторонние компоненты и ресурсы сохраняют свои условия; эта лицензия не предоставляет прав на материалы MiSide.
Текущий мод проверяется в отдельном IL2CPP-билде под Visual Studio: квартира загружается, движение и подбор подтверждены, исправлены native crash кистей и порядок разовых нажатий. Полной функциональной приёмки нет; актуальные ограничения — в [checkpoint](docs/checkpoint.md).

В текущих исходниках добавлены VR Settings в паузе и выход из ТВ-мини-игр по левой Y. Эти новые UI-правки пока прошли только сборку/static audit; проверка в шлеме остаётся открытой.

Ресурсы для форков: [исходный Unity-проект и готовый bundle](assets/README.md). Для упаковки используется включённый bundle; Unity Editor нужен только для изменения ресурсов.