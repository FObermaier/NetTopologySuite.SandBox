using System;
using System.Collections.Generic;

namespace NetTopologySuite.Index.Bushes
{
    public partial class Rbush<T>
    {
        /// <summary>
        /// A tiny and fast selection algorithm in C#.
        /// </summary>
        /// <remarks>
        /// This is a direct port of Vladimir Agafonkin's quickselect package
        /// </remarks>
        /// <typeparam name="TItem"></typeparam>
        private class Quick<TItem>
        {
            public static void Select(Span<TItem> arr, int k, int left = 0, int? right = null,
                IComparer<TItem> comparer = null)
            {
                SelectStep(arr, k, left, right ?? arr.Length - 1, comparer ?? Comparer<TItem>.Default);
            }

            private static void SelectStep(Span<TItem> arr, int k, int left, int right, IComparer<TItem> comparer)
            {
                while (right > left)
                {
                    if (right - left > 600)
                    {
                        int n = right - left + 1;
                        int m = k - left + 1;
                        double z = Math.Log(n);
                        double s = 0.5 * Math.Exp(2 * z / 3);
                        double sd = 0.5 * Math.Sqrt(z * s * (n - s) / n) * (m - n / 2 < 0 ? -1 : 1);
                        int newLeft = Math.Max(left, (int) Math.Floor(k - m * s / n + sd));
                        int newRight = Math.Min(right, (int) Math.Floor(k + (n - m) * s / n + sd));
                        SelectStep(arr, k, newLeft, newRight, comparer);
                    }

                    var t = arr[k];
                    int i = left;
                    int j = right;

                    Swap(arr, left, k);
                    if (comparer.Compare(arr[right], t) > 0) Swap(arr, left, right);

                    while (i < j)
                    {
                        Swap(arr, i, j);
                        i++;
                        j--;
                        while (comparer.Compare(arr[i], t) < 0) i++;
                        while (comparer.Compare(arr[j], t) > 0) j--;
                    }

                    if (comparer.Compare(arr[left], t) == 0) Swap(arr, left, j);
                    else
                    {
                        j++;
                        Swap(arr, j, right);
                    }

                    if (j <= k) left = j + 1;
                    if (k <= j) right = j - 1;
                }
            }

            private static void Swap(Span<TItem> arr, int i, int j)
            {
                var tmp = arr[i];
                arr[i] = arr[j];
                arr[j] = tmp;
            }
        }
    }
}
