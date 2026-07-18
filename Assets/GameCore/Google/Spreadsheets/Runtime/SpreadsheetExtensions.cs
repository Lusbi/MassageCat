using Google.Apis.Sheets.v4.Data;
using System.Text;

namespace GameCore.Google.Spreadsheets.Extensions
{
    public static class SpreadsheetExtensions
    {
        private const char CSV_SEPARATOR = ',';

        public static string ToCsv(this Sheet sheet)
        {
            StringBuilder sb = new();
            CellData cellData;
            foreach (var gridData in sheet.Data)
            {
                foreach (var rowData in gridData.RowData)
                {
                    for (int i = 0; i < rowData.Values.Count; ++i)
                    {
                        if (i > 0)
                            sb.Append(CSV_SEPARATOR);
                        cellData = rowData.Values[i];
                        sb.Append(GetCellFormattedValue(cellData));
                    }
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        public static string ToCsv(this ValueRange range)
        {
            if (range == null || range.Values == null)
                return null;

            StringBuilder sb = new();
            AppendToStringBuilder(range, sb);
            return sb.ToString();
        }

        public static string ToCsv(this BatchGetValuesResponse response)
        {
            StringBuilder sb = new();
            bool skipHeader = false;
            foreach (var range in response.ValueRanges)
            {
                if (range == null || range.Values == null)
                    continue;

                AppendToStringBuilder(range, sb, skipHeader);
                skipHeader = true;
            }
            return sb.ToString();
        }

        private static string GetCellFormattedValue(CellData cellData)
        {
            if (string.IsNullOrWhiteSpace(cellData.FormattedValue))
                return string.Empty;
            return cellData.FormattedValue;
        }

        private static void AppendToStringBuilder(ValueRange range, StringBuilder sb, bool skipHeader = false)
        {
            string value;
            foreach (var row in range.Values)
            {
                if (skipHeader)
                {
                    skipHeader = false;
                    continue;
                }

                for (int i = 0; i < row.Count; ++i)
                {
                    if (i > 0)
                        sb.Append(CSV_SEPARATOR);
                    value = row[i].ToString();
                    if (string.IsNullOrWhiteSpace(value))
                        value = string.Empty;
                    sb.Append(value);
                }
                sb.AppendLine();
            }
        }
    }
}
