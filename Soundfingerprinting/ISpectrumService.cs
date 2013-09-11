﻿namespace Soundfingerprinting.Fingerprinting.FFT
{
	using System.Collections.Generic;
	using Soundfingerprinting.Audio.Strides;

	public interface ISpectrumService
	{
		/// <summary>
		/// Cut logarithmized spetrum to spectral images
		/// </summary>
		/// <param name="logarithmizedSpectrum">Logarithmized spectrum of the initial signal</param>
		/// <param name="strideBetweenConsecutiveImages">Stride between consecutive images (static 928ms db, random 46ms query)</param>
		/// <param name="fingerprintImageLength">Length of 1 fingerprint image (w parameter equal to 128, which alogside overlap leads to 128*64 = 8192 = 1.48s)</param>
		/// <param name="overlap">Overlap between consecutive spectral images, taken previously (64 ~ 11.6ms)</param>
		/// <returns>List of logarithmic images</returns>
		List<double[][]> CutLogarithmizedSpectrum(double[][] logarithmizedSpectrum, IStride strideBetweenConsecutiveImages, int fingerprintImageLength, int overlap);
	}
}