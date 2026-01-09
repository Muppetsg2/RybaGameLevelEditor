using System;
using System.Runtime.CompilerServices;
using M = System.Runtime.CompilerServices.MethodImplAttribute;

namespace QOI
{
    public enum QOIChannels : byte
    {
        Rgb = 3,
        Rgba = 4,
    }

    public enum QOIColorSpace : byte
    {
        SRgb = 0,
        Linear = 1,
    }

    public struct QOIPixel : IEquatable<QOIPixel>
    {
        private const MethodImplOptions IL = MethodImplOptions.AggressiveInlining;

        [M(IL)]
        public override int GetHashCode() => HashCode.Combine(R, G, B, A);

        public byte R;
        public byte G;
        public byte B;
        public byte A;

        [M(IL)]
        public QOIPixel(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
        [M(IL)]
        public QOIPixel(float r, float g, float b, float a)
        {
            R = (byte)r;
            G = (byte)g;
            B = (byte)b;
            A = (byte)a;
        }

        public override bool Equals(object obj) => obj is QOIPixel other && Equals(other);

        [M(IL)]
        public bool Equals(QOIPixel other)
        {
            return R == other.R
                && G == other.G
                && B == other.B
                && A == other.A;
        }

        [M(IL)]
        public static bool operator ==(QOIPixel a, QOIPixel b) => a.Equals(b);

        [M(IL)]
        public static bool operator !=(QOIPixel a, QOIPixel b)
        {
            return a.R != b.R
                   || a.G != b.G
                   || a.B != b.B
                   || a.A != b.A;
        }

        [M(IL)] public static QOIPixel operator *(QOIPixel a, QOIPixel b) => new(a.R * b.R, a.G * b.G, a.B * b.B, a.A * b.A);

        [M(IL)] public static QOIPixel operator *(QOIPixel a, float b) => new((a.R * b), (a.G * b), (a.B * b), (a.A * b));

        [M(IL)] public static QOIPixel operator *(QOIPixel a, int b) => new(a.R * b, a.G * b, a.B * b, a.A * b);

        [M(IL)] public static QOIPixel operator *(QOIPixel a, uint b) => new(a.R * b, a.G * b, a.B * b, a.A * b);

        [M(IL)] public static QOIPixel operator *(QOIPixel a, byte b) => new(a.R * b, a.G * b, a.B * b, a.A * b);

        [M(IL)] public static QOIPixel operator /(QOIPixel a, QOIPixel b) => new(a.R / b.R, a.G / b.G, a.B / b.B, a.A / b.A);

        [M(IL)] public static QOIPixel operator /(QOIPixel a, float b) => new((a.R / b), (a.G / b), (a.B / b), (a.A / b));

        [M(IL)] public static QOIPixel operator /(QOIPixel a, int b) => new(a.R / b, a.G / b, a.B / b, a.A / b);

        [M(IL)] public static QOIPixel operator /(QOIPixel a, uint b) => new(a.R / b, a.G / b, a.B / b, a.A / b);

        [M(IL)] public static QOIPixel operator /(QOIPixel a, byte b) => new(a.R / b, a.G / b, a.B / b, a.A / b);


        [M(IL)] public static QOIPixel operator +(QOIPixel a, QOIPixel b) => new(a.R + b.R, a.G + b.G, a.B + b.B, a.A + b.A);

        [M(IL)] public static QOIPixel operator +(QOIPixel a, float b) => new((a.R + b), (a.G + b), (a.B + b), (a.A + b));

        [M(IL)] public static QOIPixel operator +(QOIPixel a, int b) => new(a.R + b, a.G + b, a.B + b, a.A + b);

        [M(IL)] public static QOIPixel operator +(QOIPixel a, uint b) => new(a.R + b, a.G + b, a.B + b, a.A + b);

        [M(IL)] public static QOIPixel operator +(QOIPixel a, byte b) => new(a.R + b, a.G + b, a.B + b, a.A + b);


        [M(IL)] public static QOIPixel operator -(QOIPixel a, QOIPixel b) => new(a.R - b.R, a.G - b.G, a.B - b.B, a.A - b.A);

        [M(IL)] public static QOIPixel operator -(QOIPixel a, float b) => new((a.R - b), (a.G - b), (a.B - b), (a.A - b));

        [M(IL)] public static QOIPixel operator -(QOIPixel a, int b) => new(a.R - b, a.G - b, a.B - b, a.A - b);

        [M(IL)] public static QOIPixel operator -(QOIPixel a, uint b) => new(a.R - b, a.G - b, a.B - b, a.A - b);

        [M(IL)] public static QOIPixel operator -(QOIPixel a, byte b) => new(a.R - b, a.G - b, a.B - b, a.A - b);


        [M(IL)] public static QOIPixel operator %(QOIPixel a, QOIPixel b) => new(a.R % b.R, a.G % b.G, a.B % b.B, a.A % b.A);

        [M(IL)] public static QOIPixel operator %(QOIPixel a, float b) => new((a.R % b), (a.G % b), (a.B % b), (a.A % b));

        [M(IL)] public static QOIPixel operator %(QOIPixel a, int b) => new(a.R % b, a.G % b, a.B % b, a.A % b);

        [M(IL)] public static QOIPixel operator %(QOIPixel a, uint b) => new(a.R % b, a.G % b, a.B % b, a.A % b);

        [M(IL)] public static QOIPixel operator %(QOIPixel a, byte b) => new(a.R % b, a.G % b, a.B % b, a.A % b);
    }
}