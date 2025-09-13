namespace Vocal;

public static class MidiHelper
{
    private static readonly string[] NoteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        
    /// <summary>
    /// Convert frequency to MIDI note number (0-127)
    /// </summary>
    public static int FrequencyToMidiNote(double frequency)
    {
        if (frequency <= 0) return -1;
            
        // MIDI note 69 = A4 = 440 Hz
        double midiNote = 69 + 12 * Math.Log2(frequency / 440.0);
        int roundedNote = (int)Math.Round(midiNote);
            
        // Clamp to valid MIDI range
        return Math.Max(0, Math.Min(127, roundedNote));
    }
        
    /// <summary>
    /// Convert MIDI note number to frequency
    /// </summary>
    public static double MidiNoteToFrequency(int midiNote)
    {
        if (midiNote < 0 || midiNote > 127) return 0;
            
        // MIDI note 69 = A4 = 440 Hz
        return 440.0 * Math.Pow(2, (midiNote - 69) / 12.0);
    }
        
    /// <summary>
    /// Convert MIDI note number to note name with octave (e.g., "A4", "C#3")
    /// </summary>
    public static string MidiNoteToNoteName(int midiNote)
    {
        if (midiNote < 0 || midiNote > 127) return "N/A";
            
        int noteIndex = midiNote % 12;
        int octave = (midiNote / 12) - 1;
            
        return $"{NoteNames[noteIndex]}{octave}";
    }
        
    /// <summary>
    /// Get just the note name without octave (e.g., "A", "C#")
    /// </summary>
    public static string MidiNoteToNoteClass(int midiNote)
    {
        if (midiNote < 0 || midiNote > 127) return "N/A";
            
        return NoteNames[midiNote % 12];
    }
        
    /// <summary>
    /// Calculate the difference in cents between two MIDI notes
    /// </summary>
    public static double CentsBetweenMidiNotes(int note1, int note2)
    {
        return (note2 - note1) * 100.0; // Each semitone = 100 cents
    }
}
    
public class StabilizedPitchDetector
{
    private readonly Queue<double> _pitchHistory;
    private readonly Queue<int> _midiNoteHistory;
    private readonly int _historySize;
    private double _lastStablePitch;
    private int _lastStableMidiNote;
    private int _stableCount;
    private readonly double _stabilityThreshold;
    private readonly int _minStableFrames;
        
    // Smoothing filter
    private readonly Queue<double> _smoothingBuffer;
    private readonly int _smoothingSize;
        
    public StabilizedPitchDetector(int historySize = 5, double stabilityThreshold = 10.0, int minStableFrames = 3, int smoothingSize = 3)
    {
        this._historySize = historySize;
        this._stabilityThreshold = stabilityThreshold; // Hz tolerance
        this._minStableFrames = minStableFrames;
        this._smoothingSize = smoothingSize;
            
        _pitchHistory = new Queue<double>();
        _midiNoteHistory = new Queue<int>();
        _smoothingBuffer = new Queue<double>();
        _lastStablePitch = 0;
        _lastStableMidiNote = -1;
        _stableCount = 0;
    }
        
    public (double pitch, int midiNote, string noteName, bool isStable) ProcessPitch(double rawPitch)
    {
        if (rawPitch <= 0)
        {
            return (0, -1, "N/A", false);
        }
            
        // Method 1: Moving average smoothing
        var smoothedPitch = ApplySmoothing(rawPitch);
            
        // Method 2: Hysteresis - only change if difference is significant
        var hysteresisPitch = ApplyHysteresis(smoothedPitch);
            
        // Method 3: Majority voting from recent history
        var votedMidiNote = GetMajorityVote(hysteresisPitch);
            
        // Method 4: Stability detection
        var (finalPitch, finalMidiNote, isStable) = CheckStability(hysteresisPitch, votedMidiNote);
            
        var noteName = MidiHelper.MidiNoteToNoteName(finalMidiNote);
            
        return (finalPitch, finalMidiNote, noteName, isStable);
    }
        
    private double ApplySmoothing(double rawPitch)
    {
        _smoothingBuffer.Enqueue(rawPitch);
            
        if (_smoothingBuffer.Count > _smoothingSize)
        {
            _smoothingBuffer.Dequeue();
        }
            
        // Use median instead of mean to avoid outlier influence
        var sortedPitches = _smoothingBuffer.OrderBy(x => x).ToArray();
            
        if (sortedPitches.Length == 0) return rawPitch;
        if (sortedPitches.Length % 2 == 1)
        {
            return sortedPitches[sortedPitches.Length / 2];
        }
        else
        {
            int mid = sortedPitches.Length / 2;
            return (sortedPitches[mid - 1] + sortedPitches[mid]) / 2.0;
        }
    }
        
    private double ApplyHysteresis(double currentPitch)
    {
        // If we have a stable pitch, only change if the new pitch is significantly different
        if (_lastStablePitch > 0)
        {
            double cents = 1200 * Math.Log2(currentPitch / _lastStablePitch);
                
            // Only accept change if it's more than 30 cents (about 1/3 of a semitone)
            if (Math.Abs(cents) < 30)
            {
                return _lastStablePitch; // Keep the stable pitch
            }
        }
            
        return currentPitch;
    }
        
    private int GetMajorityVote(double pitch)
    {
        var currentMidiNote = MidiHelper.FrequencyToMidiNote(pitch);
            
        _midiNoteHistory.Enqueue(currentMidiNote);
        if (_midiNoteHistory.Count > _historySize)
        {
            _midiNoteHistory.Dequeue();
        }
            
        // Find the most common MIDI note in recent history
        var noteCounts = _midiNoteHistory.GroupBy(n => n)
            .ToDictionary(g => g.Key, g => g.Count());
            
        var majorityNote = noteCounts.OrderByDescending(kv => kv.Value).First().Key;
            
        // Only return majority if it appears in at least half the recent samples
        if (noteCounts[majorityNote] >= Math.Ceiling(_midiNoteHistory.Count / 2.0))
        {
            return majorityNote;
        }
            
        return currentMidiNote;
    }
        
    private (double pitch, int midiNote, bool isStable) CheckStability(double pitch, int midiNote)
    {
        _pitchHistory.Enqueue(pitch);
        if (_pitchHistory.Count > _historySize)
        {
            _pitchHistory.Dequeue();
        }
            
        // Check if recent pitches are stable
        if (_pitchHistory.Count >= _minStableFrames)
        {
            var recentPitches = _pitchHistory.ToArray();
            var avgPitch = recentPitches.Average();
            var maxDeviation = recentPitches.Max(p => Math.Abs(p - avgPitch));
                
            bool isCurrentlyStable = maxDeviation < _stabilityThreshold;
                
            if (isCurrentlyStable)
            {
                if (midiNote == _lastStableMidiNote)
                {
                    _stableCount++;
                }
                else
                {
                    _stableCount = 1;
                    _lastStableMidiNote = midiNote;
                    _lastStablePitch = avgPitch;
                }
                    
                // Only report as stable after minimum stable frames
                if (_stableCount >= _minStableFrames)
                {
                    return (_lastStablePitch, _lastStableMidiNote, true);
                }
            }
            else
            {
                _stableCount = 0;
            }
        }
            
        return (pitch, midiNote, false);
    }
}