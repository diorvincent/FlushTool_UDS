
namespace Diag_BUS
{
    partial class FlashAdressSet
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FlashAdressSet));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cbCalPart = new System.Windows.Forms.CheckBox();
            this.cbDataPart = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.nuCalStart = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.nuAppEndAddr = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nuCalStart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nuAppEndAddr)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cbCalPart);
            this.groupBox1.Controls.Add(this.cbDataPart);
            this.groupBox1.Location = new System.Drawing.Point(14, 15);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox1.Size = new System.Drawing.Size(382, 84);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Flash address type";
            // 
            // cbCalPart
            // 
            this.cbCalPart.AutoSize = true;
            this.cbCalPart.Location = new System.Drawing.Point(208, 38);
            this.cbCalPart.Name = "cbCalPart";
            this.cbCalPart.Size = new System.Drawing.Size(153, 29);
            this.cbCalPart.TabIndex = 0;
            this.cbCalPart.Text = "Calibration";
            this.cbCalPart.UseVisualStyleBackColor = true;
            this.cbCalPart.CheckedChanged += new System.EventHandler(this.cbCalPart_CheckedChanged);
            // 
            // cbDataPart
            // 
            this.cbDataPart.AutoSize = true;
            this.cbDataPart.Location = new System.Drawing.Point(19, 38);
            this.cbDataPart.Name = "cbDataPart";
            this.cbDataPart.Size = new System.Drawing.Size(154, 29);
            this.cbDataPart.TabIndex = 0;
            this.cbDataPart.Text = "Application";
            this.cbDataPart.UseVisualStyleBackColor = true;
            this.cbDataPart.CheckedChanged += new System.EventHandler(this.cbDataPart_CheckedChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.nuCalStart);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.nuAppEndAddr);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Location = new System.Drawing.Point(14, 109);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(382, 136);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Address info";
            // 
            // nuCalStart
            // 
            this.nuCalStart.Hexadecimal = true;
            this.nuCalStart.Location = new System.Drawing.Point(133, 81);
            this.nuCalStart.Maximum = new decimal(new int[] {
            -2145714161,
            0,
            0,
            0});
            this.nuCalStart.Name = "nuCalStart";
            this.nuCalStart.Size = new System.Drawing.Size(208, 33);
            this.nuCalStart.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 83);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(102, 25);
            this.label2.TabIndex = 0;
            this.label2.Text = "Cal start";
            // 
            // nuAppEndAddr
            // 
            this.nuAppEndAddr.Hexadecimal = true;
            this.nuAppEndAddr.Location = new System.Drawing.Point(132, 37);
            this.nuAppEndAddr.Maximum = new decimal(new int[] {
            -2145714161,
            0,
            0,
            0});
            this.nuAppEndAddr.Name = "nuAppEndAddr";
            this.nuAppEndAddr.Size = new System.Drawing.Size(209, 33);
            this.nuAppEndAddr.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 39);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(98, 25);
            this.label1.TabIndex = 0;
            this.label1.Text = "App end";
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(287, 260);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(109, 35);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // FlashAdressSet
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(418, 308);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Font = new System.Drawing.Font("Verdana", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FlashAdressSet";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Flash adress setting";
            this.Load += new System.EventHandler(this.FlashAdressSet_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nuCalStart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nuAppEndAddr)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox cbCalPart;
        private System.Windows.Forms.CheckBox cbDataPart;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.NumericUpDown nuCalStart;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nuAppEndAddr;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnOK;
    }
}