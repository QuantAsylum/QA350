using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing.Imaging;

//
// This is the source of this control:
// http://www.codeproject.com/KB/miscctrl/transparent_controls.aspx?msg=2918915
//

namespace LightedButton2
{
    [DefaultEvent("ButtonPressed")]  
    public partial class LightedButton2 : UserControl
    {
        public class ButtonPressedArgs : EventArgs
        {
            public ButtonPressedArgs()
            {

            }
        }

        public class ButtonOptionsArgs : EventArgs
        {
            public ButtonOptionsArgs()
            {

            }
        }

        private Color brushColor = Color.Transparent;
        private Color fillColor = Color.Transparent;
        private float lineThick = 1.0f;
        private float dr, dg, db;
        Color FadeColor;

        Bitmap ButtonOnBmp;
        Bitmap ButtonOffBmp;
        Bitmap ButtonFadeBmp;

        bool m_On;
        Color m_OnColor;
        Color m_OffColor;
        string m_TextWhenOn;
        string m_TextWhenOff;
        string m_GroupName;
        bool m_AllowAllOff = true;
        bool m_OneShot = false;
        int m_OneShotInterval = 100;
        bool m_AllowFadeToOff;
        bool m_OptionsMenu;


        //
        // Callbacks
        //
        public event EventHandler<ButtonPressedArgs> ButtonPressed;
        public event EventHandler<ButtonOptionsArgs> ButtonOptions;

        protected virtual void OnButtonPressed()
        {
            if (ButtonPressed != null)
            {
                ButtonPressed(this, new ButtonPressedArgs());  // Notify Subscribers
            }
        }

        protected virtual void OnButtonOptions()
        {
            if (ButtonOptions != null)
            {
                ButtonOptions(null, new ButtonOptionsArgs());
            }
        }

        public virtual void RemoteClick()
        {
            LightedButton_Click(null, null);    
        }

        [Description("Button state")]
        public bool On
        {
            get { return m_On; }
            set
            {
                //m_On = value;
                SetOnOffState(value);
                Invalidate();
            }
        }

        [Description("Determines whether or not all buttons in a group can be off.")]
        public bool AllowAllOff
        {
            get { return m_AllowAllOff; }
            set { m_AllowAllOff = value; }
        }

        [Description("Sets whether a button visually turns off instantly, or fades off gradually")]
        public bool AllowFadeToOff
        {
            get { return m_AllowFadeToOff; }
            set { m_AllowFadeToOff = value; }
        }

        [Description("If true, the button stays on momentarily")]
        public bool OneShot
        {
            get { return m_OneShot; }
            set { m_OneShot = value; }
        }

        [Description("The interval a one-shot button stays on")]
        public int OneShotInterval
        {
            get { return m_OneShotInterval; }
            set { m_OneShotInterval = value; }
        }

        [Description("The color of an button when on")]
        public Color OnColor
        {
            get { return m_OnColor; }
            set { 
                    m_OnColor = value;
                    ButtonOnBmp = null; // Cause it to be re-built on next draw
                    if (DesignMode)
                    {
                        ReloadAndResizeBitmaps();
                        Invalidate();
                    }
                }
        }

        [Description("The color of a button when off")]
        public Color OffColor
        {
            get { return m_OffColor; }
            set
            {
                m_OffColor = value;
                ButtonOffBmp = null; // Cause it to be re-built on next draw
                if (DesignMode)
                {
                    ReloadAndResizeBitmaps();
                    Invalidate();
                }
            }
        }

        [Description("The group a button belongs to")]
        public string GroupName
        {
            get { return m_GroupName; }
            set
            {
                m_GroupName = value;
             }
        }

        [Localizable(true)]
        [Description("The text used when a button is on")]
        public string TextWhenOn
        {
            get { return m_TextWhenOn; }
            set
            {
                m_TextWhenOn = value;
            }
        }

        [Localizable(true)]
        [Description("The text used when a button is off")]
        public string TextWhenOff
        {
            get { return m_TextWhenOff; }
            set
            {
                m_TextWhenOff = value;
            }
        }



        [Description("Displays an Options Menu Indicator")]
        public bool OptionMenuIndicator
        {
            get { return m_OptionsMenu; }
            set
            {
                m_OptionsMenu = value;
            }
        }

        /// <summary>
        /// Iterates through all parent controls of this type and group and returns the 
        /// name of the illuminated group. This allows the caller to determine which button
        /// in a group is active based on button text. Note this is VERY RISKY to call when button
        /// text MAY be localized in the future.
        /// </summary>
        [Description("The text shown on the face of the active button in a group. Be careful with this")]
        public string GetActiveButtonText
        {
            get
            {
                Control parent = this.Parent;

                foreach (Control c in parent.Controls)
                {
                    if (c is LightedButton2)
                    {
                        LightedButton2 lb = (LightedButton2)c;

                        if ((lb.On) && (lb.GroupName == this.GroupName))
                        {
                            return lb.TextWhenOn;
                        }
                    }
                }

                return "?";
            }
        }


        [Description("Fill color")]
        public Color FillColor
        {
            get
            {
                return this.fillColor;
            }
            set
            {
                this.fillColor = value;
                this.Invalidate();
            }
        }

        public float LineThick
        {
            get
            {
                return this.lineThick;
            }
            set
            {
                this.lineThick = value;
                this.Invalidate();
            }
        }

        public LightedButton2()
        {
            InitializeComponent();

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.ResizeRedraw, true);

            //Set style for double buffering
            SetStyle(ControlStyles.DoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);

            this.BackColor = Color.Transparent;

            m_OnColor = Color.Blue;
            m_OffColor = Color.DarkBlue;

            m_TextWhenOn = "ON";
            m_TextWhenOff = "OFF";

            if (DesignMode)
            {
                ReloadAndResizeBitmaps();
                Invalidate();
            }
        }

        // Handles all re-size requests.
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            if (width < 25) width = 25;
            if (height < 25) height = 25;

            if ((width != this.Width) || (height != this.Height) || (x != this.Location.X) || (y != this.Location.Y))
            {
                base.SetBoundsCore(x, y, width, height, specified);

                if (DesignMode)
                {
                    ReloadAndResizeBitmaps();
                    Invalidate();
                }
            }
        }

        private void ReloadAndResizeBitmaps()
        {
            if (ButtonOnBmp != null) ButtonOnBmp.Dispose();
            if (ButtonOffBmp != null) ButtonOffBmp.Dispose();

            ButtonOnBmp = ColorizeAndResizeBitmap(Resource1.ButtonWithGlow2, m_OnColor);
            ButtonOffBmp = ColorizeAndResizeBitmap(Resource1.Button2, m_OffColor);
        }



        /// <summary>
        /// Given a grayscale bitmap, recolorizes and resizes it to a new color
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        private Bitmap ColorizeAndResizeBitmap(Bitmap bmp, Color color)
        {
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    Color maskColor = bmp.GetPixel(i, j);
                    float lightness = maskColor.GetBrightness();
                    Color newColor = Color.FromArgb(maskColor.A, (int)(color.R * lightness), (int)(color.G * lightness), (int)(color.B * lightness));

                    bmp.SetPixel(i, j, newColor);
                }
            }

            bmp = (Bitmap)ResizeImage(bmp, new Size(this.Width, this.Height));

            return bmp;

            // Faster code. See http://www.codeproject.com/KB/GDI-plus/csharpgraphicfilters11.aspx
            if (false)
            {
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                int stride = bmpData.Stride;
                System.IntPtr Scan0 = bmpData.Scan0;

                //unsafe
                {
                    for (int i = 0; i < bmp.Width; i++)
                    {
                        for (int j = 0; j < bmp.Height; j++)
                        {
                            Color maskColor = bmp.GetPixel(i, j);
                            float lightness = maskColor.GetBrightness();
                            Color newColor = Color.FromArgb(maskColor.A, (int)(color.R * lightness), (int)(color.G * lightness), (int)(color.B * lightness));

                            bmp.SetPixel(i, j, newColor);
                        }
                    }
                }

                bmp = (Bitmap)ResizeImage(bmp, new Size(this.Width, this.Height));
                return bmp;
            }
        }

        /// <summary>
        /// Resizes a bit map
        /// </summary>
        /// <param name="imgToResize"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private static Image ResizeImage(Image imgToResize, Size size)
        {
            int destWidth = (int)(size.Width);
            int destHeight = (int)(size.Height);

            Bitmap b = new Bitmap(destWidth, destHeight);
            Graphics g = Graphics.FromImage((Image)b);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            g.Dispose();

            return (Image)b;
        }


        /// <summary>
        /// Handles paint message
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)   
        {
            Stopwatch sw = new Stopwatch();

            sw.Start();
            base.OnPaint(e);
            Graphics g = e.Graphics;   
            g.SmoothingMode = SmoothingMode.HighQuality;

            RectangleF r = new RectangleF(0.0f, 0.0f, (float)this.Width, (float)this.Height);
            float cx = r.Width - 1;
            float cy = r.Height - 1;

            Bitmap bmp;

            if (TurnOffTicks >0)
            {
                bmp = ButtonFadeBmp;
            }
            else if (m_On && Enabled)
            {
                if (ButtonOnBmp == null)
                    ButtonOnBmp = ColorizeAndResizeBitmap(Resource1.ButtonWithGlow2, m_OnColor);
                bmp = ButtonOnBmp;
            }
            else
            {
                if (ButtonOffBmp == null)
                    ButtonOffBmp = ColorizeAndResizeBitmap(Resource1.Button2, m_OffColor);
                bmp = ButtonOffBmp;
            }

            if (bmp == null)
                return;

            g.DrawImage(bmp, 0, 0);

            // Add text to the button
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;
            Brush brush1;

            if (m_On)
                brush1 = Brushes.Black;
            else
                brush1 = Brushes.LightGray;

            if (!Enabled)
                brush1 = Brushes.Black;

            g.DrawString(m_On ? m_TextWhenOn : m_TextWhenOff, this.Font, brush1, bmp.Width / 2, bmp.Height / 2, sf);

            SizeF textSize = g.MeasureString(m_On ? m_TextWhenOn : m_TextWhenOff, this.Font);
            if (m_OptionsMenu)
                g.FillEllipse(brush1, bmp.Width / 2 - textSize.Width / 2 - 7, bmp.Height / 2 - textSize.Height / 2 + 10, 4, 4);

            Pen pen = new Pen(new SolidBrush(this.ForeColor), lineThick);
            pen.Alignment = PenAlignment.Center;
            SolidBrush brush = new SolidBrush(fillColor);
            SolidBrush bckgnd = new SolidBrush(this.BackColor);

            GraphicsPath path = new GraphicsPath();

            // Paint the control
            if (this.BackColor == Color.Transparent)
            {
                path.AddRectangle(r);

                this.Region = new Region(path);
            }
            else
            {
                path.AddRectangle(r);
                this.Region = new Region(path);
                g.FillRegion(bckgnd, this.Region);
            }

            pen.Dispose();
            brush.Dispose();
            bckgnd.Dispose();
            this.Region.Dispose();
            path.Dispose();
            //g.Dispose();

            sw.Stop();
            
        }

        private void TurnOffOthersInTheGroup()
        {
            Control parent = this.Parent;

            if (parent == null)
                return;

            if ((GroupName != null) && (GroupName != ""))
            {
                foreach (Control c in parent.Controls)
                {
                    if (c is LightedButton2)
                    {
                        LightedButton2 lb = (LightedButton2)c;

                        // If we're turning the button on, the we need to make sure that others in the group
                        // are turned off
                        if ((lb.GroupName != "") && (lb.GroupName == GroupName) && (lb != this))
                        {
                            lb.On = false;
                        }
                    }
                }
            }  
        }

        private bool IsEveryoneElseInTheGroupOff()
        {
            Control parent = this.Parent;

            if (parent == null)
                return true;

            if ((GroupName != null) && (GroupName != ""))
            {
                foreach (Control c in parent.Controls)
                {
                    if (c is LightedButton2)
                    {
                        LightedButton2 lb = (LightedButton2)c;

                        // Check if anyone is turned on
                        if ((lb.GroupName != "") && (lb.GroupName == GroupName) && (lb != this))
                        {
                            if (lb.m_On)
                                return false;
                        }
                    }
                }

                return true;
            }

            return false;
        }

        int previousClint = SystemInformation.DoubleClickTime;

        private void LightedButton_Click(object sender, EventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                SetOnOffState(!m_On);
                Invalidate();
                OnButtonPressed();
            }
            else
                OnButtonOptions();
        }

        private void SetOnOffState(bool state)
        {
            if (state == m_On)
            {
                return;
            }

            m_On = state;

            if (m_On == true)
            {
                TurnOffOthersInTheGroup();

                if (m_OneShot)
                {
                    OneShotTimer.Interval = m_OneShotInterval;
                    OneShotTimer.Enabled = true;
                }
            }
            else
            {
                if (m_AllowAllOff == false && IsEveryoneElseInTheGroupOff())
                    m_On = true;
                else
                {
                    if (m_AllowFadeToOff)
                        FadeToOff();
                }
            }
        }

        private void FadeToOff()
        {
            TurnOffTimer.Enabled = true;
            TurnOffTimer.Interval = 50;
            TurnOffTicks = 10;
            FadeColor = m_OnColor;
            dr = (m_OnColor.R - m_OffColor.R) / 10;
            dg = (m_OnColor.G - m_OffColor.G) / 10;
            db = (m_OnColor.B - m_OffColor.B) / 10;
            ButtonFadeBmp = ColorizeAndResizeBitmap(Resource1.ButtonWithGlow2, FadeColor);
        }

        private void OneShotTimer_Tick(object sender, EventArgs e)
        {
            OneShotTimer.Enabled = false;
            On = false;
        }

        int TurnOffTicks;
        private void TurnOffTimer_Tick(object sender, EventArgs e)
        {
            if (TurnOffTicks > 0)
                --TurnOffTicks;

            if (TurnOffTicks == 0)
            {
                if (ButtonFadeBmp != null)
                    ButtonFadeBmp.Dispose();
                TurnOffTimer.Enabled = false;
                Invalidate();

            }
            else
            {
                FadeColor = Color.FromArgb(m_OnColor.A, (byte)(FadeColor.R - dr), (byte)(FadeColor.G - dg), (byte)(FadeColor.B - db));
                if (ButtonFadeBmp != null)
                    ButtonFadeBmp.Dispose();
                ButtonFadeBmp = ColorizeAndResizeBitmap(Resource1.ButtonWithGlow2, FadeColor);
                Invalidate();

            }
        }
    }
}
