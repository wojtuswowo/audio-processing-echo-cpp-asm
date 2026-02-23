#include "pch.h"
#include <cmath>
#include <algorithm>
#include <cstdint>

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

/*
 * Procedure DelayCPP
 * Implements the echo effect algorithm in C++.
 * The function processes a buffer of 16-bit audio samples, adding to each sample
 * the signal delayed by a specified number of samples, multiple times (bounce),
 * with decreasing amplitude depending on the feedback parameter.
 *
 * Input parameters:
 * samples       – pointer to the input audio sample buffer (16-bit signed),
 *                 sample value range: -32768 to 32767
 * sampleCount   – number of samples to process (>= 0)
 * delaySamples  – echo delay in number of samples (>= 0)
 * bounce        – number of echo bounces, defines how many times the delayed signal
 *                 is added to the output sample (>= 0)
 * feedback      – feedback strength in percent (0–100),
 *                 defines attenuation of successive bounces
 *
 * Output parameters:
 * samplesout    – pointer to the output buffer containing samples
 *                 after applying the echo effect (16-bit signed)
 *
 * Return value:
 * int – function return code (0 = successful execution)
 */
extern "C" __declspec(dllexport)
int DelayCPP(short* samples, int sampleCount, short* samplesout,
    int delaySamples, int bounce, int feedback)
{
    // Feedback coefficient scaled to Q15 format (0–32767)
    short qFeedback = (feedback * 32767 / 100);

    // Loop over all input samples
    for (int n = 0; n < sampleCount; n++)
    {
        // Output sample accumulator (16-bit signed)
        short out = samples[n];

        // Current power of feedback in Q15 format (feedback^1)
        short qFbPower = qFeedback;

        // Loop implementing successive echo bounces
        for (int i = 1; i <= bounce; i++)
        {
            // Index of sample delayed by delaySamples * i
            int delayIndex = n - delaySamples * i;

            // Delayed signal sample
            short delayin = samples[delayIndex];

            // Fixed-point Q15 multiplication (equivalent to pmulhw)
            int32_t prod32 = static_cast<int32_t>(delayin) * static_cast<int32_t>(qFbPower);
            short prod = (prod32 >> 16);

            // Addition with 16-bit saturation
            int32_t sum = static_cast<int32_t>(out) + static_cast<int32_t>(prod);
            if (sum > 32767) sum = 32767;
            else if (sum < -32768) sum = -32768;

            out = (sum);

            // Compute next power of feedback (feedback^i) in Q15
            int32_t pow32 = static_cast<int32_t>(qFbPower) * static_cast<int32_t>(qFeedback);
            qFbPower = (pow32 >> 16);
        }

        // Store the output sample to the output buffer
        samplesout[n] = out;
    }

    return 0;
}