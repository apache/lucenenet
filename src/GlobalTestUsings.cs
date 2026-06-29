// add NUnit 3 compat shim with NUnit prefix
global using NUnitAssert = NUnit.Framework.Legacy.ClassicAssert;

// add remaining NUnit 3 compat classes that don't have TestFramework equivalents
global using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;
