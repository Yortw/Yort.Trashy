using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if !NETSTANDARD1_0
[assembly: AssemblyProduct("Yort.Trashy")]
[assembly: AssemblyDescription("Types, interfaces & extension methods for creating or dealing with disposable resources in .Net.")]

[assembly: ComVisible(false)]

//Do not forget to update version # in netstandard project properties too
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
#endif

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if DEBUG
[assembly: AssemblyConfiguration("DEBUG")]
#else
[assembly: AssemblyConfiguration("RELEASE")]
#endif
[assembly: AssemblyCopyright("Copyright © 2017")]
[assembly: System.CLSCompliant(false)]
