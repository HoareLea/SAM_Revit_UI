
namespace SAM.Analytical.Revit.UI.Forms
{
    partial class CopyTagsForm
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
            this.Button_OK = new System.Windows.Forms.Button();
            this.Button_Cancel = new System.Windows.Forms.Button();
            this.SplitContainer_Main = new System.Windows.Forms.SplitContainer();
            this.GroupBox_Source = new System.Windows.Forms.GroupBox();
            this.ComboBoxControl_SourceTagType = new SAM.Core.Windows.ComboBoxControl();
            this.ComboBoxControl_SourceTemplate = new SAM.Core.Windows.ComboBoxControl();
            this.GroupBox_Destination = new System.Windows.Forms.GroupBox();
            this.ComboBoxControl_DestinationTagType = new SAM.Core.Windows.ComboBoxControl();
            this.ComboBoxControl_DestinationTemplate = new SAM.Core.Windows.ComboBoxControl();
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainer_Main)).BeginInit();
            this.SplitContainer_Main.Panel1.SuspendLayout();
            this.SplitContainer_Main.Panel2.SuspendLayout();
            this.SplitContainer_Main.SuspendLayout();
            this.GroupBox_Source.SuspendLayout();
            this.GroupBox_Destination.SuspendLayout();
            this.SuspendLayout();
            // 
            // Button_OK
            // 
            this.Button_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_OK.Location = new System.Drawing.Point(414, 163);
            this.Button_OK.Name = "Button_OK";
            this.Button_OK.Size = new System.Drawing.Size(75, 28);
            this.Button_OK.TabIndex = 3;
            this.Button_OK.Text = "OK";
            this.Button_OK.UseVisualStyleBackColor = true;
            this.Button_OK.Click += new System.EventHandler(this.Button_OK_Click);
            // 
            // Button_Cancel
            // 
            this.Button_Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Button_Cancel.Location = new System.Drawing.Point(495, 163);
            this.Button_Cancel.Name = "Button_Cancel";
            this.Button_Cancel.Size = new System.Drawing.Size(75, 28);
            this.Button_Cancel.TabIndex = 2;
            this.Button_Cancel.Text = "Cancel";
            this.Button_Cancel.UseVisualStyleBackColor = true;
            this.Button_Cancel.Click += new System.EventHandler(this.Button_Cancel_Click);
            // 
            // SplitContainer_Main
            // 
            this.SplitContainer_Main.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SplitContainer_Main.Location = new System.Drawing.Point(12, 12);
            this.SplitContainer_Main.Name = "SplitContainer_Main";
            // 
            // SplitContainer_Main.Panel1
            // 
            this.SplitContainer_Main.Panel1.Controls.Add(this.GroupBox_Source);
            this.SplitContainer_Main.Panel1MinSize = 200;
            // 
            // SplitContainer_Main.Panel2
            // 
            this.SplitContainer_Main.Panel2.Controls.Add(this.GroupBox_Destination);
            this.SplitContainer_Main.Panel2MinSize = 200;
            this.SplitContainer_Main.Size = new System.Drawing.Size(558, 145);
            this.SplitContainer_Main.SplitterDistance = 270;
            this.SplitContainer_Main.TabIndex = 4;
            // 
            // GroupBox_Source
            // 
            this.GroupBox_Source.Controls.Add(this.ComboBoxControl_SourceTagType);
            this.GroupBox_Source.Controls.Add(this.ComboBoxControl_SourceTemplate);
            this.GroupBox_Source.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GroupBox_Source.Location = new System.Drawing.Point(0, 0);
            this.GroupBox_Source.Name = "GroupBox_Source";
            this.GroupBox_Source.Size = new System.Drawing.Size(270, 145);
            this.GroupBox_Source.TabIndex = 1;
            this.GroupBox_Source.TabStop = false;
            this.GroupBox_Source.Text = "Source";
            // 
            // ComboBoxControl_SourceTagType
            // 
            this.ComboBoxControl_SourceTagType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ComboBoxControl_SourceTagType.Description = "Tag Type";
            this.ComboBoxControl_SourceTagType.Location = new System.Drawing.Point(6, 81);
            this.ComboBoxControl_SourceTagType.Name = "ComboBoxControl_SourceTagType";
            this.ComboBoxControl_SourceTagType.Size = new System.Drawing.Size(258, 54);
            this.ComboBoxControl_SourceTagType.TabIndex = 6;
            // 
            // ComboBoxControl_SourceTemplate
            // 
            this.ComboBoxControl_SourceTemplate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ComboBoxControl_SourceTemplate.Description = "Template Name";
            this.ComboBoxControl_SourceTemplate.Location = new System.Drawing.Point(6, 21);
            this.ComboBoxControl_SourceTemplate.Name = "ComboBoxControl_SourceTemplate";
            this.ComboBoxControl_SourceTemplate.Size = new System.Drawing.Size(258, 54);
            this.ComboBoxControl_SourceTemplate.TabIndex = 5;
            // 
            // GroupBox_Destination
            // 
            this.GroupBox_Destination.Controls.Add(this.ComboBoxControl_DestinationTagType);
            this.GroupBox_Destination.Controls.Add(this.ComboBoxControl_DestinationTemplate);
            this.GroupBox_Destination.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GroupBox_Destination.Location = new System.Drawing.Point(0, 0);
            this.GroupBox_Destination.Name = "GroupBox_Destination";
            this.GroupBox_Destination.Size = new System.Drawing.Size(284, 145);
            this.GroupBox_Destination.TabIndex = 0;
            this.GroupBox_Destination.TabStop = false;
            this.GroupBox_Destination.Text = "Destination";
            // 
            // ComboBoxControl_DestinationTagType
            // 
            this.ComboBoxControl_DestinationTagType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ComboBoxControl_DestinationTagType.Description = "Tag Type";
            this.ComboBoxControl_DestinationTagType.Location = new System.Drawing.Point(6, 81);
            this.ComboBoxControl_DestinationTagType.Name = "ComboBoxControl_DestinationTagType";
            this.ComboBoxControl_DestinationTagType.Size = new System.Drawing.Size(272, 54);
            this.ComboBoxControl_DestinationTagType.TabIndex = 8;
            // 
            // ComboBoxControl_DestinationTemplate
            // 
            this.ComboBoxControl_DestinationTemplate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ComboBoxControl_DestinationTemplate.Description = "Template Name";
            this.ComboBoxControl_DestinationTemplate.Location = new System.Drawing.Point(6, 21);
            this.ComboBoxControl_DestinationTemplate.Name = "ComboBoxControl_DestinationTemplate";
            this.ComboBoxControl_DestinationTemplate.Size = new System.Drawing.Size(272, 54);
            this.ComboBoxControl_DestinationTemplate.TabIndex = 7;
            // 
            // CopyTagsForm
            // 
            this.AcceptButton = this.Button_OK;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.Button_Cancel;
            this.ClientSize = new System.Drawing.Size(582, 203);
            this.Controls.Add(this.SplitContainer_Main);
            this.Controls.Add(this.Button_OK);
            this.Controls.Add(this.Button_Cancel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CopyTagsForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Copy Tags";
            this.Load += new System.EventHandler(this.CopyTagsForm_Load);
            this.SplitContainer_Main.Panel1.ResumeLayout(false);
            this.SplitContainer_Main.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.SplitContainer_Main)).EndInit();
            this.SplitContainer_Main.ResumeLayout(false);
            this.GroupBox_Source.ResumeLayout(false);
            this.GroupBox_Destination.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button Button_OK;
        private System.Windows.Forms.Button Button_Cancel;
        private System.Windows.Forms.SplitContainer SplitContainer_Main;
        private System.Windows.Forms.GroupBox GroupBox_Source;
        private System.Windows.Forms.GroupBox GroupBox_Destination;
        private SAM.Core.Windows.ComboBoxControl ComboBoxControl_SourceTemplate;
        private Core.Windows.ComboBoxControl ComboBoxControl_SourceTagType;
        private Core.Windows.ComboBoxControl ComboBoxControl_DestinationTagType;
        private Core.Windows.ComboBoxControl ComboBoxControl_DestinationTemplate;
    }
}