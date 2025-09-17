using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RustRcon
{
    public class StyledButton : Button
    {
        private bool _isHovered = false;
        private bool _isPressed = false;

        public StyledButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.BackColor = Color.FromArgb(40, 40, 40);
            this.ForeColor = Color.Yellow;
            this.Font = new Font("Arial", 9F, FontStyle.Bold);
            this.Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            this.Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            this.Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _isPressed = true;
            this.Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _isPressed = false;
            this.Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = this.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            Color startColor = _isPressed ? Color.FromArgb(60, 60, 60) : 
                                _isHovered ? Color.FromArgb(50, 50, 50) : Color.FromArgb(40, 40, 40);
            Color endColor = _isPressed ? Color.FromArgb(30, 30, 30) : 
                              _isHovered ? Color.FromArgb(35, 35, 35) : Color.FromArgb(25, 25, 25);

            using (LinearGradientBrush brush = new LinearGradientBrush(rect, startColor, endColor, LinearGradientMode.Vertical))
            {
                g.FillRectangle(brush, rect);
            }

            Color borderColor = _isHovered ? Color.Yellow : Color.FromArgb(100, 100, 100);
            int borderWidth = _isHovered ? 2 : 1;

            using (Pen borderPen = new Pen(borderColor, borderWidth))
            {
                g.DrawRectangle(borderPen, rect);
            }

            if (_isHovered)
            {
                using (Pen glowPen = new Pen(Color.FromArgb(100, Color.Yellow), 1))
                {
                    Rectangle innerRect = rect;
                    innerRect.Inflate(-1, -1);
                    g.DrawRectangle(glowPen, innerRect);
                }
            }

            StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            Color textColor = _isPressed ? Color.FromArgb(200, 200, 0) : Color.Yellow;
            using (SolidBrush textBrush = new SolidBrush(textColor))
            {
                g.DrawString(this.Text, this.Font, textBrush, rect, sf);
            }

            if (_isHovered)
            {
                using (LinearGradientBrush highlightBrush = new LinearGradientBrush(
                    new Rectangle(rect.X, rect.Y, rect.Width, rect.Height / 3),
                    Color.FromArgb(50, Color.Yellow),
                    Color.Transparent,
                    LinearGradientMode.Vertical))
                {
                    g.FillRectangle(highlightBrush, new Rectangle(rect.X, rect.Y, rect.Width, rect.Height / 3));
                }
            }
        }
    }
}
