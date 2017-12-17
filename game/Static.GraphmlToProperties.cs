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
          <edge source="NODE_ID" target="NODE_ID"> // an edge node
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
    // The output is tuples like this:
    /*
      "n0::n0", "text", "Here is a small room, clean, of old wood painted white. There's a fan\non the ceiling and a small metal table with some medical equipment\non it. There's a old, metal-framed bed. There are no windows."
      "n0::n1", "text", "A man sits in a chair by the bed."
      "n0::n2", "text", "[First] [Last] lies in the bed."
      "n0::n3", "text", "There is a door with a lock."
      "n0::n0", "target", "n0::n1~door"
      "n0::n0", "target", "n0::n2~window"
      "n0::n0", "target", "n0::n3~box"
      "n0::n0", "group", "Doc Mitchell's infirmary"
    */
    public static HashSet<(string, string, string)> GraphmlToProperties(
      string graphml)
    {
      XNamespace g = "http://graphml.graphdrawing.org/xmlns";
      XNamespace y = "http://www.yworks.com/xml/graphml";
      XElement root = XElement.Parse(graphml);

      var result = new HashSet<(string, string, string)>();

      // 1. Create a value for each non-group node in the source graphml, that contains the text.

      IEnumerable<XElement> nodes =
        from node in root.Descendants(g + "node")
        where node.Attribute("yfiles.foldertype")?.Value != "group"
        select node;

      foreach (XElement node in nodes)
      {
        var item = (node.Attribute("id").Value, "text", node.Descendants(y + "NodeLabel").First().Value);
        result.Add(item);
      }

      // 2. Set the targets for the node based on the edges (arrows) in the source graphml.

      IEnumerable<XElement> edges =
        from edge in root.Descendants(g + "edge")
        select edge;

      foreach (XElement edge in edges)
      {
        string source = edge.Attribute("source").Value;
        string target = edge.Attribute("target").Value;
        string text = edge.Descendants(y + "EdgeLabel").DefaultIfEmpty(null)?.First()?.Value;
        result.Add((source, "target", target + "~" + text));
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

        result.Add((subNodes.First(), "group", groupId));
      }
      return result;
    }
  }
}
