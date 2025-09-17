using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RustRcon
{
    public class StyledPanel : Panel
    {
        public StyledPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.BackColor = Color.FromArgb(30, 30, 30);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = this.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(180, 30, 30, 30)))
            {
                g.FillRectangle(brush, rect);
            }

            using (Pen borderPen = new Pen(Color.FromArgb(100, Color.Yellow), 1))
            {
                g.DrawRectangle(borderPen, rect);
            }
        }
    }
}
