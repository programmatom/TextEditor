/*
 *  Copyright © 1992-2002, 2015 Thomas R. Lawrence
 * 
 *  GNU General Public License
 * 
 *  This file is part of "Text Editor"
 * 
 *  "Text Editor" is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
*/
namespace TextEditor
{
    partial class StochasticTest
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
            this.label1 = new System.Windows.Forms.Label();
            this.labelElapsedTime = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.buttonStop = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.labelOperationCount = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.labelLines = new System.Windows.Forms.Label();
            this.labelCharacters = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.labelMode = new System.Windows.Forms.Label();
            this.timerUpdateStatus = new System.Windows.Forms.Timer(this.components);
            this.timerTask = new System.Timers.Timer();
            this.label6 = new System.Windows.Forms.Label();
            this.labelRandomSeed = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.timerTask)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelElapsedTime, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelOperationCount, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.label4, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.labelLines, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.labelCharacters, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.label5, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.labelMode, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.label6, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.labelRandomSeed, 1, 5);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 7;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(244, 116);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(87, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Elapsed Time:";
            // 
            // labelElapsedTime
            // 
            this.labelElapsedTime.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelElapsedTime.AutoSize = true;
            this.labelElapsedTime.Location = new System.Drawing.Point(96, 0);
            this.labelElapsedTime.MinimumSize = new System.Drawing.Size(100, 13);
            this.labelElapsedTime.Name = "labelElapsedTime";
            this.labelElapsedTime.Size = new System.Drawing.Size(100, 13);
            this.labelElapsedTime.TabIndex = 1;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.flowLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanel1, 2);
            this.flowLayoutPanel1.Controls.Add(this.buttonStop);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(81, 88);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(81, 25);
            this.flowLayoutPanel1.TabIndex = 2;
            // 
            // buttonStop
            // 
            this.buttonStop.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.buttonStop.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonStop.Location = new System.Drawing.Point(3, 3);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(75, 23);
            this.buttonStop.TabIndex = 0;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 13);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(87, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Operation Count:";
            // 
            // labelOperationCount
            // 
            this.labelOperationCount.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelOperationCount.AutoSize = true;
            this.labelOperationCount.Location = new System.Drawing.Point(96, 13);
            this.labelOperationCount.MinimumSize = new System.Drawing.Size(100, 13);
            this.labelOperationCount.Name = "labelOperationCount";
            this.labelOperationCount.Size = new System.Drawing.Size(100, 13);
            this.labelOperationCount.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 26);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(87, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Lines:";
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 39);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(87, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "Characters:";
            // 
            // labelLines
            // 
            this.labelLines.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelLines.AutoSize = true;
            this.labelLines.Location = new System.Drawing.Point(96, 26);
            this.labelLines.MinimumSize = new System.Drawing.Size(100, 13);
            this.labelLines.Name = "labelLines";
            this.labelLines.Size = new System.Drawing.Size(100, 13);
            this.labelLines.TabIndex = 7;
            // 
            // labelCharacters
            // 
            this.labelCharacters.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelCharacters.AutoSize = true;
            this.labelCharacters.Location = new System.Drawing.Point(96, 39);
            this.labelCharacters.MinimumSize = new System.Drawing.Size(100, 13);
            this.labelCharacters.Name = "labelCharacters";
            this.labelCharacters.Size = new System.Drawing.Size(100, 13);
            this.labelCharacters.TabIndex = 8;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 52);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(87, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Mode:";
            // 
            // labelMode
            // 
            this.labelMode.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelMode.AutoSize = true;
            this.labelMode.Location = new System.Drawing.Point(96, 52);
            this.labelMode.MinimumSize = new System.Drawing.Size(100, 13);
            this.labelMode.Name = "labelMode";
            this.labelMode.Size = new System.Drawing.Size(100, 13);
            this.labelMode.TabIndex = 10;
            // 
            // timerUpdateStatus
            // 
            this.timerUpdateStatus.Enabled = true;
            this.timerUpdateStatus.Interval = 1000;
            this.timerUpdateStatus.Tick += new System.EventHandler(this.timerUpdateStatus_Tick);
            // 
            // timerTask
            // 
            this.timerTask.AutoReset = false;
            this.timerTask.Enabled = true;
            this.timerTask.SynchronizingObject = this;
            this.timerTask.Elapsed += new System.Timers.ElapsedEventHandler(this.timerTask_Elapsed);
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 68);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(87, 13);
            this.label6.TabIndex = 11;
            this.label6.Text = "Random Seed:";
            // 
            // labelRandomSeed
            // 
            this.labelRandomSeed.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelRandomSeed.AutoSize = true;
            this.labelRandomSeed.Location = new System.Drawing.Point(96, 68);
            this.labelRandomSeed.MinimumSize = new System.Drawing.Size(100, 13);
            this.labelRandomSeed.Name = "labelRandomSeed";
            this.labelRandomSeed.Size = new System.Drawing.Size(100, 13);
            this.labelRandomSeed.TabIndex = 12;
            this.labelRandomSeed.Text = "label7";
            // 
            // StochasticTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.buttonStop;
            this.ClientSize = new System.Drawing.Size(244, 116);
            this.Controls.Add(this.tableLayoutPanel1);
            this.MaximizeBox = false;
            this.Name = "StochasticTest";
            this.Text = "Text Editor - Stochastic Test";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.timerTask)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelElapsedTime;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelOperationCount;
        private System.Windows.Forms.Timer timerUpdateStatus;
        private System.Timers.Timer timerTask;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelLines;
        private System.Windows.Forms.Label labelCharacters;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label labelMode;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label labelRandomSeed;
    }
}
