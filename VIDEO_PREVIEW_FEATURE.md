# Video Preview Feature

## Overview
The video preview feature allows users to play video files directly in the browser without downloading them. It provides a comprehensive video player with modern controls and features.

## Supported Video Formats
- MP4 (.mp4, .m4v)
- WebM (.webm)
- OGG (.ogg)
- QuickTime (.mov)
- AVI (.avi)
- Matroska (.mkv)
- Windows Media Video (.wmv)
- Flash Video (.flv)

**Note:** Browser codec support varies. MP4, WebM, and OGG have the best cross-browser compatibility. Some formats like AVI and MKV may require specific browser plugins or may not play in all browsers.

## Features

### Video Player Interface
- **Left Sidebar**: File information and metadata
  - File name
  - File type badge
  - File size
  - Creation date
  - Modification date
  - Video duration (displayed after loading)
  - Video resolution (displayed after loading)
  - Download button
  - Back to files button

- **Center Area**: HTML5 video player
  - Custom styled video controls
  - Playback controls (play, pause, seek, volume)
  - Progress bar with seeking
  - Time display
  - Volume control

- **Top Toolbar**:
  - Theater mode button
  - Fullscreen button

### Keyboard Shortcuts
- **Space**: Play/Pause
- **Left Arrow**: Skip backward 5 seconds
- **Right Arrow**: Skip forward 5 seconds
- **Up Arrow**: Increase volume
- **Down Arrow**: Decrease volume
- **F**: Toggle fullscreen
- **M**: Mute/Unmute
- **Escape**: Exit theater mode

### Viewing Modes

#### Normal Mode
Standard video player view with sidebar and controls.

#### Theater Mode
- Expands video player to cover the entire viewport
- Removes sidebar for distraction-free viewing
- Dark background for better focus
- Press Escape or click "Exit Theater Mode" to return to normal view

#### Fullscreen Mode
- Browser native fullscreen
- Press F or fullscreen button to enter
- Press Escape or F to exit

## Technical Implementation

### Backend Components

#### PreviewController Actions

**Video Action** (`/Preview/Video/{id}`)
- Validates user authentication
- Checks file access permissions
- Verifies video file format
- Logs activity (video view)
- Returns video player view

**GetVideoStream Action** (`/Preview/GetVideoStream/{id}`)
- Streams video file with HTTP range request support
- Enables seeking and buffering
- Sets appropriate MIME type
- Returns FileStreamResult with range processing

**GetVideoMimeType Helper**
- Maps file extensions to proper video MIME types
- Ensures correct content type for browser playback

#### DocumentPreviewService Updates
- Extended `IsSupportedFormat()` to include video formats
- Returns true for 9 video file extensions

#### StorageController Integration
- Document preview now detects video files
- Automatically redirects to dedicated video player

### Frontend Components

#### Video.cshtml View
- Razor view with HTML5 video element
- Bootstrap 5 responsive layout
- Font Awesome icons
- Custom CSS for theater mode
- JavaScript for:
  - Metadata extraction (duration, resolution)
  - Theater mode toggle
  - Fullscreen toggle
  - Keyboard shortcuts
  - Error handling

### Activity Logging
Video views are logged in the activity feed with:
- Activity Type: ViewedDocument
- Description: "Viewed video: {filename}"
- User information
- IP address
- User agent
- Timestamp

## Usage

### For End Users

1. **Navigate to File**
   - Go to Storage section
   - Find a video file

2. **Preview Video**
   - Click on the video file name
   - Video player opens automatically

3. **Playback Controls**
   - Use on-screen controls or keyboard shortcuts
   - Click theater mode for immersive viewing
   - Click fullscreen for full-screen experience

4. **View Metadata**
   - Duration and resolution appear after video loads
   - File information displayed in sidebar

5. **Download**
   - Click "Download" button in sidebar to save video

### For Developers

**Adding New Video Format Support:**

1. Update `DocumentPreviewService.cs`:
```csharp
public static bool IsSupportedFormat(string fileName)
{
    var extension = Path.GetExtension(fileName).ToLowerInvariant();
    return extension switch
    {
        // Add new extension here
        ".newformat" => true,
        // ...existing formats...
    };
}
```

2. Update `GetVideoMimeType()` in `PreviewController.cs`:
```csharp
private string GetVideoMimeType(string fileName)
{
    var extension = Path.GetExtension(fileName).ToLowerInvariant();
    return extension switch
    {
        // Add MIME type mapping
        ".newformat" => "video/newformat",
        // ...existing mappings...
    };
}
```

3. Update supported formats list in this documentation

**Customizing Video Player:**

Edit `Views/Preview/Video.cshtml`:
- Modify HTML structure for layout changes
- Update JavaScript for behavior changes
- Adjust CSS in `<style>` section for appearance

**Activity Logging:**

Video views are automatically logged. To customize:
```csharp
await _activityService.LogActivityAsync(
    item.Id,
    userId,
    ActivityType.ViewedDocument,
    $"Viewed video: {item.Name}",
    HttpContext.Connection.RemoteIpAddress?.ToString(),
    HttpContext.Request.Headers["User-Agent"].ToString()
);
```

## Browser Compatibility

### Full Support
- **MP4**: All modern browsers
- **WebM**: Chrome, Firefox, Opera, Edge
- **OGG**: Firefox, Chrome, Opera

### Limited Support
- **MOV**: Safari (native), others may require plugins
- **AVI**: Requires codec support, varies by browser
- **MKV**: Limited native support, may need plugins
- **WMV**: Limited support, Windows/IE preferred
- **FLV**: Deprecated, not recommended

### Recommendations
- Use MP4 (H.264 codec) for best compatibility
- Provide WebM as fallback for Firefox
- Convert other formats to MP4/WebM for web use

## Security Considerations

1. **Authentication Required**
   - Users must be logged in to preview videos

2. **Authorization Checks**
   - File ownership verified
   - Shared file access validated
   - Public file access allowed for public files

3. **Activity Logging**
   - All video views tracked
   - IP address and user agent recorded
   - Audit trail for compliance

4. **File Validation**
   - Format verification before streaming
   - File existence check
   - Access permission validation

## Performance Optimization

### Range Request Support
- Videos stream with HTTP range requests
- Enables seeking without full download
- Reduces bandwidth usage
- Faster initial playback

### Lazy Loading
- Video metadata loads after player initialization
- Reduces initial page load time
- JavaScript extracts duration and resolution on demand

### Efficient Streaming
- FileStreamResult with `enableRangeProcessing: true`
- Browser caches video segments
- Smooth playback even on slower connections

## Troubleshooting

### Video Won't Play
- **Check format**: Ensure browser supports the format
- **Check codecs**: Some containers need specific codecs
- **Try different browser**: Test in Chrome or Firefox
- **Convert file**: Use MP4 with H.264 for best compatibility

### Seeking Not Working
- Ensure server supports range requests (already implemented)
- Check video file integrity
- Try re-uploading the video

### Theater Mode Issues
- Press Escape to exit theater mode
- Refresh page if display glitches occur

### Metadata Not Showing
- Wait for video to load metadata
- Check browser console for errors
- Ensure video file has proper encoding

## Future Enhancements

Potential improvements for future versions:

1. **Playback Speed Control**
   - 0.5x, 1x, 1.25x, 1.5x, 2x options

2. **Subtitle Support**
   - Upload .srt or .vtt files
   - Display captions/subtitles

3. **Video Thumbnails**
   - Generate preview thumbnails
   - Show in file list

4. **Quality Selection**
   - Multiple quality options
   - Adaptive bitrate streaming

5. **Playlist Support**
   - Play multiple videos sequentially
   - Auto-advance to next video

6. **Picture-in-Picture**
   - Watch while browsing other files
   - Native browser PiP API

7. **Video Analytics**
   - Track watch time
   - Completion rate
   - Popular videos dashboard

8. **Sharing Controls**
   - Share specific timestamp
   - Generate embeddable player
   - Social media sharing

## Files Modified/Created

### Backend Files
- `Services/DocumentPreviewService.cs` - Added video format detection
- `Controllers/PreviewController.cs` - Added Video and GetVideoStream actions

### Frontend Files
- `Views/Preview/Video.cshtml` - New video player view

### Documentation
- `VIDEO_PREVIEW_FEATURE.md` - This file

## Related Features
- [Document Preview](DOCUMENT_PREVIEW_IMPLEMENTATION.md)
- [Activity Feed](ACTIVITY_FEED_FEATURE.md)
- [File Sharing](SHARING_FEATURES_DOCUMENTATION.md)

## Testing Checklist

- [ ] Upload video files (MP4, WebM, OGG)
- [ ] Click to preview video
- [ ] Verify video plays
- [ ] Test playback controls
- [ ] Test seeking (skip forward/backward)
- [ ] Test volume controls
- [ ] Test keyboard shortcuts
- [ ] Test theater mode
- [ ] Test fullscreen mode
- [ ] Verify metadata display (duration, resolution)
- [ ] Test download button
- [ ] Check activity logging
- [ ] Test with shared videos
- [ ] Test browser compatibility
- [ ] Test mobile responsive layout

## Conclusion

The video preview feature provides a professional, user-friendly way to view videos within the cloud storage application. With support for multiple formats, keyboard shortcuts, and viewing modes, it enhances the user experience while maintaining security and performance.
