# Сборка и статические проверки

Runtime мода самостоятелен: Gameplay, injection и native OpenXR backend хранятся здесь. Для сборки DLL не требуется UnityProjectDir или запущенный Editor. Нужны .NET SDK, совместимый Windows x64 IL2CPP host с BepInEx 6 и generated interop именно этого host.

    dotnet build .\src\NeuroMitaVRmod\NeuroMitaVRmod.csproj -c Release
    dotnet build .\src\NeuroMita.VR.Runtime\NeuroMita.VR.Runtime.csproj -c Release -p:HostDirectory='D:\Games\NeuroMita-Test'

Проверенный host: 46d89dce1, Unity 6000.3.12f1, BepInEx 6.0.0-be.788, Il2CppInterop 1.5.3. Подстановка Editor DLL вместо interop не допускается.

Офлайн-аудит на .NET 8 читает метаданные и PE export tables; он не загружает Unity/OpenXR библиотеки для исполнения:

    dotnet run --project .\tools\InteropAudit\InteropAudit.csproj -c Release -p:HostDirectory='D:\Games\NeuroMita-Test' -- '.\src\NeuroMita.VR.Runtime\bin\Release\net6.0\NeuroMita.VR.Runtime.dll' 'D:\Games\NeuroMita-Test'

Аудит ищет достижимые Method unstripping failed stubs, неразрешённые ссылки и отсутствующие native exports. Глубина транзитивных вызовов ограничена шестью; reflection, реальное поведение Unity bindings и корректность ABI он полностью не доказывает. Это дополнительная проверка, не замена игровому запуску.

Для игрового ZIP используется assets/prebuilt/neuromita-vr-assets из репозитория; отдельно нужен официальный пакет Unity OpenXR 1.16.1:

    .\tools\Package-VrMod.ps1 -HostDirectory 'D:\Games\NeuroMita-Test' -OpenXRPackage 'D:\Dependencies\com.unity.xr.openxr'

Скрипт не запускает Unity и не меняет установленный host. Можно переопределить bundle параметром -AssetBundle или передать -UnityProject для поиска Temp/VrModAssets и Library/PackageCache. Исходники ресурсов и отдельный Unity-проект находятся в assets/source; пересборка описана в assets/README.md.

Игра, BepInEx и interop в ZIP не включаются. Receipt содержит ревизию самого мода и признак незакоммиченных исходников. Игровой пакет требует ручной приёмки в шлеме; сборка/static audit не заменяют проверку в шлеме.

SDK, host, interop, bin/obj/dist, архивы, env/private настройки и сырые логи остаются вне Git. Происхождение Gameplay-компонентов описано в source-provenance.md.
Повтор всего статического набора: .\tools\Test-Static.ps1 -HostDirectory 'D:\Games\NeuroMita-Test'. Запуск игры в этот скрипт не включён.
