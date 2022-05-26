
namespace SAM.Analytical.Revit.UI.Forms
{
    partial class SimulateForm
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
            this.Button_Cancel = new System.Windows.Forms.Button();
            this.Button_OK = new System.Windows.Forms.Button();
            this.ComboBoxControl_GeometryCalculationMethod = new SAM.Core.Windows.ComboBoxControl();
            this.SimulateControl_Main = new SAM.Analytical.UI.Controls.SimulateControl();
            this.SuspendLayout();
            // 
            // Button_Cancel
            // 
            this.Button_Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_Cancel.Location = new System.Drawing.Point(413, 498);
            this.Button_Cancel.Name = "Button_Cancel";
            this.Button_Cancel.Size = new System.Drawing.Size(75, 28);
            this.Button_Cancel.TabIndex = 0;
            this.Button_Cancel.Text = "Cancel";
            this.Button_Cancel.UseVisualStyleBackColor = true;
            this.Button_Cancel.Click += new System.EventHandler(this.Button_Cancel_Click);
            // 
            // Button_OK
            // 
            this.Button_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Button_OK.Location = new System.Drawing.Point(332, 498);
            this.Button_OK.Name = "Button_OK";
            this.Button_OK.Size = new System.Drawing.Size(75, 28);
            this.Button_OK.TabIndex = 1;
            this.Button_OK.Text = "OK";
            this.Button_OK.UseVisualStyleBackColor = true;
            this.Button_OK.Click += new System.EventHandler(this.Button_OK_Click);
            // 
            // ComboBoxControl_GeometryCalculationMethod
            // 
            this.ComboBoxControl_GeometryCalculationMethod.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ComboBoxControl_GeometryCalculationMethod.Description = "Geometry Calculation Method:";
            this.ComboBoxControl_GeometryCalculationMethod.Location = new System.Drawing.Point(12, 403);
            this.ComboBoxControl_GeometryCalculationMethod.Name = "ComboBoxControl_GeometryCalculationMethod";
            this.ComboBoxControl_GeometryCalculationMethod.Size = new System.Drawing.Size(470, 55);
            this.ComboBoxControl_GeometryCalculationMethod.TabIndex = 0;
            // 
            // SimulateControl_Main
            // 
            this.SimulateControl_Main.Location = new System.Drawing.Point(12, 12);
            this.SimulateControl_Main.Name = "SimulateControl_Main";
            this.SimulateControl_Main.OutputDirectory = "";
            this.SimulateControl_Main.RoomDataSheets = false;
            this.SimulateControl_Main.ProjectName = "";
            this.SimulateControl_Main.Size = new System.Drawing.Size(470, 385);
            this.SimulateControl_Main.TabIndex = 2;
            this.SimulateControl_Main.UnmetHours = false;
            this.SimulateControl_Main.UpdateConstructionLayersByPanelType = true;
            // 
            // SimulateForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(500, 538);
            this.Controls.Add(this.Button_OK);
            this.Controls.Add(this.Button_Cancel);
            this.Controls.Add(this.ComboBoxControl_GeometryCalculationMethod);
            this.Controls.Add(this.SimulateControl_Main);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SimulateForm";
            this.ShowInTaskbar = false;
            this.Text = "Simulate";
            this.Load += new System.EventHandler(this.SimulateForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button Button_Cancel;
        private System.Windows.Forms.Button Button_OK;
        private Core.Windows.ComboBoxControl ComboBoxControl_GeometryCalculationMethod;
        private Analytical.UI.Controls.SimulateControl SimulateControl_Main;
    }
}