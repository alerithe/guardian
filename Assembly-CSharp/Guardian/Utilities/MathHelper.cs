﻿using System;

namespace Guardian.Utilities
{
    class MathHelper
    {
        private static readonly Random RnGen = new Random();

        public static int Abs(int val)
        {
            return val < 0 ? -val : val;
        }

        public static int Floor(float val)
        {
            int n = (int)val;
            return n <= val ? n : n - 1;
        }

        // Min-inclusive, max-exclusive
        public static int RandomInt(int min, int max)
        {
            return RnGen.Next(min, max);
        }

        // Thank you, https://stackoverflow.com/a/45859570
        public static int NextPowerOf2(int val)
        {
            int n = val;

            n--;
            n |= n >> 1;
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;
            n++;

            return n;
        }

        // Yeah, I took this from RCextensions
        public static bool IsPowerOf2(int val)
        {
            return (val & (val - 1)) == 0;
        }
    }
}
