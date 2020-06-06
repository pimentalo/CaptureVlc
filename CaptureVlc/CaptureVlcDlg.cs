using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CaptureVlc
{
    public partial class CaptureVlcDlg : Form
    {
        private const string SK_CaptureVlc = @"Software\Wosgiens\CaptureVlc";
        private const string RK_VlcPath = @"VlcPath";
        private const string RK_LastOutputPath = @"LastOutputPath";

        private string _VlcPath;
        /// <summary>
        /// Le chemin vers Vlc, stocké en base de registre
        /// </summary>
        public string VlcPath
        {
            get
            {
                if (_VlcPath == null)
                {
                    _VlcPath = (string)Registry.CurrentUser.CreateSubKey(SK_CaptureVlc).GetValue(RK_VlcPath);
                }

                return _VlcPath;
            }
            set
            {
                _VlcPath = value;
                if (value != null)
                {
                    using (var regkey = Registry.CurrentUser.CreateSubKey(SK_CaptureVlc))
                    {
                        regkey.SetValue(RK_VlcPath, value);
                    }
                }
            }
        }

        private string _OutputPath;

        /// <summary>
        /// Le répertoire de sortie
        /// </summary>
        public string OutputPath
        {
            get
            {
                if (_OutputPath == null)
                {
                    _OutputPath = (string)Registry.CurrentUser.CreateSubKey(SK_CaptureVlc).GetValue(RK_LastOutputPath);
                }
                return _OutputPath;
            }
            set
            {
                _OutputPath = value;
                if (value != null)
                {
                    using (var regkey = Registry.CurrentUser.CreateSubKey(SK_CaptureVlc))
                    {
                        regkey.SetValue(RK_LastOutputPath, value);
                    }
                }
            }
        }


        public Rectangle CaptureArea { get; private set; }

        public CaptureVlcDlg()
        {
            InitializeComponent();
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;

            timer1.Interval = 1000;
            timer1.Enabled = true;
            timer1.Start();

            var formatsVideo = new List<FormatVideo>();
            formatsVideo.Add(new FormatVideo(@"MP4", @"#transcode{vcodec=h264,vb=0,scale=0,acodec=none}"));
            formatsVideo.Add(new FormatVideo(@"MP4+Son", @"#transcode{vcodec=h264,vb=0,scale=0,acodec=mpga,ab=128,channels=2,samplerate=44100}"));
            cboFormat.DataSource = formatsVideo;
            cboFormat.DisplayMember = "Nom";
            cboFormat.ValueMember = "OptionsVlc";

            using (var regkey = Registry.CurrentUser.CreateSubKey(SK_CaptureVlc))
            {
                VlcPath = (string)regkey.GetValue(RK_VlcPath, null);
            }
        }

        private Image _LastImage;
        private Image LastCapture
        {
            get
            {
                return _LastImage;
            }
            set
            {
                if (_LastImage != null)
                    _LastImage.Dispose();
                _LastImage = value;
                pictureBox1.Image = _LastImage;
            }
        }


        private string OutputFile { get; set; }


        /// <summary>
        /// Capture screen and draw capture area
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                var width = 0;
                var height = 0;
                var screens = Screen.AllScreens.OrderBy(s => s.Bounds.X);

                foreach (var screen in screens)
                {
                    width += screen.Bounds.Width;
                    height = height > screen.Bounds.Height ? height : screen.Bounds.Height;
                }

                var x = 0;
                var bmpScreenCapture = new Bitmap(width, height);
                foreach (var screen in screens)
                {
                    var size = new Size(screen.Bounds.Width, screen.Bounds.Height);
                    using (Graphics g = Graphics.FromImage(bmpScreenCapture))
                    {
                        g.CopyFromScreen(screen.Bounds.X,
                                         screen.Bounds.Y,
                                         x, screen.Bounds.Y,
                                         size,
                                         CopyPixelOperation.SourceCopy);
                        x += screen.Bounds.Width;

                        var Brush = new SolidBrush(Color.FromArgb(128, 0, 0, 255));
                        if (CaptureArea != null)
                        {
                            g.FillRectangle(Brush, CaptureArea);
                        }
                        Brush.Dispose();

                    }
                }
                LastCapture = bmpScreenCapture;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private List<CaptureWindow> Ecrans = new List<CaptureWindow>();

        private void btnSelectCaptureArea_Click(object sender, EventArgs e)
        {
            foreach (var screen in Screen.AllScreens)
            {
                var w = new CaptureWindow(screen);
                w.FormClosed += W_FormClosed;
                Ecrans.Add(w);
                w.Show(this);
                w.TopMost = true;
                this.Hide();
            }
        }

        /// <summary>
        /// La première fenêtre fermée défini la zone de capture
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void W_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Show();
            var captureWindow = (CaptureWindow)sender;

            // Adapts coordinates to have minimum x = 0
            // Just loop through screens in x order
            int xorg = Screen.AllScreens.OrderBy(s => s.Bounds.X).First().Bounds.X;
            var captureArea = captureWindow.CaptureZone;
            captureArea.Offset(-xorg, 0);
            CaptureArea = captureArea;


            label9.Text = string.Format("{0}:{1}->{2}:{3}", CaptureArea.X, CaptureArea.Y, CaptureArea.X + CaptureArea.Width, CaptureArea.Y + CaptureArea.Height);
            foreach (var c in Ecrans)
            {
                c.FormClosed -= W_FormClosed;
                if (c != sender) c.Close();
                c.Dispose();
            }
            Ecrans.Clear();
        }

        private string GetCaptureFilename()
        {
            return string.Format("Capture_{0:yyyy-MM-dd_HHmmss}", DateTime.Now) + ".MP4";
        }

        private Process VlcProcess { get; set; }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (!System.IO.File.Exists(VlcPath))
                {
                    throw new Exception("Veuillez sélectionner l'emplacement de VLC");
                }

                if (CaptureArea.Width == 0 || CaptureArea.Height == 0)
                {
                    throw new Exception("Veuillez définir la zone de capture");
                }

                if (VlcProcess != null)
                {
                    throw new Exception("VLC est déjà lancé");
                }

                if (!System.IO.Directory.Exists(OutputPath))
                {
                    throw new Exception("Veuillez sélectionner un répertoire de destination");
                }
                OutputFile = System.IO.Path.Combine(OutputPath, GetCaptureFilename());

                var commandLine = @"screen:// --qt-start-minimized --no-qt-updates-notif --one-instance --screen-fps={0} --screen-left={1} --screen-top={2} --screen-width={3} --screen-height={4} --sout={5}:standard{{access=file,mux=mp4,dst='{6}'}}";
                var width = CaptureArea.Width;
                var height = CaptureArea.Height;
                if ((width & 1) != 0) width += 1;
                if ((height & 1) != 0) height += 1;

                commandLine = string.Format(commandLine, nbFps.Value, CaptureArea.X, CaptureArea.Y, width, height, cboFormat.SelectedValue, OutputFile);

                VlcProcess = new Process();
                VlcProcess.EnableRaisingEvents = true;
                VlcProcess.ErrorDataReceived += VlcProcess_ErrorDataReceived;
                VlcProcess.Exited += VlcProcess_Exited;
                VlcProcess.StartInfo.UseShellExecute = false;
                VlcProcess.StartInfo.FileName = VlcPath;
                VlcProcess.StartInfo.Arguments = commandLine;
                VlcProcess.StartInfo.RedirectStandardInput = true;
                VlcProcess.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void VlcProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            MessageBox.Show(e.Data, "Erreur VLC");
        }

        private delegate void VlcProcess_ExitedDelegate(object sender, EventArgs e);
        private void VlcProcess_Exited(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                var d = new VlcProcess_ExitedDelegate(VlcProcess_Exited);
                this.Invoke(d, sender, e);
            }
            else
            {
                VlcProcess.Exited -= VlcProcess_Exited;
                VlcProcess.ErrorDataReceived -= VlcProcess_ErrorDataReceived;
                // Commented beacause it hangs everything
                //    VlcProcess.Dispose(); 
                VlcProcess = null;
                MessageBox.Show(this, "VLC s'est arrêté");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var path = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "VideoLan\\VLC\\vlc.exe");
            if (System.IO.File.Exists(path))
            {
                VlcPath = path;
                return;
            }
            path = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "VideoLan\\VLC\\vlc.exe");
            if (System.IO.File.Exists(path))
            {
                VlcPath = path;
                return;
            }
            var ofd = new OpenFileDialog();
            ofd.FileName = "vlc.exe";
            ofd.Filter = "*.exe";
            ofd.CheckFileExists = true;
            ofd.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                VlcPath = ofd.FileName;
                return;
            }
            VlcPath = null;
        }

        private void btnSelectOutputFile_Click(object sender, EventArgs e)
        {
            var ofd = new FolderBrowserDialog();
            ofd.ShowNewFolderButton = true;
            if (OutputPath == null)
            {
                ofd.SelectedPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            }
            else
            {
                ofd.SelectedPath = OutputPath;
            }
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {

                OutputPath = ofd.SelectedPath;

                return;
            }
            OutputFile = null;
        }

        private void btnArreter_Click(object sender, EventArgs e)
        {
            try
            {
                VlcProcess.StandardInput.WriteLine("vlc://quit");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
    }
}
