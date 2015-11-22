namespace TextEditor
{
    partial class FindDialog
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.textBoxReplace = new System.Windows.Forms.TextBox();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonFindNext = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonReplaceAndFindNext = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonReplaceAll = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.buttonDone = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.textBoxFind = new System.Windows.Forms.TextBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.checkBoxCaseSensitive = new System.Windows.Forms.CheckBox();
            this.checkBoxMatchWholeWord = new System.Windows.Forms.CheckBox();
            this.checkBoxUp = new System.Windows.Forms.CheckBox();
            this.timerReleaseControl = new System.Windows.Forms.Timer(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.groupBox2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel2, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.groupBox1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(535, 201);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.textBoxReplace);
            this.groupBox2.Location = new System.Drawing.Point(3, 70);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(529, 61);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Replace with:";
            // 
            // textBoxReplace
            // 
            this.textBoxReplace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxReplace.Location = new System.Drawing.Point(3, 16);
            this.textBoxReplace.Multiline = true;
            this.textBoxReplace.Name = "textBoxReplace";
            this.textBoxReplace.Size = new System.Drawing.Size(523, 42);
            this.textBoxReplace.TabIndex = 0;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.flowLayoutPanel2.AutoSize = true;
            this.flowLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel2.Controls.Add(this.buttonFindNext);
            this.flowLayoutPanel2.Controls.Add(this.label1);
            this.flowLayoutPanel2.Controls.Add(this.buttonReplaceAndFindNext);
            this.flowLayoutPanel2.Controls.Add(this.label2);
            this.flowLayoutPanel2.Controls.Add(this.buttonReplaceAll);
            this.flowLayoutPanel2.Controls.Add(this.label3);
            this.flowLayoutPanel2.Controls.Add(this.buttonDone);
            this.flowLayoutPanel2.Location = new System.Drawing.Point(6, 166);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(523, 29);
            this.flowLayoutPanel2.TabIndex = 1;
            // 
            // buttonFindNext
            // 
            this.buttonFindNext.Location = new System.Drawing.Point(3, 3);
            this.buttonFindNext.Name = "buttonFindNext";
            this.buttonFindNext.Size = new System.Drawing.Size(100, 23);
            this.buttonFindNext.TabIndex = 0;
            this.buttonFindNext.Text = "Find Next";
            this.buttonFindNext.UseVisualStyleBackColor = true;
            this.buttonFindNext.Click += new System.EventHandler(this.buttonFindNext_Click);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(109, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(7, 23);
            this.label1.TabIndex = 2;
            // 
            // buttonReplaceAndFindNext
            // 
            this.buttonReplaceAndFindNext.Location = new System.Drawing.Point(122, 3);
            this.buttonReplaceAndFindNext.Name = "buttonReplaceAndFindNext";
            this.buttonReplaceAndFindNext.Size = new System.Drawing.Size(130, 23);
            this.buttonReplaceAndFindNext.TabIndex = 1;
            this.buttonReplaceAndFindNext.Text = "Replace and Find Again";
            this.buttonReplaceAndFindNext.UseVisualStyleBackColor = true;
            this.buttonReplaceAndFindNext.Click += new System.EventHandler(this.buttonReplaceAndFindNext_Click);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(258, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(7, 23);
            this.label2.TabIndex = 4;
            // 
            // buttonReplaceAll
            // 
            this.buttonReplaceAll.Location = new System.Drawing.Point(271, 3);
            this.buttonReplaceAll.Name = "buttonReplaceAll";
            this.buttonReplaceAll.Size = new System.Drawing.Size(130, 23);
            this.buttonReplaceAll.TabIndex = 3;
            this.buttonReplaceAll.Text = "Replace All";
            this.buttonReplaceAll.UseVisualStyleBackColor = true;
            this.buttonReplaceAll.Click += new System.EventHandler(this.buttonReplaceAll_Click);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(407, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(7, 23);
            this.label3.TabIndex = 5;
            // 
            // buttonDone
            // 
            this.buttonDone.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonDone.Location = new System.Drawing.Point(420, 3);
            this.buttonDone.Name = "buttonDone";
            this.buttonDone.Size = new System.Drawing.Size(100, 23);
            this.buttonDone.TabIndex = 6;
            this.buttonDone.Text = "Done";
            this.buttonDone.UseVisualStyleBackColor = true;
            this.buttonDone.Click += new System.EventHandler(this.buttonDone_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.textBoxFind);
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(529, 61);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Search for:";
            // 
            // textBoxFind
            // 
            this.textBoxFind.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxFind.Location = new System.Drawing.Point(3, 16);
            this.textBoxFind.Multiline = true;
            this.textBoxFind.Name = "textBoxFind";
            this.textBoxFind.Size = new System.Drawing.Size(523, 42);
            this.textBoxFind.TabIndex = 0;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.Controls.Add(this.checkBoxCaseSensitive);
            this.flowLayoutPanel1.Controls.Add(this.checkBoxMatchWholeWord);
            this.flowLayoutPanel1.Controls.Add(this.checkBoxUp);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 137);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(20, 0, 0, 0);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(273, 23);
            this.flowLayoutPanel1.TabIndex = 2;
            // 
            // checkBoxCaseSensitive
            // 
            this.checkBoxCaseSensitive.AutoSize = true;
            this.checkBoxCaseSensitive.Location = new System.Drawing.Point(23, 3);
            this.checkBoxCaseSensitive.Name = "checkBoxCaseSensitive";
            this.checkBoxCaseSensitive.Size = new System.Drawing.Size(82, 17);
            this.checkBoxCaseSensitive.TabIndex = 0;
            this.checkBoxCaseSensitive.Text = "Match case";
            this.checkBoxCaseSensitive.UseVisualStyleBackColor = true;
            // 
            // checkBoxMatchWholeWord
            // 
            this.checkBoxMatchWholeWord.AutoSize = true;
            this.checkBoxMatchWholeWord.Location = new System.Drawing.Point(111, 3);
            this.checkBoxMatchWholeWord.Name = "checkBoxMatchWholeWord";
            this.checkBoxMatchWholeWord.Size = new System.Drawing.Size(113, 17);
            this.checkBoxMatchWholeWord.TabIndex = 1;
            this.checkBoxMatchWholeWord.Text = "Match whole word";
            this.checkBoxMatchWholeWord.UseVisualStyleBackColor = true;
            // 
            // checkBoxUp
            // 
            this.checkBoxUp.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.checkBoxUp.AutoSize = true;
            this.checkBoxUp.Location = new System.Drawing.Point(230, 3);
            this.checkBoxUp.Name = "checkBoxUp";
            this.checkBoxUp.Size = new System.Drawing.Size(40, 17);
            this.checkBoxUp.TabIndex = 2;
            this.checkBoxUp.Text = "Up";
            this.checkBoxUp.UseVisualStyleBackColor = true;
            // 
            // FindDialog
            // 
            this.AcceptButton = this.buttonFindNext;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonDone;
            this.ClientSize = new System.Drawing.Size(535, 201);
            this.Controls.Add(this.tableLayoutPanel1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FindDialog";
            this.ShowIcon = false;
            this.Text = "FindDialog";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox textBoxFind;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox textBoxReplace;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Button buttonFindNext;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonReplaceAndFindNext;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonReplaceAll;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button buttonDone;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.CheckBox checkBoxCaseSensitive;
        private System.Windows.Forms.CheckBox checkBoxMatchWholeWord;
        private System.Windows.Forms.Timer timerReleaseControl;
        private System.Windows.Forms.CheckBox checkBoxUp;
    }
}