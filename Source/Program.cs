using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;


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

namespace JaProj
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}