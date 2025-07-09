
namespace WPF_Visualizer_Temp
{
    public class Bookmark
    {
        public string DatabasePath { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;

        public override string ToString() => $"{System.IO.Path.GetFileName(DatabasePath)} → {TableName}";
    }
}
