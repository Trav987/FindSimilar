// Sound Fingerprinting framework
// git://github.com/AddictedCS/soundfingerprinting.git
// Code license: CPOL v.1.02
// ciumac.sergiu@gmail.com
// http://www.codeproject.com/Articles/206507/Duplicates-detector-via-audio-fingerprinting

using System;
using System.Diagnostics;
using System.Linq;

using Soundfingerprinting.Audio.Models;
using Soundfingerprinting.Fingerprinting.FFT;
using Mirage;

namespace Soundfingerprinting.Audio.Services
{
	public abstract class AudioService
	{
		private readonly IFFTService fftService;

		protected AudioService(IFFTService fftService)
		{
			this.fftService = fftService;
		}

		// normalize power (volume) of an audio file.
		// minimum and maximum rms to normalize from.
		// these values has been detected empirically
		private const float MinRms = 0.1f;

		private const float MaxRms = 3;

		public abstract void Dispose();

		public abstract float[] ReadMonoFromFile(
			string pathToFile, int sampleRate, int milliSeconds, int startMilliSeconds);

		public float[][] CreateSpectrogram(string pathToFilename, IWindowFunction windowFunction, int sampleRate, int overlap, int wdftSize)
		{
			// read 5512 Hz, Mono, PCM, with a specific proxy
			float[] samples = ReadMonoFromFile(pathToFilename, sampleRate, 0, 0);

			NormalizeInPlace(samples);

			int width = (samples.Length - wdftSize) / overlap; /*width of the image*/
			float[][] frames = new float[width][];
			float[] complexSignal = new float[2 * wdftSize]; /*even - Re, odd - Img, thats how Exocortex works*/
			//double[] window = windowFunction.GetWindow(wdftSize);
			float[] window = windowFunction.GetWindow();
			for (int i = 0; i < width; i++)
			{
				// take 371 ms each 11.6 ms (2048 samples each 64 samples)
				for (int j = 0; j < wdftSize; j++)
				{
					complexSignal[2 * j] = (float)window[j] * samples[(i * overlap) + j];
					complexSignal[(2 * j) + 1] = 0;
				}

				//Fourier.FFT(complexSignal, wdftSize, FourierDirection.Forward);
				//float[] complexSignal = fftService.FFTForward(complexSignal, configuration.WdftSize);

				float[] band = new float[(wdftSize / 2) + 1];
				for (int j = 0; j < (wdftSize / 2) + 1; j++)
				{
					double re = complexSignal[2 * j];
					double img = complexSignal[(2 * j) + 1];

					re /= (float)wdftSize / 2;
					img /= (float)wdftSize / 2;

					band[j] = (float)((re * re) + (img * img));
				}

				frames[i] = band;
			}

			return frames;
		}

		public float[][] CreateLogSpectrogram(string pathToFile, IWindowFunction windowFunction, AudioServiceConfiguration configuration)
		{
			float[] samples = ReadMonoFromFile(pathToFile, configuration.SampleRate, 0, 0);
			return CreateLogSpectrogram(samples, windowFunction, configuration);
		}

		public float[][] CreateLogSpectrogram(
			float[] samples, IWindowFunction windowFunction, AudioServiceConfiguration configuration)
		{
			if (configuration.NormalizeSignal)
			{
				NormalizeInPlace(samples);
			}

			int width = (samples.Length - configuration.WdftSize) / configuration.Overlap; /*width of the image*/
			float[][] frames = new float[width][];
			int[] logFrequenciesIndexes = GenerateLogFrequencies(configuration);
			//double[] window = windowFunction.GetWindow(configuration.WdftSize);
			float[] window = windowFunction.GetWindow();
			for (int i = 0; i < width; i++)
			{
				float[] complexSignal = fftService.FFTForward(samples, i * configuration.Overlap, configuration.WdftSize, window);
				frames[i] = ExtractLogBins(complexSignal, logFrequenciesIndexes, configuration.LogBins);
			}

			return frames;
		}

		private void NormalizeInPlace(float[] samples)
		{
			double squares = samples.AsParallel().Aggregate<float, double>(0, (current, t) => current + (t * t));

			float rms = (float)Math.Sqrt(squares / samples.Length) * 10;

			Debug.WriteLine("10 RMS: {0}", rms);
			
			if (rms < MinRms)
			{
				rms = MinRms;
			}

			if (rms > MaxRms)
			{
				rms = MaxRms;
			}

			for (int i = 0; i < samples.Length; i++)
			{
				samples[i] /= rms;
				samples[i] = Math.Min(samples[i], 1);
				samples[i] = Math.Max(samples[i], -1);
			}
		}

		private float[] ExtractLogBins(float[] spectrum, int[] logFrequenciesIndex, int logBins)
		{
			int width = spectrum.Length / 2;
			float[] sumFreq = new float[logBins]; /*32*/
			for (int i = 0; i < logBins; i++)
			{
				int lowBound = logFrequenciesIndex[i];
				int higherBound = logFrequenciesIndex[i + 1];

				for (int k = lowBound; k < higherBound; k++)
				{
					double re = spectrum[2 * k] / ((float)width / 2);
					double img = spectrum[(2 * k) + 1] / ((float)width / 2);
					sumFreq[i] += (float)((re * re) + (img * img));
				}

				sumFreq[i] /= higherBound - lowBound;
			}

			return sumFreq;
		}

		private int[] GenerateLogFrequenciesDynamicBase(AudioServiceConfiguration configuration)
		{
			double logBase =
				Math.Exp(
					Math.Log((float)configuration.MaxFrequency / configuration.MinFrequency) / configuration.LogBins);
			double mincoef = (float)configuration.WdftSize / configuration.SampleRate * configuration.MinFrequency;
			int[] indexes = new int[configuration.LogBins + 1];
			for (int j = 0; j < configuration.LogBins + 1; j++)
			{
				int start = (int)((Math.Pow(logBase, j) - 1.0) * mincoef);
				int end = (int)((Math.Pow(logBase, j + 1.0f) - 1.0) * mincoef);
				indexes[j] = start + (int)mincoef;
			}

			return indexes;
		}

		/// <summary>
		/// Get logarithmically spaced indices
		/// </summary>
		/// <param name="configuration">
		/// The configuration for log frequencies
		/// </param>
		/// <returns>
		/// Log indexes
		/// </returns>
		private int[] GenerateLogFrequencies(AudioServiceConfiguration configuration)
		{
			if(configuration.UseDynamicLogBase)
			{
				return GenerateLogFrequenciesDynamicBase(configuration);
			}

			return GenerateStaticLogFrequencies(configuration);
		}

		private int[] GenerateStaticLogFrequencies(AudioServiceConfiguration configuration)
		{
			double logMin = Math.Log(configuration.MinFrequency, configuration.LogBase);
			double logMax = Math.Log(configuration.MaxFrequency, configuration.LogBase);

			double delta = (logMax - logMin) / configuration.LogBins;

			int[] indexes = new int[configuration.LogBins + 1];
			double accDelta = 0;
			for (int i = 0; i <= configuration.LogBins /*32 octaves*/; ++i)
			{
				float freq = (float)Math.Pow(configuration.LogBase, logMin + accDelta);
				accDelta += delta; // accDelta = delta * i
				/*Find the start index in array from which to start the summation*/
				indexes[i] = FreqToIndex(freq, configuration.SampleRate, configuration.WdftSize);
			}

			return indexes;
		}

		/*
		 * An array of WDFT [0, 2048], contains a range of [0, 5512] frequency components.
		 * Only 1024 contain actual data. In order to find the Index, the fraction is found by dividing the frequency by max frequency
		 */

		/// <summary>
		///   Gets the index in the spectrum vector from according to the starting frequency specified as the parameter
		/// </summary>
		/// <param name = "freq">Frequency to be found in the spectrum vector [E.g. 300Hz]</param>
		/// <param name = "sampleRate">Frequency rate at which the signal was processed [E.g. 5512Hz]</param>
		/// <param name = "spectrumLength">Length of the spectrum [2048 elements generated by WDFT from which only 1024 are with the actual data]</param>
		/// <returns>Index of the frequency in the spectrum array</returns>
		/// <remarks>
		/// The Bandwidth of the spectrum runs from 0 until SampleRate / 2 [E.g. 5512 / 2]
		///   Important to remember:
		///   N points in time domain correspond to N/2 + 1 points in frequency domain
		///   E.g. 300 Hz applies to 112'th element in the array
		/// </remarks>
		private int FreqToIndex(float freq, int sampleRate, int spectrumLength)
		{
			/*N sampled points in time correspond to [0, N/2] frequency range */
			float fraction = freq / ((float)sampleRate / 2);
			/*DFT N points defines [N/2 + 1] frequency points*/
			int i = (int)Math.Round(((spectrumLength / 2) + 1) * fraction);
			return i;
		}

		/// <summary>
		///   Read data from file
		/// </summary>
		/// <param name = "pathToFile">Filename to be read</param>
		/// <param name = "sampleRate">Sample rate at which to perform reading</param>
		/// <returns>Array with data</returns>
		public float[] ReadMonoFromFile(string pathToFile, int sampleRate)
		{
			return ReadMonoFromFile(pathToFile, sampleRate, 0, 0);
		}
	}
}