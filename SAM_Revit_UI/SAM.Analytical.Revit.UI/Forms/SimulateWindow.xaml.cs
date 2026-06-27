// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using SAM.Weather;
using System;
using System.Linq;
using System.Windows;

namespace SAM.Analytical.Revit.UI.Forms
{
    /// <summary>
    /// WPF replacement for the retired WinForms SimulateForm.
    /// Hosts the shared SAM.Analytical.UI.WPF.SimulateControl and adds the
    /// Revit-specific GeometryCalculationMethod combo.
    /// </summary>
    public partial class SimulateWindow
    {
        public SimulateWindow()
        {
            InitializeComponent();
            Load();
        }

        public SimulateWindow(string projectName, string outputDirectory)
        {
            InitializeComponent();
            Load();

            simulateControl.ProjectName = projectName;
            simulateControl.OutputDirectory = outputDirectory;
        }

        private void Load()
        {
            // Mirror the WinForms SimulateForm default: construction layers update is on by default.
            simulateControl.UpdateConstructionLayersByPanelType = true;

            foreach (GeometryCalculationMethod method in Enum.GetValues(typeof(GeometryCalculationMethod))
                .Cast<GeometryCalculationMethod>()
                .Where(x => x != GeometryCalculationMethod.Undefined))
            {
                comboBox_GeometryCalculationMethod.Items.Add(Core.Query.Description((Enum)(object)method));
            }

            comboBox_GeometryCalculationMethod.SelectedItem = Core.Query.Description((Enum)(object)GeometryCalculationMethod.SAM);
        }

        // ── Properties proxied from SimulateControl ──────────────────────────

        public string OutputDirectory
        {
            get => simulateControl.OutputDirectory;
        }

        public string ProjectName
        {
            get => simulateControl.ProjectName;
        }

        public WeatherData WeatherData
        {
            get => simulateControl.WeatherData;
            set => simulateControl.WeatherData = value;
        }

        public bool UnmetHours
        {
            get => simulateControl.UnmetHours;
        }

        public bool RoomDataSheets
        {
            get => simulateControl.RoomDataSheets;
        }

        public SolarCalculationMethod SolarCalculationMethod
        {
            get => simulateControl.SolarCalculationMethod;
        }

        public bool UpdateConstructionLayersByPanelType
        {
            get => simulateControl.UpdateConstructionLayersByPanelType;
        }

        // ── Revit-specific ────────────────────────────────────────────────────

        public GeometryCalculationMethod GeometryCalculationMethod
        {
            get
            {
                string text = comboBox_GeometryCalculationMethod.SelectedItem as string;
                if (string.IsNullOrWhiteSpace(text))
                    return GeometryCalculationMethod.Undefined;

                foreach (GeometryCalculationMethod method in Enum.GetValues(typeof(GeometryCalculationMethod)))
                {
                    if (Core.Query.Description((Enum)(object)method) == text)
                        return method;
                }

                return GeometryCalculationMethod.Undefined;
            }
        }

        // ── Button handlers ───────────────────────────────────────────────────

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                MessageBox.Show("Provide project name");
                return;
            }

            if (string.IsNullOrWhiteSpace(OutputDirectory) || !System.IO.Directory.Exists(OutputDirectory))
            {
                MessageBox.Show("Given output directory does not exist. Please provide a valid directory.");
                return;
            }

            if (simulateControl.WeatherData == null)
            {
                MessageBox.Show("Provide Weather Data");
                return;
            }

            DialogResult = true;
        }

        private void button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
