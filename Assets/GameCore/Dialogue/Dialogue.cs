using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.Dialogue
{
    public class Dialogue : IRegisteredEvent
    {
        private Text m_uiText;
        private TextMeshProUGUI m_textMeshProUGUI;

        private GroupVariable m_groupVariable = new GroupVariable();
        private int m_currentIndex = 0;

        private List<CommandInfo> m_commands = new List<CommandInfo>();

        public bool isDialoguing
        {
            get
            {
                return m_groupVariable.currentCommandEnd == false;
            }
        }
        public Dialogue Initialization()
        {
            m_groupVariable.Initialization();
            m_uiText = null;
            m_currentIndex = 0;
            return this;
        }

        /// <summary>
        /// 設定該 Dialogue 使用的 Text 物件
        /// </summary>
        /// <param name="uiText"></param>
        /// <returns></returns>
        public Dialogue SetDialogue(Text uiText)
        {
            this.m_uiText = uiText;
            this.m_uiText.text = string.Empty;
            return this;
        }

        /// <summary>
        /// 設定該 Dialogue 使用的 Text 物件
        /// </summary>
        /// <param name="textMeshProUGUI"></param>
        /// <returns></returns>
        public Dialogue SetDialogue(TextMeshProUGUI textMeshProUGUI)
        {
            m_textMeshProUGUI = textMeshProUGUI;
            m_textMeshProUGUI.text = string.Empty;
            return this;
        }

        public Dialogue ParseCommands(List<string> originalContents)
        {
            m_commands = Tokenize.ParsersCommand(originalContents);
            return this;
        }

        public void Update(float time)
        {
            if (m_currentIndex < m_commands.Count)
            {
                m_commands[m_currentIndex].command.Do(ref m_groupVariable);

                if (m_groupVariable.currentCommandEnd && m_groupVariable.isPause == false)
                {
                    NextDialogueCommand();
                }
            }
            UpdateUI();
        }

        public void UpdateUI()
        {
            if (m_uiText == null)
            {
                return;
            }

            m_uiText.text = m_groupVariable.getDialogue;
        }

        public void NextDialogue(object o)
        {
            if (m_groupVariable.isPause)
            {
                m_groupVariable.isPause = false;
                NextDialogueCommand();
                m_groupVariable.Initialization();
            }
        }

        public void Registered()
        {
            DialogueManager.instance.Registered(this);
        }

        public void UnRegistered()
        {
            DialogueManager.instance.UnRegistered(this);
        }

        private void NextDialogueCommand()
        {
            m_currentIndex++;
            m_groupVariable.dialogueContent = m_groupVariable.dialogueContent + m_groupVariable.cacheDialogue;
            m_groupVariable.cacheDialogue = string.Empty;
        }
    }
}