using Aethvirse.Skills;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aethvirse.Input.Touch
{
    /// <summary>Touch button for a skill slot: casts through SkillCaster, shows the skill colour, initials and a cooldown sweep.</summary>
    public sealed class TouchSkillButton : MonoBehaviour, IPointerDownHandler
    {
        SkillCaster _caster;
        int _slot;
        Image _face;
        Image _cooldown;
        Text _label;
        string _shownId;

        public void Setup(SkillCaster caster, int slot, Image face, Image cooldown, Text label)
        {
            _caster = caster;
            _slot = slot;
            _face = face;
            _cooldown = cooldown;
            _label = label;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_caster != null) _caster.Buffer(_slot);
        }

        void Update()
        {
            if (_caster == null || _caster.Stats == null) return;
            SkillData skill = _caster.Stats.SlotSkill(_slot);
            string id = skill != null ? skill.id : null;
            if (id != _shownId)
            {
                _shownId = id;
                _label.text = skill != null ? Initials(skill.displayName) : "";
            }
            if (skill == null)
            {
                _face.color = new Color(1f, 1f, 1f, 0.15f);
                _cooldown.fillAmount = 0f;
                return;
            }
            Color c = skill.iconColor;
            c.a = _caster.Stats.Mana.CanPay(skill.mpCost) ? 0.75f : 0.3f;
            _face.color = c;
            _cooldown.fillAmount = _caster.CooldownRatio(skill);
        }

        static string Initials(string name)
        {
            string[] words = name.Replace("'", "").Split(' ');
            return words.Length > 1 ? $"{words[0][0]}{words[1][0]}" : name.Substring(0, Mathf.Min(2, name.Length));
        }
    }
}
