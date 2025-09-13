using NAudio.Midi;
using NAudio.Wave;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Options;
using Raylib_cs;
using TobiasErichsen.teVirtualMIDI;

namespace Vocal;

internal static class Program
{
    private const int SampleRate = 44100;
    private const float SampleTime = 0.05f;
    private const int MinStableFrames = 15;
    private static readonly CircularArray<float> Buf = new((int)(SampleRate * SampleTime));

    public static void Main()
    {
        var virtualMidi = new TeVirtualMIDI("Vocal");
        var waveIn = new WaveInEvent
        {
            DeviceNumber = 0,
            WaveFormat = new WaveFormat(SampleRate, 1)
        };

        var wasNote = -1;

        try
        {
            waveIn.DataAvailable += OnDataAvailable;
            waveIn.StartRecording();

            Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
            Raylib.InitWindow(900, 600, "Vocal");

            var pitchExtractor = new PitchExtractor(new PitchOptions
            {
                SamplingRate = SampleRate,
                FrameSize = Buf.Capacity,
            });

            var block = new float[Buf.Capacity];
            var features = new float[] { 0 };
            var lastStable = -1;
            var stableHits = 0;

            var stabilizer = new StabilizedPitchDetector(
                historySize: 5, // Look at last 5 samples
                stabilityThreshold: 5.0, // 5 Hz tolerance
                minStableFrames: 3, // Need 3 stable frames
                smoothingSize: 3 // Smooth over 3 samples
            );

            while (!Raylib.WindowShouldClose())
            {
                Raylib.EndDrawing();
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);
                Raylib.DrawFPS(0, 0);

                if (Buf.Count != Buf.Capacity)
                {
                    continue;
                }

                for (int i = 0; i < Buf.Capacity; i++)
                    block[i] = Buf[i];

                pitchExtractor.ProcessFrame(block, features);
                var (pitch, midi, noteName, _) = stabilizer.ProcessPitch(features[0]);
                midi = Math.Min(sbyte.MaxValue, Math.Max(0, midi));
                var loudness = (int)(sbyte.MaxValue * MathF.Max(0, MathF.Min(Rms(block) / 0.1f, 1)));

                if (midi == 0 || MathF.Abs(loudness) < 0.002f)
                {
                    if (wasNote != -1)
                    {
                        SendMessage(virtualMidi, MidiMessage.StopNote(wasNote, loudness, 1));
                        wasNote = -1;
                    }

                    continue;
                }

                if (midi != wasNote)
                {
                    // if we changed note
                    if (lastStable != midi)
                    {
                        lastStable = midi;
                        stableHits = 0;
                        midi = wasNote;
                    }
                    else
                    {
                        stableHits++;
                        if (stableHits < MinStableFrames)
                        {
                            midi = wasNote;
                        }
                        else
                        {
                            stableHits = 0;
                            lastStable = midi;
                        }
                    }
                }

                Raylib.DrawText(noteName, 0, 100, 24, Color.White);
                Raylib.DrawText(midi.ToString(), 0, 120, 24, Color.White);
                Raylib.DrawText(pitch.ToString("F3"), 0, 140, 24, Color.White);

                if (wasNote == midi)
                {
                    continue;
                }

                if (wasNote != -1)
                {
                    Console.WriteLine($"Stopped {MidiHelper.MidiNoteToNoteName(wasNote)} for {noteName}");
                    SendMessage(virtualMidi, MidiMessage.StopNote(wasNote, loudness, 1));
                }

                SendMessage(virtualMidi, MidiMessage.StartNote(midi, loudness, 1));
                wasNote = midi;
                lastStable = midi;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            virtualMidi.shutdown();
            waveIn.StopRecording();
            Raylib.CloseWindow();
        }
    }

    private static void OnDataAvailable(object? _, WaveInEventArgs e)
    {
        for (int index = 0; index < e.BytesRecorded; index += 2)
        {
            var sample = (short)((e.Buffer[index + 1] << 8) |
                                 e.Buffer[index + 0]);
            var sample32 = sample / 32768f;
            Buf.Add(sample32);
        }
    }

    private static float Rms(float[] frame)
    {
        double sumSq = 0;
        foreach (var t in frame)
            sumSq += t * t;

        return (float)Math.Sqrt(sumSq / frame.Length);
    }

    private static void SendMessage(TeVirtualMIDI virtualMidi, MidiMessage msg)
    {
        virtualMidi.sendCommand(BitConverter.GetBytes(msg.RawData));
    }
}