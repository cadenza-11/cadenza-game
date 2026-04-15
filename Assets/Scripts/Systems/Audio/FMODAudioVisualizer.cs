// The below is derived from FMOD's scripting examples
// for spectrum analysis using an FFT DSP.
// https://fmod.com/docs/2.03/unity/examples-spectrum-analysis.html

using System;
using UnityEngine;
using System.Runtime.InteropServices;

namespace Cadenza
{
    public class FMODAudioVisualizer
    {
        private FMOD.DSP mFFT;
        private FMOD.Studio.Bus bus;
        public float[] mFFTSpectrum;
        public const int WindowSize = 1024;

        public void OnInitialize(FMOD.Studio.Bus bus)
        {
            // Get bus.
            if (!bus.hasHandle())
            {
                Debug.LogWarning("FMOD: Unable to get the selected bus");
                return;
            }
            this.bus = bus;

            // These calls are necessary to ensure the bus is ready.
            this.bus.lockChannelGroup();
            FMODUnity.RuntimeManager.StudioSystem.flushCommands();

            // Create a DSP of DSP_TYPE.FFT
            FMOD.RESULT result = FMODUnity.RuntimeManager.CoreSystem.createDSPByType(FMOD.DSP_TYPE.FFT, out this.mFFT);
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogWarning("FMOD: Unable to create FMOD.DSP_TYPE.FFT");
                return;
            }

            this.mFFT.setParameterInt((int)FMOD.DSP_FFT.WINDOW, (int)FMOD.DSP_FFT_WINDOW_TYPE.HANNING);
            this.mFFT.setParameterInt((int)FMOD.DSP_FFT.WINDOWSIZE, WindowSize * 2);
            FMODUnity.RuntimeManager.StudioSystem.flushCommands();

            // Get the channel group
            result = bus.getChannelGroup(out FMOD.ChannelGroup channelGroup);
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogWarning("FMOD: Unable to get Channel Group from the selected bus");
                return;
            }

            // Add fft to the channel group
            result = channelGroup.addDSP(FMOD.CHANNELCONTROL_DSP_INDEX.HEAD, this.mFFT);
            if (result != FMOD.RESULT.OK)
            {
                Debug.LogWarning("FMOD: Unable to add mFFT to the master channel group");
                return;
            }
        }

        public void OnApplicationStop()
        {
            if (this.bus.hasHandle() &&
                this.bus.getChannelGroup(out FMOD.ChannelGroup channelGroup) == FMOD.RESULT.OK &&
                this.mFFT.hasHandle())
            {
                channelGroup.removeDSP(this.mFFT);
            }
        }

        public void OnUpdate()
        {
            if (!this.mFFT.hasHandle())
                return;

            FMOD.RESULT result = this.mFFT.getParameterData((int)FMOD.DSP_FFT.SPECTRUMDATA, out IntPtr unmanagedData, out _);
            if (result != FMOD.RESULT.OK)
                return;

            FMOD.DSP_PARAMETER_FFT fftData = (FMOD.DSP_PARAMETER_FFT)Marshal.PtrToStructure(unmanagedData, typeof(FMOD.DSP_PARAMETER_FFT));
            if (fftData.numchannels <= 0)
                return;

            if (this.mFFTSpectrum == null)
            {
                // Allocate the fft spectrum buffer once
                for (int i = 0; i < fftData.numchannels; ++i)
                    this.mFFTSpectrum = new float[fftData.length];
            }
            fftData.getSpectrum(0, ref this.mFFTSpectrum);
        }
    }
}
