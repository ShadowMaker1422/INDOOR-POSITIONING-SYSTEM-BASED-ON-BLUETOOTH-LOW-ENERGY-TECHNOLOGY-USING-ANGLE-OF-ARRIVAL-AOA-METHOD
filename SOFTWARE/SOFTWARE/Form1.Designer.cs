namespace SOFTWARE
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if (disposing)
            {
                if (mapBitmap != null)
                {
                    mapBitmap.Dispose();
                    mapBitmap = null;
                }

              

                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            panel1 = new Panel();
            button1 = new Button();
            button2 = new Button();
            panel3 = new Panel();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            pnlMap = new Panel();
            btnTag = new Button();
            tabPage2 = new TabPage();
            label4 = new Label();
            label6 = new Label();
            label2 = new Label();
            label8 = new Label();
            label7 = new Label();
            label3 = new Label();
            label5 = new Label();
            label1 = new Label();
            btnStop = new Button();
            btnSetup = new Button();
            tbAnchor2Width = new TextBox();
            tbAnchor1Width = new TextBox();
            tbWidth = new TextBox();
            tbAnchor2Height = new TextBox();
            tbAnchor1Height = new TextBox();
            tbHeight = new TextBox();
            pnlDuplicate = new Panel();
            panel2 = new Panel();
            SimulationTimer = new System.Windows.Forms.Timer(components);
            timer2 = new System.Windows.Forms.Timer(components);
            timer3 = new System.Windows.Forms.Timer(components);
            MonitorTimer = new System.Windows.Forms.Timer(components);
            panel1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            pnlMap.SuspendLayout();
            tabPage2.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(button1);
            panel1.Controls.Add(button2);
            panel1.Controls.Add(panel3);
            panel1.Dock = DockStyle.Left;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(153, 485);
            panel1.TabIndex = 0;
            // 
            // button1
            // 
            button1.Dock = DockStyle.Top;
            button1.Location = new Point(0, 157);
            button1.Name = "button1";
            button1.Size = new Size(153, 54);
            button1.TabIndex = 1;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            button2.Dock = DockStyle.Top;
            button2.Location = new Point(0, 100);
            button2.Name = "button2";
            button2.Size = new Size(153, 57);
            button2.TabIndex = 2;
            button2.Text = "button2";
            button2.UseVisualStyleBackColor = true;
            // 
            // panel3
            // 
            panel3.Dock = DockStyle.Top;
            panel3.Location = new Point(0, 0);
            panel3.Name = "panel3";
            panel3.Size = new Size(153, 100);
            panel3.TabIndex = 3;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Dock = DockStyle.Bottom;
            tabControl1.Location = new Point(153, 33);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(621, 452);
            tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(pnlMap);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(613, 424);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "tabPage1";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // pnlMap
            // 
            pnlMap.Controls.Add(btnTag);
            pnlMap.Dock = DockStyle.Fill;
            pnlMap.Location = new Point(3, 3);
            pnlMap.Name = "pnlMap";
            pnlMap.Size = new Size(607, 418);
            pnlMap.TabIndex = 4;
            pnlMap.Paint += pnlMap_Paint;
            // 
            // btnTag
            // 
            btnTag.Location = new Point(487, 372);
            btnTag.Name = "btnTag";
            btnTag.Size = new Size(127, 53);
            btnTag.TabIndex = 0;
            btnTag.Text = "Kết nối ";
            btnTag.UseVisualStyleBackColor = true;
            btnTag.Click += btnTag_Click;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(label4);
            tabPage2.Controls.Add(label6);
            tabPage2.Controls.Add(label2);
            tabPage2.Controls.Add(label8);
            tabPage2.Controls.Add(label7);
            tabPage2.Controls.Add(label3);
            tabPage2.Controls.Add(label5);
            tabPage2.Controls.Add(label1);
            tabPage2.Controls.Add(btnStop);
            tabPage2.Controls.Add(btnSetup);
            tabPage2.Controls.Add(tbAnchor2Width);
            tabPage2.Controls.Add(tbAnchor1Width);
            tabPage2.Controls.Add(tbWidth);
            tabPage2.Controls.Add(tbAnchor2Height);
            tabPage2.Controls.Add(tbAnchor1Height);
            tabPage2.Controls.Add(tbHeight);
            tabPage2.Controls.Add(pnlDuplicate);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(613, 424);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "tabPage2";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(182, 64);
            label4.Name = "label4";
            label4.Size = new Size(66, 15);
            label4.TabIndex = 5;
            label4.Text = "Chiều rộng";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(425, 161);
            label6.Name = "label6";
            label6.Size = new Size(13, 15);
            label6.TabIndex = 5;
            label6.Text = "y";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(80, 161);
            label2.Name = "label2";
            label2.Size = new Size(13, 15);
            label2.TabIndex = 5;
            label2.Text = "y";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(470, 99);
            label8.Name = "label8";
            label8.Size = new Size(52, 15);
            label8.TabIndex = 5;
            label8.Text = "Anchor2";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(123, 99);
            label7.Name = "label7";
            label7.Size = new Size(52, 15);
            label7.TabIndex = 5;
            label7.Text = "Anchor1";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(182, 28);
            label3.Name = "label3";
            label3.Size = new Size(60, 15);
            label3.TabIndex = 5;
            label3.Text = "Chiều dài ";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(425, 125);
            label5.Name = "label5";
            label5.Size = new Size(12, 15);
            label5.TabIndex = 5;
            label5.Text = "x";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(80, 125);
            label1.Name = "label1";
            label1.Size = new Size(12, 15);
            label1.TabIndex = 5;
            label1.Text = "x";
            // 
            // btnStop
            // 
            btnStop.Location = new Point(460, 370);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(153, 54);
            btnStop.TabIndex = 4;
            btnStop.Text = "Tắt";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // btnSetup
            // 
            btnSetup.Location = new Point(0, 370);
            btnSetup.Name = "btnSetup";
            btnSetup.Size = new Size(153, 54);
            btnSetup.TabIndex = 3;
            btnSetup.Text = "Tạo mặt bằng ";
            btnSetup.UseVisualStyleBackColor = true;
            btnSetup.Click += button3_Click;
            // 
            // tbAnchor2Width
            // 
            tbAnchor2Width.Location = new Point(443, 154);
            tbAnchor2Width.Name = "tbAnchor2Width";
            tbAnchor2Width.Size = new Size(100, 23);
            tbAnchor2Width.TabIndex = 0;
            // 
            // tbAnchor1Width
            // 
            tbAnchor1Width.Location = new Point(98, 154);
            tbAnchor1Width.Name = "tbAnchor1Width";
            tbAnchor1Width.Size = new Size(100, 23);
            tbAnchor1Width.TabIndex = 0;
            // 
            // tbWidth
            // 
            tbWidth.Location = new Point(248, 57);
            tbWidth.Name = "tbWidth";
            tbWidth.Size = new Size(100, 23);
            tbWidth.TabIndex = 0;
            // 
            // tbAnchor2Height
            // 
            tbAnchor2Height.Location = new Point(443, 117);
            tbAnchor2Height.Name = "tbAnchor2Height";
            tbAnchor2Height.Size = new Size(100, 23);
            tbAnchor2Height.TabIndex = 0;
            // 
            // tbAnchor1Height
            // 
            tbAnchor1Height.Location = new Point(98, 117);
            tbAnchor1Height.Name = "tbAnchor1Height";
            tbAnchor1Height.Size = new Size(100, 23);
            tbAnchor1Height.TabIndex = 0;
            // 
            // tbHeight
            // 
            tbHeight.Location = new Point(248, 20);
            tbHeight.Name = "tbHeight";
            tbHeight.Size = new Size(100, 23);
            tbHeight.TabIndex = 0;
            // 
            // pnlDuplicate
            // 
            pnlDuplicate.Dock = DockStyle.Fill;
            pnlDuplicate.Location = new Point(3, 3);
            pnlDuplicate.Name = "pnlDuplicate";
            pnlDuplicate.Size = new Size(607, 418);
            pnlDuplicate.TabIndex = 6;
            pnlDuplicate.Paint += pnlDuplicate_Paint;
            // 
            // panel2
            // 
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(153, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(621, 33);
            panel2.TabIndex = 1;
            // 
            // SimulationTimer
            // 
            SimulationTimer.Interval = 500;
            SimulationTimer.Tick += SimulationTimer_Tick;
            // 
            // MonitorTimer
            // 
            MonitorTimer.Interval = 500;
            MonitorTimer.Tick += MonitorTimer_Tick;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(774, 485);
            Controls.Add(panel2);
            Controls.Add(tabControl1);
            Controls.Add(panel1);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            panel1.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            pnlMap.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private Panel panel2;
        private TabPage tabPage2;
        private Panel panel3;
        private Button button1;
        private Button button2;
        private Button btnSetup;
        private TextBox tbWidth;
        private TextBox tbHeight;
        private System.Windows.Forms.Timer SimulationTimer;
        private System.Windows.Forms.Timer timer2;
        private System.Windows.Forms.Timer timer3;
        private System.Windows.Forms.Timer MonitorTimer;
        private TextBox tbAnchor1Width;
        private TextBox tbAnchor1Height;
        private TextBox tbAnchor2Width;
        private TextBox tbAnchor2Height;
        private Button btnStop;
        private Button btnTag;
        private Panel pnlMap;
        private Label label1;
        private Label label2;
        private Label label4;
        private Label label6;
        private Label label8;
        private Label label7;
        private Label label3;
        private Label label5;
        private Panel pnlDuplicate;
    }
}
