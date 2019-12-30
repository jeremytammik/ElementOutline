using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle( "ElementOutline" )]
[assembly: AssemblyDescription( "Revit C# .NET add-in to export 2D Element outlines" )]
[assembly: AssemblyConfiguration( "" )]
[assembly: AssemblyCompany( "Autodesk Inc." )]
[assembly: AssemblyProduct( "ElementOutline Revit C# .NET Add-In" )]
[assembly: AssemblyCopyright( "Copyright 2019 (C) Jeremy Tammik, Autodesk Inc." )]
[assembly: AssemblyTrademark( "" )]
[assembly: AssemblyCulture( "" )]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible( false )]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid( "321044f7-b0b2-4b1c-af18-e71a19252be0" )]

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
//
// History:
//
// 2019-07-23 2020.0.0.0 unchanged code extracted from RoomEditorApp
// 2019-07-23 2020.0.0.1 implemented access to instance geometry, first successful run
// 2019-07-23 2020.0.0.2 successful SvgPath text output
// 2019-08-05 2020.0.0.3 renamed GetSolidPlanViewBoundaryLoops and solidLoops variable
// 2019-08-05 2020.0.0.3 implemented GetSolidLoops and ExportLoops
// 2019-08-05 2020.0.0.3 implemented GetEdgeLoops framework
// 2019-08-05 2020.0.0.3 implemented EdgeLoopRetriever framework
// 2019-08-07 2020.0.0.4 started working on EdgeLoopRetriever.GetLoops
// 2019-08-15 2020.0.0.4 implemented JtLineCollection constructor from list of curves
// 2019-08-15 2020.0.0.4 implemented JtLineCollection.GetOutline
// 2019-08-16 2020.0.0.5 added use of Tessellate in JtLineCollection constructor
// 2019-08-16 2020.0.0.5 debugging GetOutlineRecusive
// 2019-08-17 2020.0.0.6 started implementing support for multiple loops
// 2019-08-18 2020.0.0.6 started implementing code using clipper for 2d union approach
// 2019-08-19 2020.0.0.6 implementing code using clipper for 2d union approach
// 2019-08-20 2020.0.0.6 implementing code using clipper for 2d union approach
// 2019-08-22 2020.0.0.7 completed first draft of 2d boolean union approach
// 2019-08-22 2020.0.0.8 added curve support to 2d boolean union approach
// 2019-08-23 2020.0.0.8 removed curve support to 2d boolean union approach
// 2019-08-24 2020.0.0.9 removed call to ExporterIFCUtils.SortCurveLoops on solid face loops
// 2019-08-25 2020.0.0.10 cast clipper PointInt coords from Int64 to int before constructing Point2dInt -- 2d Boolean loops work n ow
// 2019-08-25 2020.0.0.10 implemented JtLoop.Normalize -- loops are ok but family instance is offset
// 2019-08-25 2020.0.0.10 get instance geometry with identity transform -- solid and 2d boolean loops are identical
// 2019-09-03 2020.0.0.11 added support for full circle, i.e., closed Arc -- successful test on intercom element
// 2019-09-03 2020.0.0.12 deleted unused JtLine class
// 2019-09-03 2020.0.0.12 implemented GeoSnoop.DisplayLoops
// 2019-09-04 2020.0.0.13 reduce GeoSnoop.DisplayLoops target rectangle so edge lines remain visible
// 2019-09-04 2020.0.0.13 added caption
// 2019-09-04 2020.0.0.13 adjust bitmap edge size, not target image edge size
// 2019-09-04 2020.0.0.13 increased form image and edge sizes
// 2019-12-19 2020.0.1.0 implemented CmdRoomOuterOutline
// 2019-12-19 2020.0.1.0 refactored Cmd2dBoolean and implemented GetElementLoops
// 2019-12-19 2020.0.1.0 implemented GetSelectedRooms
// 2019-12-19 2020.0.1.0 started fleshing out CmdRoomOuterOutline
// 2019-12-19 2020.0.1.1 implemented AddToUnionRoom
// 2019-12-30 2020.0.1.1 worked on GetRoomOuterBoundaryLoop
//
[assembly: AssemblyVersion( "2020.0.1.1" )]
[assembly: AssemblyFileVersion( "2020.0.1.1" )]
