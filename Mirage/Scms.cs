/*
 * Mirage - High Performance Music Similarity and Automatic Playlist Generator
 * http://hop.at/mirage
 *
 * Copyright (C) 2007-2008 Dominik Schnitzer <dominik@schnitzer.at>
 * Changed and enhanced by Per Ivar Nerseth <perivar@nerseth.com>
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA  02110-1301, USA.
 */

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using Comirva.Audio.Feature;
using Comirva.Audio.Util.Maths;

using NDtw;

namespace Mirage
{
	public class ScmsImpossibleException : Exception
	{
	}

	/// <summary>
	/// Statistical Cluster Model Similarity class. A Gaussian representation
	/// of a song. The distance between two models is computed with the
	/// symmetrized Kullback Leibler Divergence.
	/// </summary>
	public class Scms : AudioFeature
	{
		private float [] mean;
		private float [] cov;
		private float [] icov;
		private int dim;

		private string name;
		public override string Name {
			get {
				return name;
			}
			set {
				this.name = value;
			}
		}
		
		public Scms(int dimension)
		{
			dim = dimension;
			int symDim = (dim * dim + dim) / 2;

			mean = new float [dim];
			cov  = new float [symDim];
			icov = new float [symDim];
		}

		/// <summary>
		/// Computes a Scms model from the MFCC representation of a song.
		/// </summary>
		/// <param name="mfcc">Comirva.Audio.Util.Maths.Matrix mfcc</param>
		/// <returns></returns>
		public static Scms GetScms(Comirva.Audio.Util.Maths.Matrix mfccs, string name)
		{
			DbgTimer t = new DbgTimer();
			t.Start();
			
			Comirva.Audio.Util.Maths.Matrix mean = mfccs.Mean(2);

			#if DEBUG
			if (Analyzer.DEBUG_INFO_VERBOSE) {
				if (Analyzer.DEBUG_OUTPUT_TEXT) mean.WriteText(name + "_mean.txt");
				mean.DrawMatrixGraph(name + "_mean.png");
			}
			#endif

			// Covariance
			Comirva.Audio.Util.Maths.Matrix covarMatrix = mfccs.Cov(mean);
			#if DEBUG
			if (Analyzer.DEBUG_INFO_VERBOSE) {
				if (Analyzer.DEBUG_OUTPUT_TEXT) covarMatrix.WriteText(name + "_covariance.txt");
				covarMatrix.DrawMatrixGraph(name + "_covariance.png");
			}
			#endif

			// Inverse Covariance
			Comirva.Audio.Util.Maths.Matrix covarMatrixInv;
			try {
				covarMatrixInv = covarMatrix.InverseGausJordan();
			} catch (Exception) {
				Dbg.WriteLine("MatrixSingularException - Scms failed!");
				return null;
			}
			#if DEBUG
			if (Analyzer.DEBUG_INFO_VERBOSE) {
				if (Analyzer.DEBUG_OUTPUT_TEXT) covarMatrixInv.WriteAscii(name + "_inverse_covariance.ascii");
				covarMatrixInv.DrawMatrixGraph(name + "_inverse_covariance.png");
			}
			#endif
			
			// Store the Mean, Covariance, Inverse Covariance in an optimal format.
			int dim = mean.Rows;
			Scms s = new Scms(dim);
			int l = 0;
			for (int i = 0; i < dim; i++) {
				s.mean[i] = (float) mean.MatrixData[i][0];
				for (int j = i; j < dim; j++) {
					s.cov[l] = (float) covarMatrix.MatrixData[i][j];
					s.icov[l] = (float) covarMatrixInv.MatrixData[i][j];
					l++;
				}
			}

			Dbg.WriteLine("Compute Scms - Execution Time: {0} ms", t.Stop().TotalMilliseconds);
			return s;
		}
		
		/// <summary>
		/// Computes a Scms model from the MFCC representation of a song.
		/// </summary>
		/// <param name="mfcc">Comirva.Audio.Util.Maths.Matrix mfcc</param>
		/// <returns></returns>
		public static Scms GetScmsNoInverse(Comirva.Audio.Util.Maths.Matrix mfccs, string name) {
			DbgTimer t = new DbgTimer();
			t.Start();
			
			Comirva.Audio.Util.Maths.Matrix mean = mfccs.Mean(2);

			#if DEBUG
			if (Analyzer.DEBUG_INFO_VERBOSE) {
				if (Analyzer.DEBUG_OUTPUT_TEXT) mean.WriteText(name + "_mean.txt");
				mean.DrawMatrixGraph(name + "_mean.png");
			}
			#endif

			// Covariance
			Comirva.Audio.Util.Maths.Matrix covarMatrix = mfccs.Cov(mean);
			#if DEBUG
			if (Analyzer.DEBUG_INFO_VERBOSE) {
				if (Analyzer.DEBUG_OUTPUT_TEXT) covarMatrix.WriteText(name + "_covariance.txt");
				covarMatrix.DrawMatrixGraph(name + "_covariance.png");
			}
			#endif
			
			Comirva.Audio.Util.Maths.Matrix covarMatrixInv = new Comirva.Audio.Util.Maths.Matrix(covarMatrix.Rows, covarMatrix.Columns);
			
			// Store the Mean, Covariance, Inverse Covariance in an optimal format.
			int dim = mean.Rows;
			Scms s = new Scms(dim);
			int l = 0;
			for (int i = 0; i < dim; i++) {
				s.mean[i] = (float) mean.MatrixData[i][0];
				for (int j = i; j < dim; j++) {
					s.cov[l] = (float) covarMatrix.MatrixData[i][j];
					s.icov[l] = (float) covarMatrixInv.MatrixData[i][j];
					l++;
				}
			}

			Dbg.WriteLine("GetScmsNoInverse - Execution Time: {0} ms", t.Stop().TotalMilliseconds);
			return s;
		}
		
		/// <summary>
		/// Computes a Scms model from the MFCC representation of a song.
		/// </summary>
		/// <param name="mfcc">Mirage.Matrix mfcc</param>
		/// <returns></returns>
		public static Scms GetScms(Matrix mfcc, string name)
		{
			DbgTimer t = new DbgTimer();
			t.Start();

			// Mean
			Vector m = mfcc.Mean();
			
			#if DEBUG
			if (Analyzer.DEBUG_INFO_VERBOSE) {
				if (Analyzer.DEBUG_OUTPUT_TEXT) m.WriteText(name + "_mean_orig.txt");
				m.DrawMatrixGraph(name + "_mean_orig.png");
			}
			#endif
			
			// Covariance
			Matrix c = mfcc.Covariance(m);

			#if DEBUG
			if (Analyzer.DEBUG_INFO_VERBOSE) {
				if (Analyzer.DEBUG_OUTPUT_TEXT) c.WriteText(name + "_covariance_orig.txt");
				c.DrawMatrixGraph(name + "_covariance_orig.png");
			}
			#endif

			// Inverse Covariance
			Matrix ic;
			try {
				ic = c.Inverse();
			} catch (MatrixSingularException) {
				//throw new ScmsImpossibleException();
				Dbg.WriteLine("MatrixSingularException - Scms failed!");
				return null;
			}

			#if DEBUG
			if (Analyzer.DEBUG_INFO_VERBOSE) {
				if (Analyzer.DEBUG_OUTPUT_TEXT) ic.WriteAscii(name + "_inverse_covariance_orig.txt");
				ic.DrawMatrixGraph(name + "_inverse_covariance_orig.png");
			}
			#endif

			// Store the Mean, Covariance, Inverse Covariance in an optimal format.
			int dim = m.rows;
			Scms s = new Scms(dim);
			int l = 0;
			for (int i = 0; i < dim; i++) {
				s.mean[i] = m.d[i, 0];
				for (int j = i; j < dim; j++) {
					s.cov[l] = c.d[i, j];
					s.icov[l] = ic.d[i, j];
					l++;
				}
			}

			Dbg.WriteLine("(Mirage) - scms created in: {0} ms", t.Stop().TotalMilliseconds);

			return s;
		}

		public override double GetDistance(AudioFeature f)
		{
			if(!(f is Scms))
			{
				new Exception("Can only handle AudioFeatures of type Scms, not of: "+f);
				return -1;
			}
			Scms other = (Scms)f;
			return Distance(this, other, new ScmsConfiguration(Analyzer.MFCC_COEFFICIENTS));
		}

		public override double GetDistance(AudioFeature f, AudioFeature.DistanceType t)
		{
			if(!(f is Scms))
			{
				new Exception("Can only handle AudioFeatures of type Scms, not of: "+f);
				return -1;
			}
			Scms other = (Scms)f;
			
			DistanceMeasure distanceMeasure = DistanceMeasure.Euclidean;
			switch (t) {
				case AudioFeature.DistanceType.Dtw_Euclidean:
					distanceMeasure = DistanceMeasure.Euclidean;
					break;
				case AudioFeature.DistanceType.Dtw_SquaredEuclidean:
					distanceMeasure = DistanceMeasure.SquaredEuclidean;
					break;
				case AudioFeature.DistanceType.Dtw_Manhattan:
					distanceMeasure = DistanceMeasure.Manhattan;
					break;
				case AudioFeature.DistanceType.Dtw_Maximum:
					distanceMeasure = DistanceMeasure.Maximum;
					break;
				case AudioFeature.DistanceType.UCR_Dtw:
					return UCRCSharp.UCR.DTW(this.GetArray(), other.GetArray());
				case AudioFeature.DistanceType.CosineSimilarity:
					return CosineSimilarity(this, other);
				case AudioFeature.DistanceType.BitStringHamming:
					return Imghash.ImagePHash.HammingDistance(this.BitString, other.BitString);
				case AudioFeature.DistanceType.KullbackLeiblerDivergence:
				default:
					return Distance(this, other, new ScmsConfiguration(Analyzer.MFCC_COEFFICIENTS));
			}
			Dtw dtw = new Dtw(this.GetArray(), other.GetArray(), distanceMeasure, true, true, null, null, null);
			return dtw.GetCost();
		}
		
		public static float CosineSimilarity(Scms s1, Scms s2) {
			float mean = 1 - CosineSimilarity(s1.mean, s2.mean);
			float cov = 1 - CosineSimilarity(s1.cov, s2.cov);
			//float icov = CosineSimilarity(s1.icov, s2.icov) * 100;
			return mean + cov;// + icov;
		}
		
		/// <summary>
		/// Calculate Cosine Similarity
		/// </summary>
		/// <param name="f1">first float array</param>
		/// <param name="f2">second float array</param>
		/// <returns>The result of this calculation will always be a value between 0 and 1,
		/// where 0 means 0% similar, and the 1 means 100% similar.</returns>
		public static float CosineSimilarity(float[] f1, float[] f2) {
			
			// To calculate the cosine similarity, we need to:
			// Take the dot product of vectors A and B.
			// Calculate the magnitude of Vector A.
			// Calculate the magnitude of Vector B.
			// Multiple the magnitudes of A and B.
			// Divide the dot product of A and B by the product of the magnitudes of A and B.
			// The result of this calculation will always be a value between 0 and 1, where 0 means 0% similar, and the 1 means 100% similar.  Pretty convenient, huh?
			
			double sim = 0.0d;
			
			int N = 0;
			N = ((f2.Length < f1.Length) ? f2.Length : f1.Length);
			
			double dot = 0.0d;
			double mag1 = 0.0d;
			double mag2 = 0.0d;
			for (int n = 0; n < N; n++)
			{
				dot += f1[n] * f2[n];
				mag1 += Math.Pow(f1[n], 2);
				mag2 += Math.Pow(f2[n], 2);
			}

			sim = dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
			return (float) (sim);
		}
		
		public double[] GetArray() {
			
			double[] d = new double[mean.Length + cov.Length + icov.Length];

			int start = 0;
			for (int i = 0; i < mean.Length; i++) {
				d[start + i] = mean[i];
			}

			start += mean.Length;
			for (int i = 0; i < cov.Length; i++) {
				d[start + i] = cov[i];
			}

			start += cov.Length;
			for (int i = 0; i < icov.Length; i++) {
				d[start + i] = icov[i];
			}
			
			return d;
		}
		
		public static float Distance(byte[] a, byte[] b)
		{
			return Distance (
				FromBytes (a),
				FromBytes (b),
				new ScmsConfiguration (Analyzer.MFCC_COEFFICIENTS)
			);
		}

		/// <summary>
		/// Function to compute the spectral distance between two song models.
		/// (Statistical Cluster Model Similarity class. A Gaussian representation of a song.)
		/// This is a fast implementation of the symmetrized Kullback Leibler
		/// Divergence.
		/// </summary>
		/// <param name="s1">A song model (Statistical Cluster Model Similarity class)</param>
		/// <param name="s2">A song model (Statistical Cluster Model Similarity class)</param>
		/// <param name="c">ScmsConfiguration</param>
		/// <returns>float distance</returns>
		public static float Distance(Scms s1, Scms s2, ScmsConfiguration c)
		{
			float val = 0;
			int i;
			int k;
			int idx = 0;
			int dim = c.Dimension;
			int covlen = c.CovarianceLength;
			float tmp1;

			unsafe {
				fixed (float* s1cov = s1.cov, s2icov = s2.icov,
				       s1icov = s1.icov, s2cov = s2.cov,
				       s1mean = s1.mean, s2mean = s2.mean,
				       mdiff = c.MeanDiff, aicov = c.AddInverseCovariance)
				{
					for (i = 0; i < covlen; i++) {
						aicov[i] = s1icov[i] + s2icov[i];
					}

					for (i = 0; i < dim; i++) {
						idx = i*dim - (i*i+i)/2;
						val += s1cov[idx+i] * s2icov[idx+i] +
							s2cov[idx+i] * s1icov[idx+i];

						for (k = i+1; k < dim; k++) {
							val += 2*s1cov[idx+k] * s2icov[idx+k] +
								2*s2cov[idx+k] * s1icov[idx+k];
						}
					}

					for (i = 0; i < dim; i++) {
						mdiff[i] = s1mean[i] - s2mean[i];
					}

					for (i = 0; i < dim; i++) {
						idx = i - dim;
						tmp1 = 0;

						for (k = 0; k <= i; k++) {
							idx += dim - k;
							tmp1 += aicov[idx] * mdiff[k];
						}
						for (k = i + 1; k < dim; k++) {
							idx++;
							tmp1 += aicov[idx] * mdiff[k];
						}
						val += tmp1 * mdiff[i];
					}
				}
			}

			// FIXME: fix the negative return values
			//val = Math.Max(0.0f, (val/2 - s1.dim));
			val = val / 4 - c.Dimension / 2;

			return val;
		}

		/// <summary>
		/// Manual serialization of a Scms object to a byte array
		/// </summary>
		/// <returns></returns>
		public override byte[] ToBytes()
		{
			using (var stream = new MemoryStream ()) {
				using (var bw = new BinaryWriter(stream)) {
					bw.Write ((Int32)dim);

					for (int i = 0; i < mean.Length; i++) {
						bw.Write(mean[i]);
					}

					for (int i = 0; i < cov.Length; i++) {
						bw.Write(cov[i]);
					}

					for (int i = 0; i < icov.Length; i++) {
						bw.Write(icov[i]);
					}

					return stream.ToArray ();
				}
			}
		}

		/// <summary>
		/// Manual deserialization a byte array to a Scms object
		/// </summary>
		/// <param name="buf">byte array</param>
		/// <returns>Song model</returns>
		public static Scms FromBytes(byte[] buf)
		{
			var scms = new Scms(Analyzer.MFCC_COEFFICIENTS);
			FromBytes(buf, scms);
			return scms;
		}

		/// <summary>
		/// Manual deserialization of an Scms from a LittleEndian byte array
		/// </summary>
		/// <param name="buf">byte array</param>
		/// <param name="s">song model</param>
		public static void FromBytes(byte[] buf, Scms s)
		{
			byte [] buf4 = new byte[4];
			int buf_i = 0;

			s.dim = GetInt32(buf, buf_i, buf4);
			buf_i += 4;

			for (int i = 0; i < s.mean.Length; i++) {
				s.mean[i] = GetFloat(buf, buf_i, buf4);
				buf_i += 4;
			}

			for (int i = 0; i < s.cov.Length; i++) {
				s.cov[i] = GetFloat(buf, buf_i, buf4);
				buf_i += 4;
			}

			for (int i = 0; i < s.icov.Length; i++) {
				s.icov[i] = GetFloat(buf, buf_i, buf4);
				buf_i += 4;
			}
		}
		
		#region Static Helper methods
		private static bool isLE = BitConverter.IsLittleEndian;
		private static int GetInt32(byte [] buf, int i, byte [] buf4)
		{
			if (isLE) {
				return BitConverter.ToInt32(buf, i);
			} else {
				return BitConverter.ToInt32(Reverse (buf, i, 4, buf4), 0);
			}
		}

		private static float GetFloat(byte [] buf, int i, byte [] buf4)
		{
			if (isLE) {
				return BitConverter.ToSingle(buf, i);
			} else {
				return BitConverter.ToSingle(Reverse (buf, i, 4, buf4), 0);
			}
		}

		private static byte [] Reverse(byte [] buf, int start, int length, byte [] out_buf)
		{
			var ret = out_buf;
			int end = start + length -1;
			for (int i = 0; i < length; i++) {
				ret[i] = buf[end - i];
			}
			return ret;
		}
		#endregion
	}
}