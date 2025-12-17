# Document Preview Feature - Setup Guide

## Overview
This feature provides comprehensive document and image preview capabilities using:
- **UglyToad.PdfPig** - PDF text extraction
- **NPOI** - Office document (Word, Excel) reading
- **Tesseract** - OCR for image text extraction
- **Microsoft.KernelMemory** - Semantic search capabilities (future enhancement)

## Supported File Formats
- **PDF**: Text extraction, page-by-page preview
- **Word**: `.docx` files with text and table extraction
- **Excel**: `.xlsx` and `.xls` files with sheet preview
- **Images**: `.png`, `.jpg`, `.jpeg`, `.gif`, `.bmp` with OCR
- **Text**: `.txt` files with plain text display

## Tesseract OCR Setup

Tesseract is required for OCR (Optical Character Recognition) on images. Follow these steps:

### 1. Download Tesseract Language Data

Download the English language data file:
- Visit: https://github.com/tesseract-ocr/tessdata
- Download: `eng.traineddata`
- For better accuracy, use tessdata_best: https://github.com/tesseract-ocr/tessdata_best

### 2. Create tessdata Directory

Create the directory structure in your project:
```
MyCloudStorage/
  └── tessdata/
      └── eng.traineddata
```

### 3. PowerShell Setup Command

Run this from the project root:
```powershell
# Create directory
New-Item -ItemType Directory -Path "tessdata" -Force

# Download English trained data (fast version)
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata" -OutFile "tessdata\eng.traineddata"
```

### 4. Alternative: Best Quality Data

For better OCR accuracy (but slower):
```powershell
# Download best quality English data (larger file, better accuracy)
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata_best/raw/main/eng.traineddata" -OutFile "tessdata\eng.traineddata"
```

### 5. Additional Languages (Optional)

To support more languages, download additional `.traineddata` files:
```powershell
# Spanish
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/spa.traineddata" -OutFile "tessdata\spa.traineddata"

# French
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/fra.traineddata" -OutFile "tessdata\fra.traineddata"

# German
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/deu.traineddata" -OutFile "tessdata\deu.traineddata"
```

## Features

### 1. PDF Preview
- Extracts text from all pages
- Displays metadata (title, author, creator)
- Page-by-page navigation
- Zoom controls

### 2. Word Document Preview
- Extracts text from paragraphs
- Extracts tables with formatting
- Displays document metadata
- Supports `.docx` format (OpenXML)

### 3. Excel Preview
- Displays multiple sheets
- Shows data in tabular format
- Supports both `.xlsx` and `.xls`
- Handles formulas and dates
- Limits to 100 rows per sheet for performance

### 4. Image Preview
- High-quality image display
- Automatic resizing for web display
- OCR text extraction (if Tesseract is configured)
- Shows image dimensions and format

### 5. Text File Preview
- Direct display of text content
- Syntax highlighting (future enhancement)
- Truncates large files for performance

## Usage

### From Storage View
1. Navigate to your files in Storage
2. Click the eye icon (👁️) next to any supported file
3. The preview will open in a dedicated view

### Preview Controls
- **Zoom**: Use +/- buttons or zoom level selector
- **Pages**: Navigate using previous/next buttons
- **View Mode**: Toggle between text and image view (for images with OCR)
- **Download**: Download the original file
- **Back**: Return to file list

## API Endpoint

### GET /Preview/GetPreview
```
Parameters:
  - id: File item ID (required)
  - maxPages: Maximum pages to extract (default: 10)

Returns:
  - success: boolean
  - fileName: string
  - fileType: string
  - content: string (full text content)
  - pages: array of page objects
  - totalPages: number
  - metadata: object with file metadata
```

## Performance Considerations

1. **Large PDF Files**: Preview is limited to first 10-20 pages by default
2. **Excel Files**: Only first 100 rows per sheet are displayed
3. **Text Files**: Truncated at 50KB for preview
4. **Images**: Automatically resized if width > 1200px
5. **OCR**: Only performed if Tesseract is properly configured

## Troubleshooting

### OCR Not Working
- Ensure `tessdata` directory exists in project root
- Verify `eng.traineddata` file is present
- Check file permissions
- Review application logs for Tesseract errors

### Preview Errors
- Check that file exists and is not corrupted
- Verify file format is supported
- Check browser console for JavaScript errors
- Review server logs for exception details

### Memory Issues
- Large files may cause high memory usage
- Consider implementing file size limits
- Use pagination for large documents
- Implement caching for frequently accessed files

## Security Notes

1. **Access Control**: Preview only available for file owners
2. **Path Validation**: All file paths are validated
3. **File Type Checking**: Only supported formats are processed
4. **Size Limits**: Consider implementing max file size for preview
5. **Sanitization**: HTML content is escaped to prevent XSS

## Future Enhancements

1. **Kernel Memory Integration**: Semantic search within documents
2. **Syntax Highlighting**: Code file preview with highlighting
3. **Video/Audio**: Media player integration
4. **Annotations**: Allow users to add notes to documents
5. **Collaborative Viewing**: Real-time shared viewing
6. **Full-Text Search**: Search within document content
7. **Document Comparison**: Side-by-side comparison tool
8. **Export Options**: Convert between formats

## Dependencies

```xml
<PackageReference Include="UglyToad.PdfPig" Version="0.1.10" />
<PackageReference Include="NPOI" Version="2.7.5" />
<PackageReference Include="Tesseract" Version="5.2.0" />
<PackageReference Include="Microsoft.KernelMemory.Core" Version="0.98.250508.3" />
<PackageReference Include="SixLabors.ImageSharp" Version="2.1.11" />
```

## Testing

Test the preview feature with various file types:
- Small and large PDFs
- Word documents with tables and images
- Excel files with multiple sheets
- Images with and without text
- Text files of various sizes

## Notes

- UglyToad.PdfPig currently shows as prerelease (0.1.10) but is stable
- NPOI supports most Office formats but has limitations with complex formatting
- Tesseract accuracy depends on image quality and language data
- Microsoft.KernelMemory is included for future semantic search features
