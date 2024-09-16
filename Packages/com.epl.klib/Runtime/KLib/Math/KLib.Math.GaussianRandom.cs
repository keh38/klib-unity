using UnityEngine;
using System.Collections.Generic;

namespace KLib.Math
{
    /// <summary>
    /// Gaussian random number generator.
    /// </summary>
    /// <remarks>
    /// Based on code found on <see href="http://stackoverflow.com/questions/218060/random-gaussian-variables">StackOverflow</see> 
    /// </remarks>
    public class GaussianRandom
    {
        private bool _hasDeviate;
        private float _storedDeviate;
		
		private System.Random _random;
		
        /// <summary>
        /// Create GaussianRandom object.
        /// </summary>
        /// <param name="random">Optional object of class <see cref="Random"/>, a uniform random number generator.</param>
		public GaussianRandom(int seed):this()
        {
            _random = new System.Random(seed);
//            Random.seed = seed;
        }

		public GaussianRandom()
		{
			_random = new System.Random();
		}
		
		public float Next()
		{
			return Next(0, 1);
		}
		
        /// <summary>
        /// Generates normally (Gaussian) distributed random numbers, using the Box-Muller
        /// transformation.  This transformation takes two uniformly distributed deviates
        /// within the unit circle, and transforms them into two independently
        /// distributed normal deviates.
        /// </summary>
        /// <param name="mu">The mean of the distribution.  Default is zero.</param>
        /// <param name="sigma">The standard deviation of the distribution.  Default is one.</param>
        /// <returns>Random number ~ N(<paramref name="mu"/>,<paramref name="sigma"/>)</returns>
        /// <exception cref="ArgumentOutOfRangeException">Standard deviation <paramref name="sigma"/> must be positive.</exception>
        public float Next(float mu, float sigma)
        {
//            if (sigma <= 0)
//                throw new ArgumentOutOfRangeException("sigma", "Must be greater than zero.");

            if (_hasDeviate)
            {
                _hasDeviate = false;
                return _storedDeviate * sigma + mu;
            }

            float v1, v2, rSquared;
            do
            {
                // two random values between -1.0 and 1.0
//                v1 = 2 * Random.value - 1;
//                v2 = 2 * Random.value - 1;
                v1 = 2 * (float)_random.NextDouble() - 1;
                v2 = 2 * (float)_random.NextDouble() - 1;
                rSquared = v1 * v1 + v2 * v2;
                // ensure within the unit circle
            } while (rSquared >= 1 || rSquared == 0);

            // calculate polar tranformation for each deviate
            var polar = Mathf.Sqrt(-2 * Mathf.Log(rSquared) / rSquared);
            // store first deviate
            _storedDeviate = v2 * polar;
            _hasDeviate = true;
            // return second deviate
            return v1 * polar * sigma + mu;
        }
    }
}
