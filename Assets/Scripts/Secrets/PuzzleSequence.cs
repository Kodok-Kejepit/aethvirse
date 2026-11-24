using System.Collections.Generic;
using Aethvirse.Interaction;
using Aethvirse.UI;
using Aethvirse.World;
using UnityEngine;

namespace Aethvirse.Secrets
{
    /// <summary>A puzzle (§9 torches, bells, plates): use its pieces in the right order; a wrong step resets. Solving sets a flag.</summary>
    public sealed class PuzzleSequence : MonoBehaviour
    {
        [SerializeField] List<Interactable> pieces = new List<Interactable>();
        [Tooltip("Order of piece indices.")]
        [SerializeField] List<int> order = new List<int>();
        [SerializeField] string solvedFlag = "";

        int _step;

        void Start()
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                int index = i;
                pieces[i].Used += _ => Press(index);
            }
        }

        public bool Solved => GameSession.Instance.Flags.Has(solvedFlag);

        /// <summary>Pure step logic (unit tested): returns the next step, -1 when solved, 0 after a wrong piece.</summary>
        public static int Step(List<int> order, int step, int piece)
        {
            if (step < 0 || step >= order.Count) return 0;
            if (order[step] != piece) return piece == order[0] ? 1 : 0;
            return step + 1 >= order.Count ? -1 : step + 1;
        }

        void Press(int piece)
        {
            if (Solved) return;
            int next = Step(order, _step, piece);
            if (next == -1)
            {
                GameSession.Instance.Flags.SetFlag(solvedFlag);
                Codex.Notify("Something clicks deep below.", CodexTone.Anomaly);
                _step = 0;
                return;
            }
            if (next == 0 && _step > 0) Codex.Notify("The sound dies. Start again.", CodexTone.Info);
            _step = next;
        }
    }
}
