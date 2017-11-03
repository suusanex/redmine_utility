using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedmineUtility
{
    //Webのサンプルをパクリ
    //https://webbibouroku.com/Blog/Article/chunk-linq
    public static class IEnumerableExtentions
    {
        // 指定サイズのチャンクに分割する拡張メソッド
        public static IEnumerable<IEnumerable<T>> Chunks<T>
        (this IEnumerable<T> list, int size)
        {
            while (list.Any())
            {
                yield return list.Take(size);
                list = list.Skip(size);
            }
        }
    }
}
