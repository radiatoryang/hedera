# simplify-csharp #

Polyline simplifier

  * Uses Radial-Distance algorithm (fast) or Douglas-Peucker (high quality) algorithm
  * Port of [hgoebl/simplify-java](https://github.com/hgoebl/simplify-java) which in turn is a port of simplify-js, a High-performance JavaScript 2D/3D polyline simplification library by Vladimir Agafonkin
  * Highly optimized for C#
  * Avoids unncessary memory allocations
  * Completely non-intrusive and can be used with any class or struct. Avoids any interfaces to prevent issues with boxing/unboxing when working with structs. 
  * Requires .NET 3.5 and above
  * Comes with a demo

# Example #
Use the SimplificationHelpers class to quickly simplify a list of System.Windows.Point objects.

The library is non-intrusive and only requires methods for extracting x,y and z values as doubles and for comparing two objects of the given type.

```csharp
    double tolerance = 0.5;
    bool highQualityEnabled = false;
    var simplifiedPoints = SimplificationHelpers.Simplify<Point>(
                    Points,
                    (p1, p2) => p1 == p2,
                    (p) => p.X, 
                    (p) => p.Y,
                    tolerence,
                    highQualityEnabled
                    );
```

**Please note**
The algorithm squares differences of x, y and z coordinates. If this difference is less than 1,
the square of it will get even less. In such cases, the tolerance has negative effect.

Solution: multiply your coordinates by a factor so the values are shifted in a way so that taking
the square of the differences creates greater values.

# Demo #

To demo the algorithm, compile and run the SimplifyCSharpDemo project and scribble some lines in the user input canvas. Click on Simplify to run the algorithm and draw the result in the output canvas.

![Demo Screenshot](https://i.imgur.com/outNL5F.png)

# Licence #

  * [The MIT License](http://opensource.org/licenses/MIT)

# TODO #

  * Add Visvalingam-Whyatt algorithm

# Alternatives / Infos #

  * <http://www.codeproject.com/Articles/114797/Polyline-Simplification>
