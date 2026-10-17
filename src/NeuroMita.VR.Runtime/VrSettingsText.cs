using NeuroMita.Menu;
using System.Text.RegularExpressions;

namespace NeuroMita.VR
{
    internal static class VrSettingsText
    {
        internal static bool Russian => LocalizationManager.CurrentLanguage == "Russian";
        internal static string Pick(string en, string ru) => Russian ? ru : en;
        internal static string Title(string name)
        {
            if (!Russian) return name == "VrHudOnWrist" ? "Microphone on wrist" :
                Regex.Replace(name.Substring(2), "([a-z])([A-Z])", "$1 $2");
            return name switch
            {
                "VrSmoothTurnRateDegreesPerSecond" => "Скорость плавного поворота",
                "VrTurnMode" => "Режим поворота",
                "VrSnapAngle" => "Угол пошагового поворота",
                "VrMovementDirection" => "Направление движения",
                "VrMovementSpeed" => "Скорость движения",
                "VrSprintMode" => "Режим бега",
                "VrRoomscale" => "Перемещение по комнате",
                "VrSeated" => "Игра сидя",
                "VrHeightOffset" => "Смещение высоты",
                "VrMoveDeadzone" => "Мёртвая зона движения",
                "VrTurnDeadzone" => "Мёртвая зона поворота",
                "VrUiDistance" => "Расстояние до меню",
                "VrUiScale" => "Размер меню",
                "VrUiHeight" => "Высота меню",
                "VrUiFollow" => "Следование меню",
                "VrHudOnWrist" => "Микрофон на запястье",
                "VrHotbarOnGaze" => "Слоты при взгляде на запястье",
                "VrPointerLeft" => "Указатель в левой руке",
                "VrHandPhysics" => "Столкновения кистей",
                "VrTouchAi" => "Реакции Миты на касания",
                "VrGraphicsPreset" => "Качество графики",
                "VrKeyboard" => "Виртуальная клавиатура",
                "VrAvatarMode" => "Отображение тела",
                "VrLeftWristPitch" => "Левая кисть: наклон",
                "VrLeftWristYaw" => "Левая кисть: поворот",
                "VrLeftWristRoll" => "Левая кисть: крен",
                "VrRightWristPitch" => "Правая кисть: наклон",
                "VrRightWristYaw" => "Правая кисть: поворот",
                "VrRightWristRoll" => "Правая кисть: крен",
                "VrWristSideOffset" => "Запястье: смещение вбок",
                "VrWristHeightOffset" => "Запястье: высота",
                "VrWristForwardOffset" => "Запястье: смещение вперёд",
                "VrVignette" => "Затемнение краёв при движении",
                "VrWallFade" => "Затемнение внутри стены",
                "VrHaptics" => "Вибрация",
                _ => name
            };
        }
    }
}
