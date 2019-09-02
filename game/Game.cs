using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gamebook
{
   public partial class Game
   {
      // Game is the API the UI uses to interact with the UI-independent game engine.

      // VARIABLES
      private Page CurrentPage;

      // Pop back to old pages to implement undo.
      private Stack<Page> UndoStack = new Stack<Page>();

      public const char NegativeDebugTextStart = '′';
      public const char PositiveDebugTextStart = '″';
      public const char DebugTextStop = '‴';

      // Add annotations about how merges were done, etc.
      public static bool DebugMode;

      // FUNCTIONS

      public string GetActionText()
      {
         return CurrentPage.ActionText;
      }

      public IEnumerable<string> GetReactionTextsByScore()
      {
         // -1 scores are links.
         return CurrentPage.Reactions.OrderByDescending(pair => pair.Value.Score).Where(pair => pair.Value.Score != -1).Select(pair => pair.Key);
      }

      public Game(
         Unit firstUnitInGame)
      {
         // This constructs the first page right away, ready to go, starting with the first unit in the game. The game is always in a valid state, that is, it contains a completed page, ready to display.
         CurrentPage = new Page();
         CurrentPage.Build(firstUnitInGame, "");
      }

      public Game(
         StreamReader reader,
         Dictionary<string, Unit> unitsByUniqueId,
         Dictionary<string, ReactionArrow> reactionArrowsByUniqueId)
      {
         // This reads the state of an existing game and links it to the static parts of the game in the dictionaries.
         CurrentPage = Page.TryLoad(reader, unitsByUniqueId, reactionArrowsByUniqueId);
         if (CurrentPage == null)
            throw new InvalidOperationException(string.Format($"Save file is empty."));
         while (true)
         {
            var undoPage = Page.TryLoad(reader, unitsByUniqueId, reactionArrowsByUniqueId);
            if (undoPage == null)
               break;
            UndoStack.Push(undoPage);
         }
         // Flip the stack so it goes the right way.
         UndoStack = new Stack<Page>(UndoStack);
      }

      public void Save(
         StreamWriter writer)
      {
         CurrentPage.Save(writer);
         foreach (var page in UndoStack)
            page.Save(writer);
      }

      public void Undo()
      {
         if (CanUndo())
            CurrentPage = UndoStack.Pop();
      }

      public bool CanUndo()
      {
         return UndoStack.Count != 0;
      }

      public void MoveToReaction(
         string reactionText)
      {
         // Before we change the page, save the old page for restoration on undo.
         UndoStack.Push(CurrentPage);

         CurrentPage = CurrentPage.BuildNext(reactionText);
      }

      public void Set(
         string id,
         string value)
      {
         CurrentPage.Settings[id] = new StringSetting(value);
      }

      public string Get(
         string id)
      {
         return CurrentPage.DereferenceString(id);
      }
   }
}
