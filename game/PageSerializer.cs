#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace Gamebook
{
   static class PageSerializer
   {
      // No state, just two related routines to save and restore pages.

      public const char SaveFileDelimiter = '∙';

      public static void Save(
         Page page,
         TextWriter writer)
      {
         foreach (var setting in page.Settings)
         {
            writer.Write("s" + SaveFileDelimiter + setting.Key + SaveFileDelimiter);
            switch (setting.Value)
            {
               case StringSetting stringSetting:
                  writer.WriteLine("s" + SaveFileDelimiter + stringSetting.Value);
                  break;
               case ScoreSetting scoreSetting:
                  writer.WriteLine("c" + SaveFileDelimiter + scoreSetting.GetChosenCount().ToString() + SaveFileDelimiter + scoreSetting.GetOpportunityCount());
                  break;
               case BooleanSetting booleanSetting:
                  writer.WriteLine("b" + SaveFileDelimiter + (booleanSetting.Value ? "1" : "0"));
                  break;
            }
         }

         foreach (var node in page.NextTargetNodeOnReturn)
            writer.WriteLine("n" + SaveFileDelimiter + node.UniqueId);

         writer.WriteLine("a" + SaveFileDelimiter + page.ActionText);

         foreach (var reaction in page.Reactions)
            writer.WriteLine("r" + SaveFileDelimiter + reaction.Value.ReactionArrow.UniqueId + SaveFileDelimiter + reaction.Value.Score + SaveFileDelimiter + reaction.Key);

         writer.WriteLine("x");
      }

      public static Page? TryLoad(
         TextReader reader,
         World world)
      {
         // Loads saved story state data and links it to the static world description. Either loads and returns a valid Page or returns null if it reaches the end of the reader. Run this multiple times to read multiple Pages from the reader.

         var settings = new Dictionary<string, Setting>(world.InitialSettings);
         string actionText = "";
         var reactions = new Dictionary<string, ScoredReactionArrow>();
         var nextTargetNodeOnReturn = new Stack<Node>();

         // End of file right at the beginning (maybe after an 'x' operation) indicates a valid end of the file. That means we're done reading all the pages in the file.
         var line = reader.ReadLine();
         if (line == null)
            return null;

         while (true)
         {
            var parts = line.Split(SaveFileDelimiter);
            switch (parts[0])
            {
               case "s":
                  Setting setting;
                  switch (parts[2])
                  {
                     case "s":
                        setting = new StringSetting(parts[3]);
                        break;
                     case "c":
                        if (!int.TryParse(parts[3], out var chosen))
                           throw new InvalidOperationException(string.Format($"Can't parse int chosen '{parts[3]}'."));
                        if (!int.TryParse(parts[4], out var opportunity))
                           throw new InvalidOperationException(string.Format($"Can't parse int opportunity '{parts[4]}'."));
                        setting = new ScoreSetting(chosen, opportunity);
                        break;
                     case "b":
                        switch (parts[3])
                        {
                           case "0":
                              setting = new BooleanSetting(false);
                              break;
                           case "1":
                              setting = new BooleanSetting(true);
                              break;
                           default:
                              throw new InvalidOperationException(string.Format($"Unexpected boolean value '{parts[3]}'."));
                        }
                        break;
                     default:
                        throw new InvalidOperationException(string.Format($"Unexpected setting type '{parts[2]}'."));
                  }
                  settings[parts[1]] = setting;
                  break;
               case "n":
                  nextTargetNodeOnReturn.Push(world.NodesByUniqueId[parts[1]]);
                  break;
               case "a":
                  actionText = parts[1];
                  break;
               case "r":
                  if (!double.TryParse(parts[2], out var score))
                     throw new InvalidOperationException(string.Format($"Can't parse double score '{parts[2]}'."));
                  reactions.Add(parts[3], new ScoredReactionArrow(score, world.ReactionArrowsByUniqueId[parts[1]]));
                  break;
               case "x":
                  // Flip the stack so it's going the right way. When you copy a stack, it flips it.
                  nextTargetNodeOnReturn = new Stack<Node>(nextTargetNodeOnReturn);
                  return new Page(actionText, reactions, settings, nextTargetNodeOnReturn);
               default:
                  throw new InvalidOperationException(string.Format($"Unexpected operation '{parts[0]}'."));
            }
            line = reader.ReadLine();
            if (line == null)
               throw new InvalidOperationException(string.Format($"Unexpected end of save file."));
         }
      }
   }
}
