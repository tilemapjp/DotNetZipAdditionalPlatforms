DotNetZipAdditionalPlatforms
============================

Provides several ports of the superb DotNetZip library (http://dotnetzip.codeplex.com/) to additional .NET platforms/runtimes.  This project also provides a much cleaner, compliant, and easier to embed source code base compared to the current DotNetZip library.

Current project status as of 9/15/2012: 
========================================

- In the middle of making the initial code base 100% static code analysis and FxCop compliant.

Roadmap:
========

- get "clean source code" .NET 2.0 version finished
- create .NET 2.0 unit tests working with the "clean source code" .NET 2.0 version
- create Mono for Android (http://xamarin.com/monoforandroid) dll with unit tests
- create Windows Runtime Component (http://msdn.microsoft.com/en-US/library/windows/apps/xaml/hh441572) with unit tests
- create Windows Phone dll with unit tests
- create MonoTouch for IOS (http://xamarin.com/monotouch) dll with unit tests

Approach:
=========================

The approach that is being used to create this codebase is:
- I downloaded the current Ionic.Zip.Reduced.dll (1.9.1.8) assembly from http://dotnetzip.codeplex.com and decompiled the source code using .NET Reflector (so the source code comes out clean)
- I then took the decompiled source code, put it in the Visual Studio 2012 project and am currently manually fixing all the compiler, static code analysis, and StyleCop errors/warnings/violations/issues
- After I get all issues resolved, I then will create a set of unit tests that are multi-platform friendly.
- After the unit tests pass, I then will start porting the source code and unit tests to other popular .NET platforms listed in the roadmap above.

Special thanks:
===============
- To Cheeso, the creator of the DotNetZip library (http://dotnetzip.codeplex.com).  Without his monumental efforts, none of this would be possible!  It truly is an awesome library with so many features!