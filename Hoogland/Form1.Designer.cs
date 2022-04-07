
namespace Hoogland
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
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.boardPanel = new System.Windows.Forms.Panel();
            this.startButton = new System.Windows.Forms.Button();
            this.exitButton = new System.Windows.Forms.Button();
            this.movesTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // boardPanel
            // 
            this.boardPanel.BackColor = System.Drawing.SystemColors.ControlDark;
            this.boardPanel.Location = new System.Drawing.Point(12, 12);
            this.boardPanel.Name = "boardPanel";
            this.boardPanel.Size = new System.Drawing.Size(600, 600);
            this.boardPanel.TabIndex = 0;
            this.boardPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.boardPanel_Paint);
            // 
            // startButton
            // 
            this.startButton.Location = new System.Drawing.Point(636, 68);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(174, 55);
            this.startButton.TabIndex = 1;
            this.startButton.Text = "Start Game";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // exitButton
            // 
            this.exitButton.Location = new System.Drawing.Point(636, 565);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new System.Drawing.Size(174, 47);
            this.exitButton.TabIndex = 2;
            this.exitButton.Text = "Exit Game";
            this.exitButton.UseVisualStyleBackColor = true;
            this.exitButton.Click += new System.EventHandler(this.exitButton_Click);
            // 
            // movesTextBox
            // 
            this.movesTextBox.BackColor = System.Drawing.SystemColors.ScrollBar;
            this.movesTextBox.Location = new System.Drawing.Point(636, 129);
            this.movesTextBox.Multiline = true;
            this.movesTextBox.Name = "movesTextBox";
            this.movesTextBox.ReadOnly = true;
            this.movesTextBox.Size = new System.Drawing.Size(174, 406);
            this.movesTextBox.TabIndex = 3;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(822, 625);
            this.Controls.Add(this.movesTextBox);
            this.Controls.Add(this.exitButton);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.boardPanel);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel boardPanel;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Button exitButton;
        private System.Windows.Forms.TextBox movesTextBox;
    }
}

