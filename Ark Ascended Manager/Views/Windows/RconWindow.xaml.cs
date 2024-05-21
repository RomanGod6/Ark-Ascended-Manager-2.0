using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using Ark_Ascended_Manager.ViewModels.Windows;

namespace Ark_Ascended_Manager.Views.Windows
{
    public partial class RconWindow : Window
    {
        public RconWindow()
        {
            InitializeComponent();
        }

        public void AddRconTab(RconViewModel rconViewModel)
        {
            // Check if a tab for this server already exists
            foreach (TabItem tabItem in RconTabControl.Items)
            {
                if (tabItem.Header is TabHeader header && header.Text == rconViewModel.ServerName)
                {
                    // Tab already exists, select it and return
                    RconTabControl.SelectedItem = tabItem;
                    return;
                }
            }

            var rconTabContent = new RconTabContent
            {
                DataContext = rconViewModel
            };

   

            var tabItemNew = new TabItem
            {
                Header = new TabHeader { Text = rconViewModel.ServerName },
                Content = rconTabContent,
                Style = (Style)FindResource("ClosableTabItemStyle")
            };

            RconTabControl.Items.Add(tabItemNew);
            RconTabControl.SelectedItem = tabItemNew;  // Automatically select the newly added tab

     
        }

        public int GetTabCount()
        {
            return RconTabControl.Items.Count;
        }

        public void CloseTab(TabItem tabItem)
        {
            if (tabItem != null && RconTabControl.Items.Contains(tabItem))
            {
                RconTabControl.Items.Remove(tabItem);
            }
        }
    }

    public class TabHeader : StackPanel
    {
        public TabHeader()
        {
            this.Orientation = Orientation.Horizontal;

            var textBlock = new TextBlock
            {
                Foreground = System.Windows.Media.Brushes.White
            };
            this.Children.Add(textBlock);

            var closeButton = new Button
            {
                Content = "X",
                Width = 15,
                Height = 15,
                VerticalAlignment = VerticalAlignment.Center,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderBrush = System.Windows.Media.Brushes.Transparent,
                Margin = new Thickness(5, 0, 0, 0),
                Style = (Style)Application.Current.Resources["CloseButtonStyle"]
            };
            closeButton.Click += CloseButton_Click;
            this.Children.Add(closeButton);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var tabItem = this.Parent as TabItem;
            if (tabItem != null)
            {
                var rconWindow = Window.GetWindow(tabItem) as RconWindow;
                rconWindow?.CloseTab(tabItem);
            }
        }

        public string Text
        {
            get => ((TextBlock)this.Children[0]).Text;
            set => ((TextBlock)this.Children[0]).Text = value;
        }
    }
}
