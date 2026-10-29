namespace BoardgameTracker.App.Presentation.Controls;

public readonly record struct NumberWheelRange(int Minimum, int Maximum)
{
    public int DisplayedValue(int? value)
    {
        if (Minimum > Maximum)
            throw new InvalidOperationException("The wheel minimum must not exceed its maximum.");
        if (value.HasValue && (value < Minimum || value > Maximum))
            throw new ArgumentOutOfRangeException(nameof(value));

        return value ?? (int)(Minimum + ((long)Maximum - Minimum + 1) / 2);
    }
}
