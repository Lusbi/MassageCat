using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace JSL.Memory.Extensions
{
    // System.MemoryExtensions.Split is not available for .NET Standard 2.1
    public static class MemoryExtensions
    {
        /// <summary>
        /// Parses the source <see cref="ReadOnlySpan{Char}"/> for the specified <paramref name="separator"/>, populating the <paramref name="destination"/> span
        /// with <see cref="Range"/> instances representing the regions between the separators.
        /// </summary>
        /// <param name="source">The source span to parse.</param>
        /// <param name="destination">The destination span into which the resulting ranges are written.</param>
        /// <param name="separator">A character that delimits the regions in this instance.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim whitespace and include empty ranges.</param>
        /// <returns>The number of ranges written into <paramref name="destination"/>.</returns>
        /// <remarks>
        /// <para>
        /// Delimiter characters are not included in the elements of the returned array.
        /// </para>
        /// <para>
        /// If the <paramref name="destination"/> span is empty, or if the <paramref name="options"/> specifies <see cref="StringSplitOptions.RemoveEmptyEntries"/> and <paramref name="source"/> is empty,
        /// or if <paramref name="options"/> specifies both <see cref="StringSplitOptions.RemoveEmptyEntries"/> and <see cref="StringSplitOptions.TrimEntries"/> and the <paramref name="source"/> is
        /// entirely whitespace, no ranges are written to the destination.
        /// </para>
        /// <para>
        /// If the span does not contain <paramref name="separator"/>, or if <paramref name="destination"/>'s length is 1, a single range will be output containing the entire <paramref name="source"/>,
        /// subject to the processing implied by <paramref name="options"/>.
        /// </para>
        /// <para>
        /// If there are more regions in <paramref name="source"/> than will fit in <paramref name="destination"/>, the first <paramref name="destination"/> length minus 1 ranges are
        /// stored in <paramref name="destination"/>, and a range for the remainder of <paramref name="source"/> is stored in <paramref name="destination"/>.
        /// </para>
        /// </remarks>
        public static int Split(this ReadOnlySpan<char> source, Span<Range> destination, char separator, StringSplitOptions options = StringSplitOptions.None)
        {
            CheckStringSplitOptions(options);
            // ReadOnlySpan(in T reference) constructor is not available
            return InternalSplit(source, destination, MemoryMarshal.CreateReadOnlySpan(ref separator, 1), options);
        }

        /// <summary>
        /// Parses the source <see cref="ReadOnlySpan{Char}"/> for the specified <paramref name="separator"/>, populating the <paramref name="destination"/> span
        /// with <see cref="Range"/> instances representing the regions between the separators.
        /// </summary>
        /// <param name="source">The source span to parse.</param>
        /// <param name="destination">The destination span into which the resulting ranges are written.</param>
        /// <param name="separator">A character that delimits the regions in this instance.</param>
        /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim whitespace and include empty ranges.</param>
        /// <returns>The number of ranges written into <paramref name="destination"/>.</returns>
        /// <remarks>
        /// <para>
        /// Delimiter characters are not included in the elements of the returned array.
        /// </para>
        /// <para>
        /// If the <paramref name="destination"/> span is empty, or if the <paramref name="options"/> specifies <see cref="StringSplitOptions.RemoveEmptyEntries"/> and <paramref name="source"/> is empty,
        /// or if <paramref name="options"/> specifies both <see cref="StringSplitOptions.RemoveEmptyEntries"/> and <see cref="StringSplitOptions.TrimEntries"/> and the <paramref name="source"/> is
        /// entirely whitespace, no ranges are written to the destination.
        /// </para>
        /// <para>
        /// If the span does not contain <paramref name="separator"/>, or if <paramref name="destination"/>'s length is 1, a single range will be output containing the entire <paramref name="source"/>,
        /// subject to the processing implied by <paramref name="options"/>.
        /// </para>
        /// <para>
        /// If there are more regions in <paramref name="source"/> than will fit in <paramref name="destination"/>, the first <paramref name="destination"/> length minus 1 ranges are
        /// stored in <paramref name="destination"/>, and a range for the remainder of <paramref name="source"/> is stored in <paramref name="destination"/>.
        /// </para>
        /// </remarks>
        public static int Split(this ReadOnlySpan<char> source, Span<Range> destination, ReadOnlySpan<char> separator, StringSplitOptions options = StringSplitOptions.None)
        {
            CheckStringSplitOptions(options);

            if (separator.IsEmpty)
            {
                if (!destination.IsEmpty)
                {
                    int end = source.Length;
                    if (end > 0 || (options & StringSplitOptions.RemoveEmptyEntries) == 0)
                    {
                        destination[0] = ..end;
                        return 1;
                    }
                }

                return 0;
            }

            return InternalSplit(source, destination, separator, options);
        }

        static void CheckStringSplitOptions(StringSplitOptions options)
        {
            if ((options & ~StringSplitOptions.RemoveEmptyEntries) != 0)
                throw new ArgumentException($"Invalid StringSplitOptions: {options}");
        }

        static int InternalSplit(ReadOnlySpan<char> source, Span<Range> destination, ReadOnlySpan<char> separator, StringSplitOptions options = StringSplitOptions.None)
        {
            if (destination.IsEmpty)
                return 0;

            bool keepEmptyEntries = (options & StringSplitOptions.RemoveEmptyEntries) == 0;

            if (source.IsEmpty)
            {
                if (keepEmptyEntries)
                {
                    destination[0] = default;
                    return 1;
                }
                return 0;
            }

            int start = 0, end;

            if (destination.Length == 1)
            {
                end = source.Length;
                if (start != end || keepEmptyEntries)
                {
                    destination[0] = start..end;
                    return 1;
                }

                return 0;
            }

            int index;
            int rangeCount = 0;
            Span<Range> destMinusOne = destination[..^1];
            while (rangeCount < destMinusOne.Length && start < source.Length && (index = source[start..].IndexOf(separator)) != -1)
            {
                end = start + index;
                Debug.Assert(start <= end);
                if (start != end || keepEmptyEntries)
                {
                    destMinusOne[rangeCount] = start..end;
                    rangeCount++;
                }

                start = end + 1;
            }

            if (rangeCount < destination.Length)
            {
                end = source.Length;
                if (start != end || keepEmptyEntries)
                {
                    destination[rangeCount] = start..end;
                    rangeCount++;
                }
            }

            return rangeCount;
        }
    }
}