using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Lucene.Net.TestFramework")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Lucene.Net.TestFramework")]
[assembly: AssemblyCopyright("Copyright ©  2013")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("5f36e3cf-82ac-4d97-af1a-6dabe60e9180")]

// for testing
//[assembly: InternalsVisibleTo("Lucene.Net.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010075a07ce602f88e" +
//                                                         "f263c7db8cb342c58ebd49ecdcc210fac874260b0213fb929ac3dcaf4f5b39744b800f99073eca" +
//                                                         "72aebfac5f7284e1d5f2c82012a804a140f06d7d043d83e830cdb606a04da2ad5374cc92c0a495" +
//                                                         "08437802fb4f8fb80a05e59f80afb99f4ccd0dfe44065743543c4b053b669509d29d332cd32a0c" +
//                                                         "b1e97e84")]

// LUCENENET NOTE: For now it is not possible to use a SNK because we have unmanaged references in Analysis.Common.
// However, we still need InternalsVisibleTo in order to prevent making everything public just for the sake of testing.
// This has broad implications, though because many methods are marked "protected internal", which means other assemblies
// must update overridden methods to match.
[assembly: InternalsVisibleTo("Lucene.Net.Tests")]
[assembly: InternalsVisibleTo("Lucene.Net.Tests.Misc")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
