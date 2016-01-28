namespace TextEditor
{
    partial class FindInFiles
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
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxSearchFor = new System.Windows.Forms.TextBox();
            this.labelSearchRoot = new System.Windows.Forms.Label();
            this.comboBoxSearchPath = new System.Windows.Forms.ComboBox();
            this.dataGridViewFindResults = new TextEditor.MyDataGridView();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.findInFilesEntryBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.labelExtensions = new System.Windows.Forms.Label();
            this.comboBoxSearchExtensions = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.labelStatus = new TextEditor.MyLabel();
            this.buttonFind = new System.Windows.Forms.Button();
            this.buttonFileDialog = new System.Windows.Forms.Button();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.checkBoxCaseSensitive = new System.Windows.Forms.CheckBox();
            this.checkBoxMatchWholeWord = new System.Windows.Forms.CheckBox();
            this.timerStatusUpdate = new System.Windows.Forms.Timer(this.components);
            this.tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewFindResults)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.findInFilesEntryBindingSource)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 5;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.textBoxSearchFor, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.labelSearchRoot, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.comboBoxSearchPath, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.dataGridViewFindResults, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.labelExtensions, 3, 1);
            this.tableLayoutPanel2.Controls.Add(this.comboBoxSearchExtensions, 4, 1);
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel1, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.buttonFileDialog, 2, 1);
            this.tableLayoutPanel2.Controls.Add(this.flowLayoutPanel1, 3, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 4;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.Size = new System.Drawing.Size(867, 282);
            this.tableLayoutPanel2.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Search for:";
            // 
            // textBoxSearchFor
            // 
            this.textBoxSearchFor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel2.SetColumnSpan(this.textBoxSearchFor, 2);
            this.textBoxSearchFor.Location = new System.Drawing.Point(68, 4);
            this.textBoxSearchFor.Name = "textBoxSearchFor";
            this.textBoxSearchFor.Size = new System.Drawing.Size(553, 20);
            this.textBoxSearchFor.TabIndex = 1;
            // 
            // labelSearchRoot
            // 
            this.labelSearchRoot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelSearchRoot.AutoSize = true;
            this.labelSearchRoot.Location = new System.Drawing.Point(3, 37);
            this.labelSearchRoot.Name = "labelSearchRoot";
            this.labelSearchRoot.Size = new System.Drawing.Size(59, 13);
            this.labelSearchRoot.TabIndex = 5;
            this.labelSearchRoot.Text = "Search in:";
            // 
            // comboBoxSearchPath
            // 
            this.comboBoxSearchPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxSearchPath.FormattingEnabled = true;
            this.comboBoxSearchPath.Location = new System.Drawing.Point(68, 33);
            this.comboBoxSearchPath.Name = "comboBoxSearchPath";
            this.comboBoxSearchPath.Size = new System.Drawing.Size(521, 21);
            this.comboBoxSearchPath.TabIndex = 6;
            // 
            // dataGridViewFindResults
            // 
            this.dataGridViewFindResults.AllowUserToAddRows = false;
            this.dataGridViewFindResults.AllowUserToDeleteRows = false;
            this.dataGridViewFindResults.AllowUserToOrderColumns = true;
            this.dataGridViewFindResults.AutoGenerateColumns = false;
            this.dataGridViewFindResults.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewFindResults.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn1,
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn3});
            this.tableLayoutPanel2.SetColumnSpan(this.dataGridViewFindResults, 5);
            this.dataGridViewFindResults.DataSource = this.findInFilesEntryBindingSource;
            this.dataGridViewFindResults.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewFindResults.Location = new System.Drawing.Point(3, 96);
            this.dataGridViewFindResults.Name = "dataGridViewFindResults";
            this.dataGridViewFindResults.ReadOnly = true;
            this.dataGridViewFindResults.Size = new System.Drawing.Size(861, 188);
            this.dataGridViewFindResults.TabIndex = 7;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "DisplayPath";
            this.dataGridViewTextBoxColumn1.HeaderText = "File";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 300;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "LineNumber";
            this.dataGridViewTextBoxColumn2.HeaderText = "Line";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "FormattedLine";
            this.dataGridViewTextBoxColumn3.HeaderText = "Text";
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 400;
            // 
            // findInFilesEntryBindingSource
            // 
            this.findInFilesEntryBindingSource.DataSource = typeof(TextEditor.FindInFilesEntry);
            // 
            // labelExtensions
            // 
            this.labelExtensions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelExtensions.AutoSize = true;
            this.labelExtensions.Location = new System.Drawing.Point(627, 37);
            this.labelExtensions.Name = "labelExtensions";
            this.labelExtensions.Size = new System.Drawing.Size(61, 13);
            this.labelExtensions.TabIndex = 8;
            this.labelExtensions.Text = "Extensions:";
            // 
            // comboBoxSearchExtensions
            // 
            this.comboBoxSearchExtensions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxSearchExtensions.FormattingEnabled = true;
            this.comboBoxSearchExtensions.Location = new System.Drawing.Point(694, 33);
            this.comboBoxSearchExtensions.MinimumSize = new System.Drawing.Size(170, 0);
            this.comboBoxSearchExtensions.Name = "comboBoxSearchExtensions";
            this.comboBoxSearchExtensions.Size = new System.Drawing.Size(170, 21);
            this.comboBoxSearchExtensions.TabIndex = 9;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel2.SetColumnSpan(this.tableLayoutPanel1, 5);
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.labelStatus, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonFind, 0, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 61);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(861, 29);
            this.tableLayoutPanel1.TabIndex = 10;
            // 
            // labelStatus
            // 
            this.labelStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelStatus.AutoEllipsis = true;
            this.labelStatus.Location = new System.Drawing.Point(134, 8);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(724, 13);
            this.labelStatus.TabIndex = 1;
            this.labelStatus.UseMnemonic = false;
            // 
            // buttonFind
            // 
            this.buttonFind.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.buttonFind.Location = new System.Drawing.Point(3, 3);
            this.buttonFind.Name = "buttonFind";
            this.buttonFind.Size = new System.Drawing.Size(125, 23);
            this.buttonFind.TabIndex = 0;
            this.buttonFind.Text = "Find";
            this.buttonFind.UseVisualStyleBackColor = true;
            this.buttonFind.Click += new System.EventHandler(this.buttonFind_Click);
            // 
            // buttonFileDialog
            // 
            this.buttonFileDialog.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.buttonFileDialog.AutoSize = true;
            this.buttonFileDialog.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonFileDialog.Location = new System.Drawing.Point(595, 32);
            this.buttonFileDialog.Name = "buttonFileDialog";
            this.buttonFileDialog.Size = new System.Drawing.Size(26, 23);
            this.buttonFileDialog.TabIndex = 11;
            this.buttonFileDialog.Text = "...";
            this.buttonFileDialog.UseVisualStyleBackColor = true;
            this.buttonFileDialog.Click += new System.EventHandler(this.buttonFileDialog_Click);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel2.SetColumnSpan(this.flowLayoutPanel1, 2);
            this.flowLayoutPanel1.Controls.Add(this.checkBoxCaseSensitive);
            this.flowLayoutPanel1.Controls.Add(this.checkBoxMatchWholeWord);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(627, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(227, 23);
            this.flowLayoutPanel1.TabIndex = 12;
            this.flowLayoutPanel1.WrapContents = false;
            // 
            // checkBoxCaseSensitive
            // 
            this.checkBoxCaseSensitive.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxCaseSensitive.AutoSize = true;
            this.checkBoxCaseSensitive.Location = new System.Drawing.Point(3, 3);
            this.checkBoxCaseSensitive.Name = "checkBoxCaseSensitive";
            this.checkBoxCaseSensitive.Size = new System.Drawing.Size(96, 17);
            this.checkBoxCaseSensitive.TabIndex = 2;
            this.checkBoxCaseSensitive.Text = "Case Sensitive";
            this.checkBoxCaseSensitive.UseVisualStyleBackColor = true;
            // 
            // checkBoxMatchWholeWord
            // 
            this.checkBoxMatchWholeWord.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxMatchWholeWord.AutoSize = true;
            this.checkBoxMatchWholeWord.Location = new System.Drawing.Point(105, 3);
            this.checkBoxMatchWholeWord.Name = "checkBoxMatchWholeWord";
            this.checkBoxMatchWholeWord.Size = new System.Drawing.Size(119, 17);
            this.checkBoxMatchWholeWord.TabIndex = 3;
            this.checkBoxMatchWholeWord.Text = "Match Whole Word";
            this.checkBoxMatchWholeWord.UseVisualStyleBackColor = true;
            // 
            // timerStatusUpdate
            // 
            this.timerStatusUpdate.Enabled = true;
            this.timerStatusUpdate.Interval = 250;
            this.timerStatusUpdate.Tick += new System.EventHandler(this.timerStatusUpdate_Tick);
            // 
            // FindInFiles
            // 
            this.AcceptButton = this.buttonFind;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(867, 282);
            this.Controls.Add(this.tableLayoutPanel2);
            this.Name = "FindInFiles";
            this.Text = "Find in Files";
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewFindResults)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.findInFilesEntryBindingSource)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxSearchFor;
        private System.Windows.Forms.CheckBox checkBoxCaseSensitive;
        private System.Windows.Forms.CheckBox checkBoxMatchWholeWord;
        private System.Windows.Forms.Button buttonFind;
        private System.Windows.Forms.Label labelSearchRoot;
        private System.Windows.Forms.ComboBox comboBoxSearchPath;
        private MyDataGridView dataGridViewFindResults;
        private System.Windows.Forms.BindingSource findInFilesEntryBindingSource;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.Label labelExtensions;
        private System.Windows.Forms.ComboBox comboBoxSearchExtensions;
        private System.Windows.Forms.Timer timerStatusUpdate;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button buttonFileDialog;
        private MyLabel labelStatus;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
    }
}
