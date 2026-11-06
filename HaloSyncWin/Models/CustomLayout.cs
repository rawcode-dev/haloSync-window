namespace HaloSyncWin.Models
{
    /// <summary>
    /// User-configured LED counts for each physical segment around the display.
    /// Mapping starts from Bottom Center and proceeds counter-clockwise:
    /// BottomLeft -> BottomLeftCorner -> Left -> TopLeftCorner -> Top ->
    /// TopRightCorner -> Right -> BottomRightCorner -> BottomRight.
    /// </summary>
    public class CustomLayout
    {
        public int BottomLeft { get; set; } = 17;
        public int BottomLeftCorner { get; set; } = 1;
        public int Left { get; set; } = 20;
        public int TopLeftCorner { get; set; } = 1;
        public int Top { get; set; } = 35;
        public int TopRightCorner { get; set; } = 1;
        public int Right { get; set; } = 20;
        public int BottomRightCorner { get; set; } = 1;
        public int BottomRight { get; set; } = 16;

        public int TotalLEDs =>
            BottomLeft + BottomLeftCorner +
            Left + TopLeftCorner +
            Top + TopRightCorner +
            Right + BottomRightCorner +
            BottomRight;

        public CustomLayout Clone()
        {
            return new CustomLayout
            {
                BottomLeft = this.BottomLeft,
                BottomLeftCorner = this.BottomLeftCorner,
                Left = this.Left,
                TopLeftCorner = this.TopLeftCorner,
                Top = this.Top,
                TopRightCorner = this.TopRightCorner,
                Right = this.Right,
                BottomRightCorner = this.BottomRightCorner,
                BottomRight = this.BottomRight
            };
        }
    }
}

