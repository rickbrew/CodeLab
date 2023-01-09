﻿using PaintDotNet.Controls;
using PaintDotNet.Direct2D1;
using PaintDotNet.Rendering;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PaintDotNet.Effects
{
    internal sealed class IndicatorBar : Direct2DControl
    {
        private Rectangle upButtonRect;
        private Rectangle downButtonRect;
        private Rectangle posSliderRect;
        private Rectangle posTrackRect;

        private bool upButtonHover;
        private bool downButtonHover;
        private bool posSliderHover;
        private bool posTrackHover;

        private bool upButtonClick;
        private bool downButtonClick;
        private bool posSliderClick;
        private bool posTrackClick;

        private Color arrowColor;
        private Color arrowColorHover;
        private Color arrowColorClick;

        private Color posTrackColor;
        private Color posColor;
        private Color posColorHover;
        private Color posColorClick;

        private Color caretColor;
        private Color errorColor;
        private Color warningColor;
        private Color matchColor;
        private Color bookmarkColor;

        private int posClicked;
        private readonly Timer arrowTimer = new Timer();
        private int trackDirection;


        private Theme theme = Theme.Light;
        private int caret = 0;
        private IEnumerable<int> errors = Array.Empty<int>();
        private IEnumerable<int> warnings = Array.Empty<int>();
        private IEnumerable<int> matches = Array.Empty<int>();
        private IEnumerable<int> bookmarks = Array.Empty<int>();
        private int maximum = 100;
        private int largeChange = 50;


        internal event EventHandler<ScrollEventArgs> Scroll;
        private void OnScroll(ScrollEventArgs args)
        {
            this.Scroll?.Invoke(this, args);
        }

        #region Properties
        internal Theme Theme
        {
            get
            {
                return theme;
            }
            set
            {
                theme = value;
                switch (value)
                {
                    case Theme.Dark:
                        arrowColor = Color.FromArgb(153, 153, 153);
                        arrowColorHover = Color.FromArgb(28, 151, 234);
                        arrowColorClick = Color.FromArgb(0, 122, 204);

                        posTrackColor = Color.FromArgb(62, 62, 66);
                        posColor = Color.FromArgb(104, 104, 104);
                        posColorHover = Color.FromArgb(158, 158, 158);
                        posColorClick = Color.FromArgb(239, 235, 239);

                        caretColor = Color.Gainsboro;
                        errorColor = Color.FromArgb(252, 62, 54);
                        warningColor = Color.FromArgb(149, 219, 125);
                        matchColor = Color.Orange;
                        bookmarkColor = Color.DeepSkyBlue;
                        break;

                    case Theme.Light:
                    default:
                        arrowColor = Color.FromArgb(134, 137, 153);
                        arrowColorHover = Color.FromArgb(28, 151, 234);
                        arrowColorClick = Color.FromArgb(0, 122, 204);

                        posTrackColor = Color.FromArgb(245, 245, 245);
                        posColor = Color.FromArgb(194, 195, 201);
                        posColorHover = Color.FromArgb(104, 104, 104);
                        posColorClick = Color.FromArgb(91, 91, 91);

                        caretColor = Color.FromArgb(0, 0, 205);
                        errorColor = Color.Red;
                        warningColor = Color.Green;
                        matchColor = Color.FromArgb(246, 185, 77);
                        bookmarkColor = Color.DeepSkyBlue;
                        break;
                }
            }
        }

        internal int Caret
        {
            get
            {
                return caret;
            }
            set
            {
                caret = value;
                Invalidate();
            }
        }

        internal IEnumerable<int> Errors
        {
            get
            {
                return errors;
            }
            set
            {
                errors = value;
                Invalidate();
            }
        }

        internal IEnumerable<int> Warnings
        {
            get
            {
                return warnings;
            }
            set
            {
                warnings = value;
                Invalidate();
            }
        }

        internal IEnumerable<int> Matches
        {
            get
            {
                return matches;
            }
            set
            {
                matches = value;
                Invalidate();
            }
        }

        internal IEnumerable<int> Bookmarks
        {
            get
            {
                return bookmarks;
            }
            set
            {
                bookmarks = value;
                Invalidate();
            }
        }

        internal int Value
        {
            get
            {
                float scale = posTrackRect.Height / (float)maximum;
                return (int)MathF.Round((posSliderRect.Y - posTrackRect.Top) / scale);
            }
            set
            {
                float scale = posTrackRect.Height / (float)maximum;
                posSliderRect.Y = posTrackRect.Top + (int)MathF.Round(value * scale);
                if (posSliderRect.Top < posTrackRect.Top)
                {
                    posSliderRect.Y = posTrackRect.Top;
                }
                else if (posSliderRect.Bottom > posTrackRect.Bottom)
                {
                    posSliderRect.Y = posTrackRect.Bottom - posSliderRect.Height;
                }

                Refresh(); // Need to redraw very quickly here. Refresh() rather than Invalidate().
            }
        }

        internal int Maximum
        {
            get
            {
                return maximum;
            }
            set
            {
                maximum = value;

                if (largeChange > maximum)
                {
                    posSliderRect.Height = posTrackRect.Height;
                }
                else
                {
                    posSliderRect.Height = largeChange * posTrackRect.Height / value;
                }

                Invalidate();
            }
        }

        internal int LargeChange
        {
            get
            {
                return largeChange;
            }
            set
            {
                largeChange = value;

                if (largeChange > maximum)
                {
                    posSliderRect.Height = posTrackRect.Height;
                }
                else
                {
                    posSliderRect.Height = value * posTrackRect.Height / maximum;
                }

                Invalidate();
            }
        }
        #endregion

        internal IndicatorBar()
        {
            int width = SystemInformation.VerticalScrollBarWidth;
            base.Width = width;

            this.upButtonRect.Size = new Size(width, width);
            this.downButtonRect.Size = new Size(width, width);

            this.posSliderRect.Width = width;

            this.posTrackRect = Rectangle.FromLTRB(this.ClientRectangle.Left, upButtonRect.Bottom + 1, this.ClientRectangle.Right, downButtonRect.Top - 1);

            this.arrowTimer.Enabled = false;
            this.arrowTimer.Interval = 500;
            this.arrowTimer.Tick += (sender, e) => scrollByDelta();

            this.Theme = Theme.Light;

            base.Dock = DockStyle.Right;
            base.Cursor = Cursors.Default;
            base.DoubleBuffered = true;
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);

            if (base.Width != SystemInformation.VerticalScrollBarWidth)
            {
                base.Width = SystemInformation.VerticalScrollBarWidth;
            }

            downButtonRect.Y = this.ClientRectangle.Bottom - downButtonRect.Height;

            posTrackRect.Height = downButtonRect.Top - upButtonRect.Bottom - 2;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (upButtonRect.Contains(e.Location))
            {
                if (!upButtonHover)
                {
                    upButtonHover = true;
                    this.Invalidate();
                }
            }
            else if (upButtonHover)
            {
                upButtonHover = false;
                this.Invalidate();
            }

            if (downButtonRect.Contains(e.Location))
            {
                if (!downButtonHover)
                {
                    downButtonHover = true;
                    this.Invalidate();
                }
            }
            else if (downButtonHover)
            {
                downButtonHover = false;
                this.Invalidate();
            }

            if (posSliderRect.Contains(e.Location))
            {
                if (!posSliderHover)
                {
                    posSliderHover = true;
                    this.Invalidate();
                }
            }
            else if (posSliderHover)
            {
                posSliderHover = false;
                this.Invalidate();
            }

            if (!posSliderRect.Contains(e.Location) && posTrackRect.Contains(e.Location))
            {
                if (trackDirection == -1 && e.Y > posSliderRect.Top)
                {
                    trackDirection = 0;
                }
                else if (trackDirection == 1 && e.Y < posSliderRect.Bottom)
                {
                    trackDirection = 0;
                }

                if (!posTrackHover)
                {
                    posTrackHover = true;
                }
            }
            else if (posTrackHover)
            {
                posTrackHover = false;
            }

            if (posSliderClick && posTrackRect.Contains(0, e.Y))
            {
                posSliderRect.Y -= posClicked - e.Y;

                if (posSliderRect.Top < posTrackRect.Top)
                {
                    posSliderRect.Y = posTrackRect.Top;
                }
                else if (posSliderRect.Bottom > posTrackRect.Bottom)
                {
                    posSliderRect.Y = posTrackRect.Bottom - posSliderRect.Height;
                }

                posClicked = e.Y;
                Refresh(); // Need to redraw very quickly here. Refresh() rather than Invalidate().
                OnScroll(new ScrollEventArgs(ScrollEventType.ThumbTrack, this.Value));
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (upButtonHover)
            {
                upButtonHover = false;
                this.Invalidate();
            }

            if (downButtonHover)
            {
                downButtonHover = false;
                this.Invalidate();
            }

            if (posSliderHover)
            {
                posSliderHover = false;
                this.Invalidate();
            }

            if (posTrackHover)
            {
                posTrackHover = false;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (upButtonRect.Contains(e.Location))
            {
                upButtonClick = true;
                this.Invalidate();
                scrollByDelta();
                arrowTimer.Enabled = true;
            }
            else if (downButtonRect.Contains(e.Location))
            {
                downButtonClick = true;
                this.Invalidate();
                scrollByDelta();
                arrowTimer.Enabled = true;
            }
            else if (posSliderRect.Contains(e.Location))
            {
                posSliderClick = true;
                posClicked = e.Y;
                this.Invalidate();
            }
            else if (posTrackRect.Contains(e.Location))
            {
                posTrackClick = true;
                posClicked = e.Y;
                trackDirection = (e.Y < posSliderRect.Top) ? -1 : 1;
                scrollByDelta();
                arrowTimer.Enabled = true;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (posSliderClick)
            {
                OnScroll(new ScrollEventArgs(ScrollEventType.EndScroll, this.Value));
            }

            upButtonClick = false;
            downButtonClick = false;
            posSliderClick = false;
            posTrackClick = false;

            trackDirection = 0;

            arrowTimer.Enabled = false;
            arrowTimer.Interval = 500;

            this.Invalidate();
        }

        protected override void OnRender(Direct2D1.IDeviceContext deviceContext, RectFloat clipRect)
        {
            base.OnRender(deviceContext, clipRect);

            Point2Float[] upArrow =
            {
                new Point2Int32(upButtonRect.Width / 2, upButtonRect.Height / 3),
                new Point2Int32(upButtonRect.Width * 4 / 5, upButtonRect.Height * 2 / 3),
                new Point2Int32(upButtonRect.Width / 5, upButtonRect.Height * 2 / 3),
            };

            Point2Float[] downArrow =
            {
                new Point2Int32(downButtonRect.Width / 5 + 1, downButtonRect.Top + downButtonRect.Height / 3),
                new Point2Int32(downButtonRect.Width * 4 / 5, downButtonRect.Top + downButtonRect.Height / 3),
                new Point2Int32(downButtonRect.Width / 2, downButtonRect.Top - 1 + downButtonRect.Height * 2 / 3)
            };

            using (ISolidColorBrush brush = deviceContext.CreateSolidColorBrush(posTrackColor))
            {
                deviceContext.FillRectangle(clipRect, brush);

                brush.Color = upButtonClick ? arrowColorClick : upButtonHover ? arrowColorHover : arrowColor;
                deviceContext.FillPolygon(upArrow, brush);

                brush.Color = downButtonClick ? arrowColorClick : downButtonHover ? arrowColorHover : arrowColor;
                deviceContext.FillPolygon(downArrow, brush);

                brush.Color = posSliderClick ? posColorClick : posSliderHover ? posColorHover : posColor;
                RectInt32 posRect = new RectInt32(posSliderRect.Width / 4, posSliderRect.Y, posSliderRect.Width - posSliderRect.Width / 2, posSliderRect.Height);
                deviceContext.FillRectangle(posRect, brush);
            }

            float dpiY = deviceContext.Dpi.Y / 96f;

            using (ISolidColorBrush caretBrush = deviceContext.CreateSolidColorBrush(caretColor))
            {
                float curLineVPos = (float)(caret + 0) / maximum * posTrackRect.Height + posTrackRect.Top;
                curLineVPos = curLineVPos.Clamp(posTrackRect.Top * dpiY, posTrackRect.Bottom * dpiY);
                deviceContext.DrawLine(posTrackRect.Left, curLineVPos, posTrackRect.Right, curLineVPos, caretBrush, 2f * dpiY);
            }

            using (ISolidColorBrush indicatorPen = deviceContext.CreateSolidColorBrush(matchColor))
            {
                float strokeWidth = 4f * dpiY;

                indicatorPen.Color = bookmarkColor;
                foreach (int bookmark in this.bookmarks)
                {
                    float bkmkVPos = (float)bookmark / maximum * posTrackRect.Height + posTrackRect.Top;
                    bkmkVPos = bkmkVPos.Clamp(posTrackRect.Top, posTrackRect.Bottom);
                    deviceContext.DrawLine(posTrackRect.Left + 6f * dpiY, bkmkVPos, posTrackRect.Right - 6f * dpiY, bkmkVPos, indicatorPen, strokeWidth);
                }

                indicatorPen.Color = matchColor;
                foreach (int match in this.matches)
                {
                    float matchLineVPos = (float)match / maximum * posTrackRect.Height + posTrackRect.Top;
                    matchLineVPos = matchLineVPos.Clamp(posTrackRect.Top, posTrackRect.Bottom);
                    deviceContext.DrawLine(posTrackRect.Left, matchLineVPos, posTrackRect.Left + 4f * dpiY, matchLineVPos, indicatorPen, strokeWidth);
                }

                indicatorPen.Color = warningColor;
                foreach (int error in this.warnings)
                {
                    float warnLineVPos = (float)error / maximum * posTrackRect.Height + posTrackRect.Top;
                    warnLineVPos = warnLineVPos.Clamp(posTrackRect.Top, posTrackRect.Bottom);
                    deviceContext.DrawLine(posTrackRect.Right - 4f * dpiY, warnLineVPos, posTrackRect.Right, warnLineVPos, indicatorPen, strokeWidth);
                }

                indicatorPen.Color = errorColor;
                foreach (int error in this.errors)
                {
                    float errLineVPos = (float)error / maximum * posTrackRect.Height + posTrackRect.Top;
                    errLineVPos = errLineVPos.Clamp(posTrackRect.Top, posTrackRect.Bottom);
                    deviceContext.DrawLine(posTrackRect.Right - 4f * dpiY, errLineVPos, posTrackRect.Right, errLineVPos, indicatorPen, strokeWidth);
                }
            }
        }

        private void scrollByDelta()
        {
            if (arrowTimer.Enabled && arrowTimer.Interval != 10)
            {
                arrowTimer.Interval = 10;
            }

            int delta;
            ScrollEventType scrollType;
            if (upButtonClick && upButtonHover)
            {
                delta = -(int)MathF.Round(posTrackRect.Height / (float)maximum);

                scrollType = ScrollEventType.SmallDecrement;
            }
            else if (downButtonClick && downButtonHover)
            {
                delta = (int)MathF.Round(posTrackRect.Height / (float)maximum);
                scrollType = ScrollEventType.SmallIncrement;
            }
            else if (posTrackClick && posTrackHover)
            {
                if (posSliderRect.Contains(posSliderRect.Left, posClicked))
                {
                    OnScroll(new ScrollEventArgs(ScrollEventType.EndScroll, this.Value));
                    if (arrowTimer.Enabled)
                    {
                        arrowTimer.Enabled = false;
                    }
                    return;
                }

                if (trackDirection == -1)
                {
                    delta = -posSliderRect.Height;
                    scrollType = ScrollEventType.LargeDecrement;
                }
                else if (trackDirection == 1)
                {
                    delta = posSliderRect.Height;
                    scrollType = ScrollEventType.LargeIncrement;
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }

            posSliderRect.Y += delta;

            if (posSliderRect.Top < posTrackRect.Top)
            {
                posSliderRect.Y = posTrackRect.Top;
                scrollType = ScrollEventType.First;
            }
            else if (posSliderRect.Bottom > posTrackRect.Bottom)
            {
                posSliderRect.Y = posTrackRect.Bottom - posSliderRect.Height;
                scrollType = ScrollEventType.Last;
            }

            Invalidate();
            OnScroll(new ScrollEventArgs(scrollType, this.Value));
        }
    }
}
