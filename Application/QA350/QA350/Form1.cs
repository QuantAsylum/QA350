using HidLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph;



namespace QA350
{
    public partial class Form1 : Form
    {
        Form This;


        //static internal Hardware HW;

        CalibrationClass CalData;

        Settings AppSettings = new Settings();

        bool IsLowRange = false;

        /// <summary>
        /// Number of data counts from the last reading
        /// </summary>
        int RawDataCounts;


        /// <summary>
        /// Last raw voltage reading. This is the unadjusted voltage measured at the input pins of the ADC
        /// </summary>
        //double LastRawReading;

        /// <summary>
        /// Last reading. This has gain and offset adjustments applied.
        /// </summary>
        double LastReading;

        /// <summary>
        /// Offset user may apply to a reading
        /// </summary>
        double UserOffset;

        /// <summary>
        /// Holds the data that is graphed in the left hand graph (v versus t)
        /// </summary>
        PointPairList GraphData = new PointPairList();
        DateTime StartTime;

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont,
            IntPtr pdv, [System.Runtime.InteropServices.In] ref uint pcFonts);

        private PrivateFontCollection fonts = new PrivateFontCollection();

        // LCD Font for display
        Font LCDFontBig;
        Font LCDFontSmall;

        // Log file
        enum LoggingSpeedEnum { Fast_1KHz, Slow_2p5Hz}
        LoggingSpeedEnum LogSpeed = LoggingSpeedEnum.Slow_2p5Hz;
        bool LoggingEnabled = false;
        string LogFile = "";

        /// <summary>
        /// Tracks history of readings. This is cleared automatically on gain range changes
        /// </summary>
        List<double> Readings = new List<double>();

        public Form1()
        {
            This = this;
            InitializeComponent();
            //HW = new Hardware();

            if (File.Exists(Constants.SettingsFile))
            {
                // There's a settings file here. See if we can load it
                try
                {
                    AppSettings = (Settings)SerDes.Deserialize(typeof(Settings), File.ReadAllText(Constants.SettingsFile));
                }
                catch (Exception ex)
                {
                    // Something went wrong. Delete it.
                    MessageBox.Show(string.Format("There was a problem loading your settings from: {0}. The exception was: {1}", Constants.SettingsFile, ex.Message));
                    File.Delete(Constants.SettingsFile);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
#if !DEBUG
            flashVirginDeviceToolStripMenuItem.Visible = false;
#endif
            TryConnect();

            // If needed directories aren't present, create them
            if (Directory.Exists(Constants.DataFilePath) == false)
                Directory.CreateDirectory(Constants.DataFilePath);

            // Check if cal data exists and use it if it does
            if (File.Exists(Constants.CalibrationFile))
            {
                CalData = (CalibrationClass)SerDes.Deserialize(typeof(CalibrationClass), File.ReadAllText(Constants.CalibrationFile));
            }
            else
            {
                MessageBox.Show("A calibration file could not be loaded. A default file will be created");
                CalData = new CalibrationClass();
            }

            // Check if the user has calibrated the device yet
            if (CalData.IsDefault)
            {
                UncalLabel.Text = "UNCALIBRATED";
            }
            else
            {
                UncalLabel.Text = "";
            }

            // Indicate we are NOT in relative mode by hiding the label
            RelModeLabel.Visible = false;

            LoadFontData();

            InitGraphs();

            Text += "  (" + Constants.Version.ToString("0.00") + ")";
        }

        /// <summary>
        /// Called when the application is closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                File.WriteAllText(Constants.CalibrationFile, SerDes.Serialize(CalData));                
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was a problem writing the calibration data");
            }

            try
            {
                File.WriteAllText(Constants.SettingsFile, SerDes.Serialize(AppSettings));
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was a problem writing the settings data");
            }

            Hardware.Close();
        }

        /// <summary>
        /// Load the LCD font from resources
        /// </summary>
        private void LoadFontData()
        {
            byte[] fontData = QA350.Resource1.advanced_pixel_lcd_7asdf;
            IntPtr fontPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(fontData.Length);
            System.Runtime.InteropServices.Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
            uint dummy = 0;
            fonts.AddMemoryFont(fontPtr, QA350.Resource1.advanced_pixel_lcd_7asdf.Length);
            AddFontMemResourceEx(fontPtr, (uint)QA350.Resource1.advanced_pixel_lcd_7asdf.Length, IntPtr.Zero, ref dummy);
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(fontPtr);
            LCDFontBig = new Font(fonts.Families[0], 24.0F);
            LCDFontSmall = new Font(fonts.Families[0], 12);
            label3.Font = LCDFontBig;
            label10.Font = LCDFontSmall;
            label11.Font = LCDFontSmall;
            label7.Font = LCDFontSmall;
            label9.Font = LCDFontSmall;
        }

        /// <summary>
        /// Initialize the graph we display
        /// </summary>
        private void InitGraphs()
        {
            zedGraphControl1.GraphPane.Fill.Color = Color.Black;
            zedGraphControl1.GraphPane.Fill.Brush = new System.Drawing.SolidBrush(Color.Black);
            zedGraphControl1.GraphPane.Chart.Fill.Color = Color.Black;
            zedGraphControl1.GraphPane.Chart.Fill.Brush = new System.Drawing.SolidBrush(Color.Black);
            zedGraphControl1.GraphPane.XAxis.Title.FontSpec.FontColor = Color.LimeGreen;
            zedGraphControl1.GraphPane.YAxis.Title.FontSpec.FontColor = Color.LimeGreen;
            zedGraphControl1.GraphPane.YAxis.MajorGrid.Color = Color.LimeGreen;
            zedGraphControl1.GraphPane.XAxis.MajorGrid.Color = Color.LimeGreen;
            zedGraphControl1.GraphPane.XAxis.Color = Color.LimeGreen;
            zedGraphControl1.GraphPane.YAxis.Color = Color.LimeGreen;
            zedGraphControl1.GraphPane.YAxis.Scale.MajorStepAuto = false;
            zedGraphControl1.GraphPane.YAxis.MajorGrid.IsVisible = true;
            zedGraphControl1.GraphPane.Title.FontSpec.FontColor = Color.LimeGreen;
            zedGraphControl1.GraphPane.Title.FontSpec.Size = 30;
            zedGraphControl1.GraphPane.Title.Text = "---";
            zedGraphControl1.GraphPane.XAxis.Title.IsVisible = false;
            zedGraphControl1.GraphPane.YAxis.Title.IsVisible = false;
            zedGraphControl1.GraphPane.Margin.All = 0;


            zedGraphControl2.GraphPane.Fill.Color = Color.Black;
            zedGraphControl2.GraphPane.Fill.Brush = new System.Drawing.SolidBrush(Color.Black);
            zedGraphControl2.GraphPane.Chart.Fill.Color = Color.Black;
            zedGraphControl2.GraphPane.Chart.Fill.Brush = new System.Drawing.SolidBrush(Color.Black);
            zedGraphControl2.GraphPane.XAxis.Title.FontSpec.FontColor = Color.LimeGreen;
            zedGraphControl2.GraphPane.YAxis.Title.FontSpec.FontColor = Color.LimeGreen;
            zedGraphControl2.GraphPane.XAxis.Color = Color.LimeGreen;
            zedGraphControl2.GraphPane.YAxis.Color = Color.LimeGreen;
            zedGraphControl2.GraphPane.Title.FontSpec.FontColor = Color.LimeGreen;
            zedGraphControl2.GraphPane.Title.FontSpec.Size = 30; 
            zedGraphControl2.GraphPane.Title.Text = "---";
            zedGraphControl2.GraphPane.XAxis.Title.IsVisible = false;
            zedGraphControl2.GraphPane.YAxis.Title.IsVisible = false;
            zedGraphControl2.GraphPane.BarSettings.MinBarGap = 0;
            zedGraphControl2.GraphPane.BarSettings.MinClusterGap = 0;
            zedGraphControl2.GraphPane.Margin.All = 0;
        }

        private void TryConnect()
        {
            // Attempt to connect to the device upon app startup
            if (Hardware.Open())
            {
                Int32 fwVersion = Hardware.GetFirmwareVersion();

                if (fwVersion < Constants.RequiredFwVersion)
                {
                    MessageBox.Show("The device firware needs to be updated. Please use the 'Tools->Update QA350 Flash' menu option.");
                }

                toolStripStatusLabel1.Text = string.Format("Opened. (FW version = {0})", fwVersion.ToString());
                SetLowRange();
                SlowUpdateBtn.On = true;
                SlowUpdateBtn_ButtonPressed(null, null);

                LineItem line = new LineItem("", GraphData, Color.LimeGreen, SymbolType.None);
                zedGraphControl1.GraphPane.CurveList.Add(line);
                AcqTimer.Enabled = true;
            }
            else
            {
                toolStripStatusLabel1.Text = "Open failed...please plug in QA350";
                AcqTimer.Enabled = false; 
            }
        }

        private void SetLowRange()
        {
            IsLowRange = true;
            Hardware.SetAtten(0);
            ResetStats();
        }

        private void SetHighRange()
        {
            IsLowRange = false;
            Hardware.SetAtten(1);
            ResetStats();
        }

        /// <summary>
        /// LED timer kick. About every 800 mS, a 'ping' message is sent to the device to keep the 'LINK' LED
        /// lit. If the device isn't connected, then an attempt to connect is re-tried
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            //return;

            if (Hardware.IsConnected)
            {
                Hardware.KickLED();
            }
            else
            {
                TryConnect();
            }
        }

        /// <summary>
        /// Computes the unscaled voltage at the inputs pins. This does not take into 
        /// account front-end attenuators. This function merely computes the voltage given
        /// the specified number of counts AND the internal reference
        /// </summary>
        /// <param name="counts"></param>
        /// <returns></returns>
        static internal double ComputeUncalibratedVoltage(double counts)
        {
            double vRef = 2.048f;

            // See table 16 in ADS1256 spec
            double oneLSBinVolts = (2 * vRef) / (Math.Pow(2, 23) - 1);

            return counts * oneLSBinVolts;
        }

        private void ResetStats()
        {
            Readings.Clear();
            GraphData.Clear();
            StartTime = DateTime.Now;

            zedGraphControl1.AxisChange();
            zedGraphControl1.Invalidate();
            zedGraphControl2.AxisChange();
            zedGraphControl2.Invalidate();
        }

        int totalReads;
        /// <summary>
        /// This timer runs slightly slower than the sample rate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AckTimer_Tick(object sender, EventArgs e)
        {
            //return;

            bool ovf = false;

            // Check if we've been unplugged
            if (Hardware.IsConnected == false)
            {
                AcqTimer.Enabled = false;
                label3.Text = "--CONNECTING--";
                return;
            }

            // Read the raw counts
            RawDataCounts = Hardware.ReadVoltageCounts();

            // Convert counts to voltage.  
            double v;
            if (IsLowRange)
            {
                v = ComputeUncalibratedVoltage(RawDataCounts - CalData.LowRangeCountsOffset);
                v = v * CalData.LowRangeGain;
                if (v > 5) ovf = true;
            }
            else
            {
                v = ComputeUncalibratedVoltage(RawDataCounts - CalData.HiRangeCountsOffset);
                v = v * CalData.HiRangeGain;
                if (v > 50) ovf = true;
            }

            // Subtract the user offset (usually zero)
            v = v - UserOffset;

            // If we're at the fast sample rate, the equiv input impedance of the 
            // ADS buffer drops from 80M to 20M. To compensate, we need to gain up
            // the result
            if (FastUpdateBtn.On)
            {
                v = v * 1.00988;
            }

            // Save the last scaled and zero'd reading
            LastReading = v;

            // Don't grow the queue too long
            while (Readings.Count > AppSettings.SampleHistory)
                Readings.RemoveAt(0);

            label10.Text = Readings.Count.ToString();
            label11.Text = AppSettings.SampleHistory.ToString();

            string displayString = "";

            if (ovf)
            {
                displayString = "OVER VOLTAGE";
                label7.Text = "-";
                label9.Text = "-";
                ResetStats();
            }
            else
            {
                // Save for calculating stats
                Readings.Add(v);

                displayString = string.Format("{0:F6}", v);
                if (v >= 0)
                    displayString = "+" + displayString;

                label7.Text = Readings.Average().ToEngineeringNotation("F7");
                label9.Text = Readings.StandardDeviation().ToEngineeringNotation("F1");
            }

            label3.Text = displayString;
            ++totalReads;

            GraphData.Add(DateTime.Now.Subtract(StartTime).TotalSeconds, v);

            if (Readings.Count > 0)
            {
                double avg = Readings.Average();
                double spanSec = DateTime.Now.Subtract(StartTime).TotalSeconds;

                if (AppSettings.YAxisIsPPM)
                {
                    zedGraphControl1.GraphPane.Title.Text = string.Format("{0}PPM per div\nMean={1}V Span={2:0}sec", AppSettings.YAxisPPMperDiv, avg.ToEngineeringNotation("0.000"), spanSec);
                    zedGraphControl1.GraphPane.YAxis.Scale.MajorStep = GetYAxisPerDiv(avg);
                    zedGraphControl1.GraphPane.YAxis.Scale.Max = avg + GetYAxisPerDiv(avg) * 5;
                    zedGraphControl1.GraphPane.YAxis.Scale.Min = avg - GetYAxisPerDiv(avg) * 5;
                    zedGraphControl1.AxisChange();
                    zedGraphControl1.Invalidate();
                }
                else
                {
                    zedGraphControl1.GraphPane.Title.Text = string.Format("{0}V per div\nMean={1}V Span={2:0}sec", GetYAxisPerDiv(avg).ToEngineeringNotation("0.000"), 
                        avg.ToEngineeringNotation("0.000"), spanSec);
                    zedGraphControl1.GraphPane.YAxis.Scale.MajorStep = GetYAxisPerDiv(avg);
                    zedGraphControl1.GraphPane.YAxis.Scale.Max = avg + GetYAxisPerDiv(avg) * 5;
                    zedGraphControl1.GraphPane.YAxis.Scale.Min = avg - GetYAxisPerDiv(avg) * 5;
                    zedGraphControl1.AxisChange();
                    zedGraphControl1.Invalidate();
                }


                // Compute histogram
                AppSettings.BinCount = 100;
                Histogram h = new Histogram(Readings, GetBinSize(), AppSettings.BinCount);
                h.Plot(zedGraphControl2);
            }
        }

        // Reset stats button
        private void lightedButton22_ButtonPressed(object sender, LightedButton2.LightedButton2.ButtonPressedArgs e)
        {
            ResetStats();
        }

        /// <summary>
        /// Adjust gain range
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lightedButton28_ButtonPressed(object sender, LightedButton2.LightedButton2.ButtonPressedArgs e)
        {
            if (lightedButton28.On) { SetHighRange(); }
            else if (lightedButton29.On) { SetLowRange(); }
            ResetStats();
        }

        private void calibrateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SlowUpdateBtn.On = true;
            SlowUpdateBtn_ButtonPressed(null, null);

            AcqTimer.Enabled = false;

            DlgCalibrate2 dlg = new DlgCalibrate2();

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CalData = dlg.calData;

                if (CalData.IsDefault == false)
                    UncalLabel.Text = "";

                if (lightedButton28.On) { SetHighRange(); }
                else if (lightedButton29.On) { SetLowRange(); }
            }

            AcqTimer.Enabled = true;
        }

        private void lightedButton211_ButtonPressed(object sender, LightedButton2.LightedButton2.ButtonPressedArgs e)
        {
            if (SetRelBtn.On == true)
            {
                // Just turned it on
                ResetStats();
                UserOffset = LastReading;
                RelModeLabel.Visible = true;
            }
            else
            {
                // Just turned it off
                ResetStats();
                UserOffset = 0;
                RelModeLabel.Visible = false;
            }
        }

        private void lightedButton210_ButtonPressed(object sender, LightedButton2.LightedButton2.ButtonPressedArgs e)
        {
            SetRelBtn.On = false;
            UserOffset = 0;
            RelModeLabel.Visible = false;
        }

        private double GetBinSize()
        {
            if (AppSettings.HistoBinIsMV)
                return AppSettings.HistoBinInMV / 1e3;
            else if (AppSettings.HistoBinIsUV)
                return AppSettings.HistoBinInUV / 1e6;
            else
                throw new NotImplementedException("Exception in GetBinSize()");
        }

        private double GetYAxisPerDiv(double avg)
        {

            if (AppSettings.YAxisIsMV)
                return AppSettings.YAxisMVperDiv / 1e3;
            else if (AppSettings.YAxisIsUV)
                return AppSettings.YAxisUVPerDiv / 1e6;
            else if (AppSettings.YAxisIsPPM)
                return AppSettings.YAxisPPMperDiv * avg / 1e6;
            else
                throw new NotImplementedException("Exception in GetYAxisPerDiv()");
        }

        private void BtnStats_ButtonPressed(object sender, LightedButton2.LightedButton2.ButtonPressedArgs e)
        {
            DlgEditStats dlg = new DlgEditStats(AppSettings);

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ResetStats();
                AppSettings = dlg.AppSettings;
            }
        }

        //asdf

        //private void button1_Click(object sender, EventArgs e)
        //{
        //    AcqTimer.Enabled = false;
        //    LEDKickerTimer.Enabled = false;
        //    Bootloader.EnterBootloader();
        //}

        //asdf

        //private void button2_Click(object sender, EventArgs e)
        //{
        //    if (Hardware.IsConnected)
        //        Hardware.USBSendData(new byte[] { 0xFF, 0x00 });
        //}

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        // Re-flash 
        private void reflashToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //if (Hardware.IsConnected == false)
            //    return;

            // Bugbug: From here on out, redirect all console output to a listbox
            AcqTimer.Enabled = false;
            LEDKickerTimer.Enabled = false;
            toolStripStatusLabel1.Text = "Reflashing...";

            if (MessageBox.Show("You are about to reflash the firmware. If you proceed, it will take 3-4 minutes wihtout any updates until the end. Proceed?", "Important!", MessageBoxButtons.OKCancel) != DialogResult.OK)
            {
                LEDKickerTimer.Enabled = true;
                AcqTimer.Enabled = true; 
                return;
            }

            Hardware.EnterBSL();

            // Enter bootloader
            Thread.Sleep(2000);
            AcqTimer.Enabled = false;
            LEDKickerTimer.Enabled = false;
            Bootloader.EnterBootloader();

            MessageBox.Show("Restart the application and replug the hardware");
            Close();
        }

        private void flashVirginDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Bootloader.EnterBootloader();
        }

        private void loggingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (loggingToolStripMenuItem.Checked == false)  
            {
                
                

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.InitialDirectory = Constants.DataFilePath;
                sfd.Filter ="Log Files|*.log";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    loggingToolStripMenuItem.Checked = true;
                    LoggingEnabled = true;
                    LogFile = sfd.FileName;

                    if (SlowUpdateBtn.On)
                    {
                        LogSpeed = LoggingSpeedEnum.Slow_2p5Hz;
                        AcqTimer.Interval = 410;
                    }
                    else if (FastUpdateBtn.On)
                    {
                        LogSpeed = LoggingSpeedEnum.Fast_1KHz;
                        AcqTimer.Interval = 10;
                    }


                }
            }
            else
            {
                loggingToolStripMenuItem.Checked = false;
                LoggingEnabled = true;
                LogFile = "";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < 100; i++)
                {
                    Stopwatch sw = Stopwatch.StartNew();

                    while (Hardware.GetFifoDepth() < 12)
                    {
                        sb.AppendLine("Sleep 5ms");
                        Thread.Sleep(5);
                    }

                    StreamSample[] buffer1 = Hardware.ReadVoltageStream();
                    sw.Stop();
                    double elapsed1 = sw.Elapsed.TotalMilliseconds;

                    for (int j = 0; j < buffer1.Length; j++)
                    {
                        sb.AppendLine(string.Format("{0},{1}", buffer1[j].SequenceId, buffer1[j].Value));
                    }

                    sb.AppendLine("---");
                }

                Console.WriteLine(sb.ToString());
            }
            ).Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Hardware.KickLED();
            //Hardware.KickLED();
            //Hardware.HidIsConnected();
            sw.Stop();
            double elapsed1 = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine("Elapsed ms: " + elapsed1.ToString("0.00"));
        }

        private void FastUpdateBtn_ButtonPressed(object sender, LightedButton2.LightedButton2.ButtonPressedArgs e)
        {
            Hardware.SetSampleRate(SampleRate.Fast);
            ResetStats();
            AcqTimer.Interval = 50;
        }

        private void SlowUpdateBtn_ButtonPressed(object sender, LightedButton2.LightedButton2.ButtonPressedArgs e)
        {
            Hardware.SetSampleRate(SampleRate.Slow);
            ResetStats();
            AcqTimer.Interval = 410;
        }
    }
}
