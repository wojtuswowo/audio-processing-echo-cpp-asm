using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

/*
------------------------------------------------------------
Project topic: Echo sound effect

Description:
The project implements an echo sound effect algorithm for an audio signal
stored in a WAV file. The algorithm operates on an input buffer containing
16-bit audio samples. For each sample of the input signal, the output buffer
stores the sum of the current value and samples from previous
positions (n samples back), depending on the specified number of bounces.

Each successive bounce has a progressively lower amplitude, scaled by the
feedback coefficient raised to the power n (feedback^n).
After summing all components, the echo effect is obtained.
The program includes safeguards to prevent reading data outside the
buffer range.

Completion date: 28.01.2026  
Semester: 5  
Academic year: 3  
Author: Wojciech Korga
------------------------------------------------------------
*/

namespace EchoEffectApp.Logic
{
    /*
     * Class EchoLogic
     * Responsible for processing WAV audio and applying the echo effect
     * using C++ or assembly implementation.
     */
    internal static class EchoLogic
    {
        /*
         * Procedure DelayCPP
         * Echo effect implementation in the C++ library.
         *
         * Input parameters:
         * samples        – pointer to the input sample buffer (16-bit signed)
         * sampleCount    – number of samples to process (>= 0)
         * delaysamples   – delay in samples (>= 0)
         * bounce         – number of echo bounces (>= 0)
         * feedback       – echo attenuation strength (0–100)
         *
         * Output parameters:
         * samplesout     – pointer to the output buffer with processed samples
         */
        [DllImport(@"CPPDLL.dll")]
        static extern unsafe void DelayCPP(
            short* samples,
            int sampleCount,
            short* samplesout,
            int delaysamples,
            int bounce,
            int feedback
        );

        /*
         * Procedure DelayASM
         * Echo effect implementation in assembly.
         *
         * Input parameters:
         * samples        – pointer to the input sample buffer (16-bit signed)
         * sampleCount    – number of samples to process (>= 0)
         * delaysamples   – delay in samples (>= 0)
         * bounce         – number of echo bounces (>= 0)
         * feedback       – echo attenuation strength (0–100)
         *
         * Output parameters:
         * samplesout     – pointer to the output buffer with processed samples
         */
        [DllImport(@"ASMDLL.dll")]
        static extern unsafe void DelayASM(
            short* samples,
            int sampleCount,
            short* samplesout,
            int delaysamples,
            int bounce,
            int feedback
        );

        /*
         * Procedure ProcessAudio
         * Loads a WAV file, processes it with the echo algorithm using multithreading,
         * and saves the result to a new WAV file.
         *
         * Input parameters:
         * useAsm   – choice of implementation (true = ASM, false = C++) 
         * bounce   – number of echo bounces (>= 0)
         * feedback – feedback strength (0–100)
         * delayMs  – echo delay in milliseconds (> 0)
         * threads  – number of threads (<= 0 means auto)
         * filePath – path to the WAV file
         *
         * Output parameters:
         * long – processing time in milliseconds (>= 0)
         */
        public static unsafe long ProcessAudio(
            bool useAsm,
            int bounce,
            int feedback,
            int delayMs,
            int threads,
            string filePath)
        {
            /*
             * Function FindDataChunkHeaderIndex
             * Searches for the "data" chunk header in a WAV file.
             *
             * Input parameters:
             * wav – byte array of the WAV file
             *
             * Output parameters:
             * int – index of the start of the "data" chunk
             */
            int FindDataChunkHeaderIndex(byte[] wav)
            {
                for (int i = 12; i < wav.Length - 8; i++)
                {
                    if (wav[i] == (byte)'d' && wav[i + 1] == (byte)'a' &&
                        wav[i + 2] == (byte)'t' && wav[i + 3] == (byte)'a')
                        return i;
                }
                throw new Exception("No 'data' chunk found!");
            }

            /*
             * Function FindFMTChunkHeaderIndex
             * Searches for the "fmt " chunk header in a WAV file.
             *
             * Input parameters:
             * wav – byte array of the WAV file
             *
             * Output parameters:
             * int – index of the start of the "fmt " chunk
             */
            int FindFMTChunkHeaderIndex(byte[] wav)
            {
                for (int i = 12; i < wav.Length - 8; i++)
                {
                    if (wav[i] == (byte)'f' && wav[i + 1] == (byte)'m' &&
                        wav[i + 2] == (byte)'t' && wav[i + 3] == (byte)' ')
                        return i;
                }
                throw new Exception("No 'fmt' chunk found!");
            }

            // Read the entire WAV file into memory
            byte[] databyte = File.ReadAllBytes(filePath);

            // WAV header indices
            int dataHeaderIndex = FindDataChunkHeaderIndex(databyte);
            int fmtHeaderIndex = FindFMTChunkHeaderIndex(databyte);
            int dataOffset = dataHeaderIndex + 8;

            // Audio format parameters
            ushort audioFormat = BitConverter.ToUInt16(databyte, fmtHeaderIndex + 8);
            ushort numChannels = BitConverter.ToUInt16(databyte, fmtHeaderIndex + 10);
            int sampleRate = BitConverter.ToInt32(databyte, fmtHeaderIndex + 12);
            ushort bitsPerSample = BitConverter.ToUInt16(databyte, fmtHeaderIndex + 22);

            // Check supported WAV format
            if (!(audioFormat == 1 && numChannels == 2 && sampleRate == 44100 && bitsPerSample == 16))
                throw new Exception("Unsupported WAV format");

            // Delay expressed in samples
            int delaySamples = delayMs * sampleRate / 1000;

            // Number of input samples
            int dataSize = BitConverter.ToInt32(databyte, dataHeaderIndex + 4);
            int originalSampleCount = dataSize / 2;

            // Copy WAV header
            byte[] header = new byte[dataOffset];
            Array.Copy(databyte, header, dataOffset);

            // ===== INPUT BUFFER PREPARATION =====

            // Number of delay samples before the signal
            int preDelaySamples = delaySamples * bounce;

            // Raw number of input samples
            int inputSamplesRaw = preDelaySamples + originalSampleCount;

            // Align to 16 samples
            int inputSamplesPadded = ((inputSamplesRaw + 15) / 16) * 16;

            // Input sample buffer
            short[] data = new short[inputSamplesPadded];

            // Convert WAV bytes to 16-bit samples
            for (int i = 0, j = dataOffset; i < originalSampleCount; i++, j += 2)
            {
                data[preDelaySamples + i] =
                    (short)(databyte[j] | (databyte[j + 1] << 8));
            }

            // ===== OUTPUT BUFFER =====

            int outputSamplesRaw = originalSampleCount;
            int outputSamplesPadded = ((outputSamplesRaw + 15) / 16) * 16;

            // Output sample buffer
            short[] data_output_short = new short[outputSamplesPadded];

            // ===== THREAD DIVISION =====

            if (threads <= 0) threads = Environment.ProcessorCount;

            int totalBlocks = (outputSamplesPadded + 15) / 16;
            threads = Math.Min(threads, Math.Max(1, totalBlocks));

            int baseBlocks = totalBlocks / threads;
            int remBlocks = totalBlocks % threads;

            int[] counts = new int[threads];
            int[] offsets = new int[threads];

            int currentOffset = 0;
            for (int t = 0; t < threads; t++)
            {
                int blocksForThread = baseBlocks + (t < remBlocks ? 1 : 0);
                int samplesForThread = blocksForThread * 16;
                if (currentOffset + samplesForThread > outputSamplesPadded)
                    samplesForThread = Math.Max(0, outputSamplesPadded - currentOffset);

                counts[t] = samplesForThread;
                offsets[t] = currentOffset;
                currentOffset += samplesForThread;
            }

            if (currentOffset < outputSamplesPadded)
                counts[threads - 1] += outputSamplesPadded - currentOffset;

            // ===== PROCESSING =====

            Task[] tasks = new Task[threads];
            Exception[] errors = new Exception[threads];

            Stopwatch totalStopwatch = Stopwatch.StartNew();

            fixed (short* pBase = data)
            fixed (short* pBaseOut = data_output_short)
            {
                IntPtr basePtr = (IntPtr)pBase;
                IntPtr basePtrOut = (IntPtr)pBaseOut;

                for (int t = 0; t < threads; t++)
                {
                    int threadIndex = t;
                    int start = offsets[t];
                    int len = counts[t];

                    if (len == 0)
                    {
                        tasks[threadIndex] = Task.CompletedTask;
                        continue;
                    }

                    tasks[threadIndex] = Task.Run(() =>
                    {
                        try
                        {
                            short* chunkPtr = (short*)basePtr + preDelaySamples + start;
                            short* chunkPtrOut = (short*)basePtrOut + start;

                            if (useAsm)
                                DelayASM(chunkPtr, len, chunkPtrOut, delaySamples, bounce, feedback);
                            else
                                DelayCPP(chunkPtr, len, chunkPtrOut, delaySamples, bounce, feedback);
                        }
                        catch (Exception ex)
                        {
                            errors[threadIndex] = ex;
                        }
                    });
                }

                Task.WaitAll(tasks);
            }

            totalStopwatch.Stop();

            foreach (var e in errors)
                if (e != null)
                    throw new AggregateException("Thread failed", e);

            // ===== WRITE WAV =====

            int newDataSize = originalSampleCount * 2;
            int newFileChunkSize = (header.Length - 8) + newDataSize;

            Array.Copy(BitConverter.GetBytes(newFileChunkSize), 0, header, 4, 4);
            Array.Copy(BitConverter.GetBytes(newDataSize), 0, header, dataHeaderIndex + 4, 4);

            byte[] databyte_output = new byte[newDataSize];
            for (int i = 0; i < originalSampleCount; i++)
            {
                short s = data_output_short[i];
                databyte_output[i * 2] = (byte)(s & 0xFF);
                databyte_output[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
            }

            string suffix = useAsm ? "-asm" : "-cpp";

            string directory = Path.GetDirectoryName(filePath);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            string outputFileName = $"{fileNameWithoutExt}-delay{suffix}{extension}";
            string outputPath = Path.Combine(directory, outputFileName);

            using (FileStream fs = new FileStream(outputPath, FileMode.Create))
            {
                fs.Write(header, 0, header.Length);
                fs.Write(databyte_output, 0, databyte_output.Length);
            }

            return totalStopwatch.ElapsedMilliseconds;
        }

        /*
         * Procedure RunFullBenchmark
         * Runs a series of performance tests for different configurations
         * and saves the results to a CSV file.
         *
         * Input parameters:
         * path – path to the WAV file used in the benchmark
         *
         * Output parameters:
         * none
         */
        public static void RunFullBenchmark(string path)
        {
            string outputLog = @"C:\Users\Wojciech\Desktop\wyniki_benchmark.csv";

            if (File.Exists(outputLog))
                File.Delete(outputLog);

            File.AppendAllText(outputLog,
                "mode,threads,bounce,feedback,delayMs,dataCategory,timeMs\n");

            string dataCategory = "duze";

            int[] threadsList = { 1, 2, 4, 8, 16, 32, 64 };
            int[] delayList = { 500 };
            int[] feedbackList = { 50 };
            int[] bounceList = { 1, 2, 3, 4, 5, 6, 7 };

            foreach (bool modeAsm in new[] { true, false })
            {
                string mode = modeAsm ? "asm" : "cpp";

                foreach (int th in threadsList)
                    foreach (int bounce in bounceList)
                        foreach (int feedback in feedbackList)
                            foreach (int delay in delayList)
                            {
                                double sum = 0;

                                for (int i = 0; i < 10; i++)
                                {
                                    long t = ProcessAudio(
                                        modeAsm, bounce, feedback, delay, th, path);

                                    sum += t;
                                }

                                double avg = sum / 10.0;

                                string line =
                                    $"{mode},{th},{bounce},{feedback},{delay},{dataCategory},{avg:F3}\n";

                                File.AppendAllText(outputLog, line);
                            }
            }
        }
    }
}