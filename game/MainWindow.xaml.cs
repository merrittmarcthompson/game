using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Xml.Linq;

namespace game
{
  public class Location
  {
    // This is the description of the location from the graphml.
    public string Text;

    // These come from the group labels in the graphml.
    public string VisualZoneId;
    public string AuditoryZoneId;

    // Key is a hyperlink string extracted from the text. Value is a node ID from the graphml.
    public Dictionary<string, string> Targets;
  }

  public partial class MainWindow : Window
  {
    static Dictionary<string, Location> GraphmlToLocations(
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
        location.Text = node.Descendants(y + "NodeLabel").First().Value;
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

    public MainWindow()
    {
      InitializeComponent(); 

      string graphml = System.IO.File.ReadAllText("map.boneyard.graphml");
      var locations = GraphmlToLocations(graphml);

      // TEST ONLY 

      string listing = "";

      foreach (var location in locations)
      {
        listing += location.Key + "=" + location.Value.Text + "\n";
        listing += "auditory zone=" + location.Value.AuditoryZoneId + "\n";
        listing += "visual zone=" + location.Value.VisualZoneId + "\n";
        foreach (var target in location.Value.Targets)
        {
          listing += "   " + target.Key + "=>" + target.Value + "\n";
        }
        listing += "\n";
      }

      FlowDocumentScrollViewer outputBox = (FlowDocumentScrollViewer)FindName("viewer");
      FlowDocument document = new FlowDocument();
      Paragraph paragraph = new Paragraph(new Run(listing));
      paragraph.FontSize = 13;
      document.Blocks.Add(paragraph);
      outputBox.Document = document;
    }
  }
}
