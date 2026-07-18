using System;
using System.Collections.Generic;
using System.Numerics;

namespace MassageCat.EditorTools
{
    internal enum DamageSimulationRoute
    {
        Normal,
        Spell,
        Tomato,
    }

    internal enum DamageSimulationActionType
    {
        None,
        NormalHit,
        SpellWord,
        TomatoSession,
    }

    internal enum DamageSimulationError
    {
        None,
        InvalidInputRate,
        InvalidWordsPerMinute,
        InvalidDamage,
        InvalidHealth,
        MissingSpellQuestions,
        MissingTomatoDuration,
        TomatoSessionCapacityZero,
    }

    internal sealed class DamageSimulationRoleInput
    {
        public int SortId;
        public string Zone;
        public string RoleKey;
        public BigInteger OriginalFatigue;
        public int SatisfyBonus;
        public int TomatoBonus;
        public decimal TomatoSeconds;
        public bool HasSpellQuestions;
    }

    internal struct DamageSimulationSettings
    {
        public BigInteger BaseHeal;
        public BigInteger SpellMultiplier;
        public BigInteger DebugMultiplier;
        public decimal InputsPerSecond;
        public decimal WordsPerMinute;
        public int SpellUnlockSortId;
        public int TomatoUnlockSortId;
    }

    internal struct DamageSimulationStepResult
    {
        public DamageSimulationActionType ActionType;
        public BigInteger DamagePerAction;
        public BigInteger RequiredNormalHits;
        public BigInteger RequiredSpellWords;
        public BigInteger RequiredTomatoSessions;
        public decimal Seconds;
        public bool DurationOverflow;
    }

    internal struct DamageSimulationRoleResult
    {
        public DamageSimulationStepResult Normal;
        public DamageSimulationStepResult Spell;
        public DamageSimulationStepResult Tomato;
    }

    internal sealed class DamageSimulationRouteResult
    {
        public bool IsValid { get; private set; }
        public DamageSimulationError Error { get; private set; }
        public int ErrorRoleIndex { get; private set; }
        public BigInteger TotalNormalHits { get; private set; }
        public BigInteger TotalSpellWords { get; private set; }
        public BigInteger TotalTomatoSessions { get; private set; }
        public decimal TotalSeconds { get; private set; }
        public bool DurationOverflow { get; private set; }

        public void Reset()
        {
            IsValid = true;
            Error = DamageSimulationError.None;
            ErrorRoleIndex = -1;
            TotalNormalHits = BigInteger.Zero;
            TotalSpellWords = BigInteger.Zero;
            TotalTomatoSessions = BigInteger.Zero;
            TotalSeconds = decimal.Zero;
            DurationOverflow = false;
        }

        public void Invalidate(DamageSimulationError error, int roleIndex = -1)
        {
            IsValid = false;
            Error = error;
            ErrorRoleIndex = roleIndex;
        }

        public void Add(in DamageSimulationStepResult step)
        {
            TotalNormalHits += step.RequiredNormalHits;
            TotalSpellWords += step.RequiredSpellWords;
            TotalTomatoSessions += step.RequiredTomatoSessions;

            if (DurationOverflow || step.DurationOverflow || decimal.MaxValue - TotalSeconds < step.Seconds)
            {
                TotalSeconds = decimal.MaxValue;
                DurationOverflow = true;
                return;
            }

            TotalSeconds += step.Seconds;
        }
    }

    internal sealed class DamageSimulationResult
    {
        public DamageSimulationRoleResult[] RoleResults { get; }
        public DamageSimulationRouteResult Normal { get; } = new DamageSimulationRouteResult();
        public DamageSimulationRouteResult Spell { get; } = new DamageSimulationRouteResult();
        public DamageSimulationRouteResult Tomato { get; } = new DamageSimulationRouteResult();

        public DamageSimulationResult(int roleCount)
        {
            RoleResults = new DamageSimulationRoleResult[roleCount];
            Reset();
        }

        public void Reset()
        {
            Array.Clear(RoleResults, 0, RoleResults.Length);
            Normal.Reset();
            Spell.Reset();
            Tomato.Reset();
        }

        public DamageSimulationRouteResult GetRoute(DamageSimulationRoute route)
        {
            switch (route)
            {
                case DamageSimulationRoute.Normal:
                    return Normal;
                case DamageSimulationRoute.Spell:
                    return Spell;
                case DamageSimulationRoute.Tomato:
                    return Tomato;
                default:
                    throw new ArgumentOutOfRangeException(nameof(route), route, null);
            }
        }
    }

    internal static class DamageSimulationCalculator
    {
        private static readonly BigInteger s_decimalMaxValue = new BigInteger(decimal.MaxValue);

        public static void CalculateAll(
            IReadOnlyList<DamageSimulationRoleInput> roles,
            IReadOnlyList<BigInteger> fatigueValues,
            in DamageSimulationSettings settings,
            DamageSimulationResult output)
        {
            if (roles == null)
                throw new ArgumentNullException(nameof(roles));
            if (fatigueValues == null)
                throw new ArgumentNullException(nameof(fatigueValues));
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (roles.Count != fatigueValues.Count || output.RoleResults.Length != roles.Count)
                throw new ArgumentException("Role, fatigue, and output counts must match.");

            output.Reset();

            if (settings.InputsPerSecond <= decimal.Zero)
            {
                InvalidateAll(output, DamageSimulationError.InvalidInputRate);
                return;
            }
            if (settings.WordsPerMinute <= decimal.Zero)
            {
                InvalidateAll(output, DamageSimulationError.InvalidWordsPerMinute);
                return;
            }
            if (settings.BaseHeal <= BigInteger.Zero ||
                settings.SpellMultiplier <= BigInteger.Zero ||
                settings.DebugMultiplier <= BigInteger.Zero)
            {
                InvalidateAll(output, DamageSimulationError.InvalidDamage);
                return;
            }

            for (int i = 0; i < fatigueValues.Count; i++)
            {
                if (fatigueValues[i] > BigInteger.Zero)
                    continue;

                InvalidateAll(output, DamageSimulationError.InvalidHealth, i);
                return;
            }

            CalculateNormalRoute(roles, fatigueValues, settings, output);
            CalculateSpellRoute(roles, fatigueValues, settings, output);
            CalculateTomatoRoute(roles, fatigueValues, settings, output);
        }

        public static bool TrySolveTarget(
            IReadOnlyList<DamageSimulationRoleInput> roles,
            IReadOnlyList<BigInteger> originalFatigueValues,
            in DamageSimulationSettings settings,
            DamageSimulationRoute referenceRoute,
            decimal targetSeconds,
            BigInteger[] bestFatigueValues,
            BigInteger[] candidateFatigueValues,
            DamageSimulationResult scratchResult,
            out decimal bestMultiplier,
            out decimal achievedSeconds,
            out bool durationOverflow,
            out DamageSimulationError error,
            out int errorRoleIndex)
        {
            bestMultiplier = decimal.Zero;
            achievedSeconds = decimal.Zero;
            durationOverflow = false;
            error = DamageSimulationError.None;
            errorRoleIndex = -1;

            if (targetSeconds <= decimal.Zero)
                return false;
            if (roles == null || originalFatigueValues == null || bestFatigueValues == null ||
                candidateFatigueValues == null || scratchResult == null)
                return false;
            if (roles.Count != originalFatigueValues.Count ||
                bestFatigueValues.Length != roles.Count ||
                candidateFatigueValues.Length != roles.Count ||
                scratchResult.RoleResults.Length != roles.Count)
                return false;

            bool hasBest = false;
            decimal bestDistance = decimal.MaxValue;

            ApplyScale(originalFatigueValues, decimal.Zero, candidateFatigueValues);
            if (!EvaluateCandidate(
                    roles,
                    candidateFatigueValues,
                    settings,
                    referenceRoute,
                    scratchResult,
                    out decimal minimumSeconds,
                    out bool minimumOverflow,
                    out error,
                    out errorRoleIndex))
                return false;

            UpdateBestCandidate(
                candidateFatigueValues,
                decimal.Zero,
                minimumSeconds,
                minimumOverflow,
                targetSeconds,
                bestFatigueValues,
                ref hasBest,
                ref bestDistance,
                ref bestMultiplier,
                ref achievedSeconds,
                ref durationOverflow);

            if (!minimumOverflow && minimumSeconds >= targetSeconds)
                return true;

            decimal lowMultiplier = decimal.Zero;
            decimal highMultiplier = decimal.One;

            ApplyScale(originalFatigueValues, highMultiplier, candidateFatigueValues);
            if (!EvaluateCandidate(
                    roles,
                    candidateFatigueValues,
                    settings,
                    referenceRoute,
                    scratchResult,
                    out decimal highSeconds,
                    out bool highOverflow,
                    out error,
                    out errorRoleIndex))
                return false;

            UpdateBestCandidate(
                candidateFatigueValues,
                highMultiplier,
                highSeconds,
                highOverflow,
                targetSeconds,
                bestFatigueValues,
                ref hasBest,
                ref bestDistance,
                ref bestMultiplier,
                ref achievedSeconds,
                ref durationOverflow);

            const int k_maxExpansionCount = 96;
            int expansionCount = 0;
            while (!highOverflow && highSeconds < targetSeconds && expansionCount < k_maxExpansionCount)
            {
                lowMultiplier = highMultiplier;
                if (highMultiplier > decimal.MaxValue / 2m)
                    break;

                highMultiplier *= 2m;
                ApplyScale(originalFatigueValues, highMultiplier, candidateFatigueValues);
                if (!EvaluateCandidate(
                        roles,
                        candidateFatigueValues,
                        settings,
                        referenceRoute,
                        scratchResult,
                        out highSeconds,
                        out highOverflow,
                        out error,
                        out errorRoleIndex))
                    return false;

                UpdateBestCandidate(
                    candidateFatigueValues,
                    highMultiplier,
                    highSeconds,
                    highOverflow,
                    targetSeconds,
                    bestFatigueValues,
                    ref hasBest,
                    ref bestDistance,
                    ref bestMultiplier,
                    ref achievedSeconds,
                    ref durationOverflow);

                expansionCount++;
            }

            const int k_binarySearchIterations = 80;
            for (int i = 0; i < k_binarySearchIterations; i++)
            {
                decimal middleMultiplier = lowMultiplier + ((highMultiplier - lowMultiplier) / 2m);
                if (middleMultiplier == lowMultiplier || middleMultiplier == highMultiplier)
                    break;

                ApplyScale(originalFatigueValues, middleMultiplier, candidateFatigueValues);
                if (!EvaluateCandidate(
                        roles,
                        candidateFatigueValues,
                        settings,
                        referenceRoute,
                        scratchResult,
                        out decimal middleSeconds,
                        out bool middleOverflow,
                        out error,
                        out errorRoleIndex))
                    return false;

                UpdateBestCandidate(
                    candidateFatigueValues,
                    middleMultiplier,
                    middleSeconds,
                    middleOverflow,
                    targetSeconds,
                    bestFatigueValues,
                    ref hasBest,
                    ref bestDistance,
                    ref bestMultiplier,
                    ref achievedSeconds,
                    ref durationOverflow);

                if (middleOverflow || middleSeconds >= targetSeconds)
                    highMultiplier = middleMultiplier;
                else
                    lowMultiplier = middleMultiplier;
            }

            return hasBest;
        }

        public static BigInteger CeilDivide(BigInteger numerator, BigInteger denominator)
        {
            if (numerator < BigInteger.Zero)
                throw new ArgumentOutOfRangeException(nameof(numerator));
            if (denominator <= BigInteger.Zero)
                throw new ArgumentOutOfRangeException(nameof(denominator));

            BigInteger quotient = BigInteger.DivRem(numerator, denominator, out BigInteger remainder);
            return remainder.IsZero ? quotient : quotient + BigInteger.One;
        }

        private static void CalculateNormalRoute(
            IReadOnlyList<DamageSimulationRoleInput> roles,
            IReadOnlyList<BigInteger> fatigueValues,
            in DamageSimulationSettings settings,
            DamageSimulationResult output)
        {
            BigInteger satisfyBonus = BigInteger.Zero;
            BigInteger tomatoBonus = BigInteger.Zero;

            for (int i = 0; i < roles.Count; i++)
            {
                DamageSimulationRoleInput role = roles[i];
                BigInteger damage = GetHealValue(settings, satisfyBonus, tomatoBonus);
                if (damage <= BigInteger.Zero)
                {
                    output.Normal.Invalidate(DamageSimulationError.InvalidDamage, i);
                    return;
                }

                BigInteger hitCount = CeilDivide(fatigueValues[i], damage);
                decimal seconds = SaturatingDivide(hitCount, settings.InputsPerSecond, out bool overflow);
                DamageSimulationStepResult step = CreateNormalStep(damage, hitCount, seconds, overflow);
                DamageSimulationRoleResult roleResult = output.RoleResults[i];
                roleResult.Normal = step;
                output.RoleResults[i] = roleResult;
                output.Normal.Add(step);

                satisfyBonus += role.SatisfyBonus;
            }
        }

        private static void CalculateSpellRoute(
            IReadOnlyList<DamageSimulationRoleInput> roles,
            IReadOnlyList<BigInteger> fatigueValues,
            in DamageSimulationSettings settings,
            DamageSimulationResult output)
        {
            BigInteger satisfyBonus = BigInteger.Zero;
            BigInteger tomatoBonus = BigInteger.Zero;

            for (int i = 0; i < roles.Count; i++)
            {
                DamageSimulationRoleInput role = roles[i];
                BigInteger healValue = GetHealValue(settings, satisfyBonus, tomatoBonus);
                if (healValue <= BigInteger.Zero)
                {
                    output.Spell.Invalidate(DamageSimulationError.InvalidDamage, i);
                    return;
                }

                DamageSimulationStepResult step;
                if (role.SortId <= settings.SpellUnlockSortId)
                {
                    BigInteger hitCount = CeilDivide(fatigueValues[i], healValue);
                    decimal seconds = SaturatingDivide(hitCount, settings.InputsPerSecond, out bool overflow);
                    step = CreateNormalStep(healValue, hitCount, seconds, overflow);
                    satisfyBonus += role.SatisfyBonus;
                }
                else
                {
                    if (!role.HasSpellQuestions)
                    {
                        output.Spell.Invalidate(DamageSimulationError.MissingSpellQuestions, i);
                        return;
                    }

                    BigInteger wordDamage = healValue * settings.SpellMultiplier;
                    if (wordDamage <= BigInteger.Zero)
                    {
                        output.Spell.Invalidate(DamageSimulationError.InvalidDamage, i);
                        return;
                    }

                    BigInteger wordCount = CeilDivide(fatigueValues[i], wordDamage);
                    decimal secondsPerWord = 60m / settings.WordsPerMinute;
                    decimal seconds = SaturatingMultiply(wordCount, secondsPerWord, out bool overflow);
                    step = new DamageSimulationStepResult
                    {
                        ActionType = DamageSimulationActionType.SpellWord,
                        DamagePerAction = wordDamage,
                        RequiredSpellWords = wordCount,
                        Seconds = seconds,
                        DurationOverflow = overflow,
                    };
                }

                DamageSimulationRoleResult roleResult = output.RoleResults[i];
                roleResult.Spell = step;
                output.RoleResults[i] = roleResult;
                output.Spell.Add(step);
            }
        }

        private static void CalculateTomatoRoute(
            IReadOnlyList<DamageSimulationRoleInput> roles,
            IReadOnlyList<BigInteger> fatigueValues,
            in DamageSimulationSettings settings,
            DamageSimulationResult output)
        {
            BigInteger satisfyBonus = BigInteger.Zero;
            BigInteger tomatoBonus = BigInteger.Zero;

            for (int i = 0; i < roles.Count; i++)
            {
                DamageSimulationRoleInput role = roles[i];
                BigInteger healValue = GetHealValue(settings, satisfyBonus, tomatoBonus);
                if (healValue <= BigInteger.Zero)
                {
                    output.Tomato.Invalidate(DamageSimulationError.InvalidDamage, i);
                    return;
                }

                DamageSimulationStepResult step;
                if (role.SortId <= settings.TomatoUnlockSortId)
                {
                    BigInteger hitCount = CeilDivide(fatigueValues[i], healValue);
                    decimal seconds = SaturatingDivide(hitCount, settings.InputsPerSecond, out bool overflow);
                    step = CreateNormalStep(healValue, hitCount, seconds, overflow);
                    satisfyBonus += role.SatisfyBonus;
                }
                else
                {
                    if (role.TomatoSeconds <= decimal.Zero)
                    {
                        output.Tomato.Invalidate(DamageSimulationError.MissingTomatoDuration, i);
                        return;
                    }

                    decimal sessionCapacityDecimal;
                    if (settings.InputsPerSecond > decimal.MaxValue / role.TomatoSeconds)
                    {
                        sessionCapacityDecimal = decimal.MaxValue;
                    }
                    else
                    {
                        sessionCapacityDecimal = decimal.Floor(settings.InputsPerSecond * role.TomatoSeconds);
                    }
                    if (sessionCapacityDecimal < decimal.One)
                    {
                        output.Tomato.Invalidate(DamageSimulationError.TomatoSessionCapacityZero, i);
                        return;
                    }

                    BigInteger hitCount = CeilDivide(fatigueValues[i], healValue);
                    BigInteger sessionCapacity = new BigInteger(sessionCapacityDecimal);
                    BigInteger sessionCount = CeilDivide(hitCount, sessionCapacity);
                    decimal seconds = SaturatingMultiply(sessionCount, role.TomatoSeconds, out bool overflow);
                    step = new DamageSimulationStepResult
                    {
                        ActionType = DamageSimulationActionType.TomatoSession,
                        DamagePerAction = healValue,
                        RequiredNormalHits = hitCount,
                        RequiredTomatoSessions = sessionCount,
                        Seconds = seconds,
                        DurationOverflow = overflow,
                    };

                    tomatoBonus += role.TomatoBonus;
                }

                DamageSimulationRoleResult roleResult = output.RoleResults[i];
                roleResult.Tomato = step;
                output.RoleResults[i] = roleResult;
                output.Tomato.Add(step);
            }
        }

        private static DamageSimulationStepResult CreateNormalStep(
            BigInteger damage,
            BigInteger hitCount,
            decimal seconds,
            bool overflow)
        {
            return new DamageSimulationStepResult
            {
                ActionType = DamageSimulationActionType.NormalHit,
                DamagePerAction = damage,
                RequiredNormalHits = hitCount,
                Seconds = seconds,
                DurationOverflow = overflow,
            };
        }

        private static BigInteger GetHealValue(
            in DamageSimulationSettings settings,
            BigInteger satisfyBonus,
            BigInteger tomatoBonus)
        {
            return (settings.BaseHeal + (satisfyBonus * (BigInteger.One + tomatoBonus))) * settings.DebugMultiplier;
        }

        private static decimal SaturatingDivide(BigInteger value, decimal divisor, out bool overflow)
        {
            overflow = false;
            if (value > s_decimalMaxValue)
            {
                overflow = true;
                return decimal.MaxValue;
            }

            decimal decimalValue = (decimal)value;
            if (divisor < decimal.One && decimalValue > decimal.MaxValue * divisor)
            {
                overflow = true;
                return decimal.MaxValue;
            }

            return decimalValue / divisor;
        }

        private static decimal SaturatingMultiply(BigInteger value, decimal multiplier, out bool overflow)
        {
            overflow = false;
            if (value > s_decimalMaxValue)
            {
                overflow = true;
                return decimal.MaxValue;
            }

            decimal decimalValue = (decimal)value;
            if (multiplier > decimal.Zero && decimalValue > decimal.MaxValue / multiplier)
            {
                overflow = true;
                return decimal.MaxValue;
            }

            return decimalValue * multiplier;
        }

        private static void InvalidateAll(
            DamageSimulationResult output,
            DamageSimulationError error,
            int roleIndex = -1)
        {
            output.Normal.Invalidate(error, roleIndex);
            output.Spell.Invalidate(error, roleIndex);
            output.Tomato.Invalidate(error, roleIndex);
        }

        private static bool EvaluateCandidate(
            IReadOnlyList<DamageSimulationRoleInput> roles,
            BigInteger[] candidateFatigueValues,
            in DamageSimulationSettings settings,
            DamageSimulationRoute referenceRoute,
            DamageSimulationResult scratchResult,
            out decimal seconds,
            out bool durationOverflow,
            out DamageSimulationError error,
            out int errorRoleIndex)
        {
            CalculateAll(roles, candidateFatigueValues, settings, scratchResult);
            DamageSimulationRouteResult routeResult = scratchResult.GetRoute(referenceRoute);
            seconds = routeResult.TotalSeconds;
            durationOverflow = routeResult.DurationOverflow;
            error = routeResult.Error;
            errorRoleIndex = routeResult.ErrorRoleIndex;
            return routeResult.IsValid;
        }

        private static void UpdateBestCandidate(
            BigInteger[] candidateFatigueValues,
            decimal multiplier,
            decimal seconds,
            bool overflow,
            decimal targetSeconds,
            BigInteger[] bestFatigueValues,
            ref bool hasBest,
            ref decimal bestDistance,
            ref decimal bestMultiplier,
            ref decimal achievedSeconds,
            ref bool durationOverflow)
        {
            decimal distance = overflow
                ? decimal.MaxValue
                : seconds >= targetSeconds ? seconds - targetSeconds : targetSeconds - seconds;

            bool isBetter = !hasBest || distance < bestDistance;
            if (!isBetter && distance == bestDistance && multiplier < bestMultiplier)
                isBetter = true;
            if (!isBetter)
                return;

            CopyValues(candidateFatigueValues, bestFatigueValues);
            hasBest = true;
            bestDistance = distance;
            bestMultiplier = multiplier;
            achievedSeconds = seconds;
            durationOverflow = overflow;
        }

        private static void ApplyScale(
            IReadOnlyList<BigInteger> originalFatigueValues,
            decimal multiplier,
            BigInteger[] destination)
        {
            GetDecimalFraction(multiplier, out BigInteger numerator, out BigInteger denominator);
            for (int i = 0; i < originalFatigueValues.Count; i++)
            {
                BigInteger scaled = originalFatigueValues[i] * numerator;
                BigInteger rounded = BigInteger.DivRem(scaled, denominator, out BigInteger remainder);
                if (!remainder.IsZero && remainder * 2 >= denominator)
                    rounded += BigInteger.One;
                if (rounded < BigInteger.One)
                    rounded = BigInteger.One;
                destination[i] = rounded;
            }
        }

        private static void GetDecimalFraction(
            decimal value,
            out BigInteger numerator,
            out BigInteger denominator)
        {
            int[] bits = decimal.GetBits(value);
            int scale = (bits[3] >> 16) & 0x7F;
            bool negative = (bits[3] & int.MinValue) != 0;

            numerator = (uint)bits[0];
            numerator += (BigInteger)(uint)bits[1] << 32;
            numerator += (BigInteger)(uint)bits[2] << 64;
            if (negative)
                numerator = -numerator;

            denominator = scale == 0 ? BigInteger.One : BigInteger.Pow(10, scale);
        }

        private static void CopyValues(BigInteger[] source, BigInteger[] destination)
        {
            for (int i = 0; i < source.Length; i++)
                destination[i] = source[i];
        }
    }
}
