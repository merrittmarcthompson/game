#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gamebook
{
   public class World
   {
      // World loads the static description of the game world from .graphml files in a directory.

      // These members describe the game world.
      public Node FirstNode { get; }
      public Dictionary<string, Node> NodesByUniqueId { get; }
      public Dictionary<string, ReactionArrow> ReactionArrowsByUniqueId { get; }
      public Dictionary<string, Setting> InitialSettings { get; }

      public World(
         string sourceDirectory)
      {
         // Load all the graphml files in the source directory.
         var sourcePaths = Directory.GetFiles(sourceDirectory, "*.graphml");
         if (sourcePaths.Length < 1)
            throw new InvalidOperationException(string.Format($"no .graphml files in directory {sourceDirectory}"));

         Node? hopefullyFirstNode = null;

         InitialSettings = LoadSettings(sourceDirectory);
         NodesByUniqueId = new Dictionary<string, Node>();
         ReactionArrowsByUniqueId = new Dictionary<string, ReactionArrow>();

         // Create a temporary list of actions from the nodes in the graphml that have scene IDs, so we can link merges to them in this routine later.
         var nodesBySceneId = new Dictionary<string, Node>();
         // Create a temporary list of merges that need to be linked to actions by scene ID.
         var mergeFixups = new List<MergeArrow>();

         var settingsReportWriter = new StreamWriter("settings.csv", false);
         settingsReportWriter.WriteLine("SETTING,OPERATION,VALUE,FILE");

         foreach (var sourcePath in sourcePaths)
         {
            var sourceName = Path.GetFileName(sourcePath);

            // Create a temporary list of actions from the nodes in the graphml, so we can link arrows to them in this routine later.
            var nodesByNodeId = new Dictionary<string, Node>();

            var graphml = new Graphml(File.ReadAllText(sourcePath));
            foreach (var (nodeId, label) in graphml.Nodes())
            {
               Node node = new Node(sourceName, nodeId, new CodeTree(label, sourceName, InitialSettings));
               NodesByUniqueId[sourceName + ":" + nodeId] = node;
               EvaluateSettingsReport(node.ActionCode, sourceName, settingsReportWriter);
               nodesByNodeId.Add(nodeId, node);

               // Check if there's a [scene ID] declaration.
               var declaredSceneId = EvaluateScene(node.ActionCode);
               if (declaredSceneId != null)
               {
                  if (nodesBySceneId.ContainsKey(declaredSceneId))
                     throw new InvalidOperationException(string.Format($"{sourceName}: Scene '{declaredSceneId}' declared twice"));
                  nodesBySceneId.Add(declaredSceneId, node);
                  if (declaredSceneId == "start")
                  {
                     if (hopefullyFirstNode != null)
                        throw new InvalidOperationException(string.Format($"{sourceName}: More than one start scene"));
                     hopefullyFirstNode = node;
                  }
               }
            }

            // Create and attach merges and reactions to the actions we just created.
            foreach (var (edgeId, sourceNodeId, targetNodeId, label) in graphml.Edges())
            {
               // Point the arrow to its target action.
               if (!nodesByNodeId.TryGetValue(targetNodeId, out var targetNode))
                  throw new InvalidOperationException(string.Format($"{sourceName}: Internal error: no node declaration for referenced target node '{targetNodeId}'"));

               var code = new CodeTree(label, sourceName, InitialSettings);
               EvaluateSettingsReport(code, sourceName, settingsReportWriter);
               var (isMerge, referencedSceneId, isReturn) = EvaluateArrowType(code);
               Arrow arrow;
               if (isMerge)
               {
                  var mergeArrow = new MergeArrow(targetNode, code, referencedSceneId, sourceName);
                  if (referencedSceneId != null)
                     mergeFixups.Add(mergeArrow);
                  arrow = mergeArrow;
               }
               else if (isReturn)
                  arrow = new ReturnArrow(targetNode, code);
               else
               {
                  var reactionArrow = new ReactionArrow(targetNode, code, sourceName, edgeId);
                  ReactionArrowsByUniqueId[reactionArrow.UniqueId] = reactionArrow;
                  arrow = reactionArrow;
               }
               // Add the arrow to the source action's arrows.
               if (!nodesByNodeId.TryGetValue(sourceNodeId, out var sourceNode))
                  throw new InvalidOperationException(string.Format($"{sourceName}: Internal error: no node declaration for referenced source node '{sourceNodeId}'"));
               sourceNode.Arrows.Add(arrow);
            }
         }

         // Make a second pass to point merges that reference scenes to the scene's first action.
         foreach (var mergeArrow in mergeFixups)
         {
            if (mergeArrow.DebugSceneId == null)
#pragma warning disable CA1303 // Do not pass literals as localized parameters
               throw new InvalidOperationException("Internal error: unexpected null scene ID");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            if (!nodesBySceneId.TryGetValue(mergeArrow.DebugSceneId, out var targetSceneNode))
               throw new InvalidOperationException(string.Format($"No scene declaration for referenced scene ID '{mergeArrow.DebugSceneId}'"));
            mergeArrow.TargetSceneNode = targetSceneNode;
         }

         settingsReportWriter.Close();

         if (hopefullyFirstNode == null)
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            throw new InvalidOperationException("No start scene found.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

         FirstNode = hopefullyFirstNode;

         // Some helper functions.

         Dictionary<string, Setting> LoadSettings(
            string sourceDirectory)
         {
            var result = new Dictionary<string, Setting>();

            // This is just a quick, cheesy way to load this.
            foreach (var words in
               File.ReadLines(Path.Combine(sourceDirectory, "settings.txt"))
                  .Select(line => line.Split(' ')))
            {
               switch (words[0])
               {
                  case "score":
                     // ex. 'score brave'
                     result.Add(words[1], new ScoreSetting());
                     break;
                  case "flag":
                     // ex. 'flag tvOn'
                     result.Add(words[1], new BooleanSetting(false));
                     break;
                  case "string":
                     result.Add(words[1], new StringSetting(""));
                     break;
                     // Ignore anything else as comments.
               }
            }
            return result;
         }

         string? EvaluateScene(
            CodeTree codeTree)
         {
            return codeTree.Traverse()
               .OfType<SceneCode>()
               .Select(sceneCode => sceneCode.SceneId)
               .Cast<string?>()
               .DefaultIfEmpty(null)
               .First();
         }

         (bool, string?, bool) EvaluateArrowType(
            CodeTree codeTree)
         {
            bool isMerge = false;
            bool isReturn = false;
            string? referencedSceneId = null;
            foreach (var code in codeTree.Traverse())
            {
               switch (code)
               {
                  case MergeCode mergeCode:
                     if (isReturn)
                        throw new InvalidOperationException(string.Format($"Can't return and merge in the same arrow in\n{codeTree.SourceText}."));
                     isMerge = true;
                     referencedSceneId = mergeCode.SceneId;
                     break;
                  case ReturnCode returnCode:
                     if (isMerge)
                        throw new InvalidOperationException(string.Format($"Can't merge and return in the same arrow in\n{codeTree.SourceText}."));
                     isReturn = true;
                     break;
               }
            }
            return (isMerge, referencedSceneId, isReturn);
         }

         void EvaluateSettingsReport(
            CodeTree codeTree,
            string sourceName,
            StreamWriter writer)
         {
            var sourceNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceName);
            foreach (var code in codeTree.Traverse())
            {
               switch (code)
               {
                  case SetCode setCode:
                     Write("set", setCode.Expressions);
                     break;
                  case WhenCode whenCode:
                     Write("when", whenCode.Expressions);
                     break;
                  case IfCode ifCode:
                     Write("if", ifCode.Expressions);
                     break;
                     //case ScoreCode scoreCode:
                     //   Write("score", scoreCode.Ids.Select(id => new Expression(false, id, null)));
                     //   break;
               }
            }

            void Write(
               string operation,
               IEnumerable<Expression> expressions)
            {
               foreach (var expression in expressions)
               {
                  var line = "";
                  line += expression.LeftId;
                  line += ",";
                  line += operation;
                  if (expression.Not)
                     line += " not";
                  line += ",";
                  if (expression.RightId != null)
                     line += expression.RightId;
                  line += ",";
                  line += sourceNameWithoutExtension;
                  writer.WriteLine(line);
               }
            }
         }
      }

      public static Node BuildReturnNodeFor(
         List<ReturnArrow> returnArrows)
      {
         if (returnArrows == null) throw new ArgumentNullException(nameof(returnArrows));
         var result = new Node("returnNode", "returnNode", new CodeTree("", "returnNode", new Dictionary<string, Setting>()));
         foreach (var returnArrow in returnArrows)
         {
            var mergeArrow = new MergeArrow(returnArrow.TargetNode, returnArrow.Code, "returnArrow", "returnArrow");
            result.Arrows.Add(mergeArrow);
         }
         return result;
      }
   }
}
