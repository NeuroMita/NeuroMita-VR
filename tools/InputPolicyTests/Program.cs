using NeuroMita.VR;

int checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new InvalidOperationException(message);
}
var crouch = new VrCrouchToggle();
crouch.Sample(0, -1, true);
Check(!crouch.IsCrouched, "Held stick on entry must not crouch");
crouch.Sample(0, 0, true);
crouch.Sample(0, -0.8f, true);
Check(crouch.IsCrouched, "Down gesture must crouch");
for (int i = 0; i < 100; i++) crouch.Sample(0, -1, true);
Check(crouch.IsCrouched, "Holding down must toggle only once");
crouch.Sample(0, -0.3f, true);
crouch.Sample(0, -1, true);
Check(crouch.IsCrouched, "Partial release must not rearm");
crouch.Sample(0, 0, true);
crouch.Sample(0, -1, true);
Check(!crouch.IsCrouched, "Second deliberate gesture must stand");
crouch.Sample(0, 0, true);
crouch.Sample(0.8f, -0.8f, true);
crouch.Sample(0, -1, true);
Check(!crouch.IsCrouched, "Turning diagonally must consume the gesture");
crouch.Sample(0, 0, true);
crouch.Sample(0, -1, false);
crouch.Sample(0, -1, true);
Check(!crouch.IsCrouched, "UI scroll must not carry into crouch on return");
crouch.Sample(0, 0, true);
crouch.Sample(0, -1, true);
crouch.Sample(0, -1, false);
Check(crouch.IsCrouched, "Temporarily blocking input must preserve the selected stance");
crouch.Reset();
crouch.Sample(0, -1, true);
Check(!crouch.IsCrouched, "Tracking or scene reset must clear stance and require neutral");
crouch.Sample(0, 0, true);
crouch.Sample(0, 1, true);
crouch.Sample(0, -1, true);
Check(!crouch.IsCrouched, "Full up-to-down sweep must require neutral");
crouch.Sample(0, 0, true);
crouch.Sample(float.NaN, -1, true);
crouch.Sample(0, -1, true);
Check(!crouch.IsCrouched, "Invalid tracking values must disarm");
crouch.Sample(0.2f, -0.2f, true);
crouch.Sample(0.2f, -0.8f, true);
Check(crouch.IsCrouched, "Small lateral noise should allow deliberate downward input");
Console.WriteLine($"Crouch gesture policy: {checks} checks passed; no Unity/player/XR started.");
