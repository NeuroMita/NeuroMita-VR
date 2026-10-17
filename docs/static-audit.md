# Статический checkpoint — 2026-10-17

Пользователь запретил новые запуски до отдельного подтверждения. В этом проходе не запускались Unity Editor, Play Mode, игра, SteamVR или XR-сессия. DLL также не устанавливались в тестовый host.

## Направление разработки

Основной runtime теперь находится в репозитории мода: src/NeuroMita.VR.Runtime/Gameplay. C#-ссылка на UnityProjectDir удалена. VR-ветка Unity — полигон и источник сравнения, а не источник файлов для каждой сборки мода. Перенос основан на b89701b7d; происхождение описано в source-provenance.md.

## Исправлено по исходникам и метаданным

- Native casts вместо ненадёжных CLR type checks для world interaction target, capsule collider и TMP/legacy input fields.
- Tracking-origin subscription работает через сохранившееся native delegate field; удалены вызовы заведомо вырезанных add/remove методов. Существующие подписчики сохраняются.
- Pending bootstrap отменяется при unload; повторное расписание не создаёт второй стартёр.
- Ошибка XR pump или presentation initialization переводит runtime в остановку, вместо продолжения с частично созданными объектами.
- Cleanup UI, input session, camera и native XR выполняется отдельными защищёнными шагами.
- Native callbacks отключаются только если этот backend действительно их установил; чужой запущенный XR display не перехватывается.
- Managed-only методы с Action, массивами и внутренними panel types скрыты от IL2CPP injection.
- Поддержан --no-vr; отсутствие полного asset bundle выявляется до старта презентации.
- При наличии новых host context-action/TV HUD методов используются они; старый host сохраняет прежние interaction gates.
- Некорректная deadzone из конфигурации ограничивается до безопасного диапазона.
- Упаковщик читает локальные исходники мода, receipt указывает ревизию мода; Unity checkout больше не является C#-зависимостью.

## Проверки

tools/Test-Static.ps1, host 46d89dce1, Unity 6000.3.12f1, BepInEx 6.0.0-be.788 / Il2CppInterop 1.5.3:

- Bootstrap, runtime, probe и audit tool: 0 ошибок, 0 предупреждений.
- Bootstrap metadata audit: 77 ссылок, 0 неразрешённых/stripped.
- Runtime metadata audit: 1653 ссылки, 0 неразрешённых/stripped.
- Native exports: 19 импортов, отсутствующих экспортов нет. PE читаются как файлы; библиотеки не исполняются.
- Контроль на предыдущем игровом ZIP: анализатор обнаруживает два tracking-origin stubs и stripped Mathf.Min overload, возвращает код 1.
- PowerShell синтаксис и diff whitespace проверяются перед коммитом.

## Что остаётся проверить запуском

Новых конкретных блокеров в выполненном статическом проходе не осталось. Это не доказательство корректности всего runtime: аудит ограничен шестью уровнями вызовов и не проверяет визуальное поведение, полную ABI-семантику, runtime reflection и все Unity native bindings.

Нужны отдельные тесты после разрешения: холодный старт/закрытие, изображение в обоих глазах и Game mirror, poses, Touch buttons/sticks, main menu -> house -> pause, смартфон/selfie, радио/UI Toolkit, чат/виртуальная клавиатура, потеря tracking и восстановление.

VR-настройки мода доступны через BepInEx config; полноценная независимая страница настроек в pause menu остаётся продуктовой задачей. Controller profile пока только Oculus Touch. Старый host 46d89dce1 не содержит новейших interaction/TV HUD API; новый release host с fresh interop отдельно не проверялся. При необходимости расширения host они должны оформляться отдельным патчем по наблюдаемому дефекту в моде.

Предыдущие ZIP в dist относятся к предыдущему checkpoint. Этот статический проход не создавал новый игровой ZIP и не заменял установленные DLL: тестировать нужно будет сборку текущего коммита, а не старый архив.