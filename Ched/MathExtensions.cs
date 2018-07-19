using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched
{
    public static class MathExtensions
    {
        public static IEnumerable<int> Factorize(this int n)
        {
            int m = n;
            for (int i = 2; i <= m; i++)
            {
                if (n == 1) break;
                if (n % i != 0) continue;
                while (n % i == 0)
                {
                    n /= i;
                    yield return i;
                }
            }
        }

        public static int Product(this IEnumerable<int> seq)
        {
            return seq.Aggregate(1, (p, q) => p * q);
        }

        public static IEnumerable<T> ExceptAll<T>(this IEnumerable<T> first, IEnumerable<T> second)
        {
            var list = second.ToList();
            return first.Where(p => !list.Remove(p));
        }
    }
}
