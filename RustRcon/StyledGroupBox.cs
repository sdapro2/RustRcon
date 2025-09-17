using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RustRcon
{
    public class StyledGroupBox : GroupBox
    {
        public StyledGroupBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.ForeColor = Color.Yellow;
            this.Font = new Font("Arial", 9F, FontStyle.Bold);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = this.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(200, 30, 30, 30)))
            {
                g.FillRectangle(brush, rect);
            }

            using (Pen borderPen = new Pen(Color.FromArgb(150, Color.Yellow), 2))
            {
                g.DrawRectangle(borderPen, rect);
            }

            if (!string.IsNullOrEmpty(this.Text))
            {
                SizeF textSize = g.MeasureString(this.Text, this.Font);
                Rectangle textRect = new Rectangle(10, 0, (int)textSize.Width + 10, (int)textSize.Height);
                
                using (SolidBrush textBgBrush = new SolidBrush(Color.FromArgb(30, 30, 30)))
                {
                    g.FillRectangle(textBgBrush, textRect);
                }

                using (SolidBrush textBrush = new SolidBrush(this.ForeColor))
                {
                    g.DrawString(this.Text, this.Font, textBrush, 15, 2);
                }

                using (Pen linePen = new Pen(Color.FromArgb(150, Color.Yellow), 1))
                {
                    g.DrawLine(linePen, textRect.Right, textRect.Height / 2, rect.Width - 10, textRect.Height / 2);
                }
            }
        }
    }
}
