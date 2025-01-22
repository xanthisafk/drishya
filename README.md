# दृश्य (Drishya)

A specialized video sorting and screenshot tool built with WPF, LibVLCSharp and FFMpegCore.

## Features

- **Video Processing**
  - Browse and sort videos from selected folders
  - Rename files with timestamps and custom postfix
  - Move processed videos to designated folders
  - Support for major video formats (mp4, mkv, avi, mov, etc.)
  - Drag and drop support (Coming Soon)
  - Smart tag suggestions (Coming Soon)

- **Video Playback**
  - Full media controls (play/pause, seek, volume)
  - Keyboard shortcuts (Coming Soon)
  - Mute on sort (Coming Soon)

- **Screenshot Capabilities**
  - Single screenshot capture
  - Batch screenshot extraction with customizable intervals:
    - Every frame
    - Every 0.1 to 30 seconds
  - Automatic subfolder creation per video

## Requirements

- Windows OS
- .NET 8.0
- VLC Media Player

## Installation

1. Install VLC Media Player
2. Install FFMpeg
3. Clone repo
4. Build and run

## Usage

### Video Sorting

1. Click "Browse" to select a video folder
2. Enable/disable "Include already sorted" as needed
3. Click "Search" to load videos
4. Use playback controls to preview videos
5. Enter new name (optional)
6. Click "Process" to sort/rename the video

### Screenshots

1. Select screenshot interval from dropdown
2. Choose save location
3. Enable/disable subfolder creation
4. Click "Take Screenshots" for batch capture or camera icon for single shot

## Settings

All preferences are automatically saved.

## Development

Built using:
- C# / WPF
- LibVLCSharp
- FFMpegCore
- MaterialDesign

## License

[MIT License](LICENSE.txt)