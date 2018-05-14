namespace QA350
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.LEDKickerTimer = new System.Windows.Forms.Timer(this.components);
            this.AcqTimer = new System.Windows.Forms.Timer(this.components);
            this.label3 = new System.Windows.Forms.Label();
            this.zedGraphControl1 = new ZedGraph.ZedGraphControl();
            this.zedGraphControl2 = new ZedGraph.ZedGraphControl();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loggingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.calibrateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reflashToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.flashVirginDeviceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.RelModeLabel = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.UncalLabel = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.lightedButton210 = new LightedButton2.LightedButton2();
            this.SetRelBtn = new LightedButton2.LightedButton2();
            this.lightedButton28 = new LightedButton2.LightedButton2();
            this.lightedButton29 = new LightedButton2.LightedButton2();
            this.lightedButton27 = new LightedButton2.LightedButton2();
            this.FastUpdateBtn = new LightedButton2.LightedButton2();
            this.SlowUpdateBtn = new LightedButton2.LightedButton2();
            this.lightedButton22 = new LightedButton2.LightedButton2();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // LEDKickerTimer
            // 
            this.LEDKickerTimer.Enabled = true;
            this.LEDKickerTimer.Interval = 800;
            this.LEDKickerTimer.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // AcqTimer
            // 
            this.AcqTimer.Interval = 410;
            this.AcqTimer.Tick += new System.EventHandler(this.AckTimer_Tick);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Font = new System.Drawing.Font("Courier New", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(43, 61);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(429, 54);
            this.label3.TabIndex = 10;
            this.label3.Text = "--CONNECTING--";
            // 
            // zedGraphControl1
            // 
            this.zedGraphControl1.BackColor = System.Drawing.Color.Black;
            this.zedGraphControl1.Location = new System.Drawing.Point(40, 220);
            this.zedGraphControl1.Name = "zedGraphControl1";
            this.zedGraphControl1.ScrollGrace = 0D;
            this.zedGraphControl1.ScrollMaxX = 0D;
            this.zedGraphControl1.ScrollMaxY = 0D;
            this.zedGraphControl1.ScrollMaxY2 = 0D;
            this.zedGraphControl1.ScrollMinX = 0D;
            this.zedGraphControl1.ScrollMinY = 0D;
            this.zedGraphControl1.ScrollMinY2 = 0D;
            this.zedGraphControl1.Size = new System.Drawing.Size(275, 197);
            this.zedGraphControl1.TabIndex = 11;
            // 
            // zedGraphControl2
            // 
            this.zedGraphControl2.Location = new System.Drawing.Point(405, 220);
            this.zedGraphControl2.Name = "zedGraphControl2";
            this.zedGraphControl2.ScrollGrace = 0D;
            this.zedGraphControl2.ScrollMaxX = 0D;
            this.zedGraphControl2.ScrollMaxY = 0D;
            this.zedGraphControl2.ScrollMaxY2 = 0D;
            this.zedGraphControl2.ScrollMinX = 0D;
            this.zedGraphControl2.ScrollMinY = 0D;
            this.zedGraphControl2.ScrollMinY2 = 0D;
            this.zedGraphControl2.Size = new System.Drawing.Size(274, 197);
            this.zedGraphControl2.TabIndex = 17;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.BackColor = System.Drawing.Color.Transparent;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(76, 119);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(44, 20);
            this.label6.TabIndex = 23;
            this.label6.Text = "Avg:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.BackColor = System.Drawing.Color.Transparent;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(126, 114);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(67, 20);
            this.label7.TabIndex = 24;
            this.label7.Text = "2.5 SPS";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.BackColor = System.Drawing.Color.Transparent;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(48, 151);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(73, 20);
            this.label8.TabIndex = 25;
            this.label8.Text = "StdDev:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.BackColor = System.Drawing.Color.Transparent;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(127, 144);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(67, 20);
            this.label9.TabIndex = 26;
            this.label9.Text = "2.5 SPS";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.calibrateToolStripMenuItem,
            this.toolsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(847, 24);
            this.menuStrip1.TabIndex = 36;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loggingToolStripMenuItem,
            this.toolStripMenuItem1,
            this.closeToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // loggingToolStripMenuItem
            // 
            this.loggingToolStripMenuItem.Name = "loggingToolStripMenuItem";
            this.loggingToolStripMenuItem.Size = new System.Drawing.Size(118, 22);
            this.loggingToolStripMenuItem.Text = "Logging";
            this.loggingToolStripMenuItem.Click += new System.EventHandler(this.loggingToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(115, 6);
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            this.closeToolStripMenuItem.Size = new System.Drawing.Size(118, 22);
            this.closeToolStripMenuItem.Text = "Close";
            this.closeToolStripMenuItem.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);
            // 
            // calibrateToolStripMenuItem
            // 
            this.calibrateToolStripMenuItem.Name = "calibrateToolStripMenuItem";
            this.calibrateToolStripMenuItem.Size = new System.Drawing.Size(66, 20);
            this.calibrateToolStripMenuItem.Text = "Calibrate";
            this.calibrateToolStripMenuItem.Click += new System.EventHandler(this.calibrateToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.reflashToolStripMenuItem,
            this.flashVirginDeviceToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(47, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // reflashToolStripMenuItem
            // 
            this.reflashToolStripMenuItem.Name = "reflashToolStripMenuItem";
            this.reflashToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.reflashToolStripMenuItem.Text = "Update QA350 Flash";
            this.reflashToolStripMenuItem.Click += new System.EventHandler(this.reflashToolStripMenuItem_Click);
            // 
            // flashVirginDeviceToolStripMenuItem
            // 
            this.flashVirginDeviceToolStripMenuItem.Name = "flashVirginDeviceToolStripMenuItem";
            this.flashVirginDeviceToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.flashVirginDeviceToolStripMenuItem.Text = "Flash Virgin Device";
            this.flashVirginDeviceToolStripMenuItem.Click += new System.EventHandler(this.flashVirginDeviceToolStripMenuItem_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 469);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(847, 22);
            this.statusStrip1.TabIndex = 37;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(118, 17);
            this.toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // panel1
            // 
            this.panel1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel1.BackgroundImage")));
            this.panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.panel1.Controls.Add(this.button2);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.RelModeLabel);
            this.panel1.Controls.Add(this.label11);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.UncalLabel);
            this.panel1.Controls.Add(this.label10);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.label9);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.lightedButton210);
            this.panel1.Controls.Add(this.SetRelBtn);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.lightedButton28);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.lightedButton29);
            this.panel1.Controls.Add(this.label8);
            this.panel1.Controls.Add(this.lightedButton27);
            this.panel1.Controls.Add(this.zedGraphControl2);
            this.panel1.Controls.Add(this.FastUpdateBtn);
            this.panel1.Controls.Add(this.zedGraphControl1);
            this.panel1.Controls.Add(this.SlowUpdateBtn);
            this.panel1.Controls.Add(this.lightedButton22);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 24);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(847, 445);
            this.panel1.TabIndex = 39;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(405, 140);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 47;
            this.button2.Text = "Kick";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(479, 114);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 46;
            this.button1.Text = "Stream";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // RelModeLabel
            // 
            this.RelModeLabel.AutoSize = true;
            this.RelModeLabel.BackColor = System.Drawing.Color.Transparent;
            this.RelModeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RelModeLabel.Location = new System.Drawing.Point(556, 81);
            this.RelModeLabel.Name = "RelModeLabel";
            this.RelModeLabel.Size = new System.Drawing.Size(88, 20);
            this.RelModeLabel.TabIndex = 45;
            this.RelModeLabel.Text = "RELATIVE";
            // 
            // label11
            // 
            this.label11.BackColor = System.Drawing.Color.Transparent;
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(584, 145);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(95, 30);
            this.label11.TabIndex = 44;
            this.label11.Text = "1";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.Color.Transparent;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(556, 151);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(31, 20);
            this.label5.TabIndex = 43;
            this.label5.Text = "OF";
            // 
            // UncalLabel
            // 
            this.UncalLabel.AutoSize = true;
            this.UncalLabel.BackColor = System.Drawing.Color.Transparent;
            this.UncalLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UncalLabel.Location = new System.Drawing.Point(531, 61);
            this.UncalLabel.Name = "UncalLabel";
            this.UncalLabel.Size = new System.Drawing.Size(134, 20);
            this.UncalLabel.TabIndex = 42;
            this.UncalLabel.Text = "UNCALIBRATED";
            // 
            // label10
            // 
            this.label10.BackColor = System.Drawing.Color.Transparent;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.label10.Location = new System.Drawing.Point(442, 145);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(118, 30);
            this.label10.TabIndex = 41;
            this.label10.Text = "1";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(391, 191);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(79, 19);
            this.label2.TabIndex = 40;
            this.label2.Text = "Histogram";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(30, 191);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(144, 19);
            this.label1.TabIndex = 39;
            this.label1.Text = "Voltage versus Time";
            // 
            // lightedButton210
            // 
            this.lightedButton210.AllowAllOff = true;
            this.lightedButton210.AllowFadeToOff = false;
            this.lightedButton210.BackColor = System.Drawing.Color.Transparent;
            this.lightedButton210.Enabled = false;
            this.lightedButton210.FillColor = System.Drawing.Color.Transparent;
            this.lightedButton210.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lightedButton210.GroupName = null;
            this.lightedButton210.LineThick = 1F;
            this.lightedButton210.Location = new System.Drawing.Point(784, 191);
            this.lightedButton210.Name = "lightedButton210";
            this.lightedButton210.OffColor = System.Drawing.Color.DarkGreen;
            this.lightedButton210.On = false;
            this.lightedButton210.OnColor = System.Drawing.Color.LimeGreen;
            this.lightedButton210.OneShot = true;
            this.lightedButton210.OneShotInterval = 100;
            this.lightedButton210.OptionMenuIndicator = false;
            this.lightedButton210.Size = new System.Drawing.Size(50, 30);
            this.lightedButton210.TabIndex = 35;
            this.lightedButton210.TextWhenOff = "Edit";
            this.lightedButton210.TextWhenOn = "Edit";
            this.lightedButton210.ButtonPressed += new System.EventHandler<LightedButton2.LightedButton2.ButtonPressedArgs>(this.lightedButton210_ButtonPressed);
            // 
            // SetRelBtn
            // 
            this.SetRelBtn.AllowAllOff = true;
            this.SetRelBtn.AllowFadeToOff = false;
            this.SetRelBtn.BackColor = System.Drawing.Color.Transparent;
            this.SetRelBtn.FillColor = System.Drawing.Color.Transparent;
            this.SetRelBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SetRelBtn.GroupName = null;
            this.SetRelBtn.LineThick = 1F;
            this.SetRelBtn.Location = new System.Drawing.Point(725, 191);
            this.SetRelBtn.Name = "SetRelBtn";
            this.SetRelBtn.OffColor = System.Drawing.Color.DarkGreen;
            this.SetRelBtn.On = false;
            this.SetRelBtn.OnColor = System.Drawing.Color.LimeGreen;
            this.SetRelBtn.OneShot = false;
            this.SetRelBtn.OneShotInterval = 100;
            this.SetRelBtn.OptionMenuIndicator = false;
            this.SetRelBtn.Size = new System.Drawing.Size(50, 30);
            this.SetRelBtn.TabIndex = 34;
            this.SetRelBtn.TextWhenOff = "Set";
            this.SetRelBtn.TextWhenOn = "Set";
            this.SetRelBtn.ButtonPressed += new System.EventHandler<LightedButton2.LightedButton2.ButtonPressedArgs>(this.lightedButton211_ButtonPressed);
            // 
            // lightedButton28
            // 
            this.lightedButton28.AllowAllOff = false;
            this.lightedButton28.AllowFadeToOff = false;
            this.lightedButton28.BackColor = System.Drawing.Color.Transparent;
            this.lightedButton28.FillColor = System.Drawing.Color.Transparent;
            this.lightedButton28.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lightedButton28.GroupName = "Range";
            this.lightedButton28.LineThick = 1F;
            this.lightedButton28.Location = new System.Drawing.Point(725, 35);
            this.lightedButton28.Name = "lightedButton28";
            this.lightedButton28.OffColor = System.Drawing.Color.DarkGreen;
            this.lightedButton28.On = false;
            this.lightedButton28.OnColor = System.Drawing.Color.LimeGreen;
            this.lightedButton28.OneShot = false;
            this.lightedButton28.OneShotInterval = 100;
            this.lightedButton28.OptionMenuIndicator = false;
            this.lightedButton28.Size = new System.Drawing.Size(50, 30);
            this.lightedButton28.TabIndex = 33;
            this.lightedButton28.TextWhenOff = "±50V";
            this.lightedButton28.TextWhenOn = "±50V";
            this.lightedButton28.ButtonPressed += new System.EventHandler<LightedButton2.LightedButton2.ButtonPressedArgs>(this.lightedButton28_ButtonPressed);
            // 
            // lightedButton29
            // 
            this.lightedButton29.AllowAllOff = true;
            this.lightedButton29.AllowFadeToOff = false;
            this.lightedButton29.BackColor = System.Drawing.Color.Transparent;
            this.lightedButton29.FillColor = System.Drawing.Color.Transparent;
            this.lightedButton29.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lightedButton29.GroupName = "Range";
            this.lightedButton29.LineThick = 1F;
            this.lightedButton29.Location = new System.Drawing.Point(784, 35);
            this.lightedButton29.Name = "lightedButton29";
            this.lightedButton29.OffColor = System.Drawing.Color.DarkGreen;
            this.lightedButton29.On = true;
            this.lightedButton29.OnColor = System.Drawing.Color.LimeGreen;
            this.lightedButton29.OneShot = false;
            this.lightedButton29.OneShotInterval = 100;
            this.lightedButton29.OptionMenuIndicator = false;
            this.lightedButton29.Size = new System.Drawing.Size(50, 30);
            this.lightedButton29.TabIndex = 32;
            this.lightedButton29.TextWhenOff = "±5V";
            this.lightedButton29.TextWhenOn = "±5V";
            this.lightedButton29.ButtonPressed += new System.EventHandler<LightedButton2.LightedButton2.ButtonPressedArgs>(this.lightedButton28_ButtonPressed);
            // 
            // lightedButton27
            // 
            this.lightedButton27.AllowAllOff = true;
            this.lightedButton27.AllowFadeToOff = false;
            this.lightedButton27.BackColor = System.Drawing.Color.Transparent;
            this.lightedButton27.FillColor = System.Drawing.Color.Transparent;
            this.lightedButton27.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lightedButton27.GroupName = null;
            this.lightedButton27.LineThick = 1F;
            this.lightedButton27.Location = new System.Drawing.Point(784, 111);
            this.lightedButton27.Name = "lightedButton27";
            this.lightedButton27.OffColor = System.Drawing.Color.DarkGreen;
            this.lightedButton27.On = false;
            this.lightedButton27.OnColor = System.Drawing.Color.LimeGreen;
            this.lightedButton27.OneShot = true;
            this.lightedButton27.OneShotInterval = 100;
            this.lightedButton27.OptionMenuIndicator = false;
            this.lightedButton27.Size = new System.Drawing.Size(50, 30);
            this.lightedButton27.TabIndex = 31;
            this.lightedButton27.TextWhenOff = "Edit";
            this.lightedButton27.TextWhenOn = "Edit";
            this.lightedButton27.ButtonPressed += new System.EventHandler<LightedButton2.LightedButton2.ButtonPressedArgs>(this.BtnStats_ButtonPressed);
            // 
            // FastUpdateBtn
            // 
            this.FastUpdateBtn.AllowAllOff = false;
            this.FastUpdateBtn.AllowFadeToOff = false;
            this.FastUpdateBtn.BackColor = System.Drawing.Color.Transparent;
            this.FastUpdateBtn.FillColor = System.Drawing.Color.Transparent;
            this.FastUpdateBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FastUpdateBtn.GroupName = "UpdateRate";
            this.FastUpdateBtn.LineThick = 1F;
            this.FastUpdateBtn.Location = new System.Drawing.Point(725, 266);
            this.FastUpdateBtn.Name = "FastUpdateBtn";
            this.FastUpdateBtn.OffColor = System.Drawing.Color.DarkGreen;
            this.FastUpdateBtn.On = false;
            this.FastUpdateBtn.OnColor = System.Drawing.Color.LimeGreen;
            this.FastUpdateBtn.OneShot = false;
            this.FastUpdateBtn.OneShotInterval = 100;
            this.FastUpdateBtn.OptionMenuIndicator = false;
            this.FastUpdateBtn.Size = new System.Drawing.Size(50, 30);
            this.FastUpdateBtn.TabIndex = 30;
            this.FastUpdateBtn.TextWhenOff = "Fast";
            this.FastUpdateBtn.TextWhenOn = "Fast";
            this.FastUpdateBtn.ButtonPressed += new System.EventHandler<LightedButton2.LightedButton2.ButtonPressedArgs>(this.FastUpdateBtn_ButtonPressed);
            // 
            // SlowUpdateBtn
            // 
            this.SlowUpdateBtn.AllowAllOff = false;
            this.SlowUpdateBtn.AllowFadeToOff = false;
            this.SlowUpdateBtn.BackColor = System.Drawing.Color.Transparent;
            this.SlowUpdateBtn.FillColor = System.Drawing.Color.Transparent;
            this.SlowUpdateBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SlowUpdateBtn.GroupName = "UpdateRate";
            this.SlowUpdateBtn.LineThick = 1F;
            this.SlowUpdateBtn.Location = new System.Drawing.Point(784, 266);
            this.SlowUpdateBtn.Name = "SlowUpdateBtn";
            this.SlowUpdateBtn.OffColor = System.Drawing.Color.DarkGreen;
            this.SlowUpdateBtn.On = true;
            this.SlowUpdateBtn.OnColor = System.Drawing.Color.LimeGreen;
            this.SlowUpdateBtn.OneShot = false;
            this.SlowUpdateBtn.OneShotInterval = 100;
            this.SlowUpdateBtn.OptionMenuIndicator = false;
            this.SlowUpdateBtn.Size = new System.Drawing.Size(50, 30);
            this.SlowUpdateBtn.TabIndex = 29;
            this.SlowUpdateBtn.TextWhenOff = "Slow";
            this.SlowUpdateBtn.TextWhenOn = "Slow";
            this.SlowUpdateBtn.ButtonPressed += new System.EventHandler<LightedButton2.LightedButton2.ButtonPressedArgs>(this.SlowUpdateBtn_ButtonPressed);
            // 
            // lightedButton22
            // 
            this.lightedButton22.AllowAllOff = true;
            this.lightedButton22.AllowFadeToOff = false;
            this.lightedButton22.BackColor = System.Drawing.Color.Transparent;
            this.lightedButton22.FillColor = System.Drawing.Color.Transparent;
            this.lightedButton22.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lightedButton22.GroupName = null;
            this.lightedButton22.LineThick = 1F;
            this.lightedButton22.Location = new System.Drawing.Point(725, 111);
            this.lightedButton22.Name = "lightedButton22";
            this.lightedButton22.OffColor = System.Drawing.Color.DarkGreen;
            this.lightedButton22.On = false;
            this.lightedButton22.OnColor = System.Drawing.Color.LimeGreen;
            this.lightedButton22.OneShot = true;
            this.lightedButton22.OneShotInterval = 100;
            this.lightedButton22.OptionMenuIndicator = false;
            this.lightedButton22.Size = new System.Drawing.Size(50, 30);
            this.lightedButton22.TabIndex = 19;
            this.lightedButton22.TextWhenOff = "Reset";
            this.lightedButton22.TextWhenOn = "Reset";
            this.lightedButton22.ButtonPressed += new System.EventHandler<LightedButton2.LightedButton2.ButtonPressedArgs>(this.lightedButton22_ButtonPressed);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(847, 491);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form1";
            this.Text = "QA350 DC Volt Meter";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Timer LEDKickerTimer;
        private System.Windows.Forms.Timer AcqTimer;
        private System.Windows.Forms.Label label3;
        private ZedGraph.ZedGraphControl zedGraphControl1;
        private ZedGraph.ZedGraphControl zedGraphControl2;
        private LightedButton2.LightedButton2 lightedButton22;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private LightedButton2.LightedButton2 SlowUpdateBtn;
        private LightedButton2.LightedButton2 FastUpdateBtn;
        private LightedButton2.LightedButton2 lightedButton27;
        private LightedButton2.LightedButton2 lightedButton28;
        private LightedButton2.LightedButton2 lightedButton29;
        private LightedButton2.LightedButton2 lightedButton210;
        private LightedButton2.LightedButton2 SetRelBtn;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem calibrateToolStripMenuItem;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label UncalLabel;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem reflashToolStripMenuItem;
        private System.Windows.Forms.Label RelModeLabel;
        private System.Windows.Forms.ToolStripMenuItem flashVirginDeviceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loggingToolStripMenuItem;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}

