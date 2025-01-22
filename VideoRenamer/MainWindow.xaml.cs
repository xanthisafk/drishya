using Drishya.Views;
using System.Windows;

namespace Drishya
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            NormalSortingView view = new NormalSortingView();
            NormalSortingViewTab.Content = view;
            this.Closing += view.CleanupMediaResources;
        }

    }
}