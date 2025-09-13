# Vocal 🎵

A real-time vocal pitch detection application that converts your voice into MIDI signals. Sing, hum, or whistle into your microphone and watch as your voice is transformed into playable MIDI notes with visual feedback.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)

## Features

- **Real-time pitch detection** using advanced signal processing algorithms
- **MIDI output** via virtual MIDI device for use with any DAW or music software
- **Visual feedback** with real-time note display and frequency information
- **Smart stabilization** to prevent note flickering and false triggers
- **Loudness-based note triggering** - only sends MIDI when you're actually singing
- **Low latency** audio processing for responsive performance

## How It Works

1. **Audio Capture**: Records audio from your default microphone using NAudio
2. **Pitch Detection**: Uses NWaves pitch extraction with autocorrelation algorithms
3. **Stabilization**: Applies multiple filtering techniques to ensure stable note detection
4. **MIDI Generation**: Converts detected pitches to MIDI note-on/note-off messages
5. **Visual Display**: Shows current note, MIDI number, and frequency in real-time

## Requirements

### System Requirements
- Windows 10/11 (for teVirtualMIDI driver)
- .NET 8.0 Runtime
- Audio input device (microphone)

### Dependencies
- **NAudio** - Audio capture and MIDI handling
- **NWaves** - Digital signal processing and pitch extraction
- **Raylib-cs** - Real-time graphics and window management
- **teVirtualMIDI** - Virtual MIDI port creation

## Installation

1. **Install teVirtualMIDI Driver**
    - Download from [Tobias Erichsen's website](https://www.tobias-erichsen.de/software/virtualmidi.html)
    - Install the driver (required for virtual MIDI functionality)

2. **Clone the Repository**
   ```bash
   git clone https://github.com/yourusername/vocal.git
   cd vocal
   ```

3. **Build the Project**
   ```bash
   dotnet build
   ```

4. **Run the Application**
   ```bash
   dotnet run
   ```

## Usage

### Getting Started
1. Launch the application
2. A virtual MIDI device named "Vocal" will be created
3. Start singing, humming, or whistling into your microphone
4. Watch the real-time note detection in the application window

### Connecting to Music Software
The application creates a virtual MIDI device that appears as "Vocal" in your system. You can connect this to any MIDI-compatible software:

- **DAWs**: Ableton Live, FL Studio, Logic Pro, Cubase, etc.
- **Virtual Instruments**: Any VST/AU instrument or sampler
- **MIDI Utilities**: MIDI monitoring tools, loopers, etc.

### Controls
- **Close Window**: ESC key or window close button
- **Audio Input**: Automatically uses default microphone (configurable in code)

## Configuration

### Audio Settings
```csharp
private const int SampleRate = 44100;      // Audio sample rate
private const float SampleTime = 0.05f;    // Processing buffer size (50ms)
private const int MinStableFrames = 15;    // Stability requirement
```

### Pitch Detection Settings
```csharp
var stabilizer = new StabilizedPitchDetector(
    historySize: 5,           // Number of previous samples to consider
    stabilityThreshold: 5.0,  // Frequency tolerance in Hz
    minStableFrames: 3,       // Frames needed for stable detection
    smoothingSize: 3          // Smoothing window size
);
```

### Sensitivity Adjustment
```csharp
var loudness = (int)(sbyte.MaxValue * MathF.Max(0, MathF.Min(Rms(block) / 0.1f, 1)));
```
Adjust the `0.1f` value to change microphone sensitivity (lower = more sensitive).

## Technical Details

### Architecture
```
Microphone → NAudio → Circular Buffer → NWaves Pitch Extractor → 
Stabilization → MIDI Conversion → Virtual MIDI Device → Your DAW
```

### Pitch Detection Algorithm
- Uses **autocorrelation** for fundamental frequency detection
- Applies **Hamming windowing** to reduce spectral leakage
- Implements **multi-stage stabilization**:
    - Median filtering for noise reduction
    - Hysteresis to prevent rapid note changes
    - Majority voting from recent history
    - Confidence-based note switching

### MIDI Implementation
- **Note Range**: MIDI 0-127 (C-1 to G9)
- **Velocity**: Based on input loudness/RMS level
- **Channel**: 1 (configurable)
- **Polyphonic**: Single note at a time (monophonic)

## Troubleshooting

### Common Issues

**No MIDI Output**
- Ensure teVirtualMIDI driver is installed
- Check that "Vocal" appears in your DAW's MIDI input list
- Verify the application is running without errors

**Poor Pitch Detection**
- Sing clearly and steadily
- Reduce background noise
- Adjust microphone sensitivity
- Try singing in a comfortable vocal range (C3-C6)

**Audio Issues**
- Check microphone permissions
- Verify correct audio input device
- Ensure microphone is not muted or too quiet

**Performance Issues**
- Close unnecessary applications
- Reduce buffer size if experiencing latency
- Check CPU usage during operation

### Debug Information
The application displays real-time information:
- **FPS**: Rendering performance
- **Note Name**: Current detected note (e.g., "A4")
- **MIDI Number**: MIDI note value (0-127)
- **Frequency**: Detected pitch in Hz

## Development

### Building from Source
```bash
git clone https://github.com/yourusername/vocal.git
cd vocal
dotnet restore
dotnet build --configuration Release
```

### Project Structure
```
Vocal/
├── Program.cs              # Main application logic
├── CircularArray.cs        # Audio buffer implementation
├── StabilizedPitchDetector.cs  # Pitch stabilization
├── MidiHelper.cs          # MIDI utility functions
├── Vocal.csproj           # Project configuration
└── README.md              # This file
```

### Contributing
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- **NAudio** - Excellent .NET audio library
- **NWaves** - Comprehensive digital signal processing
- **Raylib** - Simple and powerful graphics library
- **teVirtualMIDI** - Essential for Windows MIDI routing
- **Tobias Erichsen** - For the fantastic virtual MIDI driver

## Related Projects

- [NAudio](https://github.com/naudio/NAudio) - .NET audio library
- [NWaves](https://github.com/ar1st0crat/NWaves) - .NET digital signal processing
- [Raylib-cs](https://github.com/ChrisDill/Raylib-cs) - C# bindings for Raylib

---

**Happy Singing!** 🎤✨