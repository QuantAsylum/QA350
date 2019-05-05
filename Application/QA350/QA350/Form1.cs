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
        /// Last reading. This has gain and offset adjustments applied.
        /// </summary>
        double LastReading;

        /// <summary>
        /// Offset user may apply to a reading
        /// </summary>
        double UserOffset;

        /// <summary>
        /// Used for entering factory mode
        /// </summary>
        int FactoryModeKeyIndex = 0;

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
        enum SampleRateEnum { Fast_1KHz, Slow_2p5Hz}
        SampleRateEnum SampleRate = SampleRateEnum.Slow_2p5Hz;
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
        }

        private void Form1_Load(object sender, EventArgs e)
        {
#if !DEBUG
            flashVirginDeviceToolStripMenuItem.Visible = false;
#endif
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

            // Never want this to persist. Too confusing
            AppSettings.Math = 1.0;
            AppSettings.MathLabel = "";
            label12.Visible = false;

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
            LoggingLabel.Visible = false;

            //Label_2p5sps.Visible = true;
            //Label_1ksps.Visible = false;
            //SampleRate = SampleRateEnum.Slow_2p5Hz;
            DcBtn_Pressed(null, null);

            LoadFontData();

            InitGraphs();

            Text += "  (" + Constants.Version.ToString("0.000") + ")";

            // Not ready yet
            analysisToolStripMenuItem.Visible = false;

            LEDKickerTimer.Enabled = true;
            TryConnect();
        }

        /// <summary>
        /// Called when the application is closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (LoggingEnabled)
            {
                LoggingEnabled = false;
                Thread.Sleep(1000);
            }

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


        private double GetSampleRate()
        {
            switch (SampleRate)
            {
                case SampleRateEnum.Fast_1KHz:
                    return 1000;
                case SampleRateEnum.Slow_2p5Hz:
                    return 2.5;
                default:
                    throw new NotImplementedException("GetSampleRate in Form1.cs");
            }
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
                logToTextFileToolStripMenuItem.Enabled = true;
                logToBinaryFileToolStripMenuItem.Enabled = true;
                SetLowRange();

                SetSampleRate(SampleRateEnum.Slow_2p5Hz);
                SlowUpdateBtn.On = true;

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

        /// <summary>
        /// If any failure is detected at the application level, then this function 
        /// should be called. 
        /// </summary>
        private void ConnectionLost()
        {
            Hardware.IsConnected = false;
            AcqTimer.Enabled = false;
            label3.Text = "--CONNECTING--";
            toolStripStatusLabel1.Text = "Disconnected...please plug in QA350";
            logToTextFileToolStripMenuItem.Enabled = false;
            logToBinaryFileToolStripMenuItem.Enabled = false;
        }

        /// <summary>
        /// Sets +/-5V range
        /// </summary>
        private void SetLowRange()
        {
            IsLowRange = true;
            Hardware.SetAtten(0);
            ResetStats();
        }

        /// <summary>
        /// Sets +/- 50V range
        /// </summary>
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
            if (Hardware.IsConnected)
            {
                Hardware.KickLED();
            }
            else
            {
                TryConnect();
            }

            // If logging fails, it will be updated to the user here
            if (LoggingEnabled)
            {
                LoggingLabel.Visible = true;
                //logToTextFileToolStripMenuItem.Checked = true;
            }
            else
            {
                LoggingLabel.Visible = false;
                //logToTextFileToolStripMenuItem.Checked = false;
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

        /// <summary>
        /// Timer tick runs at either fast or slow rate to update collected data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AckTimer_Tick(object sender, EventArgs e)
        {
            // Check if we've been unplugged
            try
            {
                if (Hardware.IsConnected == false)
                {
                    ConnectionLost();
                    return;
                }

                // Read the raw counts
                if (Hardware.GetMode() == Mode.DC)
                {
                    RawDataCounts = Hardware.ReadVoltageCounts();
                }
                else if (Hardware.GetMode() == Mode.RMS)
                {
                    Console.WriteLine("Mode RMS. Reading counts.");
                    RawDataCounts = Hardware.ReadRmsCounts();
                    if (RawDataCounts != Hardware.INVALID_VALUE)
                    {
                        Console.WriteLine("Mode RMS. Starting new conversion");
                        Hardware.StartRmsConversion();
                    }
                    else
                    {
                        // Value isn't ready yet
                        Console.WriteLine("Mode RMS. Conversion not ready");
                        return;
                    }


                }
            }
            catch
            {
                ConnectionLost();
            }

            // At this point, we're connected and have pulled over the raw counts

            ProcessRawCounts();
        }

        private void ProcessRawCounts()
        {
            bool ovf = false;

            // Convert counts to voltage.  
            double v = ConvertCountsToVoltage(RawDataCounts, ref ovf);

            if (AppSettings.Math != 1)
            {
                label4.Visible = true;
                label12.Visible = true;
            }
            else
            {
                label4.Visible = false;
                label12.Visible = false;
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

            // Add the new point
            double period = 1 / GetSampleRate();
            GraphData.Add(DateTime.Now.Subtract(StartTime).TotalSeconds, v);

            // Remove the old points
            while (GraphData.Count > Readings.Count)
            {
                GraphData.RemoveAt(0);
            }

            GraphCollectedData();
        }

        private void GraphCollectedData()
        {
            if (Readings.Count > 0)
            {
                double avg = Readings.Average();

                double spanSec = 0;

                if (GraphData.Count > 1)
                {
                    spanSec = GraphData.Last().X - GraphData.First().X;
                }

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
                    string units;

                    if (AppSettings.MathLabel == "")
                        units = "V";
                    else
                        units = AppSettings.MathLabel;

                    zedGraphControl1.GraphPane.Title.Text = string.Format("{0}{1} per div\nMean={2}{3} Span={4:0}sec", GetYAxisPerDiv(avg).ToEngineeringNotation("0.000"), units,
                        avg.ToEngineeringNotation("0.000"), units, spanSec);
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

        /// <summary>
        /// Converts raw counts computed by the box and sent over via USB
        /// to actual voltages based on the calibration parameters. This will
        /// apply both gain and offset corrections, as well as input Z adjustments
        /// and any user-requested math adjustments
        /// </summary>
        /// <param name="counts"></param>
        /// <param name="ovf"></param>
        /// <returns></returns>
        private double ConvertCountsToVoltage(int counts, ref bool ovf)
        {
            double v;
            if (IsLowRange)
            {
                v = ComputeUncalibratedVoltage(counts - CalData.LowRangeCountsOffset);
                v = v * CalData.LowRangeGain;
                if (v > 5) ovf = true;
            }
            else
            {
                v = ComputeUncalibratedVoltage(counts - CalData.HiRangeCountsOffset);
                v = v * CalData.HiRangeGain;
                if (v > 50) ovf = true;
            }

            // Subtract the user offset (usually zero)
            v = v - UserOffset;

            // If we're at the fast sample rate, the equiv input impedance of the 
            // ADS buffer drops from 80M to 20M. To compensate, we need to gain up
            // the result. Note that top R of divider is 240K, and lower R is 
            // 260K. The 80M or 20M is paralleled with the 260K. The math is as follows
            // (260K || 80M) / ( (260K || 80M) + 240K)  = 0.479252
            // (260K || 20M) / ( (260K || 20M) + 240K)  = 0.477023
            // This is a ratio of 1.00467. However, emperically we determine that
            // 1.00988 gives better agreement. The correct solution here is to calibrate 
            // for both in a future update.
            if ( (Hardware.GetMode() == Mode.DC) && (SampleRate == SampleRateEnum.Fast_1KHz) )
            {
                v = v * 1.00988;
            }
            else if (Hardware.GetMode() == Mode.RMS)
            {
                v = v * 1.02315549;
            }

            v *= AppSettings.Math;

            return v;
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

        /// <summary>
        /// STarts calibration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Rel button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Allows user to update stats and data collection parameters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnStats_ButtonPressed(object sender, LightedButton2.LightedButton2.ButtonPressedArgs e)
        {
            DlgEditStats dlg = new DlgEditStats(AppSettings);

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                ResetStats();
                AppSettings = dlg.AppSettings;
                label12.Text = AppSettings.MathLabel;
            }
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        internal void FlashStatusUpdate(string status)
        {
            Invoke((System.Windows.Forms.MethodInvoker)delegate
            {
                toolStripStatusLabel1.Text = status;
                statusStrip1.Update();
            });
        }

        /// <summary>
        /// Called when user selects the menu item to reflash
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

            AcqTimer.Enabled = false;
            LEDKickerTimer.Enabled = false;
            Hardware.EnterBSL();
            Thread.Sleep(2000);
            Bootloader.EnterBootloader(FlashStatusUpdate);

            MessageBox.Show("Restart the application and replug the hardware");
            Close();
        }

        /// <summary>
        /// A virgin device is a device with no executable code present. It's fresh from
        /// the factory. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void flashVirginDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Bootloader.EnterBootloader(FlashStatusUpdate);
        }

        /// <summary>
        /// Called when user changes logging state in the menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loggingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool isBinary = false;

            ToolStripMenuItem mi = (ToolStripMenuItem)sender;
            ToolStripMenuItem otherMi = null;
            if (mi == logToBinaryFileToolStripMenuItem)
            {
                isBinary = true;
                otherMi = logToTextFileToolStripMenuItem;
            }
            else if (mi == logToTextFileToolStripMenuItem)
            {
                isBinary = false;
                otherMi = logToBinaryFileToolStripMenuItem;
            }
            else
            {
                throw new NotImplementedException("Unknown menu type in loggingToolStripMenuItem_Click");
            }

            if (mi.Checked == false)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.InitialDirectory = Constants.DataFilePath;
                sfd.Filter = "Log Files|*.log";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    mi.Checked = true;
                    otherMi.Enabled = false;
                    LoggingEnabled = true;
                    LogFile = sfd.FileName;

                    if (SlowUpdateBtn.On)
                    {
                        SampleRate = SampleRateEnum.Slow_2p5Hz;
                    }
                    else if (FastUpdateBtn.On)
                    {
                        SampleRate = SampleRateEnum.Fast_1KHz;
                    }

                    if (isBinary)
                        new Thread(LoggingThreadBinary).Start();
                    else
                        new Thread(LoggingThreadText).Start();

                    // While logging, the logging speed cannot be changed and the DC/RMS setting cannot be changed
                    DcModeBtn.Enabled = false;
                    RmsModeBtn.Enabled = false;
                    SlowUpdateBtn.Enabled = false;
                    FastUpdateBtn.Enabled = false;
                }
            }
            else
            {
                LoggingEnabled = false;
                LoggingDone();
            }
        }

        /// <summary>
        /// This thread is spun up when logging is turned on. If there's a problem during logging 
        /// of any kind (file access, bad communication with hardware, etc) then this thread will exit. 
        /// </summary>
        private void LoggingThreadBinary()
        {
            float dt;
            int sample = 0;
            byte LastSequence = 0;
            bool firstRead = true;

            Debug.WriteLine("Binary logging thread started");

            FileStream fs = null;
            BinaryWriter sw = null;

            try
            {
                dt = (float)(1.0 / GetSampleRate());
                
                fs = new FileStream(LogFile, FileMode.Create, FileAccess.Write);
                sw = new BinaryWriter(fs);

                // Barker
                sw.Write(0xCAFE8224);
                sw.Write(GetSampleRate());

                while (LoggingEnabled)
                {
                    StreamSample[] buffer = new StreamSample[0];
                    try
                    {
                        while ( (Hardware.GetFifoDepth() < 12) && (LoggingEnabled) )
                        {
                            Thread.Sleep(5);
                        }

                        if (LoggingEnabled)
                            buffer = Hardware.ReadVoltageStream();
                    }
                    catch
                    {
                        break;
                    }

                    // Sync the sequence ID on the first read
                    if (firstRead)
                    {
                        firstRead = false;

                        LastSequence = (byte)(buffer[0].SequenceId - (byte)1);
                    }

                    // If nothing read, then bail
                    if (buffer.Length == 0)
                    {
                        break;
                    }

                    // If we're missing a block of data, write null samples to indicate such. 
                    while ( (byte)(LastSequence + 1) != buffer[0].SequenceId)
                    {
                        //sw.Write(sample++ * dt);
                        sw.Write(float.NaN);
                        ++LastSequence;
                    }

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        bool ovf = false;
                        //sw.WriteLine("-,{0:0.000},{1:N6}", sample++ * dt, ConvertCountsToVoltage(buffer[i].Value, ref ovf));
                        //sw.Write(sample++ * dt);
                        sw.Write((float)ConvertCountsToVoltage(buffer[i].Value, ref ovf));
                        LastSequence = buffer[i].SequenceId;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Binary logging has stopped: " + ex.Message);
            }
        
            if (sw != null)
            {
                sw.Flush();
                sw.Close();
            }

            if (fs != null)
            {
                fs.Close();
            }

            LoggingEnabled = false;
            Debug.WriteLine("Binary logging thread exited. {0} samples written.", sample);
            LoggingDone();
        }

        private void LoggingThreadText()
        {
            float dt;
            int sample = 0;
            byte LastSequence = 0;
            bool firstRead = true;

            Debug.WriteLine("Text logging thread started");

            StreamWriter sw = null;

            try
            {
                dt = (float)(1.0 / GetSampleRate());

                sw = new StreamWriter(LogFile);

                // Barker
                sw.WriteLine("Logging file Created on " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
                sw.WriteLine("Sample Rate {0} sps", GetSampleRate());
                sw.WriteLine("Sample Time (seconds), Value");

                while (LoggingEnabled)
                {
                    StreamSample[] buffer = new StreamSample[0];
                    try
                    {
                        while ( (Hardware.GetFifoDepth() < 12) && (LoggingEnabled) )
                        {
                            Thread.Sleep(5);
                        }

                        if (LoggingEnabled)
                            buffer = Hardware.ReadVoltageStream();
                    }
                    catch
                    {
                        break;
                    }

                    // Sync the sequence ID on the first read
                    if (firstRead)
                    {
                        firstRead = false;

                        LastSequence = (byte)(buffer[0].SequenceId - (byte)1);
                    }

                    // If nothing read, then bail
                    if (buffer.Length == 0)
                    {
                        break;
                    }

                    // If we're missing a block of data, write null samples to indicate such. 
                    while ((byte)(LastSequence + 1) != buffer[0].SequenceId)
                    {
                        //sw.Write(sample++ * dt);
                        sw.WriteLine("{0}, {1}", sample++ * dt, "MISSED DATA");
                        ++LastSequence;
                    }

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        bool ovf = false;
                        sw.WriteLine("{0:0.000},{1:N6}", sample++ * dt, ConvertCountsToVoltage(buffer[i].Value, ref ovf));
                        //sw.Write(sample++ * dt);
                        //tw.Write((float)ConvertCountsToVoltage(buffer[i].Value, ref ovf));
                        LastSequence = buffer[i].SequenceId;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Text logging has stopped: " + ex.Message);
            }

            if (sw != null)
            {
                sw.Flush();
                sw.Close();
            }

            LoggingEnabled = false;
            Debug.WriteLine("Text logging thread exited. {0} samples written.", sample);
            LoggingDone();
        }

        private void LoggingDone()
        {
            Invoke((MethodInvoker)delegate 
            {
                DcModeBtn.Enabled = true;
                RmsModeBtn.Enabled = true;
                SlowUpdateBtn.Enabled = true;
                FastUpdateBtn.Enabled = true;
                logToBinaryFileToolStripMenuItem.Enabled = true;
                logToBinaryFileToolStripMenuItem.Checked = false;
                logToTextFileToolStripMenuItem.Enabled = true;
                logToTextFileToolStripMenuItem.Checked = false;
            });
        }

        /// <summary>
        /// Called whenever sample rate is changed. You must also 
        /// change the UI state to ensure it matches the state set
        /// here
        /// </summary>
        /// <param name="newSampleRate"></param>
        private void SetSampleRate(SampleRateEnum newSampleRate)
        {
            SampleRate = newSampleRate;

            switch (newSampleRate)
            {
                case SampleRateEnum.Fast_1KHz:
                    AcqTimer.Interval = 50;
                    Hardware.SetSampleRate(QA350.SampleRate.Fast);
                    Label_2p5sps.Visible = false;
                    Label_1ksps.Visible = true;
                    break;
                case SampleRateEnum.Slow_2p5Hz:
                    AcqTimer.Interval = 405;
                    Hardware.SetSampleRate(QA350.SampleRate.Slow);
                    Label_2p5sps.Visible = true;
                    Label_1ksps.Visible = false;
                    break;
                default:
                    break;
            }

            ResetStats();
        }

        /// <summary>
        /// Called when user selects fast sample rate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FastUpdateBtn_ButtonPressed(object sender, LightedButton2.LightedButton2.ButtonPressedArgs e)
        {
            if (FastUpdateBtn.On)
            {
                SetSampleRate(SampleRateEnum.Fast_1KHz);
            }
        }

        /// <summary>
        /// Called when user selects slow sample rate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SlowUpdateBtn_ButtonPressed(object sender, LightedButton2.LightedButton2.ButtonPressedArgs e)
        {
            if (SlowUpdateBtn.On)
            {
                SetSampleRate(SampleRateEnum.Slow_2p5Hz);
            }
        }

        private void analyzeLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DlgAnalyze dlg = new DlgAnalyze();

            if (dlg.ShowDialog() == DialogResult.OK)
            {

            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
           
        }

        // RMS Button
        private void RmsButton_Pressed(object sender, LightedButton2.LightedButton2.ButtonPressedArgs e)
        {
            ResetStats();
            Hardware.SetMode(Mode.RMS);
            Hardware.StartRmsConversion();
            RmsLabel.Visible = true;
            Label_2p5sps.Visible = false;
            Label_1ksps.Visible = false;
            FastUpdateBtn.Enabled = false;
            SlowUpdateBtn.Enabled = false;
        }

        // DC button
        private void DcBtn_Pressed(object sender, LightedButton2.LightedButton2.ButtonPressedArgs e)
        {
            ResetStats();
            Hardware.SetMode(Mode.DC);
            RmsLabel.Visible = false;
            SetSampleRate(SampleRateEnum.Slow_2p5Hz);
            //FastUpdateBtn.Enabled = true;
            //SlowUpdateBtn.Enabled = true;
            SlowUpdateBtn.On = true;
            //Label_1ksps.Visible = true;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            string factoryModeString = "QA350";

            if (Convert.ToChar(e.KeyCode) == factoryModeString[FactoryModeKeyIndex])
            {
                ++FactoryModeKeyIndex;

                if (FactoryModeKeyIndex == factoryModeString.Length)
                {
                    // Enable certain menu items
                    FactoryModeKeyIndex = 0;
                    flashVirginDeviceToolStripMenuItem.Visible = true;
                    Console.Beep();
                }
            }
            else
            {
                FactoryModeKeyIndex = 0;
            }

        }
    }
}
