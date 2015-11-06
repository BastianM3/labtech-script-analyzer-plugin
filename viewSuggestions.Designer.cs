namespace ScriptAnalyzer.ToolBar
{
    partial class viewSuggestions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(viewSuggestions));
            this.richTextBoxSuggestions = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // richTextBoxSuggestions
            // 
            this.richTextBoxSuggestions.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(33)))), ((int)(((byte)(27)))));
            this.richTextBoxSuggestions.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBoxSuggestions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxSuggestions.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.richTextBoxSuggestions.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxSuggestions.Name = "richTextBoxSuggestions";
            this.richTextBoxSuggestions.ReadOnly = true;
            this.richTextBoxSuggestions.Size = new System.Drawing.Size(750, 657);
            this.richTextBoxSuggestions.TabIndex = 0;
            this.richTextBoxSuggestions.Text = "";
            // 
            // viewSuggestions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(750, 657);
            this.Controls.Add(this.richTextBoxSuggestions);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "viewSuggestions";
            this.Text = "Suggested Scripting Practices ";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox richTextBoxSuggestions;
    }
}