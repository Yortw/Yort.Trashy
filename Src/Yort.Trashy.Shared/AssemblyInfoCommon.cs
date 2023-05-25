using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Resources;

[assembly: AssemblyProduct("Yort.Trashy")]
[assembly: AssemblyDescription("Types, interfaces & extension methods for creating or dealing with disposable resources in .Net.")]

[assembly: ComVisible(false)]

//Do not forget to update version # in netstandard project properties too
[assembly: AssemblyVersion("1.0.4.0")]
[assembly: AssemblyFileVersion("1.0.4.0")]

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if DEBUG
[assembly: AssemblyConfiguration("DEBUG")]
#else
[assembly: AssemblyConfiguration("RELEASE")]
#endif
[assembly: AssemblyCopyright("Copyright © 2018")]
[assembly: System.CLSCompliant(false)]
[assembly: NeutralResourcesLanguage("en")]