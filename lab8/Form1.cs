using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Globalization;
using System.Drawing;

namespace lab8
{
    public partial class Form1 : Form
    {
        private DataTable dataTable = new DataTable();

        public Form1()
        {
            InitializeComponent();
            button1.Click += Button1_Click;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            string path = textBox1.Text;

            if (!File.Exists(path))
            {
                MessageBox.Show("Файл не найден.");
                return;
            }

            LoadCsv(path);
            dataGridView1.DataSource = dataTable;

            PlotChart1();
            PlotChart2();
            PlotChart3();
        }

        private void LoadCsv(string path)
        {
            dataTable.Clear();
            dataTable.Columns.Clear();

            var lines = File.ReadAllLines(path);
            var headers = lines[0].Split(',');

            for (int i = 0; i < headers.Length; i++)
{
    headers[i] = headers[i].Trim('"');

    // Укажем тип double для всех, кроме первого (страна)
    if (i == 0)
        dataTable.Columns.Add(headers[i], typeof(string));
    else
        dataTable.Columns.Add(headers[i], typeof(double));
}

            for (int i = 1; i < lines.Length; i++)
            {
                var values = ParseCsvLine(lines[i]);

                if (values.Length != dataTable.Columns.Count)
                    continue;

                var row = dataTable.NewRow();
                row[0] = values[0]; // Первый столбец — строка (например, имя)

                for (int j = 1; j < values.Length; j++)
                {
                    if (double.TryParse(values[j].Replace('.', ','), out double result))
                        row[j] = result;
                    else
                        row[j] = DBNull.Value; // или 0, если хочешь заменить некорректные значения
                }

                dataTable.Rows.Add(row);
            }
        }

        private string[] ParseCsvLine(string line)
        {
            var values = new List<string>();
            bool inQuotes = false;
            string currentField = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    values.Add(currentField.Trim());
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }

            values.Add(currentField.Trim());

            return values.ToArray();
        }

        private void PlotChart1()
        {
            chart1.Series.Clear();
            chart1.ChartAreas.Clear();
            chart1.Titles.Clear();

            chart1.ChartAreas.Add(new ChartArea("MainArea"));

            string corruptionColumn = "Trust (Government Corruption)";
            int countryColumnIndex = 0; // первый столбец — страна

            if (!dataTable.Columns.Contains(corruptionColumn))
            {
                MessageBox.Show($"В таблице нет столбца '{corruptionColumn}'");
                return;
            }

            // Получаем N из NumericUpDown
            int N = (int)numericUpDown1.Value;

            // Список пар: страна -> коррупция
            var countryCorruption = new List<(string Country, double Corruption)>();

            foreach (DataRow row in dataTable.Rows)
            {
                string country = row[countryColumnIndex].ToString();
                if (row[corruptionColumn] != DBNull.Value &&
                    double.TryParse(row[corruptionColumn].ToString(), out double corruption))
                {
                    countryCorruption.Add((country, corruption));
                }
            }

            // Выбираем N стран с минимальной коррупцией
            var selected = countryCorruption
                .OrderBy(x => x.Corruption)
                .Take(N)
                .OrderBy(x => x.Country) // сортировка по алфавиту
                .ToList();

            var series = new Series("Коррупция")
            {
                ChartType = SeriesChartType.Bar, // Горизонтальные столбцы
                BorderWidth = 1
            };

            foreach (var item in selected)
            {
                series.Points.AddXY(item.Country, item.Corruption);
            }

            chart1.Series.Add(series);
            chart1.Titles.Add("Страны с наименьшей коррупцией");
            chart1.Invalidate();
        }

        private void PlotChart2()
        {
            chart2.Series.Clear();
            chart2.Titles.Clear();
            chart2.ChartAreas.Clear();

            chart2.ChartAreas.Add(new ChartArea("MainArea"));

            var series = new Series("Family vs Happiness")
            {
                ChartType = SeriesChartType.Point,
                MarkerSize = 7,
                ToolTip = "Tooltip enabled" // Важно!
            };

            foreach (DataRow row in dataTable.Rows)
            {
                if (row["Happiness Score"] == DBNull.Value || row["Family"] == DBNull.Value)
                    continue;

                double happiness = Convert.ToDouble(row["Happiness Score"]);
                double family = Convert.ToDouble(row["Family"]);
                string country = row["Country"].ToString();

                int pointIndex = series.Points.AddXY(family, happiness);
                series.Points[pointIndex].ToolTip = $"Страна: {country}\nСемья: {family:F2}\nСчастье: {happiness:F2}";
            }

            chart2.Series.Add(series);

            chart2.ChartAreas[0].AxisX.LabelStyle.Format = "F2";
            chart2.ChartAreas[0].AxisY.LabelStyle.Format = "F2";
            chart2.ChartAreas[0].AxisX.Title = "Family";
            chart2.ChartAreas[0].AxisY.Title = "Happiness Score";

            chart2.Titles.Add("Зависимость счастья от семьи");
        }



        private void PlotChart3()
        {
            chart3.Series.Clear();
            chart3.Titles.Clear();

            // Добавление новой области диаграммы
            chart3.ChartAreas.Clear();
            chart3.ChartAreas.Add(new ChartArea("MainArea"));

            double? russiaGenerosity = dataTable.AsEnumerable()
                .Where(r => r["Country"].ToString().Trim().ToLower() == "russia")
                .Select(r => (double?)r["Generosity"])
                .FirstOrDefault();

            if (russiaGenerosity.HasValue)
            {
                MessageBox.Show($"Generosity for Russia: {russiaGenerosity.Value}");
            }
            else
            {
                MessageBox.Show("Generosity for Russia not found or is null.");
                return; // Не строим график, если не нашли значение
            }

            int less = 0, more = 0;

            foreach (DataRow row in dataTable.Rows)
            {
                if (double.TryParse(row["Generosity"].ToString(), out double gen))
                {
                    if (gen > russiaGenerosity.Value)
                        more++;
                    else if (gen < russiaGenerosity.Value)
                        less++;
                }
            }

            // Отображаем значения в MessageBox для отладки
            MessageBox.Show($"Меньше Generosity: {less}, Больше Generosity: {more}");

            var series = new Series
            {
                ChartType = SeriesChartType.Pie,
                IsValueShownAsLabel = true,  // Показываем значения в виде меток
                Label = "#VALX: #VALY"      // Формат для метки: сначала название сектора, затем число
            };

            // Добавление данных в серию для диаграммы
            series.Points.AddXY("Меньше Generosity", less);
            series.Points.AddXY("Больше Generosity", more);

            // Настройка меток для секторов диаграммы
            foreach (var point in series.Points)
            {
                point.Label = $"{point.AxisLabel}: {point.YValues[0]}";  // Форматируем метки с названием и числом
            }

            // Добавление серии на диаграмму
            chart3.Series.Add(series);
            chart3.Titles.Add("Сравнение Generosity с Россией");

            // Обновление графика
            chart3.Invalidate();  // Принудительное обновление графика
            chart3.Update();      // Обновление отображения
        }


        private void button2_Click(object sender, EventArgs e)
        {
            string str = "";
            foreach (DataRow row in dataTable.Rows)
            {
                var values = row.ItemArray.Select(item => item.ToString());
                str += "\n" + string.Join(" | ", values);
            }
            MessageBox.Show(string.Join(" | ", str));
            return;
        }
    }
}
