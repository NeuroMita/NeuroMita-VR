# NeuroMita VR mod — experimental work in progress

Внешний VR-мод для NeuroMita под Windows x64 / Unity IL2CPP / BepInEx 6.

**Это прототип, а не готовый к обычной игре релиз.** Полный игровой runtime уже собирается отдельно от игры и запускается через BepInEx. Перенос и проверка игровых сценариев продолжаются; качество изображения и контроллеров в шлеме ещё не принято.

| Компонент | Проверено |
| --- | --- |
| Bootstrap и игровой runtime | Собираются на реальных IL2CPP interop DLL без ошибок и предупреждений |
| Нативный OpenXR backend | Создаёт сессию, запускает display/input на отдельном тестовом host |
| Touch controller actions | Регистрация и attach проходят; физические нажатия пока не проверены |
| Меши кистей, материалы указателя и comfort overlay | Вынесены в отдельный AssetBundle, собираемый из Unity-проекта |
| Изображение, меню и игра в шлеме через мод | Требуют отдельной проверки; запуск не является подтверждением |

Игровой пакет и XR probe различаются: probe остаётся диагностическим и не должен одновременно управлять OpenXR с игровым плагином.

- [Установка игрового пакета](docs/gameplay-package.md).
- [Диагностика отдельно](docs/installation.md).
- [Сборка из исходников](docs/development.md).
- [Проверенный статус и ограничения](docs/checkpoint.md).
- [Подготовка к публикации](docs/publication.md).
- [Сторонние зависимости](THIRD_PARTY_NOTICES.md).

Исходники общей игровой VR-части находятся отдельно: [VinerX/NeuroMita-Unity, codex/neuromita-vr](https://github.com/VinerX/NeuroMita-Unity/tree/codex/neuromita-vr). Runtime-проект компилирует их через UnityProjectDir. Репозиторий мода не содержит копию игры.

- src/NeuroMitaVRmod — BepInEx bootstrap и настройки.
- src/NeuroMita.VR.Runtime — IL2CPP injection, native OpenXR backend и адаптеры общей VR-логики.
- src/NeuroMita.XR.Probe — независимая диагностика.
- tools/Package-VrMod.ps1 — игровой ZIP; tools/Start-VrMod.ps1 — запуск установленного мода.
- tools/Package-XrProbe.ps1 — отдельный диагностический ZIP.

Билды игры, BepInEx, interop DLL, SDK, ZIP, локальные настройки и сырые логи игнорируются Git. Remote пока не настроен, репозиторий остаётся локальным.

Собственные исходники мода — [MIT](LICENSE). Внешние исходники Unity-проекта, его ресурсы и сторонние зависимости сохраняют свои лицензии. MIT этого репозитория на них не распространяется.