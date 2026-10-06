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

        /// <summary>
        /// Seeds the combo box with weather data already known to the caller — for this window, whatever the
        /// document's SAM_WeatherFile parameter holds. Set it; do NOT read it back to find out what the user
        /// chose, because it only ever returns the seeded entry: its getter looks the item up by the fixed
        /// internal key, so anything the user picked from disk is invisible to it. Read
        /// <see cref="SelectedWeatherData"/> instead.
        /// </summary>
        public WeatherData WeatherData
        {
            get => simulateControl.WeatherData;
            set => simulateControl.WeatherData = value;
        }

        /// <summary>
        /// What the user actually has selected — the seeded entry, or a file they browsed to. This is the one
        /// to read after the dialog closes. Mirrors SAM.Analytical.UI.WPF's own SimulateWindow, which has
        /// always had both and reads this one.
        /// </summary>
        public WeatherData SelectedWeatherData
        {
            get => simulateControl.SelectedWeatherData;
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

            // SelectedWeatherData, not WeatherData: the latter only sees the entry seeded from the document's
            // SAM_WeatherFile parameter, so on a model that has never been simulated this rejected every .epw
            // the user picked and there was no way past it.
            if (simulateControl.SelectedWeatherData == null)
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
