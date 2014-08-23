


namespace Lucene.Net
{
    using System;

    public static class RandomExtensions
    {

        public static int AtLeast(this Random random, int minimumValue)
        {
            minimumValue = Settings.Nightly ? (2*minimumValue) : minimumValue;
            minimumValue = minimumValue*Settings.RandomMultiplier;

            // Even with the current short number of tests,
            // some of tests take too long to complete due to the
            // number of loops and computations created by using
            // high numbers with AtLeast. This is to cut
            // down on local development test time.
            if (!Settings.Nightly && minimumValue > 100)
            {
                minimumValue = 100;
            }

            var max = minimumValue + (minimumValue / 2);
            return random.Next(minimumValue, max);
        }
    }
}
