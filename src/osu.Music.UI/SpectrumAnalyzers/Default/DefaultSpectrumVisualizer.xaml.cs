using Osu.Music.Services.Audio;
using Osu.Music.UI.SpectrumAnalyzers.Default;
using System;
using System.Windows.Controls;

namespace Osu.Music.UI.SpectrumAnalyzers
{
    /// <summary>
    /// Логика взаимодействия для DefaultSpectrumVisualizer.xaml
    /// </summary>
    public partial class DefaultSpectrumVisualizer : UserControl
    {
        public int ColumnsCount { get; set; } = 18;

        public DefaultSpectrumVisualizer()
        {
            InitializeComponent();
            InitializeBars();
        }

        private void InitializeBars()
        {
            for (int i = 0; i < ColumnsCount; i++)
            {
                Bars.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star)
                });

                var bar = new SpectrumBar();
                Grid.SetColumn(bar, i);

                Bars.Children.Add(bar);
            }
        }

        public void Update(FrequencySpectrum spectrum)
        {
            float[] data = spectrum.GetLogarithmicallyScaledSpectrum();
            UpdateBars(data);
        }

        private void UpdateBars(float[] data)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                for (var i = 0; i < ColumnsCount; i++)
                {
                    if (i < Bars.Children.Count)
                    {
                        var bar = Bars.Children[i] as SpectrumBar;
                        if (bar != null)
                        {
                            bar.Value = data[i];
                        }
                    }
                }
            }));
        }
    }
}
