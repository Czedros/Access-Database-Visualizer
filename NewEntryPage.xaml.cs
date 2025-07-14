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
            Title = _editingRow == null ? "New Entry" : "Edit Entry";
            BuildForm();
        }

        private readonly DataRow? _editingRow;

        public NewEntryPage(DataTable schema, string tableName, DataRow? editingRow = null)
        {
            InitializeComponent();
            _schema = schema;
            _tableName = tableName;
            _editingRow = editingRow;
            Title = _editingRow == null ? "New Entry" : "Edit Entry";
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

                if (dataType == typeof(bool))
                {
                    var checkBox = new CheckBox
                    {
                        Name = $"Field_{SanitizeName(columnName)}",
                        IsChecked = false
                    };

                    if (_editingRow != null && _editingRow[columnName] != DBNull.Value)
                        checkBox.IsChecked = Convert.ToBoolean(_editingRow[columnName]);

                    input = checkBox;
                }
                else
                {
                    var textBox = new TextBox
                    {
                        Name = $"Field_{SanitizeName(columnName)}",
                        Width = 200
                    };

                    if (_editingRow != null && _editingRow[columnName] != DBNull.Value)
                        textBox.Text = _editingRow[columnName].ToString();

                    input = textBox;
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
        private static string SanitizeName(string name)
        {
            // Only allow letters, digits, and underscores
            var sb = new System.Text.StringBuilder();
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
                else
                    sb.Append('_');
            }
            // Ensure the first character is a letter or underscore
            if (sb.Length == 0 || !(char.IsLetter(sb[0]) || sb[0] == '_'))
                sb.Insert(0, '_');
            return sb.ToString();
        }
    }
}
