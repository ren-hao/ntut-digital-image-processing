namespace control_server
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this._rightBar = new System.Windows.Forms.TrackBar();
            this._leftBar = new System.Windows.Forms.TrackBar();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this._timer = new System.Windows.Forms.Timer(this.components);
            this.button1 = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.button4 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this._sourcePictureBox = new System.Windows.Forms.PictureBox();
            this._openCameraButton = new System.Windows.Forms.Button();
            this._resultPictureBox = new System.Windows.Forms.PictureBox();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._rightBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._leftBar)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._sourcePictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._resultPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this._rightBar);
            this.groupBox1.Controls.Add(this._leftBar);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(16, 51);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox1.Size = new System.Drawing.Size(392, 190);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Control Box";
            // 
            // _rightBar
            // 
            this._rightBar.Location = new System.Drawing.Point(41, 126);
            this._rightBar.Margin = new System.Windows.Forms.Padding(4);
            this._rightBar.Maximum = 100;
            this._rightBar.Name = "_rightBar";
            this._rightBar.Size = new System.Drawing.Size(343, 56);
            this._rightBar.TabIndex = 3;
            this._rightBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this._rightBar.Value = 50;
            // 
            // _leftBar
            // 
            this._leftBar.Location = new System.Drawing.Point(41, 26);
            this._leftBar.Margin = new System.Windows.Forms.Padding(4);
            this._leftBar.Maximum = 100;
            this._leftBar.Name = "_leftBar";
            this._leftBar.Size = new System.Drawing.Size(343, 56);
            this._leftBar.TabIndex = 2;
            this._leftBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this._leftBar.Value = 50;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 126);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(20, 15);
            this.label2.TabIndex = 1;
            this.label2.Text = "R:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 26);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(20, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "L:";
            // 
            // _timer
            // 
            this._timer.Tick += new System.EventHandler(this._timer_Tick);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(16, 15);
            this.button1.Margin = new System.Windows.Forms.Padding(4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(100, 29);
            this.button1.TabIndex = 1;
            this.button1.Text = "Start";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.button4);
            this.groupBox2.Controls.Add(this.button3);
            this.groupBox2.Controls.Add(this.button2);
            this.groupBox2.Enabled = false;
            this.groupBox2.Location = new System.Drawing.Point(16, 249);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox2.Size = new System.Drawing.Size(392, 125);
            this.groupBox2.TabIndex = 2;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Donate";
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(224, 26);
            this.button4.Margin = new System.Windows.Forms.Padding(4);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(100, 29);
            this.button4.TabIndex = 2;
            this.button4.Text = "1000";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.buttonDonate_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(116, 26);
            this.button3.Margin = new System.Windows.Forms.Padding(4);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(100, 29);
            this.button3.TabIndex = 1;
            this.button3.Text = "500";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.buttonDonate_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(8, 26);
            this.button2.Margin = new System.Windows.Forms.Padding(4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(100, 29);
            this.button2.TabIndex = 0;
            this.button2.Text = "100";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.buttonDonate_Click);
            // 
            // _sourcePictureBox
            // 
            this._sourcePictureBox.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._sourcePictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._sourcePictureBox.Location = new System.Drawing.Point(416, 51);
            this._sourcePictureBox.Margin = new System.Windows.Forms.Padding(4);
            this._sourcePictureBox.Name = "_sourcePictureBox";
            this._sourcePictureBox.Size = new System.Drawing.Size(426, 300);
            this._sourcePictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this._sourcePictureBox.TabIndex = 4;
            this._sourcePictureBox.TabStop = false;
            // 
            // _openCameraButton
            // 
            this._openCameraButton.Location = new System.Drawing.Point(17, 404);
            this._openCameraButton.Margin = new System.Windows.Forms.Padding(4);
            this._openCameraButton.Name = "_openCameraButton";
            this._openCameraButton.Size = new System.Drawing.Size(100, 29);
            this._openCameraButton.TabIndex = 5;
            this._openCameraButton.Text = "開啟攝影機";
            this._openCameraButton.UseVisualStyleBackColor = true;
            this._openCameraButton.Click += new System.EventHandler(this._openCameraButton_Click_1);
            // 
            // _resultPictureBox
            // 
            this._resultPictureBox.Anchor = System.Windows.Forms.AnchorStyles.None;
            this._resultPictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._resultPictureBox.Location = new System.Drawing.Point(915, 51);
            this._resultPictureBox.Margin = new System.Windows.Forms.Padding(4);
            this._resultPictureBox.Name = "_resultPictureBox";
            this._resultPictureBox.Size = new System.Drawing.Size(426, 300);
            this._resultPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this._resultPictureBox.TabIndex = 6;
            this._resultPictureBox.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1416, 562);
            this.Controls.Add(this._resultPictureBox);
            this.Controls.Add(this._openCameraButton);
            this.Controls.Add(this._sourcePictureBox);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.groupBox1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form1";
            this.Text = "Form1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._rightBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._leftBar)).EndInit();
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._sourcePictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._resultPictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TrackBar _leftBar;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TrackBar _rightBar;
        private System.Windows.Forms.Timer _timer;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button2;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.PictureBox _sourcePictureBox;
        private System.Windows.Forms.Button _openCameraButton;
        private System.Windows.Forms.PictureBox _resultPictureBox;
    }
}

