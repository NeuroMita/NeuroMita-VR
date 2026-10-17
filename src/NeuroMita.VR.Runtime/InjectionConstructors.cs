using System;
namespace NeuroMita.VR {
public sealed partial class VrClothHandContacts { public VrClothHandContacts(IntPtr pointer) : base(pointer) {} }
public sealed partial class VrComfortPresenter { public VrComfortPresenter(IntPtr pointer) : base(pointer) {} }
public sealed partial class VrContextActionsPresenter { public VrContextActionsPresenter(IntPtr pointer) : base(pointer) {} }
public sealed partial class VrGraphicsPresenter { public VrGraphicsPresenter(IntPtr pointer) : base(pointer) {} }
public sealed partial class VrHandProxy { public VrHandProxy(IntPtr pointer) : base(pointer) {} }
public sealed partial class VrHotbarPresenter { public VrHotbarPresenter(IntPtr pointer) : base(pointer) {} }
public sealed partial class VrHudPresenter { public VrHudPresenter(IntPtr pointer) : base(pointer) {} }
public sealed partial class VrMitaHandPhysics { public VrMitaHandPhysics(IntPtr pointer) : base(pointer) {} }
public sealed partial class VrMitaTouchTracker { public VrMitaTouchTracker(IntPtr pointer) : base(pointer) {} }
public sealed partial class VrPhonePresenter { public VrPhonePresenter(IntPtr pointer) : base(pointer) {} }
public sealed partial class VrPlayerRig { public VrPlayerRig(IntPtr pointer) : base(pointer) {} }
public sealed partial class VrRadioPresenter { public VrRadioPresenter(IntPtr pointer) : base(pointer) {} }
public sealed partial class VrRuntime { public VrRuntime(IntPtr pointer) : base(pointer) {} }
public sealed partial class VrToolkitInputRelay { public VrToolkitInputRelay(IntPtr pointer) : base(pointer) {} }
public sealed partial class VrTvPresenter { public VrTvPresenter(IntPtr pointer) : base(pointer) {} }
public sealed partial class VrUiPresenter { public VrUiPresenter(IntPtr pointer) : base(pointer) {} }
public sealed partial class VrVirtualKeyboard { public VrVirtualKeyboard(IntPtr pointer) : base(pointer) {} }
public sealed partial class VrRuntime
{
    private void Awake() => BepInEx.Unity.IL2CPP.Utils.MonoBehaviourExtensions.StartCoroutine(this, Start());
}
}