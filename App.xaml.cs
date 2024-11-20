using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
        const string FormulaSettingsKey = "Formula";
        const string Formula2SettingsKey = "Formula2";
        const string ScaleSettingsKey = "Scale";
        const string OriginXSettingsKey = "OriginX";
        const string OriginYSettingsKey = "OriginY";

        async void ReadAppData()
        {
            // Try opening the user functions file.
            var folder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var file = (await folder.TryGetItemAsync(UserFunctionsFileName)) as StorageFile;
            if (file != null)
            {
                var text = await FileIO.ReadTextAsync(file);
                m_window.UserFunctions = text;
            }

            // Try getting the formula settings.
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
            object formula;
            if (settings.TryGetValue(FormulaSettingsKey, out formula))
            {
                m_window.FormulaText = formula.ToString();
            }
            if (settings.TryGetValue(Formula2SettingsKey, out formula))
            {
                m_window.Formula2Text = formula.ToString();
            }

            // Try getting the scale and origin.
            object scale, originX, originY;
            if (settings.TryGetValue(ScaleSettingsKey, out scale) &&
                settings.TryGetValue(OriginXSettingsKey, out originX) &&
                settings.TryGetValue(OriginYSettingsKey, out originY) &&
                scale is float && originX is float && originY is float)
            {
                m_window.Scale = (float)scale;
                m_window.RelativeOrigin = new System.Numerics.Vector2((float)originX, (float)originY);
            }
        }

        void SaveAppData()
        {
            // Save the formulas.
            var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
            settings[FormulaSettingsKey] = m_window.FormulaText;
            settings[Formula2SettingsKey] = m_window.Formula2Text;

            // Save the scale.
            settings[ScaleSettingsKey] = m_window.Scale;

            // Save the origin.
            var origin = m_window.RelativeOrigin;
            settings[OriginXSettingsKey] = origin.X;
            settings[OriginYSettingsKey] = origin.Y;

            // Save the user functions only if they've changed.
            if (m_window.HaveUserFunctionsChanged)
            {
                var folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                var filePath = System.IO.Path.Combine(folder.Path, UserFunctionsFileName);

                using (var writer = new StreamWriter(filePath))
                {
                    writer.Write(m_window.UserFunctions);
                }
            }
        }

        private MainWindow m_window;
    }
}
