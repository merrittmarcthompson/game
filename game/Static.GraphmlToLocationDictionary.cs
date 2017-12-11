using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Game
{
  public static partial class Static
  {
    // Convert map.*.graphml-type content into a dictionary of locations. The keys are the graphml node IDs.
    public static Dictionary<string, Location> GraphmlToLocationDictionary(
      string graphml)
    {
      XNamespace g = "http://graphml.graphdrawing.org/xmlns";
      XNamespace y = "http://www.yworks.com/xml/graphml";
      XElement root = XElement.Parse(graphml);

      var locations = new Dictionary<string, Location>();

      // 1. Create a Location for each non-group node in the source graphml.

      IEnumerable<XElement> nodes =
        from node in root.Descendants(g + "node")
        where node.Attribute("yfiles.foldertype")?.Value != "group"
        select node;

      foreach (XElement node in nodes)
      {
        Location location = new Location();
        location.SourceText = node.Descendants(y + "NodeLabel").First().Value;
        location.Targets = new Dictionary<string, string>();
        locations.Add(node.Attribute("id").Value, location);
      }

      // 2. Set the location targets based on the edges (arrows) in the source graphml.

      IEnumerable<XElement> edges =
        from edge in root.Descendants(g + "edge")
        select edge;

      foreach (XElement edge in edges)
      {
        string source = edge.Attribute("source").Value;
        string target = edge.Attribute("target").Value;
        string link = edge.Descendants(y + "EdgeLabel").DefaultIfEmpty(null)?.First()?.Value;
        if (link != null)
          locations[source].Targets.Add(link, target);
      }

      // 3. Set the zones based on the groups in the source graphml.
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

        string zoneId = groupNodes.First().Descendants(y + "NodeLabel").First().Value;

        IEnumerable<string> subNodes =
          from subNode in groupNodes.First().Parent.Parent.Parent.Parent.Descendants(g + "node")
          where subNode.Attribute("yfiles.foldertype")?.Value != "group"
          select subNode.Attribute("id").Value;

        locations[subNodes.First()].AuditoryZoneId = zoneId;
        locations[subNodes.First()].VisualZoneId = zoneId;
      }

      return locations;
    }
  }
}
