using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using NPOI.XWPF.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using Tesseract;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Text;

namespace CloudStorage.Services
{
    public interface IDocumentPreviewService
    {
        Task<DocumentPreview> GetPreviewAsync(string filePath, int maxPages = 10);
        Task<string> ExtractTextFromImageAsync(string imagePath);
        bool IsSupportedFormat(string fileName);
    }

    public class DocumentPreview
    {
        public string FileType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<PagePreview> Pages { get; set; } = new();
        public int TotalPages { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
        public string Error { get; set; } = string.Empty;
    }

    public class PagePreview
    {
        public int PageNumber { get; set; }
        public string Content { get; set; } = string.Empty;
        public string ImageBase64 { get; set; } = string.Empty;
    }

    public class DocumentPreviewService : IDocumentPreviewService
    {
        private readonly ILogger<DocumentPreviewService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly string _tesseractDataPath;

        public DocumentPreviewService(
            ILogger<DocumentPreviewService> logger,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
            
            // Setup Tesseract data path - you'll need to download tessdata
            _tesseractDataPath = Path.Combine(_environment.ContentRootPath, "tessdata");
            if (!Directory.Exists(_tesseractDataPath))
            {
                Directory.CreateDirectory(_tesseractDataPath);
                _logger.LogWarning($"Tesseract data directory created at {_tesseractDataPath}. Please download tessdata files.");
            }
        }

        public bool IsSupportedFormat(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => true,
                ".docx" => true,
                ".doc" => true,
                ".xlsx" => true,
                ".xls" => true,
                ".txt" => true,
                ".png" => true,
                ".jpg" => true,
                ".jpeg" => true,
                ".gif" => true,
                ".bmp" => true,
                _ => false
            };
        }

        public async Task<DocumentPreview> GetPreviewAsync(string filePath, int maxPages = 10)
        {
            var preview = new DocumentPreview();
            
            try
            {
                _logger.LogInformation("GetPreviewAsync called with filePath: {FilePath}", filePath);
                
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.LogError("GetPreviewAsync: filePath is null or empty");
                    preview.Error = "File path is empty";
                    return preview;
                }
                
                if (!File.Exists(filePath))
                {
                    _logger.LogError("GetPreviewAsync: File does not exist at {FilePath}", filePath);
                    preview.Error = $"File not found at path: {filePath}";
                    return preview;
                }

                _logger.LogInformation("GetPreviewAsync: File exists at {FilePath}", filePath);
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                preview.FileType = extension.TrimStart('.');
                _logger.LogInformation("GetPreviewAsync: File type is {FileType}", preview.FileType);

                switch (extension)
                {
                    case ".pdf":
                        await PreviewPdfAsync(filePath, preview, maxPages);
                        break;
                    case ".docx":
                        await PreviewDocxAsync(filePath, preview);
                        break;
                    case ".xlsx":
                    case ".xls":
                        await PreviewExcelAsync(filePath, preview, maxPages);
                        break;
                    case ".txt":
                        await PreviewTextAsync(filePath, preview);
                        break;
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".gif":
                    case ".bmp":
                        await PreviewImageAsync(filePath, preview);
                        break;
                    default:
                        preview.Error = "Unsupported file format";
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating preview for {FilePath}", filePath);
                preview.Error = $"Error generating preview: {ex.Message}";
            }

            return preview;
        }

        private async Task PreviewPdfAsync(string filePath, DocumentPreview preview, int maxPages)
        {
            await Task.Run(() =>
            {
                using var document = PdfDocument.Open(filePath);
                preview.TotalPages = document.NumberOfPages;
                preview.Metadata["Title"] = document.Information.Title ?? "N/A";
                preview.Metadata["Author"] = document.Information.Author ?? "N/A";
                preview.Metadata["Creator"] = document.Information.Creator ?? "N/A";

                var pagesToExtract = Math.Min(maxPages, document.NumberOfPages);
                var contentBuilder = new StringBuilder();

                for (int i = 1; i <= pagesToExtract; i++)
                {
                    var page = document.GetPage(i);
                    var text = page.Text;
                    
                    var pagePreview = new PagePreview
                    {
                        PageNumber = i,
                        Content = text
                    };
                    
                    preview.Pages.Add(pagePreview);
                    contentBuilder.AppendLine($"=== Page {i} ===");
                    contentBuilder.AppendLine(text);
                    contentBuilder.AppendLine();
                }

                preview.Content = contentBuilder.ToString();
            });
        }

        private async Task PreviewDocxAsync(string filePath, DocumentPreview preview)
        {
            await Task.Run(() =>
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var document = new XWPFDocument(fs);
                
                var contentBuilder = new StringBuilder();
                
                // Extract metadata
                var properties = document.GetProperties();
                if (properties?.CoreProperties != null)
                {
                    preview.Metadata["Title"] = properties.CoreProperties.Title ?? "N/A";
                    preview.Metadata["Creator"] = properties.CoreProperties.Creator ?? "N/A";
                    preview.Metadata["Subject"] = properties.CoreProperties.Subject ?? "N/A";
                }

                // Extract text from paragraphs
                foreach (var paragraph in document.Paragraphs)
                {
                    contentBuilder.AppendLine(paragraph.Text);
                }

                // Extract text from tables
                foreach (var table in document.Tables)
                {
                    contentBuilder.AppendLine("\n[TABLE]");
                    foreach (var row in table.Rows)
                    {
                        var cells = row.GetTableCells();
                        contentBuilder.AppendLine(string.Join(" | ", cells.Select(c => c.GetText())));
                    }
                    contentBuilder.AppendLine("[/TABLE]\n");
                }

                preview.Content = contentBuilder.ToString();
                preview.TotalPages = 1;
                
                preview.Pages.Add(new PagePreview
                {
                    PageNumber = 1,
                    Content = preview.Content
                });
            });
        }

        private async Task PreviewExcelAsync(string filePath, DocumentPreview preview, int maxSheets)
        {
            await Task.Run(() =>
            {
                IWorkbook workbook;
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                if (extension == ".xlsx")
                {
                    workbook = new XSSFWorkbook(fs);
                }
                else
                {
                    workbook = new HSSFWorkbook(fs);
                }

                var contentBuilder = new StringBuilder();
                preview.TotalPages = workbook.NumberOfSheets;
                
                var sheetsToProcess = Math.Min(maxSheets, workbook.NumberOfSheets);
                
                for (int i = 0; i < sheetsToProcess; i++)
                {
                    var sheet = workbook.GetSheetAt(i);
                    contentBuilder.AppendLine($"=== Sheet: {sheet.SheetName} ===");
                    
                    var sheetContent = new StringBuilder();
                    
                    // Read up to 100 rows per sheet
                    var maxRows = Math.Min(100, sheet.LastRowNum + 1);
                    for (int rowIndex = 0; rowIndex < maxRows; rowIndex++)
                    {
                        var row = sheet.GetRow(rowIndex);
                        if (row != null)
                        {
                            var cells = new List<string>();
                            for (int cellIndex = 0; cellIndex < row.LastCellNum; cellIndex++)
                            {
                                var cell = row.GetCell(cellIndex);
                                cells.Add(GetCellValue(cell));
                            }
                            sheetContent.AppendLine(string.Join("\t", cells));
                        }
                    }
                    
                    var pageContent = sheetContent.ToString();
                    contentBuilder.AppendLine(pageContent);
                    contentBuilder.AppendLine();
                    
                    preview.Pages.Add(new PagePreview
                    {
                        PageNumber = i + 1,
                        Content = pageContent
                    });
                }

                preview.Content = contentBuilder.ToString();
            });
        }

        private string GetCellValue(NPOI.SS.UserModel.ICell? cell)
        {
            if (cell == null) return string.Empty;

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue,
                CellType.Numeric => DateUtil.IsCellDateFormatted(cell) 
                    ? $"{cell.DateCellValue:yyyy-MM-dd}" 
                    : $"{cell.NumericCellValue}",
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => cell.CachedFormulaResultType switch
                {
                    CellType.String => cell.StringCellValue,
                    CellType.Numeric => $"{cell.NumericCellValue}",
                    _ => string.Empty
                },
                _ => string.Empty
            };
        }

        private async Task PreviewTextAsync(string filePath, DocumentPreview preview)
        {
            var content = await File.ReadAllTextAsync(filePath);
            
            // Limit to first 50KB for preview
            if (content.Length > 50000)
            {
                content = content.Substring(0, 50000) + "\n\n... [Content truncated for preview]";
            }

            preview.Content = content;
            preview.TotalPages = 1;
            preview.Pages.Add(new PagePreview
            {
                PageNumber = 1,
                Content = content
            });
        }

        private async Task PreviewImageAsync(string filePath, DocumentPreview preview)
        {
            await Task.Run(async () =>
            {
                // Convert image to base64 for display
                using var image = await Image.LoadAsync(filePath);
                
                // Resize if too large (max 1200px width)
                if (image.Width > 1200)
                {
                    image.Mutate(x => x.Resize(1200, 0));
                }

                preview.Metadata["Width"] = image.Width.ToString();
                preview.Metadata["Height"] = image.Height.ToString();
                
                // Get format from file extension
                var ext = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();
                preview.Metadata["Format"] = ext;

                // Convert to base64
                using var ms = new MemoryStream();
                await image.SaveAsJpegAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());

                preview.Pages.Add(new PagePreview
                {
                    PageNumber = 1,
                    ImageBase64 = base64
                });

                preview.TotalPages = 1;

                // Try OCR if Tesseract is available
                try
                {
                    var ocrText = await ExtractTextFromImageAsync(filePath);
                    if (!string.IsNullOrWhiteSpace(ocrText))
                    {
                        preview.Content = ocrText;
                        preview.Pages[0].Content = ocrText;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OCR not available or failed for {FilePath}", filePath);
                }
            });
        }

        public async Task<string> ExtractTextFromImageAsync(string imagePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(_tesseractDataPath) || 
                        !Directory.GetFiles(_tesseractDataPath, "*.traineddata").Any())
                    {
                        _logger.LogWarning("Tesseract data not found. OCR disabled.");
                        return string.Empty;
                    }

                    using var engine = new TesseractEngine(_tesseractDataPath, "eng", EngineMode.Default);
                    using var img = Pix.LoadFromFile(imagePath);
                    using var page = engine.Process(img);
                    
                    var text = page.GetText();
                    var confidence = page.GetMeanConfidence();
                    
                    _logger.LogInformation("OCR completed with {Confidence}% confidence", confidence * 100);
                    
                    return text;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error performing OCR on {ImagePath}", imagePath);
                    return string.Empty;
                }
            });
        }
    }
}
