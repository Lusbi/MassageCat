using System;
using System.Numerics;
using System.Security.Cryptography;

namespace BigMath
{
    /// <summary>
    /// 薄包裝 BigInteger，提供符號運算（+ - * / ^）與比較運算（== != < <= > >=）
    /// 注意：此型別將 ^ 視為「冪次」，僅限右側為 int 的情境
    /// </summary>
    public readonly struct BigNumber : IComparable<BigNumber>, IEquatable<BigNumber>
    {
        public BigInteger Value { get; }

        public BigNumber(BigInteger value) => Value = value;

        // 隱式轉換，方便直接用整數或 BigInteger 建立 BigNumber
        public static implicit operator BigNumber(int v) => new BigNumber(new BigInteger(v));
        public static implicit operator BigNumber(long v) => new BigNumber(new BigInteger(v));
        public static implicit operator BigNumber(BigInteger v) => new BigNumber(v);

        // 需要時也可取回 BigInteger
        public static implicit operator BigInteger(BigNumber v) => v.Value;

        // 1) 算術運算 + - * /
        public static BigNumber operator +(BigNumber a, BigNumber b) => new BigNumber(a.Value + b.Value);
        public static BigNumber operator -(BigNumber a, BigNumber b) => new BigNumber(a.Value - b.Value);
        public static BigNumber operator *(BigNumber a, BigNumber b) => new BigNumber(a.Value * b.Value);
        public static BigNumber operator /(BigNumber a, BigNumber b) => new BigNumber(a.Value / b.Value);

        // 2) 冪次：以 ^ 表示，限定右側為 int
        public static BigNumber operator ^(BigNumber a, int exponent)
        {
            return new BigNumber(BigInteger.Pow(a.Value, exponent));
        }

        // 也提供明確的方法版本（當 exponent 需要檢查或來自 BigInteger 時）
        public static BigNumber Pow(BigNumber a, BigInteger exponent)
        {
            if (exponent > int.MaxValue || exponent < int.MinValue)
                throw new ArgumentOutOfRangeException(nameof(exponent), "次方指數必須在 int 範圍內");
            return new BigNumber(BigInteger.Pow(a.Value, (int)exponent));
        }

        // 3) 比較運算 == != < <= > >= 需要成對實作
        public static bool operator ==(BigNumber a, BigNumber b) => a.Value == b.Value;
        public static bool operator !=(BigNumber a, BigNumber b) => a.Value != b.Value;
        public static bool operator <(BigNumber a, BigNumber b) => a.Value < b.Value;
        public static bool operator >(BigNumber a, BigNumber b) => a.Value > b.Value;
        public static bool operator <=(BigNumber a, BigNumber b) => a.Value <= b.Value;
        public static bool operator >=(BigNumber a, BigNumber b) => a.Value >= b.Value;

        // 4) 介面實作
        public int CompareTo(BigNumber other) => Value.CompareTo(other.Value);
        public bool Equals(BigNumber other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is BigNumber bn && Equals(bn);
        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString();

        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        public static BigNumber Range(BigNumber min, BigNumber max)
        {
            if (min > max)
            {
                (min, max) = (max, min);
            }

            var span = max.Value - min.Value;
            if (span.IsZero)
            {
                return min;
            }

            var offset = NextBigInteger(span + BigInteger.One);
            return new BigNumber(min.Value + offset);
        }

        public static BigNumber RandomSelf(BigNumber bigNumber, float x)
        {
            BigInteger maxScaled = new BigInteger(1 + x);
            BigInteger minScaled = new BigInteger(1 - x);
            var max = new BigNumber(bigNumber.Value * maxScaled);
            var min = new BigNumber(bigNumber.Value * minScaled);
            return Range(min, max);
        }

        private static BigInteger NextBigInteger(BigInteger upperExclusive)
        {
            if (upperExclusive <= BigInteger.Zero)
                throw new ArgumentOutOfRangeException(nameof(upperExclusive), "Upper bound must be positive.");

            var buffer = upperExclusive.ToByteArray();
            BigInteger result;

            do
            {
                Rng.GetBytes(buffer);
                buffer[^1] &= 0x7F;
                result = new BigInteger(buffer);
            } while (result >= upperExclusive);

            return result;
        }

        private static (BigInteger numerator, BigInteger denominator) ConvertDecimalToFraction(decimal value)
        {
            int[] bits = decimal.GetBits(value);
            int scale = (bits[3] >> 16) & 0xFF;

            BigInteger numerator = (uint)bits[0];
            numerator += (BigInteger)(uint)bits[1] << 32;
            numerator += (BigInteger)(uint)bits[2] << 64;

            if (scale == 0)
            {
                return (numerator, BigInteger.One);
            }

            var denominator = BigInteger.Pow(10, scale);
            return (numerator, denominator);
        }
    
    }

    /// <summary>
    /// 依你需求保留 Utils：提供 a ⊕ b = result 的字串格式工具
    /// </summary>
    public static class BigIntegerUtils
    {
        public enum Operator
        {
            Add, Subtract, Multiply, Divide, Pow, Compare
        }

        public static string ToSymbol(Operator op) => op switch
        {
            Operator.Add => "+",
            Operator.Subtract => "-",
            Operator.Multiply => "*",
            Operator.Divide => "/",
            Operator.Pow => "^",
            Operator.Compare => "cmp",
            _ => "?"
        };

        /// <summary>
        /// 回傳字串："a op b = result"，若為 Compare 則輸出 a ? b（<, =, >）
        /// </summary>
        public static string FormatEval(BigNumber a, Operator op, BigNumber b)
        {
            switch (op)
            {
                case Operator.Add: return $"{a} + {b} = {a + b}";
                case Operator.Subtract: return $"{a} - {b} = {a - b}";
                case Operator.Multiply: return $"{a} * {b} = {a * b}";
                case Operator.Divide: return $"{a} / {b} = {a / b}";
                case Operator.Pow:
                    // 這裡用 ^ 的 int 版本，若需要 BigInteger 指數請改用 BigNumber.Pow(a, exponent)
                    if (b.Value > int.MaxValue || b.Value < int.MinValue)
                        throw new ArgumentOutOfRangeException(nameof(b), "次方指數必須在 int 範圍內");
                    return $"{a} ^ {b} = {(a ^ (int)b.Value)}";
                case Operator.Compare:
                    var cmp = a.CompareTo(b);
                    var sign = cmp < 0 ? "<" : (cmp > 0 ? ">" : "=");
                    return $"{a} {sign} {b}";
                default:
                    throw new NotSupportedException($"不支援的運算: {op}");
            }
        }
    }
}
