namespace Soundfingerprinting.Fingerprinting.Configuration
{
    using Soundfingerprinting.Audio.Strides;
    using Mirage;

    public interface IFingerprintingConfiguration
    {
        /// <summary>
        ///   Gets number of samples to read in order to create single signature.
        ///   The granularity is 1.48 seconds
        /// </summary>
        int SamplesPerFingerprint { get; }

        /// <summary>
        ///   Gets overlap between the sub fingerprints, 11.6 ms
        /// </summary>
        int Overlap { get; }

        /// <summary>
        ///   Gets size of the WDFT block, 371 ms
        /// </summary>
        int WdftSize { get; }

        /// <summary>
        ///   Gets frequency range which is taken into account
        /// </summary>
        int MinFrequency { get; }

        /// <summary>
        ///   Gets frequency range which is taken into account
        /// </summary>
        int MaxFrequency { get; }

        /// <summary>
        ///   Gets number of Top wavelets to consider
        /// </summary>
        int TopWavelets { get; }

        /// <summary>
        ///   Gets sample rate
        /// </summary>
        int SampleRate { get; }

        /// <summary>
        ///   Gets log base used for computing the logarithmically spaced frequency bins
        /// </summary>
        double LogBase { get; }

        /// <summary>
        /// Gets number of logarithmically spaced bins between the frequency components computed by Fast Fourier Transform.
        /// </summary>
        int LogBins { get; }

        /// <summary>
        ///   Gets signature's length
        /// </summary>
        int FingerprintLength { get; }

        /// <summary>
        /// Gets default stride size between 2 consecutive signature
        /// </summary>
        /// <remarks>
        ///  Default = 5115
        /// </remarks>
        IStride Stride { get; }

        /// <summary>
        /// Gets window function applied on spectrogram
        /// </summary>
        IWindowFunction WindowFunction { get; }

        /// <summary>
        /// Gets a flag that normalizes the audio signal or not
        /// </summary>
        bool NormalizeSignal { get; }

        /// <summary>
        /// Use dynamic logarithmic base
        /// </summary>
        bool UseDynamicLogBase { get; }
    }
}