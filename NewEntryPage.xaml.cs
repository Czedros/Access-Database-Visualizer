using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace WPF_Visualizer_Temp
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class NewEntryPage : Window
    {
        public Dictionary<string, object> EntryValues { get; private set; } = new();

        private readonly DataTable _schema;
        private readonly string _tableName;

        public NewEntryPage(DataTable schema, string tableName)
        {
            InitializeComponent();
            _schema = schema;
            _tableName = tableName;
            BuildForm();
        }

        private void BuildForm()
        {
            foreach (DataColumn column in _schema.Columns)
            {
                string columnName = column.ColumnName;
                Type dataType = column.DataType;

                if (column.AutoIncrement)
                    continue; // skip primary key auto-increment columns

                var label = new TextBlock
                {
                    Text = columnName,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 5, 0, 2)
                };

                Control input;

                // Create control based on data type
                if (dataType == typeof(bool))
                {
                    input = new CheckBox
                    {
                        Name = $"Field_{columnName}",
                        IsChecked = false
                    };
                }
                else
                {
                    input = new TextBox
                    {
                        Name = $"Field_{columnName}",
                        Width = 200
                    };
                }

                FormPanel.Children.Add(label);
                FormPanel.Children.Add(input);
            }
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            EntryValues = new Dictionary<string, object>();

            foreach (var child in FormPanel.Children)
            {
                if (child is TextBox tb && tb.Name.StartsWith("Field_"))
                {
                    string fieldName = tb.Name.Substring("Field_".Length);
                    EntryValues[fieldName] = string.IsNullOrWhiteSpace(tb.Text) ? DBNull.Value : tb.Text;
                }
                else if (child is CheckBox cb && cb.Name.StartsWith("Field_"))
                {
                    string fieldName = cb.Name.Substring("Field_".Length);
                    EntryValues[fieldName] = cb.IsChecked ?? false;
                }
            }

            DialogResult = true;
            Close();
        }
    }
}
