//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.IO;
//using System.Windows;
//using ClosedXML.Excel;
//using static Platezh.Views.Casher;

//namespace Platezh.Services
//{
//    public class ExcelService
//    {
//        private readonly string _templatePath;
//        public string folderPath;
//        public ExcelService()
//        {
//            folderPath = "";
//            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
//            var templateDir = Path.Combine(baseDir, @"..\..\..\Templates");
//            _templatePath = Path.Combine(templateDir, "serviceContract.xlsx");
//        }

//        public void FillContract(List<Service> services, List<Material> materials, ContractData data, string saveDirectory)
//        {
//            folderPath = saveDirectory;
//            if (!File.Exists(_templatePath))
//                throw new FileNotFoundException("Шаблон не найден", _templatePath);

//            using (var workbook = new XLWorkbook(_templatePath))
//            {
//                var worksheet = workbook.Worksheet("Платные услуги");
//                if (worksheet == null)
//                    throw new Exception("Лист 'Платные услуги' не найден.");

//                worksheet.Cell("C1").Value = data.contractNumber;
//                worksheet.Cell("E5").Value = data.clientName;
//                worksheet.Cell("E58").Value = data.passportNumber;
//                worksheet.Cell("E59").Value = data.issuedBy;
//                worksheet.Cell("E60").Value = data.dateIssued?.ToShortDateString() ?? "";
//                worksheet.Cell("E61").Value = data.address;
//                worksheet.Cell("G3").Value = DateTime.Today.ToString("dd.MM.yyyy");

//                int startRow = 11;
//                var templateRow = worksheet.Row(startRow);
//                var culture = CultureInfo.GetCultureInfo("ru-RU");

//                decimal totalSum = 0;
//                int currentRow = startRow;
//                void CopyRowStyleAndBorders(IXLRow sourceRow, IXLRow targetRow)
//                {
//                    for (int col = 1; col <= 7; col++)
//                    {
//                        var sourceCell = sourceRow.Cell(col);
//                        var targetCell = targetRow.Cell(col);

//                        // Копирование базовых стилей
//                        targetCell.Style.Font = sourceCell.Style.Font;
//                        targetCell.Style.Alignment = sourceCell.Style.Alignment;
//                        targetCell.Style.Fill = sourceCell.Style.Fill;
//                        targetCell.Style.NumberFormat = sourceCell.Style.NumberFormat;

//                        // Принудительно выставляем границы ячейке (все 4 стороны)
//                        var border = targetCell.Style.Border;
//                        border.OutsideBorder = XLBorderStyleValues.Thin;
//                        border.OutsideBorderColor = XLColor.Black;
//                        border.InsideBorder = XLBorderStyleValues.Thin;
//                        border.InsideBorderColor = XLColor.Black;
//                    }
//                }



//                for (int i = 0; i < services.Count; i++)
//                {
//                    var row = worksheet.Row(currentRow);
//                    templateRow.CopyTo(row);
//                    CopyRowStyleAndBorders(templateRow, row);

//                    row.Cell(1).Value = services[i].id;
//                    row.Cell(2).Value = services[i].name;
//                    row.Cell(3).Value = services[i].count;
//                    row.Cell(4).Value = services[i].tariff;
//                    row.Cell(5).Value = services[i].additionalMaterialCost;
//                    row.Cell(6).Value = "";
//                    row.Cell(7).Value = services[i].totalCost;

//                    row.Cell(4).Style.NumberFormat.Format = "#,##0.00";
//                    row.Cell(5).Style.NumberFormat.Format = "#,##0.00";
//                    row.Cell(7).Style.NumberFormat.Format = "#,##0.00";

//                    totalSum += services[i].totalCost;
//                    currentRow++;
//                }

//                for (int i = 0; i < materials.Count; i++)
//                {
//                    var row = worksheet.Row(currentRow);
//                    templateRow.CopyTo(row);
//                    CopyRowStyleAndBorders(templateRow, row);

//                    row.Cell(1).Value = materials[i].id;
//                    row.Cell(2).Value = materials[i].name;
//                    row.Cell(3).Value = materials[i].count;
//                    row.Cell(4).Value = ""; // Нет тарифа
//                    row.Cell(5).Value = materials[i].totalPrice * materials[i].count;
//                    row.Cell(6).Value = materials[i].nds * materials[i].count;
//                    row.Cell(7).Value = materials[i].totalPrice * materials[i].count;

//                    row.Cell(5).Style.NumberFormat.Format = "#,##0.00";
//                    row.Cell(6).Style.NumberFormat.Format = "#,##0.00";
//                    row.Cell(7).Style.NumberFormat.Format = "#,##0.00";

//                    totalSum += materials[i].totalPrice * materials[i].count;
//                    currentRow++;
//                }

//                string sumInWords = ConvertSumToWords.NumberToWordsConverter.ConvertToWords(totalSum);
//                worksheet.Cell("A28").Value = sumInWords;

//                var safeClientName = string.Concat(data.clientName.Split(Path.GetInvalidFileNameChars()));
//                var fileName = $"{safeClientName}_{data.contractNumber}.xlsx";
//                var fullPath = Path.Combine(saveDirectory, fileName);

//                workbook.SaveAs(fullPath);

//                var showContract = MessageBox.Show(
//                    "Открыть договор в Excel?",
//                    "Подтверждение открытия",
//                    MessageBoxButton.YesNo,
//                    MessageBoxImage.Warning
//                );

//                if (showContract == MessageBoxResult.Yes)
//                {
//                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
//                    {
//                        FileName = fullPath,
//                        UseShellExecute = true
//                    });
//                }
//            }
//        }



//    }


//    public class Material
//    {
//        public int id { get; set; }
//        public string name { get; set; }
//        public int count { get; set; }
//        public decimal nds { get; set; }
//        public decimal totalPrice { get; set; }
//    }

//    public class Service
//    {
//        public int id { get; set; }
//        public string name { get; set; }
//        public int count { get; set; }
//        public decimal tariff { get; set; }
//        public decimal additionalMaterialCost { get; set; }
//        public decimal totalCost { get; set; }
//    }
//}
