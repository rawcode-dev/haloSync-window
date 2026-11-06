namespace HaloSyncWin.Models
{
    public enum AmbientMode
    {
        Ambient,
        Movie,
        Gaming,
        Desktop,
        Reading,
        Music,
        Effects,
        Night,
        Custom
    }

    public record ModeParameters(float Brightness, float Smoothness, float AmbientStrength, int TargetFPS);

    public static class AmbientModeExtensions
    {
        public static string GetDisplayName(this AmbientMode mode) => mode switch
        {
            AmbientMode.Ambient => "Ambient",
            AmbientMode.Movie => "Movie",
            AmbientMode.Gaming => "Gaming",
            AmbientMode.Desktop => "Desktop",
            AmbientMode.Reading => "Reading",
            AmbientMode.Music => "Music",
            AmbientMode.Effects => "Effects",
            AmbientMode.Night => "Night",
            AmbientMode.Custom => "Custom",
            _ => mode.ToString()
        };

        public static ModeParameters GetDefaultParameters(this AmbientMode mode) => mode switch
        {
            AmbientMode.Ambient => new ModeParameters(0.80f, 0.55f, 0.70f, 60),
            AmbientMode.Movie   => new ModeParameters(0.85f, 0.70f, 0.85f, 60),
            AmbientMode.Gaming  => new ModeParameters(1.00f, 0.20f, 0.90f, 120),
            AmbientMode.Desktop => new ModeParameters(0.60f, 0.80f, 0.50f, 30),
            AmbientMode.Reading => new ModeParameters(0.40f, 0.95f, 0.30f, 10),
            AmbientMode.Music   => new ModeParameters(0.90f, 0.10f, 0.80f, 60),
            AmbientMode.Effects => new ModeParameters(0.80f, 0.30f, 1.00f, 60),
            AmbientMode.Night   => new ModeParameters(0.15f, 0.90f, 0.20f, 10),
            AmbientMode.Custom  => new ModeParameters(0.80f, 0.50f, 0.80f, 60),
            _ => new ModeParameters(0.80f, 0.55f, 0.70f, 60)
        };
    }
}

