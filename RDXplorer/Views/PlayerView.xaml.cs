﻿using RDXplorer.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace RDXplorer.Views
{
    public partial class PlayerView : View<PlayerViewModel, PlayerViewModelEntry>
    {
        public PlayerView()
        {
            InitializeComponent();
            LoadModel();

            AppViewModel.PropertyChanged += UpdateOnDocumentChange;
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGrid grid = (DataGrid)sender;
            string column = GetDataGridColumnName(grid);

            if (string.IsNullOrEmpty(column))
                return;

            PlayerViewModelEntry entry = (PlayerViewModelEntry)grid.SelectedItem;

            Program.Windows.HexEditor.ShowFile(AppViewModel.RDXDocument.PathInfo);
            Program.Windows.HexEditor.SetPosition((long)entry.Model.Offset);
        }
    }
}