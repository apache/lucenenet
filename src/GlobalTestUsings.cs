// alias Assert to Lucene.Net.TestFramework.Assert by default
global using Assert = Lucene.Net.TestFramework.Assert;

// add NUnit 3 compat shim with NUnit prefix
global using NUnitAssert = NUnit.Framework.Legacy.ClassicAssert;

// add remaining NUnit 3 compat classes that don't have TestFramework equivalents
global using CollectionAssert = NUnit.Framework.Legacy.CollectionAssert;
