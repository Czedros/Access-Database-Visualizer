using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPF_Visualizer_Temp
{
    /// <summary>
    /// Interaction logic for ColumnFilterPopup.xaml
    /// </summary>
    public partial class ColumnFilterPopup : UserControl
    {
        public event Action<List<string>>? FilterApplied;
        public event Action? SortAscending;
        public event Action? SortDescending;
        public event Action? ResetFilter;

        private List<string> _allValues = new();

        public ColumnFilterPopup()
        {
            InitializeComponent();
        }

        public void LoadValues(List<string> values)
        {
            _allValues = values.Distinct().OrderBy(v => v).ToList();
            RenderCheckboxes(_allValues);
        }

        private void RenderCheckboxes(IEnumerable<string> values)
        {
            CheckboxList.Items.Clear();
            foreach (var val in values)
            {
                var cb = new CheckBox
                {
                    Content = val,
                    IsChecked = true
                };
                CheckboxList.Items.Add(cb);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Watermark.Visibility = string.IsNullOrEmpty(SearchBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;

            string query = SearchBox.Text.ToLower();
            var filtered = _allValues
                .Where(v => v.ToLower().Contains(query))
                .ToList();

            RenderCheckboxes(filtered);
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            var selected = CheckboxList.Items
                .OfType<CheckBox>()
                .Where(cb => cb.IsChecked == true)
                .Select(cb => cb.Content?.ToString() ?? "")
                .ToList();

            FilterApplied?.Invoke(selected);
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            ResetFilter?.Invoke();
        }

        private void SortAsc_Click(object sender, RoutedEventArgs e)
        {
            SortAscending?.Invoke();
        }

        private void SortDesc_Click(object sender, RoutedEventArgs e)
        {
            SortDescending?.Invoke();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in CheckboxList.Items.OfType<CheckBox>())
            {
                item.IsChecked = true;
            }
        }

        private void UnselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in CheckboxList.Items.OfType<CheckBox>())
            {
                item.IsChecked = false;
            }
        }
    }
}
