using FakePoc;
using FastExpressionKit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using static System.Console;

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
        public DateTime date { get; set; }
        public DateTime? mynullable { get; set; }

    }

    class Program
    {
        static void RepeatBench(string description, int n, Action action)
        {
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < n; i++)
            {
                action();
            }
            WriteLine("{0}: {1}", description, (sw.ElapsedMilliseconds * 1000) / (float) n);

        }
        static void Benchmark()
        {
            var c1 = new C() { a = 666, b = 12, date = DateTime.Now, mynullable = DateTime.Now };
            var d1 = new D() { a = 666, b = 8, c = 9, date = DateTime.Now, mynullable = DateTime.Now };

            var fields = new[] { "a", "b" };


            WriteLine("Stats (microseconds) per iteration");

            RepeatBench("new Differ()", 1000, () =>
            {
                var dd = new Differ<C, D>(fields);
            });

            var d = new Differ<C, D>(fields);

            RepeatBench("Compare small objects", 1000000, () =>
            {
                var res = d.Compare(c1, d1);
            });
            RepeatBench("new FieldExtract()", 1000, () =>
            {
                var dd = new FieldExtract<C, int>(fields);
            });
            var extractor = new FieldExtract<C, int>(fields);

            RepeatBench("Extract integers", 1000000, () =>
            {
                var ints = extractor.Extract(c1);
            });

            // big data
            var big1 = new BigDto();
            var big2 = new BigDto();

            var bigprops = ReflectionHelper.CollectProps<BigDto>();
            var bigpropnames = bigprops.SelectMany(p => p.Item2).ToArray();

            RepeatBench("new Differ() for big class", 100, () =>
            {
                var dd = new Differ<BigDto, BigDto>(bigpropnames);

            });

            var bigd = new Differ<BigDto, BigDto>(bigpropnames);
            RepeatBench("Compare large objects", 1000, () =>
            {
                bigd.Compare(big1, big2);
            });

            var types = ReflectionHelper.CollectProps<BigDto>();
            var e4 = ReflectionHelper.GetExtractorFor<BigDto, int>(types);
            var e5 = ReflectionHelper.GetExtractorFor<BigDto, string>(types);
            var e6 = ReflectionHelper.GetExtractorFor<BigDto, decimal>(types);

           
            RepeatBench("Extract fields from large object", 10000, () =>
            {
                var r1 = e4.Extract(big1);
                var r2 = e5.Extract(big1);
                var r3 = e6.Extract(big1);
            });


            RepeatBench("Extract fields from large object, convert to dict, naive", 10000, () =>
            {
                var pd = e4.ResultsAsDict(e4.Extract(big1).Select(i => i.ToString()).ToList())
                       .Union(e5.ResultsAsDict(e5.Extract(big1).Select(e => e.ToString()).ToList()))
                       .Union(e6.ResultsAsDict(e6.Extract(big1).Select(e => e.ToString()).ToList()));
            });

            var boxedExtract = new FieldExtract<BigDto, object>(bigpropnames);

            RepeatBench("Extract fields, boxed", 100000, () =>
            {
                var r1 = boxedExtract.Extract(big1);
            });

            RepeatBench("Extract fields from large dto. string -> object dict", 10000, () =>
            {
                var r1 = boxedExtract.Extract(big1);
                var r2 = boxedExtract.ResultsAsZip(r1);
            });


            var propertyInfos = typeof(BigDto).GetProperties();
            RepeatBench("Extract fields with reflection", 10000, () =>
            {
                foreach (var p in propertyInfos)
                {
                    var val = p.GetValue(big1);
                }
            });

            RepeatBench("Extract fields with reflection, convert to string dict", 10000, () =>
            {
                var resdict = new Dictionary<string, string>();
                foreach (var p in propertyInfos)
                {
                    var val = p.GetValue(big1);
                    var s = val.ToString();
                    resdict[p.Name] = s;
                }
            });


            ReadLine();
        }
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

                var pd = e4.ResultsAsDict(e4.Extract(c).Select(i => i.ToString()).ToList())
                    .Union(e5.ResultsAsDict(e5.Extract(c).Select(e => e.ToString()).ToList()))
                    .Union(e6.ResultsAsDict(e6.Extract(c).Select(e => e.ToString()).ToList()));
            };

            tryit(c1);
            tryit(c2);


            Benchmark();
        }

    }
}
