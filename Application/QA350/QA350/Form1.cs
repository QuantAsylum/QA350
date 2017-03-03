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
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph;



namespace QA350
{
    public partial class Form1 : Form
    {
        Form This;


        static internal Hardware HW;

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

        //class AttenSetting
        //{
        //    public double Atten;
        //    public double GainAdj = 1.0;
        //}
        //List<AttenSetting> AttenList = new List<AttenSetting>();
        //int AttenIndex = 0;

        /// <summary>
        /// Tracks history of readings. This is cleared automatically on gain range changes
        /// </summary>
        List<double> Readings = new List<double>();

        public Form1()
        {
            This = this;
            InitializeComponent();
            HW = new Hardware();

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

            //AttenList.Add(new AttenSetting { Atten = 1.581 });
            //AttenList.Add(new AttenSetting { Atten = 20.523 });
            //AttenList.Add(new AttenSetting { Atten = 0.009129 });
            //AttenList.Add(new AttenSetting { Atten = 0.00133 });
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (HW.Open())
            {
                toolStripStatusLabel1.Text = "Opened";
                //tools   .Text = "Opened";
                
                SetLowRange();

                LineItem line = new LineItem("", GraphData, Color.LimeGreen, SymbolType.None);
                zedGraphControl1.GraphPane.CurveList.Add(line);
            }
            else
            {
                toolStripStatusLabel1.Text = Text = "Open failed";
            }

            if (Directory.Exists(Constants.DataFilePath) == false)
                Directory.CreateDirectory(Constants.DataFilePath);

            // Check if cal data exists
            if (File.Exists(Constants.CalibrationFile))
            {
                //CalData = CalibrationClass.Deserialize(Constants.CalibrationFile);
                CalData = (CalibrationClass)SerDes.Deserialize(typeof(CalibrationClass), File.ReadAllText(Constants.CalibrationFile));
            }
            else
            {
                MessageBox.Show("A calibration file could not be loaded. A default file will be created");
                CalData = new CalibrationClass();
            }

            if (CalData.IsDefault)
            {
                label4.Text = "UNCALIBRATED";
            }
            else
            {
                label4.Text = "";
            }

            byte[] fontData = QA350.Resource1.advanced_pixel_lcd_7;
            IntPtr fontPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(fontData.Length);
            System.Runtime.InteropServices.Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
            uint dummy = 0;
            fonts.AddMemoryFont(fontPtr, QA350.Resource1.advanced_pixel_lcd_7.Length);
            AddFontMemResourceEx(fontPtr, (uint)QA350.Resource1.advanced_pixel_lcd_7.Length, IntPtr.Zero, ref dummy);
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(fontPtr);
            LCDFontBig = new Font(fonts.Families[0], 24.0F);
            LCDFontSmall = new Font(fonts.Families[0], 12);
            label3.Font = LCDFontBig;
            label10.Font = LCDFontSmall;
            label11.Font = LCDFontSmall;
            label7.Font = LCDFontSmall;
            label9.Font = LCDFontSmall;

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

       

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                //File.WriteAllText(Constants.CalibrationFile, CalData.Serialize());
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

            HW.Close();
        }

        private void SetLowRange()
        {
            IsLowRange = true;
            HW.SetAtten(0);
            ResetStats();
        }

        private void SetHighRange()
        {
            IsLowRange = false;
            HW.SetAtten(1);
            ResetStats();
        }

        //void Msp430_Removed()
        //{
        //    throw new NotImplementedException();
        //}

        //void Msp430_Inserted()
        //{
        //    Msp430.ReadReport(OnReport);
        //}

        //void OnReport(HidReport report)
        //{
        //    if (Msp430.IsConnected == false) { return; }

        //    //var data = new USBData(report.Data);
        //    //string text = System.Text.ASCIIEncoding.ASCII.GetString(data.Data);
        //    //Debug.WriteLine(text);
        //    //label1.Text = "Rec'd: " + text;

        //    //Msp430.ReadReport(OnReport);
        //}

        private void button1_Click(object sender, EventArgs e)
        {
            //if ((Msp430 != null) && (Msp430.IsConnected))
            //{
            //    Stopwatch sw = Stopwatch.StartNew();

            //    if (USBSendData(new byte[] { 0xAA }))
            //    {
            //        byte[] buffer;

            //        if (USBRecvData(out buffer, 32))
            //        {
            //            Int32 data = (buffer[1] << 8) + buffer[0];
            //            Debug.WriteLine("ReadVal:" + data.ToString());
            //        }

            //        sw.Stop();
            //        Debug.WriteLine("Elapsed " + sw.ElapsedMilliseconds.ToString());
            //        label1.Text = "Write OK";
            //        return;
            //    }
            //}

            //label1.Text = "Write failed";
        }

        //private void button3_Click(object sender, EventArgs e)
        //{
        //    //if ((Msp430 != null) && (Msp430.IsConnected))
        //    //{
        //    //    Stopwatch sw = Stopwatch.StartNew();

        //    //    if (USBSendData(new byte[] { 0x01, 0x00 }))
        //    //    {
        //    //        byte[] buffer;

        //    //        if (USBRecvData(out buffer, 32))
        //    //        {
        //    //            Int32 data = (buffer[2] << 16) + (buffer[1] << 8) + buffer[0];
        //    //            Debug.WriteLine("ReadVal:" + data.ToString());
        //    //        }

        //    //        sw.Stop();
        //    //        Debug.WriteLine("Elapsed " + sw.ElapsedMilliseconds.ToString());
        //    //        label1.Text = "Write OK";
        //    //        return;
        //    //    }
        //    //}

        //    //label1.Text = "Write failed";
        //}

        //private void button2_Click(object sender, EventArgs e)
        //{
        //    //if ((Msp430 != null) && (Msp430.IsConnected))
        //    //{
        //    //    USBSendData(new byte[] { 0x00, 0x00 });
        //    //}
        //}

        //private void SetPGA(int pga)
        //{
        //    //if ((Msp430 != null) && (Msp430.IsConnected))
        //    //{
        //    //    USBSendData(new byte[] { 0x02, (byte)pga });
        //    //}
        //}

        //private void SetAtten(int atten)
        //{
        //    if ((Msp430 != null) && (Msp430.IsConnected))
        //    {
        //        USBSendData(new byte[] { 0x03, (byte)atten });
        //    }
        //}

        // Set PGA = 1 command
        //private void button4_Click(object sender, EventArgs e)
        //{
        //    // SetPGA(0);
        //    //USBSendData(new byte[] { 0x00, 0x01 });
        //}

        //private void button5_Click(object sender, EventArgs e)
        //{
        //    //SetPGA(1);
        //    //USBSendData(new byte[] { 0x00, 0x02 });
        //}

        // Set PGA = 7
        //private void button6_Click(object sender, EventArgs e)
        //{
        //    //SetPGA(7);
        //}

        /// <summary>
        /// LED timer kick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            //if ((Msp430 != null) && (Msp430.IsConnected))
            //{
            //   USBSendData(new byte[] { 0x00, 0x00 });
            //}
            HW.KickLED();
        }

        /// <summary>
        /// Computes the unscaled voltage at the inputs pins. This does not take into 
        /// account front end attenuators. This function merely computes the voltage given
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


        Random r = new Random();
        int totalReads;
        private void AckTimer_Tick(object sender, EventArgs e)
        {
            bool ovf = false;
            // Read the raw counts
            RawDataCounts = HW.ReadVoltageCounts();

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

            // Save the last scaled and zero'd reading
            LastReading = v;

            // Don't grow the queue too long
            while (Readings.Count > AppSettings.SampleHistory)
                Readings.RemoveAt(0);

            label10.Text = Readings.Count.ToString();
            label11.Text = AppSettings.SampleHistory.ToString();

            //Debug.WriteLine(string.Format("Counts: 0x{0} [{1}]   Calibrated Volts: {2}", RawDataCounts.ToString("X"), RawDataCounts.ToString(), v.ToEngineeringNotation()));


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

                Histogram h = new Histogram(Readings, GetBinSize(), AppSettings.BinCount);
                h.Plot(zedGraphControl2);
            }
        }

        // Set user offset
        private void button7_Click(object sender, EventArgs e)
        {
            UserOffset = LastReading;
        }

        // Clear user offset
        private void button8_Click(object sender, EventArgs e)
        {
            UserOffset = 0;
            ResetStats();
        }

        // Calibrate 0
        private void button15_Click(object sender, EventArgs e)
        {
            //double lastRaw;

            if (IsLowRange)
            {
                //lastRaw = LastRawReading - Settings.LowRangeOffset;
                CalData.LowRangeCountsOffset = RawDataCounts;
            }
            else
            {
                CalData.HiRangeCountsOffset = RawDataCounts;
            }
        }

        // Cal to 2.048
        private void button12_Click(object sender, EventArgs e)
        {
            if (IsLowRange)
            {
                CalData.LowRangeGain = 2.048 / LastReading * CalData.LowRangeGain;
            }
            else
            {
                CalData.HiRangeGain = 2.048 / LastReading * CalData.HiRangeGain;
            }
        }

        // Cal to 2.5
        private void button10_Click(object sender, EventArgs e)
        {
            //double lastraw = LastRawReading - ZeroVal;
            ////double lastraw = LastRawReading;
            //AttenList[AttenIndex].GainAdj = 2.5 / lastraw;
        }

        // Cal to 10V
        private void button13_Click(object sender, EventArgs e)
        {
            if (IsLowRange)
            {
                CalData.LowRangeGain = 10 / LastReading * CalData.LowRangeGain;
            }
            else
            {
                CalData.HiRangeGain = 10 / LastReading * CalData.HiRangeGain;
            }
        }




        //private void button9_Click(object sender, EventArgs e)
        //{
        //    GraphData.Clear();
        //}

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            //if (radioButton1.Checked) { SetLowRange(); }
            //else if (radioButton2.Checked) { SetHighRange(); }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            label3.Font = LCDFontBig;
        }

        // Reset stats button
        private void lightedButton22_ButtonPressed(object sender, LightedButton2.LightedButton2.ButtonPressedArgs e)
        {
            ResetStats();
        }

        //private void lightedButton24_ButtonPressed(object sender, LightedButton2.LightedButton2.ButtonPressedArgs e)
        //{
        //    if (lightedButton21.On)
        //        AcqTimer.Enabled = true;
        //    else
        //        AcqTimer.Enabled = false;

        //}

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
            //DlgCalibrate dlg = new DlgCalibrate();

           // ack
            AcqTimer.Enabled = false;


            DlgCalibrate2 dlg = new DlgCalibrate2();

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CalData = dlg.calData;

                if (CalData.IsDefault == false)
                    label4.Text = "";

                if (lightedButton28.On) { SetHighRange(); }
                else if (lightedButton29.On) { SetLowRange(); }
            }

            AcqTimer.Enabled = true;
        }

        private void lightedButton211_ButtonPressed(object sender, LightedButton2.LightedButton2.ButtonPressedArgs e)
        {

        }

        private void lightedButton210_ButtonPressed(object sender, LightedButton2.LightedButton2.ButtonPressedArgs e)
        {

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

        //private void lightedButton21_ButtonPressed(object sender, LightedButton2.LightedButton2.ButtonPressedArgs e)
        //{

        //}

        //private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        //{

        //}

       






        //private void radioButton2_CheckedChanged(object sender, EventArgs e)
        //{

        //}
















    }
}
