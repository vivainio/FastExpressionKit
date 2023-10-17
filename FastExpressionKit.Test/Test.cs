using FakePoc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using static System.Console;
using TrivialTestRunner;
using FastExpressionKit;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using AutoFixture;
using FastExpressionKit.BulkInsert;
using FastExpressionKit.Integration.Tests;
using FastExpressionKit.Test;
using NFluent;

namespace FastExpressionKitTests
{
    public class C
    {
        public int a { get; set; }
        public int b { get; set; }
        public string s { get; set; }
        public DateTime date { get; set; }
        public DateTime? mynullable { get; set; }
        
        public int sometimesnullable { get; set; }
    }
    public class D
    {
        [Column("A_COL")]
        public int a { get; set; }
        [Column("B_COL")]

        public int b { get; set; }

        [Column("C_COL")]
        public int c { get; set; }

        [Column("DATE_COL")]
        public DateTime date { get; set; }

        public string NoTable { get; set; }
        [Column("DATE_NULLABLE_COL")]

        public DateTime? mynullable { get; set; }
        
        public int? sometimesnullable { get; set; }
        
    }

    public class SomeReadOnly
    {
        public int a { get; set; }
        public int b { get; set; }
        public int readOnly { get; }

    }

    public class SomeStrings
    {
        public string a { get; set; }
        public string b { get; set; }
        public string c
        {
            get;
            set;
        } 
        public int surpriseinteger { get; set; }
    }

    public class SomeDecimals
    {
        public decimal a { get; set; }
        public decimal b { get; set; }
        public decimal? cnullable { get; set; }
    }

    class FastExprKitTest
    {
        static void RepeatBench(string description, int n, Action action)
        {
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < n; i++)
            {
                action();
            }

            WriteLine("{0}: {1}", description, (sw.ElapsedMilliseconds * 1000) / (float)n);

        }

        public static void ValidateString(Type t, string key, string value, char[] badChars)
        {
            var loc = value.IndexOfAny(badChars);
            if (loc != -1)
                throw new ValidationError($"Type {t} Prop {key}, String contained illegal character '{value[loc]}' at position {loc}");
        }

        public static void ValidateDecimal(Type t, string key, decimal value, bool extra)
        {
            
            if (value < 0)
                throw new ValidationError($"Type {t} Property {key} was negative: {value}");
        }

        [Case]
        public static void TestTrivialValidator()
        {
            TrivialValidator.SetValidator<SomeStrings>(prop => 
                prop.PropertyType == typeof(string)
                && prop.Name != "c", TrivialValidator.BadCharacters);
            var sut = new SomeStrings()
            {
                a = "my a",
                b = "O'Reilly", // this will raise error
                c = "the ceeeee"
            };

            Check.ThatCode(() =>
            {
                TrivialValidator.Validate(sut);
            }).Throws<ValidationError>();
            // 2 times should work as well
            Check.ThatCode(() =>
            {
                TrivialValidator.Validate(sut);
            }).Throws<ValidationError>();

            Check.ThatCode(() =>
            {
                TrivialValidator.ValidateMany(new[] { sut, sut });
            }).Throws<ValidationError>();

            
            TrivialValidator.SetValidator<SomeStrings>( prop => prop.Name != "b",
                TrivialValidator.BadCharacters,
                overwrite: true);
            TrivialValidator.Validate(sut); // will not throw
        }
        [Case]
        public static void TestForEach()
        {
            var mi = typeof(FastExprKitTest).GetMethod("ValidateString");
            var fe = new RunMethodForEachProperty<SomeStrings, char[]>(new[] { "a", "b", "c" }, mi, TrivialValidator.BadCharacters);
            var sut = new SomeStrings()
            {
                a = "my a",
                b = "O'Reilly", // this will raise error
                c = "the ceeeee"
            };
            Check.ThatCode(() =>
            {
                fe.Run(sut);
            }).Throws<ValidationError>();
            sut.b = "Nondangerous string";
            fe.Run(sut); // does not throw
            
            var decimalChecker = new RunMethodForEachProperty<SomeDecimals, bool>(new[] { "a", "b" }, 
                typeof(FastExprKitTest).GetMethod(nameof(ValidateDecimal)), true);

            var dd = new SomeDecimals
            {
                a = 12,
                b = -2,
                cnullable = 3
            };
            Check.ThatCode(() =>
            {
                decimalChecker.Run(dd);
            }).Throws<ValidationError>();

        }
        [Case]
        public static void Benchmark()
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
            RepeatBench("Compare large objects", 10000, () => { bigd.Compare(big1, big2); });

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


            var copier = new FieldCopier<BigDto, BigDto>(bigpropnames);
            RepeatBench("Copy big object", 100000, () => { copier.Copy(big1, big2); });

        }

        static DateTime SomeDate = new DateTime(2020, 2, 24);

        // test data for small objects
        static C c1 = new C() { a = 666, b = 12, date = DateTime.Now, mynullable = DateTime.Now, s = "one" };
        static C c2 = new C() { a = 100, b = 12, mynullable = null, s = "two" };
        static D d1 = new D() { a = 666, b = 12, c = 123, date = SomeDate, NoTable = "not mapped", mynullable = null };
        static D d2 = new D() { a = 100, b = 12, c = 223, date = SomeDate, mynullable = SomeDate };
        static string[] fields = new[] { "a", "b" };

        [Case]
        public static void TestCopier()
        {
            c1.a = 111;
            c1.b = 999;
            var copier = new FieldCopier<C, C>(fields);
            copier.Copy(c1, c2);
        }

        public static void ManyExtractors()
        {
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
        }

        [Case]
        public static void TestFieldExtract()
        {
            var extractor = new FieldExtract<C, int>(fields);
            var results = extractor.Extract(c1);
            //var dresults = extractor.ExtractAsDict(c1);
            try
            {
                var fails = new FieldExtract<C, string>(fields);
            }
            catch (InvalidOperationException)
            {
            }

            var ee = new FieldExtract<C, DateTime?>(new[] { "mynullable" });
            var r = ee.Extract(c1);

            //var e2 = new FieldExtract<C, object>(fields);
            //e2.Extract(c1);

            var e3 = new FieldExtract<C, DateTime>(new[] { "date" });
            var r3 = e3.Extract(c1);
        }

        [Case]
        public static void TestExtractToArrays()
        {
            var extractor = new FieldExtract<C, object>(new[] { "a", "b", "s" });
            var results = extractor.Extract(c1);
            var extracted = FieldExtractUtil.ExtractToObjectArrays(extractor, new[] { c1, c2, c1, c2 });
            var serialized = JsonConvert.SerializeObject(extracted);
            Assert.AreEqual(serialized, @"[[100,100,100,100],[12,12,12,12],[""one"",""two"",""one"",""two""]]");
        }

        [Case]
        public static void DifferSmall()
        {
            var differ = new Differ<C, C>(new[] { "a", "b" });
            var res = differ.Compare(c1, c2);

            // compare different types!
            var differ2 = new Differ<C, D>(new[] { "a", "b" });
            res = differ2.Compare(c1, d1);
        }

        [Case]
        public static void TestReflectionHelper()
        {
            var writeable = ReflectionHelper.WriteablePropNames<SomeReadOnly>();
            //Assert.Contains("a", writeable);
            Assert.IsTrue(!writeable.Contains("readOnly"));
        }

        [Case]
        public static void DbBulkInsert()
        {
            var inserter = FastBulkInsertUtil.CreateBulkInserter<D>();
            var dtos = new[] { d1, d2 };
            var instructions = inserter.BuildInstructionsForRows(dtos);
            Check.That(instructions).CountIs(5);
        }

        [Case]
        public static void TestCachedInserters()
        {
            var i1 = FastBulkInsertUtil.CreateBulkInserter<D>();
            FastBulkInsertCache.Add(i1);
            Check.ThatCode(() => { FastBulkInsertCache.Add(i1); }).Throws<ArgumentException>();
            var got = FastBulkInsertCache.Get<D>();
            Check.That(got).Equals(i1);
        }

        [Case]
        public static void TestPropertyNameGeneration()
        {
            var ins = FastBulkInsertUtil.CreateBulkInserter<TestDbEntityWithoutAnnotations>(
                TableMappingRules.UpperSnake("MY_GEN_NAME"));
            Check.That(ins.TableName).Equals("MY_GEN_NAME");
            var f = new Fixture();
            var ds = f.CreateMany<TestDbEntityWithoutAnnotations>().ToArray();
            var instr = ins.BuildInstructionsForRows(ds);
            Check.That(instr).HasSize(4);
            Check.That(instr[0].DbParamType).Equals(DbParameterTypeNumbers.Raw);
            Check.That(instr[1].DbParamType).Equals(DbParameterTypeNumbers.NVarChar);
            Check.That(instr[2].DbParamType).Equals(DbParameterTypeNumbers.Date);
            // enum is number
            Check.That(instr[3].DbParamType).Equals(DbParameterTypeNumbers.Number);

        }

        [Case]
        public static void TestHasher()
        {

            var opts = new FieldHasherOptions
            {
                ZeroIfNulls = true,
                StringNormalizer = (e) =>
                {
                    var trim = typeof(string).GetMethod("Trim", new Type[] { });
                    var call = Expression.Call(e, trim);
                    return call;
                }
            };
            var h = new FieldHasher<C>(ReflectionHelper.GetProps<C>(), opts);
            var o = new C();
            var l = new List<int>();

            int Compute()
            {
                var val = h.ComputeHash(o);
                l.Add(val);
                return val;
            }

            o.a = 1;
            Compute();
            o.mynullable = SomeDate;
            Compute();

            Check.That(l).ContainsExactly(0, 0);
            l.Clear();

            o.s = "notnull";
            Compute();


            // now it starts giving nonzero
            Check.That(l[0]).IsNotZero();
            o.s = "t";
            Compute();
            o.b = 1;
            Compute();
            o.date = SomeDate;
            Compute();
            Check.That(l.Distinct()).HasSize(l.Count);

            o.s = "   hello ";
            var hashWithSpaces = Compute();
            o.s = "hello";
            var hashWithoutSpaces = Compute();
            Check.That(hashWithSpaces).Equals(hashWithoutSpaces);
        }

        [Case]
        public static void TestOwnPropertyRules()
        {
            var rules = new TableMappingRules
            {
                TableName = "MY_TAB",
                ColumnNameGenerator = pi =>
                {
                    if (pi.Name == "MyString")
                    {
                        // strip this out
                        return null;
                    }
                    var guessed = TableMappingRules.GuessColumnNameBasedOnPropertyInfo(pi);
                    return guessed == "MY_DATE" ? "MY_CHANGED_DATE" : guessed;
                }
            };
            var ins =
                FastBulkInsertUtil.CreateBulkInserter<TestDbEntityWithoutAnnotations>(
                    rules);

            Check.That(ins.TableName).Equals("MY_TAB");
            Check.That(ins.Properties).HasSize(3);
            Check.That(ins.Properties[1].DbColumnName).Equals("MY_CHANGED_DATE");
        }

        [Case]
        public static void TestCopyNullableOverNonnullable()
        {
            var src = new D();
            var dest = new C();
            var copier = new FieldCopier<C, D>(new[] {"sometimesnullable"});
            src.sometimesnullable = null;
            copier.Copy(dest, src);
            Check.That(dest.sometimesnullable).Equals(0);
            src.sometimesnullable = 12;
            copier.Copy(dest, src);
            Check.That(dest.sometimesnullable).Equals(12);
            src.sometimesnullable = null;
            copier.Copy(dest, src);
            Check.That(dest.sometimesnullable).Equals(0);
        }
    }
}
