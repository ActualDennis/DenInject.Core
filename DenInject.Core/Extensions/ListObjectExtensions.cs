using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DenInject.Core.Extensions {
    public static class ListObjectExtensions {
        public static object ListObjToEnumerableType(this List<object> items, Type type, bool performConversion = false)
        {
            var enumerableType = typeof(System.Linq.Enumerable);
            var castMethod = enumerableType.GetMethod(nameof(System.Linq.Enumerable.Cast)).MakeGenericMethod(type);
            var toListMethod = enumerableType.GetMethod(nameof(System.Linq.Enumerable.ToList)).MakeGenericMethod(type);

            IEnumerable<object> itemsToCast;

            if (performConversion)
            {
                itemsToCast = items.Select(item => Convert.ChangeType(item, type));
            }
            else
            {
                itemsToCast = items;
            }

            var castedItems = castMethod.Invoke(null, new[] { itemsToCast });

            return castedItems;
        }
    }
}
