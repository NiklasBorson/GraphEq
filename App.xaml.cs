﻿using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Linq;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GraphEq
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();

            ReadAppData();

            m_window.Closed += (object sender, WindowEventArgs args) => SaveAppData();
        }

        const string UserFunctionsFileName = "MyFunctions.txt";
        string FormulaSettingsKey(int i) => $"Formula{i + 1}";
        const string ScaleSettingsKey = "Scale";
        const string OriginXSettingsKey = "OriginX";
        const string OriginYSettingsKey = "OriginY";

        string m_savedUserFunctions = string.Empty;

        async void ReadAppData()
        {
            // Try opening the user functions file.
            var folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var file = (await folder.TryGetItemAsync(UserFunctionsFileName)) as StorageFile;
            if (file != null)
            {
                var text = await FileIO.ReadTextAsync(file);
                m_savedUserFunctions = text;
                m_window.UserFunctions.Text = text;
            }

            // Try getting the formula settings.
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;

            var formulas = m_window.Formulas;
            for (int i = 0; i < formulas.Count; i++)
            {
                object value;
                if (settings.TryGetValue(FormulaSettingsKey(i), out value))
                {
                    formulas[i].Text = value.ToString();
                }
            }

            // Try getting the scale and origin.
            object scale, originX, originY;
            if (settings.TryGetValue(ScaleSettingsKey, out scale) &&
                settings.TryGetValue(OriginXSettingsKey, out originX) &&
                settings.TryGetValue(OriginYSettingsKey, out originY) &&
                scale is float && originX is float && originY is float)
            {
                m_window.GraphScale = (float)scale;
                m_window.RelativeOrigin = new System.Numerics.Vector2((float)originX, (float)originY);
            }
        }

        void SaveAppData()
        {
            // Save the formulas.
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;

            var formulas = m_window.Formulas;
            for (int i = 0; i < formulas.Count; i++)
            {
                settings[FormulaSettingsKey(i)] = formulas[i].Text;
            }

            // Save the scale.
            settings[ScaleSettingsKey] = m_window.GraphScale;

            // Save the origin.
            var origin = m_window.RelativeOrigin;
            settings[OriginXSettingsKey] = origin.X;
            settings[OriginYSettingsKey] = origin.Y;

            // Save the user functions only if they've changed.
            if (m_window.UserFunctions.Text != m_savedUserFunctions)
            {
                var folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                var filePath = System.IO.Path.Combine(folder.Path, UserFunctionsFileName);

                using (var writer = new StreamWriter(filePath))
                {
                    writer.Write(m_window.UserFunctions.Text);
                }
            }
        }

        private MainWindow m_window;
    }
}
