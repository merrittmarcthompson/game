using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Game
{
  public static partial class Static
  {
    // Convert map.*.graphml-type content into a dictionary of locations. The keys are the graphml node IDs.
    // The overall structure of the graphml is like this:
    /*
      <graph>
          <node id="NODE_ID"> // a terminal node
              ...
                  <y:NodeLabel>NODE_TEXT</y:NodeLabel>
              ...
          </node>
          <edge id="e0" source="NODE_ID" target="NODE_ID"> // an edge node
              ...
                  <y:EdgeLabel>EDGE_TEXT</y:EdgeLabel>
              ...
          </edge>
          <node id="NODE_ID" yfiles.foldertype="group"> // a group node
              ...
                  <data>
                      ...
                          <y:NodeLabel>GROUP_TEXT</y:NodeLabel>
                      ...
                  </data>
                  <graph>
                      <node id="NODE_ID"> // a nested terminal node
                          ...
                              <y:NodeLabel>NESTED_NODE_TEXT</y:NodeLabel>
                          ...
                      <node id="NODE_ID" yfiles.foldertype="group"> // a nested group node
                          // etc.
                      </node>
                  </graph>
              ...
          </node>
      </graph>
    */
    // The output is tags like this:
    /*
      	map.test_n0:text=This is a room.
	      map.test_n0:arrow=e0
	      map.test_n0:arrow=e1
	      map.test_e0:text=There is a door.
	      map.test_e0:target=n1
	      map.test_e1:text=There is a dog.
	      map.test_e1:target=n2
	      map.test_n1:text=
	      map.test_n1:line=e2
	      map.test_n2:text=Woof!
        map.test_n2:line=n1
    */
    public static HashSet<Tag> GraphmlToTags(
      string graphml,
      string uniquifier)
    {
      XNamespace g = "http://graphml.graphdrawing.org/xmlns";
      XNamespace y = "http://www.yworks.com/xml/graphml";
      XElement root = XElement.Parse(graphml);

      var result = new HashSet<Tag>();

      // 1. Create a value for each non-group node in the source graphml, that contains the text.

      IEnumerable<XElement> nodes =
        from node in root.Descendants(g + "node")
        where node.Attribute("yfiles.foldertype")?.Value != "group"
        select node;

      foreach (XElement node in nodes)
      {
        var item = new Tag(uniquifier + "_" + node.Attribute("id").Value, "text", node.Descendants(y + "NodeLabel").First().Value);
        result.Add(item);
      }

      // 2. Add the arrows.

      IEnumerable<XElement> edges =
        from edge in root.Descendants(g + "edge")
        select edge;

      foreach (XElement edge in edges)
      {
        string sourceNodeId = edge.Attribute("source").Value;
        string targetNodeId = edge.Attribute("target").Value;
        string edgeId = edge.Attribute("id").Value;
        string edgeText = edge.Descendants(y + "EdgeLabel").DefaultIfEmpty(null)?.First()?.Value;
        result.Add(new Tag(uniquifier + "_" + sourceNodeId, "arrow", uniquifier + "_" + edgeId));
        result.Add(new Tag(uniquifier + "_" + edgeId, "text", edgeText));
        result.Add(new Tag(uniquifier + "_" + edgeId, "target", uniquifier + "_" + targetNodeId));
      }

      // 3. Set the groups based on the groups in the source graphml.
      //<node id="n2" yfiles.foldertype="group">
      //  <data key="d6">
      //    <y:GroupNode>
      //      <y:NodeLabel>Opened Name</y:NodeLabel>
      //      <y:State closed="false"/>
      //    </y:GroupNode>
      //    <y:GroupNode>
      //      <y:NodeLabel>Closed Name</y:NodeLabel>
      //      <y:State closed="true"/>
      //    </y:GroupNode>
      //  </data>

      IEnumerable<XElement> groupFolderTypeNodes =
        from groupFolderTypeNode in root.Descendants(g + "node")
        where groupFolderTypeNode.Attribute("yfiles.foldertype")?.Value == "group"
        select groupFolderTypeNode;

      foreach (XElement groupFolderTypeNode in groupFolderTypeNodes)
      {
        IEnumerable<XElement> groupNodes =
          from groupNode in groupFolderTypeNode.Descendants(y + "GroupNode")
          where groupNode.Descendants(y + "State").Attributes("closed").First().Value == "false"
          select groupNode;

        string groupId = groupNodes.First().Descendants(y + "NodeLabel").First().Value;

        IEnumerable<string> subNodes =
          from subNode in groupNodes.First().Parent.Parent.Parent.Parent.Descendants(g + "node")
          where subNode.Attribute("yfiles.foldertype")?.Value != "group"
          select subNode.Attribute("id").Value;

        result.Add(new Tag(uniquifier + ":" + subNodes.First(), "group", groupId));
      }
      return result;
    }
  }
}
