namespace ScriptAnalyzer.ToolBar
{
    partial class historyManager
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(historyManager));
            this.dgv_UpdateHistory = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_UpdateHistory)).BeginInit();
            this.SuspendLayout();
            // 
            // dgv_UpdateHistory
            // 
            this.dgv_UpdateHistory.AllowUserToAddRows = false;
            this.dgv_UpdateHistory.AllowUserToDeleteRows = false;
            this.dgv_UpdateHistory.AllowUserToOrderColumns = true;
            this.dgv_UpdateHistory.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgv_UpdateHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_UpdateHistory.Location = new System.Drawing.Point(9, 93);
            this.dgv_UpdateHistory.MultiSelect = false;
            this.dgv_UpdateHistory.Name = "dgv_UpdateHistory";
            this.dgv_UpdateHistory.ReadOnly = true;
            this.dgv_UpdateHistory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_UpdateHistory.Size = new System.Drawing.Size(1119, 573);
            this.dgv_UpdateHistory.TabIndex = 0;
            this.dgv_UpdateHistory.TabStop = false;
            // 
            // historyManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1129, 666);
            this.Controls.Add(this.dgv_UpdateHistory);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "historyManager";
            this.Text = "Script Update History";
            ((System.ComponentModel.ISupportInitialize)(this.dgv_UpdateHistory)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv_UpdateHistory;
    }
}