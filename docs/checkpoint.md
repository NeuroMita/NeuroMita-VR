# Проверенный статус — 2026-10-17

Изолированный host: Windows x64 IL2CPP, исходники 46d89dce1, Unity 6000.3.12f1, BepInEx 6.0.0-be.788 и Il2CppInterop 1.5.3. Это конкретная тестовая копия, а не гарантия совместимости со всеми релизами.

- Bootstrap и полный внешний runtime собираются с реальными host interop DLL: 0 ошибок, 0 предупреждений.
- Общая игровая VR-логика используется из отдельного Unity-checkout. XR lifecycle заменён собственным native backend, без поддельных XR Management классов.
- Реальный BepInEx запуск зарегистрировал injected компоненты и дошёл до OpenXR active.
- Native session creation: True; Native XR display=True, input=True.
- Регистрация профиля Oculus Touch и attach action sets прошли: Touch controller actions attached.
- Материалы и меши кистей собраны отдельным AssetBundle. Это устраняет зависимость от наличия VR Resources в host.
- Исправлены обнаруженные при запуске ошибки CLR RectTransform casts, stripped BeforeRender helper, чтения pose через stripped Span wrapper и отсутствующего материала указателя.
- При повторной привязке камеры host вырезал tracking-origin event. Подписка защищена; после изменения origin может требоваться recenter обоими стиками.
- 37/37 выбранных Unity EditMode тестов общей логики прошли после переноса. Это Mono-проверки, они не заменяют IL2CPP запуск.
- Headset acceptance не выполнялась: изображение, руки, физические кнопки, house, пауза, смартфон и радио во внешнем моде остаются для ручной проверки.
- Старый API-only host не имеет новых TV HUD suppression hooks; этот путь ограничен адаптером совместимости.

HOST_API_PROBE_PASS относится к отдельной проверке host API в той копии. Эта дополнительная DLL в игровой ZIP не включается. Диагностический XR probe в игровой ZIP также не входит.

Ранее наблюдалась XR_ERROR_RUNTIME_FAILURE при отключённом wireless HMD; это историческое наблюдение. Новый native backend дошёл до создания сессии и запуска subsystem, поэтому прежний неуспешный probe не описывает текущий результат.

Сырые логи, игровые файлы и generated interop остаются локальными и игнорируются. Пакет сохраняет SHA256 файлов, версию OpenXR и ревизию общей Unity-логики в package-receipt.json. Неподтверждённые возможности не следует объявлять готовыми по одному успешному запуску.
После этого checkpoint сделан отдельный статический проход: Gameplay перенесён в мод, исправлены type checks, отложенный старт/выгрузка и cleanup. Новые DLL не запускались. Подробности и ограничения — static-audit.md; исторические successful startup строки выше относятся к предыдущей сборке.
