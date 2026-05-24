namespace HospitalApp.Theme;

/// <summary>
/// Helper animation pure manual với System.Windows.Forms.Timer.
/// Tránh dependency MaterialSkin.NET để không conflict với UiTheme + GlassPanel.
/// Duration mặc định 200ms theo recommendation skill (150-300ms range).
/// </summary>
public static class Animator
{
    /// <summary>
    /// Tween 1 giá trị từ from → to qua durationMs với easing.
    /// Callback nhận giá trị hiện tại, được gọi mỗi frame.
    /// </summary>
    public static void Tween(int durationMs, double from, double to,
                             Action<double> onTick, Action? onDone = null,
                             Easing easing = Easing.EaseOutCubic)
    {
        if (durationMs <= 0) { onTick(to); onDone?.Invoke(); return; }

        var timer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60fps
        var startedAt = DateTime.Now;

        timer.Tick += (_, _) =>
        {
            var elapsed = (DateTime.Now - startedAt).TotalMilliseconds;
            var t = Math.Clamp(elapsed / durationMs, 0, 1);
            var eased = ApplyEasing(t, easing);
            var value = from + (to - from) * eased;

            try { onTick(value); }
            catch { /* swallow paint errors */ }

            if (t >= 1.0)
            {
                timer.Stop();
                timer.Dispose();
                onDone?.Invoke();
            }
        };
        timer.Start();
    }

    /// <summary>Fade Form Opacity from current to target.</summary>
    public static void FadeTo(Form form, double target, int durationMs = 200, Action? onDone = null)
    {
        if (form.IsDisposed) return;
        var from = form.Opacity;
        Tween(durationMs, from, target,
            v => { if (!form.IsDisposed) form.Opacity = v; },
            onDone);
    }

    /// <summary>Slide control's Left from current to target.</summary>
    public static void SlideLeft(Control c, int targetLeft, int durationMs = 250, Action? onDone = null)
    {
        if (c.IsDisposed) return;
        var from = c.Left;
        Tween(durationMs, from, targetLeft,
            v => { if (!c.IsDisposed) c.Left = (int)v; },
            onDone);
    }

    /// <summary>Tween any int property (Width, Height, Top, Left, etc.).</summary>
    public static void TweenInt(int durationMs, int from, int to, Action<int> apply,
                                Action? onDone = null, Easing easing = Easing.EaseOutCubic)
        => Tween(durationMs, from, to, v => apply((int)v), onDone, easing);

    public enum Easing
    {
        Linear,
        EaseOutCubic,
        EaseInCubic,
        EaseInOutCubic,
        EaseOutQuart
    }

    private static double ApplyEasing(double t, Easing e) => e switch
    {
        Easing.Linear         => t,
        Easing.EaseOutCubic   => 1 - Math.Pow(1 - t, 3),
        Easing.EaseInCubic    => t * t * t,
        Easing.EaseInOutCubic => t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2,
        Easing.EaseOutQuart   => 1 - Math.Pow(1 - t, 4),
        _ => t
    };
}
