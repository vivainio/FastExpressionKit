using FastExpressionKit;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace ConsoleApplication2
{
    public class C
    {
        public int a { get; set; }
        public int b { get; set; }
        public DateTime date { get; set; }
        public DateTime? mynullable { get; set; }
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
            var c1 = new C() { a = 666, b = 12, date = DateTime.Now, mynullable = DateTime.Now };
            var c2 = new C() { a = 100, b = 12, mynullable = null };
            var d1 = new D() { a = 666, b = 12, c = 123 };
            var d2 = new D() { a = 100, b = 12 , c = 223 };
            var fields = new[] { "a", "b" };

            var differ = new Differ<C, C>(new[] { "a", "b" });
            var res = differ.Compare(c1, c2);
            // [ false, true]

            // compare different types!
            var differ2 = new Differ<C, D>(new[] { "a", "b" });
            res = differ2.Compare(c1, d1);

            var extractor = new FieldExtract<C, int>(fields);
            var results = extractor.Extract(c1);
            //var dresults = extractor.ExtractAsDict(c1);
            try
            {
                var fails = new FieldExtract<C, string>(fields);
            } catch (InvalidOperationException e) {};

            var ee = new FieldExtract<C, DateTime?>(new[] { "mynullable" });
            var r = ee.Extract(c1);

            //var e2 = new FieldExtract<C, object>(fields);
            //e2.Extract(c1);

            var e3 = new FieldExtract<C, DateTime>(new[] { "date" });
            var r3 = e3.Extract(c1);

            // automatically create extractors per each property type
            var types = ReflectionHelper.CollectProps<C>();
            var e4 = ReflectionHelper.GetExtractorFor<C, int>(types);
            var e5 = ReflectionHelper.GetExtractorFor<C, DateTime>(types);
            var e6 = ReflectionHelper.GetExtractorFor<C, DateTime?>(types);

            Action<C> tryit = c =>
            {
                // smash exctractor results together with conversions to get string

                var pd = e4.ResultsAsDict(e4.Extract(c).Select(i => i.ToString()))
                    .Union(e5.ResultsAsDict(e5.Extract(c).Select(e => e.ToString())))
                    .Union(e6.ResultsAsDict(e6.Extract(c).Select(e => e.ToString())));
            };

            tryit(c1);
            tryit(c2);

        }

    }
}
