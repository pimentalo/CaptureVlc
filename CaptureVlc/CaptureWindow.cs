using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CaptureVlc
{
    public partial class CaptureWindow : Form
    {
        public Screen Screen { get; }

        public CaptureWindow()
        {
            InitializeComponent();
            label1.BackColor = Color.Transparent;

            pictureBox1.DoubleClick += PictureBox1_DoubleClick;
            pictureBox1.MouseDown += PictureBox1_MouseDown;
            pictureBox1.MouseMove += PictureBox1_MouseMove;
            pictureBox1.MouseUp += PictureBox1_MouseUp;
            pictureBox1.Paint += PictureBox1_Paint;

            label2.Visible = false;
          
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.DrawRectangle(Pens.Black, CaptureX, CaptureY, CaptureWidth, CaptureHeight);
        }

        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            label1.Visible = true;
            label2.Visible = false;
            Close();
        }

        private void PictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if ((CaptureStart != null) && (CaptureEnd != null))
            {
                e.Graphics.DrawRectangle(Pens.Black, CaptureX, CaptureY, CaptureWidth, CaptureHeight);
            }
        }

        private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (MouseButtons.HasFlag(MouseButtons.Left))
            {
                var position = PointToClient(MousePosition);

                CaptureEnd = position;

                pictureBox1.Invalidate();
                label2.Text = String.Format("{0}:{1} x {2}:{3}", CaptureX, CaptureY, CaptureX + CaptureWidth, CaptureY + CaptureHeight);
            
                using (var g = pictureBox1.CreateGraphics())
                {
                    var brush =  new SolidBrush(Color.FromArgb(128, 0, 0, 255));
                    g.FillRectangle(brush, CaptureX, CaptureY, CaptureWidth, CaptureHeight);
                    g.DrawRectangle(Pens.Black, CaptureX, CaptureY, CaptureWidth, CaptureHeight);
                    brush.Dispose();
                }
            }
        }

        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                CaptureStart = PointToClient(MousePosition);
                label1.Visible = false;
                label2.Visible = true;
            }
        }



        private void PictureBox1_DoubleClick(object sender, EventArgs e)
        {
            this.Close();
        }

        public Point CaptureStart { get; private set; }
        public Point CaptureEnd { get; private set; }

        public Rectangle CaptureZone { get => new Rectangle(Screen.Bounds.X + CaptureX, Screen.Bounds.Y + CaptureY, CaptureWidth, CaptureHeight); }
        public int CaptureX =>  (CaptureStart.X < CaptureEnd.X ? CaptureStart.X : CaptureEnd.X);
        public int CaptureY =>  (CaptureStart.Y < CaptureEnd.Y ? CaptureStart.Y : CaptureEnd.Y);
        public int CaptureWidth => CaptureStart.X < CaptureEnd.X ? CaptureEnd.X - CaptureStart.X : CaptureStart.X - CaptureEnd.X;
        public int CaptureHeight => CaptureStart.Y < CaptureEnd.Y ? CaptureEnd.Y - CaptureStart.Y : CaptureStart.Y - CaptureEnd.Y;


        public CaptureWindow(Screen screen) : this()
        {
            this.Screen = screen;
            var bmpScreenCapture = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);

            this.StartPosition = FormStartPosition.Manual;
            Location = screen.WorkingArea.Location;
            Size = bmpScreenCapture.Size;

            //using (Graphics g = Graphics.FromImage(bmpScreenCapture))
            //{
            //    g.CopyFromScreen(screen.Bounds.X,
            //                     screen.Bounds.Y,
            //                     0, 0,
            //                     bmpScreenCapture.Size,
            //                     CopyPixelOperation.SourceCopy);
            //}


     //       pictureBox1.Image = bmpScreenCapture;
        }
    }
}
