# ElementOutline

Revit C# .NET add-in to export 2D outlines of RVT project `Element` instances.

The add-in implements three external commands:

- [CmdExtrusionAnalyzer](#cmdextrusionanalyzer) &ndash; generate element outline using `ExtrusionAnalyzer`
- [Cmd2dBoolean](#cmd2dboolean) &ndash; generate element outline using 2D Booleans
- [CmdRoomOuterOutline](#cmdroomouteroutline) &ndash; outer room outline using 2D Booleans

All three generate element outlines of various types in varius ways.

The first uses the Revit API and 
the [`ExtrusionAnalyzer` class](https://www.revitapidocs.com/2020/ba9e3283-6868-8834-e8bf-2ea9e7358930.htm).

The other two make use of
the [Clipper integer coordinate based 2D Boolean operations library](http://angusj.com/delphi/clipper.php).

The add-in also implements a bunch of utilities for converting Revit coordinates to 2D data in millimetre units and displaying the resulting element outlines in a Windows form.


## <a name="task"></a>Task &ndash; 2D Polygon Representing Birds-Eye View of an Element

The goal is to export the 2D outlines of Revit `Element` instances, i.e., for each element, associate its element id or unique id with the list of X,Y coordinates describing a polygon representing the visual birds-eye view look of its outline.

Additional requirements:

- Address family instances as well as elements that might be built as part of the construction, including wall, floor, railing, ceiling, mechanical duct, panel, plumbing pipe.
- Generate a separate outline in place for each element, directly in its appropriate location and orientation.
- Output the result in a simple text file.

There is no need for a rendered view, just coordinates defining a 2D polygon around the element.

The goal is: given an element id, retrieve a list of X,Y coordinates describing the birds-eye view look of an element.

For instance, here are three sample images highlighting the bathtubs, doors and toilets, respectively, in a given floor of a building:

Bathtubs:

<img src="img/birdseye_view_bathtubs.png" alt="Bathtubs" title="Bathtubs" width="300"/>

Doors:

<img src="img/birdseye_view_internal_doors.png" alt="Doors" title="Doors" width="300"/>

Toilets:

<img src="img/birdseye_view_toilets.png" alt="Toilets" title="Toilets" width="300"/>

In end effect, we generate a dictionary mapping an element id or unique id to a list of space delimited pairs of X Y vertex coordinates in millimetres.


## <a name="cmdextrusionanalyzer"></a>CmdExtrusionAnalyzer

This code was originally implemented as part of (and later extracted from)
the [RoomEditorApp project](https://github.com/jeremytammik/RoomEditorApp).

The approach implemented for the room editor is not based on the 2D view, but on the element geometry solids in the 3D view and the result of applying
the [`ExtrusionAnalyzer` class](https://www.revitapidocs.com/2020/ba9e3283-6868-8834-e8bf-2ea9e7358930.htm) to them,
creating a vertical projection of the 3D element shape onto the 2D XY plane.
This approach is described in detail in the discussion on
the [extrusion analyser and plan view boundaries](https://thebuildingcoder.typepad.com/blog/2013/04/extrusion-analyser-and-plan-view-boundaries.html).

The [GeoSnoop .NET boundary curve loop visualisation](https://thebuildingcoder.typepad.com/blog/2013/04/geosnoop-net-boundary-curve-loop-visualisation.html) provides
some example images of the resulting putlines.

As you can see there, the outline generated is more precise and detailed than the standard 2D Revit representation.

The standard plan view of the default desk and chair components look like this in Revit:

<img src="img/desk_and_chair_plan.png" alt="Plan view of desk and chair in Revit" title="Plan view of desk and chair in Revit" width="318"/>

The loops exported by the RoomEditorApp add-in for the same desk and chair look like this instead:

<img src="img/desk_and_chair_loops.png" alt="Desk and chair loops in GeoSnoop" title="Desk and chair loops in GeoSnoop" width="318"/>

E.g., for the desk, you notice the little bulges for the desk drawer handles sticking out a little bit beyond the desktop surface.

For the chair, the arm rests are missing, because the solids used to model them do not make it through the extruson analyser, or maybe because the code ignores multiple disjust loops.

Here is an sample model with four elements highlighted in blue:

<img src="img/element_outline_four_selected_extrusion_analyser_svg_path.png" alt="Four elements selected" title="Four elements selected" width="300"/>

For them, the CmdExtrusionAnalyzer command generates the following JSON file defeining their outline polygon in SVG format:

```
{"name":"pt2+20+7", "id":"576786", "uid":"bc43ed2e-7e23-4f0e-9588-ab3c43f3d388-0008cd12", "svg_path":"M-56862 -9150 L-56572 -9150 -56572 -14186 -56862 -14186Z"}
{"name":"pt70/210", "id":"576925", "uid":"bc43ed2e-7e23-4f0e-9588-ab3c43f3d388-0008cd9d", "svg_path":"M-55672 -11390 L-55672 -11290 -55656 -11290 -55656 -11278 -55087 -11278 -55087 -11270 -55076 -11270 -55076 -11242 -55182 -11242 -55182 -11214 -55048 -11214 -55048 -11270 -55037 -11270 -55037 -11278 -54988 -11278 -54988 -11290 -54972 -11290 -54972 -11390Z"}
{"name":"pt80/115", "id":"576949", "uid":"bc43ed2e-7e23-4f0e-9588-ab3c43f3d388-0008cdb5", "svg_path":"M-56572 -10580 L-56572 -9430 -55772 -9430 -55772 -10580Z"}
{"name":"מנוע מזגן מפוצל", "id":"576972", "uid":"bc43ed2e-7e23-4f0e-9588-ab3c43f3d388-0008cdcc", "svg_path":"M-56753 -8031 L-56713 -8031 -56713 -8018 -56276 -8018 -56276 -8031 -56265 -8031 -56265 -8109 -56276 -8109 -56276 -8911 -56252 -8911 -56252 -8989 -56276 -8989 -56276 -9020 -56277 -9020 -56278 -9020 -56711 -9020 -56713 -9020 -56713 -8989 -56753 -8989 -56753 -8911 -56713 -8911 -56713 -8109 -56753 -8109Z"}
```

`M`, `L` and `Z` stand for `moveto`, `lineto` and `close`, respectively. Repetitions of `L` can be omitted. Nice and succinct.

However, the extrusion analyzer approach abviously fails for all elements that do not define any solids, e.g., 2D elements represented only by curves and meshes.

Hence the continued research to find an alternative approach and the implementation of `Cmd2dBoolean` dewscribed below making use of the Clipper library and 2D Booleans instead.

In July 2019, I checked with the development team and asked whether they could suggest a better way to retrieve the 2D outline of an element.

They responded that my `ExtrusionAnalyzer` approach seems like the best (and maybe only) way to achieve this right now.

Considering Cmd2dBoolean, I might add the caveat 'using the Revit API' to the last statement.


## <a name="cmd2dboolean"></a>Cmd2dBoolean

The `ExtrusionAnalyzer` approach based on element solids does not successfully address the task of generating the 2D birds-eye view outline for all Revit elements.



concave hull: 

- http://ubicomp.algoritmi.uminho.pt/local/concavehull.html
- https://towardsdatascience.com/the-concave-hull-c649795c0f0f
- /a/src/cpp/MIConvexHull/
- https://github.com/kubkon/powercrust
- /a/src/cpp/powercrust/
- 2D concave hull implementation: /a/src/cpp/concaveman-cpp/src/main/cpp/
- https://adared.ch/concaveman-cpp-a-very-fast-2d-concave-hull-maybe-even-faster-with-c-and-python/
- https://en.wikipedia.org/wiki/Alpha_shape
- https://www.codeproject.com/Articles/1201438/The-Concave-Hull-of-a-Set-of-Points
- http://www.cs.ubc.ca/research/flann/
  
2D outline:

- https://github.com/eppz/Unity.Library.eppz.Geometry
- https://github.com/eppz/Clipper
- https://github.com/eppz/Triangle.NET
- https://en.wikipedia.org/wiki/Sweep_line_algorithm
- https://stackoverflow.com/questions/4213117/the-generalization-of-bentley-ottmann-algorithm
- https://ggolikov.github.io/bentley-ottman/
- Joining unordered line segments -- https://stackoverflow.com/questions/1436091/joining-unordered-line-segments
- join all line segments into closed polygons
- union all the polygons using clipper
- List<IntPoint2d> vertices;
- List<Pair<int,int>> segments;
- Dictionary<IntPoint2d,int> map_end_point_to_segments_both_directions;
- http://www3.cs.stonybrook.edu/~algorith/implement/sweep/implement.shtml
- ~/downloads/top_sweep/
- https://github.com/mikhaildubov/Computational-geometry/blob/master/2)%20Any%20segments%20intersection/src/ru/dubov/anysegmentsintersect/SegmentsIntersect.java
- https://github.com/jeremytammik/wykobi/blob/master/wykobi_naive_group_intersections.inl

alpha shape:

- https://pypi.org/project/alphashape/
- https://alphashape.readthedocs.io/

outline_solids_and_booleans.png

outline_solids_and_booleans.png

We determined that some elements have no solids, just meshes, hence the extrusion anayser approach cannot be used

Working on the 2D contour outline following algorithm here:

https://github.com/jeremytammik/ElementOutline

Plan to look at the alpha shape implementation here:

https://pypi.org/project/alphashape

i worked quite a lot on a contour follower, but it is turning out quite complex. i have another idea now for a much simpler approach using 2D Boolean operations, uniting all the solid faces and mesh faces into one single 2D polygon set. i'll try that next and expect immediate and robust results.

still working extensively on this. do you think we can get by with handling only meshes and solids? in that case, the analysis needs to be run in a 3D view. in a 2D view, the solid of the bathtub is tested on is empty (zero faces, zero volume), and all the rest is just curves. i'll switch to a 3D view and test the bathtub and the mesh element you mention above.

i now completed a new poly2d implementation using 2D Booleans instead of the solids and extrusion analyser. i expect it is significantly faster. have not benchmarked, yet, however. i have not tested it on a mesh yet. can you provide the mesh sample element from the amidav model in a separate file, please? i cannot open the amidav model. revit 2020 says the file is corrupted. thank you. the ElementOutline release 2020.0.0.10 exportes outlines from both solids and 2d booleans and generates identical results for both, so that is a good sign:

https://github.com/jeremytammik/ElementOutline/releases/tag/2020.0.0.10

maybe meshes and solids cover all requirements. i am still experimenting and testing. a test model with a collection of test cases would be handy. for instance, a small model with just a handful of elements that cover all possible variations.

what is missing besided meshes and solids?

response: I received the new sample model from you, proj_with_mesh.rvt. It contains one single wall. That wall is not represented by a mesh, but by a solid, just as all other walls, afaict. The results of running the solid extrusion analyser and the 2d boolean command on it are identical. Furthermore, snooping its geometry in a 3D view, i see one single solid and zero meshes. did you send the right element?

release 20202.0.0.12:

https://github.com/jeremytammik/ElementOutline/releases/tag/2020.0.0.12

jeremytammik attached outline_solids_and_booleans.png to this card Sep 3, 2019 at 12:42 PM

the intercom element is not a mesh, just a circle, represented by a full closed arc. i implemented support to include that in the boolean operation.

i also implemented a utility GeoSnoop to display the loops generated in a temporary windows form.

here is an image showing the Revit model (walls, bathtub, intercom) and two GeoSnoop windows. the left one shows the loops retrieved from the solids. the right one shows the loops retrieved from the 2D Booleans, including closed arcs. not the intercom and the bathtub drain.

my target is to continue enhancing the 2D booleans until they include all the solid loop information, so that we can then get rid of the solid and extrusion analyser code.

outline_solids_and_booleans.png

maybe it is caused because you need to use LevelOfDetail=Fine?

We use this code:

```
  Options opt = new Options { IncludeNonVisibleObjects = true, DetailLevel = ViewDetailLevel.Fine};
  GeometryElement geomElem = element.get_Geometry(opt);
```

my code is on github, and the link is at the end of my previous message, release 20202.0.0.12:

https://github.com/jeremytammik/ElementOutline/releases/tag/2020.0.0.12

i can try again with fine detail level. however, the circle already looks very good to me. in fact, right now, i think all we need is there, in the combination of the two. and as i said, i plan to enhance the 2d boolean result to include everything returned by the solid extrusion analysis as well. so this is moving forward well.

the first image was capturing data from a 2D view. capturing the 2D Booleans from a 3D view gives us all we need, I think. here is a new image with larger previews and captions added:

outline_solids_and_booleans.png

the enhanced version is 2020.0.0.13:

https://github.com/jeremytammik/ElementOutline/releases/tag/2020.0.0.13

jeremytammik attached outline_solids_and_booleans.png to this card Sep 4, 2019 at 3:52 PM

Feedback: we trested a few use-cases and it seems to be working fine.

Currently, the production pipeline uses an implementation in Python using Shapely to union() the triangles. 

https://github.com/Toblerity/Shapely

https://pypi.org/project/Shapely/

Manipulation and analysis of geometric objects https://shapely.readthedocs.io/en/lat…

but, it is slower, so I believe we will switch to clipper.


## <a name="cmdroomouteroutline"></a>CmdRoomOuterOutline

I implemented the third command `CmdRoomOuterOutline` after an unsuccesful attempt at generating the outer outline of a room including its bounding elements
by [specifying a list of offsets to `CreateViaOffset`](https://thebuildingcoder.typepad.com/blog/2019/12/dashboards-createviaoffset-and-room-outline-algorithms.html#3).

After that failed, I suggested a number of alternative approaches 
to [determine th room outline including surrounding walls](https://thebuildingcoder.typepad.com/blog/2019/12/dashboards-createviaoffset-and-room-outline-algorithms.html#4).


**Question:** I started to look at the possibility of tracing the outside of the walls several weeks ago, when I was at a loss utilising `CreateViaOffset`.

I was finding it difficult to create the closed loop necessary, and particularly how I would achieve this were the wall thickness changes across its length.

Could you point me in the right direction, possibly some sample code that I could examine and see if I could get it to work to my requirements.

**Answer:** I see several possible alternative approaches avoiding the use of `CreateViaOffset`, based on:

- Room boundary curves and wall thicknesses
- Room boundary curves and wall bottom face edges
- Projection of 3D union of room and wall solids
- 2D union of room and wall footprints

The most immediate and pure Revit API approach would be to get the curves representing the room boundaries, determine the wall thicknesses, offset the wall boundary curves outwards by wall thickness plus minimum offset, and ensure that everything is well connected by adding small connecting segments in the gaps where the offset jumps.

Several slightly more complex pure Revit API approaches could be designed by using the wall solids instead of just offsetting the room boundary curves based on the wall thickness. For instance, we could query the wall bottom face for its edges, determine and patch together all the bits of edge segments required to go around the outside of the wall instead of the inside.

Slightly more complex still, and still pure Revit API: determine the room closed shell solid, unite it with all the wall solids, and make use of the extrusion analyser to project this union vertically onto the XY plane and grab its outside edge.

Finally, making use of a minimalistic yet powerful 2D Boolean operation library, perform the projection onto the XY plane first, and unite the room footprint with all its surrounding wall footprints in 2D instead. Note that the 2D Booleans are integer based. To make use of those, I convert the geometry from imperial feet units using real numbers to integer-based millimetres.

The two latter approaches are both implemented in
my [ElementOutline add-in](https://github.com/jeremytammik/ElementOutline).

I mentioned it here in two previous threads:

- [Question regarding SVG data](https://forums.autodesk.com/t5/revit-api-forum/question-regarding-svg-data-from-revit/m-p/9106146)
- [How do I get the outline and stakeout path of a built-in loft family](https://forums.autodesk.com/t5/revit-api-forum/how-do-i-get-the-outline-and-stakeout-path-of-a-built-in-loft/m-p/9148138)

Probably all the pure Revit API approaches will run into various problematic exceptional cases, whereas the 2D Booleans seem fast, reliable and robust and may well be able to handle all the exceptional cases that can possibly occur, so I would recommend trying that out first.



## Author

Jeremy Tammik, [The Building Coder](http://thebuildingcoder.typepad.com), [ADN](http://www.autodesk.com/adn) [Open](http://www.autodesk.com/adnopen), [Autodesk Inc.](http://www.autodesk.com)


## License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT).
Please see the [LICENSE](LICENSE) file for full details.

