using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace Gamebook
{
   //  All of the graphml source code gets converted into this data structure.

   public class Arrow
   {
      public Unit TargetUnit { get; protected set; }
      public Code Code { get; protected set;  }

      protected Arrow() { }
      protected Arrow(
         Unit targetUnit,
         Code code)
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
         Code code,
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
         Code code) : base(targetUnit, code)
      {
      }
   }

   public class ReactionArrow: Arrow
   {
      private ReactionArrow() { }
      // This lets the Load function make arrows. 
      public ReactionArrow(
         Unit targetUnit,
         Code code): base (targetUnit, code)
      {
      }
   }

   [JsonObject(MemberSerialization.OptIn)]
   public class Unit
   {
      private class Converter: JsonConverter<Unit>
      {
         Dictionary<string, Unit> UnitsBySourceAndId;

         public Converter(
            Dictionary<string, Unit> unitsBySourceAndId)
         {
            UnitsBySourceAndId = unitsBySourceAndId;
         }

         public override void WriteJson(JsonWriter writer, Unit value, JsonSerializer serializer)
         {
            throw new NotImplementedException();
         }

         public override Unit ReadJson(JsonReader reader, Type objectType, Unit existingValue, bool hasExistingValue, JsonSerializer serializer)
         {
            var jsonObject = JObject.Load(reader);
            var sourceName = (string)jsonObject["SourceName"];
            var sourceId = (string)jsonObject["SourceId"];
            return UnitsBySourceAndId[sourceName + ":" + sourceId];
         }
      }

      // When we save the game state, we don't save the units, reactions, code, etc. That is already coming from the .graphml files. Instead, when there is a reference to a unit, we just save the file name and internal ID of the unit. We hook up to the actual units after deserialization based on the file and ID.
      [JsonProperty]
      private string SourceName;
      [JsonProperty]
      private string SourceId;
      public string Id
      {
         get
         {
            return SourceName + ":" + SourceId;
         }
         private set { }
      }

      // Each Unit represents a unit of play. It has two parts:
      // a. The text that describes the opposing turn (the "action"), ex. "@Black Bart said, "I'm gonna burn this town to the ground!"
      // b. The list of texts that describes the options for your turn, ex. "Try to reason with him.", "Shoot him.", etc.
      public Code ActionCode { get; private set; }
   
      private List<Arrow> Arrows = new List<Arrow>();
      public IEnumerable<Arrow> GetArrows ()
      {
         return Arrows;
      }

      // The only way to make a Unit is through the Load function, which creates all of them.
      private Unit() { }

      public static JsonConverter LoadConverter(
         string sourceDirectory)
      {
         Load(sourceDirectory, out var first, out var unitsBySourceAndId);
         return new Converter(unitsBySourceAndId);
      }

      public static Unit LoadFirst(
         string sourceDirectory)
      {
         Load(sourceDirectory, out var first, out var unitsBySourceAndId);
         return first;
      }

      private static void Load(
         string sourceDirectory,
         out Unit startUnit,
         out Dictionary<string, Unit> unitsBySourceAndId)
      {
         // Load all the graphml files in the source directory.
         var sourcePaths = Directory.GetFiles(sourceDirectory, "*.graphml");
         if (sourcePaths.Length < 1)
            Log.Fail(String.Format($"no .graphml files in directory {sourceDirectory}"));

         startUnit = null;

         // Create a temporary list of actions from the nodes in the graphml that have scene IDs, so we can link merges to them in this routine later.
         var unitsBySceneId = new Dictionary<string, Unit>();
         // Create a temporary list of merges that need to be linked to actions by scene ID.
         var mergeFixups = new List<MergeArrow>();

         unitsBySourceAndId = new Dictionary<string, Unit>();

         var settingsReportWriter = new StreamWriter("settings.csv", false);
         settingsReportWriter.WriteLine("SETTING,OPERATION,VALUE,FILE");

         foreach (var sourcePath in sourcePaths)
         {
            var sourceName = Path.GetFileName(sourcePath);

            Log.SetSourceName(sourceName);

            // Create a temporary list of actions from the nodes in the graphml, so we can link arrows to them in this routine later.
            var unitsByNodeId = new Dictionary<string, Unit>();

            var graphml = new Graphml(File.ReadAllText(sourcePath));
            foreach (var (nodeId, label) in graphml.Nodes())
            {
               Unit unit = new Unit();
               unit.SourceName = sourceName;
               unit.SourceId = nodeId;
               unitsBySourceAndId[sourceName + ":" + nodeId] = unit;
               unit.ActionCode = Code.Compile(label);
               EvaluateSettingsReport(unit.ActionCode, sourceName, settingsReportWriter);
               unitsByNodeId.Add(nodeId, unit);

               // Check if there's a [scene ID] declaration.
               var declaredSceneId = EvaluateScene(unit.ActionCode);
               if (declaredSceneId != null)
               {
                  if (unitsBySceneId.ContainsKey(declaredSceneId))
                     Log.Fail(String.Format($"Scene '{declaredSceneId}' declared twice"));
                  unitsBySceneId.Add(declaredSceneId, unit);
                  if (declaredSceneId == "start")
                  {
                     if (startUnit != null)
                        Log.Fail("More than one start scene");
                     startUnit = unit;
                  }
               }
            }

            // Create and attach merges and reactions to the actions we just created.
            foreach (var (sourceNodeId, targetNodeId, label) in graphml.Edges())
            {
               // Point the arrow to its target action.
               if (!unitsByNodeId.TryGetValue(targetNodeId, out var targetUnit))
                  Log.Fail($"Internal error: no node declaration for referenced target node '{targetNodeId}'");

               Code code = Code.Compile(label);
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
                  arrow = new ReactionArrow(targetUnit, code);

               // Add the arrow to the source action's arrows.
               if (!unitsByNodeId.TryGetValue(sourceNodeId, out var sourceUnit))
                  Log.Fail($"Internal error: no node declaration for referenced source node '{sourceNodeId}'");
               sourceUnit.Arrows.Add(arrow);
            }
         }

         Log.SetSourceCode(null);

         // Make a second pass to point merges that reference scenes to the scene's first action.
         foreach (var mergeArrow in mergeFixups)
         {
            Log.SetSourceName(mergeArrow.DebugSourceName);
            if (!unitsBySceneId.TryGetValue(mergeArrow.DebugSceneId, out var targetSceneUnit))
               Log.Fail($"No scene declaration for referenced scene ID '{mergeArrow.DebugSceneId}'");
            mergeArrow.TargetSceneUnit = targetSceneUnit;
         }

         settingsReportWriter.Close();

         Log.SetSourceName(null);

         if (startUnit == null)
            Log.Fail("No start scene found.");


         // Some helper functions.

         string EvaluateScene(
            Code topCode)
         {
            string result = null;
            topCode.Traverse((code) =>
            {
               switch (code)
               {
                  case SceneCode sceneCode:
                     result = sceneCode.SceneId;
                     return true;
               }
               return false;
            });
            return result;
         }

         (bool, string, bool) EvaluateArrowType(
            Code topCode)
         {
            bool isMerge = false;
            bool isReturn = false;
            string referencedSceneId = null;
            topCode.Traverse((code) =>
            {
               switch (code)
               {
                  case MergeCode mergeCode:
                     if (isReturn)
                        throw new InvalidOperationException(string.Format($"Can't return and merge in the same arrow."));
                     isMerge = true;
                     referencedSceneId = mergeCode.SceneId;
                     return true;
                  case ReturnCode returnCode:
                     if (isMerge)
                        throw new InvalidOperationException(string.Format($"Can't merge and return in the same arrow."));
                     isReturn = true;
                     return true;
               }
               return false;
            });
            return (isMerge, referencedSceneId, isReturn);
         }

         void EvaluateSettingsReport(
            Code topCode,
            string sourceName,
            StreamWriter writer)
         {
            var sourceNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceName);
            topCode.Traverse((code) =>
            {
               switch (code)
               {
                  case SetCode setCode:
                     Write("set", setCode.GetExpressions());
                     return true;
                  case WhenCode whenCode:
                     Write("when", whenCode.GetExpressions());
                     return true;
                  case IfCode ifCode:
                     Write("if", ifCode.GetExpressions());
                     return true;
               }
               return false;
            });

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
