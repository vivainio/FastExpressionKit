using FastExpressionKit;
using System.Linq.Expressions;

namespace ConsoleApplication2
{
    public class C
    {
        public int a { get; set; }
        public int b { get; set; }
    }
    public class D
    {
        public int a { get; set; }
        public int b { get; set; }
        public int c { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // demo data
            var c1 = new C() { a = 666, b = 12 };
            var c2 = new C() { a = 100, b = 12 };
            var d1 = new D() { a = 666, b = 12, c = 123 };
            var d2 = new D() { a = 100, b = 12 , c = 223 };

            var differ = new FastObjectDiffer<C, C>(new[] { "a", "b" });
            var res = differ.Compare(c1, c2);
            // [ false, true]

            // compare different types!
            var differ2 = new FastObjectDiffer<C, D>(new[] { "a", "b" });
            res = differ2.Compare(c1, d1);
        }
            
    }
}
