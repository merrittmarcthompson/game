using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Game
{
   public static partial class Transform
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
           map_test_n0.sourceText=This is a room.
           map_test_n0.isNode=
           map_test_n0.arrow=map_test_e0
           map_test_n0.arrow=map_test_e1
           map_test_e0.sourceText=There is a door.
           map_test_e0.target=map_test_n1
           map_test_e1.sourceText=There is a dog.
           map_test_e1.target=map_test_n2
           map_test_n1.sourceText=
           map_test_n1.isNode=
           map_test_n2.sourceText=Woof!
           map_test_n2.isNode=
      */
      public static Tags GraphmlToTags(
        string graphml,
        string uniquifier)
      {
         string BuildId(
           string graphmlId)
         {
            return Transform.IntoId(uniquifier + "_" + graphmlId);
         }

         XNamespace g = "http://graphml.graphdrawing.org/xmlns";
         XNamespace y = "http://www.yworks.com/xml/graphml";
         XElement root = XElement.Parse(graphml);

         var result = new Tags();

         // 1. Create a value for each non-group node in the source graphml, that contains the text.

         IEnumerable<XElement> nodes =
           from node in root.Descendants(g + "node")
           where node.Attribute("yfiles.foldertype")?.Value != "group"
           select node;

         foreach (XElement node in nodes)
         {
            var id = BuildId(node.Attribute("id").Value);
            result.Add(id, "sourceText", node.Descendants(y + "NodeLabel").First().Value);
            result.Add(id, "isNode", "");
            var color = node.Descendants(y + "Fill").First().Attribute("color")?.Value;
            var color2 = node.Descendants(y + "Fill").First().Attribute("color2")?.Value;
            if (color == "#FFCC99") // light orange
            {
               result.Add(id, "isStage", "");
            }
            else if (color == "#99CCFF") // light blue
            {
               result.Add(id, "isDoor", "");
            }
            else if (color == "#FFFF99") // light yellow
            {
               result.Add(id, "isStorage", "");
            }
            else if (color == "#CCFFCC") // light green
            {
               result.Add(id, "isCast", "");
            }
            else if (color == "#C0C0C0") // light gray
            {
               result.Add(id, "isProp", "");
            }
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
            result.Add(BuildId(sourceNodeId), "arrow", BuildId(edgeId));
            result.Add(BuildId(edgeId), "sourceText", edgeText);
            result.Add(BuildId(edgeId), "target", BuildId(targetNodeId));
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

            result.Add(BuildId(subNodes.First()), "group", groupId);
         }
         return result;
      }
   }
}
