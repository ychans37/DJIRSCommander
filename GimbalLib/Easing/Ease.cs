using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DJIRSCommander.Easing
{
     // t : 현재 시간, b: 시작 값, c: 변화된 값,  d: 시작시간부터 지난 시간
     // 시간, 값 이라고만 표현되어 있으니 너무 모호하다면 더 풀어서 작성하겠다.
     // Unity3D 기준 
     /*
        t: 현재 Frame 수(시작Frame부터 n 밀리세컨드 동안 지난 Frame수)
        b: 시작 위치(x 값 or y 값),
        c: 이동중인 현재 위치(x값 or y값),
        d: 총 frame 숫자(3초동안 이동한다고 하면 60frame*3 총 180을 넣으면 됨, )
     */
    public class Easing
    {
        public enum EasingType
        {
            Linear = 0,
            QuadIn,
            QuadOut,
            QuadInOut,
            CubicIn,
            CubicOut,
            CubicInOut,
            QuarticIn,
            QuarticOut,
            QuarticInOut,
            QuinticIn,
            QuinticOut,
            QuinticInOut,
            SinIn,
            SinOut,
            SinInOut,
            ExpoIn,
            ExpoOut,
            ExpoInOut,
            CircularIn,
            CircularOut,
            CircularInOut,
            ElasticIn,
            ElasticOut,
            ElasticInOut,
            BounceIn,
            BounceOut,
            BounceInOut
        }


        public static float GetEasingValue(EasingType type, float t, float b, float c, float d)
        {
            switch (type)
            {
                case EasingType.Linear: return Linear(t, b, c, d);
                case EasingType.QuadIn: return QuadIn(t, b, c, d);
                case EasingType.QuadOut: return QuadOut(t, b, c, d);
                case EasingType.QuadInOut: return QuadInOut(t, b, c, d);
                case EasingType.CubicIn: return CubicIn(t, b, c, d);
                case EasingType.CubicOut: return CubicOut(t, b, c, d);
                case EasingType.CubicInOut: return CubicInOut(t, b, c, d);
                case EasingType.QuarticIn: return QuarticIn(t, b, c, d);
                case EasingType.QuarticOut: return QuarticOut(t, b, c, d);
                case EasingType.QuarticInOut: return QuarticInOut(t, b, c, d);
                case EasingType.QuinticIn: return QuinticIn(t, b, c, d);
                case EasingType.QuinticOut: return QuinticOut(t, b, c, d);
                case EasingType.QuinticInOut: return QuinticInOut(t, b, c, d);
                case EasingType.SinIn: return SinIn(t, b, c, d);
                case EasingType.SinOut: return SinOut(t, b, c, d);
                case EasingType.SinInOut: return SinInOut(t, b, c, d);
                case EasingType.ExpoIn: return ExpoIn(t, b, c, d);
                case EasingType.ExpoOut: return ExpoOut(t, b, c, d);
                case EasingType.ExpoInOut: return ExpoInOut(t, b, c, d);
                case EasingType.CircularIn: return CircularIn(t, b, c, d);
                case EasingType.CircularOut: return CircularOut(t, b, c, d);
                case EasingType.CircularInOut: return CircularInOut(t, b, c, d);
                case EasingType.ElasticIn: return ElasticEaseIn(t, b, c, d);
                case EasingType.ElasticOut: return ElasticEaseOut(t, b, c, d);
                case EasingType.ElasticInOut: return ElasticEaseInOut(t, b, c, d);
                case EasingType.BounceIn: return BounceEaseIn(t, b, c, d);
                case EasingType.BounceOut: return BounceEaseOut(t, b, c, d);
                case EasingType.BounceInOut: return BounceEaseInOut(t, b, c, d);
            }
            return 0.0f;
        }


        public static float Linear(float t, float b, float c, float d)
        {
            return c * t / d + b;
        }

        public static float QuadIn(float t, float b, float c, float d)
        {
            t /= d;
            return c * t * t + b;
        }

        public static float QuadOut(float t, float b, float c, float d)
        {
            t /= d;
            return -c * t * (t - 2) + b;
        }

        public static float QuadInOut(float t, float b, float c, float d)
        {
            t /= d / 2;
            if (t < 1) return c / 2 * t * t + b;
            t--;
            return -c / 2 * (t * (t - 2) - 1) + b;
        }

        public static float CubicIn(float t, float b, float c, float d)
        {
            t /= d;
            return c * t * t * t + b;
        }

        public static float CubicOut(float t, float b, float c, float d)
        {
            t /= d;
            t--;
            return c * (t * t * t + 1) + b;
        }

        public static float CubicInOut(float t, float b, float c, float d)
        {
            t /= d / 2;
            if (t < 1) return c / 2 * t * t * t + b;
            t -= 2;
            return c / 2 * (t * t * t + 2) + b;
        }

        public static float QuarticIn(float t, float b, float c, float d)
        {
            t /= d;
            return c * t * t * t * t + b;
        }

        public static float QuarticOut(float t, float b, float c, float d)
        {
            t /= d;
            t--;
            return -c * (t * t * t * t - 1) + b;
        }

        public static float QuarticInOut(float t, float b, float c, float d)
        {
            t /= d / 2;
            if (t < 1) return c / 2 * t * t * t * t + b;
            t -= 2;
            return -c / 2 * (t * t * t * t - 2) + b;
        }

        public static float QuinticIn(float t, float b, float c, float d)
        {
            t /= d;
            return c * t * t * t * t * t + b;
        }

        public static float QuinticOut(float t, float b, float c, float d)
        {
            t /= d;
            t--;
            return c * (t * t * t * t * t + 1) + b;
        }

        public static float QuinticInOut(float t, float b, float c, float d)
        {
            t /= d / 2;
            if (t < 1) return c / 2 * t * t * t * t * t + b;
            t -= 2;
            return c / 2 * (t * t * t * t * t + 2) + b;
        }

        public static float SinIn(float t, float b, float c, float d)
        {
            return -c * MathF.Cos(t / d * (MathF.PI / 2)) + c + b;
        }

        public static float SinOut(float t, float b, float c, float d)
        {
            return c * MathF.Sin(t / d * (MathF.PI / 2)) + b;
        }

        public static float SinInOut(float t, float b, float c, float d)
        {
            return -c / 2 * (MathF.Cos(MathF.PI * t / d) - 1) + b;
        }
        //

        public static float ExpoIn(float t, float b, float c, float d)
        {
            return c * MathF.Pow(2, 10.0f * (t / d - 1)) + b;
        }

        public static float ExpoOut(float t, float b, float c, float d)
        {
            return c * (-MathF.Pow(2, -10 * t / d) + 1) + b;
        }

        public static float ExpoInOut(float t, float b, float c, float d)
        {
            t /= d / 2;
            if (t < 1) return c / 2 * MathF.Pow(2, 10 * (t - 1)) + b;
            t--;
            return c / 2 * (-MathF.Pow(2, -10 * t) + 2) + b;
        }

        public static float CircularIn(float t, float b, float c, float d)
        {
            t /= d;
            return -c * (MathF.Sqrt(1 - t * t) - 1) + b;
        }

        public static float CircularOut(float t, float b, float c, float d)
        {
            t /= d;
            t--;
            return c * MathF.Sqrt(1 - t * t) + b;
        }

        public static float CircularInOut(float t, float b, float c, float d)
        {
            t /= d / 2;
            if (t < 1) return -c / 2 * (MathF.Sqrt(1 - t * t) - 1) + b;
            t -= 2;
            return c / 2 * (MathF.Sqrt(1 - t * t) + 1) + b;
        }

        /*
         * @descripton http://robertpenner.com/easing/
         */
        public static float ElasticEaseOut(float t, float b, float c, float d)
        {

            if ((t /= d) == 1)
                return b + c;

            float p = d * .3f;
            float s = p / 4;

            return (c * MathF.Pow(2, -10 * t) * MathF.Sin((t * d - s) * (2 * MathF.PI) / p) + c + b);
        }

        /*
         * @descripton http://robertpenner.com/easing/
         */
        public static float ElasticEaseIn(float t, float b, float c, float d)
        {
            if ((t /= d) == 1)
                return b + c;

            float p = d * .3f;
            float s = p / 4f;

            return -(c * MathF.Pow(2, 10 * (t -= 1)) * MathF.Sin((t * d - s) * (2 * MathF.PI) / p)) + b;
        }
        /*
         * @descripton http://robertpenner.com/easing/
         */
        public static float ElasticEaseInOut(float t, float b, float c, float d)
        {
            if ((t /= d / 2) == 2)
                return b + c;

            float p = d * (.3f * 1.5f);
            float s = p / 4;

            if (t < 1)
                return -.5f * (c * MathF.Pow(2, 10 * (t -= 1)) * MathF.Sin((t * d - s) * (2 * MathF.PI) / p)) + b;
            return c * MathF.Pow(2, -10 * (t -= 1)) * MathF.Sin((t * d - s) * (2 * MathF.PI) / p) * .5f + c + b;
        }
        /*
         * @descripton http://robertpenner.com/easing/
         */
        public static float BounceEaseOut(float t, float b, float c, float d)
        {
            if ((t /= d) < (1 / 2.75f))
                return c * (7.5625f * t * t) + b;
            else if (t < (2 / 2.75f))
                return c * (7.5625f * (t -= (1.5f / 2.75f)) * t + .75f) + b;
            else if (t < (2.5f / 2.75f))
                return c * (7.5625f * (t -= (2.25f / 2.75f)) * t + .9375f) + b;
            else
                return c * (7.5625f * (t -= (2.625f / 2.75f)) * t + .984375f) + b;
        }
        /*
         * @descripton http://robertpenner.com/easing/
         */
        public static float BounceEaseIn(float t, float b, float c, float d)
        {
            return c - Easing.BounceEaseOut(d - t, 0, c, d) + b;
        }

        /*
         * @descripton http://robertpenner.com/easing/
         */
        public static float BounceEaseInOut(float t, float b, float c, float d)
        {
            if (t < d / 2)
                return Easing.BounceEaseIn(t * 2, 0, c, d) * .5f + b;
            else
                return Easing.BounceEaseOut(t * 2 - d, 0, c, d) * .5f + c * .5f + b;
        }

        /*
         * @descripton http://robertpenner.com/easing/
         */
        public static float BounceEaseOutIn(float t, float b, float c, float d)
        {

            if (t < d / 2)
                return Easing.BounceEaseOut(t * 2, b, c / 2, d);
            return Easing.BounceEaseIn((t * 2) - d, b + c / 2, c / 2, d);
        }
    }
}
