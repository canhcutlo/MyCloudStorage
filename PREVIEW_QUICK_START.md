# Document Preview - Quick Start Guide

## 🚀 Getting Started (2 Minutes)

### Step 1: Build and Run
```powershell
# From project root
dotnet build
dotnet run
```

### Step 2: Test Basic Preview (No OCR Required)
1. Navigate to http://localhost:5000 (or your configured port)
2. Login to your account
3. Upload a PDF, Word document, or Excel file
4. Click the **👁️ eye icon** next to the file
5. View the preview with zoom and navigation controls

**That's it!** The basic preview feature works immediately without any additional configuration.

---

## 📷 Optional: Enable OCR for Images (5 Minutes)

If you want to extract text from images (screenshots, scanned documents, photos with text):

### Quick Setup:
```powershell
# Run these commands from project root
New-Item -ItemType Directory -Path "tessdata" -Force
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata" -OutFile "tessdata\eng.traineddata"
```

### Verify Setup:
1. Check that `tessdata/eng.traineddata` file exists (size: ~4 MB)
2. Restart the application
3. Upload an image with text
4. Preview will now show extracted text below the image

---

## 📁 Supported File Types

| File Type | Extension | Features |
|-----------|-----------|----------|
| PDF | `.pdf` | ✅ Text extraction, page navigation, metadata |
| Word | `.docx` | ✅ Text and table extraction |
| Excel | `.xlsx`, `.xls` | ✅ Multi-sheet preview, 100 rows per sheet |
| Images | `.png`, `.jpg`, `.jpeg`, `.gif`, `.bmp` | ✅ Display, optional OCR |
| Text | `.txt` | ✅ Plain text display |

---

## 🎮 Using the Preview

### Navigation:
- **Zoom In/Out**: Click +/- buttons (50% to 200%)
- **Pages**: Use Previous/Next for multi-page documents
- **View Mode**: Toggle between text and image view (for images with OCR)
- **Download**: Get original file
- **Back**: Return to file list

### Keyboard Shortcuts (Coming Soon):
- `←` / `→` : Previous/Next page
- `+` / `-` : Zoom in/out
- `Esc` : Close preview

---

## 🐛 Troubleshooting

### Preview button not showing?
- Ensure file is uploaded successfully
- Check that file type is supported
- Refresh the page

### Preview shows error?
- Check file is not corrupted
- Try re-uploading the file
- Check browser console for errors

### OCR not working?
- Verify `tessdata/eng.traineddata` exists
- Check file size is ~4 MB (not 404 error page)
- Restart application after adding tessdata
- Check application logs for Tesseract errors

### Preview is slow?
- Large files (>50MB) may take longer
- Consider implementing file size limits
- Check server memory usage

---

## 📖 Examples

### Preview a PDF Report:
1. Upload: `Monthly_Report_December.pdf`
2. Click 👁️ icon
3. Navigate through pages
4. Zoom in to read details
5. Download if needed

### Preview an Excel Spreadsheet:
1. Upload: `Sales_Data_2025.xlsx`
2. Click 👁️ icon
3. View first sheet
4. Navigate to other sheets if available
5. See cell data in table format

### Preview an Image with Text:
1. Upload: `screenshot_2025.png`
2. Click 👁️ icon
3. View high-quality image
4. Read extracted text below (if OCR enabled)
5. Zoom to see details

---

## 🔗 Related Documentation

- **Full Setup Guide**: See `DOCUMENT_PREVIEW_SETUP.md`
- **Implementation Details**: See `DOCUMENT_PREVIEW_IMPLEMENTATION.md`
- **Main README**: See `README.md`

---

## ⚡ Quick Tips

1. **Performance**: Preview is limited to first 10-20 pages for PDFs
2. **Security**: Only file owners can preview their files
3. **Mobile**: Works on mobile devices with touch gestures
4. **Caching**: Previews are generated on-demand (not cached yet)
5. **Formats**: More formats coming soon (PowerPoint, CSV, etc.)

---

## 🆘 Need Help?

### Common Questions:

**Q: Can I preview PowerPoint files?**
A: Not yet. PowerPoint support is planned for a future update.

**Q: How many pages can I preview?**
A: PDFs are limited to 10-20 pages by default. You can adjust this in the code.

**Q: Does preview work offline?**
A: Yes, but OCR requires the tessdata files to be present locally.

**Q: Can I share preview links?**
A: Not yet. Shared preview links are planned for a future update.

**Q: Is there a file size limit?**
A: No hard limit, but very large files (>100MB) may have performance issues.

---

**Ready to Preview?** Upload a file and click the 👁️ icon!
