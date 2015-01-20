/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search.Similarities;
using Sharpen;

namespace Org.Apache.Lucene.Misc
{
	/// <summary>
	/// <p>
	/// A similarity with a lengthNorm that provides for a "plateau" of
	/// equally good lengths, and tf helper functions.
	/// </summary>
	/// <remarks>
	/// <p>
	/// A similarity with a lengthNorm that provides for a "plateau" of
	/// equally good lengths, and tf helper functions.
	/// </p>
	/// <p>
	/// For lengthNorm, A min/max can be specified to define the
	/// plateau of lengths that should all have a norm of 1.0.
	/// Below the min, and above the max the lengthNorm drops off in a
	/// sqrt function.
	/// </p>
	/// <p>
	/// For tf, baselineTf and hyperbolicTf functions are provided, which
	/// subclasses can choose between.
	/// </p>
	/// </remarks>
	/// <seealso><a href="doc-files/ss.gnuplot">A Gnuplot file used to generate some of the visualizations refrenced from each function.</a>
	/// 	</seealso>
	public class SweetSpotSimilarity : DefaultSimilarity
	{
		private int ln_min = 1;

		private int ln_max = 1;

		private float ln_steep = 0.5f;

		private float tf_base = 0.0f;

		private float tf_min = 0.0f;

		private float tf_hyper_min = 0.0f;

		private float tf_hyper_max = 2.0f;

		private double tf_hyper_base = 1.3d;

		private float tf_hyper_xoffset = 10.0f;

		public SweetSpotSimilarity() : base()
		{
		}

		/// <summary>Sets the baseline and minimum function variables for baselineTf</summary>
		/// <seealso cref="BaselineTf(float)">BaselineTf(float)</seealso>
		public virtual void SetBaselineTfFactors(float @base, float min)
		{
			tf_min = min;
			tf_base = @base;
		}

		/// <summary>Sets the function variables for the hyperbolicTf functions</summary>
		/// <param name="min">the minimum tf value to ever be returned (default: 0.0)</param>
		/// <param name="max">the maximum tf value to ever be returned (default: 2.0)</param>
		/// <param name="base">the base value to be used in the exponential for the hyperbolic function (default: 1.3)
		/// 	</param>
		/// <param name="xoffset">the midpoint of the hyperbolic function (default: 10.0)</param>
		/// <seealso cref="HyperbolicTf(float)">HyperbolicTf(float)</seealso>
		public virtual void SetHyperbolicTfFactors(float min, float max, double @base, float
			 xoffset)
		{
			tf_hyper_min = min;
			tf_hyper_max = max;
			tf_hyper_base = @base;
			tf_hyper_xoffset = xoffset;
		}

		/// <summary>
		/// Sets the default function variables used by lengthNorm when no field
		/// specific variables have been set.
		/// </summary>
		/// <remarks>
		/// Sets the default function variables used by lengthNorm when no field
		/// specific variables have been set.
		/// </remarks>
		/// <seealso cref="ComputeLengthNorm(int)">ComputeLengthNorm(int)</seealso>
		public virtual void SetLengthNormFactors(int min, int max, float steepness, bool 
			discountOverlaps)
		{
			this.ln_min = min;
			this.ln_max = max;
			this.ln_steep = steepness;
			this.discountOverlaps = discountOverlaps;
		}

		/// <summary>
		/// Implemented as <code> state.getBoost()
		/// computeLengthNorm(numTokens) </code> where
		/// numTokens does not count overlap tokens if
		/// discountOverlaps is true by default or true for this
		/// specific field.
		/// </summary>
		/// <remarks>
		/// Implemented as <code> state.getBoost()
		/// computeLengthNorm(numTokens) </code> where
		/// numTokens does not count overlap tokens if
		/// discountOverlaps is true by default or true for this
		/// specific field.
		/// </remarks>
		public override float LengthNorm(FieldInvertState state)
		{
			int numTokens;
			if (discountOverlaps)
			{
				numTokens = state.GetLength() - state.GetNumOverlap();
			}
			else
			{
				numTokens = state.GetLength();
			}
			return state.GetBoost() * ComputeLengthNorm(numTokens);
		}

		/// <summary>
		/// Implemented as:
		/// <code>
		/// 1/sqrt( steepness * (abs(x-min) + abs(x-max) - (max-min)) + 1 )
		/// </code>.
		/// </summary>
		/// <remarks>
		/// Implemented as:
		/// <code>
		/// 1/sqrt( steepness * (abs(x-min) + abs(x-max) - (max-min)) + 1 )
		/// </code>.
		/// <p>
		/// This degrades to <code>1/sqrt(x)</code> when min and max are both 1 and
		/// steepness is 0.5
		/// </p>
		/// <p>
		/// :TODO: potential optimization is to just flat out return 1.0f if numTerms
		/// is between min and max.
		/// </p>
		/// </remarks>
		/// <seealso cref="SetLengthNormFactors(int, int, float, bool)">SetLengthNormFactors(int, int, float, bool)
		/// 	</seealso>
		/// <seealso><a href="doc-files/ss.computeLengthNorm.svg">An SVG visualization of this function</a>
		/// 	</seealso>
		public virtual float ComputeLengthNorm(int numTerms)
		{
			int l = ln_min;
			int h = ln_max;
			float s = ln_steep;
			return (float)(1.0f / Math.Sqrt((s * (float)(Math.Abs(numTerms - l) + Math.Abs(numTerms
				 - h) - (h - l))) + 1.0f));
		}

		/// <summary>Delegates to baselineTf</summary>
		/// <seealso cref="BaselineTf(float)">BaselineTf(float)</seealso>
		public override float Tf(float freq)
		{
			return BaselineTf(freq);
		}

		/// <summary>
		/// Implemented as:
		/// <code>
		/// (x &lt;= min) &#63; base : sqrt(x+(base**2)-min)
		/// </code>
		/// ...but with a special case check for 0.
		/// </summary>
		/// <remarks>
		/// Implemented as:
		/// <code>
		/// (x &lt;= min) &#63; base : sqrt(x+(base**2)-min)
		/// </code>
		/// ...but with a special case check for 0.
		/// <p>
		/// This degrates to <code>sqrt(x)</code> when min and base are both 0
		/// </p>
		/// </remarks>
		/// <seealso cref="SetBaselineTfFactors(float, float)">SetBaselineTfFactors(float, float)
		/// 	</seealso>
		/// <seealso><a href="doc-files/ss.baselineTf.svg">An SVG visualization of this function</a>
		/// 	</seealso>
		public virtual float BaselineTf(float freq)
		{
			if (0.0f == freq)
			{
				return 0.0f;
			}
			return (freq <= tf_min) ? tf_base : (float)Math.Sqrt(freq + (tf_base * tf_base) -
				 tf_min);
		}

		/// <summary>Uses a hyperbolic tangent function that allows for a hard max...</summary>
		/// <remarks>
		/// Uses a hyperbolic tangent function that allows for a hard max...
		/// <code>
		/// tf(x)=min+(max-min)/2*(((base**(x-xoffset)-base**-(x-xoffset))/(base**(x-xoffset)+base**-(x-xoffset)))+1)
		/// </code>
		/// <p>
		/// This code is provided as a convenience for subclasses that want
		/// to use a hyperbolic tf function.
		/// </p>
		/// </remarks>
		/// <seealso cref="SetHyperbolicTfFactors(float, float, double, float)">SetHyperbolicTfFactors(float, float, double, float)
		/// 	</seealso>
		/// <seealso><a href="doc-files/ss.hyperbolicTf.svg">An SVG visualization of this function</a>
		/// 	</seealso>
		public virtual float HyperbolicTf(float freq)
		{
			if (0.0f == freq)
			{
				return 0.0f;
			}
			float min = tf_hyper_min;
			float max = tf_hyper_max;
			double @base = tf_hyper_base;
			float xoffset = tf_hyper_xoffset;
			double x = (double)(freq - xoffset);
			float result = min + (float)((max - min) / 2.0f * (((Math.Pow(@base, x) - Math.Pow
				(@base, -x)) / (Math.Pow(@base, x) + Math.Pow(@base, -x))) + 1.0d));
			return float.IsNaN(result) ? max : result;
		}
	}
}
