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
      public Unit FirstUnit { get; }
      public Dictionary<string, Unit> UnitsByUniqueId { get; }
      public Dictionary<string, ReactionArrow> ReactionArrowsByUniqueId { get; }
      public Dictionary<string, Setting> Settings { get; }

      public World(
         string sourceDirectory)
      {
         // Load all the graphml files in the source directory.
         var sourcePaths = Directory.GetFiles(sourceDirectory, "*.graphml");
         if (sourcePaths.Length < 1)
            throw new InvalidOperationException(string.Format($"no .graphml files in directory {sourceDirectory}"));

         Unit? hopefullyFirstUnit = null;

         Settings = LoadSettings(sourceDirectory);
         UnitsByUniqueId = new Dictionary<string, Unit>();
         ReactionArrowsByUniqueId = new Dictionary<string, ReactionArrow>();

         // Create a temporary list of actions from the nodes in the graphml that have scene IDs, so we can link merges to them in this routine later.
         var unitsBySceneId = new Dictionary<string, Unit>();
         // Create a temporary list of merges that need to be linked to actions by scene ID.
         var mergeFixups = new List<MergeArrow>();

         var settingsReportWriter = new StreamWriter("settings.csv", false);
         settingsReportWriter.WriteLine("SETTING,OPERATION,VALUE,FILE");

         foreach (var sourcePath in sourcePaths)
         {
            var sourceName = Path.GetFileName(sourcePath);

            // Create a temporary list of actions from the nodes in the graphml, so we can link arrows to them in this routine later.
            var unitsByNodeId = new Dictionary<string, Unit>();

            var graphml = new Graphml(File.ReadAllText(sourcePath));
            foreach (var (nodeId, label) in graphml.Nodes())
            {
               Unit unit = new Unit(sourceName, nodeId, new CodeTree(label, sourceName, Settings));
               UnitsByUniqueId[sourceName + ":" + nodeId] = unit;
               EvaluateSettingsReport(unit.ActionCode, sourceName, settingsReportWriter);
               unitsByNodeId.Add(nodeId, unit);

               // Check if there's a [scene ID] declaration.
               var declaredSceneId = EvaluateScene(unit.ActionCode);
               if (declaredSceneId != null)
               {
                  if (unitsBySceneId.ContainsKey(declaredSceneId))
                     throw new InvalidOperationException(string.Format($"{sourceName}: Scene '{declaredSceneId}' declared twice"));
                  unitsBySceneId.Add(declaredSceneId, unit);
                  if (declaredSceneId == "start")
                  {
                     if (hopefullyFirstUnit != null)
                        throw new InvalidOperationException(string.Format($"{sourceName}: More than one start scene"));
                     hopefullyFirstUnit = unit;
                  }
               }
            }

            // Create and attach merges and reactions to the actions we just created.
            foreach (var (edgeId, sourceNodeId, targetNodeId, label) in graphml.Edges())
            {
               // Point the arrow to its target action.
               if (!unitsByNodeId.TryGetValue(targetNodeId, out var targetUnit))
                  throw new InvalidOperationException(string.Format($"{sourceName}: Internal error: no node declaration for referenced target node '{targetNodeId}'"));

               var code = new CodeTree(label, sourceName, Settings);
               EvaluateSettingsReport(code, sourceName, settingsReportWriter);
               var (isMerge, referencedSceneId, isReturn) = EvaluateArrowType(code);
               Arrow arrow;
               if (isMerge)
               {
                  var mergeArrow = new MergeArrow(targetUnit, code, referencedSceneId, sourceName);
                  if (referencedSceneId != null)
                     mergeFixups.Add(mergeArrow);
                  arrow = mergeArrow;
               }
               else if (isReturn)
                  arrow = new ReturnArrow(targetUnit, code);
               else
               {
                  var reactionArrow = new ReactionArrow(targetUnit, code, sourceName, edgeId);
                  ReactionArrowsByUniqueId[reactionArrow.UniqueId] = reactionArrow;
                  arrow = reactionArrow;
               }
               // Add the arrow to the source action's arrows.
               if (!unitsByNodeId.TryGetValue(sourceNodeId, out var sourceUnit))
                  throw new InvalidOperationException(string.Format($"{sourceName}: Internal error: no node declaration for referenced source node '{sourceNodeId}'"));
               sourceUnit.Arrows.Add(arrow);
            }
         }

         // Make a second pass to point merges that reference scenes to the scene's first action.
         foreach (var mergeArrow in mergeFixups)
         {
            if (mergeArrow.DebugSceneId == null)
#pragma warning disable CA1303 // Do not pass literals as localized parameters
               throw new InvalidOperationException("Internal error: unexpected null scene ID");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            if (!unitsBySceneId.TryGetValue(mergeArrow.DebugSceneId, out var targetSceneUnit))
               throw new InvalidOperationException(string.Format($"No scene declaration for referenced scene ID '{mergeArrow.DebugSceneId}'"));
            mergeArrow.TargetSceneUnit = targetSceneUnit;
         }

         settingsReportWriter.Close();

         if (hopefullyFirstUnit == null)
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            throw new InvalidOperationException("No start scene found.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

         FirstUnit = hopefullyFirstUnit;

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

      public static Unit BuildReturnUnitFor(
         List<ReturnArrow> returnArrows)
      {
         if (returnArrows == null) throw new ArgumentNullException(nameof(returnArrows));
         var result = new Unit("returnUnit", "returnUnit", new CodeTree("", "returnUnit", new Dictionary<string, Setting>()));
         foreach (var returnArrow in returnArrows)
         {
            var mergeArrow = new MergeArrow(returnArrow.TargetUnit, returnArrow.Code, "returnArrow", "returnArrow");
            result.Arrows.Add(mergeArrow);
         }
         return result;
      }
   }
}
