using System.Collections.Generic;

namespace MCEPatcher.Core;

public static class CollectionExtensions
{
    public static void RemoveAt<T>(this IList<T> list, int index, int count)
    {
        for (int i = 0; i < count; i++)
        {
            list.RemoveAt(index);
        }
    }

    public static void AddToEnd<T>(this IList<T> list, T value, int count)
    {
        for (int i = 0; i < count; i++)
        {
            list.Add(value);
        }
    }

    public static void Insert<T>(this IList<T> list, int index, IEnumerable<T> other)
    {
        foreach (T item in other)
        {
            list.Insert(index++, item);
        }
    }
}
