# Сборка для разработки

Нужны .NET SDK с net6.0, отдельная копия совместимого Windows x64 IL2CPP host с BepInEx 6 и уже сгенерированными interop DLL, а также Unity-проект с веткой codex/neuromita-vr. Проверенный Unity: 6000.3.12f1; OpenXR: 1.16.1.

Используйте generated interop именно своего EXE. Managed Editor DLL не заменяют interop. Игровая VR-логика компилируется из внешнего checkout, а адаптеры, injection и native OpenXR backend находятся здесь.

    dotnet build .\src\NeuroMitaVRmod\NeuroMitaVRmod.csproj -c Release
    dotnet build .\src\NeuroMita.VR.Runtime\NeuroMita.VR.Runtime.csproj -c Release -p:HostDirectory='D:\Games\NeuroMita-Test' -p:UnityProjectDir='D:\Dev\NeuroMita-Unity'

Для asset bundle откройте Unity-проект и через Unity MCP execute_code вызовите:

    return NeuroMita.VR.Editor.VrModAssetBuild.Build();

Результат: Temp/VrModAssets/neuromita-vr-assets. AssetBundle содержит существующие меши кистей и материалы с shader dependencies; Unity не должен собирать для этого весь player. Перед публичной раздачей проверьте права на эти ресурсы отдельно от MIT собственных исходников мода.

Затем соберите игровой пакет:

    .\tools\Package-VrMod.ps1 -UnityProject 'D:\Dev\NeuroMita-Unity' -HostDirectory 'D:\Games\NeuroMita-Test'

ZIP появляется в dist, host не меняется. Пакет включает bootstrap/runtime DLL, AssetBundle, native OpenXR и manifest, уведомления и запускатель. Игра, BepInEx и interop в архив не включаются. В receipt сохраняется hash Unity-checkout и признак незакоммиченной VR-логики; для повторяемого checkpoint используйте чистую зафиксированную ревизию.

Диагностика собирается отдельно:

    .\tools\Package-XrProbe.ps1 -UnityProject 'D:\Dev\NeuroMita-Unity' -HostDirectory 'D:\Games\NeuroMita-Test'

Не запускайте XR probe и игровой VR backend одновременно. Install guide для диагностики остаётся отдельным; игровой запуск описан в gameplay-package.md.

Не коммитьте SDK, host, BepInEx, interop, bin/obj/dist, архивы, IDE/env/private настройки и сырые логи. Remote этого репозитория пока не настроен по решению владельца. Основные исходники распространяются под MIT; зависимости и ресурсы сохраняют отдельные лицензии.