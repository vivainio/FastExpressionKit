# FastExpressionKit

## A small library to make reflection-y things faster

Reflection in C# can be slow. There are operations that iterate over certain properties of objects that typically use reflection, with suboptimal performance.

This mini-library (just one .cs file to copy to your project, less than 200 lines!) provides minimal building blocks that generate code (Linq Expressions) and compile it to fast machine code. The recommended pattern is to instantiate the class where you configure your mappings (takes few milliseconds), and then reuse the instance for hundreds of operations with good performance (few microseconds per round).

Classes provided are:

### FieldExtract

Extract fields a and b to array of integers:

 ```csharp
var extractor = new FieldExtract<C, int>(new[] { "a", "b" };);
var results = extractor.Extract(c1);
```

If you want fields boxed to object (i.e. don't want to specify the target type), there is a special case for object:

```csharp
var boxedExtract = new FieldExtract<BigDto, object>(bigpropnames);
```

Clearly, dealing with the end result will be slower as you got objects now.

### Differ

Compare two objects field-by-field, yielding a list of differences. Works across different classes.

```csharp
var differ = new Differ<C, C>(new[] { "a", "b" });
var res = differ.Compare(c1, c2);

// compare different types!
var differ2 = new Differ<C, D>(new[] { "a", "b" });
res = differ2.Compare(c1, d1);
```

### Copier

Copy a set of fields from one object to another.


```csharp
var copier = new FieldCopier<C, C>(fields);
copier.Copy(c1, c2);
```

Yes, this is essentially a trivial version of AutoMapper (when you specify the list of fields in the target type).
The property types must be of the same type, as copying is just doing src.foo = target.foo for each property.

### Utility classes

The library also contains some helpers for doing reflection (ReflectionHelper) and static helpers for creating
Expressions (EE), but those are not part of the documented API.

### License

License: MIT
Copyright 2017 Ville M. Vainio
