using System.ComponentModel;
using System.Windows;
using HaloSyncWin.UI.ViewModels;

namespace HaloSyncWin.UI
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _viewModel.Dispose();
            base.OnClosing(e);
        }
    }
}

