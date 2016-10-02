namespace LightedButton2
{
    partial class LightedButton2
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.OneShotTimer = new System.Windows.Forms.Timer(this.components);
            this.TurnOffTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // OneShotTimer
            // 
            this.OneShotTimer.Tick += new System.EventHandler(this.OneShotTimer_Tick);
            // 
            // TurnOffTimer
            // 
            this.TurnOffTimer.Tick += new System.EventHandler(this.TurnOffTimer_Tick);
            // 
            // LightedButton2
            // 
            this.Name = "LightedButton2";
            this.Size = new System.Drawing.Size(154, 144);
            this.Click += new System.EventHandler(this.LightedButton_Click);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer OneShotTimer;
        private System.Windows.Forms.Timer TurnOffTimer;
    }
}
