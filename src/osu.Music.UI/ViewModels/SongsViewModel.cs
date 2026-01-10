using Osu.Music.Common.Models;
using Osu.Music.UI.Models;
using Prism.Mvvm;
using System.ComponentModel;
using System.Windows.Data;

namespace Osu.Music.UI.ViewModels
{
    public class SongsViewModel : BindableBase
    {
        private readonly ICollectionView _beatmapsView;

        public ICollectionView BeatmapsView => _beatmapsView;

        private bool _isAscending = true;
        public bool IsAscending
        {
            get => _isAscending;
            set
            {
                SetProperty(ref _isAscending, value);
                ApplySort();
            }
        }

        public SongsViewModel(MainModel model)
        {
            _beatmapsView = CollectionViewSource.GetDefaultView(model.Beatmaps);

            ApplySort();
        }

        private void ApplySort()
        {
            _beatmapsView.SortDescriptions.Clear();

            _beatmapsView.SortDescriptions.Add(
                new SortDescription(
                    nameof(Beatmap.Title),
                    IsAscending ? ListSortDirection.Ascending : ListSortDirection.Descending
                )
            );
        }
    }
}
