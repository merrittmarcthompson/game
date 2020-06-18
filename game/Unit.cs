﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gamebook
{
   //  All of the graphml source code gets converted into this data structure.

   public class WithId
   {
      protected static string BuildUniqueId(
         string sourceName,
         string sourceId)
      {
         return sourceName.Replace(' ', '-') + ":" + sourceId;
      }
   }

   public class Arrow: WithId
   {
      public Unit TargetUnit { get; protected set; }
      public CodeTree Code { get; protected set;  }

      protected Arrow() { }
      protected Arrow(
         Unit targetUnit,
         CodeTree code)
      {
         TargetUnit = targetUnit;
         Code = code;
      }
   }

   public class MergeArrow: Arrow
   {
      public string DebugSceneId { get; private set; }
      public string DebugSourceName { get; private set; }

      private MergeArrow() { }
      // This lets the Load function make arrows. 
      public MergeArrow(
         Unit targetUnit,
         CodeTree code,
         string debugSceneId,
         string debugSourceName): base (targetUnit, code)
      {
         DebugSceneId = debugSceneId;
         DebugSourceName = debugSourceName;
      }
      // This lets the Load function add the target scene in a second pass after construction.
      public Unit TargetSceneUnit { get; set; }
   }

   public class ReturnArrow: Arrow
   {
      private ReturnArrow() { }
      // This lets the Load function make arrows. 
      public ReturnArrow(
         Unit targetUnit,
         CodeTree code) : base(targetUnit, code)
      {
      }
   }

   public class ReactionArrow: Arrow
   {
      private string SourceName;
      private string SourceId;

      public string UniqueId
      {
         get => BuildUniqueId(SourceName, SourceId);
         private set { }
      }

      private ReactionArrow() { }
      // This lets the Load function make arrows. 
      public ReactionArrow(
         Unit targetUnit,
         CodeTree code,
         string sourceName,
         string sourceId): base (targetUnit, code)
      {
         SourceName = sourceName;
         SourceId = sourceId;
      }
   }

   public class Unit: WithId
   {
      // When we save the game state, we don't save the units, reactions, code, etc. That is already coming from the .graphml files. Instead, when there is a reference to a unit, we just save the file name and internal ID of the unit. We hook up to the actual units after deserialization based on the file and ID.
      private string SourceName;
      private string SourceId;
      public string UniqueId
      {
         get => BuildUniqueId(SourceName, SourceId);
         private set { }
      }

      // Each Unit represents a unit of play. It has two parts:
      // a. The text that describes the opposing turn (the "action"), ex. "@Black Bart said, "I'm gonna burn this town to the ground!"
      // b. The list of texts that describes the options for your turn, ex. "Try to reason with him.", "Shoot him.", etc.
      public CodeTree ActionCode { get; private set; }
   
      private List<Arrow> Arrows = new List<Arrow>();
      public IEnumerable<Arrow> GetArrows ()
      {
         return Arrows;
      }

      // The only way to make a Unit is through the Load function, which creates all of them.
      private Unit() { }

      // Well, actually we also need to create them on the fly for return arrows.
      public static Unit BuildReturnUnitFor(
         List<ReturnArrow> returnArrows)
      {
         var result = new Unit();
         result.SourceId = "returnUnit";
         result.SourceName = "returnUnit";
         result.ActionCode = CodeTree.Compile("", "returnUnit", new Dictionary<string, Setting>());
         foreach (var returnArrow in returnArrows)
         {
            var mergeArrow = new MergeArrow(returnArrow.TargetUnit, returnArrow.Code, "returnArrow", "returnArrow");
            result.Arrows.Add(mergeArrow);
         }
         return result;
      }

      private static Dictionary<string, Setting> LoadSettings(
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

      public static (Unit, Dictionary<string, Unit>, Dictionary<string, ReactionArrow>, Dictionary<string, Setting>) Load(
         string sourceDirectory)
      {
         var settings = LoadSettings(sourceDirectory);

         // Load all the graphml files in the source directory.
         var sourcePaths = Directory.GetFiles(sourceDirectory, "*.graphml");
         if (sourcePaths.Length < 1)
            throw new InvalidOperationException(string.Format($"no .graphml files in directory {sourceDirectory}"));

         // These are the return values.
         Unit firstUnit = null;
         var unitsByUniqueId = new Dictionary<string, Unit>();
         var reactionArrowsByUniqueId = new Dictionary<string, ReactionArrow>();

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
               Unit unit = new Unit();
               unit.SourceName = sourceName;
               unit.SourceId = nodeId;
               unitsByUniqueId[sourceName + ":" + nodeId] = unit;
               unit.ActionCode = CodeTree.Compile(label, sourceName, settings);
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
                     if (firstUnit != null)
                        throw new InvalidOperationException(string.Format($"{sourceName}: More than one start scene"));
                     firstUnit = unit;
                  }
               }
            }

            // Create and attach merges and reactions to the actions we just created.
            foreach (var (edgeId, sourceNodeId, targetNodeId, label) in graphml.Edges())
            {
               // Point the arrow to its target action.
               if (!unitsByNodeId.TryGetValue(targetNodeId, out var targetUnit))
                  throw new InvalidOperationException(string.Format($"{sourceName}: Internal error: no node declaration for referenced target node '{targetNodeId}'"));

               var code = CodeTree.Compile(label, sourceName, settings);
               EvaluateSettingsReport(code, sourceName, settingsReportWriter);
               var (isMerge, referencedSceneId, isReturn) = EvaluateArrowType(code);
               Arrow arrow;
               if (isMerge)
               {
                  arrow = new MergeArrow(targetUnit, code, referencedSceneId, sourceName);
                  if (referencedSceneId != null)
                     mergeFixups.Add(arrow as MergeArrow);
               }
               else if (isReturn)
                  arrow = new ReturnArrow(targetUnit, code);
               else
               {
                  var reactionArrow = new ReactionArrow(targetUnit, code, sourceName, edgeId);
                  reactionArrowsByUniqueId[reactionArrow.UniqueId] = reactionArrow;
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
            if (!unitsBySceneId.TryGetValue(mergeArrow.DebugSceneId, out var targetSceneUnit))
               throw new InvalidOperationException(string.Format($"No scene declaration for referenced scene ID '{mergeArrow.DebugSceneId}'"));
            mergeArrow.TargetSceneUnit = targetSceneUnit;
         }

         settingsReportWriter.Close();

         if (firstUnit == null)
            throw new InvalidOperationException("No start scene found.");

         return (firstUnit, unitsByUniqueId, reactionArrowsByUniqueId, settings);

         // Some helper functions.

         string EvaluateScene(
            CodeTree codeTree)
         {
            return codeTree
               .Traverse()
               .Where(code => code is SceneCode)
               .Select(code => (code as SceneCode).SceneId)
               .DefaultIfEmpty(null)
               .First();
         }

         (bool, string, bool) EvaluateArrowType(
            CodeTree codeTree)
         {
            bool isMerge = false;
            bool isReturn = false;
            string referencedSceneId = null;
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
            foreach(var code in codeTree.Traverse())
            {
               switch (code)
               {
                  case SetCode setCode:
                     Write("set", setCode.GetExpressions());
                     break;
                  case WhenCode whenCode:
                     Write("when", whenCode.GetExpressions());
                     break;
                  case IfCode ifCode:
                     Write("if", ifCode.GetExpressions());
                     break;
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
   }
}
